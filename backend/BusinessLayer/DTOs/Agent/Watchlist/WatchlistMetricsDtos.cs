using System.Text.Json.Serialization;

namespace BusinessLayer.DTOs.Agent.Watchlist;

/// <summary>
/// Watchlist metrics event payload
/// Event: watchlist-metrics
/// </summary>
public class WatchlistMetricsPayload
{
    /// <summary>
    /// Monitored services from the watchlist
    /// </summary>
    [JsonPropertyName("services:")]
    public List<WatchlistServiceWrapper> Services { get; set; } = new();
    
    /// <summary>
    /// Monitored processes from the watchlist
    /// </summary>
    [JsonPropertyName("processes")]
    public List<WatchlistProcessWrapper> Processes { get; set; } = new();
}

/// <summary>
/// Wrapper for service response (with status and data)
/// </summary>
public class WatchlistServiceWrapper
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public WatchlistServiceInfo? Data { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Wrapper for process response (with status and data)
/// </summary>
public class WatchlistProcessWrapper
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public WatchlistProcessInfo? Data { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Service information from watchlist
/// </summary>
public class WatchlistServiceInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("activeState")]
    public string ActiveState { get; set; } = string.Empty;
    
    [JsonPropertyName("subState")]
    public string SubState { get; set; } = string.Empty;
    
    [JsonPropertyName("cpuUsagePercent")]
    public double? CpuUsagePercent { get; set; }
    
    [JsonPropertyName("memoryUsage")]
    public double? MemoryUsage { get; set; }
    
    [JsonPropertyName("mainPID")]
    public int? MainPID { get; set; }
    
    [JsonPropertyName("startTime")]
    public string? StartTime { get; set; }
    
    [JsonPropertyName("restartPolicy")]
    public string? RestartPolicy { get; set; }
}

/// <summary>
/// Process information from watchlist
/// </summary>
public class WatchlistProcessInfo
{
    [JsonPropertyName("pid")]
    public int? Pid { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("cmdline")]
    public string? Cmdline { get; set; }
    
    [JsonPropertyName("cpuPercent")]
    public double? CpuPercent { get; set; }
    
    [JsonPropertyName("memoryMb")]
    public double? MemoryMb { get; set; }
    
    [JsonPropertyName("memoryPercent")]
    public double? MemoryPercent { get; set; }
    
    [JsonPropertyName("user")]
    public string? User { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("uptimeSeconds")]
    public long? UptimeSeconds { get; set; }
    
    [JsonPropertyName("createTime")]
    public string? CreateTime { get; set; }
    
    [JsonPropertyName("nice")]
    public int? Nice { get; set; }
    
    [JsonPropertyName("numThreads")]
    public int? NumThreads { get; set; }
    
    /// <summary>
    /// Error message if process was not found
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
