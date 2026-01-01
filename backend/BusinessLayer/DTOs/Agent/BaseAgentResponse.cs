using System.Text.Json.Serialization;

namespace BusinessLayer.DTOs.Agent;

/// <summary>
/// Base structure for responses from agents
/// All agent responses include status and optional error message
/// </summary>
public class BaseAgentResponse
{
    /// <summary>
    /// Response status: "ok" or "error"
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Error message (only present when status is "error")
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Generic response with data payload
/// </summary>
/// <typeparam name="T">Type of the data payload</typeparam>
public class AgentResponse<T> : BaseAgentResponse where T : class
{
    /// <summary>
    /// Response data (only present when status is "ok")
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
