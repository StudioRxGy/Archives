using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.Services.Notifications;

namespace CsPlaywrightXun.src.playwright.Core.Utilities;

/// <summary>
/// 测试执行管理器
/// 负责实际执行测试并管理测试生命周期
/// </summary>
public class TestExecutionManager
{
    private readonly ILogger<TestExecutionManager> _logger;
    private readonly INotificationEventBus? _eventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="eventBus">通知事件总线（可选）</param>
    public TestExecutionManager(ILogger<TestExecutionManager> logger, INotificationEventBus? eventBus = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus;
    }

    /// <summary>
    /// 构造函数（向后兼容）
    /// </summary>
    /// <param name="strategy">测试执行策略</param>
    /// <param name="logger">日志记录器</param>
    [Obsolete("Use constructor with INotificationEventBus parameter instead")]
    public TestExecutionManager(TestExecutionStrategy strategy, ILogger<TestExecutionManager> logger)
        : this(logger, null)
    {
    }

    /// <summary>
    /// 执行仅 UI 测试
    /// </summary>
    /// <param name="projectPath">项目路径</param>
    /// <returns>执行结果</returns>
    public async Task<TestExecutionResult> ExecuteUITestsOnlyAsync(string? projectPath = null)
    {
        _logger.LogInformation("开始执行仅 UI 测试");
        
        var strategyLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<TestExecutionStrategy>();
        var uiStrategy = TestExecutionStrategy.CreateForUITests(strategyLogger);
        return await ExecuteTestsWithStrategyAsync(uiStrategy, projectPath, "UI Tests Only");
    }

    /// <summary>
    /// 执行仅 API 测试
    /// </summary>
    /// <param name="projectPath">项目路径</param>
    /// <returns>执行结果</returns>
    public async Task<TestExecutionResult> ExecuteAPITestsOnlyAsync(string? projectPath = null)
    {
        _logger.LogInformation("开始执行仅 API 测试");
        
        var strategyLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<TestExecutionStrategy>();
        var apiStrategy = TestExecutionStrategy.CreateForAPITests(strategyLogger);
        return await ExecuteTestsWithStrategyAsync(apiStrategy, projectPath, "API Tests Only");
    }

    /// <summary>
    /// 执行混合测试（UI + API）
    /// </summary>
    /// <param name="projectPath">项目路径</param>
    /// <returns>执行结果</returns>
    public async Task<TestExecutionResult> ExecuteMixedTestsAsync(string? projectPath = null)
    {
        _logger.LogInformation("开始执行混合测试（UI + API）");
        
        var strategyLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<TestExecutionStrategy>();
        var mixedSettings = TestExecutionSettings.CreateForMixedTests();
        var mixedStrategy = new TestExecutionStrategy(mixedSettings, strategyLogger);
        return await ExecuteTestsWithStrategyAsync(mixedStrategy, projectPath, "Mixed Tests (UI + API)");
    }

    /// <summary>
    /// 执行集成测试
    /// </summary>
    /// <param name="projectPath">项目路径</param>
    /// <returns>执行结果</returns>
    public async Task<TestExecutionResult> ExecuteIntegrationTestsAsync(string? projectPath = null)
    {
        _logger.LogInformation("开始执行集成测试");
        
        var strategyLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<TestExecutionStrategy>();
        var integrationStrategy = TestExecutionStrategy.CreateForIntegrationTests(strategyLogger);
        return await ExecuteTestsWithStrategyAsync(integrationStrategy, projectPath, "Integration Tests");
    }

    /// <summary>
    /// 执行快速测试
    /// </summary>
    /// <param name="projectPath">项目路径</param>
    /// <returns>执行结果</returns>
    public async Task<TestExecutionResult> ExecuteFastTestsAsync(string? projectPath = null)
    {
        _logger.LogInformation("开始执行快速测试");
        
        var strategyLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<TestExecutionStrategy>();
        var fastSettings = TestExecutionSettings.CreateForFastTests();
        var fastStrategy = new TestExecutionStrategy(fastSettings, strategyLogger);
        return await ExecuteTestsWithStrategyAsync(fastStrategy, projectPath, "Fast Tests");
    }

    /// <summary>
    /// 执行冒烟测试
    /// </summary>
    /// <param name="projectPath">项目路径</param>
    /// <returns>执行结果</returns>
    public async Task<TestExecutionResult> ExecuteSmokeTestsAsync(string? projectPath = null)
    {
        _logger.LogInformation("开始执行冒烟测试");
        
        var strategyLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<TestExecutionStrategy>();
        var smokeSettings = TestExecutionSettings.CreateForSmokeTests();
        var smokeStrategy = new TestExecutionStrategy(smokeSettings, strategyLogger);
        return await ExecuteTestsWithStrategyAsync(smokeStrategy, projectPath, "Smoke Tests");
    }

