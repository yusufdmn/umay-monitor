namespace Infrastructure.Entities;

/// <summary>
/// Represents a single process running on a server at a specific point in time.
/// </summary>
public class ProcessInfo
{
    public long Id { get; set; }

    /// <summary>
    /// Process ID
    /// </summary>
    public int Pid { get; set; }

    /// <summary>
    /// Process name/command
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// CPU usage percentage for this process
    /// </summary>
    public double CpuPercent { get; set; }

    /// <summary>
    /// RAM usage in MB
    /// </summary>
    public double RamMb { get; set; }

    /// <summary>
    /// Optional: process user/owner
    /// </summary>
    public string? User { get; set; }

    // Foreign key
    public long ProcessSnapshotId { get; set; }
    public ProcessSnapshot ProcessSnapshot { get; set; } = null!;
}
