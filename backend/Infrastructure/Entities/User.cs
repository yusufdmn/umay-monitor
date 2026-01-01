namespace Infrastructure.Entities;

/// <summary>
/// Represents a user who can access the monitoring system.
/// </summary>
public class User
{
    public int Id { get; set; }

    /// <summary>
    /// User's email address (used for login)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password (never store plain text!)
    /// Use BCrypt or similar for hashing
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User's full name
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// User role (e.g., "Admin", "Viewer")
    /// </summary>
    public string Role { get; set; } = "Viewer";

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this user account was created
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginUtc { get; set; }

    // Navigation property
    public ICollection<Alert> AcknowledgedAlerts { get; set; } = new List<Alert>();
}
