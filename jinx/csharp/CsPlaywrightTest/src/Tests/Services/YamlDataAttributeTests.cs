using EnterpriseAutomationFramework.Services.Data;
using EnterpriseAutomationFramework.Tests.TestModels;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// YAML 数据属性测试
/// </summary>
public class YamlDataAttributeTests : IDisposable
{
    private readonly string _testDataDirectory;
    private readonly List<string> _tempFiles;

    public YamlDataAttributeTests()
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
        var action = () => new YamlDataAttribute(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("filePath");
    }

    [Fact]
    public void GetData_WithValidYamlFile_ShouldReturnTestData()
    {
        // Arrange
        var yamlContent = @"
- testName: Test1
  searchQuery: keyword1
  expectedResultCount: 5
  environment: dev
  isEnabled: true
- testName: Test2
  searchQuery: keyword2
  expectedResultCount: 10
  environment: test
  isEnabled: false";
        
        var yamlFile = CreateTempYamlFile(yamlContent);
        var attribute = new YamlDataAttribute(yamlFile);
        
        var method = typeof(YamlDataAttributeTests).GetMethod(nameof(SampleTestMethodWithDictionary), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = attribute.GetData(method!).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().HaveCount(1);
        result[0][0].Should().BeOfType<Dictionary<string, object>>();
        
        var firstRow = (Dictionary<string, object>)result[0][0];
        firstRow["testName"].Should().Be("Test1");
        firstRow["searchQuery"].Should().Be("keyword1");
    }

    [Fact]
    public void GetData_WithStronglyTypedParameter_ShouldReturnStronglyTypedData()
    {
        // Arrange
        var yamlFile = Path.Combine(_testDataDirectory, "search_test_data.yaml");
        var attribute = new YamlDataAttribute(yamlFile);
        
        var method = typeof(YamlDataAttributeTests).GetMethod(nameof(SampleTestMethodWithStrongType), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = attribute.GetData(method!).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().HaveCount(1);
        result[0][0].Should().BeOfType<SearchTestData>();
        
        var firstRow = (SearchTestData)result[0][0];
        firstRow.TestName.Should().Be("YAML搜索功能测试1");
        firstRow.SearchQuery.Should().Be("playwright yaml");
        firstRow.ExpectedResultCount.Should().Be(15);
        firstRow.Environment.Should().Be("Development");
        firstRow.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void GetData_WithNullTestMethod_ShouldThrowArgumentNullException()
    {
        // Arrange
        var yamlFile = Path.Combine(_testDataDirectory, "search_test_data.yaml");
        var attribute = new YamlDataAttribute(yamlFile);

        // Act & Assert
        var action = () => attribute.GetData(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("testMethod");
    }

    [Fact]
    public void GetData_WithMethodWithoutParameters_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var yamlFile = Path.Combine(_testDataDirectory, "search_test_data.yaml");
        var attribute = new YamlDataAttribute(yamlFile);
        
        var method = typeof(YamlDataAttributeTests).GetMethod(nameof(SampleTestMethodWithoutParameters), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        var action = () => attribute.GetData(method!);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*测试方法必须至少有一个参数*");
    }

    [Fact]
    public void GetData_WithNonExistentFile_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDataDirectory, "non_existent.yaml");
        var attribute = new YamlDataAttribute(nonExistentFile);
        
        var method = typeof(YamlDataAttributeTests).GetMethod(nameof(SampleTestMethodWithDictionary), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        var action = () => attribute.GetData(method!);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"*读取YAML测试数据失败: {nonExistentFile}*");
    }

    [Fact]
    public void GetData_WithEmptyFile_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var emptyFile = CreateTempYamlFile("");
        var attribute = new YamlDataAttribute(emptyFile);
        
        var method = typeof(YamlDataAttributeTests).GetMethod(nameof(SampleTestMethodWithDictionary), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        var action = () => attribute.GetData(method!);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"*YAML文件为空: {emptyFile}*");
    }

    [Fact]
    public void GetData_WithInvalidYamlFormat_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidYamlFile = CreateTempYamlFile("- invalid: yaml: content: [");
        var attribute = new YamlDataAttribute(invalidYamlFile);
        
        var method = typeof(YamlDataAttributeTests).GetMethod(nameof(SampleTestMethodWithDictionary), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        var action = () => attribute.GetData(method!);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"*读取YAML测试数据失败: {invalidYamlFile}*");
    }

    // 示例测试方法用于反射测试
    private void SampleTestMethodWithDictionary(Dictionary<string, object> data) { }
    private void SampleTestMethodWithStrongType(SearchTestData data) { }
    private void SampleTestMethodWithoutParameters() { }

    /// <summary>
    /// 创建临时YAML文件
    /// </summary>
    /// <param name="content">文件内容</param>
    /// <returns>文件路径</returns>
    private string CreateTempYamlFile(string content)
    {
        var tempFile = Path.Combine(_testDataDirectory, $"temp_{Guid.NewGuid()}.yaml");
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }
}