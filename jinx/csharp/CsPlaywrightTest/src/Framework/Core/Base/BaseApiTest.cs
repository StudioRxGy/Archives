using Microsoft.Extensions.Logging;
using EnterpriseAutomationFramework.Core.Configuration;
using EnterpriseAutomationFramework.Core.Interfaces;
using EnterpriseAutomationFramework.Services.Api;

namespace EnterpriseAutomationFramework.Core.Base;

/// <summary>
/// API 测试基类
/// 提供统一的 API 测试基础功能，包括客户端管理、配置管理和日志记录
/// </summary>
public abstract class BaseApiTest : IDisposable
{
    /// <summary>
    /// API 客户端
    /// </summary>
    protected readonly IApiClient ApiClient;
    
    /// <summary>
    /// 测试配置
    /// </summary>
    protected readonly TestConfiguration Configuration;
    
    /// <summary>
    /// 日志记录器
    /// </summary>
    protected readonly ILogger Logger;
    
    /// <summary>
    /// API 服务
    /// </summary>
    protected readonly ApiService ApiService;
    
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="apiClient">API客户端</param>
    /// <param name="configuration">测试配置</param>
    /// <param name="logger">日志记录器</param>
    protected BaseApiTest(IApiClient apiClient, TestConfiguration configuration, ILogger logger)
    {
        ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // 创建 API 服务实例
        ApiService = new ApiService(ApiClient, CreateApiServiceLogger());
        
        Logger.LogInformation("BaseApiTest 初始化完成，环境: {Environment}, API基础URL: {ApiBaseUrl}", 
            Configuration.Environment.Name, Configuration.Environment.ApiBaseUrl);
    }

    /// <summary>
    /// 执行 API 测试
    /// 提供统一的 API 请求执行和日志记录
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="request">API请求</param>
    /// <param name="testName">测试名称（用于日志记录）</param>
    /// <returns>API响应</returns>
    protected async Task<ApiResponse<T>> ExecuteApiTestAsync<T>(ApiRequest request, string? testName = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var currentTestName = testName ?? GetCurrentTestName();
        
        try
        {
            Logger.LogInformation("开始执行API测试: {TestName}, 方法: {Method}, 端点: {Endpoint}", 
                currentTestName, request.Method, request.Endpoint);

            var response = await ApiService.SendRequestAsync<T>(request);
            
            Logger.LogInformation("API测试执行完成: {TestName}, 状态码: {StatusCode}, 响应时间: {ResponseTime}ms", 
                currentTestName, response.StatusCode, response.ResponseTime.TotalMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "API测试执行失败: {TestName}, 方法: {Method}, 端点: {Endpoint}", 
                currentTestName, request.Method, request.Endpoint);
            throw;
        }
    }

    /// <summary>
    /// 执行 API 测试（非泛型版本）
    /// </summary>
    /// <param name="request">API请求</param>
    /// <param name="testName">测试名称（用于日志记录）</param>
    /// <returns>API响应</returns>
    protected async Task<ApiResponse> ExecuteApiTestAsync(ApiRequest request, string? testName = null)
    {
        var response = await ExecuteApiTestAsync<object>(request, testName);
        return new ApiResponse
        {
            StatusCode = response.StatusCode,
            Data = response.Data,
            RawContent = response.RawContent,
            ResponseTime = response.ResponseTime,
            Headers = response.Headers
        };
    }

