using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Core.Models;
using Microsoft.Extensions.Logging;

namespace CsPlaywrightXun.src.playwright.Services.Reporting;

/// <summary>
/// 报告服务
/// </summary>
public class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;
    private readonly Dictionary<string, IReportGenerator> _generators;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="htmlGenerator">HTML报告生成器</param>
    /// <param name="allureGenerator">Allure报告生成器</param>
    public ReportService(ILogger<ReportService> logger, HtmlReportGenerator htmlGenerator, AllureReportGenerator allureGenerator)
    {
        _logger = logger;
        _generators = new Dictionary<string, IReportGenerator>(StringComparer.OrdinalIgnoreCase)
        {
            ["html"] = htmlGenerator,
            ["htm"] = htmlGenerator,
            ["allure"] = allureGenerator
        };
    }

    /// <summary>
    /// 生成报告
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputPath">输出路径</param>
    /// <param name="format">报告格式</param>
    /// <returns>生成的报告文件路径</returns>
    public async Task<string> GenerateReportAsync(TestReport testReport, string outputPath, string format = "html")
    {
        try
        {
            _logger.LogInformation("开始生成报告: {ReportName}, 格式: {Format}", testReport.ReportName, format);

            if (!_generators.TryGetValue(format, out var generator))
            {
                throw new NotSupportedException($"不支持的报告格式: {format}");
            }

            var reportPath = await generator.GenerateReportAsync(testReport, outputPath);

            _logger.LogInformation("报告生成完成: {ReportPath}", reportPath);
            return reportPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成报告失败: {ReportName}", testReport.ReportName);
            throw;
        }
    }

    /// <summary>
    /// 生成多格式报告
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="formats">报告格式列表</param>
    /// <returns>生成的报告文件路径列表</returns>
    public async Task<List<string>> GenerateMultipleReportsAsync(TestReport testReport, string outputDirectory, params string[] formats)
    {
        var reportPaths = new List<string>();

        foreach (var format in formats)
        {
            try
            {
                var fileName = $"{testReport.ReportName}_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";
                var outputPath = Path.Combine(outputDirectory, fileName);
                
                var reportPath = await GenerateReportAsync(testReport, outputPath, format);
                reportPaths.Add(reportPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成 {Format} 格式报告失败", format);
            }
        }

        return reportPaths;
    }

    /// <summary>
    /// 获取支持的报告格式
    /// </summary>
    /// <returns>支持的格式列表</returns>
    public List<string> GetSupportedFormats()
    {
        return _generators.Keys.ToList();
    }

    /// <summary>
    /// 注册报告生成器
    /// </summary>
    /// <param name="format">格式名称</param>
    /// <param name="generator">报告生成器</param>
    public void RegisterGenerator(string format, IReportGenerator generator)
    {
        _generators[format] = generator;
        _logger.LogInformation("注册报告生成器: {Format}", format);
    }

    /// <summary>
    /// 生成报告摘要
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <returns>报告摘要</returns>
    public ReportSummary GenerateReportSummary(TestReport testReport)
    {
        return new ReportSummary
        {
            ReportName = testReport.ReportName,
            Environment = testReport.Environment,
            GeneratedAt = testReport.GeneratedAt,
            TestStartTime = testReport.TestStartTime,
            TestEndTime = testReport.TestEndTime,
            TotalDuration = testReport.TestEndTime - testReport.TestStartTime,
            TotalTests = testReport.Summary.TotalTests,
            PassedTests = testReport.Summary.PassedTests,
            FailedTests = testReport.Summary.FailedTests,
            SkippedTests = testReport.Summary.SkippedTests,
            PassRate = testReport.Summary.PassRate,
            FailureRate = testReport.Summary.FailureRate,
            HasFailures = testReport.Summary.HasFailures(),
            ScreenshotCount = testReport.Screenshots.Count,
            Categories = testReport.GetAllCategories()
        };
    }
}

/// <summary>
/// 报告摘要
/// </summary>
public class ReportSummary
{
    /// <summary>
    /// 报告名称
    /// </summary>
    public string ReportName { get; set; } = string.Empty;

    /// <summary>
    /// 环境名称
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// 测试开始时间
    /// </summary>
    public DateTime TestStartTime { get; set; }

    /// <summary>
    /// 测试结束时间
    /// </summary>
    public DateTime TestEndTime { get; set; }

    /// <summary>
    /// 总执行时长
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// 总测试数
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// 通过测试数
    /// </summary>
    public int PassedTests { get; set; }

    /// <summary>
    /// 失败测试数
    /// </summary>
    public int FailedTests { get; set; }

    /// <summary>
    /// 跳过测试数
    /// </summary>
    public int SkippedTests { get; set; }

    /// <summary>
    /// 通过率
    /// </summary>
    public double PassRate { get; set; }

    /// <summary>
    /// 失败率
    /// </summary>
    public double FailureRate { get; set; }

    /// <summary>
    /// 是否有失败
    /// </summary>
    public bool HasFailures { get; set; }

    /// <summary>
    /// 截图数量
    /// </summary>
    public int ScreenshotCount { get; set; }

    /// <summary>
    /// 测试分类
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// 获取状态描述
    /// </summary>
    /// <returns>状态描述</returns>
    public string GetStatusDescription()
    {
        return HasFailures ? "失败" : "成功";
    }

    /// <summary>
    /// 获取简要描述
    /// </summary>
    /// <returns>简要描述</returns>
    public string GetBriefDescription()
    {
        return $"{TotalTests} 个测试，{PassedTests} 个通过，{FailedTests} 个失败，通过率 {PassRate:F1}%";
    }
}