using System.Security.Claims;
using BlogApi.Domain.Entities;

namespace BlogApi.Domain.Interfaces;

/// <summary>
/// Service for handling JWT token generation and validation
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token for the specified user
    /// </summary>
    /// <param name="user">The user to generate the token for</param>
    /// <returns>The generated access token</returns>
    string GenerateAccessToken(User user);
    
    /// <summary>
    /// Generates a refresh token
    /// </summary>
    /// <returns>The generated refresh token</returns>
    string GenerateRefreshToken();
    
    /// <summary>
    /// Validates a token and returns the claims principal
    /// </summary>
    /// <param name="token">The token to validate</param>
    /// <returns>The claims principal if valid, null otherwise</returns>
    ClaimsPrincipal? ValidateToken(string token);
    
    /// <summary>
    /// Gets the user ID from a token
    /// </summary>
    /// <param name="token">The token to extract user ID from</param>
    /// <returns>The user ID if found, null otherwise</returns>
    int? GetUserIdFromToken(string token);
    
    /// <summary>
    /// Checks if a token is expired
    /// </summary>
    /// <param name="token">The token to check</param>
    /// <returns>True if expired, false otherwise</returns>
    bool IsTokenExpired(string token);
}