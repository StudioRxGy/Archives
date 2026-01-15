namespace CsPlaywrightXun.src.playwright.Core.Exceptions;

/// <summary>
/// 可重试异常
/// </summary>
public class RetryableException : TestFrameworkException
{
    /// <summary>
    /// 是否可重试
    /// </summary>
    public bool IsRetryable { get; }
    
    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="isRetryable">是否可重试</param>
    /// <param name="retryCount">重试次数</param>
    public RetryableException(string message, bool isRetryable = true, int retryCount = 0)
        : base(message)
    {
        IsRetryable = isRetryable;
        RetryCount = retryCount;
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    /// <param name="isRetryable">是否可重试</param>
    /// <param name="retryCount">重试次数</param>
    public RetryableException(string message, Exception innerException, bool isRetryable = true, int retryCount = 0)
        : base(message, innerException)
    {
        IsRetryable = isRetryable;
        RetryCount = retryCount;
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="component">组件名称</param>
    /// <param name="message">错误消息</param>
    /// <param name="isRetryable">是否可重试</param>
    /// <param name="retryCount">重试次数</param>
    public RetryableException(string testName, string component, string message, bool isRetryable = true, int retryCount = 0)
        : base(testName, component, message)
    {
        IsRetryable = isRetryable;
        RetryCount = retryCount;
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="component">组件名称</param>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    /// <param name="isRetryable">是否可重试</param>
    /// <param name="retryCount">重试次数</param>
    public RetryableException(string testName, string component, string message, Exception innerException, bool isRetryable = true, int retryCount = 0)
        : base(testName, component, message, innerException)
    {
        IsRetryable = isRetryable;
        RetryCount = retryCount;
    }
    
    /// <summary>
    /// 创建可重试异常
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="retryCount">重试次数</param>
    /// <returns>可重试异常</returns>
    public static RetryableException CreateRetryable(string message, int retryCount = 0)
    {
        return new RetryableException(message, true, retryCount);
    }
    
    /// <summary>
    /// 创建不可重试异常
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>不可重试异常</returns>
    public static RetryableException CreateNonRetryable(string message)
    {
        return new RetryableException(message, false, 0);
    }
    
    /// <summary>
    /// 从现有异常创建可重试异常
    /// </summary>
    /// <param name="exception">现有异常</param>
    /// <param name="isRetryable">是否可重试</param>
    /// <param name="retryCount">重试次数</param>
    /// <returns>可重试异常</returns>
    public static RetryableException FromException(Exception exception, bool isRetryable = true, int retryCount = 0)
    {
        return new RetryableException(exception.Message, exception, isRetryable, retryCount);
    }
}