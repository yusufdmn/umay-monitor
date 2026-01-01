namespace Infrastructure.Entities;

/// <summary>
/// Represents a system service being monitored on a server.
/// </summary>
public class Service
{
    public int Id { get; set; }

    /// <summary>
    /// Service name (e.g., nginx, postgresql, docker)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this service does
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this service is considered critical (affects alert severity)
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// When this service was added to monitoring
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Foreign key
    public int MonitoredServerId { get; set; }
    public MonitoredServer MonitoredServer { get; set; } = null!;

    // Navigation property
    public ICollection<ServiceStatusHistory> StatusHistory { get; set; } = new List<ServiceStatusHistory>();
}
