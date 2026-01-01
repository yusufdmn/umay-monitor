namespace BusinessLayer.DTOs.Response;

public class MetricDto
{
    public long Id { get; set; }
    public int MonitoredServerId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public double CpuUsagePercent { get; set; }
    public double RamUsagePercent { get; set; }
    public double RamUsedGb { get; set; }
    public long UptimeSeconds { get; set; }
    public double Load1m { get; set; }
    public double Load5m { get; set; }
    public double Load15m { get; set; }
    public double DiskReadSpeedMBps { get; set; }
    public double DiskWriteSpeedMBps { get; set; }
    public List<DiskPartitionDto> DiskPartitions { get; set; } = new();
    public List<NetworkInterfaceDto> NetworkInterfaces { get; set; } = new();
}

public class DiskPartitionDto
{
    public string Device { get; set; } = string.Empty;
    public string MountPoint { get; set; } = string.Empty;
    public string FileSystemType { get; set; } = string.Empty;
    public double TotalGb { get; set; }
    public double UsedGb { get; set; }
    public double UsagePercent { get; set; }
}

public class NetworkInterfaceDto
{
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string? Ipv4 { get; set; }
    public string? Ipv6 { get; set; }
    public double UploadSpeedMbps { get; set; }
    public double DownloadSpeedMbps { get; set; }
}
