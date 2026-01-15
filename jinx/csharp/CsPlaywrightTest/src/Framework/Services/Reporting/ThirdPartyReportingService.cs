using EnterpriseAutomationFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace EnterpriseAutomationFramework.Services.Reporting;

/// <summary>
/// 第三方报告集成服务
/// </summary>
public class ThirdPartyReportingService
{
    private readonly ILogger<ThirdPartyReportingService> _logger;
    private readonly AllureReportGenerator _allureGenerator;
    private readonly ReportDataExporter _dataExporter;
    private readonly HistoricalReportService _historicalService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="allureGenerator">Allure报告生成器</param>
    /// <param name="dataExporter">数据导出器</param>
    /// <param name="historicalService">历史报告服务</param>
    public ThirdPartyReportingService(
        ILogger<ThirdPartyReportingService> logger,
        AllureReportGenerator allureGenerator,
        ReportDataExporter dataExporter,
        HistoricalReportService historicalService)
    {
        _logger = logger;
        _allureGenerator = allureGenerator;
        _dataExporter = dataExporter;
        _historicalService = historicalService;
    }

    /// <summary>
    /// 生成Allure报告
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <returns>生成的报告路径</returns>
    public async Task<string> GenerateAllureReportAsync(TestReport testReport, string outputDirectory)
    {
        try
        {
            _logger.LogInformation("开始生成Allure报告: {ReportName}", testReport.ReportName);

            // 确保输出目录存在
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // 生成Allure结果文件
            var allureResultsDir = Path.Combine(outputDirectory, "allure-results");
            var reportPath = await _allureGenerator.GenerateReportAsync(testReport, allureResultsDir);

            // 保存历史记录
            await _historicalService.SaveHistoricalReportAsync(testReport);

            _logger.LogInformation("Allure报告生成完成: {ReportPath}", reportPath);
            return reportPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成Allure报告失败: {ReportName}", testReport.ReportName);
            throw;
        }
    }

    /// <summary>
    /// 导出报告数据
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="formats">导出格式</param>
    /// <returns>导出的文件路径列表</returns>
    public async Task<List<string>> ExportReportDataAsync(TestReport testReport, string outputDirectory, params string[] formats)
    {
        try
        {
            _logger.LogInformation("开始导出报告数据: {ReportName}, 格式: {Formats}", 
                testReport.ReportName, string.Join(", ", formats));

            // 确保输出目录存在
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var exportedFiles = await _dataExporter.ExportMultipleFormatsAsync(testReport, outputDirectory, formats);

            _logger.LogInformation("报告数据导出完成，共导出 {Count} 个文件", exportedFiles.Count);
            return exportedFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出报告数据失败: {ReportName}", testReport.ReportName);
            throw;
        }
    }

    /// <summary>
    /// 生成历史对比报告
    /// </summary>
    /// <param name="currentReport">当前报告</param>
    /// <param name="previousReportId">之前报告ID</param>
    /// <param name="outputPath">输出路径</param>
    /// <returns>对比报告路径</returns>
    public async Task<string> GenerateComparisonReportAsync(TestReport currentReport, string previousReportId, string outputPath)
    {
        try
        {
            _logger.LogInformation("开始生成历史对比报告: 当前={CurrentId}, 之前={PreviousId}", 
                currentReport.ReportId, previousReportId);

            var comparison = await _historicalService.CompareReportsAsync(currentReport, previousReportId);
            var comparisonHtml = GenerateComparisonHtml(comparison);

            await File.WriteAllTextAsync(outputPath, comparisonHtml);

            _logger.LogInformation("历史对比报告生成完成: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成历史对比报告失败");
            throw;
        }
    }

    /// <summary>
    /// 生成趋势分析报告
    /// </summary>
    /// <param name="environment">环境名称</param>
    /// <param name="days">分析天数</param>
    /// <param name="outputPath">输出路径</param>
    /// <returns>趋势分析报告路径</returns>
    public async Task<string> GenerateTrendAnalysisReportAsync(string environment, int days, string outputPath)
    {
        try
        {
            _logger.LogInformation("开始生成趋势分析报告: 环境={Environment}, 天数={Days}", environment, days);

            var trendAnalysis = await _historicalService.GenerateTrendAnalysisAsync(environment, days);
            var trendHtml = GenerateTrendAnalysisHtml(trendAnalysis);

            await File.WriteAllTextAsync(outputPath, trendHtml);

            _logger.LogInformation("趋势分析报告生成完成: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成趋势分析报告失败");
            throw;
        }
    }

