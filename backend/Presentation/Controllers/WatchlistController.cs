using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services.Interfaces;

namespace Presentation.Controllers;

/// <summary>
/// Manages watchlist configuration for services and processes
/// </summary>
[Authorize]
[ApiController]
[Route("api/servers/{serverId}/watchlist")]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistService _watchlistService;
    private readonly ILogger<WatchlistController> _logger;

    public WatchlistController(
        IWatchlistService watchlistService,
        ILogger<WatchlistController> logger)
    {
        _watchlistService = watchlistService;
        _logger = logger;
    }

    /// <summary>
    /// Get current watchlist configuration (all watched services and processes)
    /// GET /api/servers/{serverId}/watchlist
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWatchlist(int serverId)
    {
        try
        {
            var config = await _watchlistService.GetWatchlistConfigAsync(serverId);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting watchlist for server {ServerId}", serverId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Add a service to the watchlist
    /// POST /api/servers/{serverId}/watchlist/services/{serviceName}
    /// </summary>
    [HttpPost("services/{serviceName}")]
    public async Task<IActionResult> AddService(int serverId, string serviceName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return BadRequest(new { message = "Service name is required" });
            }

            await _watchlistService.AddServiceAsync(serverId, serviceName);
            return Ok(new { message = $"Service '{serviceName}' added to watchlist" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            return StatusCode(503, new { message = "Server is not connected" });
        }
        catch (TimeoutException)
        {
            return StatusCode(504, new { message = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding service {ServiceName} to watchlist for server {ServerId}",
                serviceName, serverId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove a service from the watchlist
    /// DELETE /api/servers/{serverId}/watchlist/services/{serviceName}
    /// </summary>
    [HttpDelete("services/{serviceName}")]
    public async Task<IActionResult> RemoveService(int serverId, string serviceName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return BadRequest(new { message = "Service name is required" });
            }

            await _watchlistService.RemoveServiceAsync(serverId, serviceName);
            return Ok(new { message = $"Service '{serviceName}' removed from watchlist" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            return StatusCode(503, new { message = "Server is not connected" });
        }
        catch (TimeoutException)
        {
            return StatusCode(504, new { message = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing service {ServiceName} from watchlist for server {ServerId}",
                serviceName, serverId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Add a process to the watchlist
    /// POST /api/servers/{serverId}/watchlist/processes/{processName}
    /// </summary>
    [HttpPost("processes/{processName}")]
    public async Task<IActionResult> AddProcess(int serverId, string processName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return BadRequest(new { message = "Process name is required" });
            }

            await _watchlistService.AddProcessAsync(serverId, processName);
            return Ok(new { message = $"Process '{processName}' added to watchlist" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            return StatusCode(503, new { message = "Server is not connected" });
        }
        catch (TimeoutException)
        {
            return StatusCode(504, new { message = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding process {ProcessName} to watchlist for server {ServerId}",
                processName, serverId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove a process from the watchlist
    /// DELETE /api/servers/{serverId}/watchlist/processes/{processName}
    /// </summary>
    [HttpDelete("processes/{processName}")]
    public async Task<IActionResult> RemoveProcess(int serverId, string processName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return BadRequest(new { message = "Process name is required" });
            }

            await _watchlistService.RemoveProcessAsync(serverId, processName);
            return Ok(new { message = $"Process '{processName}' removed from watchlist" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            return StatusCode(503, new { message = "Server is not connected" });
        }
        catch (TimeoutException)
        {
            return StatusCode(504, new { message = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing process {ProcessName} from watchlist for server {ServerId}",
                processName, serverId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get list of watched services
    /// GET /api/servers/{serverId}/watchlist/services
    /// </summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetWatchedServices(int serverId)
    {
        try
        {
            var services = await _watchlistService.GetWatchedServicesAsync(serverId);
            return Ok(new { services });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting watched services for server {ServerId}", serverId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get list of watched processes
    /// GET /api/servers/{serverId}/watchlist/processes
    /// </summary>
    [HttpGet("processes")]
    public async Task<IActionResult> GetWatchedProcesses(int serverId)
    {
        try
        {
            var processes = await _watchlistService.GetWatchedProcessesAsync(serverId);
            return Ok(new { processes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting watched processes for server {ServerId}", serverId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
