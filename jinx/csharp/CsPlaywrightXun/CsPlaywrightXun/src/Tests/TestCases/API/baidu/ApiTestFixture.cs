using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Services.Api;

namespace CsPlaywrightXun.src.playwright.Tests.API.baidu;

/// <summary>
/// API 测试固件
/// 为 API 测试提供共享的资源和配置
/// </summary>
public class ApiTestFixture : IAsyncLifetime
{
    /// <summary>
    /// API 客户端
    /// </summary>
    public IApiClient ApiClient { get; private set; } = null!;

    /// <summary>
    /// 测试配置
    /// </summary>
    public TestConfiguration Configuration { get; private set; } = null!;

    /// <summary>
    /// 日志记录器
    /// </summary>
    public ILogger Logger { get; private set; } = null!;

    /// <summary>
    /// HTTP 客户端
    /// </summary>
    public HttpClient HttpClient { get; private set; } = null!;

    /// <summary>
    /// API 性能监控器
    /// </summary>
    public IApiPerformanceMonitor? PerformanceMonitor { get; private set; }

    /// <summary>
    /// 初始化异步资源
    /// </summary>
    /// <returns>异步任务</returns>
    public async Task InitializeAsync()
    {
        // 创建日志记录器
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole()
                   .SetMinimumLevel(LogLevel.Information);
        });
        Logger = loggerFactory.CreateLogger<ApiTestFixture>();

        Logger.LogInformation("开始初始化 API 测试固件");

        try
        {
            // 创建测试配置
            Configuration = CreateTestConfiguration();
            Logger.LogInformation("测试配置创建完成");

            // 创建 HTTP 客户端
            HttpClient = new HttpClient();
            Logger.LogInformation("HTTP 客户端创建完成");

            // 创建性能监控器
            var performanceLogger = loggerFactory.CreateLogger<ApiPerformanceMonitor>();
            PerformanceMonitor = new ApiPerformanceMonitor(performanceLogger);
            Logger.LogInformation("API 性能监控器创建完成");

            // 创建 API 客户端
            var apiClientLogger = loggerFactory.CreateLogger<ApiClient>();
            ApiClient = new ApiClient(HttpClient, Configuration, apiClientLogger);
            Logger.LogInformation("API 客户端创建完成");

            // 验证配置
            await ValidateConfigurationAsync();
            Logger.LogInformation("配置验证完成");

            Logger.LogInformation("API 测试固件初始化完成");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "API 测试固件初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 清理异步资源
    /// </summary>
    /// <returns>异步任务</returns>
    public async Task DisposeAsync()
    {
        Logger.LogInformation("开始清理 API 测试固件资源");

        try
        {
            // 清理性能监控器（先清理，以便获取最终报告）
            if (PerformanceMonitor != null)
            {
                // 输出最终的性能报告
                var finalReport = PerformanceMonitor.GetPerformanceReport(24);
                if (finalReport.TotalRequests > 0)
                {
                    Logger.LogInformation("=== 最终 API 性能报告 ===");
                    Logger.LogInformation("总请求数: {TotalRequests}", finalReport.TotalRequests);
                    Logger.LogInformation("平均响应时间: {AverageResponseTime}ms", 
                        finalReport.AverageResponseTime.TotalMilliseconds);
                    Logger.LogInformation("成功率: {SuccessRate}%", finalReport.SuccessRate);
                }
                Logger.LogInformation("性能监控器已清理");
            }

            // 清理 API 客户端（ApiClient 会自动处理 HttpClient 的释放）
            if (ApiClient is IDisposable disposableApiClient)
            {
                disposableApiClient.Dispose();
                Logger.LogInformation("API 客户端已释放（包含 HTTP 客户端）");
            }

            // 注意：不直接释放 HttpClient，因为 ApiClient 已经处理了它的释放
            // 避免双重释放导致的 "Cannot access a disposed object" 错误

            Logger.LogInformation("API 测试固件资源清理完成");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "清理 API 测试固件资源时发生错误");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 创建测试配置
    /// </summary>
    /// <returns>测试配置</returns>
    private TestConfiguration CreateTestConfiguration()
    {
        var configuration = new TestConfiguration
        {
            Environment = new EnvironmentSettings
            {
                Name = "Test",
                BaseUrl = "https://www.baidu.com",
                ApiBaseUrl = "https://www.baidu.com",
                Variables = new Dictionary<string, string>
                {
                    ["TestEnvironment"] = "API_Testing",
                    ["UserAgent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
                }
            },
            Api = new ApiSettings
            {
                Timeout = 30000,      // 30 秒超时
                RetryCount = 3,       // 重试 3 次
                RetryDelay = 1000     // 重试间隔 1 秒
            },
            Logging = new LoggingSettings
            {
                Level = "Information",
                FilePath = "Logs/api-test-{Date}.log",
                EnableConsole = true,
                EnableFile = true,
                EnableStructuredLogging = true
            },
            Reporting = new ReportingSettings
            {
                OutputPath = "Reports/API",
                Format = "Html",
                IncludeScreenshots = false // API 测试不需要截图
            },
            Browser = new BrowserSettings() // API 测试不需要浏览器，但配置需要完整
        };

        // 验证配置
        if (!configuration.IsValid())
        {
            var errors = configuration.GetValidationErrors();
            throw new InvalidOperationException($"测试配置无效: {string.Join("; ", errors)}");
        }

        return configuration;
    }

    /// <summary>
    /// 验证配置是否正确
    /// </summary>
    /// <returns>异步任务</returns>
    private async Task ValidateConfigurationAsync()
    {
        try
        {
            // 验证 API 基础 URL 是否可访问
            Logger.LogInformation("验证 API 基础 URL: {ApiBaseUrl}", Configuration.Environment.ApiBaseUrl);
            
            using var testClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await testClient.GetAsync(Configuration.Environment.ApiBaseUrl);
            
            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("API 基础 URL 验证成功，状态码: {StatusCode}", response.StatusCode);
            }
            else
            {
                Logger.LogWarning("API 基础 URL 返回非成功状态码: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "验证 API 基础 URL 时发生异常，但测试将继续进行");
        }
    }

    /// <summary>
    /// 获取测试统计信息
    /// </summary>
    /// <returns>测试统计信息</returns>
    public ApiTestStatistics GetTestStatistics()
    {
        var performanceReport = PerformanceMonitor?.GetPerformanceReport(24);
        
        return new ApiTestStatistics
        {
            TotalRequests = performanceReport?.TotalRequests ?? 0,
            AverageResponseTime = performanceReport?.AverageResponseTime ?? TimeSpan.Zero,
            MaxResponseTime = performanceReport?.MaxResponseTime ?? TimeSpan.Zero,
            MinResponseTime = performanceReport?.MinResponseTime ?? TimeSpan.Zero,
            SuccessRate = performanceReport?.SuccessRate ?? 0,
            TestStartTime = DateTime.UtcNow, // 这里应该记录实际的开始时间
            TestEndTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 重置性能监控数据
    /// </summary>
    public void ResetPerformanceMetrics()
    {
        PerformanceMonitor?.ClearAllMetrics();
        Logger.LogInformation("性能监控数据已重置");
    }

    /// <summary>
    /// 创建用于特定测试的 API 客户端
    /// </summary>
    /// <param name="customSettings">自定义 API 设置</param>
    /// <returns>自定义的 API 客户端</returns>
    public IApiClient CreateCustomApiClient(ApiSettings? customSettings = null)
    {
        var customConfiguration = new TestConfiguration
        {
            Environment = Configuration.Environment,
            Api = customSettings ?? Configuration.Api,
            Logging = Configuration.Logging,
            Reporting = Configuration.Reporting,
            Browser = Configuration.Browser
        };

        // 创建新的 HttpClient，它将由返回的 ApiClient 管理和释放
        var customHttpClient = new HttpClient();
        var customLogger = LoggerFactory.Create(builder => builder.AddConsole())
                                       .CreateLogger<ApiClient>();

        Logger.LogInformation("创建自定义 API 客户端，超时设置: {Timeout}ms", customConfiguration.Api.Timeout);
        
        return new ApiClient(customHttpClient, customConfiguration, customLogger);
    }
}

/// <summary>
/// API 测试统计信息
/// </summary>
public class ApiTestStatistics
{
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
    /// 测试开始时间
    /// </summary>
    public DateTime TestStartTime { get; set; }

    /// <summary>
    /// 测试结束时间
    /// </summary>
    public DateTime TestEndTime { get; set; }

    /// <summary>
    /// 测试总耗时
    /// </summary>
    public TimeSpan TotalTestTime => TestEndTime - TestStartTime;

    /// <summary>
    /// 获取统计摘要
    /// </summary>
    /// <returns>统计摘要字符串</returns>
    public string GetSummary()
    {
        return $"总请求: {TotalRequests}, " +
               $"平均响应时间: {AverageResponseTime.TotalMilliseconds:F2}ms, " +
               $"成功率: {SuccessRate:F2}%, " +
               $"测试耗时: {TotalTestTime.TotalSeconds:F2}s";
    }
}