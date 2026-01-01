using BusinessLayer.Hubs;
using BusinessLayer.DTOs.Response;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Presentation.Controllers;

/// <summary>
/// Handles SignalR subscription management via REST endpoints.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly ILogger<MonitoringController> _logger;
    private readonly ServerMonitoringDbContext _dbContext;

    public MonitoringController(
        IHubContext<MonitoringHub> hubContext, 
        ILogger<MonitoringController> logger,
        ServerMonitoringDbContext dbContext)
    {
        _hubContext = hubContext;
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Subscribes a SignalR connection to receive updates for a specific server.
    /// Returns recent historical metrics for immediate display.
    /// </summary>
    /// <param name="serverId">The server ID to subscribe to.</param>
    /// <returns>Success message with recent metrics history.</returns>
    [HttpPost("subscribe/{serverId}")]
    public async Task<IActionResult> SubscribeToServer(int serverId)
    {
        // Get SignalR connection ID from custom header
        var connectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();

        if (string.IsNullOrEmpty(connectionId))
        {
            _logger.LogWarning("Subscribe failed for server {ServerId}: Missing connection ID", serverId);
            return BadRequest(new { message = "SignalR connection ID is required in X-SignalR-ConnectionId header" });
        }

        try
        {
            await _hubContext.Groups.AddToGroupAsync(connectionId, $"server-{serverId}");
            
            _logger.LogInformation("Subscribed: ConnectionId {ConnectionId} → Server {ServerId}", 
                connectionId.Substring(0, 8) + "...", serverId);

            // Fetch last 300 metrics with related data
            var recentMetrics = await _dbContext.MetricSamples
                .Where(m => m.MonitoredServerId == serverId)
                .OrderByDescending(m => m.TimestampUtc)
                .Take(300)
                .Include(m => m.DiskPartitions)
                .Include(m => m.NetworkInterfaces)
                .Select(m => new MetricDto
                {
                    Id = m.Id,
                    MonitoredServerId = m.MonitoredServerId,
                    TimestampUtc = m.TimestampUtc,
                    CpuUsagePercent = m.CpuUsagePercent,
                    RamUsagePercent = m.RamUsagePercent,
                    RamUsedGb = m.RamUsedGb,
                    UptimeSeconds = m.UptimeSeconds,
                    Load1m = m.Load1m,
                    Load5m = m.Load5m,
                    Load15m = m.Load15m,
                    DiskReadSpeedMBps = m.DiskReadSpeedMBps,
                    DiskWriteSpeedMBps = m.DiskWriteSpeedMBps,
                    DiskPartitions = m.DiskPartitions.Select(dp => new DiskPartitionDto
                    {
                        Device = dp.Device,
                        MountPoint = dp.MountPoint,
                        FileSystemType = dp.FileSystemType,
                        TotalGb = dp.TotalGb,
                        UsedGb = dp.UsedGb,
                        UsagePercent = dp.UsagePercent
                    }).ToList(),
                    NetworkInterfaces = m.NetworkInterfaces.Select(ni => new NetworkInterfaceDto
                    {
                        Name = ni.Name,
                        MacAddress = ni.MacAddress,
                        Ipv4 = ni.Ipv4,
                        Ipv6 = ni.Ipv6,
                        UploadSpeedMbps = ni.UploadSpeedMbps,
                        DownloadSpeedMbps = ni.DownloadSpeedMbps
                    }).ToList()
                })
                .ToListAsync();

            var response = new SubscribeResponseDto
            {
                Message = $"Subscribed to server {serverId}",
                ServerId = serverId,
                ConnectionId = connectionId,
                RecentMetrics = recentMetrics
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subscribe failed: Server {ServerId}, ConnectionId {ConnectionId}", 
                serverId, connectionId.Substring(0, 8) + "...");
            return StatusCode(500, new { message = "Failed to subscribe to server" });
        }
    }

    /// <summary>
    /// Unsubscribes a SignalR connection from receiving updates for a specific server.
    /// </summary>
    /// <param name="serverId">The server ID to unsubscribe from.</param>
    /// <returns>Success message.</returns>
    [HttpPost("unsubscribe/{serverId}")]
    public async Task<IActionResult> UnsubscribeFromServer(int serverId)
    {
        var connectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();

        if (string.IsNullOrEmpty(connectionId))
        {
            _logger.LogWarning("Unsubscribe failed for server {ServerId}: Missing connection ID", serverId);
            return BadRequest(new { message = "SignalR connection ID is required in X-SignalR-ConnectionId header" });
        }

        try
        {
            await _hubContext.Groups.RemoveFromGroupAsync(connectionId, $"server-{serverId}");
            
            _logger.LogInformation("Unsubscribed: ConnectionId {ConnectionId} ? Server {ServerId}", 
                connectionId.Substring(0, 8) + "...", serverId);

            return Ok(new { message = $"Unsubscribed from server {serverId}", serverId, connectionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unsubscribe failed: Server {ServerId}, ConnectionId {ConnectionId}", 
                serverId, connectionId.Substring(0, 8) + "...");
            return StatusCode(500, new { message = "Failed to unsubscribe from server" });
        }
    }
}
