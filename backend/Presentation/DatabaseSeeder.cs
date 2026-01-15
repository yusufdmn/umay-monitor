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

        // Seed default admin user or update existing
        var existingUser = await dbContext.Users.FirstOrDefaultAsync();
        
        if (existingUser == null)
        {
            var adminUser = new User
            {
                Email = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                FullName = "Administrator",
                Role = "Admin",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();
            
            logger.LogInformation("========================================");
            logger.LogInformation("DEFAULT ADMIN USER CREATED");
            logger.LogInformation("Password: admin");
            logger.LogInformation("⚠️  CHANGE THIS PASSWORD IMMEDIATELY!");
            logger.LogInformation("========================================");
        }
        else if (existingUser.Email != "admin")
        {
            // Migrate old user format to new single-user format
            existingUser.Email = "admin";
            existingUser.FullName = "Administrator";
            existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin");
            await dbContext.SaveChangesAsync();
            
            logger.LogInformation("========================================");
            logger.LogInformation("ADMIN USER MIGRATED TO NEW FORMAT");
            logger.LogInformation("Password has been reset to: admin");
            logger.LogInformation("⚠️  CHANGE THIS PASSWORD IMMEDIATELY!");
            logger.LogInformation("========================================");
        }
    }
}
