using System.Text.Json.Serialization;

namespace CsPlaywrightXun.src.playwright.Tests.API.Models;

/// <summary>
/// API 测试数据模型
/// </summary>
public class ApiTestData
{
    /// <summary>
    /// 测试名称
    /// </summary>
    [JsonPropertyName("testName")]
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// API 端点
    /// </summary>
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// HTTP 方法
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = "GET";

    /// <summary>
    /// 查询参数
    /// </summary>
    [JsonPropertyName("queryParameters")]
    public Dictionary<string, string> QueryParameters { get; set; } = new();

    /// <summary>
    /// 请求头
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// 请求体
    /// </summary>
    [JsonPropertyName("body")]
    public object? Body { get; set; }

    /// <summary>
    /// 期望的状态码
    /// </summary>
    [JsonPropertyName("expectedStatusCode")]
    public int ExpectedStatusCode { get; set; } = 200;

    /// <summary>
    /// 期望的状态码列表
    /// </summary>
    [JsonPropertyName("expectedStatusCodes")]
    public List<int>? ExpectedStatusCodes { get; set; }

    /// <summary>
    /// 期望内容包含的文本列表
    /// </summary>
    [JsonPropertyName("expectedContentContains")]
    public List<string>? ExpectedContentContains { get; set; }

    /// <summary>
    /// 期望内容不包含的文本列表
    /// </summary>
    [JsonPropertyName("expectedContentNotContains")]
    public List<string>? ExpectedContentNotContains { get; set; }

    /// <summary>
    /// 最大响应时间（毫秒）
    /// </summary>
    [JsonPropertyName("maxResponseTimeMs")]
    public int MaxResponseTimeMs { get; set; } = 30000;

    /// <summary>
    /// 最小响应时间（毫秒）
    /// </summary>
    [JsonPropertyName("minResponseTimeMs")]
    public int? MinResponseTimeMs { get; set; }

    /// <summary>
    /// 内容正则表达式
    /// </summary>
    [JsonPropertyName("contentRegex")]
    public string? ContentRegex { get; set; }

    /// <summary>
    /// 必需的响应头
    /// </summary>
    [JsonPropertyName("requiredHeaders")]
    public Dictionary<string, string>? RequiredHeaders { get; set; }

    /// <summary>
    /// 禁止的响应头
    /// </summary>
    [JsonPropertyName("forbiddenHeaders")]
    public List<string>? ForbiddenHeaders { get; set; }

    /// <summary>
    /// 测试描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 测试标签
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 测试优先级
    /// </summary>
    [JsonPropertyName("priority")]
    public TestPriority Priority { get; set; } = TestPriority.Medium;

    /// <summary>
    /// 转换为 ApiRequest 对象
    /// </summary>
    /// <returns>ApiRequest 对象</returns>
    public Services.Api.ApiRequest ToApiRequest()
    {
        return new Services.Api.ApiRequest
        {
            Method = Method,
            Endpoint = Endpoint,
            QueryParameters = QueryParameters,
            Headers = Headers,
            Body = Body
        };
    }

    /// <summary>
    /// 转换为 ApiValidation 对象
    /// </summary>
    /// <returns>ApiValidation 对象</returns>
    public Services.Api.ApiValidation ToApiValidation()
    {
        var validation = new Services.Api.ApiValidation();

        // 设置期望状态码
        if (ExpectedStatusCodes?.Any() == true)
        {
            validation.ExpectedStatusCodes = ExpectedStatusCodes;
        }
        else
        {
            validation.ExpectedStatusCode = ExpectedStatusCode;
        }

        // 设置响应时间限制
        validation.MaxResponseTime = TimeSpan.FromMilliseconds(MaxResponseTimeMs);
        if (MinResponseTimeMs.HasValue)
        {
            validation.MinResponseTime = TimeSpan.FromMilliseconds(MinResponseTimeMs.Value);
        }

        // 设置内容验证
        if (ExpectedContentContains?.Any() == true)
        {
            validation.ContentContainsList = ExpectedContentContains;
        }

        if (ExpectedContentNotContains?.Any() == true)
        {
            validation.ContentNotContainsList = ExpectedContentNotContains;
        }

        if (!string.IsNullOrEmpty(ContentRegex))
        {
            validation.ContentRegex = ContentRegex;
        }

        // 设置响应头验证
        if (RequiredHeaders?.Any() == true)
        {
            validation.RequiredHeaders = RequiredHeaders;
        }

        if (ForbiddenHeaders?.Any() == true)
        {
            validation.ForbiddenHeaders = ForbiddenHeaders;
        }

        return validation;
    }

