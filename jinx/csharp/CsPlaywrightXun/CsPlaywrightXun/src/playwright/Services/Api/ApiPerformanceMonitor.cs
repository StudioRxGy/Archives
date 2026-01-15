using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Interfaces;

namespace CsPlaywrightXun.src.playwright.Services.Api;

/// <summary>
/// API 性能监控器
/// </summary>
public class ApiPerformanceMonitor : IApiPerformanceMonitor
{
    private readonly ILogger<ApiPerformanceMonitor> _logger;
    private readonly Dictionary<string, List<ApiPerformanceMetric>> _metrics;
    private readonly object _lock = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public ApiPerformanceMonitor(ILogger<ApiPerformanceMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = new Dictionary<string, List<ApiPerformanceMetric>>();
    }

    /// <summary>
    /// 记录API性能指标
    /// </summary>
    /// <param name="endpoint">API端点</param>
    /// <param name="method">HTTP方法</param>
    /// <param name="responseTime">响应时间</param>
    /// <param name="statusCode">状态码</param>
    /// <param name="requestSize">请求大小（字节）</param>
    /// <param name="responseSize">响应大小（字节）</param>
    public void RecordMetric(string endpoint, string method, TimeSpan responseTime, int statusCode, 
        long requestSize = 0, long responseSize = 0)
    {
        var key = $"{method.ToUpperInvariant()} {endpoint}";
        var metric = new ApiPerformanceMetric
        {
            Endpoint = endpoint,
            Method = method,
            ResponseTime = responseTime,
            StatusCode = statusCode,
            RequestSize = requestSize,
            ResponseSize = responseSize,
            Timestamp = DateTime.UtcNow
        };

        lock (_lock)
        {
            if (!_metrics.ContainsKey(key))
            {
                _metrics[key] = new List<ApiPerformanceMetric>();
            }
            _metrics[key].Add(metric);
        }

        _logger.LogDebug("记录API性能指标: {Method} {Endpoint}, 响应时间: {ResponseTime}ms, 状态码: {StatusCode}",
            method, endpoint, responseTime.TotalMilliseconds, statusCode);
    }

    /// <summary>
    /// 获取指定端点的性能统计
    /// </summary>
    /// <param name="endpoint">API端点</param>
    /// <param name="method">HTTP方法</param>
    /// <returns>性能统计</returns>
    public ApiPerformanceStatistics? GetStatistics(string endpoint, string method)
    {
        var key = $"{method.ToUpperInvariant()} {endpoint}";
        
        lock (_lock)
        {
            if (!_metrics.ContainsKey(key) || !_metrics[key].Any())
            {
                return null;
            }

            var metrics = _metrics[key];
            return CalculateStatistics(key, metrics);
        }
    }

    /// <summary>
    /// 获取所有端点的性能统计
    /// </summary>
    /// <returns>所有性能统计</returns>
    public List<ApiPerformanceStatistics> GetAllStatistics()
    {
        lock (_lock)
        {
            return _metrics.Where(kvp => kvp.Value.Any())
                          .Select(kvp => CalculateStatistics(kvp.Key, kvp.Value))
                          .ToList();
        }
    }

    /// <summary>
    /// 清除指定端点的性能数据
    /// </summary>
    /// <param name="endpoint">API端点</param>
    /// <param name="method">HTTP方法</param>
    public void ClearMetrics(string endpoint, string method)
    {
        var key = $"{method.ToUpperInvariant()} {endpoint}";
        
        lock (_lock)
        {
            if (_metrics.ContainsKey(key))
            {
                _metrics[key].Clear();
                _logger.LogInformation("清除API性能数据: {Key}", key);
            }
        }
    }

    /// <summary>
    /// 清除所有性能数据
    /// </summary>
    public void ClearAllMetrics()
    {
        lock (_lock)
        {
            _metrics.Clear();
            _logger.LogInformation("清除所有API性能数据");
        }
    }

    /// <summary>
    /// 获取性能报告
    /// </summary>
    /// <param name="timeRange">时间范围（小时）</param>
    /// <returns>性能报告</returns>
    public ApiPerformanceReport GetPerformanceReport(int timeRange = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-timeRange);
        
        lock (_lock)
        {
            var allMetrics = _metrics.SelectMany(kvp => kvp.Value)
                                   .Where(m => m.Timestamp >= cutoffTime)
                                   .ToList();

            if (!allMetrics.Any())
            {
                return new ApiPerformanceReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    TimeRangeHours = timeRange,
                    TotalRequests = 0,
                    Statistics = new List<ApiPerformanceStatistics>()
                };
            }

            var statisticsList = _metrics.Where(kvp => kvp.Value.Any(m => m.Timestamp >= cutoffTime))
                                       .Select(kvp => CalculateStatistics(kvp.Key, 
                                           kvp.Value.Where(m => m.Timestamp >= cutoffTime).ToList()))
                                       .ToList();

