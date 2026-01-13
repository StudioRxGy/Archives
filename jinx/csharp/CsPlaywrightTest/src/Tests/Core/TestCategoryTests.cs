using Xunit;
using FluentAssertions;
using EnterpriseAutomationFramework.Core.Attributes;
using EnterpriseAutomationFramework.Core.Utilities;
using EnterpriseAutomationFramework.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// 测试分类标记功能测试
/// </summary>
[Trait("Type", "Unit")]
[Trait("Category", "Core")]
[Trait("Priority", "High")]
[Trait("Speed", "Fast")]
public class TestCategoryTests
{
    /// <summary>
    /// 测试类型属性应该正确设置 Trait
    /// </summary>
    [Fact]
    [Trait("Tag", "Attributes")]
    public void TestTypeAttribute_ShouldSetCorrectTrait()
    {
        // Arrange & Act
        var attribute = new TestTypeAttribute(TestType.UI);

        // Assert
        attribute.Name.Should().Be("Type");
        attribute.Value.Should().Be("UI");
        attribute.Type.Should().Be(TestType.UI);
    }

    /// <summary>
    /// 测试分类属性应该正确设置 Trait
    /// </summary>
    [Fact]
    [Trait("Tag", "Attributes")]
    public void TestCategoryAttribute_ShouldSetCorrectTrait()
    {
        // Arrange & Act
        var attribute = new TestCategoryAttribute(TestCategory.PageObject);

        // Assert
        attribute.Name.Should().Be("Category");
        attribute.Value.Should().Be("PageObject");
        attribute.Category.Should().Be(TestCategory.PageObject);
    }

    /// <summary>
    /// 测试优先级属性应该正确设置 Trait
    /// </summary>
    [Fact]
    [Trait("Tag", "Attributes")]
    public void TestPriorityAttribute_ShouldSetCorrectTrait()
    {
        // Arrange & Act
        var attribute = new TestPriorityAttribute(TestPriority.Critical);

        // Assert
        attribute.Name.Should().Be("Priority");
        attribute.Value.Should().Be("Critical");
        attribute.Priority.Should().Be(TestPriority.Critical);
    }

    /// <summary>
    /// UI 测试属性应该正确应用
    /// </summary>
    [Fact]
    [Trait("Tag", "Attributes")]
    public void UITestAttribute_ShouldApplyCorrectly()
    {
        // Arrange & Act
        var attribute = new UITestAttribute();

        // Assert
        attribute.Should().NotBeNull();
        attribute.Should().BeOfType<UITestAttribute>();
    }

    /// <summary>
    /// API 测试属性应该正确应用
    /// </summary>
    [Fact]
    [Trait("Tag", "Attributes")]
    public void APITestAttribute_ShouldApplyCorrectly()
    {
        // Arrange & Act
        var attribute = new APITestAttribute();

        // Assert
        attribute.Should().NotBeNull();
        attribute.Should().BeOfType<APITestAttribute>();
    }

    /// <summary>
    /// 测试过滤器应该生成正确的类型过滤器
    /// </summary>
    [Fact]
    [Trait("Tag", "Filter")]
    public void TestFilter_ByType_ShouldGenerateCorrectFilter()
    {
        // Act
        var filter = TestFilter.ByType(TestType.UI);

        // Assert
        filter.Should().Be("Type=UI");
    }

    /// <summary>
    /// 测试过滤器应该生成正确的多类型过滤器
    /// </summary>
    [Fact]
    [Trait("Tag", "Filter")]
    public void TestFilter_ByTypes_ShouldGenerateCorrectFilter()
    {
        // Act
        var filter = TestFilter.ByTypes(TestType.UI, TestType.API);

        // Assert
        filter.Should().Be("(Type=UI|Type=API)");
    }

