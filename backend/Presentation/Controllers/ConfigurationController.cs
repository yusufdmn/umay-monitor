using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services.Interfaces;
using BusinessLayer.DTOs.Agent.Configuration;

namespace Presentation.Controllers;

/// <summary>
/// Handles agent configuration updates
/// </summary>
[Authorize]
[ApiController]
[Route("api/servers/{serverId}/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IAgentCommandService _commandService;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(
        IAgentCommandService commandService,
        ILogger<ConfigurationController> logger)
    {
        _commandService = commandService;
        _logger = logger;
    }

    /// <summary>
    /// Update agent configuration (metrics interval and/or watchlist)
    /// PUT /api/servers/{serverId}/configuration
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateConfiguration(
        int serverId,
        [FromBody] UpdateAgentConfigRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate metrics interval if provided (agent accepts 0-3600)
            if (request.MetricsInterval.HasValue &&
                (request.MetricsInterval.Value < 0 || request.MetricsInterval.Value > 3600))
            {
                return BadRequest(new { message = "MetricsInterval must be between 0 and 3600 seconds" });
            }

            var logMessage = $"Updating configuration for server {serverId}";
            if (request.MetricsInterval.HasValue)
            {
                logMessage += $", Interval={request.MetricsInterval}s";
            }
            if (request.Watchlist != null)
            {
                var serviceCount = request.Watchlist.Services?.Count ?? 0;
                var processCount = request.Watchlist.Processes?.Count ?? 0;
                logMessage += $", Watchlist: {serviceCount} services, {processCount} processes";
            }

            _logger.LogInformation(logMessage);

            var response = await _commandService.SendCommandAsync<UpdateAgentConfigRequest, UpdateAgentConfigResponse>(
                serverId,
                "update-agent-config",
                request,
                cancellationToken: cancellationToken
            );

            if (response.Status == "ok")
            {
                return Ok(new { message = "Configuration updated successfully" });
            }

            return BadRequest(new { message = response.Message ?? "Failed to update configuration" });
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
            _logger.LogError(ex, "Error updating configuration for server {ServerId}", serverId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
