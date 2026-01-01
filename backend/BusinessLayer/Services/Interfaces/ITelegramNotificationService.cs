using Infrastructure.Entities;

namespace BusinessLayer.Services.Interfaces;

/// <summary>
/// Service for sending alert notifications via Telegram
/// </summary>
public interface ITelegramNotificationService
{
    /// <summary>
    /// Send an alert notification to all configured Telegram chat IDs
    /// </summary>
    Task SendAlertAsync(Alert alert);

    /// <summary>
    /// Test the Telegram bot connection
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Check if Telegram notifications are enabled and configured
    /// </summary>
    Task<bool> IsEnabledAsync();
}
