using Microsoft.Playwright;
using System.Text.Json;
using Xunit;

namespace CsPlaywrightApi.src.playwright.Core.Api
{
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
            var errorMsg = message ?? $"期望状态码 {expectedStatusCode}，实际状态码 {response.Status}";
            Assert.True(response.Status == expectedStatusCode, errorMsg);
        }

        /// <summary>
        /// 断言响应成功（2xx）
        /// </summary>
        public static void AssertSuccess(IAPIResponse response, string? message = null)
        {
            var errorMsg = message ?? $"请求失败，状态码: {response.Status}";
            Assert.True(response.Ok, errorMsg);
        }

        /// <summary>
        /// 断言响应包含指定文本
        /// </summary>
        public static async Task AssertContainsTextAsync(IAPIResponse response, string expectedText, string? message = null)
        {
            var responseText = await response.TextAsync();
            var errorMsg = message ?? $"响应中未找到文本: {expectedText}";
            Assert.Contains(expectedText, responseText, StringComparison.Ordinal);
        }

        /// <summary>
        /// 断言JSON字段值
        /// </summary>
        public static async Task AssertJsonFieldAsync(IAPIResponse response, string fieldPath, string expectedValue, string? message = null)
        {
            var actualValue = await ExtractJsonFieldAsync(response, fieldPath);
            var errorMsg = message ?? $"字段 {fieldPath} 期望值: {expectedValue}，实际值: {actualValue}";
            Assert.Equal(expectedValue, actualValue);
        }

        /// <summary>
        /// 断言JSON字段存在
        /// </summary>
        public static async Task AssertJsonFieldExistsAsync(IAPIResponse response, string fieldPath, string? message = null)
        {
            var value = await ExtractJsonFieldAsync(response, fieldPath);
            var errorMsg = message ?? $"字段 {fieldPath} 不存在";
            Assert.NotNull(value);
        }

        /// <summary>
        /// 断言响应头包含指定键
        /// </summary>
        public static void AssertHeaderExists(IAPIResponse response, string headerKey, string? message = null)
        {
            var errorMsg = message ?? $"响应头中未找到: {headerKey}";
            Assert.True(response.Headers.ContainsKey(headerKey.ToLower()), errorMsg);
        }

        /// <summary>
        /// 断言响应头值
        /// </summary>
        public static void AssertHeaderValue(IAPIResponse response, string headerKey, string expectedValue, string? message = null)
        {
            Assert.True(response.Headers.TryGetValue(headerKey.ToLower(), out var actualValue), 
                $"响应头中未找到: {headerKey}");

            var errorMsg = message ?? $"响应头 {headerKey} 期望值: {expectedValue}，实际值: {actualValue}";
            Assert.Equal(expectedValue, actualValue);
        }

        /// <summary>
        /// 断言响应时间小于指定毫秒数
        /// </summary>
        public static void AssertResponseTime(long actualMs, long maxMs, string? message = null)
        {
            var errorMsg = message ?? $"响应时间 {actualMs}ms 超过最大限制 {maxMs}ms";
            Assert.True(actualMs <= maxMs, errorMsg);
        }

        /// <summary>
        /// 从响应中提取JSON字段值
        /// </summary>
        private static async Task<string?> ExtractJsonFieldAsync(IAPIResponse response, string fieldPath)
        {
            var responseText = await response.TextAsync();
            try
            {
                var jsonDoc = JsonDocument.Parse(responseText);
                var fields = fieldPath.Split('.');
                JsonElement element = jsonDoc.RootElement;

                foreach (var field in fields)
                {
                    if (element.TryGetProperty(field, out var nextElement))
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
}
