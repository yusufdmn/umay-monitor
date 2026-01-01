namespace Infrastructure.Entities;

/// <summary>
/// Represents a service that is being watched for monitoring and auto-restart
/// </summary>
public class WatchlistService
{
    public int Id { get; set; }
    
    /// <summary>
    /// The monitored server this service belongs to
    /// </summary>
    public int MonitoredServerId { get; set; }
    public MonitoredServer MonitoredServer { get; set; } = null!;
    
    /// <summary>
    /// Service name (e.g., "nginx", "docker", "ssh")
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// When this service was added to the watchlist
    /// </summary>
    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this watchlist entry is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
