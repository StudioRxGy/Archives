namespace CsPlaywrightXun.src.playwright.Core.Interfaces;

/// <summary>
/// API 客户端接口
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// 发送 GET 请求
    /// </summary>
    Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string>? headers = null);
    
    /// <summary>
    /// 发送 POST 请求
    /// </summary>
    Task<HttpResponseMessage> PostAsync(string endpoint, object? data, Dictionary<string, string>? headers = null);
    
    /// <summary>
    /// 发送 PUT 请求
    /// </summary>
    Task<HttpResponseMessage> PutAsync(string endpoint, object? data, Dictionary<string, string>? headers = null);
    
    /// <summary>
    /// 发送 DELETE 请求
    /// </summary>
    Task<HttpResponseMessage> DeleteAsync(string endpoint, Dictionary<string, string>? headers = null);
}