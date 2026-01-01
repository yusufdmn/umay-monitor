using System.Text.Json.Serialization;

namespace BusinessLayer.DTOs.Agent.SystemInfo;

/// <summary>
/// Request to get server system information
/// Action: get-server-info
/// Payload: null
/// </summary>
public class GetServerInfoRequest
{
    // No payload needed for this request
}

/// <summary>
/// Response data containing server system information
/// </summary>
public class ServerInfoData
{
    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;
    
    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("os")]
    public string Os { get; set; } = string.Empty;
    
    [JsonPropertyName("osVersion")]
    public string OsVersion { get; set; } = string.Empty;
    
    [JsonPropertyName("kernel")]
    public string Kernel { get; set; } = string.Empty;
    
    [JsonPropertyName("architecture")]
    public string Architecture { get; set; } = string.Empty;
    
    [JsonPropertyName("cpuModel")]
    public string CpuModel { get; set; } = string.Empty;
    
    /// <summary>
    /// Physical CPU cores
    /// </summary>
    [JsonPropertyName("cpuCores")]
    public int CpuCores { get; set; }
    
    /// <summary>
    /// Logical CPU threads
    /// </summary>
    [JsonPropertyName("cpuThreads")]
    public int CpuThreads { get; set; }
}

/// <summary>
/// Complete response for get-server-info request
/// </summary>
public class GetServerInfoResponse : AgentResponse<ServerInfoData>
{
}
