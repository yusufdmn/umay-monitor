using System.Collections.Concurrent;
using BusinessLayer.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services.Concrete;

/// <summary>
/// In-memory tracking of service restart attempts with cooldown periods
/// </summary>
public class ServiceRestartTracker : IServiceRestartTracker
{
    private readonly ConcurrentDictionary<string, RestartAttemptInfo> _attempts = new();
    private readonly ILogger<ServiceRestartTracker> _logger;
    private const int MaxAttempts = 3;
    private const int CooldownSeconds = 20;

    public ServiceRestartTracker(ILogger<ServiceRestartTracker> logger)
    {
        _logger = logger;
    }

    public int RecordAttempt(int serverId, string serviceName)
    {
        var key = GetKey(serverId, serviceName);
        
        var info = _attempts.AddOrUpdate(
            key,
            _ => new RestartAttemptInfo 
            { 
                AttemptCount = 1, 
                LastAttemptUtc = DateTime.UtcNow 
            },
            (_, existing) => 
            {
                existing.AttemptCount++;
                existing.LastAttemptUtc = DateTime.UtcNow;
                return existing;
            }
        );

        _logger.LogInformation(
            "Recorded restart attempt {Attempt}/{MaxAttempts} for service {ServiceName} on server {ServerId}",
            info.AttemptCount, MaxAttempts, serviceName, serverId);

        return info.AttemptCount;
    }

    public int GetAttemptCount(int serverId, string serviceName)
    {
        var key = GetKey(serverId, serviceName);
        
        if (_attempts.TryGetValue(key, out var info))
        {
            return info.AttemptCount;
        }

        return 0;
    }

    public bool HasReachedMaxAttempts(int serverId, string serviceName)
    {
        return GetAttemptCount(serverId, serviceName) >= MaxAttempts;
    }

    public void ResetAttempts(int serverId, string serviceName)
    {
        var key = GetKey(serverId, serviceName);
        
        if (_attempts.TryRemove(key, out _))
        {
            _logger.LogInformation(
                "Reset restart attempts for service {ServiceName} on server {ServerId}",
                serviceName, serverId);
        }
    }

    public bool IsInCooldown(int serverId, string serviceName)
    {
        var key = GetKey(serverId, serviceName);
        
        if (_attempts.TryGetValue(key, out var info))
        {
            var cooldownEnd = info.CooldownUntilUtc ?? DateTime.MinValue;
            return DateTime.UtcNow < cooldownEnd;
        }

        return false;
    }

    public void SetCooldown(int serverId, string serviceName)
    {
        var key = GetKey(serverId, serviceName);
        
        if (_attempts.TryGetValue(key, out var info))
        {
            info.CooldownUntilUtc = DateTime.UtcNow.AddSeconds(CooldownSeconds);
            
            _logger.LogDebug(
                "Set {Cooldown}s cooldown for service {ServiceName} on server {ServerId}",
                CooldownSeconds, serviceName, serverId);
        }
    }

    public void MarkFailureAlertSent(int serverId, string serviceName)
    {
        var key = GetKey(serverId, serviceName);
        
        if (_attempts.TryGetValue(key, out var info))
        {
            info.FailureAlertSent = true;
            
            _logger.LogInformation(
                "Marked failure alert as sent for service {ServiceName} on server {ServerId}",
                serviceName, serverId);
        }
    }

    public bool WasFailureAlertSent(int serverId, string serviceName)
    {
        var key = GetKey(serverId, serviceName);
        
        if (_attempts.TryGetValue(key, out var info))
        {
            return info.FailureAlertSent;
        }

        return false;
    }

    public void MarkProcessOfflineAlertSent(int serverId, string processName)
    {
        var key = GetKey(serverId, $"process:{processName}");
        
        _attempts.AddOrUpdate(
            key,
            _ => new RestartAttemptInfo { FailureAlertSent = true },
            (_, existing) =>
            {
                existing.FailureAlertSent = true;
                return existing;
            }
        );
        
        _logger.LogInformation(
            "Marked process offline alert as sent for process {ProcessName} on server {ServerId}",
            processName, serverId);
    }

    public bool WasProcessOfflineAlertSent(int serverId, string processName)
    {
        var key = GetKey(serverId, $"process:{processName}");
        
        if (_attempts.TryGetValue(key, out var info))
        {
            return info.FailureAlertSent;
        }

        return false;
    }

    public void ResetProcessAlerts(int serverId, string processName)
    {
        var key = GetKey(serverId, $"process:{processName}");
        
        if (_attempts.TryRemove(key, out _))
        {
            _logger.LogInformation(
                "Reset process alerts for process {ProcessName} on server {ServerId}",
                processName, serverId);
        }
    }

    private static string GetKey(int serverId, string serviceName)
    {
        return $"{serverId}:{serviceName}";
    }

    /// <summary>
    /// Internal tracking structure
    /// </summary>
    private class RestartAttemptInfo
    {
        public int AttemptCount { get; set; }
        public DateTime LastAttemptUtc { get; set; }
        public DateTime? CooldownUntilUtc { get; set; }
        public bool FailureAlertSent { get; set; }
    }
}