    /// <summary>
    /// 验证 API 响应
    /// 提供统一的响应验证和日志记录
    /// </summary>
    /// <param name="response">API响应</param>
    /// <param name="validation">验证规则</param>
    /// <param name="testName">测试名称（用于日志记录）</param>
    /// <returns>验证结果</returns>
    protected ValidationResult ValidateApiResponse(ApiResponse response, ApiValidation validation, string? testName = null)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));
        if (validation == null)
            throw new ArgumentNullException(nameof(validation));

        var currentTestName = testName ?? GetCurrentTestName();
        
        Logger.LogInformation("开始验证API响应: {TestName}, 状态码: {StatusCode}", 
            currentTestName, response.StatusCode);

        var result = ApiService.ValidateResponse(response, validation);
        
        if (result.IsValid)
        {
            Logger.LogInformation("API响应验证通过: {TestName}", currentTestName);
        }
        else
        {
            Logger.LogWarning("API响应验证失败: {TestName}, 错误数量: {ErrorCount}, 错误详情: {Errors}", 
                currentTestName, result.Errors.Count, string.Join("; ", result.Errors));
        }

        return result;
    }

    /// <summary>
    /// 创建标准的 GET 请求
    /// </summary>
    /// <param name="endpoint">请求端点</param>
    /// <param name="queryParameters">查询参数</param>
    /// <param name="headers">请求头</param>
    /// <returns>API请求对象</returns>
    protected ApiRequest CreateGetRequest(string endpoint, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null)
    {
        return new ApiRequest
        {
            Method = "GET",
            Endpoint = endpoint,
            QueryParameters = queryParameters ?? new Dictionary<string, string>(),
            Headers = headers ?? new Dictionary<string, string>()
        };
    }

    /// <summary>
    /// 创建标准的 POST 请求
    /// </summary>
    /// <param name="endpoint">请求端点</param>
    /// <param name="body">请求体</param>
    /// <param name="headers">请求头</param>
    /// <returns>API请求对象</returns>
    protected ApiRequest CreatePostRequest(string endpoint, object? body = null, Dictionary<string, string>? headers = null)
    {
        return new ApiRequest
        {
            Method = "POST",
            Endpoint = endpoint,
            Body = body,
            Headers = headers ?? new Dictionary<string, string>()
        };
    }

    /// <summary>
    /// 创建标准的 PUT 请求
    /// </summary>
    /// <param name="endpoint">请求端点</param>
    /// <param name="body">请求体</param>
    /// <param name="headers">请求头</param>
    /// <returns>API请求对象</returns>
    protected ApiRequest CreatePutRequest(string endpoint, object? body = null, Dictionary<string, string>? headers = null)
    {
        return new ApiRequest
        {
            Method = "PUT",
            Endpoint = endpoint,
            Body = body,
            Headers = headers ?? new Dictionary<string, string>()
        };
    }

    /// <summary>
    /// 创建标准的 DELETE 请求
    /// </summary>
    /// <param name="endpoint">请求端点</param>
    /// <param name="headers">请求头</param>
    /// <returns>API请求对象</returns>
    protected ApiRequest CreateDeleteRequest(string endpoint, Dictionary<string, string>? headers = null)
    {
        return new ApiRequest
        {
            Method = "DELETE",
            Endpoint = endpoint,
            Headers = headers ?? new Dictionary<string, string>()
        };
    }

    /// <summary>
    /// 创建基础的 API 验证规则
    /// </summary>
    /// <param name="expectedStatusCode">期望的状态码</param>
    /// <param name="maxResponseTime">最大响应时间</param>
    /// <returns>API验证规则</returns>
    protected ApiValidation CreateBasicValidation(int expectedStatusCode = 200, TimeSpan? maxResponseTime = null)
    {
        return new ApiValidation
        {
            ExpectedStatusCode = expectedStatusCode,
            MaxResponseTime = maxResponseTime ?? TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// 获取当前测试名称
    /// 尝试从调用堆栈中获取测试方法名称
    /// </summary>
    /// <returns>测试名称</returns>
    protected virtual string GetCurrentTestName()
    {
        var stackTrace = new System.Diagnostics.StackTrace();
        var frames = stackTrace.GetFrames();
        
        // 查找测试方法（通常包含 Test 关键字或有测试特性）
        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            if (method != null && 
                (method.Name.Contains("Test") || 
                 method.GetCustomAttributes(typeof(Xunit.FactAttribute), false).Any() ||
                 method.GetCustomAttributes(typeof(Xunit.TheoryAttribute), false).Any()))
            {
                return $"{method.DeclaringType?.Name}.{method.Name}";
            }
        }
        
        return "UnknownTest";
    }

    /// <summary>
    /// 创建 API 服务专用的日志记录器
    /// </summary>
    /// <returns>API服务日志记录器</returns>
    private ILogger<ApiService> CreateApiServiceLogger()
    {
        // 创建一个包装器，将通用 ILogger 转换为 ILogger<ApiService>
        return new ApiServiceLoggerWrapper(Logger);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="disposing">是否正在释放</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            Logger.LogInformation("BaseApiTest 正在释放资源");
            
            ApiService?.Dispose();
            
            if (ApiClient is IDisposable disposableClient)
            {
                disposableClient.Dispose();
            }
            
            _disposed = true;
            Logger.LogInformation("BaseApiTest 资源释放完成");
        }
    }
}

/// <summary>
/// API 服务日志记录器包装器
/// 将通用 ILogger 包装为 ILogger&lt;ApiService&gt;
/// </summary>
internal class ApiServiceLoggerWrapper : ILogger<ApiService>
{
    private readonly ILogger _logger;

    public ApiServiceLoggerWrapper(ILogger logger)
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