    /// <summary>
    /// 获取测试显示名称
    /// </summary>
    /// <returns>测试显示名称</returns>
    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(TestName) ? TestName : $"{Method} {Endpoint}";
    }

    /// <summary>
    /// 验证测试数据是否有效
    /// </summary>
    /// <returns>验证结果</returns>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Endpoint))
            errors.Add("端点不能为空");

        if (string.IsNullOrWhiteSpace(Method))
            errors.Add("HTTP方法不能为空");

        var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
        if (!validMethods.Contains(Method.ToUpperInvariant()))
            errors.Add($"不支持的HTTP方法: {Method}");

        if (ExpectedStatusCode < 100 || ExpectedStatusCode > 599)
            errors.Add($"无效的状态码: {ExpectedStatusCode}");

        if (MaxResponseTimeMs <= 0)
            errors.Add("最大响应时间必须大于0");

        if (MinResponseTimeMs.HasValue && MinResponseTimeMs.Value < 0)
            errors.Add("最小响应时间不能小于0");

        if (MinResponseTimeMs.HasValue && MinResponseTimeMs.Value >= MaxResponseTimeMs)
            errors.Add("最小响应时间必须小于最大响应时间");

        return (errors.Count == 0, errors);
    }
}

/// <summary>
/// API 验证规则数据模型
/// </summary>
public class ApiValidationRuleData
{
    /// <summary>
    /// 测试名称
    /// </summary>
    [JsonPropertyName("testName")]
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// 期望的状态码
    /// </summary>
    [JsonPropertyName("expectedStatusCode")]
    public int? ExpectedStatusCode { get; set; }

    /// <summary>
    /// 期望的状态码列表
    /// </summary>
    [JsonPropertyName("expectedStatusCodes")]
    public List<int>? ExpectedStatusCodes { get; set; }

    /// <summary>
    /// 最大响应时间（毫秒）
    /// </summary>
    [JsonPropertyName("maxResponseTimeMs")]
    public int? MaxResponseTimeMs { get; set; }

    /// <summary>
    /// 最小响应时间（毫秒）
    /// </summary>
    [JsonPropertyName("minResponseTimeMs")]
    public int? MinResponseTimeMs { get; set; }

    /// <summary>
    /// 内容必须包含的文本
    /// </summary>
    [JsonPropertyName("contentContains")]
    public string? ContentContains { get; set; }

    /// <summary>
    /// 内容必须包含的文本列表
    /// </summary>
    [JsonPropertyName("contentContainsList")]
    public List<string>? ContentContainsList { get; set; }

    /// <summary>
    /// 内容不能包含的文本
    /// </summary>
    [JsonPropertyName("contentNotContains")]
    public string? ContentNotContains { get; set; }

    /// <summary>
    /// 内容不能包含的文本列表
    /// </summary>
    [JsonPropertyName("contentNotContainsList")]
    public List<string>? ContentNotContainsList { get; set; }

    /// <summary>
    /// 内容正则表达式
    /// </summary>
    [JsonPropertyName("contentRegex")]
    public string? ContentRegex { get; set; }

    /// <summary>
    /// 必需的响应头
    /// </summary>
    [JsonPropertyName("requiredHeaders")]
    public Dictionary<string, string>? RequiredHeaders { get; set; }

    /// <summary>
    /// 禁止的响应头
    /// </summary>
    [JsonPropertyName("forbiddenHeaders")]
    public List<string>? ForbiddenHeaders { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 转换为 ApiValidation 对象
    /// </summary>
    /// <returns>ApiValidation 对象</returns>
    public Services.Api.ApiValidation ToApiValidation()
    {
        var validation = new Services.Api.ApiValidation();

        if (ExpectedStatusCode.HasValue)
            validation.ExpectedStatusCode = ExpectedStatusCode.Value;

        if (ExpectedStatusCodes?.Any() == true)
            validation.ExpectedStatusCodes = ExpectedStatusCodes;

        if (MaxResponseTimeMs.HasValue)
            validation.MaxResponseTime = TimeSpan.FromMilliseconds(MaxResponseTimeMs.Value);

        if (MinResponseTimeMs.HasValue)
            validation.MinResponseTime = TimeSpan.FromMilliseconds(MinResponseTimeMs.Value);

        if (!string.IsNullOrEmpty(ContentContains))
            validation.ContentContains = ContentContains;

        if (ContentContainsList?.Any() == true)
            validation.ContentContainsList = ContentContainsList;

        if (!string.IsNullOrEmpty(ContentNotContains))
            validation.ContentNotContains = ContentNotContains;

        if (ContentNotContainsList?.Any() == true)
            validation.ContentNotContainsList = ContentNotContainsList;

        if (!string.IsNullOrEmpty(ContentRegex))
            validation.ContentRegex = ContentRegex;

        if (RequiredHeaders?.Any() == true)
            validation.RequiredHeaders = RequiredHeaders;

        if (ForbiddenHeaders?.Any() == true)
            validation.ForbiddenHeaders = ForbiddenHeaders;

        return validation;
    }
}

/// <summary>
/// 测试优先级枚举
/// </summary>
public enum TestPriority
{
    /// <summary>
    /// 低优先级
    /// </summary>
    Low = 1,

    /// <summary>
    /// 中等优先级
    /// </summary>
    Medium = 2,

    /// <summary>
    /// 高优先级
    /// </summary>
    High = 3,

    /// <summary>
    /// 关键优先级
    /// </summary>
    Critical = 4
}