using System.Text.Json.Serialization;

namespace BusinessLayer.DTOs.Agent;

public class AuthenticatePayload
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;
    
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