            return new ApiPerformanceReport
            {
                GeneratedAt = DateTime.UtcNow,
                TimeRangeHours = timeRange,
                TotalRequests = allMetrics.Count,
                AverageResponseTime = TimeSpan.FromMilliseconds(allMetrics.Average(m => m.ResponseTime.TotalMilliseconds)),
                MaxResponseTime = allMetrics.Max(m => m.ResponseTime),
                MinResponseTime = allMetrics.Min(m => m.ResponseTime),
                SuccessRate = (double)allMetrics.Count(m => m.StatusCode >= 200 && m.StatusCode < 300) / allMetrics.Count * 100,
                Statistics = statisticsList
            };
        }
    }

    /// <summary>
    /// 计算性能统计
    /// </summary>
    /// <param name="key">端点键</param>
    /// <param name="metrics">性能指标列表</param>
    /// <returns>性能统计</returns>
    private ApiPerformanceStatistics CalculateStatistics(string key, List<ApiPerformanceMetric> metrics)
    {
        var responseTimes = metrics.Select(m => m.ResponseTime.TotalMilliseconds).ToList();
        var successfulRequests = metrics.Count(m => m.StatusCode >= 200 && m.StatusCode < 300);

        return new ApiPerformanceStatistics
        {
            EndpointKey = key,
            TotalRequests = metrics.Count,
            SuccessfulRequests = successfulRequests,
            FailedRequests = metrics.Count - successfulRequests,
            SuccessRate = (double)successfulRequests / metrics.Count * 100,
            AverageResponseTime = TimeSpan.FromMilliseconds(responseTimes.Average()),
            MaxResponseTime = TimeSpan.FromMilliseconds(responseTimes.Max()),
            MinResponseTime = TimeSpan.FromMilliseconds(responseTimes.Min()),
            MedianResponseTime = TimeSpan.FromMilliseconds(CalculateMedian(responseTimes)),
            P95ResponseTime = TimeSpan.FromMilliseconds(CalculatePercentile(responseTimes, 95)),
            P99ResponseTime = TimeSpan.FromMilliseconds(CalculatePercentile(responseTimes, 99)),
            TotalRequestSize = metrics.Sum(m => m.RequestSize),
            TotalResponseSize = metrics.Sum(m => m.ResponseSize),
            FirstRequestTime = metrics.Min(m => m.Timestamp),
            LastRequestTime = metrics.Max(m => m.Timestamp)
        };
    }

    /// <summary>
    /// 计算中位数
    /// </summary>
    /// <param name="values">数值列表</param>
    /// <returns>中位数</returns>
    private double CalculateMedian(List<double> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var count = sorted.Count;
        
        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        }
        else
        {
            return sorted[count / 2];
        }
    }

    /// <summary>
    /// 计算百分位数
    /// </summary>
    /// <param name="values">数值列表</param>
    /// <param name="percentile">百分位（0-100）</param>
    /// <returns>百分位数值</returns>
    private double CalculatePercentile(List<double> values, double percentile)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var index = (percentile / 100.0) * (sorted.Count - 1);
        
        if (index == Math.Floor(index))
        {
            return sorted[(int)index];
        }
        else
        {
            var lower = sorted[(int)Math.Floor(index)];
            var upper = sorted[(int)Math.Ceiling(index)];
            return lower + (upper - lower) * (index - Math.Floor(index));
        }
    }
}

/// <summary>
/// API 性能指标
/// </summary>
public class ApiPerformanceMetric
{
    /// <summary>
    /// API端点
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// HTTP方法
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 响应时间
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// 状态码
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 请求大小（字节）
    /// </summary>
    public long RequestSize { get; set; }

    /// <summary>
    /// 响应大小（字节）
    /// </summary>
    public long ResponseSize { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// API 性能统计
/// </summary>
public class ApiPerformanceStatistics
{
    /// <summary>
    /// 端点键（方法 + 端点）
    /// </summary>
    public string EndpointKey { get; set; } = string.Empty;

    /// <summary>
    /// 总请求数
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// 成功请求数
    /// </summary>
    public int SuccessfulRequests { get; set; }

    /// <summary>
    /// 失败请求数
    /// </summary>
    public int FailedRequests { get; set; }

    /// <summary>
    /// 成功率（百分比）
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// 平均响应时间
    /// </summary>
    public TimeSpan AverageResponseTime { get; set; }

    /// <summary>
    /// 最大响应时间
    /// </summary>
    public TimeSpan MaxResponseTime { get; set; }

    /// <summary>
    /// 最小响应时间
    /// </summary>
    public TimeSpan MinResponseTime { get; set; }

    /// <summary>
    /// 中位数响应时间
    /// </summary>
    public TimeSpan MedianResponseTime { get; set; }

    /// <summary>
    /// 95百分位响应时间
    /// </summary>
    public TimeSpan P95ResponseTime { get; set; }

    /// <summary>
    /// 99百分位响应时间
    /// </summary>
    public TimeSpan P99ResponseTime { get; set; }

    /// <summary>
    /// 总请求大小
    /// </summary>
    public long TotalRequestSize { get; set; }

    /// <summary>
    /// 总响应大小
    /// </summary>
    public long TotalResponseSize { get; set; }

    /// <summary>
    /// 第一次请求时间
    /// </summary>
    public DateTime FirstRequestTime { get; set; }

    /// <summary>
    /// 最后一次请求时间
    /// </summary>
    public DateTime LastRequestTime { get; set; }
}

/// <summary>
/// API 性能报告
/// </summary>
public class ApiPerformanceReport
{
    /// <summary>
    /// 报告生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// 时间范围（小时）
    /// </summary>
    public int TimeRangeHours { get; set; }

    /// <summary>
    /// 总请求数
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// 平均响应时间
    /// </summary>
    public TimeSpan AverageResponseTime { get; set; }

    /// <summary>
    /// 最大响应时间
    /// </summary>
    public TimeSpan MaxResponseTime { get; set; }

    /// <summary>
    /// 最小响应时间
    /// </summary>
    public TimeSpan MinResponseTime { get; set; }

    /// <summary>
    /// 成功率（百分比）
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// 各端点统计
    /// </summary>
    public List<ApiPerformanceStatistics> Statistics { get; set; } = new();
}