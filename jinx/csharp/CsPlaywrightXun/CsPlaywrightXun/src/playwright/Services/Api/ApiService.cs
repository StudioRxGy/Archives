using System.Text.Json;
using System.Text.RegularExpressions;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace CsPlaywrightXun.src.playwright.Services.Api;

/// <summary>
/// API 服务实现
/// </summary>
public class ApiService : IDisposable
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<ApiService> _logger;
    private readonly IApiPerformanceMonitor? _performanceMonitor;
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="apiClient">API客户端</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="performanceMonitor">性能监控器（可选）</param>
    public ApiService(IApiClient apiClient, ILogger<ApiService> logger, IApiPerformanceMonitor? performanceMonitor = null)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _performanceMonitor = performanceMonitor;
    }

    /// <summary>
    /// 发送API请求并返回强类型响应
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="request">API请求</param>
    /// <returns>API响应</returns>
    public async Task<ApiResponse<T>> SendRequestAsync<T>(ApiRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var startTime = DateTime.UtcNow;
        HttpResponseMessage? httpResponse = null;

        try
        {
            _logger.LogInformation("发送 {Method} 请求到: {Endpoint}", request.Method, request.Endpoint);

            // 根据HTTP方法发送请求
            httpResponse = request.Method.ToUpperInvariant() switch
            {
                "GET" => await _apiClient.GetAsync(request.BuildEndpointWithQuery(), request.Headers),
                "POST" => await _apiClient.PostAsync(request.Endpoint, request.Body, request.Headers),
                "PUT" => await _apiClient.PutAsync(request.Endpoint, request.Body, request.Headers),
                "DELETE" => await _apiClient.DeleteAsync(request.Endpoint, request.Headers),
                _ => throw new ArgumentException($"不支持的HTTP方法: {request.Method}")
            };

            var endTime = DateTime.UtcNow;
            var responseTime = endTime - startTime;

            // 读取响应内容
            var rawContent = await httpResponse.Content.ReadAsStringAsync();

            // 构建响应对象
            var response = new ApiResponse<T>
            {
                StatusCode = (int)httpResponse.StatusCode,
                RawContent = rawContent,
                ResponseTime = responseTime,
                Headers = ExtractHeaders(httpResponse)
            };

            // 尝试反序列化响应数据
            if (!string.IsNullOrWhiteSpace(rawContent) && httpResponse.IsSuccessStatusCode)
            {
                try
                {
                    if (typeof(T) == typeof(string))
                    {
                        response.Data = (T)(object)rawContent;
                    }
                    else
                    {
                        response.Data = JsonSerializer.Deserialize<T>(rawContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "响应内容反序列化失败: {Content}", rawContent);
                    // 反序列化失败时，Data保持为null，但不抛出异常
                }
            }

            _logger.LogInformation("API请求完成: {Method} {Endpoint}, 状态码: {StatusCode}, 响应时间: {ResponseTime}ms",
                request.Method, request.Endpoint, response.StatusCode, responseTime.TotalMilliseconds);

            // 记录性能指标
            _performanceMonitor?.RecordMetric(
                request.Endpoint, 
                request.Method, 
                responseTime, 
                response.StatusCode,
                GetRequestSize(request),
                rawContent.Length);

            return response;
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            var responseTime = endTime - startTime;

            _logger.LogError(ex, "API请求失败: {Method} {Endpoint}, 响应时间: {ResponseTime}ms",
                request.Method, request.Endpoint, responseTime.TotalMilliseconds);

            // 如果有HTTP响应，尝试读取错误内容
            string? errorContent = null;
            if (httpResponse != null)
            {
                try
                {
                    errorContent = await httpResponse.Content.ReadAsStringAsync();
                }
                catch
                {
                    // 忽略读取错误内容时的异常
                }
            }

            throw new ApiException(request.Endpoint, httpResponse?.StatusCode != null ? (int)httpResponse.StatusCode : 0,
                $"API请求失败: {ex.Message}{(errorContent != null ? $", 错误内容: {errorContent}" : "")}", ex);
        }
        finally
        {
            httpResponse?.Dispose();
        }
    }

    /// <summary>
    /// 发送API请求并返回非泛型响应
    /// </summary>
    /// <param name="request">API请求</param>
    /// <returns>API响应</returns>
    public async Task<ApiResponse> SendRequestAsync(ApiRequest request)
    {
        var response = await SendRequestAsync<object>(request);
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
    /// 验证API响应
    /// </summary>
    /// <param name="response">API响应</param>
    /// <param name="validation">验证规则</param>
    /// <returns>验证结果</returns>
    public ValidationResult ValidateResponse(ApiResponse response, ApiValidation validation)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));
        if (validation == null)
            throw new ArgumentNullException(nameof(validation));

        var result = new ValidationResult { IsValid = true };

        try
        {
            // 验证状态码
            ValidateStatusCode(response, validation, result);

            // 验证响应时间
            ValidateResponseTime(response, validation, result);

            // 验证内容包含
            ValidateContentContains(response, validation, result);

            // 验证内容不包含
            ValidateContentNotContains(response, validation, result);

            // 验证内容匹配正则表达式
            ValidateContentRegex(response, validation, result);

            // 验证响应头
            ValidateHeaders(response, validation, result);

            // 验证JSON Path
            ValidateJsonPath(response, validation, result);

            // 验证JSON Schema（如果提供）
            ValidateJsonSchema(response, validation, result);

            _logger.LogInformation("API响应验证完成: {IsValid}, 错误数量: {ErrorCount}", result.IsValid, result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API响应验证过程中发生异常");
            result.IsValid = false;
            result.Errors.Add($"验证过程中发生异常: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 获取API性能统计
    /// </summary>
    /// <param name="endpoint">API端点</param>
    /// <param name="method">HTTP方法</param>
    /// <returns>性能统计</returns>
    public ApiPerformanceStatistics? GetPerformanceStatistics(string endpoint, string method)
    {
        return _performanceMonitor?.GetStatistics(endpoint, method);
    }

    /// <summary>
    /// 获取所有API性能统计
    /// </summary>
    /// <returns>所有性能统计</returns>
    public List<ApiPerformanceStatistics> GetAllPerformanceStatistics()
    {
        return _performanceMonitor?.GetAllStatistics() ?? new List<ApiPerformanceStatistics>();
    }

    /// <summary>
    /// 获取API性能报告
    /// </summary>
    /// <param name="timeRangeHours">时间范围（小时）</param>
    /// <returns>性能报告</returns>
    public ApiPerformanceReport GetPerformanceReport(int timeRangeHours = 24)
    {
        return _performanceMonitor?.GetPerformanceReport(timeRangeHours) ?? new ApiPerformanceReport
        {
            GeneratedAt = DateTime.UtcNow,
            TimeRangeHours = timeRangeHours,
            TotalRequests = 0,
            Statistics = new List<ApiPerformanceStatistics>()
        };
    }

    /// <summary>
    /// 清除性能数据
    /// </summary>
    /// <param name="endpoint">API端点</param>
    /// <param name="method">HTTP方法</param>
    public void ClearPerformanceMetrics(string endpoint, string method)
    {
        _performanceMonitor?.ClearMetrics(endpoint, method);
    }

    /// <summary>
    /// 清除所有性能数据
    /// </summary>
    public void ClearAllPerformanceMetrics()
    {
        _performanceMonitor?.ClearAllMetrics();
    }

    /// <summary>
    /// 验证状态码
    /// </summary>
    private void ValidateStatusCode(ApiResponse response, ApiValidation validation, ValidationResult result)
    {
        if (validation.ExpectedStatusCode.HasValue)
        {
            if (response.StatusCode != validation.ExpectedStatusCode.Value)
            {
                result.IsValid = false;
                result.Errors.Add($"状态码验证失败: 期望 {validation.ExpectedStatusCode.Value}, 实际 {response.StatusCode}");
            }
        }

        if (validation.ExpectedStatusCodes?.Any() == true)
        {
            if (!validation.ExpectedStatusCodes.Contains(response.StatusCode))
            {
                result.IsValid = false;
                result.Errors.Add($"状态码验证失败: 期望 [{string.Join(", ", validation.ExpectedStatusCodes)}], 实际 {response.StatusCode}");
            }
        }
    }

    /// <summary>
    /// 验证响应时间
    /// </summary>
    private void ValidateResponseTime(ApiResponse response, ApiValidation validation, ValidationResult result)
    {
        if (validation.MaxResponseTime.HasValue)
        {
            if (response.ResponseTime > validation.MaxResponseTime.Value)
            {
                result.IsValid = false;
                result.Errors.Add($"响应时间验证失败: 期望小于 {validation.MaxResponseTime.Value.TotalMilliseconds}ms, 实际 {response.ResponseTime.TotalMilliseconds}ms");
            }
        }

        if (validation.MinResponseTime.HasValue)
        {
            if (response.ResponseTime < validation.MinResponseTime.Value)
            {
                result.IsValid = false;
                result.Errors.Add($"响应时间验证失败: 期望大于 {validation.MinResponseTime.Value.TotalMilliseconds}ms, 实际 {response.ResponseTime.TotalMilliseconds}ms");
            }
        }
    }

    /// <summary>
    /// 验证内容包含
    /// </summary>
    private void ValidateContentContains(ApiResponse response, ApiValidation validation, ValidationResult result)
    {
        if (!string.IsNullOrEmpty(validation.ContentContains))
        {
            if (!response.RawContent.Contains(validation.ContentContains, StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Errors.Add($"内容包含验证失败: 响应内容不包含 '{validation.ContentContains}'");
            }
        }

        if (validation.ContentContainsList?.Any() == true)
        {
            foreach (var content in validation.ContentContainsList)
            {
                if (!response.RawContent.Contains(content, StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Errors.Add($"内容包含验证失败: 响应内容不包含 '{content}'");
                }
            }
        }
    }

    /// <summary>
    /// 验证内容不包含
    /// </summary>
    private void ValidateContentNotContains(ApiResponse response, ApiValidation validation, ValidationResult result)
    {
        if (!string.IsNullOrEmpty(validation.ContentNotContains))
        {
            if (response.RawContent.Contains(validation.ContentNotContains, StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Errors.Add($"内容不包含验证失败: 响应内容包含 '{validation.ContentNotContains}'");
            }
        }

        if (validation.ContentNotContainsList?.Any() == true)
        {
            foreach (var content in validation.ContentNotContainsList)
            {
                if (response.RawContent.Contains(content, StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Errors.Add($"内容不包含验证失败: 响应内容包含 '{content}'");
                }
            }
        }
    }

    /// <summary>
    /// 验证内容正则表达式匹配
    /// </summary>
    private void ValidateContentRegex(ApiResponse response, ApiValidation validation, ValidationResult result)
    {
        if (!string.IsNullOrEmpty(validation.ContentRegex))
        {
            try
            {
                var regex = new Regex(validation.ContentRegex, RegexOptions.IgnoreCase);
                if (!regex.IsMatch(response.RawContent))
                {
                    result.IsValid = false;
                    result.Errors.Add($"内容正则表达式验证失败: 响应内容不匹配正则表达式 '{validation.ContentRegex}'");
                }
            }
            catch (ArgumentException ex)
            {
                result.IsValid = false;
                result.Errors.Add($"内容正则表达式验证失败: 正则表达式格式错误 '{validation.ContentRegex}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 验证响应头
    /// </summary>
    private void ValidateHeaders(ApiResponse response, ApiValidation validation, ValidationResult result)
    {
        if (validation.RequiredHeaders?.Any() == true)
        {
            foreach (var requiredHeader in validation.RequiredHeaders)
            {
                if (!response.Headers.ContainsKey(requiredHeader.Key))
                {
                    result.IsValid = false;
                    result.Errors.Add($"响应头验证失败: 缺少必需的响应头 '{requiredHeader.Key}'");
                }
                else if (!string.IsNullOrEmpty(requiredHeader.Value) && 
                         response.Headers[requiredHeader.Key] != requiredHeader.Value)
                {
                    result.IsValid = false;
                    result.Errors.Add($"响应头验证失败: 响应头 '{requiredHeader.Key}' 值不匹配, 期望 '{requiredHeader.Value}', 实际 '{response.Headers[requiredHeader.Key]}'");
                }
            }
        }

        if (validation.ForbiddenHeaders?.Any() == true)
        {
            foreach (var forbiddenHeader in validation.ForbiddenHeaders)
            {
                if (response.Headers.ContainsKey(forbiddenHeader))
                {
                    result.IsValid = false;
                    result.Errors.Add($"响应头验证失败: 响应包含禁止的响应头 '{forbiddenHeader}'");
                }
            }
        }
    }

    /// <summary>
    /// 验证JSON Path
    /// </summary>
    private void ValidateJsonPath(ApiResponse response, ApiValidation validation, ValidationResult result)
    {
        if (validation.JsonPathValidations?.Any() == true)
        {
            try
            {
                var jsonObject = JObject.Parse(response.RawContent);

                foreach (var jsonPathValidation in validation.JsonPathValidations)
                {
                    var token = jsonObject.SelectToken(jsonPathValidation.Path);

                    if (token == null)
                    {
                        if (jsonPathValidation.IsRequired)
                        {
                            result.IsValid = false;
                            result.Errors.Add($"JSON Path验证失败: 路径 '{jsonPathValidation.Path}' 不存在");
                        }
                        continue;
                    }

                    // 验证值
                    if (jsonPathValidation.ExpectedValue != null)
                    {
                        var actualValue = token.ToString();
                        var expectedValue = jsonPathValidation.ExpectedValue.ToString();

                        if (actualValue != expectedValue)
                        {
                            result.IsValid = false;
                            result.Errors.Add($"JSON Path验证失败: 路径 '{jsonPathValidation.Path}' 值不匹配, 期望 '{expectedValue}', 实际 '{actualValue}'");
                        }
                    }

                    // 验证类型
                    if (jsonPathValidation.ExpectedType.HasValue)
                    {
                        var actualType = token.Type;
                        if (actualType != jsonPathValidation.ExpectedType.Value)
                        {
                            result.IsValid = false;
                            result.Errors.Add($"JSON Path验证失败: 路径 '{jsonPathValidation.Path}' 类型不匹配, 期望 '{jsonPathValidation.ExpectedType.Value}', 实际 '{actualType}'");
                        }
                    }

                    // 验证数组长度
                    if (jsonPathValidation.ExpectedArrayLength.HasValue && token.Type == JTokenType.Array)
                    {
                        var array = (JArray)token;
                        if (array.Count != jsonPathValidation.ExpectedArrayLength.Value)
                        {
                            result.IsValid = false;
                            result.Errors.Add($"JSON Path验证失败: 路径 '{jsonPathValidation.Path}' 数组长度不匹配, 期望 {jsonPathValidation.ExpectedArrayLength.Value}, 实际 {array.Count}");
                        }
                    }

                    // 验证正则表达式
                    if (!string.IsNullOrEmpty(jsonPathValidation.ValueRegex))
                    {
                        try
                        {
                            var regex = new Regex(jsonPathValidation.ValueRegex, RegexOptions.IgnoreCase);
                            var value = token.ToString();
                            if (!regex.IsMatch(value))
                            {
                                result.IsValid = false;
                                result.Errors.Add($"JSON Path验证失败: 路径 '{jsonPathValidation.Path}' 值不匹配正则表达式 '{jsonPathValidation.ValueRegex}', 实际值 '{value}'");
                            }
                        }
                        catch (ArgumentException ex)
                        {
                            result.IsValid = false;
                            result.Errors.Add($"JSON Path验证失败: 正则表达式格式错误 '{jsonPathValidation.ValueRegex}': {ex.Message}");
                        }
                    }
                }
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                result.IsValid = false;
                result.Errors.Add($"JSON Path验证失败: 响应内容不是有效的JSON格式: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 验证JSON Schema
    /// </summary>
    private void ValidateJsonSchema(ApiResponse response, ApiValidation validation, ValidationResult result)
    {
        if (!string.IsNullOrEmpty(validation.JsonSchema))
        {
            try
            {
                // 这里可以集成JSON Schema验证库，如Newtonsoft.Json.Schema
                // 由于这是一个基础实现，暂时只做基本的JSON格式验证
                JObject.Parse(response.RawContent);
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                result.IsValid = false;
                result.Errors.Add($"JSON Schema验证失败: 响应内容不是有效的JSON格式: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取请求大小
    /// </summary>
    /// <param name="request">API请求</param>
    /// <returns>请求大小（字节）</returns>
    private static long GetRequestSize(ApiRequest request)
    {
        long size = 0;

        // 计算URL大小
        size += System.Text.Encoding.UTF8.GetByteCount(request.Endpoint);

        // 计算请求头大小
        if (request.Headers?.Any() == true)
        {
            foreach (var header in request.Headers)
            {
                size += System.Text.Encoding.UTF8.GetByteCount($"{header.Key}: {header.Value}\r\n");
            }
        }

        // 计算请求体大小
        if (request.Body != null)
        {
            if (request.Body is string stringBody)
            {
                size += System.Text.Encoding.UTF8.GetByteCount(stringBody);
            }
            else
            {
                var jsonBody = JsonSerializer.Serialize(request.Body);
                size += System.Text.Encoding.UTF8.GetByteCount(jsonBody);
            }
        }

        return size;
    }

    /// <summary>
    /// 提取HTTP响应头
    /// </summary>
    /// <param name="response">HTTP响应消息</param>
    /// <returns>响应头字典</returns>
    private static Dictionary<string, string> ExtractHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>();

        // 添加响应头
        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        // 添加内容头
        if (response.Content?.Headers != null)
        {
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }

        return headers;
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
            if (_apiClient is IDisposable disposableClient)
            {
                disposableClient.Dispose();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// API 验证规则
/// </summary>
public class ApiValidation
{
    /// <summary>
    /// 期望的状态码
    /// </summary>
    public int? ExpectedStatusCode { get; set; }

    /// <summary>
    /// 期望的状态码列表（任一匹配即可）
    /// </summary>
    public List<int>? ExpectedStatusCodes { get; set; }

    /// <summary>
    /// 最大响应时间
    /// </summary>
    public TimeSpan? MaxResponseTime { get; set; }

    /// <summary>
    /// 最小响应时间
    /// </summary>
    public TimeSpan? MinResponseTime { get; set; }

    /// <summary>
    /// 内容必须包含的文本
    /// </summary>
    public string? ContentContains { get; set; }

    /// <summary>
    /// 内容必须包含的文本列表
    /// </summary>
    public List<string>? ContentContainsList { get; set; }

    /// <summary>
    /// 内容不能包含的文本
    /// </summary>
    public string? ContentNotContains { get; set; }

    /// <summary>
    /// 内容不能包含的文本列表
    /// </summary>
    public List<string>? ContentNotContainsList { get; set; }

    /// <summary>
    /// 内容必须匹配的正则表达式
    /// </summary>
    public string? ContentRegex { get; set; }

    /// <summary>
    /// 必需的响应头
    /// </summary>
    public Dictionary<string, string>? RequiredHeaders { get; set; }

    /// <summary>
    /// 禁止的响应头
    /// </summary>
    public List<string>? ForbiddenHeaders { get; set; }

    /// <summary>
    /// JSON Path 验证规则列表
    /// </summary>
    public List<JsonPathValidation>? JsonPathValidations { get; set; }

    /// <summary>
    /// JSON Schema 验证（JSON Schema 字符串）
    /// </summary>
    public string? JsonSchema { get; set; }
}

/// <summary>
/// JSON Path 验证规则
/// </summary>
public class JsonPathValidation
{
    /// <summary>
    /// JSON Path 路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 是否必需存在
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 期望的值
    /// </summary>
    public object? ExpectedValue { get; set; }

    /// <summary>
    /// 期望的JSON类型
    /// </summary>
    public Newtonsoft.Json.Linq.JTokenType? ExpectedType { get; set; }

    /// <summary>
    /// 期望的数组长度（仅当类型为数组时有效）
    /// </summary>
    public int? ExpectedArrayLength { get; set; }

    /// <summary>
    /// 值必须匹配的正则表达式
    /// </summary>
    public string? ValueRegex { get; set; }
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 是否验证通过
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 验证错误列表
    /// </summary>
    public List<string> Errors { get; set; } = new();
}