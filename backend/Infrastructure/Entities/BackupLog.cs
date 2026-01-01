namespace Infrastructure.Entities;

/// <summary>
/// Stores execution history of backup jobs.
/// The Id serves as the taskId for tracking backup operations.
/// </summary>
public class BackupLog
{
    /// <summary>
    /// Unique identifier for this log entry (also used as taskId in WebSocket communication)
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Link to the parent backup job
    /// </summary>
    public Guid JobId { get; set; }
    
    /// <summary>
    /// Execution status (success or error)
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Restic snapshot hash/ID
    /// </summary>
    public string? SnapshotId { get; set; }
    
    /// <summary>
    /// Count of new files added in this backup
    /// </summary>
    public int? FilesNew { get; set; }
    
    /// <summary>
    /// Bytes added to the repository
    /// </summary>
    public long? DataAdded { get; set; }
    
    /// <summary>
    /// Execution time in seconds
    /// </summary>
    public double? DurationSeconds { get; set; }
    
    /// <summary>
    /// Error message if backup failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Timestamp when the backup was executed
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation property
    public BackupJob Job { get; set; } = null!;
}
