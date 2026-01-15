using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Core.Logging;

/// <summary>
/// Serilog 配置类
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// 创建 Serilog 日志记录器
    /// </summary>
    /// <param name="settings">日志设置</param>
    /// <param name="testName">测试名称（可选）</param>
    /// <returns>日志记录器实例</returns>
    public static Serilog.ILogger CreateLogger(LoggingSettings settings, string? testName = null)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(ParseLogLevel(settings.Level))
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId();

        // 添加测试名称到日志上下文
        if (!string.IsNullOrEmpty(testName))
        {
            loggerConfig = loggerConfig.Enrich.WithProperty("TestName", testName);
        }

        // 配置控制台输出
        if (settings.EnableConsole)
        {
            loggerConfig = loggerConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }

        // 配置文件输出，支持文件轮转
        if (settings.EnableFile)
        {
            var logFilePath = ExpandLogFilePath(settings.FilePath);
            loggerConfig = loggerConfig.WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: settings.RetainedFileCount,
                fileSizeLimitBytes: settings.FileSizeLimitMB * 1024 * 1024,
                rollOnFileSizeLimit: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}");

            // 配置结构化日志输出（JSON格式）
            if (settings.EnableStructuredLogging)
            {
                var structuredLogPath = logFilePath.Replace(".log", "-structured.json");
                loggerConfig = loggerConfig.WriteTo.File(
                    new Serilog.Formatting.Json.JsonFormatter(),
                    path: structuredLogPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: settings.RetainedFileCount,
                    fileSizeLimitBytes: settings.FileSizeLimitMB * 1024 * 1024,
                    rollOnFileSizeLimit: true);
            }
        }

        return loggerConfig.CreateLogger();
    }

    /// <summary>
    /// 创建 Microsoft.Extensions.Logging 兼容的日志工厂
    /// </summary>
    /// <param name="settings">日志设置</param>
    /// <param name="testName">测试名称（可选）</param>
    /// <returns>日志工厂实例</returns>
    public static ILoggerFactory CreateLoggerFactory(LoggingSettings settings, string? testName = null)
    {
        var serilogLogger = CreateLogger(settings, testName);
        Log.Logger = serilogLogger;

        return new SerilogLoggerFactory(serilogLogger);
    }

    /// <summary>
    /// 解析日志级别
    /// </summary>
    /// <param name="level">日志级别字符串</param>
    /// <returns>Serilog 日志级别</returns>
    private static LogEventLevel ParseLogLevel(string level)
    {
        return level.ToUpperInvariant() switch
        {
            "VERBOSE" => LogEventLevel.Verbose,
            "DEBUG" => LogEventLevel.Debug,
            "INFORMATION" => LogEventLevel.Information,
            "WARNING" => LogEventLevel.Warning,
            "ERROR" => LogEventLevel.Error,
            "FATAL" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }

    /// <summary>
    /// 展开日志文件路径中的占位符
    /// </summary>
    /// <param name="filePath">原始文件路径</param>
    /// <returns>展开后的文件路径</returns>
    private static string ExpandLogFilePath(string filePath)
    {
        var expandedPath = filePath
            .Replace("{Date}", DateTime.Now.ToString("yyyy-MM-dd"))
            .Replace("{DateTime}", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"))
            .Replace("{MachineName}", Environment.MachineName)
            .Replace("{ProcessId}", Environment.ProcessId.ToString());

        // 确保目录存在
        var directory = Path.GetDirectoryName(expandedPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return expandedPath;
    }

    /// <summary>
    /// 配置测试上下文日志
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="testClass">测试类名</param>
    /// <param name="testMethod">测试方法名</param>
    /// <returns>日志上下文</returns>
    public static IDisposable ConfigureTestContext(string testName, string? testClass = null, string? testMethod = null)
    {
        var contextProperties = new List<IDisposable>();

        contextProperties.Add(Serilog.Context.LogContext.PushProperty("TestName", testName));

        if (!string.IsNullOrEmpty(testClass))
        {
            contextProperties.Add(Serilog.Context.LogContext.PushProperty("TestClass", testClass));
        }

        if (!string.IsNullOrEmpty(testMethod))
        {
            contextProperties.Add(Serilog.Context.LogContext.PushProperty("TestMethod", testMethod));
        }

        return new CompositeDisposable(contextProperties);
    }

    /// <summary>
    /// 清理日志资源
    /// </summary>
    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}

/// <summary>
/// 复合可释放对象，用于管理多个 IDisposable 对象
/// </summary>
internal class CompositeDisposable : IDisposable
{
    private readonly List<IDisposable> _disposables;
    private bool _disposed = false;

    public CompositeDisposable(IEnumerable<IDisposable> disposables)
    {
        _disposables = disposables.ToList();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposed = true;
        }
    }
}