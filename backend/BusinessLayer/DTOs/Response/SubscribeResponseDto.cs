namespace BusinessLayer.DTOs.Response;

/// <summary>
/// Response DTO for subscribe endpoint that includes historical metrics
/// </summary>
public class SubscribeResponseDto
{
    public string Message { get; set; } = string.Empty;
    public int ServerId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public List<MetricDto> RecentMetrics { get; set; } = new();
}
