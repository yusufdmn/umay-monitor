namespace BusinessLayer.Services.Interfaces;

/// <summary>
/// Tracks service restart attempts to implement retry logic with delays
/// </summary>
public interface IServiceRestartTracker
{
    /// <summary>
    /// Record a restart attempt for a service
    /// Returns the attempt number (1, 2, or 3)
    /// </summary>
    int RecordAttempt(int serverId, string serviceName);
    
    /// <summary>
    /// Get the number of restart attempts for a service
    /// </summary>
    int GetAttemptCount(int serverId, string serviceName);
    
    /// <summary>
    /// Check if max attempts (3) have been reached
    /// </summary>
    bool HasReachedMaxAttempts(int serverId, string serviceName);
    
    /// <summary>
    /// Reset the attempt counter for a service (when it comes back online)
    /// </summary>
    void ResetAttempts(int serverId, string serviceName);
    
    /// <summary>
    /// Mark that failure alert has been sent for this service
    /// </summary>
    void MarkFailureAlertSent(int serverId, string serviceName);
    
    /// <summary>
    /// Check if failure alert was already sent (prevents spam)
    /// </summary>
    bool WasFailureAlertSent(int serverId, string serviceName);
    
    /// <summary>
    /// Check if service is currently in restart cooldown
    /// </summary>
    bool IsInCooldown(int serverId, string serviceName);
    
    /// <summary>
    /// Mark service as in cooldown (20 seconds)
    /// </summary>
    void SetCooldown(int serverId, string serviceName);
    
    /// <summary>
    /// Mark that process offline alert has been sent
    /// </summary>
    void MarkProcessOfflineAlertSent(int serverId, string processName);
    
    /// <summary>
    /// Check if process offline alert was already sent (prevents spam)
    /// </summary>
    bool WasProcessOfflineAlertSent(int serverId, string processName);
    
    /// <summary>
    /// Mark that process recovery alert has been sent
    /// </summary>
    void ResetProcessAlerts(int serverId, string processName);
}
