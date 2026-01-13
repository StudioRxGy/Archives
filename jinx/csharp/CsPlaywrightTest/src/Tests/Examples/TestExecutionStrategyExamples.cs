using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using EnterpriseAutomationFramework.Core.Attributes;
using EnterpriseAutomationFramework.Core.Configuration;
using EnterpriseAutomationFramework.Core.Utilities;

namespace EnterpriseAutomationFramework.Tests.Examples;

/// <summary>
/// 测试执行策略示例
/// 演示如何使用不同的测试执行策略
/// </summary>
public class TestExecutionStrategyExamples
{
    private readonly ILogger<TestExecutionStrategy> _logger;

    public TestExecutionStrategyExamples()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<TestExecutionStrategy>();
    }

    /// <summary>
    /// 示例：仅执行 UI 测试
    /// </summary>
    [Fact]
    [UITest]
    [TestCategory(TestCategory.UserInterface)]
    [TestPriority(TestPriority.High)]
    [Trait("Speed", "Fast")]
    [Trait("Suite", "Smoke")]
    public void Example_UITestsOnly_ShouldGenerateCorrectFilter()
    {
        // Arrange
        var strategy = TestExecutionStrategy.CreateForUITests(_logger);

        // Act
        var filter = strategy.GenerateFilterExpression();
        var command = strategy.GenerateExecutionCommand("MyProject.Tests.csproj");

        // Assert
        filter.Should().Be("Type=UI");
        command.Should().Contain("--filter \"Type=UI\"");
        command.Should().Contain("--max-parallel-threads 2");

        // 输出示例
        Console.WriteLine("=== UI 测试执行策略示例 ===");
        Console.WriteLine($"过滤器: {filter}");
        Console.WriteLine($"命令: {command}");
        Console.WriteLine();
    }

    /// <summary>
    /// 示例：仅执行 API 测试
    /// </summary>
    [Fact]
    [APITest]
    [TestCategory(TestCategory.ApiClient)]
    [TestPriority(TestPriority.High)]
    [Trait("Speed", "Fast")]
    [Trait("Suite", "Smoke")]
    public void Example_APITestsOnly_ShouldGenerateCorrectFilter()
    {
        // Arrange
        var strategy = TestExecutionStrategy.CreateForAPITests(_logger);

        // Act
        var filter = strategy.GenerateFilterExpression();
        var command = strategy.GenerateExecutionCommand("MyProject.Tests.csproj");

        // Assert
        filter.Should().Be("Type=API");
        command.Should().Contain("--filter \"Type=API\"");
        command.Should().Contain($"--max-parallel-threads {Environment.ProcessorCount}");

        // 输出示例
        Console.WriteLine("=== API 测试执行策略示例 ===");
        Console.WriteLine($"过滤器: {filter}");
        Console.WriteLine($"命令: {command}");
        Console.WriteLine();
    }

    /// <summary>
    /// 示例：执行混合测试（UI + API）
    /// </summary>
    [Fact]
    [Trait("Type", "Mixed")]
    [TestCategory(TestCategory.Core)]
    [TestPriority(TestPriority.Medium)]
    [Trait("Speed", "Fast")]
    public void Example_MixedTests_ShouldGenerateCorrectFilter()
    {
        // Arrange
        var settings = TestExecutionSettings.CreateForMixedTests();
        var strategy = new TestExecutionStrategy(settings, _logger);

        // Act
        var filter = strategy.GenerateFilterExpression();
        var command = strategy.GenerateExecutionCommand("MyProject.Tests.csproj");

        // Assert
        filter.Should().Be("(Type=UI|Type=API)");
        command.Should().Contain("--filter \"(Type=UI|Type=API)\"");
        command.Should().Contain($"--max-parallel-threads {Environment.ProcessorCount / 2}");

        // 输出示例
        Console.WriteLine("=== 混合测试执行策略示例 ===");
        Console.WriteLine($"过滤器: {filter}");
        Console.WriteLine($"命令: {command}");
        Console.WriteLine();
    }

    /// <summary>
    /// 示例：执行集成测试
    /// </summary>
    [Fact]
    [IntegrationTest]
    [TestCategory(TestCategory.Core)]
    [TestPriority(TestPriority.Medium)]
    [Trait("Speed", "Slow")]
    [Trait("Suite", "Regression")]
    public void Example_IntegrationTests_ShouldGenerateCorrectFilter()
    {
        // Arrange
        var strategy = TestExecutionStrategy.CreateForIntegrationTests(_logger);

        // Act
        var filter = strategy.GenerateFilterExpression();
        var command = strategy.GenerateExecutionCommand("MyProject.Tests.csproj");

        // Assert
        filter.Should().Be("Type=Integration");
        command.Should().Contain("--filter \"Type=Integration\"");
        command.Should().Contain("--parallel false"); // 集成测试串行执行

        // 输出示例
        Console.WriteLine("=== 集成测试执行策略示例 ===");
        Console.WriteLine($"过滤器: {filter}");
        Console.WriteLine($"命令: {command}");
        Console.WriteLine();
    }

    /// <summary>
    /// 示例：执行快速测试
    /// </summary>
    [Fact]
    [Trait("Type", "Unit")]
    [TestCategory(TestCategory.Core)]
    [TestPriority(TestPriority.High)]
    [Trait("Speed", "Fast")]
    public void Example_FastTests_ShouldGenerateCorrectFilter()
    {
        // Arrange
        var settings = TestExecutionSettings.CreateForFastTests();
        var strategy = new TestExecutionStrategy(settings, _logger);

        // Act
        var filter = strategy.GenerateFilterExpression();
        var command = strategy.GenerateExecutionCommand("MyProject.Tests.csproj");

        // Assert
        filter.Should().Be("Speed=Fast");
        command.Should().Contain("--filter \"Speed=Fast\"");
        command.Should().Contain("--blame-hang-timeout 60000ms"); // 1分钟超时

        // 输出示例
        Console.WriteLine("=== 快速测试执行策略示例 ===");
        Console.WriteLine($"过滤器: {filter}");
        Console.WriteLine($"命令: {command}");
        Console.WriteLine();
    }

    /// <summary>
    /// 示例：执行冒烟测试
    /// </summary>
    [Fact]
    [Trait("Type", "Unit")]
    [TestCategory(TestCategory.Core)]
    [TestPriority(TestPriority.Critical)]
    [Trait("Speed", "Fast")]
    [Trait("Suite", "Smoke")]
    public void Example_SmokeTests_ShouldGenerateCorrectFilter()
    {
        // Arrange
        var settings = TestExecutionSettings.CreateForSmokeTests();
        var strategy = new TestExecutionStrategy(settings, _logger);

        // Act
        var filter = strategy.GenerateFilterExpression();
        var command = strategy.GenerateExecutionCommand("MyProject.Tests.csproj");

        // Assert
        filter.Should().Contain("Suite=Smoke");
        filter.Should().Contain("Priority=Critical|Priority=High");
        command.Should().Contain("--blame-crash"); // 失败时停止

        // 输出示例
        Console.WriteLine("=== 冒烟测试执行策略示例 ===");
        Console.WriteLine($"过滤器: {filter}");
        Console.WriteLine($"命令: {command}");
        Console.WriteLine();
    }

    /// <summary>
    /// 示例：执行回归测试
    /// </summary>
    [Fact]
    [Trait("Type", "Unit")]
    [TestCategory(TestCategory.Core)]
    [TestPriority(TestPriority.Medium)]
    [Trait("Speed", "Slow")]
    [Trait("Suite", "Regression")]
    public void Example_RegressionTests_ShouldGenerateCorrectFilter()
    {
        // Arrange
        var settings = TestExecutionSettings.CreateForRegressionTests();
        var strategy = new TestExecutionStrategy(settings, _logger);

        // Act
        var filter = strategy.GenerateFilterExpression();
        var command = strategy.GenerateExecutionCommand("MyProject.Tests.csproj");

        // Assert
        filter.Should().Be("Suite=Regression");
        command.Should().Contain("--filter \"Suite=Regression\"");
        command.Should().Contain("--collect:\"XPlat Code Coverage\""); // 代码覆盖率收集

        // 输出示例
        Console.WriteLine("=== 回归测试执行策略示例 ===");
        Console.WriteLine($"过滤器: {filter}");
        Console.WriteLine($"命令: {command}");
        Console.WriteLine();
    }

    /// <summary>
    /// 示例：使用 TestExecutionManager 执行不同策略
    /// </summary>
    [Fact]
    [Trait("Type", "Unit")]
    [TestCategory(TestCategory.Core)]
    [TestPriority(TestPriority.Medium)]
    [Trait("Speed", "Fast")]
    public async Task Example_TestExecutionManager_ShouldSupportAllStrategies()
    {
        // Arrange
        var managerLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<TestExecutionManager>();
        var defaultStrategy = TestExecutionStrategy.CreateDefault(_logger);
        var manager = new TestExecutionManager(defaultStrategy, managerLogger);

        // Act
        var supportedStrategies = manager.GetSupportedExecutionStrategies();

        // Assert
        supportedStrategies.Should().HaveCount(7);
        supportedStrategies.Should().Contain("UITestsOnly");
        supportedStrategies.Should().Contain("APITestsOnly");
        supportedStrategies.Should().Contain("MixedTests");
        supportedStrategies.Should().Contain("IntegrationTests");
        supportedStrategies.Should().Contain("FastTests");
        supportedStrategies.Should().Contain("SmokeTests");
        supportedStrategies.Should().Contain("RegressionTests");

        // 输出示例
        Console.WriteLine("=== TestExecutionManager 支持的策略 ===");
        foreach (var strategy in supportedStrategies)
        {
            Console.WriteLine($"- {strategy}");
        }
        Console.WriteLine();

        // 演示策略名称执行（注意：这些调用会尝试执行实际的测试命令）
        Console.WriteLine("=== 策略名称映射示例 ===");
        Console.WriteLine("manager.ExecuteTestsByStrategyNameAsync(\"UITestsOnly\") -> 执行仅 UI 测试");
        Console.WriteLine("manager.ExecuteTestsByStrategyNameAsync(\"APITestsOnly\") -> 执行仅 API 测试");
        Console.WriteLine("manager.ExecuteTestsByStrategyNameAsync(\"MixedTests\") -> 执行混合测试");
        Console.WriteLine();
    }

    /// <summary>
    /// 示例：自定义测试执行策略
    /// </summary>
    [Fact]
    [Trait("Type", "Unit")]
    [TestCategory(TestCategory.Core)]
    [TestPriority(TestPriority.Low)]
    [Trait("Speed", "Fast")]
    public void Example_CustomTestExecutionStrategy_ShouldWorkCorrectly()
    {
        // Arrange - 创建自定义设置
        var customSettings = new TestExecutionSettings
        {
            TestTypes = new List<TestType> { TestType.UI },
            TestCategories = new List<TestCategory> { TestCategory.PageObject, TestCategory.UserInterface },
            TestPriorities = new List<TestPriority> { TestPriority.Critical },
            TestTags = new List<string> { "Smoke", "Critical" },
            TestSpeeds = new List<string> { "Fast" },
            ExcludedTestTags = new List<string> { "Slow", "Flaky" },
            ParallelExecution = true,
            MaxParallelism = 3,
            TestTimeout = 180000, // 3分钟
            VerboseOutput = true,
            StopOnFirstFailure = true
        };

        var customStrategy = new TestExecutionStrategy(customSettings, _logger);

        // Act
        var filter = customStrategy.GenerateFilterExpression();
        var command = customStrategy.GenerateExecutionCommand("MyProject.Tests.csproj");
        var description = customStrategy.GetSettingsDescription();

        // Assert
        filter.Should().Contain("Type=UI");
        filter.Should().Contain("Category=PageObject|Category=UserInterface");
        filter.Should().Contain("Priority=Critical");
        filter.Should().Contain("Tag=Smoke|Tag=Critical");
        filter.Should().Contain("Speed=Fast");
        filter.Should().Contain("!Tag=Slow");
        filter.Should().Contain("!Tag=Flaky");

        command.Should().Contain("--max-parallel-threads 3");
        command.Should().Contain("--verbosity normal");
        command.Should().Contain("--blame-crash");
        command.Should().Contain("--blame-hang-timeout 180000ms");

        // 输出示例
        Console.WriteLine("=== 自定义测试执行策略示例 ===");
        Console.WriteLine($"过滤器: {filter}");
        Console.WriteLine($"命令: {command}");
        Console.WriteLine("设置描述:");
        Console.WriteLine(description);
        Console.WriteLine();
    }

    /// <summary>
    /// 示例：测试结果处理
    /// </summary>
    [Fact]
    [Trait("Type", "Unit")]
    [TestCategory(TestCategory.Core)]
    [TestPriority(TestPriority.Low)]
    [Trait("Speed", "Fast")]
    public void Example_TestExecutionResult_ShouldProvideComprehensiveInformation()
    {
        // Arrange
        var result = new TestExecutionResult
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(10),
            Success = true,
            Command = "dotnet test MyProject.Tests.csproj --filter \"Type=UI\" --parallel --max-parallel-threads 2",
            Filter = "Type=UI"
        };

        // 添加测试结果
        result.AddTestResult(new TestCaseResult
        {
            TestName = "HomePage_SearchFunctionality_ShouldWork",
            TestClass = "HomePageTests",
            TestMethod = "SearchFunctionality_ShouldWork",
            Passed = true,
            Duration = TimeSpan.FromSeconds(5),
            Categories = new List<string> { "UI", "PageObject" },
            Tags = new List<string> { "Smoke", "Fast" }
        });

        result.AddTestResult(new TestCaseResult
        {
            TestName = "HomePage_Navigation_ShouldWork",
            TestClass = "HomePageTests", 
            TestMethod = "Navigation_ShouldWork",
            Passed = false,
            Duration = TimeSpan.FromSeconds(8),
            ErrorMessage = "Element '#navigation-menu' was not found",
            StackTrace = "at HomePage.ClickNavigationMenu() line 45",
            Categories = new List<string> { "UI", "PageObject" },
            Tags = new List<string> { "Regression" }
        });

        result.AddTestResult(new TestCaseResult
        {
            TestName = "HomePage_LoadTime_ShouldBeFast",
            TestClass = "HomePageTests",
            TestMethod = "LoadTime_ShouldBeFast", 
            Skipped = true,
            Categories = new List<string> { "UI", "Performance" },
            Tags = new List<string> { "Slow" }
        });

        result.AddOutputFile("TestResults.trx");
        result.AddOutputFile("TestResults.html");
        result.AddMetadata("Environment", "Development");
        result.AddMetadata("Browser", "Chrome");
        result.AddMetadata("Resolution", "1920x1080");

        // Act
        var summary = result.GetSummary();
        var detailedReport = result.GetDetailedReport();

        // Assert
        result.TotalTests.Should().Be(3);
        result.PassedTests.Should().Be(1);
        result.FailedTests.Should().Be(1);
        result.SkippedTests.Should().Be(1);
        result.PassRate.Should().BeApproximately(33.3, 0.1);
        result.FailureRate.Should().BeApproximately(33.3, 0.1);

        // 输出示例
        Console.WriteLine("=== 测试执行结果示例 ===");
        Console.WriteLine($"摘要: {summary}");
        Console.WriteLine();
        Console.WriteLine("详细报告:");
        Console.WriteLine(detailedReport);
        Console.WriteLine();
    }
}