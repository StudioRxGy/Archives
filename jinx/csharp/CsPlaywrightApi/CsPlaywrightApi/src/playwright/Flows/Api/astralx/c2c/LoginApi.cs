// ---------------------------------------------------------------
// 文件描述：C2C 登录 API
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
/// 登录API（使用ApiClient基类）
/// </summary>
public class LoginApi : ApiClient
{
    private readonly AppSettings _settings;

    public LoginApi(IAPIRequestContext apiContext, ApiLogger? logger = null)
        : base(apiContext, logger)
    {
        _settings = AppSettings.Instance;
    }

    /// <summary>
    /// 执行用户登录授权请求
    /// </summary>
    public async Task<IAPIResponse> AuthorizeUserAsync()
    {
        Dictionary<string, string> formData = new()
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
        string url = $"{_settings.Config.BaseUrl}/api/user/authorize";
        return await PostFormAsync(url, formData).ConfigureAwait(false);
    }

    /// <summary>
    /// 从登录响应中提取token
    /// </summary>
    public async Task<string?> GetTokenFromResponseAsync(IAPIResponse response) =>
        await ExtractJsonFieldAsync(response, "data.token").ConfigureAwait(false);
}
