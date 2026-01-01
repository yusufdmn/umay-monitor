namespace Infrastructure.Entities;

/// <summary>
/// Represents a status change event for a monitored service.
/// </summary>
public class ServiceStatusHistory
{
    public long Id { get; set; }

    /// <summary>
    /// When this status was recorded
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// Service status (e.g., Running, Stopped, Restarting, Failed, Unknown)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Optional additional information or error message
    /// </summary>
    public string? Message { get; set; }

    // Foreign key
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}
