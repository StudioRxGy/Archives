using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CsPlaywrightXun.src.playwright.Core.Models;
using Microsoft.Extensions.Logging;

namespace CsPlaywrightXun.src.playwright.Services.Reporting;

/// <summary>
/// 报告数据导出器
/// </summary>
public class ReportDataExporter
{
    private readonly ILogger<ReportDataExporter> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public ReportDataExporter(ILogger<ReportDataExporter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 导出为JSON格式
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputPath">输出路径</param>
    /// <returns>导出的文件路径</returns>
    public async Task<string> ExportToJsonAsync(TestReport testReport, string outputPath)
    {
        try
        {
            _logger.LogInformation("开始导出JSON格式报告: {ReportName}", testReport.ReportName);

            var exportData = CreateExportData(testReport);
            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(outputPath, json, Encoding.UTF8);

            _logger.LogInformation("JSON格式报告导出完成: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出JSON格式报告失败: {ReportName}", testReport.ReportName);
            throw;
        }
    }

    /// <summary>
    /// 导出为XML格式
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputPath">输出路径</param>
    /// <returns>导出的文件路径</returns>
    public async Task<string> ExportToXmlAsync(TestReport testReport, string outputPath)
    {
        try
        {
            _logger.LogInformation("开始导出XML格式报告: {ReportName}", testReport.ReportName);

            var xml = CreateXmlReport(testReport);
            await File.WriteAllTextAsync(outputPath, xml.ToString(), Encoding.UTF8);

            _logger.LogInformation("XML格式报告导出完成: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出XML格式报告失败: {ReportName}", testReport.ReportName);
            throw;
        }
    }

    /// <summary>
    /// 导出为CSV格式
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputPath">输出路径</param>
    /// <returns>导出的文件路径</returns>
    public async Task<string> ExportToCsvAsync(TestReport testReport, string outputPath)
    {
        try
        {
            _logger.LogInformation("开始导出CSV格式报告: {ReportName}", testReport.ReportName);

            var csv = CreateCsvReport(testReport);
            await File.WriteAllTextAsync(outputPath, csv, Encoding.UTF8);

            _logger.LogInformation("CSV格式报告导出完成: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出CSV格式报告失败: {ReportName}", testReport.ReportName);
            throw;
        }
    }

    /// <summary>
    /// 导出测试结果摘要
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputPath">输出路径</param>
    /// <returns>导出的文件路径</returns>
    public async Task<string> ExportSummaryAsync(TestReport testReport, string outputPath)
    {
        try
        {
            _logger.LogInformation("开始导出测试摘要: {ReportName}", testReport.ReportName);

            var summary = CreateSummaryReport(testReport);
            await File.WriteAllTextAsync(outputPath, summary, Encoding.UTF8);

            _logger.LogInformation("测试摘要导出完成: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出测试摘要失败: {ReportName}", testReport.ReportName);
            throw;
        }
    }

    /// <summary>
    /// 批量导出多种格式
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="formats">导出格式列表</param>
    /// <returns>导出的文件路径列表</returns>
    public async Task<List<string>> ExportMultipleFormatsAsync(TestReport testReport, string outputDirectory, params string[] formats)
    {
        var exportedFiles = new List<string>();

        foreach (var format in formats)
        {
            try
            {
                var fileName = $"{testReport.ReportName}_{DateTime.Now:yyyyMMdd_HHmmss}.{format.ToLower()}";
                var outputPath = Path.Combine(outputDirectory, fileName);

                var exportedPath = format.ToLower() switch
                {
                    "json" => await ExportToJsonAsync(testReport, outputPath),
                    "xml" => await ExportToXmlAsync(testReport, outputPath),
                    "csv" => await ExportToCsvAsync(testReport, outputPath),
                    "txt" => await ExportSummaryAsync(testReport, outputPath),
                    _ => throw new NotSupportedException($"不支持的导出格式: {format}")
                };

                exportedFiles.Add(exportedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出 {Format} 格式失败", format);
            }
        }

        return exportedFiles;
    }

    /// <summary>
    /// 创建导出数据对象
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <returns>导出数据对象</returns>
    private object CreateExportData(TestReport testReport)
    {
        return new
        {
            ReportInfo = new
            {
                testReport.ReportId,
                testReport.ReportName,
                testReport.GeneratedAt,
                testReport.TestStartTime,
                testReport.TestEndTime,
                testReport.Environment,
                testReport.ReportVersion
            },
            Summary = new
            {
                testReport.Summary.TotalTests,
                testReport.Summary.PassedTests,
                testReport.Summary.FailedTests,
                testReport.Summary.SkippedTests,
                testReport.Summary.InconclusiveTests,
                testReport.Summary.PassRate,
                testReport.Summary.FailureRate,
                testReport.Summary.SkipRate,
                TotalDuration = testReport.Summary.TotalDuration.TotalSeconds,
                AverageDuration = testReport.Summary.AverageDuration.TotalSeconds,
                FastestTest = testReport.Summary.FastestTest.TotalSeconds,
                SlowestTest = testReport.Summary.SlowestTest.TotalSeconds
            },
            TestResults = testReport.Results.Select(r => new
            {
                r.TestName,
                r.TestClass,
                r.TestMethod,
                Status = r.Status.ToString(),
                r.StartTime,
                r.EndTime,
                Duration = r.Duration.TotalSeconds,
                r.ErrorMessage,
                r.StackTrace,
                r.Output,
                r.Screenshots,
                r.Categories,
                r.Tags,
                r.TestData,
                r.Metadata
            }),
            Screenshots = testReport.Screenshots,
            SystemInfo = testReport.SystemInfo,
            Configuration = testReport.Configuration,
            Metadata = testReport.Metadata
        };
    }

    /// <summary>
    /// 创建XML报告
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <returns>XML文档</returns>
    private XDocument CreateXmlReport(TestReport testReport)
    {
        var root = new XElement("TestReport",
            new XAttribute("reportId", testReport.ReportId),
            new XAttribute("reportName", testReport.ReportName),
            new XAttribute("generatedAt", testReport.GeneratedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
            new XAttribute("environment", testReport.Environment),
            new XAttribute("version", testReport.ReportVersion)
        );

        // 添加报告信息
        var reportInfo = new XElement("ReportInfo",
            new XElement("TestStartTime", testReport.TestStartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
            new XElement("TestEndTime", testReport.TestEndTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
            new XElement("TotalDuration", (testReport.TestEndTime - testReport.TestStartTime).TotalSeconds)
        );
        root.Add(reportInfo);

        // 添加摘要信息
        var summary = new XElement("Summary",
            new XElement("TotalTests", testReport.Summary.TotalTests),
            new XElement("PassedTests", testReport.Summary.PassedTests),
            new XElement("FailedTests", testReport.Summary.FailedTests),
            new XElement("SkippedTests", testReport.Summary.SkippedTests),
            new XElement("InconclusiveTests", testReport.Summary.InconclusiveTests),
            new XElement("PassRate", testReport.Summary.PassRate),
            new XElement("FailureRate", testReport.Summary.FailureRate),
            new XElement("SkipRate", testReport.Summary.SkipRate),
            new XElement("AverageDuration", testReport.Summary.AverageDuration.TotalSeconds),
            new XElement("FastestTest", testReport.Summary.FastestTest.TotalSeconds),
            new XElement("SlowestTest", testReport.Summary.SlowestTest.TotalSeconds)
        );
        root.Add(summary);

        // 添加测试结果
        var testResults = new XElement("TestResults");
        foreach (var result in testReport.Results)
        {
            var testResult = new XElement("TestResult",
                new XAttribute("name", result.TestName),
                new XAttribute("status", result.Status.ToString()),
                new XElement("TestClass", result.TestClass ?? ""),
                new XElement("TestMethod", result.TestMethod ?? ""),
                new XElement("StartTime", result.StartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                new XElement("EndTime", result.EndTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                new XElement("Duration", result.Duration.TotalSeconds),
                new XElement("ErrorMessage", result.ErrorMessage ?? ""),
                new XElement("StackTrace", result.StackTrace ?? ""),
                new XElement("Output", result.Output ?? "")
            );

            // 添加分类
            if (result.Categories.Any())
            {
                var categories = new XElement("Categories");
                foreach (var category in result.Categories)
                {
                    categories.Add(new XElement("Category", category));
                }
                testResult.Add(categories);
            }

            // 添加标签
            if (result.Tags.Any())
            {
                var tags = new XElement("Tags");
                foreach (var tag in result.Tags)
                {
                    tags.Add(new XElement("Tag", tag));
                }
                testResult.Add(tags);
            }

            // 添加截图
            if (result.Screenshots.Any())
            {
                var screenshots = new XElement("Screenshots");
                foreach (var screenshot in result.Screenshots)
                {
                    screenshots.Add(new XElement("Screenshot", screenshot));
                }
                testResult.Add(screenshots);
            }

            testResults.Add(testResult);
        }
        root.Add(testResults);

        // 添加系统信息
        if (testReport.SystemInfo.Any())
        {
            var systemInfo = new XElement("SystemInfo");
            foreach (var kvp in testReport.SystemInfo)
            {
                systemInfo.Add(new XElement("Property", 
                    new XAttribute("name", kvp.Key), 
                    kvp.Value?.ToString() ?? ""));
            }
            root.Add(systemInfo);
        }

        return new XDocument(root);
    }

    /// <summary>
    /// 创建CSV报告
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <returns>CSV内容</returns>
    private string CreateCsvReport(TestReport testReport)
    {
        var csv = new StringBuilder();
        
        // CSV头部
        csv.AppendLine("TestName,TestClass,TestMethod,Status,StartTime,EndTime,Duration(s),ErrorMessage,Categories,Tags");

        // 测试结果数据
        foreach (var result in testReport.Results)
        {
            var line = new[]
            {
                EscapeCsvField(result.TestName),
                EscapeCsvField(result.TestClass ?? ""),
                EscapeCsvField(result.TestMethod ?? ""),
                result.Status.ToString(),
                result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                result.Duration.TotalSeconds.ToString("F2"),
                EscapeCsvField(result.ErrorMessage ?? ""),
                EscapeCsvField(string.Join(";", result.Categories)),
                EscapeCsvField(string.Join(";", result.Tags))
            };

            csv.AppendLine(string.Join(",", line));
        }

        return csv.ToString();
    }

    /// <summary>
    /// 创建摘要报告
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <returns>摘要内容</returns>
    private string CreateSummaryReport(TestReport testReport)
    {
        var summary = new StringBuilder();
        
        summary.AppendLine("=".PadRight(80, '='));
        summary.AppendLine($"测试报告摘要: {testReport.ReportName}");
        summary.AppendLine("=".PadRight(80, '='));
        summary.AppendLine();
        
        summary.AppendLine("基本信息:");
        summary.AppendLine($"  报告ID: {testReport.ReportId}");
        summary.AppendLine($"  环境: {testReport.Environment}");
        summary.AppendLine($"  生成时间: {testReport.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        summary.AppendLine($"  测试开始时间: {testReport.TestStartTime:yyyy-MM-dd HH:mm:ss}");
        summary.AppendLine($"  测试结束时间: {testReport.TestEndTime:yyyy-MM-dd HH:mm:ss}");
        summary.AppendLine($"  总执行时长: {(testReport.TestEndTime - testReport.TestStartTime).TotalSeconds:F1}秒");
        summary.AppendLine();
        
        summary.AppendLine("测试结果统计:");
        summary.AppendLine($"  总测试数: {testReport.Summary.TotalTests}");
        summary.AppendLine($"  通过: {testReport.Summary.PassedTests} ({testReport.Summary.PassRate:F1}%)");
        summary.AppendLine($"  失败: {testReport.Summary.FailedTests} ({testReport.Summary.FailureRate:F1}%)");
        summary.AppendLine($"  跳过: {testReport.Summary.SkippedTests} ({testReport.Summary.SkipRate:F1}%)");
        summary.AppendLine($"  不确定: {testReport.Summary.InconclusiveTests}");
        summary.AppendLine();
        
        summary.AppendLine("执行时间分析:");
        summary.AppendLine($"  平均执行时间: {testReport.Summary.AverageDuration.TotalSeconds:F2}秒");
        summary.AppendLine($"  最快测试: {testReport.Summary.FastestTest.TotalSeconds:F2}秒");
        summary.AppendLine($"  最慢测试: {testReport.Summary.SlowestTest.TotalSeconds:F2}秒");
        summary.AppendLine();

        // 失败测试详情
        var failedTests = testReport.GetFailedTests();
        if (failedTests.Any())
        {
            summary.AppendLine("失败测试详情:");
            foreach (var failed in failedTests)
            {
                summary.AppendLine($"  - {failed.TestName}");
                if (!string.IsNullOrEmpty(failed.ErrorMessage))
                {
                    summary.AppendLine($"    错误: {failed.ErrorMessage}");
                }
            }
            summary.AppendLine();
        }

        // 分类统计
        var categories = testReport.GetAllCategories();
        if (categories.Any())
        {
            summary.AppendLine("测试分类统计:");
            foreach (var category in categories)
            {
                var categoryTests = testReport.GetTestsByCategory(category);
                var categoryPassed = categoryTests.Count(t => t.Status == TestStatus.Passed);
                var categoryFailed = categoryTests.Count(t => t.Status == TestStatus.Failed);
                summary.AppendLine($"  {category}: {categoryTests.Count} 个测试 ({categoryPassed} 通过, {categoryFailed} 失败)");
            }
            summary.AppendLine();
        }

        summary.AppendLine("=".PadRight(80, '='));
        
        return summary.ToString();
    }

    /// <summary>
    /// 转义CSV字段
    /// </summary>
    /// <param name="field">字段值</param>
    /// <returns>转义后的字段值</returns>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}