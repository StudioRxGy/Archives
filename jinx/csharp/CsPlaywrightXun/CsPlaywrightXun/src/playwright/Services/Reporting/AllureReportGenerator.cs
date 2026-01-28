using System.Text.Json;
using System.Text.Json.Serialization;
using Allure.Net.Commons;
using Allure.Net.Commons.Configuration;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Core.Models;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using Microsoft.Extensions.Logging;
using AllureTestResult = Allure.Net.Commons.TestResult;

namespace CsPlaywrightXun.src.playwright.Services.Reporting;

/// <summary>
/// Allure 报告生成器
/// </summary>
public class AllureReportGenerator : IReportGenerator
{
    private readonly ILogger<AllureReportGenerator> _logger;
    private readonly AllureLifecycle _allureLifecycle;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public AllureReportGenerator(ILogger<AllureReportGenerator> logger)
    {
        _logger = logger;
        _allureLifecycle = AllureLifecycle.Instance;
    }

    /// <summary>
    /// 生成Allure报告
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputPath">输出路径（可选，如果为空则使用默认路径）</param>
    /// <returns>生成的报告文件路径</returns>
    public async Task<string> GenerateReportAsync(TestReport testReport, string? outputPath = null)
    {
        try
        {
            _logger.LogInformation("开始生成Allure报告: {ReportName}", testReport.ReportName);

            // 使用PathConfiguration获取输出目录
            var outputDir = outputPath ?? PathConfiguration.GetOutputPath("allure-results", "reports");
            
            // 确保输出目录存在
            PathConfiguration.EnsureDirectoryExists(outputDir);

            // 设置Allure配置
            ConfigureAllure(outputDir);

            // 生成测试容器
            var containerUuid = Guid.NewGuid().ToString();
            var container = CreateTestContainer(testReport, containerUuid);
            
            // 写入容器信息
            await WriteAllureContainerAsync(container, outputDir);

            // 生成测试结果
            foreach (var testResult in testReport.Results)
            {
                var allureResult = await CreateAllureTestResultAsync(testResult, containerUuid);
                await WriteAllureTestResultAsync(allureResult, outputDir);
            }

            // 生成环境信息
            await WriteEnvironmentInfoAsync(testReport, outputDir);

            // 生成分类信息
            await WriteCategoriesAsync(outputDir);

            // 生成执行器信息
            await WriteExecutorInfoAsync(testReport, outputDir);

            _logger.LogInformation("Allure报告生成完成: {OutputPath}", outputDir);
            return outputDir;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成Allure报告失败: {ReportName}", testReport.ReportName);
            throw;
        }
    }

    /// <summary>
    /// 配置Allure
    /// </summary>
    /// <param name="outputDir">输出目录</param>
    private void ConfigureAllure(string outputDir)
    {
        // Allure配置通过环境变量或配置文件设置
        Environment.SetEnvironmentVariable("ALLURE_RESULTS_DIRECTORY", outputDir);
    }

    /// <summary>
    /// 创建测试容器
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="containerUuid">容器UUID</param>
    /// <returns>测试容器</returns>
    private TestResultContainer CreateTestContainer(TestReport testReport, string containerUuid)
    {
        return new TestResultContainer
        {
            uuid = containerUuid,
            name = testReport.ReportName,
            start = DateTimeOffset.FromUnixTimeMilliseconds(
                ((DateTimeOffset)testReport.TestStartTime).ToUnixTimeMilliseconds()).ToUnixTimeMilliseconds(),
            stop = DateTimeOffset.FromUnixTimeMilliseconds(
                ((DateTimeOffset)testReport.TestEndTime).ToUnixTimeMilliseconds()).ToUnixTimeMilliseconds(),
            children = testReport.Results.Select(r => Guid.NewGuid().ToString()).ToList()
        };
    }

