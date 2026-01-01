namespace BusinessLayer.DTOs.Auth;

/// <summary>
/// Response returned after successful login.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT token to be used for authentication.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When the token expires (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Information about the authenticated user.
    /// </summary>
    public UserDto User { get; set; } = new();
}
