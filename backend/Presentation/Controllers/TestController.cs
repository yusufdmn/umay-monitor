using BusinessLayer.DTOs.Response;
using BusinessLayer.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IHubContext<MonitoringHub> _hubContext;

    public TestController(IHubContext<MonitoringHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost("broadcast-test")]
    public async Task<IActionResult> BroadcastTest([FromQuery] int serverId = 1)
    {
        var testMetric = new MetricDto
        {
            Id = 999,
            MonitoredServerId = serverId,
            TimestampUtc = DateTime.UtcNow,
            CpuUsagePercent = 42.5,
            RamUsagePercent = 67.8,
            RamUsedGb = 8.2,
            UptimeSeconds = 123456,
            Load1m = 0.5,
            Load5m = 0.3,
            Load15m = 0.2,
            DiskReadSpeedMBps = 10.5,
            DiskWriteSpeedMBps = 5.2,
            DiskPartitions = new List<DiskPartitionDto>
            {
                new() { Device = "/dev/sda1", MountPoint = "/", FileSystemType = "ext4", TotalGb = 100, UsedGb = 50, UsagePercent = 50 }
            },
            NetworkInterfaces = new List<NetworkInterfaceDto>
            {
                new() { Name = "eth0", MacAddress = "00:11:22:33:44:55", Ipv4 = "192.168.1.100", UploadSpeedMbps = 10, DownloadSpeedMbps = 20 }
            }
        };

        await _hubContext.Clients.Group($"server-{serverId}").SendAsync("MetricsUpdated", testMetric);

        return Ok(new { message = "Test broadcast sent", serverId, timestamp = DateTime.UtcNow });
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "Server is running", timestamp = DateTime.UtcNow });
    }
}
