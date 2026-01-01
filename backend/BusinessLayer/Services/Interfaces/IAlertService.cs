using BusinessLayer.DTOs.Agent;
using Infrastructure.Entities;

namespace BusinessLayer.Services.Interfaces;

/// <summary>
/// Service for evaluating metrics against alert rules and triggering alerts
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Evaluate incoming metrics against active alert rules for a server
    /// </summary>
    /// <param name="serverId">Server ID</param>
    /// <param name="metrics">Metrics payload from agent</param>
    Task EvaluateMetricsAsync(int serverId, MetricsPayload metrics);

    /// <summary>
    /// Evaluate watchlist metrics (services and processes) against active alert rules
    /// </summary>
    /// <param name="serverId">Server ID</param>
    /// <param name="watchlistMetrics">Watchlist metrics payload from agent</param>
    Task EvaluateWatchlistMetricsAsync(int serverId, BusinessLayer.DTOs.Agent.Watchlist.WatchlistMetricsPayload watchlistMetrics);

    /// <summary>
    /// Get all active alert rules for a specific server
    /// </summary>
    Task<List<AlertRule>> GetActiveRulesForServerAsync(int serverId);

    /// <summary>
    /// Check if an alert can be triggered (respects cooldown period)
    /// </summary>
    Task<bool> CanTriggerAlertAsync(int ruleId, int serverId);
}
