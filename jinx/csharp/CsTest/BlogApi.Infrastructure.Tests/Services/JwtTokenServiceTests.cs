using BlogApi.Domain.Entities;
using BlogApi.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BlogApi.Infrastructure.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;
    private readonly User _testUser;

    public JwtTokenServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "this-is-a-very-long-secret-key-for-testing-purposes-that-is-at-least-32-characters",
                ["JwtSettings:Issuer"] = "BlogApi-Test",
                ["JwtSettings:Audience"] = "BlogApi-Test",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        _service = new JwtTokenService(configuration);
        
        _testUser = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsToken()
    {
        // Act
        var token = _service.GenerateAccessToken(_testUser);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT tokens contain dots
    }

    [Fact]
    public void GenerateAccessToken_NullUser_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.GenerateAccessToken(null!));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsToken()
    {
        // Act
        var token = _service.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT tokens contain dots
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var token = _service.GenerateAccessToken(_testUser);

        // Act
        var principal = _service.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.NotNull(principal.Identity);
        Assert.True(principal.Identity.IsAuthenticated);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        // Act
        var principal = _service.ValidateToken("invalid-token");

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_NullToken_ReturnsNull()
    {
        // Act
        var principal = _service.ValidateToken(null!);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetUserIdFromToken_ValidToken_ReturnsUserId()
    {
        // Arrange
        var token = _service.GenerateAccessToken(_testUser);

        // Act
        var userId = _service.GetUserIdFromToken(token);

        // Assert
        Assert.NotNull(userId);
        Assert.Equal(_testUser.Id, userId.Value);
    }

    [Fact]
    public void GetUserIdFromToken_InvalidToken_ReturnsNull()
    {
        // Act
        var userId = _service.GetUserIdFromToken("invalid-token");

        // Assert
        Assert.Null(userId);
    }

    [Fact]
    public void IsTokenExpired_ValidToken_ReturnsFalse()
    {
        // Arrange
        var token = _service.GenerateAccessToken(_testUser);

        // Act
        var isExpired = _service.IsTokenExpired(token);

        // Assert
        Assert.False(isExpired);
    }

    [Fact]
    public void IsTokenExpired_InvalidToken_ReturnsTrue()
    {
        // Act
        var isExpired = _service.IsTokenExpired("invalid-token");

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void IsTokenExpired_NullToken_ReturnsTrue()
    {
        // Act
        var isExpired = _service.IsTokenExpired(null!);

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void GenerateAccessToken_TwiceForSameUser_GeneratesDifferentTokens()
    {
        // Act
        var token1 = _service.GenerateAccessToken(_testUser);
        var token2 = _service.GenerateAccessToken(_testUser);

        // Assert
        Assert.NotEqual(token1, token2);
        
        // Both tokens should be valid
        Assert.NotNull(_service.ValidateToken(token1));
        Assert.NotNull(_service.ValidateToken(token2));
    }
}