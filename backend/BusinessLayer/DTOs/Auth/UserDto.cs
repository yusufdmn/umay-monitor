namespace BusinessLayer.DTOs.Auth;

/// <summary>
/// Represents user information returned to the frontend.
/// </summary>
public class UserDto
{
    /// <summary>
    /// User's unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name (if available).
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// User's role (e.g., "Admin", "Viewer").
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; }
}
