using Microsoft.Playwright;
using CsPlaywrightApi.src.playwright.Flows.Api.Uheyue;
using CsPlaywrightApi.src.playwright.Core.Api;
using CsPlaywrightApi.src.playwright.Core.Logging;
using CsPlaywrightApi.src.playwright.Core.Config;

Console.WriteLine("开始执行API");

try
{
    // 获取配置实例
    var settings = AppSettings.Instance;
    
    Console.WriteLine($"当前环境: {settings.CurrentEnvironment}");
    Console.WriteLine($"Base URL: {settings.Config.BaseUrl}");
    Console.WriteLine($"日志目录: {settings.LogDirectory}");
    
    // 创建日志记录器（使用配置中的设置）
    var logger = new ApiLogger();
    
    // 设置测试上下文信息
    logger.CurrentTestClass = "Program";
    logger.CurrentTestMethod = "Main";
    logger.CurrentSourceFile = "CsPlaywrightApi/src/playwright/Flows/Api/astralx/Uheyue/Program.cs";
    logger.CurrentTestScenario = "U本位合约交易流程";
    logger.CurrentTestDisplayName = "登录->创建U本位订单->闪电平仓";
    logger.CurrentTestCategories = new List<string> { "API测试", "合约交易", "完整流程" };
    logger.CurrentTestPriority = "High";
    
    // 创建Playwright实例
    using var playwright = await Playwright.CreateAsync();
    
    // 创建API请求上下文（使用配置中的BaseURL）
    var apiContext = await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
    {
        BaseURL = settings.Config.BaseUrl
    });

    try
    {
        Console.WriteLine("\n=== 执行登录 ===");
        var loginApi = new Login(apiContext, logger);
        
        // 执行登录
        var loginResponse = await loginApi.AuthorizeUserAsync();
        
        // 验证登录成功
        ApiAssertions.AssertSuccess(loginResponse);
        Console.WriteLine("✓ 登录成功");

        Console.WriteLine("\n=== 提取Token ===");
        // 从登录响应中提取 c_token（从set-cookie头中提取）
        var cToken = ExtractCTokenFromHeaders(loginResponse.Headers);
        
        if (!string.IsNullOrEmpty(cToken))
        {
            Console.WriteLine($"✓ 成功提取到 c_token: {cToken}\n");
            
            Console.WriteLine("=== 创建U本位订单 ===");
            // 创建BTC API实例
            var btcApi = new ShijiaBuy(apiContext, logger);
            btcApi.SetCToken(cToken);
            
            // 执行BTC订单创建
            var btcResponse = await btcApi.CreateBtcOrderAsync();
            
            // 验证订单创建
            ApiAssertions.AssertSuccess(btcResponse);
            
            var isSuccess = await btcApi.IsOrderCreatedSuccessfullyAsync(btcResponse);
            if (isSuccess)
            {
                var orderId = await btcApi.GetOrderIdFromResponseAsync(btcResponse);
                Console.WriteLine($"✓ BTC订单创建成功，订单ID: {orderId}");
                
                Console.WriteLine("\n=== 执行闪电平仓 ===");
                // 创建平仓API实例
                var pingcangApi = new Pingcang(apiContext, logger);
                pingcangApi.SetCToken(cToken);
                
                // 执行平仓操作
                var pingcangResponse = await pingcangApi.CreateBtcOrderAsync();
                
                // 验证平仓操作
                ApiAssertions.AssertSuccess(pingcangResponse);
                
                var isPingcangSuccess = await pingcangApi.IsOrderCreatedSuccessfullyAsync(pingcangResponse);
                if (isPingcangSuccess)
                {
                    var pingcangOrderId = await pingcangApi.GetOrderIdFromResponseAsync(pingcangResponse);
                    Console.WriteLine($"✓ 平仓订单创建成功，订单ID: {pingcangOrderId}");
                }
                else
                {
                    Console.WriteLine("✗ 平仓订单创建失败");
                }
            }
            else
            {
                Console.WriteLine("✗ BTC订单创建失败");
            }
        }
        else
        {
            Console.WriteLine("✗ 未能从登录响应中提取到 c_token");
        }
    }
    finally
    {
        await apiContext.DisposeAsync();
    }
    
    // 生成HTML报告
    await logger.GenerateHtmlReportAsync();
    
    Console.WriteLine("\n=== 执行完成 ===");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ 执行出错: {ex.Message}");
    Console.WriteLine($"详细信息: {ex}");
}

Console.WriteLine("\n按任意键退出...");
Console.ReadKey();

// 从Set-Cookie头中提取c_token的辅助方法
static string? ExtractCTokenFromHeaders(IDictionary<string, string> headers)
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
