using Microsoft.Playwright;
using System.Text.Json;

namespace CsPlaywrightApi
{
    public class BtcApi
    {
        private readonly IAPIRequestContext _apiContext;
        private string? _cToken;

        public BtcApi(IAPIRequestContext apiContext)
        {
            _apiContext = apiContext;
        }

        /// <summary>
        /// 设置C Token
        /// </summary>
        /// <param name="cToken">从登录响应中获取的c_token</param>
        public void SetCToken(string cToken)
        {
            _cToken = cToken;
        }

        /// <summary>
        /// 创建BTC合约订单
        /// </summary>
        /// <returns>API响应</returns>
        public async Task<IAPIResponse> CreateBtcOrderAsync()
        {
            if (string.IsNullOrEmpty(_cToken))
            {
                throw new InvalidOperationException("C Token 未设置。请先调用 SetCToken 方法。");
            }

            // 请求参数
            var formData = new Dictionary<string, string>
            {
                ["side"] = "BUY_OPEN",
                ["type"] = "LIMIT",
                ["price_type"] = "MARKET_PRICE",
                ["trigger_price"] = "",
                ["leverage"] = "400",
                ["quantity"] = "3.00",
                ["symbol_id"] = "BTCUSDT_PERP",
                ["client_order_id"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                ["exchange_id"] = "888",
                ["order_side"] = "BUY",
                ["is_cross"] = "true",
                ["time_in_force"] = "IOC",
                ["deduction"] = "score"
            };

            // 转换为URL编码格式
            var formContent = string.Join("&", 
                formData.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var response = await _apiContext.PostAsync($"https://www.ast1001.com/api/contract/order/create?c_token={_cToken}", new APIRequestContextOptions
            {
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/x-www-form-urlencoded"
                },
                Data = formContent
            });

            return response;
        }

        /// <summary>
        /// 获取响应内容
        /// </summary>
        /// <returns>响应的JSON字符串</returns>
        public async Task<string> CreateBtcOrderAndGetResponseAsync()
        {
            Console.WriteLine($"发送BTC订单创建请求到: https://www.ast1001.com/api/contract/order/create");
            Console.WriteLine($"使用C Token: {_cToken}");
            Console.WriteLine($"交易对: BTCUSDT_PERP");
            Console.WriteLine($"订单类型: BUY_OPEN (买入开仓)");
            Console.WriteLine($"杠杆: 400倍");
            Console.WriteLine($"数量: 3.00");
            Console.WriteLine($"价格类型: MARKET_PRICE (市价)");
            
            var response = await CreateBtcOrderAsync();
            var responseText = await response.TextAsync();
            
            Console.WriteLine($"HTTP状态码: {response.Status}");
            Console.WriteLine($"响应头信息:");
            foreach (var header in response.Headers)
            {
                Console.WriteLine($"  {header.Key}: {header.Value}");
            }
            Console.WriteLine($"响应内容: {responseText}");
            
            return responseText;
        }
    }

    /// <summary>
    /// Token 提取工具类
    /// </summary>
    public static class TokenExtractor
    {
        /// <summary>
        /// 从Set-Cookie头中提取c_token
        /// </summary>
        /// <param name="headers">响应头</param>
        /// <returns>c_token值</returns>
        public static string? ExtractCTokenFromHeaders(IDictionary<string, string> headers)
        {
            if (headers.TryGetValue("set-cookie", out var setCookieHeader))
            {
                // 查找c_token
                var cookies = setCookieHeader.Split('\n');
                foreach (var cookie in cookies)
                {
                    if (cookie.Trim().StartsWith("c_token="))
                    {
                        var tokenPart = cookie.Trim().Split(';')[0];
                        return tokenPart.Substring("c_token=".Length);
                    }
                }
            }
            return null;
        }
    }
}