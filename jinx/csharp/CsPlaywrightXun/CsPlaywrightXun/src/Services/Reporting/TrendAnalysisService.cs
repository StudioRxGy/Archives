using System.Text;
using System.Text.Json;
using CsPlaywrightXun.src.playwright.Core.Models;
using Microsoft.Extensions.Logging;

namespace CsPlaywrightXun.src.playwright.Services.Reporting;

/// <summary>
/// 趋势分析服务
/// </summary>
public class TrendAnalysisService
{
    private readonly ILogger<TrendAnalysisService> _logger;
    private readonly string _historyDirectory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="historyDirectory">历史数据目录</param>
    public TrendAnalysisService(ILogger<TrendAnalysisService> logger, string historyDirectory = "Reports/History")
    {
        _logger = logger;
        _historyDirectory = historyDirectory;
        
        // 确保历史目录存在
        if (!Directory.Exists(_historyDirectory))
        {
            Directory.CreateDirectory(_historyDirectory);
        }
    }

    /// <summary>
    /// 保存报告历史数据
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <returns>保存的历史文件路径</returns>
    public async Task<string> SaveReportHistoryAsync(TestReport testReport)
    {
        try
        {
            var historyData = CreateHistoryData(testReport);
            var fileName = $"{testReport.Environment}_{testReport.GeneratedAt:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(_historyDirectory, fileName);

            var json = JsonSerializer.Serialize(historyData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(filePath, json);
            
            _logger.LogInformation("报告历史数据已保存: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存报告历史数据失败");
            throw;
        }
    }

    /// <summary>
    /// 获取历史趋势数据
    /// </summary>
    /// <param name="environment">环境名称</param>
    /// <param name="days">天数</param>
    /// <returns>趋势数据</returns>
    public async Task<TrendData> GetTrendDataAsync(string environment, int days = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var historyFiles = Directory.GetFiles(_historyDirectory, $"{environment}_*.json")
                .Where(f => GetDateFromFileName(f) >= cutoffDate)
                .OrderBy(f => f)
                .ToList();

            var historyDataList = new List<ReportHistoryData>();

            foreach (var file in historyFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var historyData = JsonSerializer.Deserialize<ReportHistoryData>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (historyData != null)
                    {
                        historyDataList.Add(historyData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "读取历史文件失败: {File}", file);
                }
            }

            return AnalyzeTrend(historyDataList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取趋势数据失败");
            throw;
        }
    }

    /// <summary>
    /// 生成趋势报告
    /// </summary>
    /// <param name="environment">环境名称</param>
    /// <param name="days">天数</param>
    /// <returns>趋势报告HTML</returns>
    public async Task<string> GenerateTrendReportAsync(string environment, int days = 30)
    {
        var trendData = await GetTrendDataAsync(environment, days);
        return GenerateTrendHtml(trendData);
    }

    /// <summary>
    /// 比较两个报告
    /// </summary>
    /// <param name="currentReport">当前报告</param>
    /// <param name="previousReport">之前的报告</param>
    /// <returns>比较结果</returns>
    public SummaryComparison CompareReports(TestReport currentReport, TestReport previousReport)
    {
        var currentData = CreateHistoryData(currentReport);
        var previousData = CreateHistoryData(previousReport);
        
        return new SummaryComparison
        {
            TotalTestsChange = currentData.TotalTests - previousData.TotalTests,
            PassedTestsChange = currentData.PassedTests - previousData.PassedTests,
            FailedTestsChange = currentData.FailedTests - previousData.FailedTests,
            PassRateChange = currentData.PassRate - previousData.PassRate,
            DurationChange = currentData.TotalDuration - previousData.TotalDuration
        };
    }

    /// <summary>
    /// 创建历史数据
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <returns>历史数据</returns>
    private ReportHistoryData CreateHistoryData(TestReport testReport)
    {
        return new ReportHistoryData
        {
            ReportName = testReport.ReportName,
            Environment = testReport.Environment,
            GeneratedAt = testReport.GeneratedAt,
            TestStartTime = testReport.TestStartTime,
            TestEndTime = testReport.TestEndTime,
            TotalTests = testReport.Summary.TotalTests,
            PassedTests = testReport.Summary.PassedTests,
            FailedTests = testReport.Summary.FailedTests,
            SkippedTests = testReport.Summary.SkippedTests,
            PassRate = testReport.Summary.PassRate,
            TotalDuration = testReport.Summary.TotalDuration,
            AverageDuration = testReport.Summary.AverageDuration,
            Categories = testReport.GetAllCategories(),
            FailedTestNames = testReport.GetFailedTests().Select(t => t.TestName).ToList()
        };
    }

    /// <summary>
    /// 分析趋势
    /// </summary>
    /// <param name="historyDataList">历史数据列表</param>
    /// <returns>趋势数据</returns>
    private TrendData AnalyzeTrend(List<ReportHistoryData> historyDataList)
    {
        if (!historyDataList.Any())
        {
            return new TrendData();
        }

        var trendData = new TrendData
        {
            Environment = historyDataList.First().Environment,
            StartDate = historyDataList.First().GeneratedAt,
            EndDate = historyDataList.Last().GeneratedAt,
            DataPoints = historyDataList.Count
        };

        // 计算趋势指标
        foreach (var data in historyDataList)
        {
            trendData.PassRateTrend.Add(new TrendPoint
            {
                Date = data.GeneratedAt,
                Value = data.PassRate
            });

            trendData.TotalTestsTrend.Add(new TrendPoint
            {
                Date = data.GeneratedAt,
                Value = data.TotalTests
            });

            trendData.DurationTrend.Add(new TrendPoint
            {
                Date = data.GeneratedAt,
                Value = data.TotalDuration.TotalSeconds
            });

            trendData.FailuresTrend.Add(new TrendPoint
            {
                Date = data.GeneratedAt,
                Value = data.FailedTests
            });
        }

        // 计算平均值和趋势方向
        trendData.AveragePassRate = trendData.PassRateTrend.Average(p => p.Value);
        trendData.AverageDuration = TimeSpan.FromSeconds(trendData.DurationTrend.Average(p => p.Value));
        trendData.AverageFailures = trendData.FailuresTrend.Average(p => p.Value);

        // 计算趋势方向（简单线性回归）
        trendData.PassRateTrendDirection = CalculateTrendDirection(trendData.PassRateTrend);
        trendData.DurationTrendDirection = CalculateTrendDirection(trendData.DurationTrend);
        trendData.FailuresTrendDirection = CalculateTrendDirection(trendData.FailuresTrend);

        return trendData;
    }

    /// <summary>
    /// 计算趋势方向
    /// </summary>
    /// <param name="points">趋势点列表</param>
    /// <returns>趋势方向</returns>
    private TrendDirection CalculateTrendDirection(List<TrendPoint> points)
    {
        if (points.Count < 2) return TrendDirection.Stable;

        var firstHalf = points.Take(points.Count / 2).Average(p => p.Value);
        var secondHalf = points.Skip(points.Count / 2).Average(p => p.Value);

        var difference = secondHalf - firstHalf;
        var threshold = Math.Abs(firstHalf) * 0.05; // 5% 阈值

        if (Math.Abs(difference) <= threshold)
            return TrendDirection.Stable;

        return difference > 0 ? TrendDirection.Increasing : TrendDirection.Decreasing;
    }

    /// <summary>
    /// 从文件名获取日期
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>日期</returns>
    private DateTime GetDateFromFileName(string fileName)
    {
        try
        {
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var parts = baseName.Split('_');
            if (parts.Length >= 2)
            {
                var datePart = parts[^2]; // 倒数第二个部分
                var timePart = parts[^1]; // 最后一个部分
                
                if (DateTime.TryParseExact($"{datePart}_{timePart}", "yyyyMMdd_HHmmss", null, 
                    System.Globalization.DateTimeStyles.None, out var date))
                {
                    return date;
                }
            }
        }
        catch
        {
            // 忽略解析错误
        }

        return DateTime.MinValue;
    }

    /// <summary>
    /// 生成趋势HTML
    /// </summary>
    /// <param name="trendData">趋势数据</param>
    /// <returns>HTML内容</returns>
    private string GenerateTrendHtml(TrendData trendData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<div class='trend-analysis'>");
        sb.AppendLine($"    <h2>趋势分析 - {trendData.Environment}</h2>");
        sb.AppendLine("    <div class='trend-summary'>");
        
        // 平均通过率
        sb.AppendLine("        <div class='trend-metric'>");
        sb.AppendLine("            <h3>平均通过率</h3>");
        sb.AppendLine($"            <div class='metric-value'>{trendData.AveragePassRate:F1}%</div>");
        sb.AppendLine($"            <div class='trend-indicator {GetTrendClass(trendData.PassRateTrendDirection)}'>");
        sb.AppendLine($"                {GetTrendIcon(trendData.PassRateTrendDirection)} {trendData.PassRateTrendDirection}");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");
        
        // 平均执行时间
        sb.AppendLine("        <div class='trend-metric'>");
        sb.AppendLine("            <h3>平均执行时间</h3>");
        sb.AppendLine($"            <div class='metric-value'>{trendData.AverageDuration.TotalSeconds:F1}s</div>");
        sb.AppendLine($"            <div class='trend-indicator {GetTrendClass(trendData.DurationTrendDirection)}'>");
        sb.AppendLine($"                {GetTrendIcon(trendData.DurationTrendDirection)} {trendData.DurationTrendDirection}");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");
        
        // 平均失败数
        sb.AppendLine("        <div class='trend-metric'>");
        sb.AppendLine("            <h3>平均失败数</h3>");
        sb.AppendLine($"            <div class='metric-value'>{trendData.AverageFailures:F1}</div>");
        sb.AppendLine($"            <div class='trend-indicator {GetTrendClass(trendData.FailuresTrendDirection)}'>");
        sb.AppendLine($"                {GetTrendIcon(trendData.FailuresTrendDirection)} {trendData.FailuresTrendDirection}");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");
        
        sb.AppendLine("    </div>");
        
        // 图表区域（简化版，不包含JavaScript）
        sb.AppendLine("    <div class='trend-charts'>");
        sb.AppendLine("        <div class='trend-chart'>");
        sb.AppendLine("            <h3>通过率趋势</h3>");
        sb.AppendLine("            <canvas id='passRateChart'></canvas>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class='trend-chart'>");
        sb.AppendLine("            <h3>执行时间趋势</h3>");
        sb.AppendLine("            <canvas id='durationChart'></canvas>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class='trend-chart'>");
        sb.AppendLine("            <h3>失败测试趋势</h3>");
        sb.AppendLine("            <canvas id='failuresChart'></canvas>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");
        
        return sb.ToString();
    }

    /// <summary>
    /// 获取趋势样式类
    /// </summary>
    /// <param name="direction">趋势方向</param>
    /// <returns>样式类</returns>
    private string GetTrendClass(TrendDirection direction)
    {
        return direction switch
        {
            TrendDirection.Increasing => "trend-improving",
            TrendDirection.Decreasing => "trend-declining",
            _ => "trend-stable"
        };
    }

    /// <summary>
    /// 获取趋势图标
    /// </summary>
    /// <param name="direction">趋势方向</param>
    /// <returns>图标</returns>
    private string GetTrendIcon(TrendDirection direction)
    {
        return direction switch
        {
            TrendDirection.Increasing => "📈",
            TrendDirection.Decreasing => "📉",
            _ => "➡️"
        };
    }
}

/// <summary>
/// 报告历史数据
/// </summary>
public class ReportHistoryData
{
    public string ReportName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime TestStartTime { get; set; }
    public DateTime TestEndTime { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public double PassRate { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> FailedTestNames { get; set; } = new();
}

/// <summary>
/// 趋势数据
/// </summary>
public class TrendData
{
    public string Environment { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DataPoints { get; set; }
    public double AveragePassRate { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public double AverageFailures { get; set; }
    public TrendDirection PassRateTrendDirection { get; set; }
    public TrendDirection DurationTrendDirection { get; set; }
    public TrendDirection FailuresTrendDirection { get; set; }
    public List<TrendPoint> PassRateTrend { get; set; } = new();
    public List<TrendPoint> TotalTestsTrend { get; set; } = new();
    public List<TrendPoint> DurationTrend { get; set; } = new();
    public List<TrendPoint> FailuresTrend { get; set; } = new();
}

/// <summary>
/// 趋势点
/// </summary>
public class TrendPoint
{
    public DateTime Date { get; set; }
    public double Value { get; set; }
}