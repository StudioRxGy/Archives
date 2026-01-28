using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using EnterpriseAutomationFramework.Core.Attributes;
using EnterpriseAutomationFramework.Core.Configuration;
using EnterpriseAutomationFramework.Core.Utilities;

namespace EnterpriseAutomationFramework.Tests.Integration;

/// <summary>
/// 测试执行策略集成测试
/// 验证不同测试执行策略的实际执行效果
/// </summary>
[IntegrationTest]
[TestCategory(TestCategory.Core)]
[TestPriority(TestPriority.High)]
[Trait("Suite", "Integration")]
public class TestExecutionStrategyIntegrationTests : IAsyncLifetime
{
    private ILogger<TestExecutionStrategy>? _strategyLogger;
    private ILogger<TestExecutionManager>? _managerLogger;
    private TestExecutionManager? _executionManager;
    private readonly string _testProjectPath;

    public TestExecutionStrategyIntegrationTests()
    {
        // 使用当前测试项目作为测试目标
        _testProjectPath = "src/Tests/EnterpriseAutomationFramework.Tests.csproj";
    }

    public Task InitializeAsync()
    {
        // 创建日志记录器
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        _strategyLogger = loggerFactory.CreateLogger<TestExecutionStrategy>();
        _managerLogger = loggerFactory.CreateLogger<TestExecutionManager>();

        // 创建默认策略和执行管理器
        var defaultStrategy = TestExecutionStrategy.CreateDefault(_strategyLogger);
        _executionManager = new TestExecutionManager(defaultStrategy, _managerLogger);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 测试仅 UI 测试执行策略
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "UI")]
    public async Task ExecuteUITestsOnly_ShouldFilterAndExecuteOnlyUITests()
    {
        // Act
        var result = await _executionManager!.ExecuteUITestsOnlyAsync(_testProjectPath);

        // Assert
        result.Should().NotBeNull();
        result.Filter.Should().Contain("Type=UI");
        result.Command.Should().Contain("--filter");
        result.Command.Should().Contain("Type=UI");
        result.Command.Should().Contain("--max-parallel-threads 2"); // UI tests use limited parallelism
        
        // 验证执行元数据
        result.GetMetadata<string>("ExecutionName").Should().Be("UI Tests Only");
        result.StartTime.Should().BeBefore(result.EndTime);
        result.Duration.Should().BePositive();

        _managerLogger!.LogInformation("UI 测试执行结果: {Summary}", result.GetSummary());
    }

    /// <summary>
    /// 测试仅 API 测试执行策略
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "API")]
    public async Task ExecuteAPITestsOnly_ShouldFilterAndExecuteOnlyAPITests()
    {
        // Act
        var result = await _executionManager!.ExecuteAPITestsOnlyAsync(_testProjectPath);

        // Assert
        result.Should().NotBeNull();
        result.Filter.Should().Contain("Type=API");
        result.Command.Should().Contain("--filter");
        result.Command.Should().Contain("Type=API");
        result.Command.Should().Contain($"--max-parallel-threads {Environment.ProcessorCount}"); // API tests use full parallelism
        
        // 验证执行元数据
        result.GetMetadata<string>("ExecutionName").Should().Be("API Tests Only");
        result.StartTime.Should().BeBefore(result.EndTime);
        result.Duration.Should().BePositive();

        _managerLogger!.LogInformation("API 测试执行结果: {Summary}", result.GetSummary());
    }

    /// <summary>
    /// 测试混合测试执行策略
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Mixed")]
    public async Task ExecuteMixedTests_ShouldExecuteBothUIAndAPITests()
    {
        // Act
        var result = await _executionManager!.ExecuteMixedTestsAsync(_testProjectPath);

        // Assert
        result.Should().NotBeNull();
        result.Filter.Should().Contain("Type=UI|Type=API");
        result.Command.Should().Contain("--filter");
        result.Command.Should().Contain("Type=UI|Type=API");
        result.Command.Should().Contain($"--max-parallel-threads {Environment.ProcessorCount / 2}"); // Mixed tests use moderate parallelism
        
        // 验证执行元数据
        result.GetMetadata<string>("ExecutionName").Should().Be("Mixed Tests (UI + API)");
        result.StartTime.Should().BeBefore(result.EndTime);
        result.Duration.Should().BePositive();

        _managerLogger!.LogInformation("混合测试执行结果: {Summary}", result.GetSummary());
    }

    /// <summary>
    /// 测试集成测试执行策略
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Integration")]
    public async Task ExecuteIntegrationTests_ShouldExecuteIntegrationTestsSerially()
    {
        // Act
        var result = await _executionManager!.ExecuteIntegrationTestsAsync(_testProjectPath);

        // Assert
        result.Should().NotBeNull();
        result.Filter.Should().Contain("Type=Integration");
        result.Command.Should().Contain("--filter");
        result.Command.Should().Contain("Type=Integration");
        result.Command.Should().Contain("--parallel false"); // Integration tests run serially
        
        // 验证执行元数据
        result.GetMetadata<string>("ExecutionName").Should().Be("Integration Tests");
        result.StartTime.Should().BeBefore(result.EndTime);
        result.Duration.Should().BePositive();

        _managerLogger!.LogInformation("集成测试执行结果: {Summary}", result.GetSummary());
    }

    /// <summary>
    /// 测试快速测试执行策略
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Fast")]
    public async Task ExecuteFastTests_ShouldExecuteOnlyFastTests()
    {
        // Act
        var result = await _executionManager!.ExecuteFastTestsAsync(_testProjectPath);

        // Assert
        result.Should().NotBeNull();
        result.Filter.Should().Contain("Speed=Fast");
        result.Command.Should().Contain("--filter");
        result.Command.Should().Contain("Speed=Fast");
        
        // 验证执行元数据
        result.GetMetadata<string>("ExecutionName").Should().Be("Fast Tests");
        result.StartTime.Should().BeBefore(result.EndTime);
        result.Duration.Should().BePositive();

        _managerLogger!.LogInformation("快速测试执行结果: {Summary}", result.GetSummary());
    }

    /// <summary>
    /// 测试冒烟测试执行策略
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Smoke")]
    public async Task ExecuteSmokeTests_ShouldExecuteCriticalAndHighPriorityTests()
    {
        // Act
        var result = await _executionManager!.ExecuteSmokeTestsAsync(_testProjectPath);

        // Assert
        result.Should().NotBeNull();
        result.Filter.Should().Contain("Suite=Smoke");
        result.Filter.Should().Contain("Priority=Critical|Priority=High");
        result.Command.Should().Contain("--filter");
        result.Command.Should().Contain("--blame-crash"); // Stop on first failure for smoke tests
        
        // 验证执行元数据
        result.GetMetadata<string>("ExecutionName").Should().Be("Smoke Tests");
        result.StartTime.Should().BeBefore(result.EndTime);
        result.Duration.Should().BePositive();

        _managerLogger!.LogInformation("冒烟测试执行结果: {Summary}", result.GetSummary());
    }

    /// <summary>
    /// 测试回归测试执行策略
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Regression")]
    public async Task ExecuteRegressionTests_ShouldExecuteWithCodeCoverage()
    {
        // Act
        var result = await _executionManager!.ExecuteRegressionTestsAsync(_testProjectPath);

        // Assert
        result.Should().NotBeNull();
        result.Filter.Should().Contain("Suite=Regression");
        result.Command.Should().Contain("--filter");
        result.Command.Should().Contain("Suite=Regression");
        result.Command.Should().Contain("--collect:\"XPlat Code Coverage\""); // Code coverage collection
        
        // 验证执行元数据
        result.GetMetadata<string>("ExecutionName").Should().Be("Regression Tests");
        result.StartTime.Should().BeBefore(result.EndTime);
        result.Duration.Should().BePositive();

        _managerLogger!.LogInformation("回归测试执行结果: {Summary}", result.GetSummary());
    }

    /// <summary>
    /// 测试策略名称执行
    /// </summary>
    [Theory]
    [InlineData("UITestsOnly")]
    [InlineData("APITestsOnly")]
    [InlineData("MixedTests")]
    [InlineData("IntegrationTests")]
    [InlineData("FastTests")]
    [InlineData("SmokeTests")]
    [InlineData("RegressionTests")]
    [Trait("ExecutionType", "ByName")]
    public async Task ExecuteTestsByStrategyName_ShouldExecuteCorrectStrategy(string strategyName)
    {
        // Act
        var result = await _executionManager!.ExecuteTestsByStrategyNameAsync(strategyName, _testProjectPath);

        // Assert
        result.Should().NotBeNull();
        result.Command.Should().NotBeNullOrEmpty();
        result.StartTime.Should().BeBefore(result.EndTime);
        result.Duration.Should().BePositive();
        result.GetMetadata<string>("ExecutionName").Should().NotBeNullOrEmpty();

        _managerLogger!.LogInformation("策略 {StrategyName} 执行结果: {Summary}", strategyName, result.GetSummary());
    }

    /// <summary>
    /// 测试不支持的策略名称应该抛出异常
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Error")]
    public async Task ExecuteTestsByStrategyName_WithUnsupportedStrategy_ShouldThrowException()
    {
        // Act & Assert
        var action = async () => await _executionManager!.ExecuteTestsByStrategyNameAsync("UnsupportedStrategy", _testProjectPath);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("不支持的执行策略: UnsupportedStrategy*");
    }

    /// <summary>
    /// 测试获取支持的执行策略列表
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Info")]
    public void GetSupportedExecutionStrategies_ShouldReturnAllSupportedStrategies()
    {
        // Act
        var strategies = _executionManager!.GetSupportedExecutionStrategies();

        // Assert
        strategies.Should().NotBeEmpty();
        strategies.Should().Contain("UITestsOnly");
        strategies.Should().Contain("APITestsOnly");
        strategies.Should().Contain("MixedTests");
        strategies.Should().Contain("IntegrationTests");
        strategies.Should().Contain("FastTests");
        strategies.Should().Contain("SmokeTests");
        strategies.Should().Contain("RegressionTests");
        strategies.Should().HaveCount(7);

        _managerLogger!.LogInformation("支持的执行策略: {Strategies}", string.Join(", ", strategies));
    }

    /// <summary>
    /// 测试过滤器表达式生成
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Filter")]
    public void TestExecutionStrategy_ShouldGenerateCorrectFilterExpressions()
    {
        // Arrange & Act
        var uiStrategy = TestExecutionStrategy.CreateForUITests(_strategyLogger!);
        var apiStrategy = TestExecutionStrategy.CreateForAPITests(_strategyLogger!);
        var integrationStrategy = TestExecutionStrategy.CreateForIntegrationTests(_strategyLogger!);

        var uiFilter = uiStrategy.GenerateFilterExpression();
        var apiFilter = apiStrategy.GenerateFilterExpression();
        var integrationFilter = integrationStrategy.GenerateFilterExpression();

        // Assert
        uiFilter.Should().Contain("Type=UI");
        apiFilter.Should().Contain("Type=API");
        integrationFilter.Should().Contain("Type=Integration");

        _managerLogger!.LogInformation("UI 过滤器: {UIFilter}", uiFilter);
        _managerLogger!.LogInformation("API 过滤器: {APIFilter}", apiFilter);
        _managerLogger!.LogInformation("集成测试过滤器: {IntegrationFilter}", integrationFilter);
    }

    /// <summary>
    /// 测试执行命令生成
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Command")]
    public void TestExecutionStrategy_ShouldGenerateCorrectExecutionCommands()
    {
        // Arrange
        var uiStrategy = TestExecutionStrategy.CreateForUITests(_strategyLogger!);
        var apiStrategy = TestExecutionStrategy.CreateForAPITests(_strategyLogger!);

        // Act
        var uiCommand = uiStrategy.GenerateExecutionCommand(_testProjectPath);
        var apiCommand = apiStrategy.GenerateExecutionCommand(_testProjectPath);

        // Assert
        uiCommand.Should().StartWith("dotnet test");
        uiCommand.Should().Contain(_testProjectPath);
        uiCommand.Should().Contain("--filter");
        uiCommand.Should().Contain("Type=UI");
        uiCommand.Should().Contain("--max-parallel-threads 2");

        apiCommand.Should().StartWith("dotnet test");
        apiCommand.Should().Contain(_testProjectPath);
        apiCommand.Should().Contain("--filter");
        apiCommand.Should().Contain("Type=API");
        apiCommand.Should().Contain($"--max-parallel-threads {Environment.ProcessorCount}");

        _managerLogger!.LogInformation("UI 执行命令: {UICommand}", uiCommand);
        _managerLogger!.LogInformation("API 执行命令: {APICommand}", apiCommand);
    }

    /// <summary>
    /// 测试设置验证
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Validation")]
    public void TestExecutionStrategy_ShouldValidateSettingsCorrectly()
    {
        // Arrange
        var validStrategy = TestExecutionStrategy.CreateForUITests(_strategyLogger!);
        
        var invalidSettings = new TestExecutionSettings
        {
            TestTimeout = -1, // Invalid timeout
            MaxParallelism = 0 // Invalid parallelism
        };
        var invalidStrategy = new TestExecutionStrategy(invalidSettings, _strategyLogger!);

        // Act & Assert
        validStrategy.ValidateSettings().Should().BeTrue();
        invalidStrategy.ValidateSettings().Should().BeFalse();

        _managerLogger!.LogInformation("有效策略验证: {Valid}", validStrategy.ValidateSettings());
        _managerLogger!.LogInformation("无效策略验证: {Invalid}", invalidStrategy.ValidateSettings());
    }

    /// <summary>
    /// 测试设置描述生成
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Description")]
    public void TestExecutionStrategy_ShouldGenerateCorrectSettingsDescription()
    {
        // Arrange
        var strategy = TestExecutionStrategy.CreateForUITests(_strategyLogger!);

        // Act
        var description = strategy.GetSettingsDescription();

        // Assert
        description.Should().NotBeNullOrEmpty();
        description.Should().Contain("测试类型: UI");
        description.Should().Contain("并行执行: True");
        description.Should().Contain("最大并行度: 2");
        description.Should().Contain("测试超时: 600000ms");

        _managerLogger!.LogInformation("UI 策略设置描述:\n{Description}", description);
    }

    /// <summary>
    /// 测试执行结果统计计算
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Statistics")]
    public void TestExecutionResult_ShouldCalculateStatisticsCorrectly()
    {
        // Arrange
        var result = new TestExecutionResult
        {
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(5)
        };

        // Act
        result.AddTestResult(new TestCaseResult { TestName = "Test1", Passed = true, Duration = TimeSpan.FromSeconds(1) });
        result.AddTestResult(new TestCaseResult { TestName = "Test2", Passed = false, Duration = TimeSpan.FromSeconds(2) });
        result.AddTestResult(new TestCaseResult { TestName = "Test3", Passed = true, Duration = TimeSpan.FromSeconds(1) });
        result.AddTestResult(new TestCaseResult { TestName = "Test4", Skipped = true });

        // Assert
        result.TotalTests.Should().Be(4);
        result.PassedTests.Should().Be(2);
        result.FailedTests.Should().Be(1);
        result.SkippedTests.Should().Be(1);
        result.PassRate.Should().Be(50.0);
        result.FailureRate.Should().Be(25.0);

        var summary = result.GetSummary();
        summary.Should().Contain("总计: 4");
        summary.Should().Contain("通过: 2");
        summary.Should().Contain("失败: 1");
        summary.Should().Contain("跳过: 1");
        summary.Should().Contain("通过率: 50.0%");

        _managerLogger!.LogInformation("测试结果统计: {Summary}", summary);
    }

    /// <summary>
    /// 测试执行结果详细报告生成
    /// </summary>
    [Fact]
    [Trait("ExecutionType", "Report")]
    public void TestExecutionResult_ShouldGenerateDetailedReport()
    {
        // Arrange
        var result = new TestExecutionResult
        {
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(5),
            Success = true,
            Command = "dotnet test --filter Type=UI",
            Filter = "Type=UI"
        };

        result.AddTestResult(new TestCaseResult 
        { 
            TestName = "UITest1", 
            Passed = true, 
            Duration = TimeSpan.FromSeconds(2),
            TestClass = "HomePageTests",
            TestMethod = "SearchFunctionality_ShouldWork"
        });

        result.AddTestResult(new TestCaseResult 
        { 
            TestName = "UITest2", 
            Passed = false, 
            Duration = TimeSpan.FromSeconds(3),
            ErrorMessage = "Element not found",
            TestClass = "HomePageTests",
            TestMethod = "Navigation_ShouldWork"
        });

        result.AddOutputFile("TestResults.trx");
        result.AddMetadata("Environment", "Test");
        result.AddMetadata("Browser", "Chrome");

        // Act
        var detailedReport = result.GetDetailedReport();

        // Assert
        detailedReport.Should().NotBeNullOrEmpty();
        detailedReport.Should().Contain("=== 测试执行报告 ===");
        detailedReport.Should().Contain("=== 测试统计 ===");
        detailedReport.Should().Contain("=== 过滤器 ===");
        detailedReport.Should().Contain("=== 执行命令 ===");
        detailedReport.Should().Contain("=== 输出文件 ===");
        detailedReport.Should().Contain("=== 测试结果详情 ===");
        detailedReport.Should().Contain("UITest2: Element not found");

        _managerLogger!.LogInformation("详细测试报告:\n{Report}", detailedReport);
    }
}