namespace BusinessLayer.DTOs.Auth;

/// <summary>
/// Request payload for user login.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's plain-text password (will be verified against hashed password).
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
