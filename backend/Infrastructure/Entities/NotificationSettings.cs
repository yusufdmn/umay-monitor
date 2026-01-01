namespace Infrastructure.Entities;

/// <summary>
/// Global notification settings for the system (Telegram bot configuration)
/// Singleton entity - only one record should exist
/// </summary>
public class NotificationSettings
{
    public int Id { get; set; }

    /// <summary>
    /// Telegram Bot API Token (from @BotFather)
    /// Example: "1234567890:ABCdefGhIJKlmNoPQRsTUVwxyZ"
    /// </summary>
    public string? TelegramBotToken { get; set; }

    /// <summary>
    /// Whether Telegram notifications are enabled globally
    /// </summary>
    public bool IsTelegramEnabled { get; set; } = false;

    /// <summary>
    /// When these settings were last updated
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Telegram chat IDs that will receive alert notifications
    /// </summary>
    public ICollection<TelegramChatId> TelegramChatIds { get; set; } = new List<TelegramChatId>();
}

/// <summary>
/// Represents a Telegram chat ID that can receive alert notifications
/// </summary>
public class TelegramChatId
{
    public int Id { get; set; }

    /// <summary>
    /// Telegram chat ID (can be user or group)
    /// Example: "123456789" (user) or "-1001234567890" (group)
    /// </summary>
    public string ChatId { get; set; } = string.Empty;

    /// <summary>
    /// Optional label/description for this chat ID
    /// Example: "Admin Group", "John's Personal Chat"
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// When this chat ID was added
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Foreign key to NotificationSettings
    /// </summary>
    public int NotificationSettingsId { get; set; }
    public NotificationSettings NotificationSettings { get; set; } = null!;
}
