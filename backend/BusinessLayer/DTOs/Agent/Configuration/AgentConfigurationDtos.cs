using System.Text.Json.Serialization;

namespace BusinessLayer.DTOs.Agent.Configuration;

/// <summary>
/// Watchlist configuration for monitoring specific services and processes
/// </summary>
public class WatchlistConfig
{
    /// <summary>
    /// List of service names to monitor (e.g., "nginx", "docker")
    /// </summary>
    [JsonPropertyName("services")]
    public List<string>? Services { get; set; }
    
    /// <summary>
    /// List of process command lines to monitor (e.g., "python app.py", "node server.js")
    /// </summary>
    [JsonPropertyName("processes")]
    public List<string>? Processes { get; set; }
}

/// <summary>
/// Request to update agent configuration
/// Action: update-agent-config
/// </summary>
public class UpdateAgentConfigRequest
{
    /// <summary>
    /// Metrics collection interval in seconds (0-3600) - Optional
    /// </summary>
    [JsonPropertyName("metricsInterval")]
    public int? MetricsInterval { get; set; }
    
    /// <summary>
    /// Watchlist of services and processes to monitor - Optional
    /// </summary>
    [JsonPropertyName("watchlist")]
    public WatchlistConfig? Watchlist { get; set; }
}

/// <summary>
/// Response for update-agent-config request
/// Returns status "ok" or "error" with message
/// </summary>
public class UpdateAgentConfigResponse : BaseAgentResponse
{
}