    /// <summary>
    /// 集成多种第三方报告工具
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="options">集成选项</param>
    /// <returns>集成结果</returns>
    public async Task<ThirdPartyIntegrationResult> IntegrateThirdPartyToolsAsync(
        TestReport testReport, 
        string outputDirectory, 
        ThirdPartyIntegrationOptions options)
    {
        var result = new ThirdPartyIntegrationResult
        {
            ReportId = testReport.ReportId,
            IntegrationStartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("开始第三方工具集成: {ReportName}", testReport.ReportName);

            // 生成Allure报告
            if (options.GenerateAllureReport)
            {
                try
                {
                    var allurePath = await GenerateAllureReportAsync(testReport, outputDirectory);
                    result.AllureReportPath = allurePath;
                    result.SuccessfulIntegrations.Add("Allure");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Allure集成失败");
                    result.FailedIntegrations.Add("Allure");
                    result.Errors.Add($"Allure: {ex.Message}");
                }
            }

            // 导出数据
            if (options.ExportFormats?.Any() == true)
            {
                try
                {
                    var exportedFiles = await ExportReportDataAsync(testReport, outputDirectory, options.ExportFormats);
                    result.ExportedFiles.AddRange(exportedFiles);
                    result.SuccessfulIntegrations.Add("DataExport");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "数据导出失败");
                    result.FailedIntegrations.Add("DataExport");
                    result.Errors.Add($"DataExport: {ex.Message}");
                }
            }

            // 生成历史对比
            if (!string.IsNullOrEmpty(options.CompareWithReportId))
            {
                try
                {
                    var comparisonPath = Path.Combine(outputDirectory, "comparison.html");
                    var comparisonReportPath = await GenerateComparisonReportAsync(
                        testReport, options.CompareWithReportId, comparisonPath);
                    result.ComparisonReportPath = comparisonReportPath;
                    result.SuccessfulIntegrations.Add("HistoricalComparison");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "历史对比失败");
                    result.FailedIntegrations.Add("HistoricalComparison");
                    result.Errors.Add($"HistoricalComparison: {ex.Message}");
                }
            }

