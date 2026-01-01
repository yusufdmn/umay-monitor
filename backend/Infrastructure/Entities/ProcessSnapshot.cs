namespace Infrastructure.Entities;

/// <summary>
/// Represents a snapshot of all running processes at a specific point in time.
/// Optional feature for detailed process monitoring.
/// </summary>
public class ProcessSnapshot
{
    public long Id { get; set; }

    /// <summary>
    /// When this process snapshot was taken
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    // Foreign key
    public int MonitoredServerId { get; set; }
    public MonitoredServer MonitoredServer { get; set; } = null!;

    // Navigation property
    public ICollection<ProcessInfo> Processes { get; set; } = new List<ProcessInfo>();
}
