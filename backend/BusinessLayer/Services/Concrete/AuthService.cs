using BusinessLayer.DTOs.Auth;
using BusinessLayer.Services.Interfaces;
using BusinessLayer.Configuration;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Services.Concrete;

/// <summary>
/// Service for user authentication.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ServerMonitoringDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ServerMonitoringDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// Since we only support a single admin user, email is optional.
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login attempt with empty password");
            return null;
        }

        // Find the admin user (we only have one user)
        var user = await _dbContext.Users.FirstOrDefaultAsync();

        if (user == null)
        {
            _logger.LogWarning("No admin user found in database");
            return null;
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user");
            return null;
        }

        // Verify password
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        
        if (!isPasswordValid)
        {
            _logger.LogWarning("Invalid password attempt");
            return null;
        }

        // Update last login timestamp
        user.LastLoginUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        // Generate JWT token
        var token = _jwtTokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        _logger.LogInformation("User logged in successfully: {Email} (UserId: {UserId})", user.Email, user.Id);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive
            }
        };
    }

    /// <summary>
    /// Changes the admin user's password.
    /// </summary>
    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            _logger.LogWarning("Change password attempt with empty password");
            return false;
        }

        if (newPassword.Length < 4)
        {
            _logger.LogWarning("New password too short");
            return false;
        }

        // Find the admin user
        var user = await _dbContext.Users.FirstOrDefaultAsync();
        if (user == null)
        {
            _logger.LogWarning("No admin user found");
            return false;
        }

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Change password failed: incorrect current password");
            return false;
        }

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Password changed successfully");
        return true;
    }
}
