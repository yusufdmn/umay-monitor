using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure;

/// <summary>
/// Factory for creating DbContext instances at design time (for migrations).
/// This is needed because EF Core migrations tools can't resolve dependencies from DI.
/// </summary>
public class ServerMonitoringDbContextFactory : IDesignTimeDbContextFactory<ServerMonitoringDbContext>
{
    public ServerMonitoringDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ServerMonitoringDbContext>();
        
        // Build configuration to read from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Presentation"))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        optionsBuilder.UseNpgsql(connectionString);

        return new ServerMonitoringDbContext(optionsBuilder.Options);
    }
}
