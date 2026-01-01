using BusinessLayer.DTOs.Alerts;
using Infrastructure;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/servers/{serverId}/alert-rules")]
public class AlertRulesController : ControllerBase
{
    private readonly ServerMonitoringDbContext _dbContext;
    private readonly ILogger<AlertRulesController> _logger;

    public AlertRulesController(
        ServerMonitoringDbContext dbContext,
        ILogger<AlertRulesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all alert rules for a specific server
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AlertRuleDto>>> GetAlertRules(int serverId, [FromQuery] bool? activeOnly = null)
    {
        var query = _dbContext.AlertRules.Where(r => r.MonitoredServerId == serverId);

        if (activeOnly == true)
        {
            query = query.Where(r => r.IsActive);
        }

        var rules = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new AlertRuleDto
            {
                Id = r.Id,
                MonitoredServerId = r.MonitoredServerId,
                Metric = r.Metric,
                ThresholdValue = r.ThresholdValue,
                Comparison = r.Comparison,
                Severity = r.Severity,
                IsActive = r.IsActive,
                TargetType = r.TargetType,
                TargetId = r.TargetId,
                CooldownMinutes = r.CooldownMinutes,
                CreatedAtUtc = r.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(rules);
    }

    /// <summary>
    /// Get a specific alert rule
    /// </summary>
    [HttpGet("{ruleId}")]
    public async Task<ActionResult<AlertRuleDto>> GetAlertRule(int serverId, int ruleId)
    {
        var rule = await _dbContext.AlertRules
            .Where(r => r.Id == ruleId && r.MonitoredServerId == serverId)
            .Select(r => new AlertRuleDto
            {
                Id = r.Id,
                MonitoredServerId = r.MonitoredServerId,
                Metric = r.Metric,
                ThresholdValue = r.ThresholdValue,
                Comparison = r.Comparison,
                Severity = r.Severity,
                IsActive = r.IsActive,
                TargetType = r.TargetType,
                TargetId = r.TargetId,
                CooldownMinutes = r.CooldownMinutes,
                CreatedAtUtc = r.CreatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (rule == null)
        {
            return NotFound(new { message = "Alert rule not found" });
        }

        return Ok(rule);
    }

    /// <summary>
    /// Create a new alert rule
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AlertRuleDto>> CreateAlertRule(int serverId, [FromBody] CreateAlertRuleDto dto)
    {
        // Verify server exists
        var serverExists = await _dbContext.MonitoredServers.AnyAsync(s => s.Id == serverId);
        if (!serverExists)
        {
            return NotFound(new { message = "Server not found" });
        }

        // Validate metric name
        var validMetrics = new[] { "CPU", "RAM", "LOAD1M", "LOAD5M", "LOAD15M", "DISKUSAGE", "NETWORKUPLOAD", "NETWORKDOWNLOAD" };
        if (!validMetrics.Contains(dto.Metric.ToUpper()))
        {
            return BadRequest(new { message = $"Invalid metric. Valid metrics: {string.Join(", ", validMetrics)}" });
        }

        // Validate comparison operator
        var validComparisons = new[] { ">", ">=", "<", "<=", "==" };
        if (!validComparisons.Contains(dto.Comparison))
        {
            return BadRequest(new { message = $"Invalid comparison. Valid operators: {string.Join(", ", validComparisons)}" });
        }

        var rule = new AlertRule
        {
            MonitoredServerId = serverId,
            Metric = dto.Metric.ToUpper(),
            ThresholdValue = dto.ThresholdValue,
            Comparison = dto.Comparison,
            Severity = dto.Severity,
            TargetType = dto.TargetType,
            TargetId = dto.TargetId,
            CooldownMinutes = dto.CooldownMinutes,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.AlertRules.Add(rule);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created alert rule {RuleId} for server {ServerId}: {Metric} {Comparison} {Threshold}",
            rule.Id, serverId, rule.Metric, rule.Comparison, rule.ThresholdValue);

        var result = new AlertRuleDto
        {
            Id = rule.Id,
            MonitoredServerId = rule.MonitoredServerId,
            Metric = rule.Metric,
            ThresholdValue = rule.ThresholdValue,
            Comparison = rule.Comparison,
            Severity = rule.Severity,
            IsActive = rule.IsActive,
            TargetType = rule.TargetType,
            TargetId = rule.TargetId,
            CooldownMinutes = rule.CooldownMinutes,
            CreatedAtUtc = rule.CreatedAtUtc
        };

        return CreatedAtAction(nameof(GetAlertRule), new { serverId, ruleId = rule.Id }, result);
    }

    /// <summary>
    /// Update an existing alert rule
    /// </summary>
    [HttpPut("{ruleId}")]
    public async Task<ActionResult<AlertRuleDto>> UpdateAlertRule(int serverId, int ruleId, [FromBody] UpdateAlertRuleDto dto)
    {
        var rule = await _dbContext.AlertRules
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.MonitoredServerId == serverId);

        if (rule == null)
        {
            return NotFound(new { message = "Alert rule not found" });
        }

        // Update fields if provided
        if (dto.Metric != null) rule.Metric = dto.Metric.ToUpper();
        if (dto.ThresholdValue.HasValue) rule.ThresholdValue = dto.ThresholdValue.Value;
        if (dto.Comparison != null) rule.Comparison = dto.Comparison;
        if (dto.Severity != null) rule.Severity = dto.Severity;
        if (dto.IsActive.HasValue) rule.IsActive = dto.IsActive.Value;
        if (dto.TargetId != null) rule.TargetId = dto.TargetId;
        if (dto.CooldownMinutes.HasValue) rule.CooldownMinutes = dto.CooldownMinutes.Value;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated alert rule {RuleId} for server {ServerId}", ruleId, serverId);

        var result = new AlertRuleDto
        {
            Id = rule.Id,
            MonitoredServerId = rule.MonitoredServerId,
            Metric = rule.Metric,
            ThresholdValue = rule.ThresholdValue,
            Comparison = rule.Comparison,
            Severity = rule.Severity,
            IsActive = rule.IsActive,
            TargetType = rule.TargetType,
            TargetId = rule.TargetId,
            CooldownMinutes = rule.CooldownMinutes,
            CreatedAtUtc = rule.CreatedAtUtc
        };

        return Ok(result);
    }

    /// <summary>
    /// Delete an alert rule
    /// </summary>
    [HttpDelete("{ruleId}")]
    public async Task<IActionResult> DeleteAlertRule(int serverId, int ruleId)
    {
        var rule = await _dbContext.AlertRules
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.MonitoredServerId == serverId);

        if (rule == null)
        {
            return NotFound(new { message = "Alert rule not found" });
        }

        _dbContext.AlertRules.Remove(rule);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted alert rule {RuleId} for server {ServerId}", ruleId, serverId);

        return Ok(new { message = "Alert rule deleted successfully" });
    }
}
