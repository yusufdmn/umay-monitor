using System.Text.Json.Serialization;

namespace BusinessLayer.DTOs.Agent;

public class MetricsPayload
{
    public double CpuUsagePercent { get; set; }
    public double RamUsagePercent { get; set; }
    public double RamUsedGB { get; set; }
    public List<DiskUsageInfo> DiskUsage { get; set; }
    public List<NetworkInterfaceInfo> NetworkInterfaces { get; set; }
    public long UptimeSeconds { get; set; }
    public NormalizedLoadInfo NormalizedLoad { get; set; }
    public double DiskReadSpeedMBps { get; set; }
    public double DiskWriteSpeedMBps { get; set; }
}

public class DiskUsageInfo
{
    public string Device { get; set; }
    public string Mountpoint { get; set; }
    public string Fstype { get; set; }
    public double TotalGB { get; set; }
    public double UsedGB { get; set; }
    public double UsagePercent { get; set; }
}

public class NetworkInterfaceInfo
{
    public string Name { get; set; }
    public string Mac { get; set; }
    public string Ipv4 { get; set; }
    public string Ipv6 { get; set; }
    public double UploadSpeedMbps { get; set; }
    public double DownloadSpeedMbps { get; set; }
}

public class NormalizedLoadInfo
{
    // We must map the JSON property "1m" to our C# property "OneMinute"
    [JsonPropertyName("1m")]
    public double OneMinute { get; set; }

    [JsonPropertyName("5m")]
    public double FiveMinute { get; set; }

    [JsonPropertyName("15m")]
    public double FifteenMinute { get; set; }
}