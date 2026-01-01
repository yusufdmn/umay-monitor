using BusinessLayer.Services.Interfaces;
using BusinessLayer.Services.Concrete;
using BusinessLayer.Services.Infrastructure;
using Infrastructure;
using Presentation.WebSockets;
using Microsoft.EntityFrameworkCore;

namespace Presentation;

public static class ServiceRegistration
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // WebSocket services
        services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();
        services.AddSingleton<IRequestResponseManager>(sp => 
            new RequestResponseManager(sp.GetService<ILogger<RequestResponseManager>>()));
        services.AddScoped<IAgentMessageHandler, AgentMessageHandler>();
        services.AddScoped<IAgentCommandService, AgentCommandService>();
        services.AddScoped<WebSocketHandler>();

        // Authentication services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // Alert services
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();

        // Watchlist services
        services.AddScoped<IWatchlistService, WatchlistService>();
        services.AddSingleton<IServiceRestartTracker, ServiceRestartTracker>();
        services.AddScoped<IWatchlistAutoRestartService, WatchlistAutoRestartService>();

        // Backup services
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddScoped<IBackupJobService, BackupJobService>();
        
        // Register BackupSchedulerService as both IBackupSchedulerService and IHostedService
        services.AddSingleton<BackupSchedulerService>();
        services.AddSingleton<IBackupSchedulerService>(sp => sp.GetRequiredService<BackupSchedulerService>());
        services.AddHostedService(sp => sp.GetRequiredService<BackupSchedulerService>());

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ServerMonitoringDbContext>(options =>
            options.UseNpgsql(connectionString, b =>
                b.MigrationsAssembly("Infrastructure")
            )
        );

        services.AddSignalR();
    }
}
