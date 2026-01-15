using BlogApi.Infrastructure.Services;
using Xunit;

namespace BlogApi.Infrastructure.Tests.Services;

public class BCryptPasswordHashingServiceTests
{
    private readonly BCryptPasswordHashingService _service;

    public BCryptPasswordHashingServiceTests()
    {
        _service = new BCryptPasswordHashingService();
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashedPassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hashedPassword = _service.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.NotEmpty(hashedPassword);
        Assert.NotEqual(password, hashedPassword);
        Assert.True(hashedPassword.StartsWith("$2a$") || hashedPassword.StartsWith("$2b$"));
    }

    [Fact]
    public void HashPassword_NullPassword_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.HashPassword(null!));
    }

    [Fact]
    public void HashPassword_EmptyPassword_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.HashPassword(string.Empty));
    }

    [Fact]
    public void VerifyPassword_ValidPasswordAndHash_ReturnsTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(password, hashedPassword);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_InvalidPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hashedPassword = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_NullPassword_ThrowsArgumentException()
    {
        // Arrange
        var hashedPassword = _service.HashPassword("TestPassword123!");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.VerifyPassword(null!, hashedPassword));
    }

    [Fact]
    public void VerifyPassword_NullHash_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.VerifyPassword("TestPassword123!", null!));
    }

    [Fact]
    public void VerifyPassword_InvalidHash_ReturnsFalse()
    {
        // Act
        var result = _service.VerifyPassword("TestPassword123!", "invalid-hash");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_GeneratesDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _service.HashPassword(password);
        var hash2 = _service.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2);
        Assert.True(_service.VerifyPassword(password, hash1));
        Assert.True(_service.VerifyPassword(password, hash2));
    }
}