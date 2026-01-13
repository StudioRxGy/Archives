using Microsoft.Playwright;
using CsPlaywrightApi;

Console.WriteLine("开始执行API");

try
{
    // 创建Playwright实例
    using var playwright = await Playwright.CreateAsync();
    
    // 创建API请求上下文
    var apiContext = await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
    {
        BaseURL = "https://www.ast1001.com"
    });

    try
    {
        Console.WriteLine("=== 步骤1: 执行登录 ===");
        var loginApi = new LoginApi(apiContext);
        
        // 执行登录请求并获取响应对象
        var loginResponse = await loginApi.AuthorizeUserAsync();
        var loginResponseText = await loginResponse.TextAsync();
        
        Console.WriteLine($"登录 HTTP状态码: {loginResponse.Status}");
        Console.WriteLine($"登录响应内容: {loginResponseText}");
        Console.WriteLine("登录请求完成\n");

        Console.WriteLine("=== 步骤2: 创建BTC订单 ===");
        // 从登录响应中提取 c_token
        var cToken = TokenExtractor.ExtractCTokenFromHeaders(loginResponse.Headers);
        
        if (!string.IsNullOrEmpty(cToken))
        {
            Console.WriteLine($"成功提取到 c_token: {cToken}");
            
            // 创建BTC API实例并设置token
            var btcApi = new BtcApi(apiContext);
            btcApi.SetCToken(cToken);
            
            // 执行BTC订单创建
            var btcResponseText = await btcApi.CreateBtcOrderAndGetResponseAsync();
            Console.WriteLine("BTC订单创建请求完成");
        }
        else
        {
            Console.WriteLine("未能从登录响应中提取到 c_token");
        }


    }
    finally
    {
        await apiContext.DisposeAsync();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"执行出错: {ex.Message}");
    Console.WriteLine($"详细信息: {ex}");
}

Console.WriteLine("按任意键退出...");
Console.ReadKey();