    /// <summary>
    /// 创建Allure测试结果
    /// </summary>
    /// <param name="testResult">测试结果</param>
    /// <param name="containerUuid">容器UUID</param>
    /// <returns>Allure测试结果</returns>
    private async Task<AllureTestResult> CreateAllureTestResultAsync(Core.Models.TestResult testResult, string containerUuid)
    {
        var allureResult = new AllureTestResult
        {
            uuid = Guid.NewGuid().ToString(),
            name = testResult.TestName,
            fullName = $"{testResult.TestClass}.{testResult.TestMethod}",
            testCaseId = testResult.TestName,
            historyId = GenerateHistoryId(testResult),
            start = DateTimeOffset.FromUnixTimeMilliseconds(
                ((DateTimeOffset)testResult.StartTime).ToUnixTimeMilliseconds()).ToUnixTimeMilliseconds(),
            stop = DateTimeOffset.FromUnixTimeMilliseconds(
                ((DateTimeOffset)testResult.EndTime).ToUnixTimeMilliseconds()).ToUnixTimeMilliseconds(),
            status = MapTestStatus(testResult.Status),
            statusDetails = CreateStatusDetails(testResult),
            labels = CreateLabels(testResult),
            links = CreateLinks(testResult),
            parameters = CreateParameters(testResult),
            attachments = await CreateAttachmentsAsync(testResult),
            steps = new List<StepResult>()
        };

        return allureResult;
    }

    /// <summary>
    /// 映射测试状态
    /// </summary>
    /// <param name="status">框架测试状态</param>
    /// <returns>Allure状态</returns>
    private Status MapTestStatus(TestStatus status)
    {
        return status switch
        {
            TestStatus.Passed => Status.passed,
            TestStatus.Failed => Status.failed,
            TestStatus.Skipped => Status.skipped,
            TestStatus.Inconclusive => Status.broken,
            _ => Status.broken
        };
    }

    /// <summary>
    /// 创建状态详情
    /// </summary>
    /// <param name="testResult">测试结果</param>
    /// <returns>状态详情</returns>
    private StatusDetails? CreateStatusDetails(Core.Models.TestResult testResult)
    {
        if (testResult.Status == TestStatus.Failed && !string.IsNullOrEmpty(testResult.ErrorMessage))
        {
            return new StatusDetails
            {
                message = testResult.ErrorMessage,
                trace = testResult.StackTrace
            };
        }
        return null;
    }

    /// <summary>
    /// 创建标签
    /// </summary>
    /// <param name="testResult">测试结果</param>
    /// <returns>标签列表</returns>
    private List<Label> CreateLabels(Core.Models.TestResult testResult)
    {
        var labels = new List<Label>();

        // 添加基本标签
        if (!string.IsNullOrEmpty(testResult.TestClass))
        {
            labels.Add(new Label { name = "suite", value = testResult.TestClass });
        }

        if (!string.IsNullOrEmpty(testResult.TestMethod))
        {
            labels.Add(new Label { name = "subSuite", value = testResult.TestMethod });
        }

        // 添加分类标签
        foreach (var category in testResult.Categories)
        {
            labels.Add(new Label { name = "tag", value = category });
        }

        // 添加自定义标签
        foreach (var tag in testResult.Tags)
        {
            labels.Add(new Label { name = "tag", value = tag });
        }

        // 添加严重性标签
        var severity = DetermineSeverity(testResult);
        labels.Add(new Label { name = "severity", value = severity });

        // 添加框架标签
        labels.Add(new Label { name = "framework", value = "EnterpriseAutomationFramework" });

        return labels;
    }

