using BusinessLayer.DTOs.Backup;
using BusinessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

/// <summary>
/// Handles backup job management endpoints
/// </summary>
[ApiController]
[Route("api/backups")]
[Authorize]
public class BackupController : ControllerBase
{
    private readonly IBackupJobService _backupJobService;
    private readonly IBackupSchedulerService _schedulerService;
    private readonly ILogger<BackupController> _logger;

    public BackupController(
        IBackupJobService backupJobService,
        IBackupSchedulerService schedulerService,
        ILogger<BackupController> logger)
    {
        _backupJobService = backupJobService;
        _schedulerService = schedulerService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new backup job
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBackupJob([FromBody] CreateBackupJobRequest request)
    {
        _logger.LogInformation("Creating backup job {JobName} for agent {AgentId}", 
            request.Name, request.AgentId);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var job = await _backupJobService.CreateBackupJobAsync(request);
            return CreatedAtAction(nameof(GetBackupJob), new { id = job.Id }, job);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Failed to create backup job: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup job");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all backup jobs (optionally filtered by agent)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBackupJobs([FromQuery] int? agentId = null)
    {
        try
        {
            var jobs = agentId.HasValue
                ? await _backupJobService.GetBackupJobsByAgentAsync(agentId.Value)
                : await _backupJobService.GetAllBackupJobsAsync();

            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup jobs");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a specific backup job by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBackupJob(Guid id)
    {
        try
        {
            var job = await _backupJobService.GetBackupJobByIdAsync(id);
            if (job == null)
                return NotFound(new { error = "Backup job not found" });

            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup job {JobId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates an existing backup job
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBackupJob(Guid id, [FromBody] UpdateBackupJobRequest request)
    {
        _logger.LogInformation("Updating backup job {JobId}", id);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var job = await _backupJobService.UpdateBackupJobAsync(id, request);
            if (job == null)
                return NotFound(new { error = "Backup job not found" });

            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating backup job {JobId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a backup job and all its logs
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBackupJob(Guid id)
    {
        _logger.LogInformation("Deleting backup job {JobId}", id);

        try
        {
            var success = await _backupJobService.DeleteBackupJobAsync(id);
            if (!success)
                return NotFound(new { error = "Backup job not found" });

            return Ok(new { message = "Backup job deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup job {JobId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Manually triggers a backup job immediately
    /// </summary>
    [HttpPost("{id}/trigger")]
    public async Task<IActionResult> TriggerBackup(Guid id)
    {
        _logger.LogInformation("Manual trigger requested for backup job {JobId}", id);

        try
        {
            var taskId = await _schedulerService.TriggerBackupJobAsync(id);
            return Ok(new 
            { 
                message = "Backup triggered successfully",
                jobId = id,
                taskId = taskId
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to trigger backup job {JobId}: {Message}", id, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering backup job {JobId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets execution logs for a backup job
    /// </summary>
    [HttpGet("{id}/logs")]
    public async Task<IActionResult> GetBackupLogs(Guid id, [FromQuery] int limit = 50)
    {
        try
        {
            var logs = await _backupJobService.GetBackupLogsAsync(id, limit);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving logs for backup job {JobId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets snapshots from the backup repository (placeholder - requires Restic integration)
    /// </summary>
    [HttpGet("{id}/snapshots")]
    public async Task<IActionResult> GetBackupSnapshots(Guid id)
    {
        // TODO: Implement Restic snapshots command via agent
        // For now, return logs as a temporary implementation
        try
        {
            var job = await _backupJobService.GetBackupJobByIdAsync(id);
            if (job == null)
                return NotFound(new { error = "Backup job not found" });

            var logs = await _backupJobService.GetBackupLogsAsync(id, 100);
            
            var snapshots = logs
                .Where(l => l.Status == "success" && !string.IsNullOrEmpty(l.SnapshotId))
                .Select(l => new BackupSnapshotDto
                {
                    Id = l.SnapshotId!,
                    Time = l.CreatedAtUtc,
                    Hostname = job.AgentName,
                    Paths = new[] { job.SourcePath },
                    Size = l.DataAdded
                })
                .ToList();

            return Ok(snapshots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving snapshots for backup job {JobId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