    /// <summary>
    /// 执行回归测试
    /// </summary>
    /// <param name="projectPath">项目路径</param>
    /// <returns>执行结果</returns>
    public async Task<TestExecutionResult> ExecuteRegressionTestsAsync(string? projectPath = null)
    {
        _logger.LogInformation("开始执行回归测试");
        
        var strategyLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<TestExecutionStrategy>();
        var regressionSettings = TestExecutionSettings.CreateForRegressionTests();
        var regressionStrategy = new TestExecutionStrategy(regressionSettings, strategyLogger);
        return await ExecuteTestsWithStrategyAsync(regressionStrategy, projectPath, "Regression Tests");
    }

    /// <summary>
    /// 使用指定策略执行测试
    /// </summary>
    /// <param name="strategy">测试执行策略</param>
    /// <param name="projectPath">项目路径</param>
    /// <param name="executionName">执行名称</param>
    /// <returns>执行结果</returns>
    private async Task<TestExecutionResult> ExecuteTestsWithStrategyAsync(
        TestExecutionStrategy strategy, 
        string? projectPath, 
        string executionName)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("开始执行测试: {ExecutionName}", executionName);

        // Publish test started event
        if (_eventBus != null)
        {
            try
            {
                var testStartedEvent = new TestStartedEvent
                {
                    TestSuiteName = executionName,
                    StartTime = startTime,
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                    Metadata = new Dictionary<string, object>
                    {
                        { "ProjectPath", projectPath ?? "Default" },
                        { "Strategy", strategy.GetType().Name }
                    }
                };
                await _eventBus.PublishAsync(testStartedEvent);
                _logger.LogDebug("Published TestStartedEvent for {ExecutionName}", executionName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish TestStartedEvent for {ExecutionName}", executionName);
            }
        }

        try
        {
            // 验证策略设置
            if (!strategy.ValidateSettings())
            {
                throw new InvalidOperationException("测试执行策略设置无效");
            }

            var command = strategy.GenerateExecutionCommand(projectPath);
            var filter = strategy.GenerateFilterExpression();

            _logger.LogInformation("执行命令: {Command}", command);
            _logger.LogInformation("过滤器: {Filter}", filter);

            // 实际执行测试命令
            var processResult = await ExecuteCommandAsync(command);

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            var result = new TestExecutionResult
            {
                StartTime = startTime,
                EndTime = endTime,
                Duration = duration,
                Success = processResult.ExitCode == 0,
                Command = command,
                Filter = filter,
                ErrorMessage = processResult.ExitCode != 0 ? processResult.StandardError : null
            };

            // 解析测试结果
            ParseTestResults(processResult.StandardOutput, result);

            result.AddMetadata("ExecutionName", executionName);
            result.AddMetadata("ExitCode", processResult.ExitCode);
            result.AddMetadata("StandardOutput", processResult.StandardOutput);
            result.AddMetadata("StandardError", processResult.StandardError);

            _logger.LogInformation("测试执行完成: {ExecutionName}, 结果: {Summary}", 
                executionName, result.GetSummary());

            // Publish test completed event
            if (_eventBus != null)
            {
                try
                {
                    var testSuiteResult = ConvertToTestSuiteResult(result, executionName);
                    var testCompletedEvent = new TestCompletedEvent
                    {
                        Result = testSuiteResult,
                        IsSuccess = result.Success,
                        CompletedAt = endTime
                    };
                    await _eventBus.PublishAsync(testCompletedEvent);
                    _logger.LogDebug("Published TestCompletedEvent for {ExecutionName}", executionName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish TestCompletedEvent for {ExecutionName}", executionName);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            _logger.LogError(ex, "测试执行失败: {ExecutionName}", executionName);

            var errorResult = new TestExecutionResult
            {
                StartTime = startTime,
                EndTime = endTime,
                Duration = duration,
                Success = false,
                ErrorMessage = ex.Message,
                Command = strategy.GenerateExecutionCommand(projectPath),
                Filter = strategy.GenerateFilterExpression()
            };

            // Publish test failure event
            if (_eventBus != null)
            {
                try
                {
                    var testSuiteResult = ConvertToTestSuiteResult(errorResult, executionName);
                    var testCompletedEvent = new TestCompletedEvent
                    {
                        Result = testSuiteResult,
                        IsSuccess = false,
                        CompletedAt = endTime
                    };
                    await _eventBus.PublishAsync(testCompletedEvent);
                    _logger.LogDebug("Published TestCompletedEvent (failure) for {ExecutionName}", executionName);
                }
                catch (Exception eventEx)
                {
                    _logger.LogWarning(eventEx, "Failed to publish TestCompletedEvent for {ExecutionName}", executionName);
                }
            }

            return errorResult;
        }
    }

    /// <summary>
    /// 将 TestExecutionResult 转换为 TestSuiteResult
    /// </summary>
    /// <param name="result">测试执行结果</param>
    /// <param name="suiteName">测试套件名称</param>
    /// <returns>测试套件结果</returns>
    private TestSuiteResult ConvertToTestSuiteResult(TestExecutionResult result, string suiteName)
    {
        return new TestSuiteResult
        {
            TestSuiteName = suiteName,
            StartTime = result.StartTime,
            EndTime = result.EndTime,
            TotalTests = result.TotalTests,
            PassedTests = result.PassedTests,
            FailedTests = result.FailedTests,
            SkippedTests = result.SkippedTests,
            FailedTestCases = result.TestResults
                .Where(t => !t.Passed && !t.Skipped)
                .Select(t => new TestCaseResult
                {
                    TestName = t.TestName,
                    ErrorMessage = t.ErrorMessage ?? string.Empty,
                    StackTrace = t.StackTrace ?? string.Empty,
                    Duration = t.Duration,
                    Category = t.Category ?? string.Empty,
                    IsCritical = t.IsCritical
                })
                .ToList(),
            Environment = result.Metadata.ContainsKey("Environment") 
                ? result.Metadata["Environment"]?.ToString() ?? string.Empty 
                : string.Empty,
            Metadata = result.Metadata
        };
    }

    /// <summary>
    /// 执行命令行命令
    /// </summary>
    /// <param name="command">命令</param>
    /// <returns>执行结果</returns>
    private async Task<ProcessResult> ExecuteCommandAsync(string command)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
                _logger.LogDebug("测试输出: {Output}", e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
                _logger.LogWarning("测试错误: {Error}", e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = outputBuilder.ToString(),
            StandardError = errorBuilder.ToString()
        };
    }

    /// <summary>
    /// 解析测试结果
    /// </summary>
    /// <param name="output">测试输出</param>
    /// <param name="result">测试执行结果</param>
    private void ParseTestResults(string output, TestExecutionResult result)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return;
        }

        try
        {
            // 解析 dotnet test 输出格式
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // 查找测试统计信息
                if (trimmedLine.Contains("Total tests:") || trimmedLine.Contains("总测试数:"))
                {
                    ParseTestStatistics(trimmedLine, result);
                }
                else if (trimmedLine.Contains("Passed:") || trimmedLine.Contains("通过:"))
                {
                    ParsePassedTests(trimmedLine, result);
                }
                else if (trimmedLine.Contains("Failed:") || trimmedLine.Contains("失败:"))
                {
                    ParseFailedTests(trimmedLine, result);
                }
                else if (trimmedLine.Contains("Skipped:") || trimmedLine.Contains("跳过:"))
                {
                    ParseSkippedTests(trimmedLine, result);
                }
                else if (trimmedLine.Contains("[PASS]") || trimmedLine.Contains("[FAIL]"))
                {
                    ParseIndividualTestResult(trimmedLine, result);
                }
            }

            // 如果没有解析到统计信息，设置默认值
            if (result.TotalTests == 0)
            {
                result.TotalTests = result.TestResults.Count;
                result.PassedTests = result.TestResults.Count(t => t.Passed);
                result.FailedTests = result.TestResults.Count(t => !t.Passed && !t.Skipped);
                result.SkippedTests = result.TestResults.Count(t => t.Skipped);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析测试结果时出错");
        }
    }

    /// <summary>
    /// 解析测试统计信息
    /// </summary>
    /// <param name="line">输出行</param>
    /// <param name="result">测试执行结果</param>
    private void ParseTestStatistics(string line, TestExecutionResult result)
    {
        // 示例: "Total tests: 10. Passed: 8. Failed: 1. Skipped: 1."
        var parts = line.Split(new[] { '.', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (int.TryParse(parts[i + 1], out var number))
            {
                if (parts[i].Contains("Total") || parts[i].Contains("总"))
                {
                    result.TotalTests = number;
                }
                else if (parts[i].Contains("Passed") || parts[i].Contains("通过"))
                {
                    result.PassedTests = number;
                }
                else if (parts[i].Contains("Failed") || parts[i].Contains("失败"))
                {
                    result.FailedTests = number;
                }
                else if (parts[i].Contains("Skipped") || parts[i].Contains("跳过"))
                {
                    result.SkippedTests = number;
                }
            }
        }
    }

    /// <summary>
    /// 解析通过的测试
    /// </summary>
    /// <param name="line">输出行</param>
    /// <param name="result">测试执行结果</param>
    private void ParsePassedTests(string line, TestExecutionResult result)
    {
        var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)");
        if (match.Success && int.TryParse(match.Value, out var count))
        {
            result.PassedTests = count;
        }
    }

    /// <summary>
    /// 解析失败的测试
    /// </summary>
    /// <param name="line">输出行</param>
    /// <param name="result">测试执行结果</param>
    private void ParseFailedTests(string line, TestExecutionResult result)
    {
        var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)");
        if (match.Success && int.TryParse(match.Value, out var count))
        {
            result.FailedTests = count;
        }
    }

