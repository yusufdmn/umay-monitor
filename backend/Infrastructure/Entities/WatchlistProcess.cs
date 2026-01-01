namespace Infrastructure.Entities;

/// <summary>
/// Represents a process that is being watched for monitoring
/// </summary>
public class WatchlistProcess
{
    public int Id { get; set; }
    
    /// <summary>
    /// The monitored server this process belongs to
    /// </summary>
    public int MonitoredServerId { get; set; }
    public MonitoredServer MonitoredServer { get; set; } = null!;
    
    /// <summary>
    /// Process command line pattern to match (e.g., "python app.py", "node server.js")
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;
    
    /// <summary>
    /// When this process was added to the watchlist
    /// </summary>
    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this watchlist entry is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
