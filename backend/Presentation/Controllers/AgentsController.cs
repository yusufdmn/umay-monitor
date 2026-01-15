using Infrastructure;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly ServerMonitoringDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(
        ServerMonitoringDbContext dbContext,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<AgentsController> logger)
    {
        _dbContext = dbContext;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Register a new agent/server
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterAgentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required" });
        }

        // Generate unique token (this is shown once)
        var plainToken = Guid.NewGuid().ToString();
        
        // Hash token for storage (like password hashing)
        var hashedToken = BCrypt.Net.BCrypt.HashPassword(plainToken);

        var server = new MonitoredServer
        {
            Name = request.Name,
            Hostname = request.Name,
            AgentToken = hashedToken, // Store hashed
            IsOnline = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.MonitoredServers.Add(server);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Registered new agent: {Name} (ID: {Id})", 
            server.Name, server.Id);

        // Build install command URL
        // Use configured AGENT_SERVER_URL if available, otherwise use request host
        var agentServerUrl = Environment.GetEnvironmentVariable("AGENT_SERVER_URL");
        string installHost;
        string installScheme;
        
        if (!string.IsNullOrEmpty(agentServerUrl))
        {
            var uri = new Uri(agentServerUrl);
            installHost = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
            // ws:// -> http://, wss:// -> https://
            installScheme = uri.Scheme == "wss" ? "https" : "http";
        }
        else
        {
            installHost = Request.Host.Value;
            installScheme = Request.Scheme;
        }
        
        var installScriptUrl = Url.Action(
            action: "GetInstallScript",
            controller: "Agents",
            values: new { agentId = server.Id, token = plainToken },
            protocol: installScheme,
            host: installHost
        );

        var installCommand = installScheme == "https"
            ? $"curl -skL {installScriptUrl} | sudo bash"
            : $"curl -sL {installScriptUrl} | sudo bash";

        return Ok(new RegisterAgentResponse
        {
            Id = server.Id,
            Name = server.Name,
            Token = plainToken, // Return plain token (ONE TIME ONLY)
            InstallCommand = installCommand,
            CreatedAtUtc = server.CreatedAtUtc
        });
    }

    /// <summary>
    /// Get installation script for agent
    /// </summary>
    [HttpGet("install/{agentId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetInstallScript(int agentId, [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("# Token is required");
        }

        var server = await _dbContext.MonitoredServers.FindAsync(agentId);

        if (server == null)
        {
            return NotFound("# Agent not found");
        }

        // Verify token (compare with hashed version)
        if (!BCrypt.Net.BCrypt.Verify(token, server.AgentToken))
        {
            return Unauthorized("# Invalid token");
        }

        // Read template from Resources/Templates
        var templatePath = Path.Combine(_environment.ContentRootPath, "Resources", "Templates", "agent-installer.sh");
        
        if (!System.IO.File.Exists(templatePath))
        {
            _logger.LogError("Install template not found at {Path}", templatePath);
            return StatusCode(500, "# Installation template not found");
        }

        var template = await System.IO.File.ReadAllTextAsync(templatePath);

        // Get the agent server URL from environment (set by docker-compose)
        // This is the URL that agents on OTHER machines will use to connect
        // Falls back to auto-detection from request if not configured
        var agentServerUrl = Environment.GetEnvironmentVariable("AGENT_SERVER_URL");
        string domainWithPort;
        
        if (!string.IsNullOrEmpty(agentServerUrl))
        {
            // Extract host:port from the URL (e.g., "ws://192.168.1.100:5123" -> "192.168.1.100:5123")
            var uri = new Uri(agentServerUrl);
            domainWithPort = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
            _logger.LogInformation("Using configured AGENT_SERVER_URL: {Url} -> {Domain}", agentServerUrl, domainWithPort);
        }
        else
        {
            // Auto-detect domain and port from the incoming request (legacy behavior)
            // This works correctly for:
            // - localhost:7286 (local development)
            // - localhost:8765 (SSH tunnel)
            // - api.yourdomain.com (production)
            domainWithPort = Request.Host.Value;
            _logger.LogInformation("AGENT_SERVER_URL not configured, using request host: {Domain}", domainWithPort);
        }

        // Replace placeholders (inject PLAIN token into script)
        var script = template
            .Replace("{{AGENT_ID}}", server.Id.ToString())
            .Replace("{{TOKEN}}", token)
            .Replace("{{DOMAIN}}", domainWithPort);

        _logger.LogInformation("Generated install script for agent {AgentId}", agentId);

        return Content(script, "text/x-shellscript");
    }

    /// <summary>
    /// Get agent status
    /// </summary>
    [HttpGet("{agentId}/status")]
    public async Task<IActionResult> GetStatus(int agentId)
    {
        var server = await _dbContext.MonitoredServers.FindAsync(agentId);

        if (server == null)
        {
            return NotFound(new { message = "Agent not found" });
        }

        return Ok(new
        {
            id = server.Id,
            name = server.Name,
            isOnline = server.IsOnline,
            lastSeenUtc = server.LastSeenUtc
        });
    }

    /// <summary>
    /// List all agents
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var servers = await _dbContext.MonitoredServers
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new
            {
                id = s.Id,
                name = s.Name,
                hostname = s.Hostname,
                isOnline = s.IsOnline,
                lastSeenUtc = s.LastSeenUtc,
                createdAtUtc = s.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(servers);
    }

    /// <summary>
    /// Delete agent
    /// </summary>
    [HttpDelete("{agentId}")]
    public async Task<IActionResult> Delete(int agentId)
    {
        var server = await _dbContext.MonitoredServers.FindAsync(agentId);

        if (server == null)
        {
            return NotFound(new { message = "Agent not found" });
        }

        _dbContext.MonitoredServers.Remove(server);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted agent: {Name} (ID: {Id})", server.Name, agentId);

        return Ok(new { message = "Agent deleted successfully" });
    }
}

// DTOs
public class RegisterAgentRequest
{
    public string Name { get; set; } = string.Empty;
}

public class RegisterAgentResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string InstallCommand { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
