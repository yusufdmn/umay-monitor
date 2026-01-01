using BusinessLayer.Services.Interfaces;
using BusinessLayer.Services.Infrastructure;
using Microsoft.Extensions.Logging;
using BusinessLayer.DTOs.Agent;
using BusinessLayer.DTOs.Response;
using System.Text.Json;
using Infrastructure;
using Infrastructure.Entities;
using Microsoft.AspNetCore.SignalR;
using BusinessLayer.Hubs;

namespace BusinessLayer.Services.Concrete;

public class AgentMessageHandler : IAgentMessageHandler
{
    private readonly ILogger<AgentMessageHandler> _logger;
    private readonly ServerMonitoringDbContext _dbContext;
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly IRequestResponseManager _requestResponseManager;
    private readonly IAlertService _alertService;
    private readonly IWatchlistAutoRestartService _watchlistAutoRestartService;

    public AgentMessageHandler(
        ILogger<AgentMessageHandler> logger,
        ServerMonitoringDbContext dbContext,
        IHubContext<MonitoringHub> hubContext,
        IRequestResponseManager requestResponseManager,
        IAlertService alertService,
        IWatchlistAutoRestartService watchlistAutoRestartService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _hubContext = hubContext;
        _requestResponseManager = requestResponseManager;
        _alertService = alertService;
        _watchlistAutoRestartService = watchlistAutoRestartService;
        
        // Subscribe to request failure events
        _requestResponseManager.OnRequestFailed += HandleRequestFailed;
    }

