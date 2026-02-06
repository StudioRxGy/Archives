// ---------------------------------------------------------------
// 文件描述：U合约平仓流程
// 创建时间：
// 创建人：eleven
// 修改历史：
// ---------------------------------------------------------------

using CsPlaywrightApi.src.playwright.Core.Api;
using CsPlaywrightApi.src.playwright.Core.Logging;
using CsPlaywrightApi.src.playwright.Core.Config;
using Microsoft.Playwright;

namespace CsPlaywrightApi.src.playwright.Flows.Api.astralx.Uheyue;

/// <summary>
/// 闪电平仓API（使用ApiClient基类）
/// </summary>
public class Pingcang : ApiClient
{
    private readonly AppSettings _settings;
    private string? _cToken;

    public Pingcang(IAPIRequestContext apiContext, ApiLogger? logger = null)
        : base(apiContext, logger)
    {
        _settings = AppSettings.Instance;
    }

    /// <summary>
    /// 设置C_Token
    /// </summary>
    public void SetCToken(string cToken)
    {
        _cToken = cToken;
    }

    /// <summary>
    /// 创建闪电平仓
    /// </summary>
    public async Task<IAPIResponse> CreateBtcOrderAsync()
    {
        if (string.IsNullOrEmpty(_cToken))
        {
            throw new InvalidOperationException("C Token 未设置。请先调用 SetCToken 方法。");
        }

        Dictionary<string, string> formData = new()
        {
            ["client_order_id"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            ["symbol_id"] = "BTCUSDT_PERP",
            ["is_long"] = "1", /// 1是多仓 0是空仓
            ["exchange_id"] = "888",
            ["is_cross"] = "true",
        };

        // 使用配置的 BaseUrl
        string url = $"{_settings.Config.BaseUrl}/api/contract/order/close_promptly?c_token={_cToken}";
        return await PostFormAsync(url, formData).ConfigureAwait(false);
    }

    /// <summary>
    /// 获取订单ID（平仓响应中订单ID在order对象内）
    /// </summary>
    public async Task<string?> GetOrderIdFromResponseAsync(IAPIResponse response) =>
        await ExtractJsonFieldAsync(response, "order.orderId").ConfigureAwait(false);

    /// <summary>
    /// 验证订单创建成功（平仓响应中订单ID在order对象内）
    /// </summary>
    public async Task<bool> IsOrderCreatedSuccessfullyAsync(IAPIResponse response)
    {
        string? orderId = await ExtractJsonFieldAsync(response, "order.orderId").ConfigureAwait(false);
        return !string.IsNullOrEmpty(orderId) && orderId != "Null";
    }
}
