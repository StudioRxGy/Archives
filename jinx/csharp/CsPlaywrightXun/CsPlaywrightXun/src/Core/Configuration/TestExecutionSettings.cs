using CsPlaywrightXun.src.playwright.Core.Attributes;

namespace CsPlaywrightXun.src.playwright.Core.Configuration;

/// <summary>
/// 测试执行设置
/// </summary>
public class TestExecutionSettings
{
    /// <summary>
    /// 测试类型过滤器
    /// </summary>
    public List<TestType> TestTypes { get; set; } = new();

    /// <summary>
    /// 测试分类过滤器
    /// </summary>
    public List<TestCategory> TestCategories { get; set; } = new();

    /// <summary>
    /// 测试优先级过滤器
    /// </summary>
    public List<TestPriority> TestPriorities { get; set; } = new();

    /// <summary>
    /// 测试环境过滤器
    /// </summary>
    public List<string> TestEnvironments { get; set; } = new();

    /// <summary>
    /// 测试标签过滤器
    /// </summary>
    public List<string> TestTags { get; set; } = new();

    /// <summary>
    /// 测试速度过滤器
    /// </summary>
    public List<string> TestSpeeds { get; set; } = new();

    /// <summary>
    /// 测试套件过滤器
    /// </summary>
    public List<string> TestSuites { get; set; } = new();

    /// <summary>
    /// 排除的测试类型
    /// </summary>
    public List<TestType> ExcludedTestTypes { get; set; } = new();

    /// <summary>
    /// 排除的测试分类
    /// </summary>
    public List<TestCategory> ExcludedTestCategories { get; set; } = new();

    /// <summary>
    /// 排除的测试标签
    /// </summary>
    public List<string> ExcludedTestTags { get; set; } = new();

    /// <summary>
    /// 是否并行执行测试
    /// </summary>
    public bool ParallelExecution { get; set; } = true;

    /// <summary>
    /// 最大并行度
    /// </summary>
    public int MaxParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// 测试超时时间（毫秒）
    /// </summary>
    public int TestTimeout { get; set; } = 300000; // 5 minutes

    /// <summary>
    /// 是否在失败时停止执行
    /// </summary>
    public bool StopOnFirstFailure { get; set; } = false;

    /// <summary>
    /// 是否生成详细输出
    /// </summary>
    public bool VerboseOutput { get; set; } = false;

    /// <summary>
    /// 是否收集代码覆盖率
    /// </summary>
    public bool CollectCodeCoverage { get; set; } = false;

    /// <summary>
    /// 测试结果输出路径
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// 测试报告格式
    /// </summary>
    public List<string> ReportFormats { get; set; } = new() { "trx", "html" };

    /// <summary>
    /// 创建默认测试执行设置
    /// </summary>
    /// <returns>默认设置</returns>
    public static TestExecutionSettings CreateDefault()
    {
        return new TestExecutionSettings();
    }

    /// <summary>
    /// 创建仅 UI 测试的设置
    /// </summary>
    /// <returns>UI 测试设置</returns>
    public static TestExecutionSettings CreateForUITests()
    {
        return new TestExecutionSettings
        {
            TestTypes = new List<TestType> { TestType.UI },
            ParallelExecution = true,
            MaxParallelism = 2, // UI 测试通常需要较少的并行度
            TestTimeout = 600000 // 10 minutes for UI tests
        };
    }

    /// <summary>
    /// 创建仅 API 测试的设置
    /// </summary>
    /// <returns>API 测试设置</returns>
    public static TestExecutionSettings CreateForAPITests()
    {
        return new TestExecutionSettings
        {
            TestTypes = new List<TestType> { TestType.API },
            ParallelExecution = true,
            MaxParallelism = Environment.ProcessorCount,
            TestTimeout = 120000 // 2 minutes for API tests
        };
    }

    /// <summary>
    /// 创建仅集成测试的设置
    /// </summary>
    /// <returns>集成测试设置</returns>
    public static TestExecutionSettings CreateForIntegrationTests()
    {
        return new TestExecutionSettings
        {
            TestTypes = new List<TestType> { TestType.Integration },
            ParallelExecution = false, // 集成测试通常需要串行执行
            TestTimeout = 900000 // 15 minutes for integration tests
        };
    }