    public async Task HandleMessageAsync(string message, int serverId)
    {
        try
        {
            // 🆕 Log raw incoming message
            _logger.LogInformation("=== RAW MESSAGE FROM AGENT ===");
            _logger.LogInformation("Server ID: {ServerId}", serverId);
            _logger.LogInformation("Message Length: {Length} bytes", message.Length);
            _logger.LogInformation("Full Message: {Message}", message);
            _logger.LogInformation("================================");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var baseMessage = JsonSerializer.Deserialize<BaseAgentMessage>(message, options);

            if (baseMessage == null)
            {
                _logger.LogWarning("Failed to deserialize message from server {ServerId}", serverId);
                return;
            }

            // Log message type and action for all messages
            _logger.LogInformation("Received {Type} message - Action: '{Action}', ID: {Id} from server {ServerId}", 
                baseMessage.Type, baseMessage.Action, baseMessage.Id, serverId);

            // Route based on message type
            switch (baseMessage.Type)
            {
                case MessageTypes.Event:
                    await HandleEvent(serverId, baseMessage, options);
                    break;

                case MessageTypes.Response:
                    await HandleResponse(serverId, baseMessage, options);
                    break;

                case MessageTypes.Request:
                    _logger.LogInformation("Received request from agent (unexpected): {Action}", baseMessage.Action);
                    break;

                default:
                    _logger.LogWarning("Unknown message type: {Type} from server {ServerId}", baseMessage.Type, serverId);
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse WebSocket message from server {ServerId}. Message: {Message}", 
                serverId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message from server {ServerId}", serverId);
        }
    }

    private async Task HandleEvent(int serverId, BaseAgentMessage baseMessage, JsonSerializerOptions options)
    {
        switch (baseMessage.Action)
        {
            case AgentActions.Metrics:
                var metrics = baseMessage.Payload.Deserialize<MetricsPayload>(options);
                if (metrics != null)
                {
                    await ProcessMetrics(serverId, metrics);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize metrics payload for server {ServerId}", serverId);
                }
                break;

            case AgentActions.WatchlistMetrics:
                var watchlistMetrics = baseMessage.Payload.Deserialize<BusinessLayer.DTOs.Agent.Watchlist.WatchlistMetricsPayload>(options);
                if (watchlistMetrics != null)
                {
                    await ProcessWatchlistMetrics(serverId, watchlistMetrics);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize watchlist metrics payload for server {ServerId}", serverId);
                }
                break;

            case AgentActions.BackupCompleted:
                var backupEvent = baseMessage.Payload.Deserialize<BusinessLayer.DTOs.Agent.Backup.BackupCompletedEvent>(options);
                if (backupEvent != null)
                {
                    await ProcessBackupCompleted(serverId, backupEvent);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize backup-completed payload for server {ServerId}", serverId);
                }
                break;

            default:
                _logger.LogWarning("Unknown event action: {Action} from server {ServerId}", baseMessage.Action, serverId);
                break;
        }
    }

    private async Task HandleResponse(int serverId, BaseAgentMessage baseMessage, JsonSerializerOptions options)
    {        
        _logger.LogInformation("=== RESPONSE RECEIVED ===");
        _logger.LogInformation("Server ID: {ServerId}", serverId);
        _logger.LogInformation("Message ID: {MessageId}", baseMessage.Id);
        _logger.LogInformation("Action: {Action}", baseMessage.Action);
        _logger.LogInformation("Full message: {Message}", System.Text.Json.JsonSerializer.Serialize(baseMessage));
                
        // Serialize the full message for the waiting handler
        var responseJson = JsonSerializer.Serialize(baseMessage, options);
                
        // Complete the pending request
        var completed = _requestResponseManager.CompleteRequest(baseMessage.Id, responseJson);
                
        if (!completed)
        {
            _logger.LogWarning("Received response for unknown request ID {Id} from server {ServerId}", 
                baseMessage.Id, serverId);
            _logger.LogWarning("This might mean the request already timed out or ID mismatch");
        }
        else
        {
            _logger.LogInformation("Successfully matched response to pending request ID {Id}", baseMessage.Id);
                        
            // Broadcast success event via SignalR
            try
            {
                await _hubContext.Clients.Group($"server-{serverId}")
                    .SendAsync("CommandSuccess", new
                    {
                        ServerId = serverId,
                        Action = baseMessage.Action,
                        MessageId = baseMessage.Id,
                        Message = $"Command '{baseMessage.Action}' executed successfully",
                        Timestamp = DateTime.UtcNow
                    });
                                
                _logger.LogDebug("Broadcast CommandSuccess for action '{Action}' on server {ServerId}", 
                    baseMessage.Action, serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast CommandSuccess event");
            }
        }
        
        _logger.LogInformation("=== RESPONSE HANDLING COMPLETE ===");
    }

    private async Task ProcessMetrics(int serverId, MetricsPayload payload)
    {
        var metricSample = new MetricSample
        {
            MonitoredServerId = serverId,
            TimestampUtc = DateTime.UtcNow,
            CpuUsagePercent = payload.CpuUsagePercent,
            RamUsagePercent = payload.RamUsagePercent,
            RamUsedGb = payload.RamUsedGB,
            UptimeSeconds = payload.UptimeSeconds,
            Load1m = payload.NormalizedLoad.OneMinute,
            Load5m = payload.NormalizedLoad.FiveMinute,
            Load15m = payload.NormalizedLoad.FifteenMinute,
            DiskReadSpeedMBps = payload.DiskReadSpeedMBps,
            DiskWriteSpeedMBps = payload.DiskWriteSpeedMBps
        };

        foreach (var disk in payload.DiskUsage)
        {
            metricSample.DiskPartitions.Add(new DiskPartitionMetric
            {
                Device = disk.Device,
                MountPoint = disk.Mountpoint,
                FileSystemType = disk.Fstype,
                TotalGb = disk.TotalGB,
                UsedGb = disk.UsedGB,
                UsagePercent = disk.UsagePercent
            });
        }

        foreach (var iface in payload.NetworkInterfaces)
        {
            metricSample.NetworkInterfaces.Add(new NetworkInterfaceMetric
            {
                Name = iface.Name,
                MacAddress = iface.Mac ?? string.Empty,
                Ipv4 = iface.Ipv4,
                Ipv6 = iface.Ipv6,
                UploadSpeedMbps = iface.UploadSpeedMbps,
                DownloadSpeedMbps = iface.DownloadSpeedMbps
            });
        }

        _dbContext.MetricSamples.Add(metricSample);
        await _dbContext.SaveChangesAsync();

        // 🆕 Log successful save
        _logger.LogInformation("✅ Saved metrics to DB for server {ServerId} - Metric ID: {MetricId}", 
            serverId, metricSample.Id);

        var metricDto = new MetricDto
        {
            Id = metricSample.Id,
            MonitoredServerId = serverId,
            TimestampUtc = metricSample.TimestampUtc,
            CpuUsagePercent = metricSample.CpuUsagePercent,
            RamUsagePercent = metricSample.RamUsagePercent,
            RamUsedGb = metricSample.RamUsedGb,
            UptimeSeconds = metricSample.UptimeSeconds,
            Load1m = metricSample.Load1m,
            Load5m = metricSample.Load5m,
            Load15m = metricSample.Load15m,
            DiskReadSpeedMBps = metricSample.DiskReadSpeedMBps,
            DiskWriteSpeedMBps = metricSample.DiskWriteSpeedMBps,
            DiskPartitions = metricSample.DiskPartitions.Select(d => new DiskPartitionDto
            {
                Device = d.Device,
                MountPoint = d.MountPoint,
                FileSystemType = d.FileSystemType,
                TotalGb = d.TotalGb,
                UsedGb = d.UsedGb,
                UsagePercent = d.UsagePercent
            }).ToList(),
            NetworkInterfaces = metricSample.NetworkInterfaces.Select(n => new NetworkInterfaceDto
            {
                Name = n.Name,
                MacAddress = n.MacAddress,
                Ipv4 = n.Ipv4,
                Ipv6 = n.Ipv6,
                UploadSpeedMbps = n.UploadSpeedMbps,
                DownloadSpeedMbps = n.DownloadSpeedMbps
            }).ToList()
        };

        await _hubContext.Clients.Group($"server-{serverId}").SendAsync("MetricsUpdated", metricDto);

        _logger.LogDebug("📡 Broadcast metrics via SignalR for server {ServerId}", serverId);

        // Evaluate alert rules against these metrics
        await _alertService.EvaluateMetricsAsync(serverId, payload);
    }

    private async Task ProcessWatchlistMetrics(int serverId, BusinessLayer.DTOs.Agent.Watchlist.WatchlistMetricsPayload payload)
    {
        _logger.LogInformation("Received watchlist metrics for server {ServerId}: {ServiceCount} services, {ProcessCount} processes", 
            serverId, payload.Services.Count, payload.Processes.Count);

        // Broadcast via SignalR to subscribed frontend clients
        await _hubContext.Clients.Group($"server-{serverId}").SendAsync("WatchlistMetricsUpdated", new
        {
            ServerId = serverId,
            TimestampUtc = DateTime.UtcNow,
            Services = payload.Services,
            Processes = payload.Processes
        });

        _logger.LogDebug("Broadcast watchlist metrics for server {ServerId}", serverId);

        // Process watchlist metrics for auto-restart and alerts
        await _watchlistAutoRestartService.ProcessWatchlistMetricsAsync(serverId, payload);
        
        // Evaluate alert rules for processes (existing alert system)
        await _alertService.EvaluateWatchlistMetricsAsync(serverId, payload);
    }

    /// <summary>
    /// Handle request failures (max retries exceeded)
    /// </summary>
    private async void HandleRequestFailed(PendingRequest request)
    {
        try
        {
            _logger.LogError("Broadcasting CommandFailed for action '{Action}' on server {ServerId}", 
                request.Action, request.ServerId);
            
            await _hubContext.Clients.Group($"server-{request.ServerId}")
                .SendAsync("CommandFailed", new
                {
                    ServerId = request.ServerId,
                    Action = request.Action,
                    MessageId = request.MessageId,
                    Message = $"Command '{request.Action}' failed after {request.RetryCount} retries",
                    Timestamp = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast CommandFailed event");
        }
    }

    /// <summary>
    /// Process backup-completed event from agent
    /// </summary>
    private async Task ProcessBackupCompleted(int serverId, BusinessLayer.DTOs.Agent.Backup.BackupCompletedEvent backupEvent)
    {
        _logger.LogInformation(
            "Backup completed event received for server {ServerId}, TaskId: {TaskId}, Status: {Status}",
            serverId, backupEvent.TaskId, backupEvent.Result.Status);

        // Parse taskId (which is the backup log ID)
        if (!Guid.TryParse(backupEvent.TaskId, out var taskId))
        {
            _logger.LogError("Invalid taskId format in backup-completed event: {TaskId}", backupEvent.TaskId);
            return;
        }

        // Find the backup log entry
        var log = await _dbContext.BackupLogs.FindAsync(taskId);
        if (log == null)
        {
            _logger.LogWarning("Backup log not found for taskId {TaskId}", taskId);
            return;
        }

        // Update log with results
        log.Status = backupEvent.Result.Status == "ok" ? "success" : "error";
        log.SnapshotId = backupEvent.Result.SnapshotId;
        log.FilesNew = backupEvent.Result.FilesNew;
        log.DataAdded = backupEvent.Result.DataAdded;
        log.DurationSeconds = backupEvent.Result.Duration;
        log.ErrorMessage = backupEvent.Result.ErrorMessage;

        await _dbContext.SaveChangesAsync();

        // Update job status
        var job = await _dbContext.BackupJobs.FindAsync(log.JobId);
        if (job != null)
        {
            job.LastRunStatus = log.Status;
            job.LastRunAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Updated backup log {LogId} for job {JobId} with status {Status}",
            taskId, log.JobId, log.Status);

        // Broadcast backup completion event via SignalR (optional - for real-time UI updates)
        try
        {
            await _hubContext.Clients.Group($"server-{serverId}")
                .SendAsync("BackupCompleted", new
                {
                    ServerId = serverId,
                    JobId = log.JobId,
                    TaskId = taskId,
                    Status = log.Status,
                    SnapshotId = log.SnapshotId,
                    FilesNew = log.FilesNew,
                    DataAdded = log.DataAdded,
                    DurationSeconds = log.DurationSeconds,
                    ErrorMessage = log.ErrorMessage,
                    Timestamp = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast BackupCompleted event");
        }
    }
}
