using EnterpriseAutomationFramework.Core.Exceptions;
using EnterpriseAutomationFramework.Services.Data;
using EnterpriseAutomationFramework.Tests.TestModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// CSV 数据读取器测试
/// </summary>
public class CsvDataReaderTests : IDisposable
{
    private readonly CsvDataReader _csvDataReader;
    private readonly string _testDataDirectory;
    private readonly List<string> _tempFiles;

    public CsvDataReaderTests()
    {
        var logger = new LoggerFactory().CreateLogger<CsvDataReader>();
        _csvDataReader = new CsvDataReader(logger);
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
    public void ReadData_WithValidCsvFile_ShouldReturnStronglyTypedData()
    {
        // Arrange
        var filePath = Path.Combine(_testDataDirectory, "valid_test_data.csv");

        // Act
        var result = _csvDataReader.ReadData<SearchTestData>(filePath).ToList();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(4);
        
        var firstRecord = result.First();
        firstRecord.TestName.Should().Be("搜索功能测试1");
        firstRecord.SearchQuery.Should().Be("playwright");
        firstRecord.ExpectedResultCount.Should().Be(10);
        firstRecord.Environment.Should().Be("Development");
        firstRecord.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void ReadDynamicData_WithValidCsvFile_ShouldReturnDynamicData()
    {
        // Arrange
        var filePath = Path.Combine(_testDataDirectory, "valid_test_data.csv");

        // Act
        var result = _csvDataReader.ReadDynamicData(filePath).ToList();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(4);
        
        var firstRecord = result.First();
        firstRecord.Should().ContainKey("TestName");
        firstRecord.Should().ContainKey("SearchQuery");
        firstRecord.Should().ContainKey("ExpectedResultCount");
        firstRecord.Should().ContainKey("Environment");
        firstRecord.Should().ContainKey("IsEnabled");
        
        firstRecord["TestName"].Should().Be("搜索功能测试1");
        firstRecord["SearchQuery"].Should().Be("playwright");
        firstRecord["ExpectedResultCount"].Should().Be(10);
        firstRecord["Environment"].Should().Be("Development");
        firstRecord["IsEnabled"].Should().Be(true);
    }

    [Fact]
    public void ReadData_WithNullFilePath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => _csvDataReader.ReadData<SearchTestData>(null!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*文件路径不能为空*");
    }

    [Fact]
    public void ReadData_WithEmptyFilePath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => _csvDataReader.ReadData<SearchTestData>("");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*文件路径不能为空*");
    }

    [Fact]
    public void ReadData_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDataDirectory, "non_existent.csv");

        // Act & Assert
        var action = () => _csvDataReader.ReadData<SearchTestData>(nonExistentFile);
        action.Should().Throw<FileNotFoundException>()
            .WithMessage($"*{nonExistentFile}*");
    }

    [Fact]
    public void ReadDynamicData_WithNullFilePath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => _csvDataReader.ReadDynamicData(null!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*文件路径不能为空*");
    }

    [Fact]
    public void ReadDynamicData_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDataDirectory, "non_existent.csv");

        // Act & Assert
        var action = () => _csvDataReader.ReadDynamicData(nonExistentFile);
        action.Should().Throw<FileNotFoundException>()
            .WithMessage($"*{nonExistentFile}*");
    }

    [Fact]
    public void ReadDynamicData_WithEmptyFile_ShouldThrowCsvDataException()
    {
        // Arrange
        var emptyFile = CreateTempCsvFile("");

        // Act & Assert
        var action = () => _csvDataReader.ReadDynamicData(emptyFile);
        action.Should().Throw<CsvDataException>()
            .WithMessage("*CSV文件缺少标题行*");
    }

    [Fact]
    public void ReadDynamicData_WithOnlyHeaders_ShouldReturnEmptyCollection()
    {
        // Arrange
        var headersOnlyFile = CreateTempCsvFile("Name,Value,Count");

        // Act
        var result = _csvDataReader.ReadDynamicData(headersOnlyFile).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ReadDynamicData_WithMixedDataTypes_ShouldConvertValuesCorrectly()
    {
        // Arrange
        var csvContent = "Name,Age,IsActive,CreatedDate,Score\n" +
                        "John,25,true,2023-01-01,95.5\n" +
                        "Jane,30,false,2023-02-15,87.2";
        var mixedDataFile = CreateTempCsvFile(csvContent);

        // Act
        var result = _csvDataReader.ReadDynamicData(mixedDataFile).ToList();

        // Assert
        result.Should().HaveCount(2);
        
        var firstRecord = result.First();
        firstRecord["Name"].Should().Be("John");
        firstRecord["Age"].Should().Be(25);
        firstRecord["IsActive"].Should().Be(true);
        firstRecord["CreatedDate"].Should().BeOfType<DateTime>();
        firstRecord["Score"].Should().Be(95.5);
    }

    [Fact]
    public void ReadData_WithMalformedCsv_ShouldThrowCsvDataException()
    {
        // Arrange
        var malformedCsv = CreateTempCsvFile("Name,Age\nJohn,25,ExtraColumn\nJane");

        // Act & Assert
        var action = () => _csvDataReader.ReadData<SearchTestData>(malformedCsv);
        action.Should().Throw<CsvDataException>()
            .WithMessage($"*读取CSV文件失败: {malformedCsv}*");
    }

    [Fact]
    public void ReadDynamicData_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var csvContent = "Name,Description\n" +
                        "测试,包含中文字符\n" +
                        "Test,\"Contains, comma and quotes\"";
        var specialCharsFile = CreateTempCsvFile(csvContent);

        // Act
        var result = _csvDataReader.ReadDynamicData(specialCharsFile).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0]["Name"].Should().Be("测试");
        result[0]["Description"].Should().Be("包含中文字符");
        result[1]["Name"].Should().Be("Test");
        result[1]["Description"].Should().Be("Contains, comma and quotes");
    }

    [Fact]
    public void ReadData_WithCaseInsensitiveHeaders_ShouldMapCorrectly()
    {
        // Arrange
        var csvContent = "testname,searchquery,expectedresultcount,environment,isenabled\n" +
                        "Test1,keyword1,5,dev,true";
        var caseInsensitiveFile = CreateTempCsvFile(csvContent);

        // Act
        var result = _csvDataReader.ReadData<SearchTestData>(caseInsensitiveFile).ToList();

        // Assert
        result.Should().HaveCount(1);
        var record = result.First();
        record.TestName.Should().Be("Test1");
        record.SearchQuery.Should().Be("keyword1");
        record.ExpectedResultCount.Should().Be(5);
        record.Environment.Should().Be("dev");
        record.IsEnabled.Should().BeTrue();
    }

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