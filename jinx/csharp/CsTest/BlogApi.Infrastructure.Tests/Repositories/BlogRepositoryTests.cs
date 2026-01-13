using FluentAssertions;
using BlogApi.Domain.Entities;
using BlogApi.Domain.Common;
using BlogApi.Infrastructure.Repositories;
using BlogApi.Infrastructure.Tests.TestHelpers;

namespace BlogApi.Infrastructure.Tests.Repositories;

/// <summary>
/// BlogRepository 单元测试
/// </summary>
public class BlogRepositoryTests : IDisposable
{
    private readonly BlogApi.Infrastructure.Data.BlogDbContext _context;
    private readonly BlogRepository _repository;

    public BlogRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _repository = new BlogRepository(_context);
        TestDbContextFactory.SeedTestData(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnBlogWithAuthor()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Title.Should().Be("Test Blog 1");
        result.Author.Should().NotBeNull();
        result.Author.Username.Should().Be("testuser1");
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
    public async Task GetPagedAsync_WithBasicParameters_ShouldReturnPagedResults()
    {
        // Arrange
        var parameters = new BlogQueryParameters
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetPagedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithPublishedFilter_ShouldReturnOnlyPublishedBlogs()
    {
        // Arrange
        var parameters = new BlogQueryParameters
        {
            IsPublished = true,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetPagedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().IsPublished.Should().BeTrue();
        result.Items.First().Title.Should().Be("Test Blog 1");
    }

    [Fact]
    public async Task GetPagedAsync_WithAuthorFilter_ShouldReturnBlogsByAuthor()
    {
        // Arrange
        var parameters = new BlogQueryParameters
        {
            AuthorId = 1,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetPagedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().AuthorId.Should().Be(1);
        result.Items.First().Title.Should().Be("Test Blog 1");
    }

    [Fact]
    public async Task GetPagedAsync_WithSearchTerm_ShouldReturnMatchingBlogs()
    {
        // Arrange
        var parameters = new BlogQueryParameters
        {
            SearchTerm = "Blog 1",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetPagedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Title.Should().Contain("Blog 1");
    }

    [Fact]
    public async Task GetPagedAsync_WithTagFilter_ShouldReturnBlogsWithMatchingTags()
    {
        // Arrange
        var parameters = new BlogQueryParameters
        {
            Tags = new List<string> { "tag1" },
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetPagedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Tags.Should().Contain("tag1");
    }

    [Fact]
    public async Task GetPagedAsync_WithDateFilter_ShouldReturnBlogsInDateRange()
    {
        // Arrange
        var parameters = new BlogQueryParameters
        {
            CreatedAfter = DateTime.UtcNow.AddDays(-7), // 只包含最近7天的博客
            CreatedBefore = DateTime.UtcNow,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetPagedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Title.Should().Be("Test Blog 2");
    }

    [Fact]
    public async Task GetByAuthorIdAsync_WithValidAuthorId_ShouldReturnAuthorBlogs()
    {
        // Act
        var result = await _repository.GetByAuthorIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().AuthorId.Should().Be(1);
        result.First().Title.Should().Be("Test Blog 1");
    }

    [Fact]
    public async Task GetByAuthorIdAsync_WithIncludeUnpublished_ShouldReturnAllBlogs()
    {
        // Act
        var result = await _repository.GetByAuthorIdAsync(2, includeUnpublished: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().AuthorId.Should().Be(2);
        result.First().IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task GetByAuthorIdAsync_WithoutIncludeUnpublished_ShouldReturnOnlyPublished()
    {
        // Act
        var result = await _repository.GetByAuthorIdAsync(2, includeUnpublished: false);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithValidBlog_ShouldCreateBlogWithTimestamps()
    {
        // Arrange
        var newBlog = new Blog
        {
            Title = "New Test Blog",
            Content = "This is new test content",
            Summary = "New test summary",
            Tags = "[\"newtag\"]",
            IsPublished = true,
            AuthorId = 1
        };

        // Act
        var result = await _repository.CreateAsync(newBlog);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("New Test Blog");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // 验证数据库中确实存在该博客
        var blogInDb = await _repository.GetByIdAsync(result.Id);
        blogInDb.Should().NotBeNull();
        blogInDb!.Title.Should().Be("New Test Blog");
    }

    [Fact]
    public async Task UpdateAsync_WithValidBlog_ShouldUpdateBlogAndTimestamp()
    {
        // Arrange
        var blog = await _repository.GetByIdAsync(1);
        var originalCreatedAt = blog!.CreatedAt;
        blog.Title = "Updated Test Blog";
        blog.Content = "Updated content";

        // Act
        var result = await _repository.UpdateAsync(blog);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Test Blog");
        result.Content.Should().Be("Updated content");
        result.CreatedAt.Should().Be(originalCreatedAt); // 创建时间不应改变
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // 验证数据库中的数据已更新
        var blogInDb = await _repository.GetByIdAsync(1);
        blogInDb!.Title.Should().Be("Updated Test Blog");
        blogInDb.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteBlog()
    {
        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();

        // 验证博客已被删除
        var blogInDb = await _repository.GetByIdAsync(1);
        blogInDb.Should().BeNull();
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