using CsPlaywrightApi.src.playwright.Core.Api;
using CsPlaywrightApi.src.playwright.Core.Logging;
using CsPlaywrightApi.src.playwright.Core.Config;
using Microsoft.Playwright;

namespace CsPlaywrightApi.src.playwright.Flows.Api.Uheyue
{
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

            var formData = new Dictionary<string, string>
            {
                ["client_order_id"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                ["symbol_id"] = "BTCUSDT_PERP",
                ["is_long"] = "1", /// 1是多仓 0是空仓
                ["exchange_id"] = "888",
                ["is_cross"] = "true",
            };

            // 使用配置的 BaseUrl
            var url = $"{_settings.Config.BaseUrl}/api/contract/order/close_promptly?c_token={_cToken}";
            return await PostFormAsync(url, formData);
        }

        /// <summary>
        /// 获取订单ID（平仓响应中订单ID在order对象内）
        /// </summary>
        public async Task<string?> GetOrderIdFromResponseAsync(IAPIResponse response)
        {
            return await ExtractJsonFieldAsync(response, "order.orderId");
        }

        /// <summary>
        /// 验证订单创建成功（平仓响应中订单ID在order对象内）
        /// </summary>
        public async Task<bool> IsOrderCreatedSuccessfullyAsync(IAPIResponse response)
        {
            var orderId = await ExtractJsonFieldAsync(response, "order.orderId");
            return !string.IsNullOrEmpty(orderId) && orderId != "Null";
        }
    }
}
