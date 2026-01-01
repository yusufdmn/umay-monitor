using BusinessLayer.DTOs.Backup;

namespace BusinessLayer.Services.Interfaces;

/// <summary>
/// Background service that monitors backup job schedules and triggers them
/// </summary>
public interface IBackupSchedulerService
{
    /// <summary>
    /// Starts the scheduler background task
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Stops the scheduler background task
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Manually triggers a backup job immediately
    /// </summary>
    Task<Guid> TriggerBackupJobAsync(Guid jobId);
}
