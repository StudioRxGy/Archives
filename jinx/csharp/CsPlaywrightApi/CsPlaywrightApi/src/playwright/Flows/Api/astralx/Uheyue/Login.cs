using Microsoft.Playwright;
using CsPlaywrightApi.src.playwright.Core.Api;
using CsPlaywrightApi.src.playwright.Core.Logging;
using CsPlaywrightApi.src.playwright.Core.Config;

namespace CsPlaywrightApi.src.playwright.Flows.Api.Uheyue
{
    /// <summary>
    /// 登录API（使用ApiClient基类）
    /// </summary>
    public class Login : ApiClient
    {
        private readonly AppSettings _settings;

        public Login(IAPIRequestContext apiContext, ApiLogger? logger = null) 
            : base(apiContext, logger)
        {
            _settings = AppSettings.Instance;
        }

        /// <summary>
        /// 执行用户登录授权请求
        /// </summary>
        public async Task<IAPIResponse> AuthorizeUserAsync()
        {
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

            // 使用配置的 BaseUrl
            var url = $"{_settings.Config.BaseUrl}/api/user/authorize";
            return await PostFormAsync(url, formData);
        }

        /// <summary>
        /// 从登录响应中提取token
        /// </summary>
        public async Task<string?> GetTokenFromResponseAsync(IAPIResponse response)
        {
            return await ExtractJsonFieldAsync(response, "data.token");
        }
    }
}
