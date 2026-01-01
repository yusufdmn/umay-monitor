using BusinessLayer.DTOs.Auth;

namespace BusinessLayer.Services.Interfaces;

/// <summary>
/// Service for user authentication.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>Login response with JWT token if successful, null otherwise.</returns>
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
