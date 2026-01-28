using CsPlaywrightXun.src.playwright.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CsPlaywrightXun.src.playwright.Core.Extensions;

/// <summary>
/// 重试扩展方法
/// </summary>
public static class RetryExtensions
{
    /// <summary>
    /// 执行带重试的异步操作
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="policy">重试策略</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>操作结果</returns>
    public static async Task<T> WithRetryAsync<T>(
        this Func<Task<T>> operation,
        RetryPolicy policy,
        ILogger logger,
        string operationName = "Operation")
    {
        var executor = new RetryExecutor(policy, logger as ILogger<RetryExecutor> ?? 
            new LoggerAdapter<RetryExecutor>(logger));
        return await executor.ExecuteAsync(operation, operationName);
    }
    
    /// <summary>
    /// 执行带重试的异步操作（无返回值）
    /// </summary>
    /// <param name="operation">要执行的操作</param>
    /// <param name="policy">重试策略</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="operationName">操作名称</param>
    public static async Task WithRetryAsync(
        this Func<Task> operation,
        RetryPolicy policy,
        ILogger logger,
        string operationName = "Operation")
    {
        var executor = new RetryExecutor(policy, logger as ILogger<RetryExecutor> ?? 
            new LoggerAdapter<RetryExecutor>(logger));
        await executor.ExecuteAsync(operation, operationName);
    }
    
    /// <summary>
    /// 执行带重试的同步操作
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="policy">重试策略</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>操作结果</returns>
    public static async Task<T> WithRetryAsync<T>(
        this Func<T> operation,
        RetryPolicy policy,
        ILogger logger,
        string operationName = "Operation")
    {
        var executor = new RetryExecutor(policy, logger as ILogger<RetryExecutor> ?? 
            new LoggerAdapter<RetryExecutor>(logger));
        return await executor.ExecuteAsync(operation, operationName);
    }
    
    /// <summary>
    /// 执行带重试的同步操作（无返回值）
    /// </summary>
    /// <param name="operation">要执行的操作</param>
    /// <param name="policy">重试策略</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="operationName">操作名称</param>
    public static async Task WithRetryAsync(
        this Action operation,
        RetryPolicy policy,
        ILogger logger,
        string operationName = "Operation")
    {
        var executor = new RetryExecutor(policy, logger as ILogger<RetryExecutor> ?? 
            new LoggerAdapter<RetryExecutor>(logger));
        await executor.ExecuteAsync(operation, operationName);
    }
    
    /// <summary>
    /// 使用默认API重试策略执行操作
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>操作结果</returns>
    public static async Task<T> WithApiRetryAsync<T>(
        this Func<Task<T>> operation,
        ILogger logger,
        string operationName = "API Operation")
    {
        return await operation.WithRetryAsync(RetryPolicy.CreateDefaultApiPolicy(), logger, operationName);
    }
    
    /// <summary>
    /// 使用默认UI重试策略执行操作
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>操作结果</returns>
    public static async Task<T> WithUiRetryAsync<T>(
        this Func<Task<T>> operation,
        ILogger logger,
        string operationName = "UI Operation")
    {
        return await operation.WithRetryAsync(RetryPolicy.CreateDefaultUiPolicy(), logger, operationName);
    }
}

/// <summary>
/// 日志适配器，用于将ILogger转换为ILogger&lt;T&gt;
/// </summary>
/// <typeparam name="T">日志类型</typeparam>
public class LoggerAdapter<T> : ILogger<T>
{
    private readonly ILogger _logger;
    
    public LoggerAdapter(ILogger logger)
    {
        _logger = logger;
    }
    
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }
    
    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}