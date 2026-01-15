using EnterpriseAutomationFramework.Services.Data;
using EnterpriseAutomationFramework.Tests.TestModels;
using EnterpriseAutomationFramework.Core.Attributes;
using FluentAssertions;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Integration;

/// <summary>
/// 数据驱动集成测试
/// </summary>
[IntegrationTest]
[TestCategory(TestCategory.DataDriven)]
[TestPriority(TestPriority.Medium)]
[FastTest]
public class DataDrivenIntegrationTests
{
    /// <summary>
    /// 使用CSV数据源的搜索功能测试
    /// </summary>
    /// <param name="testData">测试数据</param>
    [Theory]
    [CsvData("TestData/valid_test_data.csv")]
    public void SearchFunctionality_WithCsvData_ShouldProcessTestData(SearchTestData testData)
    {
        // Arrange & Act
        var result = ProcessSearchTestData(testData);

        // Assert
        result.Should().NotBeNull();
        result.TestName.Should().Be(testData.TestName);
        result.SearchQuery.Should().Be(testData.SearchQuery);
        result.ExpectedResultCount.Should().Be(testData.ExpectedResultCount);
        result.Environment.Should().Be(testData.Environment);
        result.IsEnabled.Should().Be(testData.IsEnabled);
    }

    /// <summary>
    /// 使用JSON数据源的搜索功能测试
    /// </summary>
    /// <param name="testData">测试数据</param>
    [Theory]
    [JsonData("TestData/search_test_data.json")]
    public void SearchFunctionality_WithJsonData_ShouldProcessTestData(SearchTestData testData)
    {
        // Arrange & Act
        var result = ProcessSearchTestData(testData);

        // Assert
        result.Should().NotBeNull();
        result.TestName.Should().Be(testData.TestName);
        result.SearchQuery.Should().Be(testData.SearchQuery);
        result.ExpectedResultCount.Should().Be(testData.ExpectedResultCount);
        result.Environment.Should().Be(testData.Environment);
        result.IsEnabled.Should().Be(testData.IsEnabled);
    }

    /// <summary>
    /// 使用YAML数据源的搜索功能测试
    /// </summary>
    /// <param name="testData">测试数据</param>
    [Theory]
    [YamlData("TestData/search_test_data.yaml")]
    public void SearchFunctionality_WithYamlData_ShouldProcessTestData(SearchTestData testData)
    {
        // Arrange & Act
        var result = ProcessSearchTestData(testData);

        // Assert
        result.Should().NotBeNull();
        result.TestName.Should().Be(testData.TestName);
        result.SearchQuery.Should().Be(testData.SearchQuery);
        result.ExpectedResultCount.Should().Be(testData.ExpectedResultCount);
        result.Environment.Should().Be(testData.Environment);
        result.IsEnabled.Should().Be(testData.IsEnabled);
    }

    /// <summary>
    /// 使用CSV数据源的动态数据测试
    /// </summary>
    /// <param name="data">动态数据</param>
    [Theory]
    [CsvData("TestData/valid_test_data.csv")]
    public void DynamicDataProcessing_WithCsvData_ShouldProcessDynamicData(Dictionary<string, object> data)
    {
        // Arrange & Act
        var result = ProcessDynamicData(data);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("TestName");
        result.Should().ContainKey("SearchQuery");
        result.Should().ContainKey("ExpectedResultCount");
        result.Should().ContainKey("Environment");
        result.Should().ContainKey("IsEnabled");
        
        // 验证数据类型转换
        result["TestName"].Should().BeOfType<string>();
        result["SearchQuery"].Should().BeOfType<string>();
        result["ExpectedResultCount"].Should().BeOfType<int>();
        result["Environment"].Should().BeOfType<string>();
        result["IsEnabled"].Should().BeOfType<bool>();
    }

    /// <summary>
    /// 使用JSON数据源的动态数据测试
    /// </summary>
    /// <param name="data">动态数据</param>
    [Theory]
    [JsonData("TestData/search_test_data.json")]
    public void DynamicDataProcessing_WithJsonData_ShouldProcessDynamicData(Dictionary<string, object> data)
    {
        // Arrange & Act
        var result = ProcessDynamicData(data);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("testName");
        result.Should().ContainKey("searchQuery");
        result.Should().ContainKey("expectedResultCount");
        result.Should().ContainKey("environment");
        result.Should().ContainKey("isEnabled");
        
        // 验证数据内容
        result["testName"].ToString().Should().StartWith("JSON");
        result["searchQuery"].ToString().Should().Contain("json");
    }

    /// <summary>
    /// 使用YAML数据源的动态数据测试
    /// </summary>
    /// <param name="data">动态数据</param>
    [Theory]
    [YamlData("TestData/search_test_data.yaml")]
    public void DynamicDataProcessing_WithYamlData_ShouldProcessDynamicData(Dictionary<string, object> data)
    {
        // Arrange & Act
        var result = ProcessDynamicData(data);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("testName");
        result.Should().ContainKey("searchQuery");
        result.Should().ContainKey("expectedResultCount");
        result.Should().ContainKey("environment");
        result.Should().ContainKey("isEnabled");
        
        // 验证数据内容
        result["testName"].ToString().Should().StartWith("YAML");
        result["searchQuery"].ToString().Should().Contain("yaml");
    }

    /// <summary>
    /// 跨数据源一致性测试
    /// </summary>
    [Fact]
    public void DataSourceConsistency_AllDataSources_ShouldHaveConsistentStructure()
    {
        // Arrange
        var csvAttribute = new CsvDataAttribute("TestData/valid_test_data.csv");
        var jsonAttribute = new JsonDataAttribute("TestData/search_test_data.json");
        var yamlAttribute = new YamlDataAttribute("TestData/search_test_data.yaml");
        
        var method = typeof(DataDrivenIntegrationTests).GetMethod(nameof(SearchFunctionality_WithCsvData_ShouldProcessTestData));

        // Act
        var csvData = csvAttribute.GetData(method!).ToList();
        var jsonData = jsonAttribute.GetData(method!).ToList();
        var yamlData = yamlAttribute.GetData(method!).ToList();

        // Assert
        csvData.Should().NotBeEmpty();
        jsonData.Should().NotBeEmpty();
        yamlData.Should().NotBeEmpty();
        
        // 验证所有数据源都返回SearchTestData类型
        csvData.All(row => row[0] is SearchTestData).Should().BeTrue();
        jsonData.All(row => row[0] is SearchTestData).Should().BeTrue();
        yamlData.All(row => row[0] is SearchTestData).Should().BeTrue();
    }

    /// <summary>
    /// 处理搜索测试数据
    /// </summary>
    /// <param name="testData">测试数据</param>
    /// <returns>处理后的测试数据</returns>
    private SearchTestData ProcessSearchTestData(SearchTestData testData)
    {
        // 模拟业务逻辑处理
        return new SearchTestData
        {
            TestName = testData.TestName,
            SearchQuery = testData.SearchQuery,
            ExpectedResultCount = testData.ExpectedResultCount,
            Environment = testData.Environment,
            IsEnabled = testData.IsEnabled
        };
    }

    /// <summary>
    /// 处理动态数据
    /// </summary>
    /// <param name="data">动态数据</param>
    /// <returns>处理后的动态数据</returns>
    private Dictionary<string, object> ProcessDynamicData(Dictionary<string, object> data)
    {
        // 模拟业务逻辑处理
        var result = new Dictionary<string, object>();
        
        foreach (var kvp in data)
        {
            result[kvp.Key] = kvp.Value;
        }
        
        return result;
    }
}