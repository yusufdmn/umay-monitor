using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services.Interfaces;
using BusinessLayer.DTOs.Agent.ServiceManagement;

namespace Presentation.Controllers;

/// <summary>
/// Handles service management operations
/// </summary>
[Authorize]
[ApiController]
[Route("api/servers/{serverId}/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IAgentCommandService _commandService;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IAgentCommandService commandService,
        ILogger<ServicesController> logger)
    {
        _commandService = commandService;
        _logger = logger;
    }

    /// <summary>
    /// Get list of all services on a server
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetServices(int serverId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/servers/{ServerId}/services", serverId);
        
        try
        {
            var response = await _commandService.SendCommandAsync<GetServicesRequest, GetServicesResponse>(
                serverId,
                "get-services",
                null,
                cancellationToken: cancellationToken
            );

            if (response.Status == "ok" && response.Data != null)
            {
                // Filter out invalid/not-found services (systemd artifacts)
                // These are services referenced but not actually loaded (e.g., "?" entries)
                var validServices = response.Data
                    .Where(s => !string.IsNullOrWhiteSpace(s.Name) && 
                                s.Name != "?" && 
                                s.ActiveState != "not-found")
                    .ToList();
                
                var filteredCount = response.Data.Count - validServices.Count;
                if (filteredCount > 0)
                {
                    _logger.LogDebug("Filtered out {Count} invalid/not-found services for server {ServerId}", 
                        filteredCount, serverId);
                }
                
                _logger.LogInformation("GET /api/servers/{ServerId}/services ? 200 OK ({Count} services)", 
                    serverId, validServices.Count);
                return Ok(validServices);
            }

            _logger.LogWarning("GET /api/servers/{ServerId}/services ? 400 Bad Request: {Message}", 
                serverId, response.Message);
            return BadRequest(new { message = response.Message ?? "Failed to get services" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            _logger.LogWarning("GET /api/servers/{ServerId}/services ? 503: Server not connected", serverId);
            return StatusCode(503, new { message = "Server is not connected" });
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("GET /api/servers/{ServerId}/services ? 504: Timeout", serverId);
            return StatusCode(504, new { message = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET /api/servers/{ServerId}/services ? 500: Error", serverId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific service
    /// </summary>
    [HttpGet("{serviceName}")]
    public async Task<IActionResult> GetService(int serverId, string serviceName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/servers/{ServerId}/services/{ServiceName}", serverId, serviceName);
        
        try
        {
            var response = await _commandService.SendCommandAsync<GetServiceRequest, GetServiceResponse>(
                serverId,
                "get-service",
                new GetServiceRequest { Name = serviceName },
                cancellationToken: cancellationToken
            );

            if (response.Status == "ok" && response.Data != null)
            {
                _logger.LogInformation("GET /api/servers/{ServerId}/services/{ServiceName} ? 200 OK", 
                    serverId, serviceName);
                return Ok(response.Data);
            }

            _logger.LogWarning("GET /api/servers/{ServerId}/services/{ServiceName} ? 400: {Message}", 
                serverId, serviceName, response.Message);
            return BadRequest(new { message = response.Message ?? "Failed to get service details" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            _logger.LogWarning("GET /api/servers/{ServerId}/services/{ServiceName} ? 503: Not connected", 
                serverId, serviceName);
            return StatusCode(503, new { message = "Server is not connected" });
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("GET /api/servers/{ServerId}/services/{ServiceName} ? 504: Timeout", 
                serverId, serviceName);
            return StatusCode(504, new { message = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET /api/servers/{ServerId}/services/{ServiceName} ? 500", 
                serverId, serviceName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get logs for a specific service (max 1000 lines)
    /// </summary>
    [HttpGet("{serviceName}/logs")]
    public async Task<IActionResult> GetServiceLogs(int serverId, string serviceName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/servers/{ServerId}/services/{ServiceName}/logs", serverId, serviceName);
        
        try
        {
            var response = await _commandService.SendCommandAsync<GetServiceLogRequest, GetServiceLogResponse>(
                serverId,
                "get-service-log",
                new GetServiceLogRequest { Name = serviceName },
                timeout: TimeSpan.FromSeconds(15), // Logs might take longer
                cancellationToken: cancellationToken
            );

            if (response.Status == "ok" && response.Data != null)
            {
                _logger.LogInformation("GET /api/servers/{ServerId}/services/{ServiceName}/logs ? 200 OK ({Count} lines)", 
                    serverId, serviceName, response.Data.Count);
                return Ok(response.Data);
            }

            _logger.LogWarning("GET /api/servers/{ServerId}/services/{ServiceName}/logs ? 400: {Message}", 
                serverId, serviceName, response.Message);
            return BadRequest(new { message = response.Message ?? "Failed to get service logs" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            _logger.LogWarning("GET /api/servers/{ServerId}/services/{ServiceName}/logs ? 503: Not connected", 
                serverId, serviceName);
            return StatusCode(503, new { message = "Server is not connected" });
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("GET /api/servers/{ServerId}/services/{ServiceName}/logs ? 504: Timeout", 
                serverId, serviceName);
            return StatusCode(504, new { message = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET /api/servers/{ServerId}/services/{ServiceName}/logs ? 500", 
                serverId, serviceName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Restart a service
    /// </summary>
    [HttpPost("{serviceName}/restart")]
    public async Task<IActionResult> RestartService(int serverId, string serviceName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /api/servers/{ServerId}/services/{ServiceName}/restart", serverId, serviceName);

        try
        {
            var response = await _commandService.SendCommandAsync<RestartServiceRequest, RestartServiceResponse>(
                serverId,
                "restart-service",
                new RestartServiceRequest { Name = serviceName },
                timeout: TimeSpan.FromSeconds(30), // Restart might take time
                cancellationToken: cancellationToken
            );

            if (response.Status == "ok")
            {
                _logger.LogInformation("POST /api/servers/{ServerId}/services/{ServiceName}/restart ? 200 OK", 
                    serverId, serviceName);
                return Ok(new { message = $"Service {serviceName} restarted successfully" });
            }

            _logger.LogWarning("POST /api/servers/{ServerId}/services/{ServiceName}/restart ? 400: {Message}", 
                serverId, serviceName, response.Message);
            return BadRequest(new { message = response.Message ?? "Failed to restart service" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            _logger.LogWarning("POST /api/servers/{ServerId}/services/{ServiceName}/restart ? 503: Not connected", 
                serverId, serviceName);
            return StatusCode(503, new { message = "Server is not connected" });
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("POST /api/servers/{ServerId}/services/{ServiceName}/restart ? 504: Timeout (after retries)", 
                serverId, serviceName);
            return StatusCode(504, new { message = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST /api/servers/{ServerId}/services/{ServiceName}/restart ? 500", 
                serverId, serviceName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
