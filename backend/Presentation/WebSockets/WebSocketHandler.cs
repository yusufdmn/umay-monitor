using BusinessLayer.Services.Interfaces;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BusinessLayer.DTOs.Agent;

namespace Presentation.WebSockets;

/// <summary>
/// Handles the lifecycle of a WebSocket connection from an agent.
/// This class is created once per connection (scoped).
/// </summary>
public class WebSocketHandler
{
    private readonly IWebSocketConnectionManager _connectionManager;
    private readonly IAgentMessageHandler _messageHandler;
    private readonly ServerMonitoringDbContext _dbContext;
    private readonly ILogger<WebSocketHandler> _logger;

    /// <summary>
    /// Injects the required singleton and scoped services.
    /// </summary>
    /// <param name="connectionManager">Manages the dictionary of active connections.</param>
    /// <param name="messageHandler">Parses and processes incoming JSON messages.</param>
    /// <param name="dbContext">DbContext for server monitoring data.</param>
    /// <param name="logger">Logger for WebSocket events.</param>
    public WebSocketHandler(
        IWebSocketConnectionManager connectionManager,
        IAgentMessageHandler messageHandler,
        ServerMonitoringDbContext dbContext,
        ILogger<WebSocketHandler> logger)
    {
        _connectionManager = connectionManager;
        _messageHandler = messageHandler;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Entry point called by Program.cs to handle a new WebSocket request.
    /// </summary>
    /// <param name="context">The HttpContext for the request.</param>
    public async Task HandleConnection(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        
        _logger.LogInformation("WebSocket connection accepted. Waiting for authentication...");

        await HandleWebSocketLoop(webSocket);
    }

    /// <summary>
    /// Listens for messages from the agent until the connection is closed.
    /// </summary>
    /// <param name="webSocket">The active WebSocket connection.</param>
    private async Task HandleWebSocketLoop(WebSocket webSocket)
    {
        var buffer = new ArraySegment<byte>(new byte[1024 * 4]);
        var messageBytes = new List<byte>();
        int? authenticatedServerId = null;
        bool isAuthenticated = false;

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                messageBytes.Clear();
                WebSocketReceiveResult receiveResult;

                do
                {
                    receiveResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Connection closed by client",
                            CancellationToken.None);
                        return;
                    }

                    if (buffer.Array != null)
                    {
                        messageBytes.AddRange(buffer.Array[..receiveResult.Count]);
                    }
                } while (!receiveResult.EndOfMessage);

                var message = Encoding.UTF8.GetString(messageBytes.ToArray());

                if (string.IsNullOrEmpty(message))
                    continue;

                if (!isAuthenticated)
                {
                    var authResult = await HandleAuthentication(message, webSocket);
                    
                    if (!authResult.IsAuthenticated)
                    {
                        _logger.LogWarning("Authentication failed. Closing connection.");
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.PolicyViolation,
                            "Authentication failed",
                            CancellationToken.None);
                        return;
                    }

                    isAuthenticated = true;
                    authenticatedServerId = authResult.ServerId;
                    
                    _logger.LogInformation("Agent authenticated successfully: Server {ServerId}", authenticatedServerId);
                }
                else
                {
                    if (authenticatedServerId.HasValue)
                    {
                        try
                        {
                            await _messageHandler.HandleMessageAsync(message, authenticatedServerId.Value)
                                .ConfigureAwait(false);

                            var server = await _dbContext.MonitoredServers.FindAsync(authenticatedServerId.Value);
                            if (server != null)
                            {
                                server.LastSeenUtc = DateTime.UtcNow;
                                await _dbContext.SaveChangesAsync();
                            }
                        }
                        catch (ObjectDisposedException ex)
                        {
                            _logger.LogError(ex, "Service was disposed - scope ended prematurely for server {ServerId}", 
                                authenticatedServerId);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message for server {ServerId}", 
                                authenticatedServerId);
                            throw;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Received message but server ID is not set");
                    }
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogError(ex, "WebSocket error for server {ServerId}", authenticatedServerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error for server {ServerId}", authenticatedServerId);
        }
        finally
        {
            if (authenticatedServerId.HasValue)
            {
                var agentId = _connectionManager.GetId(webSocket);
                if (agentId != null)
                {
                    await _connectionManager.RemoveSocket(agentId);
                }

                var server = await _dbContext.MonitoredServers.FindAsync(authenticatedServerId.Value);
                if (server != null)
                {
                    server.IsOnline = false;
                    await _dbContext.SaveChangesAsync();
                }

                _logger.LogInformation("Agent disconnected: Server {ServerId}", authenticatedServerId);
            }
        }
    }

    private async Task<(bool IsAuthenticated, int? ServerId)> HandleAuthentication(string message, WebSocket webSocket)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var baseMessage = JsonSerializer.Deserialize<BaseAgentMessage>(message, options);

            if (baseMessage == null || baseMessage.Action != "authenticate")
            {
                await SendAuthResponse(webSocket, baseMessage?.Id ?? 0, "error", "First message must be authentication");
                return (false, null);
            }

            var authPayload = baseMessage.Payload.Deserialize<AuthenticatePayload>(options);
            
            if (authPayload == null || string.IsNullOrEmpty(authPayload.AgentId) || string.IsNullOrEmpty(authPayload.Token))
            {
                _logger.LogWarning("Authentication failed: Invalid payload");
                await SendAuthResponse(webSocket, baseMessage.Id, "error", "Invalid authentication payload");
                return (false, null);
            }

            var servers = await _dbContext.MonitoredServers
                .ToListAsync(); // Get all servers

            // Find server by verifying BCrypt hash
            var server = servers.FirstOrDefault(s => 
                !string.IsNullOrEmpty(s.AgentToken) && 
                BCrypt.Net.BCrypt.Verify(authPayload.Token, s.AgentToken));

            if (server == null)
            {
                _logger.LogWarning("Authentication failed: Invalid token '{Token}'", authPayload.Token);
                await SendAuthResponse(webSocket, baseMessage.Id, "error", "Invalid credentials");
                return (false, null);
            }

            _logger.LogInformation("Authentication successful: Server {ServerId} ('{ServerName}')", 
                server.Id, server.Name);

            server.IsOnline = true;
            server.LastSeenUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            await _connectionManager.AddSocket(server.Id.ToString(), webSocket);

            await SendAuthResponse(
                webSocket, 
                baseMessage.Id, 
                "ok", 
                "Authentication successful", 
                server.Id, 
                server.Name
            );

            return (true, server.Id);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse authentication message");
            await SendAuthResponse(webSocket, 0, "error", "Invalid message format");
            return (false, null);
        }
    }

    private async Task SendAuthResponse(
        WebSocket webSocket,
        int messageId,
        string status,
        string message,
        int? serverId = null,
        string? serverName = null)
    {
        var response = new AuthenticationResponse
        {
            Status = status,
            Message = message,
            ServerId = serverId,
            ServerName = serverName
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var bytes = Encoding.UTF8.GetBytes(json);

        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }
}
