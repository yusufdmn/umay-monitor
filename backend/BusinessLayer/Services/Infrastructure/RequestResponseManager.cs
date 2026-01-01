using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services.Infrastructure;

/// <summary>
/// Tracks pending requests sent to agents and matches them with responses
/// CRITICAL: Each request must have a unique ID to prevent agent response caching issues
/// </summary>
public class PendingRequest
{
    public int MessageId { get; set; }
    public int ServerId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public TaskCompletionSource<string> ResponseTask { get; set; } = new();
    public CancellationTokenSource TimeoutCts { get; set; } = new();
    
    /// <summary>
    /// Number of retry attempts made (0 = first attempt)
    /// </summary>
    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Timestamp of the last retry attempt
    /// </summary>
    public DateTime? LastRetryTime { get; set; }
    
    /// <summary>
    /// Original request payload (for retries)
    /// </summary>
    public object? Payload { get; set; }
}

/// <summary>
/// Manages request/response correlation for agent commands
/// </summary>
public interface IRequestResponseManager : IDisposable
{
    /// <summary>
    /// Register a new pending request and return its unique ID
    /// </summary>
    int RegisterRequest(int serverId, string action, object? payload, TimeSpan timeout);
    
    /// <summary>
    /// Complete a pending request with the received response
    /// </summary>
    bool CompleteRequest(int messageId, string responseJson);
    
    /// <summary>
    /// Wait for a response to a specific request
    /// </summary>
    Task<string> WaitForResponseAsync(int messageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancel a pending request (timeout or error)
    /// </summary>
    void CancelRequest(int messageId, string reason);
    
    /// <summary>
    /// Get all pending requests (for diagnostics)
    /// </summary>
    IEnumerable<PendingRequest> GetPendingRequests();
    
    /// <summary>
    /// Event raised when a request needs to be retried
    /// </summary>
    event Action<PendingRequest>? OnRetryNeeded;
    
    /// <summary>
    /// Event raised when a request fails after max retries
    /// </summary>
    event Action<PendingRequest>? OnRequestFailed;
}

public class RequestResponseManager : IRequestResponseManager
{
    private readonly ConcurrentDictionary<int, PendingRequest> _pendingRequests = new();
    private int _nextMessageId = 1;
    private readonly object _idLock = new();
    private readonly ILogger<RequestResponseManager>? _logger;
    private readonly CancellationTokenSource _monitoringCts = new();
    private Task? _monitoringTask;
    
    // Retry configuration
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);
    
    public RequestResponseManager(ILogger<RequestResponseManager>? logger = null)
    {
        _logger = logger;
        StartMonitoring();
    }
    
