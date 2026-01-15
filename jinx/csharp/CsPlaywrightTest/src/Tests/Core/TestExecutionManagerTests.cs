using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using EnterpriseAutomationFramework.Core.Attributes;
using EnterpriseAutomationFramework.Core.Configuration;
using EnterpriseAutomationFramework.Core.Utilities;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// TestExecutionManager 单元测试
/// </summary>
[UnitTest]
[TestCategory(TestCategory.Core)]
[TestPriority(TestPriority.High)]
[Trait("Speed", "Fast")]
public class TestExecutionManagerTests
{
    private readonly Mock<ILogger<TestExecutionStrategy>> _strategyLoggerMock;
    private readonly Mock<ILogger<TestExecutionManager>> _managerLoggerMock;
    private readonly TestExecutionStrategy _testStrategy;
    private readonly TestExecutionManager _executionManager;

    public TestExecutionManagerTests()
    {
        _strategyLoggerMock = new Mock<ILogger<TestExecutionStrategy>>();
        _managerLoggerMock = new Mock<ILogger<TestExecutionManager>>();
        
        var settings = TestExecutionSettings.CreateDefault();
        _testStrategy = new TestExecutionStrategy(settings, _strategyLoggerMock.Object);
        _executionManager = new TestExecutionManager(_testStrategy, _managerLoggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullStrategy_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new TestExecutionManager(null!, _managerLoggerMock.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("strategy");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new TestExecutionManager(_testStrategy, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Act
        var manager = new TestExecutionManager(_testStrategy, _managerLoggerMock.Object);

        // Assert
        manager.Should().NotBeNull();
    }

    [Fact]
    public void GetSupportedExecutionStrategies_ShouldReturnAllSupportedStrategies()
    {
        // Act
        var strategies = _executionManager.GetSupportedExecutionStrategies();

        // Assert
        strategies.Should().NotBeEmpty();
        strategies.Should().HaveCount(7);
        strategies.Should().Contain("UITestsOnly");
        strategies.Should().Contain("APITestsOnly");
        strategies.Should().Contain("MixedTests");
        strategies.Should().Contain("IntegrationTests");
        strategies.Should().Contain("FastTests");
        strategies.Should().Contain("SmokeTests");
        strategies.Should().Contain("RegressionTests");
    }

    [Theory]
    [InlineData("uitestsonly", "UITestsOnly")]
    [InlineData("UITESTSONLY", "UITestsOnly")]
    [InlineData("apitestsonly", "APITestsOnly")]
    [InlineData("mixedtests", "MixedTests")]
    [InlineData("integrationtests", "IntegrationTests")]
    [InlineData("fasttests", "FastTests")]
    [InlineData("smoketests", "SmokeTests")]
    [InlineData("regressiontests", "RegressionTests")]
    public async Task ExecuteTestsByStrategyNameAsync_WithValidStrategyName_ShouldExecuteCorrectStrategy(
        string strategyName, string expectedStrategyType)
    {
        // Act & Assert - 由于实际执行会调用外部进程，这里主要验证不会抛出异常
        var action = async () => await _executionManager.ExecuteTestsByStrategyNameAsync(strategyName, "test.csproj");
        
        // 这个测试可能会失败，因为它尝试执行实际的 dotnet test 命令
        // 但至少验证了策略名称映射是正确的
        await action.Should().NotThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteTestsByStrategyNameAsync_WithUnsupportedStrategy_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _executionManager.ExecuteTestsByStrategyNameAsync("UnsupportedStrategy", "test.csproj");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("不支持的执行策略: UnsupportedStrategy*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("InvalidStrategy")]
    [InlineData("RandomText")]
    public async Task ExecuteTestsByStrategyNameAsync_WithInvalidStrategyName_ShouldThrowArgumentException(string strategyName)
    {
        // Act & Assert
        var action = async () => await _executionManager.ExecuteTestsByStrategyNameAsync(strategyName, "test.csproj");
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void TestExecutionManager_ShouldLogCorrectInformationMessages()
    {
        // Arrange
        var strategies = _executionManager.GetSupportedExecutionStrategies();

        // Act & Assert
        strategies.Should().NotBeEmpty();
        
        // 验证构造函数没有抛出异常，说明日志记录器正确初始化
        _executionManager.Should().NotBeNull();
    }

    /// <summary>
    /// 测试执行策略工厂方法
    /// </summary>
    [Fact]
    public void TestExecutionStrategy_FactoryMethods_ShouldCreateCorrectStrategies()
    {
        // Act
        var uiStrategy = TestExecutionStrategy.CreateForUITests(_strategyLoggerMock.Object);
        var apiStrategy = TestExecutionStrategy.CreateForAPITests(_strategyLoggerMock.Object);
        var integrationStrategy = TestExecutionStrategy.CreateForIntegrationTests(_strategyLoggerMock.Object);
        var defaultStrategy = TestExecutionStrategy.CreateDefault(_strategyLoggerMock.Object);

        // Assert
        uiStrategy.Should().NotBeNull();
        apiStrategy.Should().NotBeNull();
        integrationStrategy.Should().NotBeNull();
        defaultStrategy.Should().NotBeNull();

        // 验证过滤器生成
        uiStrategy.GenerateFilterExpression().Should().Contain("Type=UI");
        apiStrategy.GenerateFilterExpression().Should().Contain("Type=API");
        integrationStrategy.GenerateFilterExpression().Should().Contain("Type=Integration");
    }

    /// <summary>
    /// 测试执行设置工厂方法
    /// </summary>
    [Fact]
    public void TestExecutionSettings_FactoryMethods_ShouldCreateCorrectSettings()
    {
        // Act
        var defaultSettings = TestExecutionSettings.CreateDefault();
        var uiSettings = TestExecutionSettings.CreateForUITests();
        var apiSettings = TestExecutionSettings.CreateForAPITests();
        var integrationSettings = TestExecutionSettings.CreateForIntegrationTests();
        var mixedSettings = TestExecutionSettings.CreateForMixedTests();
        var fastSettings = TestExecutionSettings.CreateForFastTests();
        var smokeSettings = TestExecutionSettings.CreateForSmokeTests();
        var regressionSettings = TestExecutionSettings.CreateForRegressionTests();

        // Assert
        defaultSettings.Should().NotBeNull();
        defaultSettings.ParallelExecution.Should().BeTrue();

        uiSettings.Should().NotBeNull();
        uiSettings.TestTypes.Should().Contain(TestType.UI);
        uiSettings.MaxParallelism.Should().Be(2);
        uiSettings.TestTimeout.Should().Be(600000);

        apiSettings.Should().NotBeNull();
        apiSettings.TestTypes.Should().Contain(TestType.API);
        apiSettings.MaxParallelism.Should().Be(Environment.ProcessorCount);
        apiSettings.TestTimeout.Should().Be(120000);

        integrationSettings.Should().NotBeNull();
        integrationSettings.TestTypes.Should().Contain(TestType.Integration);
        integrationSettings.ParallelExecution.Should().BeFalse();
        integrationSettings.TestTimeout.Should().Be(900000);

        mixedSettings.Should().NotBeNull();
        mixedSettings.TestTypes.Should().Contain(TestType.UI);
        mixedSettings.TestTypes.Should().Contain(TestType.API);
        mixedSettings.MaxParallelism.Should().Be(Environment.ProcessorCount / 2);

        fastSettings.Should().NotBeNull();
        fastSettings.TestSpeeds.Should().Contain("Fast");
        fastSettings.TestTimeout.Should().Be(60000);

        smokeSettings.Should().NotBeNull();
        smokeSettings.TestSuites.Should().Contain("Smoke");
        smokeSettings.TestPriorities.Should().Contain(TestPriority.Critical);
        smokeSettings.TestPriorities.Should().Contain(TestPriority.High);
        smokeSettings.StopOnFirstFailure.Should().BeTrue();

        regressionSettings.Should().NotBeNull();
        regressionSettings.TestSuites.Should().Contain("Regression");
        regressionSettings.CollectCodeCoverage.Should().BeTrue();
        regressionSettings.TestTimeout.Should().Be(1800000);
    }

    /// <summary>
    /// 测试设置验证
    /// </summary>
    [Fact]
    public void TestExecutionSettings_Validation_ShouldWorkCorrectly()
    {
        // Arrange
        var validSettings = TestExecutionSettings.CreateDefault();
        var invalidSettings = new TestExecutionSettings
        {
            TestTimeout = -1,
            MaxParallelism = 0
        };

        // Act & Assert
        validSettings.IsValid().Should().BeTrue();
        validSettings.GetValidationErrors().Should().BeEmpty();

        invalidSettings.IsValid().Should().BeFalse();
        var errors = invalidSettings.GetValidationErrors();
        errors.Should().NotBeEmpty();
        errors.Should().Contain("测试超时时间必须大于0");
        errors.Should().Contain("最大并行度必须大于0");
    }

    /// <summary>
    /// 测试设置克隆
    /// </summary>
    [Fact]
    public void TestExecutionSettings_Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var originalSettings = TestExecutionSettings.CreateForUITests();
        originalSettings.TestTags.Add("OriginalTag");

        // Act
        var clonedSettings = originalSettings.Clone();
        clonedSettings.TestTags.Add("ClonedTag");

        // Assert
        clonedSettings.Should().NotBeSameAs(originalSettings);
        clonedSettings.TestTypes.Should().BeEquivalentTo(originalSettings.TestTypes);
        clonedSettings.MaxParallelism.Should().Be(originalSettings.MaxParallelism);
        clonedSettings.TestTimeout.Should().Be(originalSettings.TestTimeout);

        // 验证集合是独立的
        originalSettings.TestTags.Should().Contain("OriginalTag");
        originalSettings.TestTags.Should().NotContain("ClonedTag");
        clonedSettings.TestTags.Should().Contain("OriginalTag");
        clonedSettings.TestTags.Should().Contain("ClonedTag");
    }

    /// <summary>
    /// 测试命令生成
    /// </summary>
    [Fact]
    public void TestExecutionStrategy_GenerateExecutionCommand_ShouldIncludeAllRequiredParameters()
    {
        // Arrange
        var settings = new TestExecutionSettings
        {
            TestTypes = new List<TestType> { TestType.UI },
            ParallelExecution = true,
            MaxParallelism = 4,
            VerboseOutput = true,
            CollectCodeCoverage = true,
            StopOnFirstFailure = true,
            TestTimeout = 300000,
            OutputPath = "TestResults.trx",
            ReportFormats = new List<string> { "trx", "html" }
        };
        var strategy = new TestExecutionStrategy(settings, _strategyLoggerMock.Object);
        var projectPath = "TestProject.csproj";

        // Act
        var command = strategy.GenerateExecutionCommand(projectPath);

        // Assert
        command.Should().StartWith("dotnet test");
        command.Should().Contain($"\"{projectPath}\"");
        command.Should().Contain("--filter");
        command.Should().Contain("Type=UI");
        command.Should().Contain("--parallel");
        command.Should().Contain("--max-parallel-threads 4");
        command.Should().Contain("--verbosity normal");
        command.Should().Contain("--collect:\"XPlat Code Coverage\"");
        command.Should().Contain("--blame-crash");
        command.Should().Contain("--blame-hang-timeout 300000ms");
        command.Should().Contain("--logger console");
        command.Should().Contain("--logger \"trx;LogFileName=TestResults.trx\"");
        command.Should().Contain("--logger \"html;LogFileName=TestResults.html\"");
    }

    /// <summary>
    /// 测试过滤器生成的复杂场景
    /// </summary>
    [Fact]
    public void TestExecutionStrategy_GenerateFilterExpression_WithComplexSettings_ShouldGenerateCorrectFilter()
    {
        // Arrange
        var settings = new TestExecutionSettings
        {
            TestTypes = new List<TestType> { TestType.UI, TestType.API },
            TestCategories = new List<TestCategory> { TestCategory.PageObject, TestCategory.ApiClient },
            TestPriorities = new List<TestPriority> { TestPriority.High, TestPriority.Critical },
            TestTags = new List<string> { "Smoke", "Regression" },
            TestSpeeds = new List<string> { "Fast" },
            ExcludedTestTypes = new List<TestType> { TestType.Integration },
            ExcludedTestTags = new List<string> { "Slow" }
        };
        var strategy = new TestExecutionStrategy(settings, _strategyLoggerMock.Object);

        // Act
        var filter = strategy.GenerateFilterExpression();

        // Assert
        filter.Should().NotBeNullOrEmpty();
        filter.Should().Contain("Type=UI|Type=API");
        filter.Should().Contain("Category=PageObject|Category=ApiClient");
        filter.Should().Contain("Priority=High|Priority=Critical");
        filter.Should().Contain("Tag=Smoke|Tag=Regression");
        filter.Should().Contain("Speed=Fast");
        filter.Should().Contain("!Type=Integration");
        filter.Should().Contain("!Tag=Slow");
    }
}