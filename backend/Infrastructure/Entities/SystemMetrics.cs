namespace Infrastructure.Entities;

/// <summary>
/// Represents a single snapshot of system metrics at a point in time.
/// This is a high-volume table - use long for Id.
/// </summary>
public class MetricSample
{
    public long Id { get; set; }
    
    /// <summary>
    /// When this metric snapshot was collected (UTC)
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    // CPU metrics
    public double CpuUsagePercent { get; set; }

    // RAM metrics
    public double RamUsagePercent { get; set; }
    public double RamUsedGb { get; set; }

    // System uptime
    public long UptimeSeconds { get; set; }

    // Load averages (normalized by CPU count)
    public double Load1m { get; set; }
    public double Load5m { get; set; }
    public double Load15m { get; set; }

    // Disk I/O speeds
    public double DiskReadSpeedMBps { get; set; }
    public double DiskWriteSpeedMBps { get; set; }

    // Foreign key
    public int MonitoredServerId { get; set; }
    public MonitoredServer MonitoredServer { get; set; } = null!;

    // Child collections for detailed metrics
    public ICollection<DiskPartitionMetric> DiskPartitions { get; set; } = new List<DiskPartitionMetric>();
    public ICollection<NetworkInterfaceMetric> NetworkInterfaces { get; set; } = new List<NetworkInterfaceMetric>();
}
