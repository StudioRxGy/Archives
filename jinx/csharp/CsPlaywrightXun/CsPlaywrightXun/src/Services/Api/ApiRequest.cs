namespace CsPlaywrightXun.src.playwright.Services.Api;

/// <summary>
/// API 请求模型
/// </summary>
public class ApiRequest
{
    /// <summary>
    /// HTTP 方法
    /// </summary>
    public string Method { get; set; } = "GET";
    
    /// <summary>
    /// 请求端点
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求体
    /// </summary>
    public object? Body { get; set; }
    
    /// <summary>
    /// 请求头
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();
    
    /// <summary>
    /// 查询参数
    /// </summary>
    public Dictionary<string, string> QueryParameters { get; set; } = new();

    /// <summary>
    /// 构建带查询参数的完整端点
    /// </summary>
    /// <returns>完整端点URL</returns>
    public string BuildEndpointWithQuery()
    {
        if (!QueryParameters.Any())
            return Endpoint;

        var queryString = string.Join("&", 
            QueryParameters.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
                          .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value ?? string.Empty)}"));

        var separator = Endpoint.Contains('?') ? "&" : "?";
        return $"{Endpoint}{separator}{queryString}";
    }
}

/// <summary>
/// API 响应模型
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int StatusCode { get; set; }
    
    /// <summary>
    /// 响应数据
    /// </summary>
    public T? Data { get; set; }
    
    /// <summary>
    /// 原始内容
    /// </summary>
    public string RawContent { get; set; } = string.Empty;
    
    /// <summary>
    /// 响应头
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();
    
    /// <summary>
    /// 响应时间
    /// </summary>
    public TimeSpan ResponseTime { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
}

/// <summary>
/// API 响应模型（非泛型）
/// </summary>
public class ApiResponse : ApiResponse<object>
{
}