using System.Security.Claims;
using BusinessLayer.DTOs.Auth;
using BusinessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

/// <summary>
/// Handles authentication endpoints for admin users.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="request">Login credentials (password only).</param>
    /// <returns>JWT token and user information if successful.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt");
        
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Login failed: Invalid model state");
            return BadRequest(ModelState);
        }

        var response = await _authService.LoginAsync(request);

        if (response == null)
        {
            _logger.LogWarning("Login failed: Invalid credentials");
            return Unauthorized(new { message = "Invalid password" });
        }

        _logger.LogInformation("Login successful, UserId: {UserId}", response.User.Id);
        return Ok(response);
    }

    /// <summary>
    /// Changes the admin user's password.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "Current password and new password are required" });
        }

        if (request.NewPassword.Length < 4)
        {
            return BadRequest(new { message = "New password must be at least 4 characters" });
        }

        var success = await _authService.ChangePasswordAsync(request.CurrentPassword, request.NewPassword);

        if (!success)
        {
            return BadRequest(new { message = "Current password is incorrect" });
        }

        _logger.LogInformation("Password changed successfully");
        return Ok(new { message = "Password changed successfully" });
    }

    /// <summary>
    /// Returns information about the currently authenticated user.
    /// </summary>
    /// <returns>Current user details.</returns>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("GetCurrentUser failed: Invalid token");
            return Unauthorized(new { message = "Invalid token" });
        }

        _logger.LogDebug("GetCurrentUser: UserId {UserId}, Email {Email}", userId, email);

        return Ok(new
        {
            id = int.Parse(userId),
            email,
            fullName = name,
            role,
            isActive = true
        });
    }
}

