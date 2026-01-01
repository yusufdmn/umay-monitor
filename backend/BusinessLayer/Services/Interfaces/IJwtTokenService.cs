using Infrastructure.Entities;

namespace BusinessLayer.Services.Interfaces;

/// <summary>
/// Service for generating JWT tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for the given user.
    /// </summary>
    /// <param name="user">The user to generate a token for.</param>
    /// <returns>A signed JWT token string.</returns>
    string GenerateToken(User user);
}
