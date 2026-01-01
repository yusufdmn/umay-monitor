using BusinessLayer.Services.Interfaces;
using Infrastructure;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/notification-settings")]
public class NotificationSettingsController : ControllerBase
{
    private readonly ServerMonitoringDbContext _dbContext;
    private readonly ITelegramNotificationService _telegramService;
    private readonly ILogger<NotificationSettingsController> _logger;

    public NotificationSettingsController(
        ServerMonitoringDbContext dbContext,
        ITelegramNotificationService telegramService,
        ILogger<NotificationSettingsController> logger)
    {
        _dbContext = dbContext;
        _telegramService = telegramService;
        _logger = logger;
    }

    /// <summary>
    /// Get current notification settings
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetSettings()
    {
        var settings = await _dbContext.NotificationSettings
            .Include(n => n.TelegramChatIds)
            .FirstOrDefaultAsync();

        if (settings == null)
        {
            return Ok(new
            {
                isTelegramEnabled = false,
                hasBotToken = false,
                chatIds = new List<object>()
            });
        }

        return Ok(new
        {
            isTelegramEnabled = settings.IsTelegramEnabled,
            hasBotToken = !string.IsNullOrEmpty(settings.TelegramBotToken),
            chatIds = settings.TelegramChatIds.Select(c => new
            {
                id = c.Id,
                chatId = c.ChatId,
                label = c.Label,
                createdAtUtc = c.CreatedAtUtc
            }).ToList(),
            updatedAtUtc = settings.UpdatedAtUtc
        });
    }

    /// <summary>
    /// Update Telegram bot token
    /// </summary>
    [HttpPut("telegram/bot-token")]
    public async Task<IActionResult> UpdateBotToken([FromBody] UpdateBotTokenDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.BotToken))
        {
            return BadRequest(new { message = "Bot token cannot be empty" });
        }

        var settings = await GetOrCreateSettings();
        settings.TelegramBotToken = dto.BotToken;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated Telegram bot token");

        return Ok(new { message = "Bot token updated successfully" });
    }

    /// <summary>
    /// Enable or disable Telegram notifications
    /// </summary>
    [HttpPut("telegram/enabled")]
    public async Task<IActionResult> UpdateTelegramEnabled([FromBody] UpdateTelegramEnabledDto dto)
    {
        var settings = await GetOrCreateSettings();

        if (dto.Enabled && string.IsNullOrEmpty(settings.TelegramBotToken))
        {
            return BadRequest(new { message = "Cannot enable Telegram notifications without a bot token" });
        }

        settings.IsTelegramEnabled = dto.Enabled;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Telegram notifications {Status}", dto.Enabled ? "enabled" : "disabled");

        return Ok(new { message = $"Telegram notifications {(dto.Enabled ? "enabled" : "disabled")}" });
    }

    /// <summary>
    /// Add a new Telegram chat ID
    /// </summary>
    [HttpPost("telegram/chat-ids")]
    public async Task<IActionResult> AddChatId([FromBody] AddChatIdDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ChatId))
        {
            return BadRequest(new { message = "Chat ID cannot be empty" });
        }

        var settings = await GetOrCreateSettings();

        // Check if chat ID already exists
        var exists = await _dbContext.TelegramChatIds
            .AnyAsync(c => c.ChatId == dto.ChatId && c.NotificationSettingsId == settings.Id);

        if (exists)
        {
            return Conflict(new { message = "Chat ID already exists" });
        }

        var chatId = new TelegramChatId
        {
            ChatId = dto.ChatId,
            Label = dto.Label,
            NotificationSettingsId = settings.Id,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.TelegramChatIds.Add(chatId);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Added Telegram chat ID {ChatId} with label '{Label}'", dto.ChatId, dto.Label);

        return Ok(new
        {
            message = "Chat ID added successfully",
            chatId = new
            {
                id = chatId.Id,
                chatId = chatId.ChatId,
                label = chatId.Label,
                createdAtUtc = chatId.CreatedAtUtc
            }
        });
    }

    /// <summary>
    /// Update chat ID label
    /// </summary>
    [HttpPut("telegram/chat-ids/{id}")]
    public async Task<IActionResult> UpdateChatId(int id, [FromBody] UpdateChatIdDto dto)
    {
        var chatId = await _dbContext.TelegramChatIds.FindAsync(id);

        if (chatId == null)
        {
            return NotFound(new { message = "Chat ID not found" });
        }

        chatId.Label = dto.Label;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated label for Telegram chat ID {ChatId} to '{Label}'", chatId.ChatId, dto.Label);

        return Ok(new { message = "Chat ID updated successfully" });
    }

    /// <summary>
    /// Delete a Telegram chat ID
    /// </summary>
    [HttpDelete("telegram/chat-ids/{id}")]
    public async Task<IActionResult> DeleteChatId(int id)
    {
        var chatId = await _dbContext.TelegramChatIds.FindAsync(id);

        if (chatId == null)
        {
            return NotFound(new { message = "Chat ID not found" });
        }

        _dbContext.TelegramChatIds.Remove(chatId);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted Telegram chat ID {ChatId}", chatId.ChatId);

        return Ok(new { message = "Chat ID deleted successfully" });
    }

    /// <summary>
    /// Test Telegram bot connection
    /// </summary>
    [HttpPost("telegram/test")]
    public async Task<IActionResult> TestTelegramConnection()
    {
        var isConnected = await _telegramService.TestConnectionAsync();

        if (isConnected)
        {
            return Ok(new { message = "Telegram bot connection successful", success = true });
        }
        else
        {
            return BadRequest(new { message = "Telegram bot connection failed. Check bot token.", success = false });
        }
    }

    private async Task<NotificationSettings> GetOrCreateSettings()
    {
        var settings = await _dbContext.NotificationSettings.FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new NotificationSettings
            {
                IsTelegramEnabled = false,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _dbContext.NotificationSettings.Add(settings);
            await _dbContext.SaveChangesAsync();
        }

        return settings;
    }
}

// DTOs
public class UpdateBotTokenDto
{
    public string BotToken { get; set; } = string.Empty;
}

public class UpdateTelegramEnabledDto
{
    public bool Enabled { get; set; }
}

public class AddChatIdDto
{
    public string ChatId { get; set; } = string.Empty;
    public string? Label { get; set; }
}

public class UpdateChatIdDto
{
    public string? Label { get; set; }
}
