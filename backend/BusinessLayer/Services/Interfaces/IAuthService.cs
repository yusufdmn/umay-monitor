using BusinessLayer.DTOs.Auth;

namespace BusinessLayer.Services.Interfaces;

/// <summary>
/// Service for user authentication.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user with password.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>Login response with JWT token if successful, null otherwise.</returns>
    Task<LoginResponse?> LoginAsync(LoginRequest request);

    /// <summary>
    /// Changes the admin user's password.
    /// </summary>
    /// <param name="currentPassword">Current password for verification.</param>
    /// <param name="newPassword">New password to set.</param>
    /// <returns>True if password was changed successfully.</returns>
    Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
}
