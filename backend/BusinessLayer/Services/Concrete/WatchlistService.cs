using BusinessLayer.DTOs.Agent.Configuration;
using BusinessLayer.Services.Interfaces;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WatchlistServiceEntity = Infrastructure.Entities.WatchlistService;
using WatchlistProcessEntity = Infrastructure.Entities.WatchlistProcess;

namespace BusinessLayer.Services.Concrete;

/// <summary>
/// Service for managing watchlist configuration
/// </summary>
public class WatchlistService : IWatchlistService
{
    private readonly ServerMonitoringDbContext _dbContext;
    private readonly IAgentCommandService _commandService;
    private readonly ILogger<WatchlistService> _logger;

    public WatchlistService(
        ServerMonitoringDbContext dbContext,
        IAgentCommandService commandService,
        ILogger<WatchlistService> logger)
    {
        _dbContext = dbContext;
        _commandService = commandService;
        _logger = logger;
    }

    public async Task<WatchlistConfig> GetWatchlistConfigAsync(int serverId)
    {
        var services = await _dbContext.WatchlistServices
            .Where(w => w.MonitoredServerId == serverId && w.IsActive)
            .Select(w => w.ServiceName)
            .ToListAsync();

        var processes = await _dbContext.WatchlistProcesses
            .Where(w => w.MonitoredServerId == serverId && w.IsActive)
            .Select(w => w.ProcessName)
            .ToListAsync();

        return new WatchlistConfig
        {
            Services = services,
            Processes = processes
        };
    }

    public async Task AddServiceAsync(int serverId, string serviceName)
    {
        // Check if already exists
        var existing = await _dbContext.WatchlistServices
            .FirstOrDefaultAsync(w => w.MonitoredServerId == serverId && w.ServiceName == serviceName);

        if (existing != null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.AddedAtUtc = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Reactivated service {ServiceName} in watchlist for server {ServerId}", serviceName, serverId);
            }
            else
            {
                _logger.LogInformation("Service {ServiceName} already in watchlist for server {ServerId}", serviceName, serverId);
                return; // Already active, no need to update
            }
        }
        else
        {
            // Add new entry
            var watchlistService = new WatchlistServiceEntity
            {
                MonitoredServerId = serverId,
                ServiceName = serviceName,
                AddedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            _dbContext.WatchlistServices.Add(watchlistService);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Added service {ServiceName} to watchlist for server {ServerId}", serviceName, serverId);
        }

        // Update agent configuration
        await UpdateAgentConfigurationAsync(serverId);
    }

    public async Task RemoveServiceAsync(int serverId, string serviceName)
    {
        var watchlistService = await _dbContext.WatchlistServices
            .FirstOrDefaultAsync(w => w.MonitoredServerId == serverId && w.ServiceName == serviceName && w.IsActive);

        if (watchlistService == null)
        {
            _logger.LogWarning("Service {ServiceName} not found in watchlist for server {ServerId}", serviceName, serverId);
            return;
        }

        // Soft delete by setting IsActive to false
        watchlistService.IsActive = false;
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Removed service {ServiceName} from watchlist for server {ServerId}", serviceName, serverId);

        // Update agent configuration
        await UpdateAgentConfigurationAsync(serverId);
    }

    public async Task AddProcessAsync(int serverId, string processName)
    {
        // Check if already exists
        var existing = await _dbContext.WatchlistProcesses
            .FirstOrDefaultAsync(w => w.MonitoredServerId == serverId && w.ProcessName == processName);

        if (existing != null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.AddedAtUtc = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Reactivated process {ProcessName} in watchlist for server {ServerId}", processName, serverId);
            }
            else
            {
                _logger.LogInformation("Process {ProcessName} already in watchlist for server {ServerId}", processName, serverId);
                return; // Already active, no need to update
            }
        }
        else
        {
            // Add new entry
            var watchlistProcess = new WatchlistProcessEntity
            {
                MonitoredServerId = serverId,
                ProcessName = processName,
                AddedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            _dbContext.WatchlistProcesses.Add(watchlistProcess);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Added process {ProcessName} to watchlist for server {ServerId}", processName, serverId);
        }

        // Update agent configuration
        await UpdateAgentConfigurationAsync(serverId);
    }

    public async Task RemoveProcessAsync(int serverId, string processName)
    {
        var watchlistProcess = await _dbContext.WatchlistProcesses
            .FirstOrDefaultAsync(w => w.MonitoredServerId == serverId && w.ProcessName == processName && w.IsActive);

        if (watchlistProcess == null)
        {
            _logger.LogWarning("Process {ProcessName} not found in watchlist for server {ServerId}", processName, serverId);
            return;
        }

        // Soft delete by setting IsActive to false
        watchlistProcess.IsActive = false;
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Removed process {ProcessName} from watchlist for server {ServerId}", processName, serverId);

        // Update agent configuration
        await UpdateAgentConfigurationAsync(serverId);
    }

    public async Task<List<string>> GetWatchedServicesAsync(int serverId)
    {
        return await _dbContext.WatchlistServices
            .Where(w => w.MonitoredServerId == serverId && w.IsActive)
            .Select(w => w.ServiceName)
            .ToListAsync();
    }

    public async Task<List<string>> GetWatchedProcessesAsync(int serverId)
    {
        return await _dbContext.WatchlistProcesses
            .Where(w => w.MonitoredServerId == serverId && w.IsActive)
            .Select(w => w.ProcessName)
            .ToListAsync();
    }

    /// <summary>
    /// Send updated watchlist configuration to the agent
    /// </summary>
    private async Task UpdateAgentConfigurationAsync(int serverId)
    {
        var config = await GetWatchlistConfigAsync(serverId);

        var updateRequest = new UpdateAgentConfigRequest
        {
            Watchlist = config
        };

        try
        {
            var response = await _commandService.SendCommandAsync<UpdateAgentConfigRequest, UpdateAgentConfigResponse>(
                serverId,
                "update-agent-config",
                updateRequest,
                cancellationToken: CancellationToken.None
            );

            if (response.Status == "ok")
            {
                _logger.LogInformation("Successfully updated agent configuration for server {ServerId}", serverId);
            }
            else
            {
                _logger.LogWarning("Failed to update agent configuration for server {ServerId}: {Message}",
                    serverId, response.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agent configuration for server {ServerId}", serverId);
            throw;
        }
    }
}
