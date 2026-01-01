using BusinessLayer.DTOs.Backup;
using BusinessLayer.Services.Interfaces;
using Infrastructure;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services.Concrete;

/// <summary>
/// Manages backup job CRUD operations with automatic encryption/decryption of credentials
/// </summary>
public class BackupJobService : IBackupJobService
{
    private readonly ServerMonitoringDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<BackupJobService> _logger;

    public BackupJobService(
        ServerMonitoringDbContext context,
        IEncryptionService encryptionService,
        ILogger<BackupJobService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<BackupJobDto> CreateBackupJobAsync(CreateBackupJobRequest request)
    {
        // Validate agent exists
        var agentExists = await _context.MonitoredServers
            .AnyAsync(s => s.Id == request.AgentId);

        if (!agentExists)
            throw new ArgumentException($"Agent with ID {request.AgentId} not found");

        // Encrypt sensitive credentials
        var encryptedPassword = _encryptionService.Encrypt(request.RepoPassword);
        var encryptedSshKey = _encryptionService.Encrypt(request.SshPrivateKey);

        var job = new BackupJob
        {
            Id = Guid.NewGuid(),
            AgentId = request.AgentId,
            Name = request.Name,
            SourcePath = request.SourcePath,
            RepoUrl = request.RepoUrl,
            RepoPasswordEncrypted = encryptedPassword,
            SshPrivateKeyEncrypted = encryptedSshKey,
            ScheduleCron = request.ScheduleCron,
            IsActive = request.IsActive,
            LastRunStatus = "pending",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _context.BackupJobs.Add(job);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created backup job {JobId} for agent {AgentId}", job.Id, job.AgentId);

        return await GetBackupJobByIdAsync(job.Id) 
               ?? throw new InvalidOperationException("Failed to retrieve created job");
    }

    public async Task<List<BackupJobDto>> GetBackupJobsByAgentAsync(int agentId)
    {
        return await _context.BackupJobs
            .Where(j => j.AgentId == agentId)
            .Include(j => j.Agent)
            .Select(j => MapToDto(j))
            .ToListAsync();
    }

    public async Task<List<BackupJobDto>> GetAllBackupJobsAsync()
    {
        return await _context.BackupJobs
            .Include(j => j.Agent)
            .Select(j => MapToDto(j))
            .ToListAsync();
    }

    public async Task<BackupJobDto?> GetBackupJobByIdAsync(Guid jobId)
    {
        var job = await _context.BackupJobs
            .Include(j => j.Agent)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        return job != null ? MapToDto(job) : null;
    }

    public async Task<BackupJobDto?> UpdateBackupJobAsync(Guid jobId, UpdateBackupJobRequest request)
    {
        var job = await _context.BackupJobs.FindAsync(jobId);
        if (job == null)
            return null;

        // Update only provided fields
        if (request.Name != null)
            job.Name = request.Name;

        if (request.SourcePath != null)
            job.SourcePath = request.SourcePath;

        if (request.RepoUrl != null)
            job.RepoUrl = request.RepoUrl;

        if (request.RepoPassword != null)
            job.RepoPasswordEncrypted = _encryptionService.Encrypt(request.RepoPassword);

        if (request.SshPrivateKey != null)
            job.SshPrivateKeyEncrypted = _encryptionService.Encrypt(request.SshPrivateKey);

        if (request.ScheduleCron != null)
            job.ScheduleCron = request.ScheduleCron;

        if (request.IsActive.HasValue)
            job.IsActive = request.IsActive.Value;

        job.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated backup job {JobId}", jobId);

        return await GetBackupJobByIdAsync(jobId);
    }

    public async Task<bool> DeleteBackupJobAsync(Guid jobId)
    {
        var job = await _context.BackupJobs.FindAsync(jobId);
        if (job == null)
            return false;

        _context.BackupJobs.Remove(job);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted backup job {JobId}", jobId);

        return true;
    }

    public async Task<(string repoPassword, string sshPrivateKey)?> GetDecryptedCredentialsAsync(Guid jobId)
    {
        var job = await _context.BackupJobs.FindAsync(jobId);
        if (job == null)
            return null;

        try
        {
            var repoPassword = _encryptionService.Decrypt(job.RepoPasswordEncrypted);
            var sshPrivateKey = _encryptionService.Decrypt(job.SshPrivateKeyEncrypted);

            return (repoPassword, sshPrivateKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt credentials for job {JobId}", jobId);
            throw new InvalidOperationException("Failed to decrypt backup credentials", ex);
        }
    }

    public async Task<List<BackupLogDto>> GetBackupLogsAsync(Guid jobId, int limit = 50)
    {
        return await _context.BackupLogs
            .Where(l => l.JobId == jobId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(limit)
            .Select(l => new BackupLogDto
            {
                Id = l.Id,
                JobId = l.JobId,
                Status = l.Status,
                SnapshotId = l.SnapshotId,
                FilesNew = l.FilesNew,
                DataAdded = l.DataAdded,
                DurationSeconds = l.DurationSeconds,
                ErrorMessage = l.ErrorMessage,
                CreatedAtUtc = l.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<BackupLogDto> CreateBackupLogAsync(Guid jobId, Guid taskId, string status,
        string? snapshotId = null, int? filesNew = null, long? dataAdded = null,
        double? durationSeconds = null, string? errorMessage = null)
    {
        var log = new BackupLog
        {
            Id = taskId,
            JobId = jobId,
            Status = status,
            SnapshotId = snapshotId,
            FilesNew = filesNew,
            DataAdded = dataAdded,
            DurationSeconds = durationSeconds,
            ErrorMessage = errorMessage,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.BackupLogs.Add(log);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created backup log {LogId} for job {JobId} with status {Status}", 
            taskId, jobId, status);

        return new BackupLogDto
        {
            Id = log.Id,
            JobId = log.JobId,
            Status = log.Status,
            SnapshotId = log.SnapshotId,
            FilesNew = log.FilesNew,
            DataAdded = log.DataAdded,
            DurationSeconds = log.DurationSeconds,
            ErrorMessage = log.ErrorMessage,
            CreatedAtUtc = log.CreatedAtUtc
        };
    }

    public async Task UpdateJobStatusAsync(Guid jobId, string status)
    {
        var job = await _context.BackupJobs.FindAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Attempted to update status for non-existent job {JobId}", jobId);
            return;
        }

        job.LastRunStatus = status;
        job.LastRunAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated job {JobId} status to {Status}", jobId, status);
    }

    private static BackupJobDto MapToDto(BackupJob job)
    {
        return new BackupJobDto
        {
            Id = job.Id,
            AgentId = job.AgentId,
            AgentName = job.Agent?.Name ?? "Unknown",
            Name = job.Name,
            SourcePath = job.SourcePath,
            RepoUrl = job.RepoUrl,
            ScheduleCron = job.ScheduleCron,
            IsActive = job.IsActive,
            LastRunStatus = job.LastRunStatus,
            LastRunAtUtc = job.LastRunAtUtc,
            CreatedAtUtc = job.CreatedAtUtc,
            UpdatedAtUtc = job.UpdatedAtUtc
        };
    }
}
