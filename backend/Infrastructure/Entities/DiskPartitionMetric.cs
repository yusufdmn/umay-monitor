namespace Infrastructure.Entities;

/// <summary>
/// Represents metrics for a single disk partition at a specific point in time.
/// </summary>
public class DiskPartitionMetric
{
    public long Id { get; set; }

    /// <summary>
    /// Device identifier (e.g., /dev/sda1)
    /// </summary>
    public string Device { get; set; } = string.Empty;

    /// <summary>
    /// Mount point (e.g., /, /boot, /home)
    /// </summary>
    public string MountPoint { get; set; } = string.Empty;

    /// <summary>
    /// File system type (e.g., ext4, xfs, btrfs)
    /// </summary>
    public string FileSystemType { get; set; } = string.Empty;

    /// <summary>
    /// Total capacity in GB
    /// </summary>
    public double TotalGb { get; set; }

    /// <summary>
    /// Used space in GB
    /// </summary>
    public double UsedGb { get; set; }

    /// <summary>
    /// Usage percentage (0-100)
    /// </summary>
    public double UsagePercent { get; set; }

    // Foreign key
    public long MetricSampleId { get; set; }
    public MetricSample MetricSample { get; set; } = null!;
}
