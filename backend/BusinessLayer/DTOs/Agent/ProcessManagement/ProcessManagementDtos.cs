using System.Text.Json.Serialization;

namespace BusinessLayer.DTOs.Agent.ProcessManagement;

/// <summary>
/// Request to get all processes
/// Action: get-processes
/// Payload: null
/// </summary>
public class GetProcessesRequest
{
    // No payload needed
}

/// <summary>
/// Process information in the process list
/// </summary>
public class ProcessInfo
{
    [JsonPropertyName("pid")]
    public int Pid { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("cpuPercent")]
    public double CpuPercent { get; set; }
    
    [JsonPropertyName("memoryPercent")]
    public double MemoryPercent { get; set; }
}

/// <summary>
/// Response for get-processes request
/// </summary>
public class GetProcessesResponse : AgentResponse<List<ProcessInfo>>
{
}

/// <summary>
/// Request to get detailed process information
/// Action: get-process
/// </summary>
public class GetProcessRequest
{
    /// <summary>
    /// Process ID (can be string or int, agent will cast to int)
    /// </summary>
    [JsonPropertyName("pid")]
    public int Pid { get; set; }
}

/// <summary>
/// Detailed process information
/// </summary>
public class ProcessDetails
{
    [JsonPropertyName("pid")]
    public int Pid { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("cpuPercent")]
    public double CpuPercent { get; set; }
    
    [JsonPropertyName("memoryPercent")]
    public double MemoryPercent { get; set; }
    
    [JsonPropertyName("cmdline")]
    public string? Cmdline { get; set; }
    
    [JsonPropertyName("nice")]
    public int? Nice { get; set; }
    
    [JsonPropertyName("numThreads")]
    public int? NumThreads { get; set; }
    
    [JsonPropertyName("uptimeSeconds")]
    public long? UptimeSeconds { get; set; }
}

/// <summary>
/// Response for get-process request
/// </summary>
public class GetProcessResponse : AgentResponse<ProcessDetails>
{
}
