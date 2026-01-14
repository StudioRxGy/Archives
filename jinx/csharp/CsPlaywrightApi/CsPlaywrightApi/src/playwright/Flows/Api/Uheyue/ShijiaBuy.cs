using Microsoft.Playwright;
using CsPlaywrightApi.src.playwright.Core.Api;
using CsPlaywrightApi.src.playwright.Flows.Api.Uheyue;
using CsPlaywrightApi.src.playwright.Core.Logging;

namespace CsPlaywrightApi.src.playwright.Flows.Api.Uheyue
{
    /// <summary>
    /// BTC交易API（使用ApiClient基类）
    /// </summary>
    public class ShijiaBuy : ApiClient
    {
        private string? _cToken;

        public ShijiaBuy(IAPIRequestContext apiContext, ApiLogger? logger = null) 
            : base(apiContext, logger)
        {
        }

        /// <summary>
        /// 设置C Token
        /// </summary>
        public void SetCToken(string cToken)
        {
            _cToken = cToken;
        }

        /// <summary>
        /// 创建BTC合约订单
        /// </summary>
        public async Task<IAPIResponse> CreateBtcOrderAsync()
        {
            if (string.IsNullOrEmpty(_cToken))
            {
                throw new InvalidOperationException("C Token 未设置。请先调用 SetCToken 方法。");
            }

            var formData = new Dictionary<string, string>
            {
                ["side"] = "BUY_OPEN",
                ["type"] = "LIMIT",
                ["price_type"] = "MARKET_PRICE",
                ["trigger_price"] = "",
                ["leverage"] = "400",
                ["quantity"] = "1.00",
                ["symbol_id"] = "BTCUSDT_PERP",
                ["client_order_id"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                ["exchange_id"] = "888",
                ["order_side"] = "BUY",
                ["is_cross"] = "true",
                ["time_in_force"] = "IOC",
                ["deduction"] = "score"
            };

            var url = $"https://www.ast1001.com/api/contract/order/create?c_token={_cToken}";
            return await PostFormAsync(url, formData);
        }

        /// <summary>
        /// 获取订单ID
        /// </summary>
        public async Task<string?> GetOrderIdFromResponseAsync(IAPIResponse response)
        {
            return await ExtractJsonFieldAsync(response, "orderId");
        }

        /// <summary>
        /// 验证订单创建成功
        /// </summary>
        public async Task<bool> IsOrderCreatedSuccessfullyAsync(IAPIResponse response)
        {
            var orderId = await ExtractJsonFieldAsync(response, "orderId");
            return !string.IsNullOrEmpty(orderId) && orderId != "Null";
        }
    }
}