    /// <summary>
    /// 解析跳过的测试
    /// </summary>
    /// <param name="line">输出行</param>
    /// <param name="result">测试执行结果</param>
    private void ParseSkippedTests(string line, TestExecutionResult result)
    {
        var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)");
        if (match.Success && int.TryParse(match.Value, out var count))
        {
            result.SkippedTests = count;
        }
    }

    /// <summary>
    /// 解析单个测试结果
    /// </summary>
    /// <param name="line">输出行</param>
    /// <param name="result">测试执行结果</param>
    private void ParseIndividualTestResult(string line, TestExecutionResult result)
    {
        var testResult = new TestCaseResult();
        
        if (line.Contains("[PASS]"))
        {
            testResult.Passed = true;
            testResult.TestName = ExtractTestName(line, "[PASS]");
        }
        else if (line.Contains("[FAIL]"))
        {
            testResult.Passed = false;
            testResult.TestName = ExtractTestName(line, "[FAIL]");
        }

        if (!string.IsNullOrEmpty(testResult.TestName))
        {
            result.AddTestResult(testResult);
        }
    }

    /// <summary>
    /// 提取测试名称
    /// </summary>
    /// <param name="line">输出行</param>
    /// <param name="marker">标记</param>
    /// <returns>测试名称</returns>
    private string ExtractTestName(string line, string marker)
    {
        var index = line.IndexOf(marker);
        if (index >= 0)
        {
            var remaining = line.Substring(index + marker.Length).Trim();
            return remaining.Split(' ')[0];
        }
        return string.Empty;
    }

    /// <summary>
    /// 获取支持的执行策略列表
    /// </summary>
    /// <returns>执行策略列表</returns>
    public List<string> GetSupportedExecutionStrategies()
    {
        return new List<string>
        {
            "UITestsOnly",
            "APITestsOnly", 
            "MixedTests",
            "IntegrationTests",
            "FastTests",
            "SmokeTests",
            "RegressionTests"
        };
    }

    /// <summary>
    /// 根据策略名称执行测试
    /// </summary>
    /// <param name="strategyName">策略名称</param>
    /// <param name="projectPath">项目路径</param>
    /// <returns>执行结果</returns>
    public async Task<TestExecutionResult> ExecuteTestsByStrategyNameAsync(string strategyName, string? projectPath = null)
    {
        return strategyName.ToLowerInvariant() switch
        {
            "uitestsonly" => await ExecuteUITestsOnlyAsync(projectPath),
            "apitestsonly" => await ExecuteAPITestsOnlyAsync(projectPath),
            "mixedtests" => await ExecuteMixedTestsAsync(projectPath),
            "integrationtests" => await ExecuteIntegrationTestsAsync(projectPath),
            "fasttests" => await ExecuteFastTestsAsync(projectPath),
            "smoketests" => await ExecuteSmokeTestsAsync(projectPath),
            "regressiontests" => await ExecuteRegressionTestsAsync(projectPath),
            _ => throw new ArgumentException($"不支持的执行策略: {strategyName}", nameof(strategyName))
        };
    }
}

/// <summary>
/// 进程执行结果
/// </summary>
internal class ProcessResult
{
    /// <summary>
    /// 退出代码
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// 标准输出
    /// </summary>
    public string StandardOutput { get; set; } = string.Empty;

    /// <summary>
    /// 标准错误
    /// </summary>
    public string StandardError { get; set; } = string.Empty;
}