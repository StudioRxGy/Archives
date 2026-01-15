using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace CsPlaywrightXun.src.playwright.Services.Api;

/// <summary>
/// API 客户端实现
/// </summary>
public class ApiClient : IApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<ApiClient> _logger;
    private readonly string _baseUrl;
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpClient">HTTP客户端</param>
    /// <param name="configuration">测试配置</param>
    /// <param name="logger">日志记录器</param>
    public ApiClient(HttpClient httpClient, TestConfiguration configuration, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiSettings = configuration?.Api ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseUrl = configuration.Environment.ApiBaseUrl;

        // 配置HttpClient
        _httpClient.Timeout = TimeSpan.FromMilliseconds(_apiSettings.Timeout);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// 发送 GET 请求
    /// </summary>
    /// <param name="endpoint">请求端点</param>
    /// <param name="headers">请求头</param>
    /// <returns>HTTP响应消息</returns>
    public async Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string>? headers = null)
    {
        return await GetAsync(endpoint, null, headers);
    }

    /// <summary>
    /// 发送 GET 请求（支持查询参数）
    /// </summary>
    /// <param name="endpoint">请求端点</param>
    /// <param name="queryParameters">查询参数</param>
    /// <param name="headers">请求头</param>
    /// <returns>HTTP响应消息</returns>
    public async Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string>? queryParameters, Dictionary<string, string>? headers)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("端点不能为空", nameof(endpoint));

        var endpointWithQuery = BuildEndpointWithQuery(endpoint, queryParameters);
        var fullUrl = BuildFullUrl(endpointWithQuery);
        _logger.LogInformation("发送GET请求到: {Url}", fullUrl);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
            AddHeaders(request, headers);

            var response = await SendRequestWithRetryAsync(request);
            await LogResponseAsync(response, "GET", fullUrl);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET请求失败: {Url}", fullUrl);
            throw new ApiException(endpoint, 0, $"GET请求失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 发送 POST 请求
    /// </summary>
    /// <param name="endpoint">请求端点</param>
    /// <param name="data">请求数据</param>
    /// <param name="headers">请求头</param>
    /// <returns>HTTP响应消息</returns>
    public async Task<HttpResponseMessage> PostAsync(string endpoint, object? data, Dictionary<string, string>? headers = null)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("端点不能为空", nameof(endpoint));

        var fullUrl = BuildFullUrl(endpoint);
        _logger.LogInformation("发送POST请求到: {Url}", fullUrl);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
            AddHeaders(request, headers);
            AddRequestBody(request, data);

            var response = await SendRequestWithRetryAsync(request);
            await LogResponseAsync(response, "POST", fullUrl);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST请求失败: {Url}", fullUrl);
            throw new ApiException(endpoint, 0, $"POST请求失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 发送 PUT 请求
    /// </summary>
    /// <param name="endpoint">请求端点</param>
    /// <param name="data">请求数据</param>
    /// <param name="headers">请求头</param>
    /// <returns>HTTP响应消息</returns>
    public async Task<HttpResponseMessage> PutAsync(string endpoint, object? data, Dictionary<string, string>? headers = null)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("端点不能为空", nameof(endpoint));

        var fullUrl = BuildFullUrl(endpoint);
        _logger.LogInformation("发送PUT请求到: {Url}", fullUrl);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, fullUrl);
            AddHeaders(request, headers);
            AddRequestBody(request, data);

            var response = await SendRequestWithRetryAsync(request);
            await LogResponseAsync(response, "PUT", fullUrl);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT请求失败: {Url}", fullUrl);
            throw new ApiException(endpoint, 0, $"PUT请求失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 发送 DELETE 请求
    /// </summary>
    /// <param name="endpoint">请求端点</param>
    /// <param name="headers">请求头</param>
    /// <returns>HTTP响应消息</returns>
    public async Task<HttpResponseMessage> DeleteAsync(string endpoint, Dictionary<string, string>? headers = null)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("端点不能为空", nameof(endpoint));

        var fullUrl = BuildFullUrl(endpoint);
        _logger.LogInformation("发送DELETE请求到: {Url}", fullUrl);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, fullUrl);
            AddHeaders(request, headers);

            var response = await SendRequestWithRetryAsync(request);
            await LogResponseAsync(response, "DELETE", fullUrl);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE请求失败: {Url}", fullUrl);
            throw new ApiException(endpoint, 0, $"DELETE请求失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 构建带查询参数的端点
    /// </summary>
    /// <param name="endpoint">端点</param>
    /// <param name="queryParameters">查询参数</param>
    /// <returns>带查询参数的端点</returns>
    private static string BuildEndpointWithQuery(string endpoint, Dictionary<string, string>? queryParameters)
    {
        if (queryParameters == null || !queryParameters.Any())
            return endpoint;

        var queryString = string.Join("&", 
            queryParameters.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
                          .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value ?? string.Empty)}"));

        if (string.IsNullOrEmpty(queryString))
            return endpoint;

        var separator = endpoint.Contains('?') ? "&" : "?";
        return $"{endpoint}{separator}{queryString}";
    }

    /// <summary>
    /// 构建完整URL
    /// </summary>
    /// <param name="endpoint">端点</param>
    /// <returns>完整URL</returns>
    private string BuildFullUrl(string endpoint)
    {
        if (Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
        {
            return endpoint;
        }

        var baseUri = new Uri(_baseUrl);
        var endpointUri = endpoint.StartsWith('/') ? endpoint : $"/{endpoint}";
        return new Uri(baseUri, endpointUri).ToString();
    }

    /// <summary>
    /// 添加请求头
    /// </summary>
    /// <param name="request">HTTP请求消息</param>
    /// <param name="headers">请求头字典</param>
    private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
    {
        if (headers == null) return;

        foreach (var header in headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key)) continue;

            // 尝试添加到请求头
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                // 如果添加到请求头失败，尝试添加到内容头
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }

    /// <summary>
    /// 添加请求体
    /// </summary>
    /// <param name="request">HTTP请求消息</param>
    /// <param name="data">请求数据</param>
    private static void AddRequestBody(HttpRequestMessage request, object? data)
    {
        if (data == null) return;

        string jsonContent;
        if (data is string stringData)
        {
            jsonContent = stringData;
        }
        else
        {
            jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }

        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// 发送请求并支持重试
    /// </summary>
    /// <param name="request">HTTP请求消息</param>
    /// <returns>HTTP响应消息</returns>
    private async Task<HttpResponseMessage> SendRequestWithRetryAsync(HttpRequestMessage request)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= _apiSettings.RetryCount)
        {
            try
            {
                // 克隆请求以支持重试
                using var clonedRequest = await CloneRequestAsync(request);
                var response = await _httpClient.SendAsync(clonedRequest);

                // 检查响应状态
                if (response.IsSuccessStatusCode || !ShouldRetry(response.StatusCode))
                {
                    return response;
                }

                // 如果不是最后一次尝试，记录警告并准备重试
                if (attempt < _apiSettings.RetryCount)
                {
                    _logger.LogWarning("请求失败，状态码: {StatusCode}，将在 {Delay}ms 后重试 (尝试 {Attempt}/{MaxAttempts})",
                        (int)response.StatusCode, _apiSettings.RetryDelay, attempt + 1, _apiSettings.RetryCount);
                    
                    response.Dispose();
                    await Task.Delay(_apiSettings.RetryDelay);
                }
                else
                {
                    // 最后一次尝试失败，抛出异常
                    var errorContent = await response.Content.ReadAsStringAsync();
                    response.Dispose();
                    throw new ApiException(request.RequestUri?.ToString() ?? "Unknown", (int)response.StatusCode, 
                        $"请求失败，状态码: {(int)response.StatusCode}，响应内容: {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                if (attempt < _apiSettings.RetryCount)
                {
                    _logger.LogWarning(ex, "网络请求异常，将在 {Delay}ms 后重试 (尝试 {Attempt}/{MaxAttempts})",
                        _apiSettings.RetryDelay, attempt + 1, _apiSettings.RetryCount);
                    await Task.Delay(_apiSettings.RetryDelay);
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                lastException = ex;
                if (attempt < _apiSettings.RetryCount)
                {
                    _logger.LogWarning(ex, "请求超时，将在 {Delay}ms 后重试 (尝试 {Attempt}/{MaxAttempts})",
                        _apiSettings.RetryDelay, attempt + 1, _apiSettings.RetryCount);
                    await Task.Delay(_apiSettings.RetryDelay);
                }
            }

            attempt++;
        }

        // 所有重试都失败了
        throw new ApiException(request.RequestUri?.ToString() ?? "Unknown", 0, 
            $"请求在 {_apiSettings.RetryCount + 1} 次尝试后仍然失败", lastException);
    }

    /// <summary>
    /// 克隆HTTP请求消息
    /// </summary>
    /// <param name="original">原始请求</param>
    /// <returns>克隆的请求</returns>
    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        // 复制请求头
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // 复制内容
        if (original.Content != null)
        {
            var contentBytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            // 复制内容头
            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    /// <summary>
    /// 判断是否应该重试
    /// </summary>
    /// <param name="statusCode">HTTP状态码</param>
    /// <returns>是否应该重试</returns>
    private static bool ShouldRetry(System.Net.HttpStatusCode statusCode)
    {
        // 对于以下状态码进行重试
        return statusCode switch
        {
            System.Net.HttpStatusCode.RequestTimeout => true,
            System.Net.HttpStatusCode.InternalServerError => true,
            System.Net.HttpStatusCode.BadGateway => true,
            System.Net.HttpStatusCode.ServiceUnavailable => true,
            System.Net.HttpStatusCode.GatewayTimeout => true,
            System.Net.HttpStatusCode.TooManyRequests => true,
            _ => false
        };
    }

    /// <summary>
    /// 记录响应日志
    /// </summary>
    /// <param name="response">HTTP响应</param>
    /// <param name="method">HTTP方法</param>
    /// <param name="url">请求URL</param>
    private async Task LogResponseAsync(HttpResponseMessage response, string method, string url)
    {
        var statusCode = (int)response.StatusCode;
        var responseTime = response.Headers.Date?.ToString() ?? "Unknown";

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("{Method} 请求成功: {Url}, 状态码: {StatusCode}, 响应时间: {ResponseTime}",
                method, url, statusCode, responseTime);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("{Method} 请求失败: {Url}, 状态码: {StatusCode}, 错误内容: {ErrorContent}",
                method, url, statusCode, errorContent);
        }
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
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}