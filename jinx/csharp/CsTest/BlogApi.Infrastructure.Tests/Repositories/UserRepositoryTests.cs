using FluentAssertions;
using BlogApi.Domain.Entities;
using BlogApi.Infrastructure.Repositories;
using BlogApi.Infrastructure.Tests.TestHelpers;

namespace BlogApi.Infrastructure.Tests.Repositories;

/// <summary>
/// UserRepository 单元测试
/// </summary>
public class UserRepositoryTests : IDisposable
{
    private readonly BlogApi.Infrastructure.Data.BlogDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _repository = new UserRepository(_context);
        TestDbContextFactory.SeedTestData(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Username.Should().Be("testuser1");
        result.Email.Should().Be("test1@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WithValidEmail_ShouldReturnUser()
    {
        // Act
        var result = await _repository.GetByEmailAsync("test1@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test1@example.com");
        result.Username.Should().Be("testuser1");
    }

    [Fact]
    public async Task GetByEmailAsync_WithCaseInsensitiveEmail_ShouldReturnUser()
    {
        // Act
        var result = await _repository.GetByEmailAsync("TEST1@EXAMPLE.COM");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test1@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_WithInvalidEmail_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_WithValidUsername_ShouldReturnUser()
    {
        // Act
        var result = await _repository.GetByUsernameAsync("testuser1");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser1");
        result.Email.Should().Be("test1@example.com");
    }

    [Fact]
    public async Task GetByUsernameAsync_WithCaseInsensitiveUsername_ShouldReturnUser()
    {
        // Act
        var result = await _repository.GetByUsernameAsync("TESTUSER1");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser1");
    }

    [Fact]
    public async Task GetByUsernameAsync_WithInvalidUsername_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByUsernameAsync("nonexistentuser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ShouldReturnTrue()
    {
        // Act
        var result = await _repository.EmailExistsAsync("test1@example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WithNonExistingEmail_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.EmailExistsAsync("nonexistent@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_WithExcludeUserId_ShouldExcludeSpecifiedUser()
    {
        // Act
        var result = await _repository.EmailExistsAsync("test1@example.com", 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UsernameExistsAsync_WithExistingUsername_ShouldReturnTrue()
    {
        // Act
        var result = await _repository.UsernameExistsAsync("testuser1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UsernameExistsAsync_WithNonExistingUsername_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.UsernameExistsAsync("nonexistentuser");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UsernameExistsAsync_WithExcludeUserId_ShouldExcludeSpecifiedUser()
    {
        // Act
        var result = await _repository.UsernameExistsAsync("testuser1", 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WithValidUser_ShouldCreateUser()
    {
        // Arrange
        var newUser = new User
        {
            Username = "newuser",
            Email = "newuser@example.com",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        var result = await _repository.CreateAsync(newUser);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Username.Should().Be("newuser");
        result.Email.Should().Be("newuser@example.com");

        // 验证数据库中确实存在该用户
        var userInDb = await _repository.GetByIdAsync(result.Id);
        userInDb.Should().NotBeNull();
        userInDb!.Username.Should().Be("newuser");
    }

    [Fact]
    public async Task UpdateAsync_WithValidUser_ShouldUpdateUser()
    {
        // Arrange
        var user = await _repository.GetByIdAsync(1);
        user!.Username = "updateduser";
        user.Email = "updated@example.com";

        // Act
        var result = await _repository.UpdateAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("updateduser");
        result.Email.Should().Be("updated@example.com");

        // 验证数据库中的数据已更新
        var userInDb = await _repository.GetByIdAsync(1);
        userInDb!.Username.Should().Be("updateduser");
        userInDb.Email.Should().Be("updated@example.com");
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteUser()
    {
        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();

        // 验证用户已被删除
        var userInDb = await _repository.GetByIdAsync(1);
        userInDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithValidId_ShouldReturnTrue()
    {
        // Act
        var result = await _repository.ExistsAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }
}