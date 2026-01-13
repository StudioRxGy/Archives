using Microsoft.Playwright;

namespace CsPlaywrightApi
{
    public class LoginApi
    {
        private readonly IAPIRequestContext _apiContext;

        public LoginApi(IAPIRequestContext apiContext)
        {
            _apiContext = apiContext;
        }

        /// <summary>
        /// 执行用户登录授权请求
        /// </summary>
        /// <returns>API响应</returns>
        public async Task<IAPIResponse> AuthorizeUserAsync()
        {
            // 请求参数
            var formData = new Dictionary<string, string>
            {
                ["verify_code"] = "",
                ["type"] = "0",
                ["login_type"] = "email",
                ["national_code"] = "",
                ["order_id"] = "",
                ["email"] = "Aaaanew@ast1.com",
                ["password"] = "2c9341ca4cf3d87b9e4eb905d6a3ec45",
                ["username"] = "Aaaanew@ast1.com",
                ["captcha_response"] = "",
                ["secure_login_flag"] = "true"
            };

            // 转换为URL编码格式
            var formContent = string.Join("&", 
                formData.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var response = await _apiContext.PostAsync("https://www.ast1001.com/api/user/authorize", new APIRequestContextOptions
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
        /// 获取Json响应内容
        /// </summary>
        /// <returns>响应的JSON字符串</returns>
        public async Task<string> LoginAndGetResponseAsync()
        {
            Console.WriteLine($"发送登录请求到: https://www.ast1001.com/api/user/authorize");
            Console.WriteLine($"邮箱: Aaaanew@ast1.com");
            Console.WriteLine($"用户名: Aaaanew@ast1.com");
            
            var response = await AuthorizeUserAsync();
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
}