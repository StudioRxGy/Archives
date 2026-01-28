using FluentAssertions;
using Xunit;
using EnterpriseAutomationFramework.Services.Browser;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// ScreenshotHelper 单元测试
/// </summary>
public class ScreenshotHelperTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GenerateFileName_WithInvalidTestName_ShouldThrowArgumentException(string testName)
    {
        // Act & Assert
        var action = () => ScreenshotHelper.GenerateFileName(testName, "Chromium");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("testName");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GenerateFileName_WithInvalidBrowserType_ShouldThrowArgumentException(string browserType)
    {
        // Act & Assert
        var action = () => ScreenshotHelper.GenerateFileName("TestName", browserType);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("browserType");
    }

    [Fact]
    public void GenerateFileName_WithValidInputs_ShouldReturnCorrectFormat()
    {
        // Arrange
        var testName = "SearchFunctionality";
        var browserType = "Chromium";
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 45, 123);

        // Act
        var fileName = ScreenshotHelper.GenerateFileName(testName, browserType, timestamp);

        // Assert
        fileName.Should().Be("SearchFunctionality_Chromium_20240115_143045_123.png");
    }

    [Fact]
    public void GenerateFileName_WithSpecialCharactersInTestName_ShouldSanitize()
    {
        // Arrange
        var testName = "Test<>Name|With*Invalid?Characters";
        var browserType = "Firefox";
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 45, 123);

        // Act
        var fileName = ScreenshotHelper.GenerateFileName(testName, browserType, timestamp);

        // Assert
        fileName.Should().Be("Test__Name_With_Invalid_Characters_Firefox_20240115_143045_123.png");
    }

    [Fact]
    public void GenerateFileName_WithSpacesInTestName_ShouldReplaceWithUnderscores()
    {
        // Arrange
        var testName = "Test Name With Spaces";
        var browserType = "Webkit";
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 45, 123);

        // Act
        var fileName = ScreenshotHelper.GenerateFileName(testName, browserType, timestamp);

        // Assert
        fileName.Should().Be("Test_Name_With_Spaces_Webkit_20240115_143045_123.png");
    }

    [Fact]
    public void GenerateFileName_WithLongTestName_ShouldTruncate()
    {
        // Arrange
        var testName = new string('A', 150); // 150个字符的测试名称
        var browserType = "Chromium";
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 45, 123);

        // Act
        var fileName = ScreenshotHelper.GenerateFileName(testName, browserType, timestamp);

        // Assert
        var expectedTestNamePart = new string('A', 100); // 应该被截断为100个字符
        fileName.Should().StartWith(expectedTestNamePart);
        fileName.Should().EndWith("_Chromium_20240115_143045_123.png");
    }

    [Fact]
    public void GenerateFileName_WithoutTimestamp_ShouldUseCurrentTime()
    {
        // Arrange
        var testName = "TestName";
        var browserType = "Chromium";

        // Act
        var fileName = ScreenshotHelper.GenerateFileName(testName, browserType);

        // Assert
        fileName.Should().StartWith("TestName_Chromium_");
        fileName.Should().EndWith(".png");
        
        // 验证时间戳格式（允许1-3位毫秒）
        var timestampPart = fileName.Substring("TestName_Chromium_".Length);
        timestampPart = timestampPart.Substring(0, timestampPart.Length - ".png".Length);
        
        // 检查格式是否正确（yyyyMMdd_HHmmss_fff，毫秒部分可能是1-3位）
        timestampPart.Should().MatchRegex(@"^\d{8}_\d{6}_\d{1,3}$");
    }

    [Fact]
    public void GenerateFilePath_WithDefaultDirectory_ShouldUseScreenshotsDirectory()
    {
        // Arrange
        var testName = "TestName";
        var browserType = "Chromium";
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 45, 123);

        // Act
        var filePath = ScreenshotHelper.GenerateFilePath(testName, browserType, null, timestamp);

        // Assert
        var expectedPath = Path.Combine("Screenshots", "TestName_Chromium_20240115_143045_123.png");
        filePath.Should().Be(expectedPath);
    }

    [Fact]
    public void GenerateFilePath_WithCustomDirectory_ShouldUseCustomDirectory()
    {
        // Arrange
        var testName = "TestName";
        var browserType = "Chromium";
        var customDir = "CustomScreenshots";
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 45, 123);

        // Act
        var filePath = ScreenshotHelper.GenerateFilePath(testName, browserType, customDir, timestamp);

        // Assert
        var expectedPath = Path.Combine("CustomScreenshots", "TestName_Chromium_20240115_143045_123.png");
        filePath.Should().Be(expectedPath);
    }

    [Fact]
    public void EnsureDirectoryExists_WithNonExistentDirectory_ShouldCreateDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "ScreenshotHelperTest_" + Guid.NewGuid());

        try
        {
            // Ensure directory doesn't exist
            Directory.Exists(tempDir).Should().BeFalse();

            // Act
            ScreenshotHelper.EnsureDirectoryExists(tempDir);

            // Assert
            Directory.Exists(tempDir).Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void EnsureDirectoryExists_WithExistingDirectory_ShouldNotThrow()
    {
        // Arrange
        var tempDir = Path.GetTempPath();

        // Act & Assert
        var action = () => ScreenshotHelper.EnsureDirectoryExists(tempDir);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void EnsureDirectoryExists_WithInvalidPath_ShouldNotThrow(string directoryPath)
    {
        // Act & Assert
        var action = () => ScreenshotHelper.EnsureDirectoryExists(directoryPath);
        action.Should().NotThrow();
    }

    [Fact]
    public void CleanupOldScreenshots_WithNegativeDaysToKeep_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => ScreenshotHelper.CleanupOldScreenshots("SomeDirectory", -1);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("daysToKeep");
    }

    [Fact]
    public void CleanupOldScreenshots_WithNonExistentDirectory_ShouldReturnZero()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid());

        // Act
        var deletedCount = ScreenshotHelper.CleanupOldScreenshots(nonExistentDir, 7);

        // Assert
        deletedCount.Should().Be(0);
    }

    [Fact]
    public void CleanupOldScreenshots_WithOldFiles_ShouldDeleteOldFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "ScreenshotCleanupTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            // 创建一些测试文件
            var oldFile = Path.Combine(tempDir, "old_file.png");
            var newFile = Path.Combine(tempDir, "new_file.png");
            var nonPngFile = Path.Combine(tempDir, "other_file.txt");

            File.WriteAllText(oldFile, "old content");
            File.WriteAllText(newFile, "new content");
            File.WriteAllText(nonPngFile, "other content");

            // 设置旧文件的创建时间为10天前
            File.SetCreationTime(oldFile, DateTime.Now.AddDays(-10));
            File.SetCreationTime(newFile, DateTime.Now.AddDays(-1));
            File.SetCreationTime(nonPngFile, DateTime.Now.AddDays(-10));

            // Act
            var deletedCount = ScreenshotHelper.CleanupOldScreenshots(tempDir, 7);

            // Assert
            deletedCount.Should().Be(1); // 只应该删除旧的PNG文件
            File.Exists(oldFile).Should().BeFalse();
            File.Exists(newFile).Should().BeTrue();
            File.Exists(nonPngFile).Should().BeTrue(); // 非PNG文件不应该被删除
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CleanupOldScreenshots_WithInvalidDirectory_ShouldReturnZero(string directoryPath)
    {
        // Act
        var deletedCount = ScreenshotHelper.CleanupOldScreenshots(directoryPath, 7);

        // Assert
        deletedCount.Should().Be(0);
    }
}