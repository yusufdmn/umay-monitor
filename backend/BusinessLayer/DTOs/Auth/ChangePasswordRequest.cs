namespace BusinessLayer.DTOs.Auth;

/// <summary>
/// Request payload for changing password.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Current password for verification.
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password to set.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}
