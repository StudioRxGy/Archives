using Microsoft.Extensions.Logging;

namespace CsPlaywrightXun.src.playwright.Core.Utilities;

/// <summary>
/// 重试执行器
/// </summary>
public class RetryExecutor
{
    private readonly RetryPolicy _policy;
    private readonly ILogger<RetryExecutor> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="policy">重试策略</param>
    /// <param name="logger">日志记录器</param>
    public RetryExecutor(RetryPolicy policy, ILogger<RetryExecutor> logger)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// 执行带重试的操作
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <returns>操作结果</returns>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName = "Operation")
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));
        
        var attempt = 0;
        Exception? lastException = null;
        
        while (attempt <= _policy.MaxAttempts)
        {
            try
            {
                _logger.LogDebug("执行操作 '{OperationName}' - 尝试 {Attempt}/{MaxAttempts}", 
                    operationName, attempt + 1, _policy.MaxAttempts + 1);
                
                var result = await operation();
                
                if (attempt > 0)
                {
                    _logger.LogInformation("操作 '{OperationName}' 在第 {Attempt} 次尝试后成功", 
                        operationName, attempt + 1);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                // 检查是否应该重试
                if (attempt >= _policy.MaxAttempts || !_policy.ShouldRetry(ex))
                {
                    _logger.LogError(ex, "操作 '{OperationName}' 最终失败，尝试次数: {Attempts}", 
                        operationName, attempt + 1);
                    throw;
                }
                
                // 计算延迟时间
                var delay = _policy.CalculateDelay(attempt);
                
                _logger.LogWarning(ex, "操作 '{OperationName}' 失败，将在 {Delay}ms 后重试 (尝试 {Attempt}/{MaxAttempts})",
                    operationName, delay.TotalMilliseconds, attempt + 1, _policy.MaxAttempts + 1);
                
                await Task.Delay(delay);
                attempt++;
            }
        }
        
        // 这里不应该到达，但为了编译器满意
        throw lastException ?? new InvalidOperationException($"操作 '{operationName}' 执行失败");
    }
    
    /// <summary>
    /// 执行带重试的操作（无返回值）
    /// </summary>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    public async Task ExecuteAsync(Func<Task> operation, string operationName = "Operation")
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true; // 返回一个虚拟值
        }, operationName);
    }
    
    /// <summary>
    /// 执行带重试的同步操作
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <returns>操作结果</returns>
    public async Task<T> ExecuteAsync<T>(Func<T> operation, string operationName = "Operation")
    {
        return await ExecuteAsync(() => Task.FromResult(operation()), operationName);
    }
    
    /// <summary>
    /// 执行带重试的同步操作（无返回值）
    /// </summary>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    public async Task ExecuteAsync(Action operation, string operationName = "Operation")
    {
        await ExecuteAsync(() =>
        {
            operation();
            return Task.CompletedTask;
        }, operationName);
    }
    
    /// <summary>
    /// 创建重试执行器
    /// </summary>
    /// <param name="policy">重试策略</param>
    /// <param name="logger">日志记录器</param>
    /// <returns>重试执行器</returns>
    public static RetryExecutor Create(RetryPolicy policy, ILogger<RetryExecutor> logger)
    {
        return new RetryExecutor(policy, logger);
    }
    
    /// <summary>
    /// 创建带默认API策略的重试执行器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <returns>重试执行器</returns>
    public static RetryExecutor CreateForApi(ILogger<RetryExecutor> logger)
    {
        return new RetryExecutor(RetryPolicy.CreateDefaultApiPolicy(), logger);
    }
    
    /// <summary>
    /// 创建带默认UI策略的重试执行器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <returns>重试执行器</returns>
    public static RetryExecutor CreateForUi(ILogger<RetryExecutor> logger)
    {
        return new RetryExecutor(RetryPolicy.CreateDefaultUiPolicy(), logger);
    }
}