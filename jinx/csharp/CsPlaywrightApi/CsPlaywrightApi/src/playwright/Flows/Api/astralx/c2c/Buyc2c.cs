// ---------------------------------------------------------------
// 文件描述：C2C 买入流程
// 创建时间：
// 创建人：eleven
// 修改历史：
// ---------------------------------------------------------------

using Microsoft.Playwright;
using CsPlaywrightApi.src.playwright.Core.Api;
using CsPlaywrightApi.src.playwright.Core.Logging;
using CsPlaywrightApi.src.playwright.Core.Config;

namespace CsPlaywrightApi.src.playwright.Flows.Api.astralx.c2c;

/// <summary>
/// C2C购买API（使用ApiClient基类）
/// </summary>
public class Buyc2c : ApiClient
{
    private readonly AppSettings _settings;
    private string? _cToken;

    public Buyc2c(IAPIRequestContext apiContext, ApiLogger? logger = null)
        : base(apiContext, logger)
    {
        _settings = AppSettings.Instance;
    }

    /// <summary>
    /// 设置C Token
    /// </summary>
    public void SetCToken(string cToken)
    {
        _cToken = cToken;
    }

    /// <summary>
    /// 执行C2C下单请求
    /// </summary>
    public async Task<IAPIResponse> CreateC2cOrderAsync()
    {
        if (string.IsNullOrEmpty(_cToken))
        {
            throw new InvalidOperationException("C Token 未设置。请先调用 SetCToken 方法。");
        }

        var payload = new
        {
            fiatPrice = 1,
            fiatAmount = "{{sharedRandomValue}}",
            tokenAmount = "{{sharedRandomValue}}",
            adId = 887,
            paymentMethodId = 11
        };

        // 使用配置的 BaseUrl
        string url = $"{_settings.Config.BaseUrl}/api/broker/c2c/order?c_token={_cToken}";
        return await PostJsonAsync(url, payload).ConfigureAwait(false);
    }

    /// <summary>
    /// 从响应中提取订单IDdata
    /// </summary>
    public async Task<string?> GetOrderIdFromResponseAsync(IAPIResponse response) =>
        await ExtractJsonFieldAsync(response, "data").ConfigureAwait(false);

    /// <summary>
    /// 验证订单创建成功
    /// </summary>
    public async Task<bool> IsOrderCreatedSuccessfullyAsync(IAPIResponse response)
    {
        string? orderId = await ExtractJsonFieldAsync(response, "data").ConfigureAwait(false);
        return !string.IsNullOrEmpty(orderId) && orderId != "Null";
    }
}
