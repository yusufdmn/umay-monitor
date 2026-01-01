namespace BusinessLayer.Configuration;

/// <summary>
/// Configuration for JWT authentication.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// Secret key used to sign JWT tokens (min 32 characters recommended).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (typically the backend URL or application name).
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience (typically the frontend URL or application name).
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes.
    /// </summary>
    public int ExpiryMinutes { get; set; } = 60;
}