    /// <summary>
    /// Start background task to monitor pending requests and handle retries
    /// </summary>
    private void StartMonitoring()
    {
        _monitoringTask = Task.Run(async () =>
        {
            _logger?.LogInformation("RequestResponseManager monitoring started");
            
            while (!_monitoringCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, _monitoringCts.Token); // Check every second
                    
                    var now = DateTime.UtcNow;
                    var requestsToRetry = new List<PendingRequest>();
                    var requestsToFail = new List<PendingRequest>();
                    
                    foreach (var request in _pendingRequests.Values.ToList())
                    {
                        var lastCheckTime = request.LastRetryTime ?? request.CreatedAt;
                        var elapsed = now - lastCheckTime;
                        
                        // Check if this request needs a retry
                        if (elapsed >= RetryInterval)
                        {
                            if (request.RetryCount < MaxRetries)
                            {
                                requestsToRetry.Add(request);
                            }
                            else
                            {
                                // Max retries exceeded
                                requestsToFail.Add(request);
                            }
                        }
                    }
                    
                    // Handle retries
                    foreach (var request in requestsToRetry)
                    {
                        request.RetryCount++;
                        request.LastRetryTime = now;
                        _logger?.LogWarning(
                            "Retrying request {MessageId} for action '{Action}' on server {ServerId} (attempt {RetryCount}/{MaxRetries})",
                            request.MessageId, request.Action, request.ServerId, request.RetryCount, MaxRetries
                        );
                        
                        // Note: Actual resending will be handled by ResendRequest callback
                        OnRetryNeeded?.Invoke(request);
                    }
                    
                    // Handle failures
                    foreach (var request in requestsToFail)
                    {
                        _logger?.LogError(
                            "Request {MessageId} for action '{Action}' on server {ServerId} failed after {MaxRetries} retries",
                            request.MessageId, request.Action, request.ServerId, MaxRetries
                        );
                        
                        CancelRequest(request.MessageId, $"Max retries ({MaxRetries}) exceeded");
                        
                        // Notify failure
                        OnRequestFailed?.Invoke(request);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected on shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in request monitoring loop");
                }
            }
            
            _logger?.LogInformation("RequestResponseManager monitoring stopped");
        });
    }
    
    /// <summary>
    /// Event raised when a request needs to be retried
    /// </summary>
    public event Action<PendingRequest>? OnRetryNeeded;
    
    /// <summary>
    /// Event raised when a request fails after max retries
    /// </summary>
    public event Action<PendingRequest>? OnRequestFailed;
    
    /// <summary>
    /// Stop the monitoring task (cleanup)
    /// </summary>
    public void Dispose()
    {
        _monitoringCts.Cancel();
        _monitoringTask?.Wait(TimeSpan.FromSeconds(5));
        _monitoringCts.Dispose();
    }

    public int RegisterRequest(int serverId, string action, TimeSpan timeout)
    {
        int messageId;
        lock (_idLock)
        {
            messageId = _nextMessageId++;
        }

        var request = new PendingRequest
        {
            MessageId = messageId,
            ServerId = serverId,
            Action = action,
            CreatedAt = DateTime.UtcNow
        };

        // Set up timeout cancellation
        request.TimeoutCts.CancelAfter(timeout);
        
        // CRITICAL: Use Task.Run to prevent deadlock when Cancel() is called
        request.TimeoutCts.Token.Register(() =>
        {
            Task.Run(() => CancelRequest(messageId, $"Request timeout after {timeout.TotalSeconds}s"));
        });

        _pendingRequests.TryAdd(messageId, request);
        
        return messageId;
    }

    public bool CompleteRequest(int messageId, string responseJson)
    {
        if (_pendingRequests.TryRemove(messageId, out var request))
        {
            // CRITICAL FIX: Don't call Cancel() - it can block!
            // Dispose will cancel the token source safely without blocking
            request.TimeoutCts.Dispose();
            
            request.ResponseTask.TrySetResult(responseJson);
            return true;
        }
        
        return false;
    }

    public async Task<string> WaitForResponseAsync(int messageId, CancellationToken cancellationToken = default)
    {
        if (!_pendingRequests.TryGetValue(messageId, out var request))
        {
            throw new InvalidOperationException($"Request with ID {messageId} not found");
        }

        // Combine timeout and external cancellation
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            request.TimeoutCts.Token, 
            cancellationToken
        );

        try
        {
            return await request.ResponseTask.Task.WaitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            _pendingRequests.TryRemove(messageId, out _);
            throw new TimeoutException($"Request {messageId} ({request.Action}) timed out or was cancelled");
        }
    }

    public void CancelRequest(int messageId, string reason)
    {
        if (_pendingRequests.TryRemove(messageId, out var request))
        {
            request.ResponseTask.TrySetException(new OperationCanceledException(reason));
        }
    }

    public IEnumerable<PendingRequest> GetPendingRequests()
    {
        return _pendingRequests.Values.ToList();
    }

    public int RegisterRequest(int serverId, string action, object? payload, TimeSpan timeout)
    {
        int messageId;
        lock (_idLock)
        {
            messageId = _nextMessageId++;
        }

        var request = new PendingRequest
        {
            MessageId = messageId,
            ServerId = serverId,
            Action = action,
            CreatedAt = DateTime.UtcNow,
            Payload = payload
        };

        // Set up timeout cancellation (but retries will happen first)
        request.TimeoutCts.CancelAfter(timeout);
        
        // CRITICAL: Use Task.Run to prevent deadlock when Cancel() is called
        // The callback must not block the thread calling Cancel()
        request.TimeoutCts.Token.Register(() =>
        {
            // Only cancel if max retries reached
            if (request.RetryCount >= MaxRetries)
            {
                // Execute asynchronously to prevent blocking Cancel()
                Task.Run(() => CancelRequest(messageId, $"Request timeout after {timeout.TotalSeconds}s and {MaxRetries} retries"));
            }
        });

        _pendingRequests.TryAdd(messageId, request);
        
        _logger?.LogDebug("Registered request {MessageId} for action '{Action}' on server {ServerId}", 
            messageId, action, serverId);
        
        return messageId;
    }
}