    /// <summary>
    /// 创建混合测试的设置
    /// </summary>
    /// <returns>混合测试设置</returns>
    public static TestExecutionSettings CreateForMixedTests()
    {
        return new TestExecutionSettings
        {
            TestTypes = new List<TestType> { TestType.UI, TestType.API },
            ParallelExecution = true,
            MaxParallelism = Environment.ProcessorCount / 2,
            TestTimeout = 600000 // 10 minutes
        };
    }

    /// <summary>
    /// 创建快速测试的设置
    /// </summary>
    /// <returns>快速测试设置</returns>
    public static TestExecutionSettings CreateForFastTests()
    {
        return new TestExecutionSettings
        {
            TestSpeeds = new List<string> { "Fast" },
            ParallelExecution = true,
            MaxParallelism = Environment.ProcessorCount,
            TestTimeout = 60000 // 1 minute
        };
    }

    /// <summary>
    /// 创建冒烟测试的设置
    /// </summary>
    /// <returns>冒烟测试设置</returns>
    public static TestExecutionSettings CreateForSmokeTests()
    {
        return new TestExecutionSettings
        {
            TestSuites = new List<string> { "Smoke" },
            TestPriorities = new List<TestPriority> { TestPriority.Critical, TestPriority.High },
            ParallelExecution = true,
            StopOnFirstFailure = true,
            TestTimeout = 300000 // 5 minutes
        };
    }

    /// <summary>
    /// 创建回归测试的设置
    /// </summary>
    /// <returns>回归测试设置</returns>
    public static TestExecutionSettings CreateForRegressionTests()
    {
        return new TestExecutionSettings
        {
            TestSuites = new List<string> { "Regression" },
            ParallelExecution = true,
            CollectCodeCoverage = true,
            TestTimeout = 1800000 // 30 minutes
        };
    }

    /// <summary>
    /// 验证设置是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        return TestTimeout > 0 &&
               MaxParallelism > 0 &&
               !string.IsNullOrWhiteSpace(OutputPath) == false || 
               (OutputPath != null && Directory.Exists(Path.GetDirectoryName(OutputPath)));
    }

    /// <summary>
    /// 获取验证错误列表
    /// </summary>
    /// <returns>验证错误列表</returns>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (TestTimeout <= 0)
            errors.Add("测试超时时间必须大于0");

        if (MaxParallelism <= 0)
            errors.Add("最大并行度必须大于0");

        if (!string.IsNullOrWhiteSpace(OutputPath))
        {
            var directory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                errors.Add($"输出路径的目录不存在: {directory}");
        }

        return errors;
    }

    /// <summary>
    /// 克隆设置
    /// </summary>
    /// <returns>克隆的设置</returns>
    public TestExecutionSettings Clone()
    {
        return new TestExecutionSettings
        {
            TestTypes = new List<TestType>(TestTypes),
            TestCategories = new List<TestCategory>(TestCategories),
            TestPriorities = new List<TestPriority>(TestPriorities),
            TestEnvironments = new List<string>(TestEnvironments),
            TestTags = new List<string>(TestTags),
            TestSpeeds = new List<string>(TestSpeeds),
            TestSuites = new List<string>(TestSuites),
            ExcludedTestTypes = new List<TestType>(ExcludedTestTypes),
            ExcludedTestCategories = new List<TestCategory>(ExcludedTestCategories),
            ExcludedTestTags = new List<string>(ExcludedTestTags),
            ParallelExecution = ParallelExecution,
            MaxParallelism = MaxParallelism,
            TestTimeout = TestTimeout,
            StopOnFirstFailure = StopOnFirstFailure,
            VerboseOutput = VerboseOutput,
            CollectCodeCoverage = CollectCodeCoverage,
            OutputPath = OutputPath,
            ReportFormats = new List<string>(ReportFormats)
        };
    }
}