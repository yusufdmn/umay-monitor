namespace Infrastructure.Entities;

/// <summary>
/// Represents a single monitored server that connects via the agent.
/// </summary>
public class MonitoredServer
{
    public int Id { get; set; }
    
    /// <summary>
    /// Friendly display name for the server (e.g., "Production Web Server 1")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Hostname of the server (from agent)
    /// </summary>
    public string Hostname { get; set; } = string.Empty;
    
    /// <summary>
    /// IP address of the server
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// Operating system (e.g., "Linux")
    /// </summary>
    public string? Os { get; set; }
    
    /// <summary>
    /// OS version string (e.g., "#75-Ubuntu...")
    /// </summary>
    public string? OsVersion { get; set; }
    
    /// <summary>
    /// Kernel version (e.g., "5.15.0")
    /// </summary>
    public string? Kernel { get; set; }
    
    /// <summary>
    /// System architecture (e.g., "x86_64")
    /// </summary>
    public string? Architecture { get; set; }
    
    /// <summary>
    /// CPU model name
    /// </summary>
    public string? CpuModel { get; set; }
    
    /// <summary>
    /// Number of physical CPU cores
    /// </summary>
    public int? CpuCores { get; set; }
    
    /// <summary>
    /// Number of logical CPU threads
    /// </summary>
    public int? CpuThreads { get; set; }
    
    /// <summary>
    /// Secret token used for authenticating the agent over WebSocket
    /// Store this hashed in production!
    /// </summary>
    public string AgentToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates whether the agent is currently connected
    /// </summary>
    public bool IsOnline { get; set; }
    
    /// <summary>
    /// Last time we received data from the agent
    /// </summary>
    public DateTime? LastSeenUtc { get; set; }
    
    /// <summary>
    /// When this server record was created
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<MetricSample> Metrics { get; set; } = new List<MetricSample>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<ProcessSnapshot> ProcessSnapshots { get; set; } = new List<ProcessSnapshot>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
