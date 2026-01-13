using EnterpriseAutomationFramework.Services.Data;
using EnterpriseAutomationFramework.Tests.TestModels;
using FluentAssertions;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// JSON 数据属性测试
/// </summary>
public class JsonDataAttributeTests : IDisposable
{
    private readonly string _testDataDirectory;
    private readonly List<string> _tempFiles;

    public JsonDataAttributeTests()
    {
        _testDataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        _tempFiles = new List<string>();
        
        // 确保测试数据目录存在
        Directory.CreateDirectory(_testDataDirectory);
    }

    public void Dispose()
    {
        // 清理临时文件
        foreach (var tempFile in _tempFiles)
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void Constructor_WithNullFilePath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new JsonDataAttribute(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("filePath");
    }

    [Fact]
    public void GetData_WithValidJsonFile_ShouldReturnTestData()
    {
        // Arrange
        var jsonContent = JsonSerializer.Serialize(new[]
        {
            new { TestName = "Test1", SearchQuery = "keyword1", ExpectedResultCount = 5, Environment = "dev", IsEnabled = true },
            new { TestName = "Test2", SearchQuery = "keyword2", ExpectedResultCount = 10, Environment = "test", IsEnabled = false }
        }, new JsonSerializerOptions { WriteIndented = true });
        
        var jsonFile = CreateTempJsonFile(jsonContent);
        var attribute = new JsonDataAttribute(jsonFile);
        
        var method = typeof(JsonDataAttributeTests).GetMethod(nameof(SampleTestMethodWithDictionary), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = attribute.GetData(method!).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().HaveCount(1);
        result[0][0].Should().BeOfType<Dictionary<string, object>>();
        
        var firstRow = (Dictionary<string, object>)result[0][0];
        firstRow["TestName"].Should().Be("Test1");
        firstRow["SearchQuery"].Should().Be("keyword1");
    }

    [Fact]
    public void GetData_WithStronglyTypedParameter_ShouldReturnStronglyTypedData()
    {
        // Arrange
        var jsonFile = Path.Combine(_testDataDirectory, "search_test_data.json");
        var attribute = new JsonDataAttribute(jsonFile);
        
        var method = typeof(JsonDataAttributeTests).GetMethod(nameof(SampleTestMethodWithStrongType), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = attribute.GetData(method!).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().HaveCount(1);
        result[0][0].Should().BeOfType<SearchTestData>();
        
        var firstRow = (SearchTestData)result[0][0];
        firstRow.TestName.Should().Be("JSON搜索功能测试1");
        firstRow.SearchQuery.Should().Be("playwright json");
        firstRow.ExpectedResultCount.Should().Be(12);
        firstRow.Environment.Should().Be("Development");
        firstRow.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void GetData_WithNullTestMethod_ShouldThrowArgumentNullException()
    {
        // Arrange
        var jsonFile = Path.Combine(_testDataDirectory, "search_test_data.json");
        var attribute = new JsonDataAttribute(jsonFile);

        // Act & Assert
        var action = () => attribute.GetData(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("testMethod");
    }

    [Fact]
    public void GetData_WithMethodWithoutParameters_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var jsonFile = Path.Combine(_testDataDirectory, "search_test_data.json");
        var attribute = new JsonDataAttribute(jsonFile);
        
        var method = typeof(JsonDataAttributeTests).GetMethod(nameof(SampleTestMethodWithoutParameters), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        var action = () => attribute.GetData(method!);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*测试方法必须至少有一个参数*");
    }

    [Fact]
    public void GetData_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDataDirectory, "non_existent.json");
        var attribute = new JsonDataAttribute(nonExistentFile);
        
        var method = typeof(JsonDataAttributeTests).GetMethod(nameof(SampleTestMethodWithDictionary), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        var action = () => attribute.GetData(method!);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"*读取JSON测试数据失败: {nonExistentFile}*");
    }

    [Fact]
    public void GetData_WithEmptyFile_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var emptyFile = CreateTempJsonFile("");
        var attribute = new JsonDataAttribute(emptyFile);
        
        var method = typeof(JsonDataAttributeTests).GetMethod(nameof(SampleTestMethodWithDictionary), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        var action = () => attribute.GetData(method!);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"*读取JSON测试数据失败: {emptyFile}*");
    }

    [Fact]
    public void GetData_WithInvalidJsonFormat_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidJsonFile = CreateTempJsonFile("{ invalid json content");
        var attribute = new JsonDataAttribute(invalidJsonFile);
        
        var method = typeof(JsonDataAttributeTests).GetMethod(nameof(SampleTestMethodWithDictionary), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        var action = () => attribute.GetData(method!);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"*JSON格式错误: {invalidJsonFile}*");
    }

    // 示例测试方法用于反射测试
    private void SampleTestMethodWithDictionary(Dictionary<string, object> data) { }
    private void SampleTestMethodWithStrongType(SearchTestData data) { }
    private void SampleTestMethodWithoutParameters() { }

    /// <summary>
    /// 创建临时JSON文件
    /// </summary>
    /// <param name="content">文件内容</param>
    /// <returns>文件路径</returns>
    private string CreateTempJsonFile(string content)
    {
        var tempFile = Path.Combine(_testDataDirectory, $"temp_{Guid.NewGuid()}.json");
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }
}