using BlogApi.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace BlogApi.Infrastructure.Tests.Services;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly LocalFileStorageService _service;
    private readonly string _testUploadPath;
    private readonly Mock<ILogger<LocalFileStorageService>> _mockLogger;

    public LocalFileStorageServiceTests()
    {
        _testUploadPath = Path.Combine(Path.GetTempPath(), "BlogApiTests", Guid.NewGuid().ToString());
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:UploadPath"] = _testUploadPath,
                ["FileStorage:MaxFileSizeBytes"] = "1048576", // 1MB
                ["FileStorage:AllowedExtensions"] = ".txt,.jpg,.png,.pdf"
            })
            .Build();

        _mockLogger = new Mock<ILogger<LocalFileStorageService>>();
        _service = new LocalFileStorageService(configuration, _mockLogger.Object);
    }

    [Fact]
    public async Task SaveFileAsync_ValidFile_SavesSuccessfully()
    {
        // Arrange
        var content = "Test file content";
        var fileName = "test.txt";
        var contentType = "text/plain";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var filePath = await _service.SaveFileAsync(stream, fileName, contentType);

        // Assert
        Assert.NotNull(filePath);
        Assert.NotEmpty(filePath);
        Assert.True(await _service.FileExistsAsync(filePath));
    }

    [Fact]
    public async Task SaveFileAsync_NullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.SaveFileAsync(null!, "test.txt", "text/plain"));
    }

    [Fact]
    public async Task SaveFileAsync_EmptyFileName_ThrowsArgumentException()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.SaveFileAsync(stream, string.Empty, "text/plain"));
    }

    [Fact]
    public async Task SaveFileAsync_DisallowedFileType_ThrowsInvalidOperationException()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.SaveFileAsync(stream, "test.exe", "application/octet-stream"));
    }

    [Fact]
    public async Task GetFileStreamAsync_ExistingFile_ReturnsStream()
    {
        // Arrange
        var content = "Test file content";
        var fileName = "test.txt";
        using var saveStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var filePath = await _service.SaveFileAsync(saveStream, fileName, "text/plain");

        // Act
        using var retrievedStream = await _service.GetFileStreamAsync(filePath);

        // Assert
        Assert.NotNull(retrievedStream);
        using var reader = new StreamReader(retrievedStream);
        var retrievedContent = await reader.ReadToEndAsync();
        Assert.Equal(content, retrievedContent);
    }

    [Fact]
    public async Task GetFileStreamAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _service.GetFileStreamAsync("non-existent-file.txt"));
    }

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_ReturnsTrue()
    {
        // Arrange
        var content = "Test file content";
        var fileName = "test.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var filePath = await _service.SaveFileAsync(stream, fileName, "text/plain");

        // Act
        var result = await _service.DeleteFileAsync(filePath);

        // Assert
        Assert.True(result);
        Assert.False(await _service.FileExistsAsync(filePath));
    }

    [Fact]
    public async Task DeleteFileAsync_NonExistentFile_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteFileAsync("non-existent-file.txt");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task FileExistsAsync_ExistingFile_ReturnsTrue()
    {
        // Arrange
        var content = "Test file content";
        var fileName = "test.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var filePath = await _service.SaveFileAsync(stream, fileName, "text/plain");

        // Act
        var exists = await _service.FileExistsAsync(filePath);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task FileExistsAsync_NonExistentFile_ReturnsFalse()
    {
        // Act
        var exists = await _service.FileExistsAsync("non-existent-file.txt");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetFileInfoAsync_ExistingFile_ReturnsFileInfo()
    {
        // Arrange
        var content = "Test file content";
        var fileName = "test.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var filePath = await _service.SaveFileAsync(stream, fileName, "text/plain");

        // Act
        var fileInfo = await _service.GetFileInfoAsync(filePath);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.Equal(filePath, fileInfo.FilePath);
        Assert.Equal(content.Length, fileInfo.Size);
        Assert.Equal("text/plain", fileInfo.ContentType);
        Assert.True(fileInfo.Exists);
    }

    [Fact]
    public void IsFileTypeAllowed_AllowedExtension_ReturnsTrue()
    {
        // Act
        var result = _service.IsFileTypeAllowed("test.txt", "text/plain");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFileTypeAllowed_DisallowedExtension_ReturnsFalse()
    {
        // Act
        var result = _service.IsFileTypeAllowed("test.exe", "application/octet-stream");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFileSizeAllowed_ValidSize_ReturnsTrue()
    {
        // Act
        var result = _service.IsFileSizeAllowed(1024); // 1KB

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFileSizeAllowed_TooLarge_ReturnsFalse()
    {
        // Act
        var result = _service.IsFileSizeAllowed(2 * 1024 * 1024); // 2MB (exceeds 1MB limit)

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GenerateUniqueFileName_ValidFileName_ReturnsUniqueFileName()
    {
        // Arrange
        var originalFileName = "test.txt";

        // Act
        var uniqueFileName1 = _service.GenerateUniqueFileName(originalFileName);
        var uniqueFileName2 = _service.GenerateUniqueFileName(originalFileName);

        // Assert
        Assert.NotNull(uniqueFileName1);
        Assert.NotNull(uniqueFileName2);
        Assert.NotEqual(uniqueFileName1, uniqueFileName2);
        Assert.EndsWith(".txt", uniqueFileName1);
        Assert.EndsWith(".txt", uniqueFileName2);
        Assert.Contains("test", uniqueFileName1);
        Assert.Contains("test", uniqueFileName2);
    }

    [Fact]
    public void GetMimeType_KnownExtension_ReturnsCorrectMimeType()
    {
        // Act
        var mimeType = _service.GetMimeType("test.txt");

        // Assert
        Assert.Equal("text/plain", mimeType);
    }

    [Fact]
    public void GetMimeType_UnknownExtension_ReturnsDefaultMimeType()
    {
        // Act
        var mimeType = _service.GetMimeType("test.unknown");

        // Assert
        Assert.Equal("application/octet-stream", mimeType);
    }

    [Theory]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1073741824, "1.0 GB")]
    [InlineData(512, "512 B")]
    public void FormatFileSize_VariousSizes_ReturnsFormattedString(long bytes, string expected)
    {
        // Act
        var result = _service.FormatFileSize(bytes);

        // Assert
        Assert.Equal(expected, result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testUploadPath))
        {
            Directory.Delete(_testUploadPath, true);
        }
    }
}