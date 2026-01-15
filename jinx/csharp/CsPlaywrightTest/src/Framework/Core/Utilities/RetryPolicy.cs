using System.Net;
using System.Net.Sockets;

namespace EnterpriseAutomationFramework.Core.Utilities;

/// <summary>
/// 重试策略
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxAttempts { get; set; } = 3;
    
    /// <summary>
    /// 重试间隔时间
    /// </summary>
    public TimeSpan DelayBetweenAttempts { get; set; } = TimeSpan.FromSeconds(1);
    
    /// <summary>
    /// 可重试的异常类型
    /// </summary>
    public List<Type> RetryableExceptions { get; set; } = new();
    
    /// <summary>
    /// 可重试的HTTP状态码
    /// </summary>
    public List<HttpStatusCode> RetryableStatusCodes { get; set; } = new();
    
    /// <summary>
    /// 是否启用指数退避
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = false;
    
    /// <summary>
    /// 指数退避的基数
    /// </summary>
    public double ExponentialBackoffMultiplier { get; set; } = 2.0;
    
    /// <summary>
    /// 最大延迟时间
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// 重试条件委托
    /// </summary>
    public Func<Exception, bool>? RetryCondition { get; set; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public RetryPolicy()
    {
        // 默认可重试的异常类型
        RetryableExceptions.AddRange(new[]
        {
            typeof(HttpRequestException),
            typeof(TaskCanceledException),
            typeof(TimeoutException),
            typeof(SocketException)
        });
        
        // 默认可重试的HTTP状态码
        RetryableStatusCodes.AddRange(new[]
        {
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout,
            HttpStatusCode.TooManyRequests
        });
    }
    
    /// <summary>
    /// 判断异常是否可重试
    /// </summary>
    /// <param name="exception">异常</param>
    /// <returns>是否可重试</returns>
    public bool ShouldRetry(Exception exception)
    {
        // 如果有自定义重试条件，优先使用
        if (RetryCondition != null)
        {
            return RetryCondition(exception);
        }
        
        // 检查异常类型
        var exceptionType = exception.GetType();
        if (RetryableExceptions.Any(type => type.IsAssignableFrom(exceptionType)))
        {
            return true;
        }
        
        // 检查内部异常
        if (exception.InnerException != null)
        {
            return ShouldRetry(exception.InnerException);
        }
        
        return false;
    }
    
    /// <summary>
    /// 判断HTTP状态码是否可重试
    /// </summary>
    /// <param name="statusCode">HTTP状态码</param>
    /// <returns>是否可重试</returns>
    public bool ShouldRetry(HttpStatusCode statusCode)
    {
        return RetryableStatusCodes.Contains(statusCode);
    }
    
    /// <summary>
    /// 计算延迟时间
    /// </summary>
    /// <param name="attemptNumber">尝试次数（从0开始）</param>
    /// <returns>延迟时间</returns>
    public TimeSpan CalculateDelay(int attemptNumber)
    {
        if (!UseExponentialBackoff)
        {
            return DelayBetweenAttempts;
        }
        
        var delay = TimeSpan.FromMilliseconds(
            DelayBetweenAttempts.TotalMilliseconds * Math.Pow(ExponentialBackoffMultiplier, attemptNumber));
        
        return delay > MaxDelay ? MaxDelay : delay;
    }
    
    /// <summary>
    /// 创建默认的API重试策略
    /// </summary>
    /// <returns>API重试策略</returns>
    public static RetryPolicy CreateDefaultApiPolicy()
    {
        return new RetryPolicy
        {
            MaxAttempts = 3,
            DelayBetweenAttempts = TimeSpan.FromSeconds(1),
            UseExponentialBackoff = true,
            ExponentialBackoffMultiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(30)
        };
    }
    
    /// <summary>
    /// 创建默认的UI重试策略
    /// </summary>
    /// <returns>UI重试策略</returns>
    public static RetryPolicy CreateDefaultUiPolicy()
    {
        return new RetryPolicy
        {
            MaxAttempts = 2,
            DelayBetweenAttempts = TimeSpan.FromSeconds(2),
            UseExponentialBackoff = false,
            RetryableExceptions = new List<Type>
            {
                typeof(TimeoutException),
                typeof(InvalidOperationException)
            }
        };
    }
    
    /// <summary>
    /// 创建自定义重试策略
    /// </summary>
    /// <param name="maxAttempts">最大重试次数</param>
    /// <param name="delay">延迟时间</param>
    /// <param name="retryableExceptions">可重试的异常类型</param>
    /// <returns>自定义重试策略</returns>
    public static RetryPolicy CreateCustomPolicy(int maxAttempts, TimeSpan delay, params Type[] retryableExceptions)
    {
        return new RetryPolicy
        {
            MaxAttempts = maxAttempts,
            DelayBetweenAttempts = delay,
            RetryableExceptions = retryableExceptions.ToList()
        };
    }
}