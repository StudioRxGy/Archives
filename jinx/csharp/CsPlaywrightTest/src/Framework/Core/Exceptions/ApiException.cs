namespace EnterpriseAutomationFramework.Core.Exceptions;

/// <summary>
/// API 异常
/// </summary>
public class ApiException : TestFrameworkException
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int StatusCode { get; }
    
    /// <summary>
    /// 请求端点
    /// </summary>
    public string Endpoint { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="endpoint">请求端点</param>
    /// <param name="statusCode">状态码</param>
    /// <param name="message">错误消息</param>
    public ApiException(string testName, string endpoint, int statusCode, string message)
        : base(testName, "ApiService", message)
    {
        StatusCode = statusCode;
        Endpoint = endpoint;
    }
    
    /// <summary>
    /// 构造函数 - 端点和状态码
    /// </summary>
    /// <param name="endpoint">请求端点</param>
    /// <param name="statusCode">状态码</param>
    public ApiException(string endpoint, int statusCode)
        : base($"API 请求失败: {endpoint}, 状态码: {statusCode}")
    {
        Endpoint = endpoint;
        StatusCode = statusCode;
    }
    
    /// <summary>
    /// 构造函数 - 端点、状态码和消息
    /// </summary>
    /// <param name="endpoint">请求端点</param>
    /// <param name="statusCode">状态码</param>
    /// <param name="message">错误消息</param>
    public ApiException(string endpoint, int statusCode, string message)
        : base(message)
    {
        Endpoint = endpoint;
        StatusCode = statusCode;
    }
    
    /// <summary>
    /// 构造函数 - 端点、状态码、消息和内部异常
    /// </summary>
    /// <param name="endpoint">请求端点</param>
    /// <param name="statusCode">状态码</param>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public ApiException(string endpoint, int statusCode, string message, Exception innerException)
        : base(message, innerException)
    {
        Endpoint = endpoint;
        StatusCode = statusCode;
    }
}