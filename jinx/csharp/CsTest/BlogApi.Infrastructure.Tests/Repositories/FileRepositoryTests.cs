using FluentAssertions;
using BlogApi.Domain.Entities;
using BlogApi.Domain.Common;
using BlogApi.Infrastructure.Repositories;
using BlogApi.Infrastructure.Tests.TestHelpers;

namespace BlogApi.Infrastructure.Tests.Repositories;

/// <summary>
/// FileRepository 单元测试
/// </summary>
public class FileRepositoryTests : IDisposable
{
    private readonly BlogApi.Infrastructure.Data.BlogDbContext _context;
    private readonly FileRepository _repository;

    public FileRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _repository = new FileRepository(_context);
        TestDbContextFactory.SeedTestData(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnFileWithUploader()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.OriginalName.Should().Be("test1.jpg");
        result.Uploader.Should().NotBeNull();
        result.Uploader.Username.Should().Be("testuser1");
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
    public async Task GetByUserIdAsync_WithValidUserId_ShouldReturnUserFiles()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().UploadedBy.Should().Be(1);
        result.First().OriginalName.Should().Be("test1.jpg");
    }

    [Fact]
    public async Task GetByUserIdAsync_WithIncludePrivateFalse_ShouldReturnOnlyPublicFiles()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(2, includePrivate: false);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // user2的文件是私有的
    }

    [Fact]
    public async Task GetByUserIdAsync_WithIncludePrivateTrue_ShouldReturnAllFiles()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(2, includePrivate: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().IsPublic.Should().BeFalse();
    }

    [Fact]
    public async Task GetPublicFilesAsync_ShouldReturnOnlyPublicFiles()
    {
        // Act
        var result = await _repository.GetPublicFilesAsync(page: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().IsPublic.Should().BeTrue();
        result.Items.First().OriginalName.Should().Be("test1.jpg");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByContentTypeAsync_WithValidContentType_ShouldReturnMatchingFiles()
    {
        // Act
        var result = await _repository.GetByContentTypeAsync("image/jpeg");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().ContentType.Should().Be("image/jpeg");
        result.First().OriginalName.Should().Be("test1.jpg");
    }

    [Fact]
    public async Task GetByContentTypeAsync_WithUserIdFilter_ShouldReturnUserSpecificFiles()
    {
        // Act
        var result = await _repository.GetByContentTypeAsync("application/pdf", userId: 2);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().ContentType.Should().Be("application/pdf");
        result.First().UploadedBy.Should().Be(2);
    }

    [Fact]
    public async Task GetByContentTypeAsync_WithNonExistentContentType_ShouldReturnEmpty()
    {
        // Act
        var result = await _repository.GetByContentTypeAsync("video/mp4");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPagedAsync_WithBasicParameters_ShouldReturnPagedResults()
    {
        // Arrange
        var parameters = new FileQueryParameters
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
    }

    [Fact]
    public async Task GetPagedAsync_WithUploaderFilter_ShouldReturnFilesByUploader()
    {
        // Arrange
        var parameters = new FileQueryParameters
        {
            UploadedBy = 1,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetPagedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().UploadedBy.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithContentTypeFilter_ShouldReturnMatchingFiles()
    {
        // Arrange
        var parameters = new FileQueryParameters
        {
            ContentType = "image",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetPagedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().ContentType.Should().Contain("image");
    }

    [Fact]
    public async Task GetPagedAsync_WithPublicFilter_ShouldReturnPublicFiles()
    {
        // Arrange
        var parameters = new FileQueryParameters
        {
            IsPublic = true,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetPagedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().IsPublic.Should().BeTrue();
    }

    [Fact]
    public async Task GetPagedAsync_WithSearchTerm_ShouldReturnMatchingFiles()
    {
        // Arrange
        var parameters = new FileQueryParameters
        {
            SearchTerm = "test1",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetPagedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().OriginalName.Should().Contain("test1");
    }

    [Fact]
    public async Task GetPagedAsync_WithDateFilter_ShouldReturnFilesInDateRange()
    {
        // Arrange
        var parameters = new FileQueryParameters
        {
            UploadedAfter = DateTime.UtcNow.AddDays(-5),
            UploadedBefore = DateTime.UtcNow,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetPagedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().OriginalName.Should().Be("test2.pdf");
    }

    [Fact]
    public async Task CreateAsync_WithValidFile_ShouldCreateFileWithTimestamp()
    {
        // Arrange
        var newFile = new FileEntity
        {
            OriginalName = "newfile.txt",
            StoredName = "stored_newfile.txt",
            ContentType = "text/plain",
            Size = 512,
            FilePath = "/uploads/stored_newfile.txt",
            UploadedBy = 1,
            IsPublic = true
        };

        // Act
        var result = await _repository.CreateAsync(newFile);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.OriginalName.Should().Be("newfile.txt");
        result.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // 验证数据库中确实存在该文件
        var fileInDb = await _repository.GetByIdAsync(result.Id);
        fileInDb.Should().NotBeNull();
        fileInDb!.OriginalName.Should().Be("newfile.txt");
    }

    [Fact]
    public async Task UpdateAsync_WithValidFile_ShouldUpdateFile()
    {
        // Arrange
        var file = await _repository.GetByIdAsync(1);
        file!.OriginalName = "updated_test1.jpg";
        file.IsPublic = false;

        // Act
        var result = await _repository.UpdateAsync(file);

        // Assert
        result.Should().NotBeNull();
        result.OriginalName.Should().Be("updated_test1.jpg");
        result.IsPublic.Should().BeFalse();

        // 验证数据库中的数据已更新
        var fileInDb = await _repository.GetByIdAsync(1);
        fileInDb!.OriginalName.Should().Be("updated_test1.jpg");
        fileInDb.IsPublic.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteFile()
    {
        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();

        // 验证文件已被删除
        var fileInDb = await _repository.GetByIdAsync(1);
        fileInDb.Should().BeNull();
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