    /// <summary>
    /// 测试过滤器应该生成正确的分类过滤器
    /// </summary>
    [Fact]
    [Trait("Tag", "Filter")]
    public void TestFilter_ByCategory_ShouldGenerateCorrectFilter()
    {
        // Act
        var filter = TestFilter.ByCategory(TestCategory.PageObject);

        // Assert
        filter.Should().Be("Category=PageObject");
    }

    /// <summary>
    /// 测试过滤器应该正确组合 AND 条件
    /// </summary>
    [Fact]
    [Trait("Tag", "Filter")]
    public void TestFilter_And_ShouldCombineFiltersCorrectly()
    {
        // Arrange
        var typeFilter = TestFilter.ByType(TestType.UI);
        var categoryFilter = TestFilter.ByCategory(TestCategory.PageObject);

        // Act
        var combinedFilter = TestFilter.And(typeFilter, categoryFilter);

        // Assert
        combinedFilter.Should().Be("Type=UI&Category=PageObject");
    }

    /// <summary>
    /// 测试过滤器应该正确组合 OR 条件
    /// </summary>
    [Fact]
    [Trait("Tag", "Filter")]
    public void TestFilter_Or_ShouldCombineFiltersCorrectly()
    {
        // Arrange
        var typeFilter1 = TestFilter.ByType(TestType.UI);
        var typeFilter2 = TestFilter.ByType(TestType.API);

        // Act
        var combinedFilter = TestFilter.Or(typeFilter1, typeFilter2);

        // Assert
        combinedFilter.Should().Be("(Type=UI|Type=API)");
    }

    /// <summary>
    /// 测试过滤器应该正确生成 NOT 条件
    /// </summary>
    [Fact]
    [Trait("Tag", "Filter")]
    public void TestFilter_Not_ShouldGenerateCorrectFilter()
    {
        // Arrange
        var typeFilter = TestFilter.ByType(TestType.Integration);

        // Act
        var notFilter = TestFilter.Not(typeFilter);

        // Assert
        notFilter.Should().Be("!Type=Integration");
    }

    /// <summary>
    /// 测试过滤器预定义过滤器应该正确工作
    /// </summary>
    [Fact]
    [Trait("Tag", "Filter")]
    public void TestFilter_PredefinedFilters_ShouldWorkCorrectly()
    {
        // Assert
        TestFilter.UITestsOnly.Should().Be("Type=UI");
        TestFilter.APITestsOnly.Should().Be("Type=API");
        TestFilter.IntegrationTestsOnly.Should().Be("Type=Integration");
        TestFilter.UIAndAPITests.Should().Be("(Type=UI|Type=API)");
        TestFilter.FastTestsOnly.Should().Be("Speed=Fast");
        TestFilter.SmokeTestsOnly.Should().Be("Suite=Smoke");
    }

    /// <summary>
    /// 测试过滤器应该生成正确的测试命令
    /// </summary>
    [Fact]
    [Trait("Tag", "Filter")]
    public void TestFilter_GenerateTestCommand_ShouldCreateCorrectCommand()
    {
        // Arrange
        var filter = TestFilter.UITestsOnly;
        var projectPath = "MyProject.Tests.csproj";

        // Act
        var command = TestFilter.GenerateTestCommand(filter, projectPath);

        // Assert
        command.Should().Be("dotnet test \"MyProject.Tests.csproj\" --filter \"Type=UI\"");
    }

    /// <summary>
    /// 测试执行设置应该正确创建默认设置
    /// </summary>
    [Fact]
    [Trait("Tag", "ExecutionSettings")]
    public void TestExecutionSettings_CreateDefault_ShouldHaveCorrectDefaults()
    {
        // Act
        var settings = TestExecutionSettings.CreateDefault();

        // Assert
        settings.ParallelExecution.Should().BeTrue();
        settings.MaxParallelism.Should().Be(Environment.ProcessorCount);
        settings.TestTimeout.Should().Be(300000);
        settings.StopOnFirstFailure.Should().BeFalse();
    }

