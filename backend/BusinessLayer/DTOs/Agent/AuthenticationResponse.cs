using System.Text.Json.Serialization;

namespace BusinessLayer.DTOs.Agent;

/// <summary>
/// Response sent to agent after authentication attempt
/// </summary>
public class AuthenticationResponse
{
    /// <summary>
    /// Status: "ok" or "error"
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Server ID (only present on successful authentication)
    /// </summary>
    [JsonPropertyName("serverId")]
    public int? ServerId { get; set; }
    
    /// <summary>
    /// Server name (only present on successful authentication)
    /// </summary>
    [JsonPropertyName("serverName")]
    public string? ServerName { get; set; }
}
