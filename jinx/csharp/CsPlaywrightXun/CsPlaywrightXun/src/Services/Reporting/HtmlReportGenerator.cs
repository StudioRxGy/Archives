using System.Text;
using System.Text.Json;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Core.Models;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace CsPlaywrightXun.src.playwright.Services.Reporting;

/// <summary>
/// HTML 报告生成器
/// </summary>
public class HtmlReportGenerator : IReportGenerator
{
    private readonly ILogger<HtmlReportGenerator> _logger;
    private readonly HtmlTemplateProvider _templateProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="templateProvider">模板提供器</param>
    public HtmlReportGenerator(ILogger<HtmlReportGenerator> logger, HtmlTemplateProvider templateProvider)
    {
        _logger = logger;
        _templateProvider = templateProvider;
    }

    /// <summary>
    /// 生成HTML报告
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputPath">输出路径（可选，如果为空则使用默认路径）</param>
    /// <returns>生成的报告文件路径</returns>
    public async Task<string> GenerateReportAsync(TestReport testReport, string? outputPath = null)
    {
        try
        {
            _logger.LogInformation("开始生成HTML报告: {ReportName}", testReport.ReportName);

            // 使用PathConfiguration获取报告文件路径
            var reportPath = outputPath ?? PathConfiguration.GetReportPath($"{testReport.ReportName}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.html");
            
            // 确保输出目录存在
            var outputDir = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                PathConfiguration.EnsureDirectoryExists(outputDir);
            }

            // 生成HTML内容
            var htmlContent = await GenerateHtmlContentAsync(testReport);

            // 写入文件
            await File.WriteAllTextAsync(reportPath, htmlContent, Encoding.UTF8);

            // 复制静态资源
            await CopyStaticResourcesAsync(outputDir!);

            _logger.LogInformation("HTML报告生成完成: {OutputPath}", reportPath);
            return reportPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成HTML报告失败: {ReportName}", testReport.ReportName);
            throw;
        }
    }

    /// <summary>
    /// 生成HTML内容
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <returns>HTML内容</returns>
    private async Task<string> GenerateHtmlContentAsync(TestReport testReport)
    {
        var template = await _templateProvider.GetMainTemplateAsync();
        
        // 替换模板变量
        var html = template
            .Replace("{{REPORT_TITLE}}", testReport.ReportName)
            .Replace("{{REPORT_GENERATED_AT}}", testReport.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{{ENVIRONMENT}}", testReport.Environment)
            .Replace("{{TEST_START_TIME}}", testReport.TestStartTime.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{{TEST_END_TIME}}", testReport.TestEndTime.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{{TOTAL_DURATION}}", FormatDuration(testReport.TestEndTime - testReport.TestStartTime))
            .Replace("{{SUMMARY_SECTION}}", GenerateSummarySection(testReport.Summary))
            .Replace("{{CHARTS_SECTION}}", GenerateChartsSection(testReport.Summary))
            .Replace("{{RESULTS_SECTION}}", GenerateResultsSection(testReport.Results))
            .Replace("{{FAILED_TESTS_SECTION}}", GenerateFailedTestsSection(testReport.GetFailedTests()))
            .Replace("{{SCREENSHOTS_SECTION}}", GenerateScreenshotsSection(testReport.Screenshots))
            .Replace("{{SYSTEM_INFO_SECTION}}", GenerateSystemInfoSection(testReport.SystemInfo))
            .Replace("{{CONFIGURATION_SECTION}}", GenerateConfigurationSection(testReport.Configuration))
            .Replace("{{METADATA_SECTION}}", GenerateMetadataSection(testReport.Metadata));

        return html;
    }

    /// <summary>
    /// 生成摘要部分
    /// </summary>
    /// <param name="summary">测试摘要</param>
    /// <returns>摘要HTML</returns>
    private string GenerateSummarySection(TestSummary summary)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<div class='summary-cards'>");
        
        // 总测试数卡片
        sb.AppendLine($@"
            <div class='summary-card total'>
                <div class='card-icon'>📊</div>
                <div class='card-content'>
                    <div class='card-number'>{summary.TotalTests}</div>
                    <div class='card-label'>总测试数</div>
                </div>
            </div>");

        // 通过测试卡片
        sb.AppendLine($@"
            <div class='summary-card passed'>
                <div class='card-icon'>✅</div>
                <div class='card-content'>
                    <div class='card-number'>{summary.PassedTests}</div>
                    <div class='card-label'>通过</div>
                    <div class='card-percentage'>{summary.PassRate:F1}%</div>
                </div>
            </div>");

        // 失败测试卡片
        sb.AppendLine($@"
            <div class='summary-card failed'>
                <div class='card-icon'>❌</div>
                <div class='card-content'>
                    <div class='card-number'>{summary.FailedTests}</div>
                    <div class='card-label'>失败</div>
                    <div class='card-percentage'>{summary.FailureRate:F1}%</div>
                </div>
            </div>");

        // 跳过测试卡片
        sb.AppendLine($@"
            <div class='summary-card skipped'>
                <div class='card-icon'>⏭️</div>
                <div class='card-content'>
                    <div class='card-number'>{summary.SkippedTests}</div>
                    <div class='card-label'>跳过</div>
                    <div class='card-percentage'>{summary.SkipRate:F1}%</div>
                </div>
            </div>");

        // 执行时长卡片
        sb.AppendLine($@"
            <div class='summary-card duration'>
                <div class='card-icon'>⏱️</div>
                <div class='card-content'>
                    <div class='card-number'>{FormatDuration(summary.TotalDuration)}</div>
                    <div class='card-label'>总耗时</div>
                    <div class='card-percentage'>平均 {FormatDuration(summary.AverageDuration)}</div>
                </div>
            </div>");

        sb.AppendLine("</div>");
        
        return sb.ToString();
    }

    /// <summary>
    /// 生成图表部分
    /// </summary>
    /// <param name="summary">测试摘要</param>
    /// <returns>图表HTML</returns>
    private string GenerateChartsSection(TestSummary summary)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<div class='charts-container'>");
        
        // 饼图数据
        var pieChartData = new
        {
            labels = new[] { "通过", "失败", "跳过", "不确定" },
            datasets = new[]
            {
                new
                {
                    data = new[] { summary.PassedTests, summary.FailedTests, summary.SkippedTests, summary.InconclusiveTests },
                    backgroundColor = new[] { "#28a745", "#dc3545", "#ffc107", "#6c757d" },
                    borderWidth = 2,
                    borderColor = "#fff"
                }
            }
        };

        sb.AppendLine($@"
            <div class='chart-container'>
                <h3>测试结果分布</h3>
                <canvas id='pieChart' width='400' height='400'></canvas>
                <script>
                    const pieCtx = document.getElementById('pieChart').getContext('2d');
                    new Chart(pieCtx, {{
                        type: 'pie',
                        data: {JsonSerializer.Serialize(pieChartData)},
                        options: {{
                            responsive: true,
                            plugins: {{
                                legend: {{
                                    position: 'bottom'
                                }},
                                tooltip: {{
                                    callbacks: {{
                                        label: function(context) {{
                                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                            const percentage = ((context.parsed / total) * 100).toFixed(1);
                                            return context.label + ': ' + context.parsed + ' (' + percentage + '%)';
                                        }}
                                    }}
                                }}
                            }}
                        }}
                    }});
                </script>
            </div>");

        // 趋势图（如果有历史数据）
        sb.AppendLine($@"
            <div class='chart-container'>
                <h3>执行时间分析</h3>
                <canvas id='timeChart' width='400' height='200'></canvas>
                <script>
                    const timeCtx = document.getElementById('timeChart').getContext('2d');
                    new Chart(timeCtx, {{
                        type: 'bar',
                        data: {{
                            labels: ['最快测试', '平均时间', '最慢测试'],
                            datasets: [{{
                                label: '执行时间 (秒)',
                                data: [{summary.FastestTest.TotalSeconds:F2}, {summary.AverageDuration.TotalSeconds:F2}, {summary.SlowestTest.TotalSeconds:F2}],
                                backgroundColor: ['#28a745', '#17a2b8', '#dc3545'],
                                borderWidth: 1
                            }}]
                        }},
                        options: {{
                            responsive: true,
                            scales: {{
                                y: {{
                                    beginAtZero: true,
                                    title: {{
                                        display: true,
                                        text: '时间 (秒)'
                                    }}
                                }}
                            }},
                            plugins: {{
                                legend: {{
                                    display: false
                                }}
                            }}
                        }}
                    }});
                </script>
            </div>");

        sb.AppendLine("</div>");
        
        return sb.ToString();
    }

    /// <summary>
    /// 生成测试结果部分
    /// </summary>
    /// <param name="results">测试结果列表</param>
    /// <returns>结果HTML</returns>
    private string GenerateResultsSection(List<Core.Models.TestResult> results)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<div class='results-section'>");
        sb.AppendLine("<h2>测试结果详情</h2>");
        
        // 过滤器
        sb.AppendLine(@"
            <div class='filters'>
                <button class='filter-btn active' data-filter='all'>全部</button>
                <button class='filter-btn' data-filter='passed'>通过</button>
                <button class='filter-btn' data-filter='failed'>失败</button>
                <button class='filter-btn' data-filter='skipped'>跳过</button>
            </div>");

        sb.AppendLine("<div class='results-table-container'>");
        sb.AppendLine("<table class='results-table'>");
        sb.AppendLine(@"
            <thead>
                <tr>
                    <th>状态</th>
                    <th>测试名称</th>
                    <th>测试类</th>
                    <th>执行时间</th>
                    <th>开始时间</th>
                    <th>操作</th>
                </tr>
            </thead>");
        
        sb.AppendLine("<tbody>");
        
        foreach (var result in results)
        {
            var statusClass = result.Status.ToString().ToLower();
            var statusIcon = GetStatusIcon(result.Status);
            var statusText = result.GetStatusDescription();
            
            sb.AppendLine($@"
                <tr class='result-row {statusClass}' data-status='{statusClass}'>
                    <td class='status-cell'>
                        <span class='status-badge {statusClass}'>
                            {statusIcon} {statusText}
                        </span>
                    </td>
                    <td class='test-name'>{EscapeHtml(result.TestName)}</td>
                    <td>{EscapeHtml(result.TestClass ?? "")}</td>
                    <td>{FormatDuration(result.Duration)}</td>
                    <td>{result.StartTime:HH:mm:ss}</td>
                    <td>
                        <button class='btn-details' onclick='showTestDetails(""{EscapeHtml(result.TestName)}"")'>详情</button>
                    </td>
                </tr>");
        }
        
        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        
        // 添加测试详情的JavaScript数据
        sb.AppendLine("<script>");
        sb.AppendLine("const testDetailsData = {");
        
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var detailsJson = JsonSerializer.Serialize(new
            {
                testName = result.TestName,
                testClass = result.TestClass,
                testMethod = result.TestMethod,
                status = result.GetStatusDescription(),
                startTime = result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                endTime = result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                duration = FormatDuration(result.Duration),
                errorMessage = result.ErrorMessage,
                stackTrace = result.StackTrace,
                output = result.Output,
                screenshots = result.Screenshots,
                categories = result.Categories,
                tags = result.Tags,
                testData = result.TestData,
                metadata = result.Metadata
            });
            
            sb.AppendLine($"  \"{EscapeHtml(result.TestName)}\": {detailsJson}");
            if (i < results.Count - 1) sb.AppendLine(",");
        }
        
        sb.AppendLine("};");
        sb.AppendLine("</script>");
        
        return sb.ToString();
    }

    /// <summary>
    /// 生成失败测试部分
    /// </summary>
    /// <param name="failedTests">失败的测试列表</param>
    /// <returns>失败测试HTML</returns>
    private string GenerateFailedTestsSection(List<Core.Models.TestResult> failedTests)
    {
        if (!failedTests.Any())
        {
            return "<div class='no-failures'><h2>🎉 没有失败的测试！</h2></div>";
        }

        var sb = new StringBuilder();
        
        sb.AppendLine("<div class='failed-tests-section'>");
        sb.AppendLine("<h2>失败测试详情</h2>");
        
        foreach (var test in failedTests)
        {
            sb.AppendLine($@"
                <div class='failed-test-card'>
                    <div class='failed-test-header'>
                        <h3>❌ {EscapeHtml(test.TestName)}</h3>
                        <span class='test-duration'>{FormatDuration(test.Duration)}</span>
                    </div>
                    <div class='failed-test-content'>
                        <div class='error-message'>
                            <h4>错误信息:</h4>
                            <pre>{EscapeHtml(test.ErrorMessage ?? "无错误信息")}</pre>
                        </div>");

            if (!string.IsNullOrEmpty(test.StackTrace))
            {
                sb.AppendLine($@"
                        <div class='stack-trace'>
                            <h4>堆栈跟踪:</h4>
                            <pre class='stack-trace-content'>{EscapeHtml(test.StackTrace)}</pre>
                        </div>");
            }

            if (test.Screenshots.Any())
            {
                sb.AppendLine("<div class='test-screenshots'>");
                sb.AppendLine("<h4>相关截图:</h4>");
                sb.AppendLine("<div class='screenshot-gallery'>");
                
                foreach (var screenshot in test.Screenshots)
                {
                    var fileName = Path.GetFileName(screenshot);
                    sb.AppendLine($@"
                        <div class='screenshot-item'>
                            <img src='{screenshot}' alt='Screenshot' onclick='openScreenshot(""{screenshot}"")' />
                            <div class='screenshot-name'>{fileName}</div>
                        </div>");
                }
                
                sb.AppendLine("</div>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
        }
        
        sb.AppendLine("</div>");
        
        return sb.ToString();
    }

    /// <summary>
    /// 生成截图部分
    /// </summary>
    /// <param name="screenshots">截图列表</param>
    /// <returns>截图HTML</returns>
    private string GenerateScreenshotsSection(List<string> screenshots)
    {
        if (!screenshots.Any())
        {
            return "";
        }

        var sb = new StringBuilder();
        
        sb.AppendLine("<div class='screenshots-section'>");
        sb.AppendLine("<h2>测试截图</h2>");
        sb.AppendLine("<div class='screenshots-gallery'>");
        
        foreach (var screenshot in screenshots)
        {
            var fileName = Path.GetFileName(screenshot);
            sb.AppendLine($@"
                <div class='screenshot-thumbnail'>
                    <img src='{screenshot}' alt='Screenshot' onclick='openScreenshot(""{screenshot}"")' />
                    <div class='screenshot-caption'>{fileName}</div>
                </div>");
        }
        
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        
        return sb.ToString();
    }

    /// <summary>
    /// 生成系统信息部分
    /// </summary>
    /// <param name="systemInfo">系统信息</param>
    /// <returns>系统信息HTML</returns>
    private string GenerateSystemInfoSection(Dictionary<string, object> systemInfo)
    {
        if (!systemInfo.Any())
        {
            return "";
        }

        var sb = new StringBuilder();
        
        sb.AppendLine("<div class='system-info-section'>");
        sb.AppendLine("<h2>系统信息</h2>");
        sb.AppendLine("<div class='info-grid'>");
        
        foreach (var kvp in systemInfo)
        {
            sb.AppendLine($@"
                <div class='info-item'>
                    <div class='info-label'>{EscapeHtml(kvp.Key)}:</div>
                    <div class='info-value'>{EscapeHtml(kvp.Value?.ToString() ?? "")}</div>
                </div>");
        }
        
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        
        return sb.ToString();
    }

    /// <summary>
    /// 生成配置信息部分
    /// </summary>
    /// <param name="configuration">配置信息</param>
    /// <returns>配置信息HTML</returns>
    private string GenerateConfigurationSection(Dictionary<string, object> configuration)
    {
        if (!configuration.Any())
        {
            return "";
        }

        var sb = new StringBuilder();
        
        sb.AppendLine("<div class='configuration-section'>");
        sb.AppendLine("<h2>测试配置</h2>");
        sb.AppendLine("<div class='config-grid'>");
        
        foreach (var kvp in configuration)
        {
            sb.AppendLine($@"
                <div class='config-item'>
                    <div class='config-label'>{EscapeHtml(kvp.Key)}:</div>
                    <div class='config-value'>{EscapeHtml(kvp.Value?.ToString() ?? "")}</div>
                </div>");
        }
        
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        
        return sb.ToString();
    }

    /// <summary>
    /// 生成元数据部分
    /// </summary>
    /// <param name="metadata">元数据</param>
    /// <returns>元数据HTML</returns>
    private string GenerateMetadataSection(Dictionary<string, object> metadata)
    {
        if (!metadata.Any())
        {
            return "";
        }

        var sb = new StringBuilder();
        
        sb.AppendLine("<div class='metadata-section'>");
        sb.AppendLine("<h2>元数据</h2>");
        sb.AppendLine("<div class='metadata-grid'>");
        
        foreach (var kvp in metadata)
        {
            sb.AppendLine($@"
                <div class='metadata-item'>
                    <div class='metadata-label'>{EscapeHtml(kvp.Key)}:</div>
                    <div class='metadata-value'>{EscapeHtml(kvp.Value?.ToString() ?? "")}</div>
                </div>");
        }
        
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        
        return sb.ToString();
    }

    /// <summary>
    /// 复制静态资源
    /// </summary>
    /// <param name="outputDir">输出目录</param>
    private async Task CopyStaticResourcesAsync(string outputDir)
    {
        var assetsDir = Path.Combine(outputDir, "assets");
        if (!Directory.Exists(assetsDir))
        {
            Directory.CreateDirectory(assetsDir);
        }

        // 复制CSS文件
        var cssContent = await _templateProvider.GetStylesheetAsync();
        await File.WriteAllTextAsync(Path.Combine(assetsDir, "report.css"), cssContent);

        // 复制JavaScript文件
        var jsContent = await _templateProvider.GetJavaScriptAsync();
        await File.WriteAllTextAsync(Path.Combine(assetsDir, "report.js"), jsContent);
    }

    /// <summary>
    /// 获取状态图标
    /// </summary>
    /// <param name="status">测试状态</param>
    /// <returns>状态图标</returns>
    private static string GetStatusIcon(TestStatus status)
    {
        return status switch
        {
            TestStatus.Passed => "✅",
            TestStatus.Failed => "❌",
            TestStatus.Skipped => "⏭️",
            TestStatus.Inconclusive => "❓",
            _ => "❓"
        };
    }

    /// <summary>
    /// 格式化时长
    /// </summary>
    /// <param name="duration">时长</param>
    /// <returns>格式化的时长字符串</returns>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
        else if (duration.TotalMinutes >= 1)
        {
            return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
        else
        {
            return $"{duration.TotalSeconds:F1}s";
        }
    }

    /// <summary>
    /// HTML转义
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <returns>转义后的文本</returns>
    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}