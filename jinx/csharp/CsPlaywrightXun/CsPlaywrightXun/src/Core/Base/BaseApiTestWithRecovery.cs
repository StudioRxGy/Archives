using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Core.Utilities;
using CsPlaywrightXun.src.playwright.Services.Api;

namespace CsPlaywrightXun.src.playwright.Core.Base;

/// <summary>
/// 带错误恢复功能的API测试基类
/// </summary>
public abstract class BaseApiTestWithRecovery : BaseApiTest
{
    protected readonly ErrorRecoveryStrategy _errorRecoveryStrategy;
    protected readonly ErrorRecoveryContext _recoveryContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="apiClient">API客户端</param>
    /// <param name="configuration">测试配置</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="errorRecoveryStrategy">错误恢复策略</param>
    protected BaseApiTestWithRecovery(
        IApiClient apiClient,
        TestConfiguration configuration,
        ILogger logger,
        ErrorRecoveryStrategy? errorRecoveryStrategy = null)
        : base(apiClient, configuration, logger)
    {
        _errorRecoveryStrategy = errorRecoveryStrategy ?? ErrorRecoveryStrategy.CreateForApi(
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ErrorRecoveryStrategy>());
        
        _recoveryContext = ErrorRecoveryContext.ForApi(ApiClient, GetType().Name);
    }

    /// <summary>
    /// 带错误恢复的API请求执行
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="request">API请求</param>
    /// <returns>API响应</returns>
    protected async Task<ApiResponse<T>> ExecuteApiRequestWithRecoveryAsync<T>(ApiRequest request)
    {
        return await _errorRecoveryStrategy.ExecuteWithApiRetryRecoveryAsync(
            ApiClient,
            async () => await ExecuteApiTestAsync<T>(request),
            $"ApiRequest_{request.Method}_{request.Endpoint}");
    }

    /// <summary>
    /// 带错误恢复的API请求执行（非泛型）
    /// </summary>
    /// <param name="request">API请求</param>
    /// <returns>API响应</returns>
    protected async Task<ApiResponse> ExecuteApiRequestWithRecoveryAsync(ApiRequest request)
    {
        var response = await ExecuteApiRequestWithRecoveryAsync<object>(request);
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
    /// 带错误恢复的GET请求
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="endpoint">API端点</param>
    /// <param name="headers">请求头</param>
    /// <param name="queryParameters">查询参数</param>
    /// <returns>API响应</returns>
    protected async Task<ApiResponse<T>> GetWithRecoveryAsync<T>(
        string endpoint,
        Dictionary<string, string>? headers = null,
        Dictionary<string, string>? queryParameters = null)
    {
        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = endpoint,
            Headers = headers ?? new Dictionary<string, string>(),
            QueryParameters = queryParameters ?? new Dictionary<string, string>()
        };

        return await ExecuteApiRequestWithRecoveryAsync<T>(request);
    }

    /// <summary>
    /// 带错误恢复的POST请求
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="endpoint">API端点</param>
    /// <param name="body">请求体</param>
    /// <param name="headers">请求头</param>
    /// <returns>API响应</returns>
    protected async Task<ApiResponse<T>> PostWithRecoveryAsync<T>(
        string endpoint,
        object? body = null,
        Dictionary<string, string>? headers = null)
    {
        var request = new ApiRequest
        {
            Method = "POST",
            Endpoint = endpoint,
            Body = body,
            Headers = headers ?? new Dictionary<string, string>()
        };

        return await ExecuteApiRequestWithRecoveryAsync<T>(request);
    }

    /// <summary>
    /// 带错误恢复的PUT请求
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="endpoint">API端点</param>
    /// <param name="body">请求体</param>
    /// <param name="headers">请求头</param>
    /// <returns>API响应</returns>
    protected async Task<ApiResponse<T>> PutWithRecoveryAsync<T>(
        string endpoint,
        object? body = null,
        Dictionary<string, string>? headers = null)
    {
        var request = new ApiRequest
        {
            Method = "PUT",
            Endpoint = endpoint,
            Body = body,
            Headers = headers ?? new Dictionary<string, string>()
        };

        return await ExecuteApiRequestWithRecoveryAsync<T>(request);
    }

    /// <summary>
    /// 带错误恢复的DELETE请求
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="endpoint">API端点</param>
    /// <param name="headers">请求头</param>
    /// <returns>API响应</returns>
    protected async Task<ApiResponse<T>> DeleteWithRecoveryAsync<T>(
        string endpoint,
        Dictionary<string, string>? headers = null)
    {
        var request = new ApiRequest
        {
            Method = "DELETE",
            Endpoint = endpoint,
            Headers = headers ?? new Dictionary<string, string>()
        };

        return await ExecuteApiRequestWithRecoveryAsync<T>(request);
    }

    /// <summary>
    /// 带错误恢复的API响应验证
    /// </summary>
    /// <param name="response">API响应</param>
    /// <param name="validation">验证规则</param>
    /// <returns>验证结果</returns>
    protected async Task<ValidationResult> ValidateResponseWithRecoveryAsync(
        ApiResponse response,
        ApiValidation validation)
    {
        return await _errorRecoveryStrategy.ExecuteWithApiRetryRecoveryAsync(
            ApiClient,
            async () =>
            {
                // 创建临时的ApiService来执行验证
                using var apiService = new ApiService(ApiClient, LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ApiService>());
                return apiService.ValidateResponse(response, validation);
            },
            $"ValidateResponse_{response.StatusCode}");
    }

    /// <summary>
    /// 执行自定义API操作并带错误恢复
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>操作结果</returns>
    protected async Task<T> ExecuteApiOperationWithRecoveryAsync<T>(
        Func<Task<T>> operation,
        string operationName)
    {
        return await _errorRecoveryStrategy.ExecuteWithApiRetryRecoveryAsync(
            ApiClient,
            operation,
            operationName);
    }

    /// <summary>
    /// 执行自定义API操作并带错误恢复（无返回值）
    /// </summary>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称</param>
    protected async Task ExecuteApiOperationWithRecoveryAsync(
        Func<Task> operation,
        string operationName)
    {
        await _errorRecoveryStrategy.ExecuteWithApiRetryRecoveryAsync(
            ApiClient,
            operation,
            operationName);
    }

    /// <summary>
    /// 批量执行API请求并带错误恢复
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="requests">API请求列表</param>
    /// <param name="maxConcurrency">最大并发数</param>
    /// <returns>API响应列表</returns>
    protected async Task<List<ApiResponse<T>>> ExecuteBatchRequestsWithRecoveryAsync<T>(
        List<ApiRequest> requests,
        int maxConcurrency = 5)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = requests.Select(async request =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await ExecuteApiRequestWithRecoveryAsync<T>(request);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    /// <summary>
    /// 更新恢复上下文
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="operationName">操作名称</param>
    protected void UpdateRecoveryContext(string? testName = null, string? operationName = null)
    {
        if (!string.IsNullOrEmpty(testName))
            _recoveryContext.TestName = testName;
        
        if (!string.IsNullOrEmpty(operationName))
            _recoveryContext.OperationName = operationName;
    }

    /// <summary>
    /// 获取恢复统计信息
    /// </summary>
    /// <returns>恢复统计信息</returns>
    protected string GetRecoveryStatistics()
    {
        return _recoveryContext.GetDescription();
    }
}