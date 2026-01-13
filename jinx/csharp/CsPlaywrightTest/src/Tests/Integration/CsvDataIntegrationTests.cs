using EnterpriseAutomationFramework.Services.Data;
using EnterpriseAutomationFramework.Tests.TestModels;
using FluentAssertions;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Integration;

/// <summary>
/// CSV 数据集成测试
/// </summary>
public class CsvDataIntegrationTests
{
    [Theory]
    [CsvData("TestData/valid_test_data.csv")]
    public void TestWithCsvDataAttribute_ShouldReceiveDataFromCsv(Dictionary<string, object> testData)
    {
        // Assert
        testData.Should().NotBeNull();
        testData.Should().ContainKey("TestName");
        testData.Should().ContainKey("SearchQuery");
        testData.Should().ContainKey("ExpectedResultCount");
        testData.Should().ContainKey("Environment");
        testData.Should().ContainKey("IsEnabled");

        // 验证数据类型转换
        testData["TestName"].Should().BeOfType<string>();
        testData["SearchQuery"].Should().BeOfType<string>();
        testData["ExpectedResultCount"].Should().BeOfType<int>();
        testData["Environment"].Should().BeOfType<string>();
        testData["IsEnabled"].Should().BeOfType<bool>();

        // 验证数据不为空
        testData["TestName"].ToString().Should().NotBeNullOrEmpty();
        testData["SearchQuery"].ToString().Should().NotBeNullOrEmpty();
        ((int)testData["ExpectedResultCount"]).Should().BeGreaterThan(0);
        testData["Environment"].ToString().Should().NotBeNullOrEmpty();
    }

    [Theory]
    [CsvData("TestData/valid_test_data.csv")]
    public void TestWithStronglyTypedCsvData_ShouldReceiveStronglyTypedData(SearchTestData testData)
    {
        // Assert
        testData.Should().NotBeNull();
        testData.TestName.Should().NotBeNullOrEmpty();
        testData.SearchQuery.Should().NotBeNullOrEmpty();
        testData.ExpectedResultCount.Should().BeGreaterThan(0);
        testData.Environment.Should().NotBeNullOrEmpty();

        // 验证具体的测试数据值（基于我们创建的测试数据）
        var validTestNames = new[] { "搜索功能测试1", "搜索功能测试2", "搜索功能测试3", "搜索功能测试4" };
        validTestNames.Should().Contain(testData.TestName);

        var validEnvironments = new[] { "Development", "Test", "Staging" };
        validEnvironments.Should().Contain(testData.Environment);
    }
}