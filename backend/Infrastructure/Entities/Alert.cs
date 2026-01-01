namespace Infrastructure.Entities;

/// <summary>
/// Represents an alert that was triggered by an AlertRule.
/// </summary>
public class Alert
{
    public int Id { get; set; }

    /// <summary>
    /// When this alert was created
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Brief title of the alert (e.g., "High CPU Usage")
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed message (e.g., "CPU usage is 95%, threshold is 80%")
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Severity level (e.g., "Info", "Warning", "Critical")
    /// </summary>
    public string Severity { get; set; } = "Warning";

    /// <summary>
    /// Whether this alert has been acknowledged by a user
    /// </summary>
    public bool IsAcknowledged { get; set; }

    /// <summary>
    /// When the alert was acknowledged (if applicable)
    /// </summary>
    public DateTime? AcknowledgedAtUtc { get; set; }

    /// <summary>
    /// Who acknowledged the alert (if applicable)
    /// </summary>
    public int? AcknowledgedByUserId { get; set; }

    // Foreign keys
    public int MonitoredServerId { get; set; }
    public MonitoredServer MonitoredServer { get; set; } = null!;

    /// <summary>
    /// The rule that triggered this alert (nullable in case rule is deleted)
    /// </summary>
    public int? AlertRuleId { get; set; }
    public AlertRule? AlertRule { get; set; }

    public User? AcknowledgedByUser { get; set; }
}
