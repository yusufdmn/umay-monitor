namespace Infrastructure.Entities;

/// <summary>
/// Represents metrics for a single network interface at a specific point in time.
/// </summary>
public class NetworkInterfaceMetric
{
    public long Id { get; set; }

    /// <summary>
    /// Interface name (e.g., eth0, lo, wlan0)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// MAC address
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// IPv4 address (if assigned)
    /// </summary>
    public string? Ipv4 { get; set; }

    /// <summary>
    /// IPv6 address (if assigned)
    /// </summary>
    public string? Ipv6 { get; set; }

    /// <summary>
    /// Upload speed in Mbps
    /// </summary>
    public double UploadSpeedMbps { get; set; }

    /// <summary>
    /// Download speed in Mbps
    /// </summary>
    public double DownloadSpeedMbps { get; set; }

    // Foreign key
    public long MetricSampleId { get; set; }
    public MetricSample MetricSample { get; set; } = null!;
}
