using BlogApi.Domain.Interfaces;
using BCrypt.Net;

namespace BlogApi.Infrastructure.Services;

/// <summary>
/// BCrypt implementation of password hashing service
/// </summary>
public class BCryptPasswordHashingService : IPasswordHashingService
{
    private const int WorkFactor = 12; // BCrypt work factor for security

    /// <summary>
    /// Hashes a plain text password using BCrypt
    /// </summary>
    /// <param name="password">The plain text password to hash</param>
    /// <returns>The hashed password</returns>
    /// <exception cref="ArgumentException">Thrown when password is null or empty</exception>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verifies a plain text password against a BCrypt hash
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hash">The BCrypt hash to verify against</param>
    /// <returns>True if the password matches the hash, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when password or hash is null or empty</exception>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("Hash cannot be null or empty", nameof(hash));
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception)
        {
            // If hash format is invalid or verification fails, return false
            return false;
        }
    }
}