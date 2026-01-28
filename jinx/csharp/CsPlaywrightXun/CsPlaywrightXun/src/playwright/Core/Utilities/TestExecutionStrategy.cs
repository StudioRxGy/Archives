using System.Text;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Attributes;
using CsPlaywrightXun.src.playwright.Core.Configuration;

namespace CsPlaywrightXun.src.playwright.Core.Utilities;

/// <summary>
/// 测试执行策略管理器
/// 负责根据配置生成测试过滤器和执行命令
/// </summary>
public class TestExecutionStrategy
{
    private readonly TestExecutionSettings _settings;
    private readonly ILogger<TestExecutionStrategy> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="settings">测试执行设置</param>
    /// <param name="logger">日志记录器</param>
    public TestExecutionStrategy(TestExecutionSettings settings, ILogger<TestExecutionStrategy> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 生成测试过滤器表达式
    /// </summary>
    /// <returns>过滤器表达式</returns>
    public string GenerateFilterExpression()
    {
        var includeFilters = new List<string>();
        var excludeFilters = new List<string>();

        // 添加包含过滤器
        if (_settings.TestTypes.Any())
        {
            includeFilters.Add(TestFilter.ByTypes(_settings.TestTypes.ToArray()));
        }

        if (_settings.TestCategories.Any())
        {
            includeFilters.Add(TestFilter.ByCategories(_settings.TestCategories.ToArray()));
        }

        if (_settings.TestPriorities.Any())
        {
            var priorityFilters = _settings.TestPriorities.Select(p => TestFilter.ByPriority(p));
            includeFilters.Add(TestFilter.Or(priorityFilters.ToArray()));
        }

        if (_settings.TestEnvironments.Any())
        {
            var envFilters = _settings.TestEnvironments.Select(e => TestFilter.ByEnvironment(e));
            includeFilters.Add(TestFilter.Or(envFilters.ToArray()));
        }

        if (_settings.TestTags.Any())
        {
            var tagFilters = _settings.TestTags.Select(t => TestFilter.ByTag(t));
            includeFilters.Add(TestFilter.Or(tagFilters.ToArray()));
        }

        if (_settings.TestSpeeds.Any())
        {
            var speedFilters = _settings.TestSpeeds.Select(s => TestFilter.BySpeed(s));
            includeFilters.Add(TestFilter.Or(speedFilters.ToArray()));
        }

        if (_settings.TestSuites.Any())
        {
            var suiteFilters = _settings.TestSuites.Select(s => TestFilter.BySuite(s));
            includeFilters.Add(TestFilter.Or(suiteFilters.ToArray()));
        }

        // 添加排除过滤器
        if (_settings.ExcludedTestTypes.Any())
        {
            excludeFilters.Add(TestFilter.Not(TestFilter.ByTypes(_settings.ExcludedTestTypes.ToArray())));
        }

        if (_settings.ExcludedTestCategories.Any())
        {
            excludeFilters.Add(TestFilter.Not(TestFilter.ByCategories(_settings.ExcludedTestCategories.ToArray())));
        }

        if (_settings.ExcludedTestTags.Any())
        {
            var excludedTagFilters = _settings.ExcludedTestTags.Select(t => TestFilter.Not(TestFilter.ByTag(t)));
            excludeFilters.AddRange(excludedTagFilters);
        }

        // 组合过滤器
        var allFilters = new List<string>();
        
        if (includeFilters.Any())
        {
            allFilters.Add(TestFilter.And(includeFilters.ToArray()));
        }

        if (excludeFilters.Any())
        {
            allFilters.AddRange(excludeFilters);
        }

        var finalFilter = allFilters.Any() ? TestFilter.And(allFilters.ToArray()) : string.Empty;

        _logger.LogInformation("生成的测试过滤器表达式: {Filter}", finalFilter);
        return finalFilter;
    }

    /// <summary>
    /// 生成测试执行命令
    /// </summary>
    /// <param name="projectPath">项目路径</param>
    /// <returns>测试执行命令</returns>
    public string GenerateExecutionCommand(string? projectPath = null)
    {
        var command = new StringBuilder("dotnet test");

        // 添加项目路径
        if (!string.IsNullOrWhiteSpace(projectPath))
        {
            command.Append($" \"{projectPath}\"");
        }

        // 添加过滤器
        var filter = GenerateFilterExpression();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            command.Append($" --filter \"{filter}\"");
        }

        // 添加并行执行设置
        if (_settings.ParallelExecution)
        {
            command.Append($" --parallel --max-parallel-threads {_settings.MaxParallelism}");
        }
        else
        {
            command.Append(" --parallel false");
        }

        // 添加输出设置
        if (_settings.VerboseOutput)
        {
            command.Append(" --verbosity normal");
        }
        else
        {
            command.Append(" --verbosity minimal");
        }

        // 添加日志记录器
        command.Append(" --logger console");

        // 添加测试结果输出
        if (!string.IsNullOrWhiteSpace(_settings.OutputPath))
        {
            foreach (var format in _settings.ReportFormats)
            {
                var outputFile = Path.ChangeExtension(_settings.OutputPath, format);
                command.Append($" --logger \"{format};LogFileName={outputFile}\"");
            }
        }

        // 添加代码覆盖率收集
        if (_settings.CollectCodeCoverage)
        {
            command.Append(" --collect:\"XPlat Code Coverage\"");
        }

        // 添加超时设置
        command.Append($" --blame-hang-timeout {_settings.TestTimeout}ms");

        // 添加失败时停止设置
        if (_settings.StopOnFirstFailure)
        {
            command.Append(" --blame-crash");
        }

        var finalCommand = command.ToString();
        _logger.LogInformation("生成的测试执行命令: {Command}", finalCommand);
        return finalCommand;
    }

