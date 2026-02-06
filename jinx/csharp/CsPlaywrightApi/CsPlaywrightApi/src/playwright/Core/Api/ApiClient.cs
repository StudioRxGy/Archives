// ---------------------------------------------------------------
//  文件描述：
//  创建时间：
//  创建人：eleven
//  修改历史：
// ---------------------------------------------------------------

using Microsoft.Playwright;
using System.Diagnostics;
using System.Text.Json;
using CsPlaywrightApi.src.playwright.Core.Logging;

namespace CsPlaywrightApi.src.playwright.Core.Api;

/// <summary>
/// API客户端基类，封装常用的API请求方法
/// </summary>
public class ApiClient
{
    protected readonly IAPIRequestContext _apiContext;
    protected readonly ApiLogger _logger;
    protected Dictionary<string, string> _defaultHeaders;

    public ApiClient(IAPIRequestContext apiContext, ApiLogger? logger = null)
    {
        _apiContext = apiContext;
        _logger = logger ?? new ApiLogger();
        _defaultHeaders = [];
    }

    /// <summary>
    /// 设置默认请求头（会应用到所有请求）
    /// </summary>
    public void SetDefaultHeaders(Dictionary<string, string> headers)
    {
        _defaultHeaders = headers;
    }

    /// <summary>
    /// 添加单个默认请求头
    /// </summary>
    public void AddDefaultHeader(string key, string value)
    {
        _defaultHeaders[key] = value;
    }

