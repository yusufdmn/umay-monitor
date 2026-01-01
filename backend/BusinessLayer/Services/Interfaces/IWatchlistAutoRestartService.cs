using BusinessLayer.DTOs.Agent.Watchlist;

namespace BusinessLayer.Services.Interfaces;

/// <summary>
/// Handles automatic service restart logic for watchlist items
/// </summary>
public interface IWatchlistAutoRestartService
{
    /// <summary>
    /// Process watchlist metrics and handle offline services
    /// Will attempt to restart services up to 3 times with 20s delays
    /// Sends alert if all attempts fail
    /// </summary>
    Task ProcessWatchlistMetricsAsync(int serverId, WatchlistMetricsPayload payload);
}