            // 生成趋势分析
            if (options.GenerateTrendAnalysis)
            {
                try
                {
                    var trendPath = Path.Combine(outputDirectory, "trend-analysis.html");
                    var trendReportPath = await GenerateTrendAnalysisReportAsync(
                        testReport.Environment, options.TrendAnalysisDays, trendPath);
                    result.TrendAnalysisReportPath = trendReportPath;
                    result.SuccessfulIntegrations.Add("TrendAnalysis");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "趋势分析失败");
                    result.FailedIntegrations.Add("TrendAnalysis");
                    result.Errors.Add($"TrendAnalysis: {ex.Message}");
                }
            }

            result.IntegrationEndTime = DateTime.UtcNow;
            result.IsSuccessful = result.FailedIntegrations.Count == 0;

            _logger.LogInformation("第三方工具集成完成: 成功={SuccessCount}, 失败={FailureCount}", 
                result.SuccessfulIntegrations.Count, result.FailedIntegrations.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "第三方工具集成失败");
            result.IntegrationEndTime = DateTime.UtcNow;
            result.IsSuccessful = false;
            result.Errors.Add($"General: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// 获取历史报告列表
    /// </summary>
    /// <param name="environment">环境名称</param>
    /// <param name="days">天数</param>
    /// <returns>历史报告列表</returns>
    public async Task<List<HistoricalReportSummary>> GetHistoricalReportsAsync(string? environment = null, int days = 30)
    {
        return await _historicalService.GetHistoricalReportsAsync(environment, days);
    }

    /// <summary>
    /// 清理过期报告
    /// </summary>
    /// <param name="retentionDays">保留天数</param>
    /// <returns>清理的文件数量</returns>
    public async Task<int> CleanupHistoricalReportsAsync(int retentionDays = 90)
    {
        return await _historicalService.CleanupHistoricalReportsAsync(retentionDays);
    }

    /// <summary>
    /// 生成对比HTML
    /// </summary>
    /// <param name="comparison">对比结果</param>
    /// <returns>HTML内容</returns>
    private string GenerateComparisonHtml(ReportComparison comparison)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>测试报告对比</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background: #f5f5f5; padding: 20px; border-radius: 5px; }}
        .summary {{ display: flex; gap: 20px; margin: 20px 0; }}
        .metric {{ background: white; border: 1px solid #ddd; padding: 15px; border-radius: 5px; flex: 1; }}
        .positive {{ color: green; }}
        .negative {{ color: red; }}
        .neutral {{ color: #666; }}
        .test-changes {{ margin: 20px 0; }}
        .change-item {{ padding: 10px; border-left: 4px solid #ddd; margin: 5px 0; }}
        .fixed {{ border-left-color: green; background: #f0fff0; }}
        .regressed {{ border-left-color: red; background: #fff0f0; }}
        .new {{ border-left-color: blue; background: #f0f0ff; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>测试报告对比</h1>
        <p>当前报告: {comparison.CurrentReport.ReportName} ({comparison.CurrentReport.TestStartTime:yyyy-MM-dd HH:mm:ss})</p>
        <p>对比报告: {comparison.PreviousReport.ReportName} ({comparison.PreviousReport.TestStartTime:yyyy-MM-dd HH:mm:ss})</p>
        <p>对比时间: {comparison.ComparisonDate:yyyy-MM-dd HH:mm:ss}</p>
    </div>

    <div class='summary'>
        <div class='metric'>
            <h3>总测试数</h3>
            <p>当前: {comparison.CurrentReport.Summary.TotalTests}</p>
            <p>之前: {comparison.PreviousReport.Summary.TotalTests}</p>
            <p class='{GetChangeClass(comparison.SummaryComparison.TotalTestsChange)}'>
                变化: {comparison.SummaryComparison.TotalTestsChange:+0;-0;0}
            </p>
        </div>
        <div class='metric'>
            <h3>通过率</h3>
            <p>当前: {comparison.CurrentReport.Summary.PassRate:F1}%</p>
            <p>之前: {comparison.PreviousReport.Summary.PassRate:F1}%</p>
            <p class='{GetChangeClass(comparison.SummaryComparison.PassRateChange)}'>
                变化: {comparison.SummaryComparison.PassRateChange:+0.0;-0.0;0.0}%
            </p>
        </div>
        <div class='metric'>
            <h3>失败测试数</h3>
            <p>当前: {comparison.CurrentReport.Summary.FailedTests}</p>
            <p>之前: {comparison.PreviousReport.Summary.FailedTests}</p>
            <p class='{GetChangeClass(-comparison.SummaryComparison.FailedTestsChange)}'>
                变化: {comparison.SummaryComparison.FailedTestsChange:+0;-0;0}
            </p>
        </div>
    </div>

    <div class='test-changes'>
        <h2>测试变化详情</h2>
        {GenerateTestChangesHtml(comparison.TestComparisons)}
    </div>
</body>
</html>";

        return html;
    }

    /// <summary>
    /// 生成趋势分析HTML
    /// </summary>
    /// <param name="trendAnalysis">趋势分析</param>
    /// <returns>HTML内容</returns>
    private string GenerateTrendAnalysisHtml(TrendAnalysis trendAnalysis)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>趋势分析报告</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background: #f5f5f5; padding: 20px; border-radius: 5px; }}
        .metrics {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; margin: 20px 0; }}
        .metric {{ background: white; border: 1px solid #ddd; padding: 15px; border-radius: 5px; }}
        .trend-up {{ color: green; }}
        .trend-down {{ color: red; }}
        .trend-stable {{ color: #666; }}
        .problematic-tests {{ margin: 20px 0; }}
        .test-item {{ padding: 10px; border: 1px solid #ddd; margin: 5px 0; border-radius: 3px; }}
        .flaky {{ background: #fff3cd; border-color: #ffeaa7; }}
        .high-failure {{ background: #f8d7da; border-color: #f5c6cb; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>趋势分析报告</h1>
        <p>环境: {trendAnalysis.Environment}</p>
        <p>分析期间: {trendAnalysis.StartDate:yyyy-MM-dd} 至 {trendAnalysis.EndDate:yyyy-MM-dd} ({trendAnalysis.AnalysisPeriod} 天)</p>
        <p>报告数量: {trendAnalysis.TotalReports}</p>
    </div>

    <div class='metrics'>
        <div class='metric'>
            <h3>通过率趋势</h3>
            <p class='{GetTrendClass(trendAnalysis.PassRateTrend.Trend)}'>
                趋势: {GetTrendText(trendAnalysis.PassRateTrend.Trend)}
            </p>
            <p>变化: {trendAnalysis.PassRateTrend.ChangePercentage:F1}%</p>
            <p>平均: {trendAnalysis.PassRateTrend.Average:F1}%</p>
            <p>范围: {trendAnalysis.PassRateTrend.Min:F1}% - {trendAnalysis.PassRateTrend.Max:F1}%</p>
        </div>
        <div class='metric'>
            <h3>测试数量趋势</h3>
            <p class='{GetTrendClass(trendAnalysis.TestCountTrend.Trend)}'>
                趋势: {GetTrendText(trendAnalysis.TestCountTrend.Trend)}
            </p>
            <p>变化: {trendAnalysis.TestCountTrend.ChangePercentage:F1}%</p>
            <p>平均: {trendAnalysis.TestCountTrend.Average:F0}</p>
            <p>范围: {trendAnalysis.TestCountTrend.Min:F0} - {trendAnalysis.TestCountTrend.Max:F0}</p>
        </div>
        <div class='metric'>
            <h3>执行时长趋势</h3>
            <p class='{GetTrendClass(trendAnalysis.DurationTrend.Trend)}'>
                趋势: {GetTrendText(trendAnalysis.DurationTrend.Trend)}
            </p>
            <p>变化: {trendAnalysis.DurationTrend.ChangePercentage:F1}%</p>
            <p>平均: {trendAnalysis.DurationTrend.Average:F1}秒</p>
            <p>范围: {trendAnalysis.DurationTrend.Min:F1}秒 - {trendAnalysis.DurationTrend.Max:F1}秒</p>
        </div>
        <div class='metric'>
            <h3>稳定性指标</h3>
            <p>通过率标准差: {trendAnalysis.StabilityMetrics.PassRateStandardDeviation:F2}</p>
            <p>测试数量标准差: {trendAnalysis.StabilityMetrics.TestCountStandardDeviation:F2}</p>
            <p>一致性分数: {trendAnalysis.StabilityMetrics.ConsistencyScore:F1}/100</p>
        </div>
    </div>

    <div class='problematic-tests'>
        <h2>问题测试</h2>
        {GenerateProblematicTestsHtml(trendAnalysis.ProblematicTests)}
    </div>
</body>
</html>";

        return html;
    }

    /// <summary>
    /// 生成测试变化HTML
    /// </summary>
    /// <param name="testComparisons">测试比较列表</param>
    /// <returns>HTML内容</returns>
    private string GenerateTestChangesHtml(List<TestComparison> testComparisons)
    {
        var html = "";
        var significantChanges = testComparisons.Where(t => 
            t.ChangeType == TestChangeType.Fixed || 
            t.ChangeType == TestChangeType.Regressed || 
            t.ChangeType == TestChangeType.New).ToList();

        foreach (var change in significantChanges)
        {
            var cssClass = change.ChangeType switch
            {
                TestChangeType.Fixed => "fixed",
                TestChangeType.Regressed => "regressed",
                TestChangeType.New => "new",
                _ => ""
            };

            html += $@"
                <div class='change-item {cssClass}'>
                    <strong>{change.TestName}</strong>
                    <p>变化类型: {GetChangeTypeText(change.ChangeType)}</p>
                    <p>状态: {change.PreviousStatus} → {change.CurrentStatus}</p>
                </div>";
        }

        return html;
    }

    /// <summary>
    /// 生成问题测试HTML
    /// </summary>
    /// <param name="problematicTests">问题测试列表</param>
    /// <returns>HTML内容</returns>
    private string GenerateProblematicTestsHtml(List<ProblematicTest> problematicTests)
    {
        var html = "";
        
        foreach (var test in problematicTests.Take(10)) // 只显示前10个
        {
            var cssClass = test.IsFlaky ? "flaky" : "high-failure";
            var description = test.IsFlaky ? "不稳定测试" : "高失败率测试";

            html += $@"
                <div class='test-item {cssClass}'>
                    <strong>{test.TestName}</strong>
                    <p>类型: {description}</p>
                    <p>失败率: {test.FailureRate:F1}% ({test.FailedExecutions}/{test.TotalExecutions})</p>
                </div>";
        }

        return html;
    }

    /// <summary>
    /// 获取变化CSS类
    /// </summary>
    /// <param name="change">变化值</param>
    /// <returns>CSS类名</returns>
    private string GetChangeClass(double change)
    {
        return change switch
        {
            > 0 => "positive",
            < 0 => "negative",
            _ => "neutral"
        };
    }

    /// <summary>
    /// 获取趋势CSS类
    /// </summary>
    /// <param name="trend">趋势方向</param>
    /// <returns>CSS类名</returns>
    private string GetTrendClass(TrendDirection trend)
    {
        return trend switch
        {
            TrendDirection.Increasing => "trend-up",
            TrendDirection.Decreasing => "trend-down",
            _ => "trend-stable"
        };
    }

    /// <summary>
    /// 获取趋势文本
    /// </summary>
    /// <param name="trend">趋势方向</param>
    /// <returns>趋势文本</returns>
    private string GetTrendText(TrendDirection trend)
    {
        return trend switch
        {
            TrendDirection.Increasing => "上升",
            TrendDirection.Decreasing => "下降",
            _ => "稳定"
        };
    }

    /// <summary>
    /// 获取变化类型文本
    /// </summary>
    /// <param name="changeType">变化类型</param>
    /// <returns>变化类型文本</returns>
    private string GetChangeTypeText(TestChangeType changeType)
    {
        return changeType switch
        {
            TestChangeType.Fixed => "已修复",
            TestChangeType.Regressed => "回归",
            TestChangeType.New => "新增",
            TestChangeType.Removed => "已删除",
            TestChangeType.StatusChanged => "状态变化",
            _ => "无变化"
        };
    }
}

/// <summary>
/// 第三方集成选项
/// </summary>
public class ThirdPartyIntegrationOptions
{
    /// <summary>
    /// 是否生成Allure报告
    /// </summary>
    public bool GenerateAllureReport { get; set; } = true;

    /// <summary>
    /// 导出格式列表
    /// </summary>
    public string[] ExportFormats { get; set; } = { "json", "xml", "csv" };

    /// <summary>
    /// 对比的报告ID
    /// </summary>
    public string? CompareWithReportId { get; set; }

    /// <summary>
    /// 是否生成趋势分析
    /// </summary>
    public bool GenerateTrendAnalysis { get; set; } = true;

    /// <summary>
    /// 趋势分析天数
    /// </summary>
    public int TrendAnalysisDays { get; set; } = 30;
}

/// <summary>
/// 第三方集成结果
/// </summary>
public class ThirdPartyIntegrationResult
{
    /// <summary>
    /// 报告ID
    /// </summary>
    public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// 集成开始时间
    /// </summary>
    public DateTime IntegrationStartTime { get; set; }

    /// <summary>
    /// 集成结束时间
    /// </summary>
    public DateTime IntegrationEndTime { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 成功的集成
    /// </summary>
    public List<string> SuccessfulIntegrations { get; set; } = new();

    /// <summary>
    /// 失败的集成
    /// </summary>
    public List<string> FailedIntegrations { get; set; } = new();

    /// <summary>
    /// 错误信息
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Allure报告路径
    /// </summary>
    public string? AllureReportPath { get; set; }

    /// <summary>
    /// 导出的文件列表
    /// </summary>
    public List<string> ExportedFiles { get; set; } = new();

    /// <summary>
    /// 对比报告路径
    /// </summary>
    public string? ComparisonReportPath { get; set; }

    /// <summary>
    /// 趋势分析报告路径
    /// </summary>
    public string? TrendAnalysisReportPath { get; set; }

    /// <summary>
    /// 获取集成摘要
    /// </summary>
    /// <returns>集成摘要</returns>
    public string GetIntegrationSummary()
    {
        var duration = IntegrationEndTime - IntegrationStartTime;
        return $"第三方集成完成: 成功 {SuccessfulIntegrations.Count} 项，失败 {FailedIntegrations.Count} 项，耗时 {duration.TotalSeconds:F1} 秒";
    }
}