    /// <summary>
    /// 执行测试
    /// </summary>
    /// <param name="projectPath">项目路径</param>
    /// <returns>执行结果</returns>
    public async Task<TestExecutionResult> ExecuteTestsAsync(string? projectPath = null)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("开始执行测试，设置: {Settings}", GetSettingsDescription());

        try
        {
            var command = GenerateExecutionCommand(projectPath);
            
            // 这里应该实际执行命令，为了演示目的，我们模拟执行
            _logger.LogInformation("执行命令: {Command}", command);
            
            // 模拟测试执行时间
            await Task.Delay(1000);

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            var result = new TestExecutionResult
            {
                StartTime = startTime,
                EndTime = endTime,
                Duration = duration,
                Success = true,
                Command = command,
                Filter = GenerateFilterExpression(),
                TotalTests = 10, // 模拟数据
                PassedTests = 9,
                FailedTests = 1,
                SkippedTests = 0
            };

            _logger.LogInformation("测试执行完成，结果: {Result}", result.GetSummary());
            return result;
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            _logger.LogError(ex, "测试执行失败");

            return new TestExecutionResult
            {
                StartTime = startTime,
                EndTime = endTime,
                Duration = duration,
                Success = false,
                ErrorMessage = ex.Message,
                Command = GenerateExecutionCommand(projectPath),
                Filter = GenerateFilterExpression()
            };
        }
    }

    /// <summary>
    /// 获取设置描述
    /// </summary>
    /// <returns>设置描述</returns>
    public string GetSettingsDescription()
    {
        var description = new StringBuilder();

        if (_settings.TestTypes.Any())
            description.AppendLine($"测试类型: {string.Join(", ", _settings.TestTypes)}");

        if (_settings.TestCategories.Any())
            description.AppendLine($"测试分类: {string.Join(", ", _settings.TestCategories)}");

        if (_settings.TestPriorities.Any())
            description.AppendLine($"测试优先级: {string.Join(", ", _settings.TestPriorities)}");

        if (_settings.TestTags.Any())
            description.AppendLine($"测试标签: {string.Join(", ", _settings.TestTags)}");

        if (_settings.TestSuites.Any())
            description.AppendLine($"测试套件: {string.Join(", ", _settings.TestSuites)}");

        description.AppendLine($"并行执行: {_settings.ParallelExecution}");
        description.AppendLine($"最大并行度: {_settings.MaxParallelism}");
        description.AppendLine($"测试超时: {_settings.TestTimeout}ms");

        return description.ToString().TrimEnd();
    }

    /// <summary>
    /// 验证设置
    /// </summary>
    /// <returns>验证结果</returns>
    public bool ValidateSettings()
    {
        var isValid = _settings.IsValid();
        if (!isValid)
        {
            var errors = _settings.GetValidationErrors();
            foreach (var error in errors)
            {
                _logger.LogError("设置验证错误: {Error}", error);
            }
        }
        return isValid;
    }

    /// <summary>
    /// 创建默认策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <returns>默认策略</returns>
    public static TestExecutionStrategy CreateDefault(ILogger<TestExecutionStrategy> logger)
    {
        return new TestExecutionStrategy(TestExecutionSettings.CreateDefault(), logger);
    }

    /// <summary>
    /// 创建 UI 测试策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <returns>UI 测试策略</returns>
    public static TestExecutionStrategy CreateForUITests(ILogger<TestExecutionStrategy> logger)
    {
        return new TestExecutionStrategy(TestExecutionSettings.CreateForUITests(), logger);
    }

    /// <summary>
    /// 创建 API 测试策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <returns>API 测试策略</returns>
    public static TestExecutionStrategy CreateForAPITests(ILogger<TestExecutionStrategy> logger)
    {
        return new TestExecutionStrategy(TestExecutionSettings.CreateForAPITests(), logger);
    }

    /// <summary>
    /// 创建集成测试策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <returns>集成测试策略</returns>
    public static TestExecutionStrategy CreateForIntegrationTests(ILogger<TestExecutionStrategy> logger)
    {
        return new TestExecutionStrategy(TestExecutionSettings.CreateForIntegrationTests(), logger);
    }
}