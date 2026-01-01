using BusinessLayer.DTOs.Alerts;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly ServerMonitoringDbContext _dbContext;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        ServerMonitoringDbContext dbContext,
        ILogger<AlertsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all alerts (with optional filtering)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AlertDto>>> GetAlerts(
        [FromQuery] int? serverId = null,
        [FromQuery] bool? acknowledged = null,
        [FromQuery] string? severity = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _dbContext.Alerts
            .Include(a => a.MonitoredServer)
            .AsQueryable();

        // Apply filters
        if (serverId.HasValue)
        {
            query = query.Where(a => a.MonitoredServerId == serverId.Value);
        }

        if (acknowledged.HasValue)
        {
            query = query.Where(a => a.IsAcknowledged == acknowledged.Value);
        }

        if (!string.IsNullOrEmpty(severity))
        {
            query = query.Where(a => a.Severity.ToLower() == severity.ToLower());
        }

        // Pagination
        var total = await query.CountAsync();
        var alerts = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AlertDto
            {
                Id = a.Id,
                CreatedAtUtc = a.CreatedAtUtc,
                Title = a.Title,
                Message = a.Message,
                Severity = a.Severity,
                IsAcknowledged = a.IsAcknowledged,
                AcknowledgedAtUtc = a.AcknowledgedAtUtc,
                MonitoredServerId = a.MonitoredServerId,
                ServerName = a.MonitoredServer.Name,
                AlertRuleId = a.AlertRuleId
            })
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", total.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(alerts);
    }

    /// <summary>
    /// Get alerts for a specific server
    /// </summary>
    [HttpGet("servers/{serverId}")]
    public async Task<ActionResult<List<AlertDto>>> GetServerAlerts(
        int serverId,
        [FromQuery] bool? acknowledged = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        return await GetAlerts(serverId, acknowledged, null, page, pageSize);
    }

    /// <summary>
    /// Get a specific alert
    /// </summary>
    [HttpGet("{alertId}")]
    public async Task<ActionResult<AlertDto>> GetAlert(int alertId)
    {
        var alert = await _dbContext.Alerts
            .Include(a => a.MonitoredServer)
            .Where(a => a.Id == alertId)
            .Select(a => new AlertDto
            {
                Id = a.Id,
                CreatedAtUtc = a.CreatedAtUtc,
                Title = a.Title,
                Message = a.Message,
                Severity = a.Severity,
                IsAcknowledged = a.IsAcknowledged,
                AcknowledgedAtUtc = a.AcknowledgedAtUtc,
                MonitoredServerId = a.MonitoredServerId,
                ServerName = a.MonitoredServer.Name,
                AlertRuleId = a.AlertRuleId
            })
            .FirstOrDefaultAsync();

        if (alert == null)
        {
            return NotFound(new { message = "Alert not found" });
        }

        return Ok(alert);
    }

    /// <summary>
    /// Acknowledge an alert
    /// </summary>
    [HttpPost("{alertId}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(int alertId)
    {
        var alert = await _dbContext.Alerts.FindAsync(alertId);

        if (alert == null)
        {
            return NotFound(new { message = "Alert not found" });
        }

        if (alert.IsAcknowledged)
        {
            return BadRequest(new { message = "Alert already acknowledged" });
        }

        // Get user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            alert.AcknowledgedByUserId = userId;
        }

        alert.IsAcknowledged = true;
        alert.AcknowledgedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Alert {AlertId} acknowledged by user {UserId}", alertId, userId);

        return Ok(new { message = "Alert acknowledged successfully" });
    }

    /// <summary>
    /// Acknowledge multiple alerts
    /// </summary>
    [HttpPost("acknowledge-batch")]
    public async Task<IActionResult> AcknowledgeBatch([FromBody] List<int> alertIds)
    {
        if (alertIds == null || !alertIds.Any())
        {
            return BadRequest(new { message = "No alert IDs provided" });
        }

        var alerts = await _dbContext.Alerts
            .Where(a => alertIds.Contains(a.Id) && !a.IsAcknowledged)
            .ToListAsync();

        if (!alerts.Any())
        {
            return NotFound(new { message = "No unacknowledged alerts found with provided IDs" });
        }

        // Get user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = null;
        if (int.TryParse(userIdClaim, out int parsedUserId))
        {
            userId = parsedUserId;
        }

        var now = DateTime.UtcNow;
        foreach (var alert in alerts)
        {
            alert.IsAcknowledged = true;
            alert.AcknowledgedAtUtc = now;
            if (userId.HasValue)
            {
                alert.AcknowledgedByUserId = userId.Value;
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Batch acknowledged {Count} alerts by user {UserId}", alerts.Count, userId);

        return Ok(new { message = $"{alerts.Count} alerts acknowledged successfully", count = alerts.Count });
    }

    /// <summary>
    /// Delete an alert
    /// </summary>
    [HttpDelete("{alertId}")]
    public async Task<IActionResult> DeleteAlert(int alertId)
    {
        var alert = await _dbContext.Alerts.FindAsync(alertId);

        if (alert == null)
        {
            return NotFound(new { message = "Alert not found" });
        }

        _dbContext.Alerts.Remove(alert);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted alert {AlertId}", alertId);

        return Ok(new { message = "Alert deleted successfully" });
    }

    /// <summary>
    /// Delete all acknowledged alerts (cleanup)
    /// </summary>
    [HttpDelete("acknowledged")]
    public async Task<IActionResult> DeleteAcknowledged([FromQuery] int? serverId = null)
    {
        var query = _dbContext.Alerts.Where(a => a.IsAcknowledged);

        if (serverId.HasValue)
        {
            query = query.Where(a => a.MonitoredServerId == serverId.Value);
        }

        var alerts = await query.ToListAsync();
        _dbContext.Alerts.RemoveRange(alerts);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted {Count} acknowledged alerts", alerts.Count);

        return Ok(new { message = $"{alerts.Count} acknowledged alerts deleted", count = alerts.Count });
    }
}