    /// <summary>
    /// 测试执行设置应该正确创建 UI 测试设置
    /// </summary>
    [Fact]
    [Trait("Tag", "ExecutionSettings")]
    public void TestExecutionSettings_CreateForUITests_ShouldHaveCorrectSettings()
    {
        // Act
        var settings = TestExecutionSettings.CreateForUITests();

        // Assert
        settings.TestTypes.Should().Contain(TestType.UI);
        settings.TestTypes.Should().HaveCount(1);
        settings.MaxParallelism.Should().Be(2);
        settings.TestTimeout.Should().Be(600000);
    }

    /// <summary>
    /// 测试执行设置应该正确创建 API 测试设置
    /// </summary>
    [Fact]
    [Trait("Tag", "ExecutionSettings")]
    public void TestExecutionSettings_CreateForAPITests_ShouldHaveCorrectSettings()
    {
        // Act
        var settings = TestExecutionSettings.CreateForAPITests();

        // Assert
        settings.TestTypes.Should().Contain(TestType.API);
        settings.TestTypes.Should().HaveCount(1);
        settings.MaxParallelism.Should().Be(Environment.ProcessorCount);
        settings.TestTimeout.Should().Be(120000);
    }

    /// <summary>
    /// 测试执行策略应该生成正确的过滤器表达式
    /// </summary>
    [Fact]
    [Trait("Tag", "ExecutionStrategy")]
    public void TestExecutionStrategy_GenerateFilterExpression_ShouldCreateCorrectFilter()
    {
        // Arrange
        var settings = new TestExecutionSettings
        {
            TestTypes = new List<TestType> { TestType.UI, TestType.API },
            TestCategories = new List<TestCategory> { TestCategory.PageObject }
        };
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<TestExecutionStrategy>();
        var strategy = new TestExecutionStrategy(settings, logger);

        // Act
        var filter = strategy.GenerateFilterExpression();

        // Assert
        filter.Should().Contain("Type=UI|Type=API");
        filter.Should().Contain("Category=PageObject");
    }

    /// <summary>
    /// 测试执行策略应该生成正确的执行命令
    /// </summary>
    [Fact]
    [Trait("Tag", "ExecutionStrategy")]
    public void TestExecutionStrategy_GenerateExecutionCommand_ShouldCreateCorrectCommand()
    {
        // Arrange
        var settings = TestExecutionSettings.CreateForUITests();
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<TestExecutionStrategy>();
        var strategy = new TestExecutionStrategy(settings, logger);

        // Act
        var command = strategy.GenerateExecutionCommand("TestProject.csproj");

        // Assert
        command.Should().Contain("dotnet test");
        command.Should().Contain("TestProject.csproj");
        command.Should().Contain("--filter");
        command.Should().Contain("Type=UI");
        command.Should().Contain("--parallel");
        command.Should().Contain("--max-parallel-threads 2");
    }

    /// <summary>
    /// 测试执行结果应该正确计算统计信息
    /// </summary>
    [Fact]
    [Trait("Tag", "ExecutionResult")]
    public void TestExecutionResult_Statistics_ShouldCalculateCorrectly()
    {
        // Arrange
        var result = new TestExecutionResult();

        // Act
        result.AddTestResult(new TestCaseResult { TestName = "Test1", Passed = true });
        result.AddTestResult(new TestCaseResult { TestName = "Test2", Passed = false });
        result.AddTestResult(new TestCaseResult { TestName = "Test3", Passed = true });
        result.AddTestResult(new TestCaseResult { TestName = "Test4", Skipped = true });

        // Assert
        result.TotalTests.Should().Be(4);
        result.PassedTests.Should().Be(2);
        result.FailedTests.Should().Be(1);
        result.SkippedTests.Should().Be(1);
        result.PassRate.Should().Be(50.0);
        result.FailureRate.Should().Be(25.0);
    }
}