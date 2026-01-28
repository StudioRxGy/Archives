using EnterpriseAutomationFramework.Services.Data;
using EnterpriseAutomationFramework.Tests.TestModels;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// CSV 数据属性测试
/// </summary>
public class CsvDataAttributeTests : IDisposable
{
    private readonly string _testDataDirectory;
    private readonly List<string> _tempFiles;

    public CsvDataAttributeTests()
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
        var action = () => new CsvDataAttribute(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("filePath");
    }

    [Fact]
    public void GetData_WithValidCsvFile_ShouldReturnTestData()
    {
        // Arrange
        var csvContent = "TestName,SearchQuery,ExpectedResultCount,Environment,IsEnabled\n" +
                        "Test1,keyword1,5,dev,true\n" +
                        "Test2,keyword2,10,test,false";
        var csvFile = CreateTempCsvFile(csvContent);
        var attribute = new CsvDataAttribute(csvFile);
        
        // 创建一个模拟的测试方法
        var method = typeof(CsvDataAttributeTests).GetMethod(nameof(SampleTestMethodWithDictionary), 
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
        firstRow["ExpectedResultCount"].Should().Be(5);
        firstRow["Environment"].Should().Be("dev");
        firstRow["IsEnabled"].Should().Be(true);
    }

    [Fact]
    public void GetData_WithStronglyTypedParameter_ShouldReturnStronglyTypedData()
    {
        // Arrange
        var csvFile = Path.Combine(_testDataDirectory, "valid_test_data.csv");
        var attribute = new CsvDataAttribute(csvFile);
        
        // 创建一个模拟的测试方法
        var method = typeof(CsvDataAttributeTests).GetMethod(nameof(SampleTestMethodWithStrongType), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = attribute.GetData(method!).ToList();

        // Assert
        result.Should().HaveCount(4);
        result[0].Should().HaveCount(1);
        result[0][0].Should().BeOfType<SearchTestData>();
        
        var firstRow = (SearchTestData)result[0][0];
        firstRow.TestName.Should().Be("搜索功能测试1");
        firstRow.SearchQuery.Should().Be("playwright");
        firstRow.ExpectedResultCount.Should().Be(10);
        firstRow.Environment.Should().Be("Development");
        firstRow.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void GetData_WithNullTestMethod_ShouldThrowArgumentNullException()
    {
        // Arrange
        var csvFile = Path.Combine(_testDataDirectory, "valid_test_data.csv");
        var attribute = new CsvDataAttribute(csvFile);

        // Act & Assert
        var action = () => attribute.GetData(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("testMethod");
    }

    [Fact]
    public void GetData_WithMethodWithoutParameters_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var csvFile = Path.Combine(_testDataDirectory, "valid_test_data.csv");
        var attribute = new CsvDataAttribute(csvFile);
        
        var method = typeof(CsvDataAttributeTests).GetMethod(nameof(SampleTestMethodWithoutParameters), 
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
        var nonExistentFile = Path.Combine(_testDataDirectory, "non_existent.csv");
        var attribute = new CsvDataAttribute(nonExistentFile);
        
        var method = typeof(CsvDataAttributeTests).GetMethod(nameof(SampleTestMethodWithDictionary), 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        var action = () => attribute.GetData(method!);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"*读取CSV测试数据失败: {nonExistentFile}*");
    }

    // 示例测试方法用于反射测试
    private void SampleTestMethodWithDictionary(Dictionary<string, object> data) { }
    private void SampleTestMethodWithStrongType(SearchTestData data) { }
    private void SampleTestMethodWithoutParameters() { }

    /// <summary>
    /// 创建临时CSV文件
    /// </summary>
    /// <param name="content">文件内容</param>
    /// <returns>文件路径</returns>
    private string CreateTempCsvFile(string content)
    {
        var tempFile = Path.Combine(_testDataDirectory, $"temp_{Guid.NewGuid()}.csv");
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }
}