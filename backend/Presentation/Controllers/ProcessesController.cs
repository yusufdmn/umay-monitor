using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services.Interfaces;
using BusinessLayer.DTOs.Agent.ProcessManagement;

namespace Presentation.Controllers;

/// <summary>
/// Handles process monitoring operations
/// </summary>
[Authorize]
[ApiController]
[Route("api/servers/{serverId}/[controller]")]
public class ProcessesController : ControllerBase
{
    private readonly IAgentCommandService _commandService;
    private readonly ILogger<ProcessesController> _logger;

    public ProcessesController(
        IAgentCommandService commandService,
        ILogger<ProcessesController> logger)
    {
        _commandService = commandService;
        _logger = logger;
    }

    /// <summary>
    /// Get list of all running processes on a server
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProcesses(int serverId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/servers/{ServerId}/processes", serverId);

        try
        {
            var response = await _commandService.SendCommandAsync<GetProcessesRequest, GetProcessesResponse>(
                serverId,
                "get-processes",
                null,
                timeout: TimeSpan.FromSeconds(15), // Process listing might take time
                cancellationToken: cancellationToken
            );

            if (response.Status == "ok" && response.Data != null)
            {
                _logger.LogInformation("GET /api/servers/{ServerId}/processes ? 200 OK ({Count} processes)",
                    serverId, response.Data.Count);
                return Ok(response.Data);
            }

            _logger.LogWarning("GET /api/servers/{ServerId}/processes ? 400: {Message}",
                serverId, response.Message);
            return BadRequest(new { message = response.Message ?? "Failed to get processes" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            _logger.LogWarning("GET /api/servers/{ServerId}/processes ? 503: Server not connected", serverId);
            return StatusCode(503, new { message = "Server is not connected" });
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("GET /api/servers/{ServerId}/processes ? 504: Timeout", serverId);
            return StatusCode(504, new { message = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET /api/servers/{ServerId}/processes ? 500", serverId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific process
    /// </summary>
    [HttpGet("{pid}")]
    public async Task<IActionResult> GetProcess(int serverId, int pid, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/servers/{ServerId}/processes/{Pid}", serverId, pid);

        try
        {
            var response = await _commandService.SendCommandAsync<GetProcessRequest, GetProcessResponse>(
                serverId,
                "get-process",
                new GetProcessRequest { Pid = pid },
                cancellationToken: cancellationToken
            );

            if (response.Status == "ok" && response.Data != null)
            {
                _logger.LogInformation("GET /api/servers/{ServerId}/processes/{Pid} ? 200 OK (Name: {ProcessName})",
                    serverId, pid, response.Data.Name);
                return Ok(response.Data);
            }

            _logger.LogWarning("GET /api/servers/{ServerId}/processes/{Pid} ? 400: {Message}",
                serverId, pid, response.Message);
            return BadRequest(new { message = response.Message ?? "Failed to get process details" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            _logger.LogWarning("GET /api/servers/{ServerId}/processes/{Pid} ? 503: Server not connected",
                serverId, pid);
            return StatusCode(503, new { message = "Server is not connected" });
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("GET /api/servers/{ServerId}/processes/{Pid} ? 504: Timeout", serverId, pid);
            return StatusCode(504, new { message = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET /api/servers/{ServerId}/processes/{Pid} ? 500", serverId, pid);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
