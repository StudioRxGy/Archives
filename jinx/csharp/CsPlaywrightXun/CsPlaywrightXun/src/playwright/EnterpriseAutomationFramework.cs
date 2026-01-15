using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Core.Logging;

namespace CsPlaywrightXun.src.playwright;

/// <summary>
/// 企业自动化框架主入口
/// </summary>
public static class Framework
{
    private static ILoggerFactory? _loggerFactory;
    private static TestConfiguration? _configuration;
    
    /// <summary>
    /// 初始化框架
    /// </summary>
    /// <param name="loggerFactory">自定义日志工厂（可选）</param>
    /// <param name="testName">测试名称（可选，用于日志上下文）</param>
    public static void Initialize(ILoggerFactory? loggerFactory = null, string? testName = null)
    {
        _configuration = ConfigurationManager.GetConfiguration();
        _loggerFactory = loggerFactory ?? CreateSerilogLoggerFactory(testName);
    }

    /// <summary>
    /// 获取日志记录器
    /// </summary>
    public static ILogger<T> GetLogger<T>()
    {
        if (_loggerFactory == null)
        {
            Initialize();
        }
        
        return _loggerFactory!.CreateLogger<T>();
    }

    /// <summary>
    /// 获取非泛型日志记录器
    /// </summary>
    /// <param name="categoryName">类别名称</param>
    /// <returns>日志记录器</returns>
    public static ILogger GetLogger(string categoryName)
    {
        if (_loggerFactory == null)
        {
            Initialize();
        }
        
        return _loggerFactory!.CreateLogger(categoryName);
    }

    /// <summary>
    /// 获取测试配置
    /// </summary>
    public static TestConfiguration GetConfiguration()
    {
        return _configuration ??= ConfigurationManager.GetConfiguration();
    }

    /// <summary>
    /// 创建测试上下文日志记录器
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="testClass">测试类名</param>
    /// <param name="testMethod">测试方法名</param>
    /// <returns>测试上下文日志记录器</returns>
    public static TestContextLogger CreateTestLogger(string testName, string? testClass = null, string? testMethod = null)
    {
        return new TestContextLogger(testName, testClass, testMethod);
    }

    /// <summary>
    /// 关闭并刷新日志
    /// </summary>
    public static void CloseAndFlushLogs()
    {
        SerilogConfiguration.CloseAndFlush();
    }

    /// <summary>
    /// 创建 Serilog 日志工厂
    /// </summary>
    /// <param name="testName">测试名称（可选）</param>
    /// <returns>日志工厂实例</returns>
    private static ILoggerFactory CreateSerilogLoggerFactory(string? testName = null)
    {
        var configuration = GetConfiguration();
        return SerilogConfiguration.CreateLoggerFactory(configuration.Logging, testName);
    }

    /// <summary>
    /// 创建默认日志工厂（向后兼容）
    /// </summary>
    [Obsolete("使用 CreateSerilogLoggerFactory 替代")]
    private static ILoggerFactory CreateDefaultLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information);
        });
    }
}