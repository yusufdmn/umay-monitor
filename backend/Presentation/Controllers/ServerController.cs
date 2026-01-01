using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessLayer.Services.Interfaces;
using BusinessLayer.DTOs.Agent.SystemInfo;
using Infrastructure;

namespace Presentation.Controllers;

/// <summary>
/// Handles server information requests
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ServerController : ControllerBase
{
    private readonly IAgentCommandService _commandService;
    private readonly ILogger<ServerController> _logger;
    private readonly ServerMonitoringDbContext _dbContext;

    public ServerController(
        IAgentCommandService commandService,
        ILogger<ServerController> logger,
        ServerMonitoringDbContext dbContext)
    {
        _commandService = commandService;
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all registered servers in the system
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllServers()
    {
        _logger.LogInformation("GET /api/server - Fetching all servers");

        try
        {
            var servers = await _dbContext.MonitoredServers
                .OrderBy(s => s.Name)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    hostname = s.Hostname,
                    ipAddress = s.IpAddress,
                    os = s.Os,
                    isOnline = s.IsOnline,
                    lastSeenUtc = s.LastSeenUtc
                })
                .ToListAsync();

            _logger.LogInformation("GET /api/server ? 200 OK ({Count} servers)", servers.Count);
            return Ok(servers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET /api/server ? 500");
            return StatusCode(500, new { message = "Failed to fetch servers" });
        }
    }

    /// <summary>
    /// Get system information from a server
    /// </summary>
    [HttpGet("{serverId}/info")]
    public async Task<IActionResult> GetServerInfo(int serverId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/server/{ServerId}/info", serverId);

        try
        {
            var response = await _commandService.SendCommandAsync<GetServerInfoRequest, GetServerInfoResponse>(
                serverId,
                "get-server-info",
                null,
                timeout: TimeSpan.FromSeconds(10),
                cancellationToken
            );

            if (response.Status == "ok" && response.Data != null)
            {
                _logger.LogInformation("GET /api/server/{ServerId}/info ? 200 OK (Host: {Hostname})", 
                    serverId, response.Data.Hostname);
                return Ok(response.Data);
            }

            _logger.LogWarning("GET /api/server/{ServerId}/info ? 400: {Message}", serverId, response.Message);
            return BadRequest(new { message = response.Message ?? "Failed to get server info" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            _logger.LogWarning("GET /api/server/{ServerId}/info ? 503: Server not connected", serverId);
            return StatusCode(503, new { message = "Server is not connected" });
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("GET /api/server/{ServerId}/info ? 504: Timeout", serverId);
            return StatusCode(504, new { message = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET /api/server/{ServerId}/info ? 500", serverId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
