// ---------------------------------------------------------------
// 文件描述：API 断言工具类
// 创建时间：
// 创建人：eleven
// 修改历史：
// ---------------------------------------------------------------

using Microsoft.Playwright;
using System.Text.Json;
using Xunit;

namespace CsPlaywrightApi.src.playwright.Core.Api;

/// <summary>
/// API断言辅助类
/// </summary>
public static class ApiAssertions
{
    /// <summary>
    /// 断言状态码
    /// </summary>
    public static void AssertStatusCode(IAPIResponse response, int expectedStatusCode, string? message = null)
    {
        string errorMsg = message ?? $"期望状态码 {expectedStatusCode}，实际状态码 {response.Status}";
        Assert.True(response.Status == expectedStatusCode, errorMsg);
    }

    /// <summary>
    /// 断言响应成功（2xx）
    /// </summary>
    public static void AssertSuccess(IAPIResponse response, string? message = null)
    {
        string errorMsg = message ?? $"请求失败，状态码: {response.Status}";
        Assert.True(response.Ok, errorMsg);
    }

    /// <summary>
    /// 断言响应包含指定文本
    /// </summary>
    public static async Task AssertContainsTextAsync(IAPIResponse response, string expectedText, string? message = null)
    {
        string responseText = await response.TextAsync().ConfigureAwait(false);
        string errorMsg = message ?? $"响应中未找到文本: {expectedText}";
        Assert.Contains(expectedText, responseText, StringComparison.Ordinal);
    }

    /// <summary>
    /// 断言JSON字段值
    /// </summary>
    public static async Task AssertJsonFieldAsync(IAPIResponse response, string fieldPath, string expectedValue, string? message = null)
    {
        string? actualValue = await ExtractJsonFieldAsync(response, fieldPath).ConfigureAwait(false);
        string errorMsg = message ?? $"字段 {fieldPath} 期望值: {expectedValue}，实际值: {actualValue}";
        Assert.Equal(expectedValue, actualValue);
    }

    /// <summary>
    /// 断言JSON字段存在
    /// </summary>
    public static async Task AssertJsonFieldExistsAsync(IAPIResponse response, string fieldPath, string? message = null)
    {
        string? value = await ExtractJsonFieldAsync(response, fieldPath).ConfigureAwait(false);
        string errorMsg = message ?? $"字段 {fieldPath} 不存在";
        Assert.NotNull(value);
    }

    /// <summary>
    /// 断言响应头包含指定键
    /// </summary>
    public static void AssertHeaderExists(IAPIResponse response, string headerKey, string? message = null)
    {
        string errorMsg = message ?? $"响应头中未找到: {headerKey}";
        Assert.True(response.Headers.ContainsKey(headerKey.ToLower()), errorMsg);
    }

    /// <summary>
    /// 断言响应头值
    /// </summary>
    public static void AssertHeaderValue(IAPIResponse response, string headerKey, string expectedValue, string? message = null)
    {
        Assert.True(response.Headers.TryGetValue(headerKey.ToLower(), out string? actualValue),
            $"响应头中未找到: {headerKey}");

        string errorMsg = message ?? $"响应头 {headerKey} 期望值: {expectedValue}，实际值: {actualValue}";
        Assert.Equal(expectedValue, actualValue);
    }

    /// <summary>
    /// 断言响应时间小于指定毫秒数
    /// </summary>
    public static void AssertResponseTime(long actualMs, long maxMs, string? message = null)
    {
        string errorMsg = message ?? $"响应时间 {actualMs}ms 超过最大限制 {maxMs}ms";
        Assert.True(actualMs <= maxMs, errorMsg);
    }

    /// <summary>
    /// 从响应中提取JSON字段值
    /// </summary>
    private static async Task<string?> ExtractJsonFieldAsync(IAPIResponse response, string fieldPath)
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
}