    /// <summary>
    /// 确定测试严重性
    /// </summary>
    /// <param name="testResult">测试结果</param>
    /// <returns>严重性级别</returns>
    private string DetermineSeverity(Core.Models.TestResult testResult)
    {
        // 根据测试分类确定严重性
        if (testResult.Categories.Any(c => c.Contains("Critical", StringComparison.OrdinalIgnoreCase)))
            return "critical";
        if (testResult.Categories.Any(c => c.Contains("High", StringComparison.OrdinalIgnoreCase)))
            return "critical";
        if (testResult.Categories.Any(c => c.Contains("Medium", StringComparison.OrdinalIgnoreCase)))
            return "normal";
        if (testResult.Categories.Any(c => c.Contains("Low", StringComparison.OrdinalIgnoreCase)))
            return "minor";
        if (testResult.Categories.Any(c => c.Contains("UI", StringComparison.OrdinalIgnoreCase)))
            return "normal";
        if (testResult.Categories.Any(c => c.Contains("API", StringComparison.OrdinalIgnoreCase)))
            return "normal";

        return "normal";
    }

    /// <summary>
    /// 创建链接
    /// </summary>
    /// <param name="testResult">测试结果</param>
    /// <returns>链接列表</returns>
    private List<Link> CreateLinks(Core.Models.TestResult testResult)
    {
        var links = new List<Link>();

        // 从元数据中提取链接信息
        if (testResult.Metadata.TryGetValue("IssueUrl", out var issueUrl) && issueUrl is string issueUrlStr)
        {
            links.Add(new Link { name = "Issue", url = issueUrlStr, type = "issue" });
        }

        if (testResult.Metadata.TryGetValue("TestCaseUrl", out var testCaseUrl) && testCaseUrl is string testCaseUrlStr)
        {
            links.Add(new Link { name = "Test Case", url = testCaseUrlStr, type = "tms" });
        }

        return links;
    }

    /// <summary>
    /// 创建参数
    /// </summary>
    /// <param name="testResult">测试结果</param>
    /// <returns>参数列表</returns>
    private List<Parameter> CreateParameters(Core.Models.TestResult testResult)
    {
        var parameters = new List<Parameter>();

        foreach (var kvp in testResult.TestData)
        {
            parameters.Add(new Parameter
            {
                name = kvp.Key,
                value = kvp.Value?.ToString() ?? ""
            });
        }

        return parameters;
    }

    /// <summary>
    /// 创建附件
    /// </summary>
    /// <param name="testResult">测试结果</param>
    /// <returns>附件列表</returns>
    private async Task<List<Attachment>> CreateAttachmentsAsync(Core.Models.TestResult testResult)
    {
        var attachments = new List<Attachment>();

        foreach (var screenshot in testResult.Screenshots)
        {
            var fileName = Path.GetFileName(screenshot);
            var attachmentName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}";
            
            attachments.Add(new Attachment
            {
                name = "Screenshot",
                source = attachmentName,
                type = "image/png"
            });

            // 如果文件存在，复制截图到Allure结果目录
            // 这将在WriteAllureTestResultAsync中处理
        }

        // 添加测试输出作为附件
        if (!string.IsNullOrEmpty(testResult.Output))
        {
            attachments.Add(new Attachment
            {
                name = "Test Output",
                source = $"output_{testResult.TestName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                type = "text/plain"
            });
        }