    /// <summary>
    /// 发送GET请求
    /// </summary>
    public async Task<IAPIResponse> GetAsync(string url, Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        // 构建完整URL（包含查询参数）
        string fullUrl = BuildUrlWithQuery(url, queryParams);

        // 合并请求头
        Dictionary<string, string> mergedHeaders = MergeHeaders(headers);

        IAPIResponse response = await _apiContext.GetAsync(fullUrl, new APIRequestContextOptions
        {
            Headers = mergedHeaders
        }).ConfigureAwait(false);

        stopwatch.Stop();
        await LogRequestAsync(fullUrl, "GET", string.Empty, response, stopwatch.ElapsedMilliseconds, mergedHeaders).ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// 发送POST请求（JSON格式）
    /// </summary>
    public async Task<IAPIResponse> PostJsonAsync(string url, object data, Dictionary<string, string>? headers = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        string jsonData = JsonSerializer.Serialize(data);
        Dictionary<string, string> mergedHeaders = MergeHeaders(headers);

        // 确保Content-Type为application/json
        if (!mergedHeaders.ContainsKey("Content-Type"))
        {
            mergedHeaders["Content-Type"] = "application/json";
        }

        IAPIResponse response = await _apiContext.PostAsync(url, new APIRequestContextOptions
        {
            Headers = mergedHeaders,
            Data = jsonData
        }).ConfigureAwait(false);

        stopwatch.Stop();
        await LogRequestAsync(url, "POST", jsonData, response, stopwatch.ElapsedMilliseconds, mergedHeaders).ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// 发送POST请求（表单格式）
    /// </summary>
    public async Task<IAPIResponse> PostFormAsync(string url, Dictionary<string, string> formData, Dictionary<string, string>? headers = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        string formContent = string.Join("&",
            formData.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        Dictionary<string, string> mergedHeaders = MergeHeaders(headers);

        // 确保Content-Type为application/x-www-form-urlencoded
        if (!mergedHeaders.ContainsKey("Content-Type"))
        {
            mergedHeaders["Content-Type"] = "application/x-www-form-urlencoded";
        }

        IAPIResponse response = await _apiContext.PostAsync(url, new APIRequestContextOptions
        {
            Headers = mergedHeaders,
            Data = formContent
        }).ConfigureAwait(false);

        stopwatch.Stop();
        await LogRequestAsync(url, "POST", formContent, response, stopwatch.ElapsedMilliseconds, mergedHeaders).ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// 发送PUT请求（JSON格式）
    /// </summary>
    public async Task<IAPIResponse> PutJsonAsync(string url, object data, Dictionary<string, string>? headers = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        string jsonData = JsonSerializer.Serialize(data);
        Dictionary<string, string> mergedHeaders = MergeHeaders(headers);

        if (!mergedHeaders.ContainsKey("Content-Type"))
        {
            mergedHeaders["Content-Type"] = "application/json";
        }

        IAPIResponse response = await _apiContext.PutAsync(url, new APIRequestContextOptions
        {
            Headers = mergedHeaders,
            Data = jsonData
        }).ConfigureAwait(false);

        stopwatch.Stop();
        await LogRequestAsync(url, "PUT", jsonData, response, stopwatch.ElapsedMilliseconds, mergedHeaders).ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// 发送DELETE请求
    /// </summary>
    public async Task<IAPIResponse> DeleteAsync(string url, Dictionary<string, string>? headers = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        Dictionary<string, string> mergedHeaders = MergeHeaders(headers);

        IAPIResponse response = await _apiContext.DeleteAsync(url, new APIRequestContextOptions
        {
            Headers = mergedHeaders
        }).ConfigureAwait(false);

        stopwatch.Stop();
        await LogRequestAsync(url, "DELETE", string.Empty, response, stopwatch.ElapsedMilliseconds, mergedHeaders).ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// 发送PATCH请求（JSON格式）
    /// </summary>
    public async Task<IAPIResponse> PatchJsonAsync(string url, object data, Dictionary<string, string>? headers = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        string jsonData = JsonSerializer.Serialize(data);
        Dictionary<string, string> mergedHeaders = MergeHeaders(headers);

        if (!mergedHeaders.ContainsKey("Content-Type"))
        {
            mergedHeaders["Content-Type"] = "application/json";
        }

        IAPIResponse response = await _apiContext.PatchAsync(url, new APIRequestContextOptions
        {
            Headers = mergedHeaders,
            Data = jsonData
        }).ConfigureAwait(false);

        stopwatch.Stop();
        await LogRequestAsync(url, "PATCH", jsonData, response, stopwatch.ElapsedMilliseconds, mergedHeaders).ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// 获取响应的JSON对象
    /// </summary>
    public async Task<T?> GetJsonResponseAsync<T>(IAPIResponse response)
    {
        string responseText = await response.TextAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(responseText);
    }

    /// <summary>
    /// 获取响应的文本内容
    /// </summary>
    public async Task<string> GetTextResponseAsync(IAPIResponse response) =>
        await response.TextAsync().ConfigureAwait(false);

    /// <summary>
    /// 验证响应状态码
    /// </summary>
    public void AssertStatusCode(IAPIResponse response, int expectedStatusCode)
    {
        if (response.Status != expectedStatusCode)
        {
            throw new InvalidOperationException($"期望状态码 {expectedStatusCode}，实际状态码 {response.Status}");
        }
    }

    /// <summary>
    /// 验证响应是否成功（2xx状态码）
    /// </summary>
    public void AssertSuccess(IAPIResponse response)
    {
        if (!response.Ok)
        {
            throw new InvalidOperationException($"请求失败，状态码: {response.Status}");
        }
    }

    /// <summary>
    /// 从响应中提取JSON字段值
    /// </summary>
    public async Task<string?> ExtractJsonFieldAsync(IAPIResponse response, string fieldPath)
    {
        string responseText = await response.TextAsync().ConfigureAwait(false);
        try
        {
            JsonDocument jsonDoc = JsonDocument.Parse(responseText);
            string[] fields = fieldPath.Split('.');
            JsonElement element = jsonDoc.RootElement;

            foreach (string field in fields)
            {
                if (element.TryGetProperty(field, out JsonElement nextElement))
                {
                    element = nextElement;
                }
                else
                {
                    return null;
                }
            }

            return element.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 构建带查询参数的URL
    /// </summary>
    private static string BuildUrlWithQuery(string url, Dictionary<string, string>? queryParams)
    {
        if (queryParams == null || queryParams.Count == 0)
        {
            return url;
        }

        string queryString = string.Join("&",
            queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return url.Contains('?') ? $"{url}&{queryString}" : $"{url}?{queryString}";
    }

    /// <summary>
    /// 合并默认请求头和自定义请求头
    /// </summary>
    private Dictionary<string, string> MergeHeaders(Dictionary<string, string>? customHeaders)
    {
        Dictionary<string, string> merged = new(_defaultHeaders);

        if (customHeaders != null)
        {
            foreach (KeyValuePair<string, string> header in customHeaders)
            {
                merged[header.Key] = header.Value;
            }
        }

        return merged;
    }

    /// <summary>
    /// 记录API请求日志
    /// </summary>
    protected async Task LogRequestAsync(string url, string method, string requestBody,
        IAPIResponse response, long duration, Dictionary<string, string>? requestHeaders = null)
    {
        string responseBody = await response.TextAsync().ConfigureAwait(false);
        string? responseCode = null;

        // 尝试从响应中提取code字段
        try
        {
            JsonDocument jsonDoc = JsonDocument.Parse(responseBody);
            if (jsonDoc.RootElement.TryGetProperty("code", out JsonElement codeElement))
            {
                responseCode = codeElement.ToString();
            }
        }
        catch
        {
            // 如果解析失败，忽略
        }

        ApiLogEntry logEntry = new()
        {
            Url = url,
            Method = method,
            RequestBody = requestBody,
            StatusCode = response.Status,
            ResponseCode = responseCode,
            ResponseBody = responseBody,
            Duration = duration,
            RequestHeaders = requestHeaders,
            ResponseHeaders = response.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            // 使用Logger中设置的测试上下文信息
            TestMethod = _logger.CurrentTestMethod,
            TestClass = _logger.CurrentTestClass,
            SourceFile = _logger.CurrentSourceFile,
            TestScenario = _logger.CurrentTestScenario,
            TestCategories = _logger.CurrentTestCategories,
            TestPriority = _logger.CurrentTestPriority,
            TestDisplayName = _logger.CurrentTestDisplayName
        };

        await _logger.LogApiRequestAsync(logEntry).ConfigureAwait(false);
    }
}
