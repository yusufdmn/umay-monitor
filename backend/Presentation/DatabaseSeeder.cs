using Infrastructure;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Presentation;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServerMonitoringDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await dbContext.Database.MigrateAsync();

        // Seed test server
        if (!await dbContext.MonitoredServers.AnyAsync())
        {
            var testServer = new MonitoredServer
            {
                Name = "Ankara",
                Hostname = "Ankara-01",
                AgentToken = "test-token-123",
                IsOnline = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.MonitoredServers.Add(testServer);
            await dbContext.SaveChangesAsync();
            
            logger.LogInformation("Test server seeded successfully");
        }

        // Seed default admin user
        if (!await dbContext.Users.AnyAsync())
        {
            var adminUser = new User
            {
                Email = "admin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                FullName = "System Administrator",
                Role = "Admin",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();
            
            logger.LogInformation("========================================");
            logger.LogInformation("DEFAULT ADMIN USER CREATED:");
            logger.LogInformation("Email: admin@localhost");
            logger.LogInformation("Password: Admin123!");
            logger.LogInformation("========================================");
        }
    }
}