        return attachments;
    }

    /// <summary>
    /// 生成历史ID
    /// </summary>
    /// <param name="testResult">测试结果</param>
    /// <returns>历史ID</returns>
    private string GenerateHistoryId(Core.Models.TestResult testResult)
    {
        var input = $"{testResult.TestClass}.{testResult.TestMethod}";
        return Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input))).ToLower();
    }

    /// <summary>
    /// 写入Allure容器
    /// </summary>
    /// <param name="container">测试容器</param>
    /// <param name="outputDir">输出目录</param>
    private async Task WriteAllureContainerAsync(TestResultContainer container, string outputDir)
    {
        var containerPath = Path.Combine(outputDir, $"{container.uuid}-container.json");
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        var json = JsonSerializer.Serialize(container, options);
        await File.WriteAllTextAsync(containerPath, json);
    }

    /// <summary>
    /// 写入Allure测试结果
    /// </summary>
    /// <param name="testResult">测试结果</param>
    /// <param name="outputDir">输出目录</param>
    private async Task WriteAllureTestResultAsync(AllureTestResult testResult, string outputDir)
    {
        var resultPath = Path.Combine(outputDir, $"{testResult.uuid}-result.json");
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        var json = JsonSerializer.Serialize(testResult, options);
        await File.WriteAllTextAsync(resultPath, json);

        // 复制附件
        foreach (var attachment in testResult.attachments)
        {
            await CopyAttachmentAsync(attachment, outputDir);
        }
    }

    /// <summary>
    /// 复制附件
    /// </summary>
    /// <param name="attachment">附件</param>
    /// <param name="outputDir">输出目录</param>
    private async Task CopyAttachmentAsync(Attachment attachment, string outputDir)
    {
        try
        {
            var sourcePath = attachment.source;
            var targetPath = Path.Combine(outputDir, attachment.source);

            if (File.Exists(sourcePath))
            {
                await using var sourceStream = File.OpenRead(sourcePath);
                await using var targetStream = File.Create(targetPath);
                await sourceStream.CopyToAsync(targetStream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "复制附件失败: {AttachmentSource}", attachment.source);
        }
    }

    /// <summary>
    /// 写入环境信息
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputDir">输出目录</param>
    private async Task WriteEnvironmentInfoAsync(TestReport testReport, string outputDir)
    {
        var environmentPath = Path.Combine(outputDir, "environment.properties");
        var lines = new List<string>
        {
            $"Environment={testReport.Environment}",
            $"Report.Name={testReport.ReportName}",
            $"Test.Start.Time={testReport.TestStartTime:yyyy-MM-dd HH:mm:ss}",
            $"Test.End.Time={testReport.TestEndTime:yyyy-MM-dd HH:mm:ss}",
            $"Total.Tests={testReport.Summary.TotalTests}",
            $"Passed.Tests={testReport.Summary.PassedTests}",
            $"Failed.Tests={testReport.Summary.FailedTests}",
            $"Skipped.Tests={testReport.Summary.SkippedTests}"
        };

        // 添加系统信息
        foreach (var kvp in testReport.SystemInfo)
        {
            lines.Add($"System.{kvp.Key}={kvp.Value}");
        }

        // 添加配置信息
        foreach (var kvp in testReport.Configuration)
        {
            lines.Add($"Config.{kvp.Key}={kvp.Value}");
        }

        await File.WriteAllLinesAsync(environmentPath, lines);
    }

    /// <summary>
    /// 写入分类信息
    /// </summary>
    /// <param name="outputDir">输出目录</param>
    private async Task WriteCategoriesAsync(string outputDir)
    {
        var categories = new object[]
        {
            new
            {
                name = "Product defects",
                matchedStatuses = new[] { "failed" },
                messageRegex = ".*AssertionError.*"
            },
            new
            {
                name = "Test defects",
                matchedStatuses = new[] { "broken" },
                messageRegex = ".*Exception.*"
            },
            new
            {
                name = "Ignored tests",
                matchedStatuses = new[] { "skipped" }
            }
        };

        var categoriesPath = Path.Combine(outputDir, "categories.json");
        var json = JsonSerializer.Serialize(categories, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(categoriesPath, json);
    }

    /// <summary>
    /// 写入执行器信息
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputDir">输出目录</param>
    private async Task WriteExecutorInfoAsync(TestReport testReport, string outputDir)
    {
        var executor = new
        {
            name = "Enterprise Automation Framework",
            type = "xunit",
            url = "",
            buildOrder = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            buildName = testReport.ReportName,
            buildUrl = "",
            reportName = testReport.ReportName,
            reportUrl = ""
        };

        var executorPath = Path.Combine(outputDir, "executor.json");
        var json = JsonSerializer.Serialize(executor, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(executorPath, json);
    }
}