using Infrastructure.Entities;

namespace BusinessLayer.DTOs.Alerts;

/// <summary>
/// DTO for creating a new alert rule
/// </summary>
public class CreateAlertRuleDto
{
    public int MonitoredServerId { get; set; }
    public string Metric { get; set; } = string.Empty;
    public double ThresholdValue { get; set; }
    public string Comparison { get; set; } = ">";
    public string Severity { get; set; } = "Warning";
    public AlertTargetType TargetType { get; set; } = AlertTargetType.Server;
    public string? TargetId { get; set; }
    public int CooldownMinutes { get; set; } = 15;
}

/// <summary>
/// DTO for updating an existing alert rule
/// </summary>
public class UpdateAlertRuleDto
{
    public string? Metric { get; set; }
    public double? ThresholdValue { get; set; }
    public string? Comparison { get; set; }
    public string? Severity { get; set; }
    public bool? IsActive { get; set; }
    public string? TargetId { get; set; }
    public int? CooldownMinutes { get; set; }
}

/// <summary>
/// DTO for returning alert rule information
/// </summary>
public class AlertRuleDto
{
    public int Id { get; set; }
    public int MonitoredServerId { get; set; }
    public string Metric { get; set; } = string.Empty;
    public double ThresholdValue { get; set; }
    public string Comparison { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public AlertTargetType TargetType { get; set; }
    public string? TargetId { get; set; }
    public int CooldownMinutes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// DTO for returning alert information
/// </summary>
public class AlertDto
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public int MonitoredServerId { get; set; }
    public string? ServerName { get; set; }
    public int? AlertRuleId { get; set; }
}
