using Microsoft.Playwright;
using CsPlaywrightApi.src.playwright.Flows.Api.Uheyue;
using CsPlaywrightApi.src.playwright.Core.Api;
using CsPlaywrightApi.src.playwright.Core.Logging;
using CsPlaywrightApi.src.playwright.Core.Config;
using Xunit.Abstractions;

namespace CsPlaywrightApi.src.playwright.Tests.astralx.heyuye
{
    /// <summary>
    /// Uheyue API 自动化测试
    /// 使用 IClassFixture 共享 Logger 和其他资源，所有测试共享同一个日志会话
    /// </summary>
    public class UheyueApiTests : IClassFixture<Fixture>
    {
        private readonly Fixture _fixture;
        private readonly ITestOutputHelper _output;

        public UheyueApiTests(Fixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        /// <summary>
        /// 设置测试上下文信息到Logger
        /// </summary>
        private void SetTestContext(string methodName, string displayName, string scenario, 
            List<string> categories, string priority)
        {
            var sourceFile = @"D:\Test tools\Docs\Test Case\jinx\csharp\CsPlaywrightApi\CsPlaywrightApi\src\playwright\Tests\Api\astralx\heyuye\UheyueApiTests.cs";
            
            _fixture.Logger.CurrentTestMethod = methodName;
            _fixture.Logger.CurrentTestClass = "CsPlaywrightApi.src.playwright.Tests.astralx.heyuye.UheyueApiTests";
            _fixture.Logger.CurrentSourceFile = sourceFile;
            _fixture.Logger.CurrentTestScenario = scenario;
            _fixture.Logger.CurrentTestCategories = categories;
            _fixture.Logger.CurrentTestPriority = priority;
            _fixture.Logger.CurrentTestDisplayName = displayName;
        }

        /// <summary>
        /// 辅助方法：执行登录并获取 Token
        /// </summary>
        private async Task<string?> LoginAndGetTokenAsync()
        {
            var loginApi = new Login(_fixture.ApiContext!, _fixture.Logger);
            var loginResponse = await loginApi.AuthorizeUserAsync();
            
            ApiAssertions.AssertSuccess(loginResponse);
            
            // 从 Set-Cookie 头中提取 c_token
            if (loginResponse.Headers.TryGetValue("set-cookie", out var setCookieHeader))
            {
                var cookies = setCookieHeader.Split('\n');
                foreach (var cookie in cookies)
                {
                    if (cookie.Trim().StartsWith("c_token="))
                    {
                        var tokenPart = cookie.Trim().Split(';')[0];
                        return tokenPart["c_token=".Length..];
                    }
                }
            }
            return null;
        }

        #region 登录测试

        [Fact(DisplayName = "测试01 - 用户登录成功")]
        [Trait("Category", "API")]
        [Trait("Category", "Login")]
        [Trait("Fast", "true")]
        [Trait("Priority", "High")]
        public async Task Test01_Login_ShouldReturnSuccess()
        {
            // 设置测试上下文
            SetTestContext("Test01_Login_ShouldReturnSuccess", "测试01 - 用户登录成功", 
                "登录测试", new List<string> { "API", "Login" }, "High");
            
            // Arrange
            var loginApi = new Login(_fixture.ApiContext!, _fixture.Logger);

            // Act
            var response = await loginApi.AuthorizeUserAsync();

            // Assert
            ApiAssertions.AssertSuccess(response);
            Assert.Equal(200, response.Status);
            
            _output.WriteLine("✓ 登录成功");
            _output.WriteLine($"响应状态码: {response.Status}");
            _output.WriteLine($"响应内容: {await response.TextAsync()}");
        }

        [Fact(DisplayName = "测试02 - 登录后能提取到Token")]
        [Trait("Category", "Login")]
        [Trait("Fast", "true")]
        [Trait("Priority", "High")]
        public async Task Test02_Login_ShouldExtractToken()
        {
            // 设置测试上下文
            SetTestContext("Test02_Login_ShouldExtractToken", "测试02 - 登录后能提取到Token", 
                "登录测试", new List<string> { "Login" }, "High");
            
            // Arrange
            var loginApi = new Login(_fixture.ApiContext!, _fixture.Logger);

            // Act
            var response = await loginApi.AuthorizeUserAsync();
            var token = await LoginAndGetTokenAsync();

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            
            _output.WriteLine($"✓ 成功提取 Token: {token}");
        }

        #endregion

        #region 市价买入测试

        [Fact(DisplayName = "测试03 - 创建BTC市价买入订单")]
        [Trait("Category", "API")]
        [Trait("Category", "Trade")]
        [Trait("Category", "BuyOrder")]
        [Trait("Slow", "true")]
        [Trait("Priority", "High")]
        public async Task Test03_CreateBtcOrder_ShouldSuccess()
        {
            // 设置测试上下文
            SetTestContext("Test03_CreateBtcOrder_ShouldSuccess", "测试03 - 创建BTC市价买入订单", 
                "市价买入测试", new List<string> { "API", "Trade", "BuyOrder" }, "High");
            
            // Arrange - 先登录获取 Token
            var cToken = await LoginAndGetTokenAsync();
            Assert.NotNull(cToken);

            var btcApi = new ShijiaBuy(_fixture.ApiContext!, _fixture.Logger);
            btcApi.SetCToken(cToken);

            // Act
            var response = await btcApi.CreateBtcOrderAsync();

            // Assert
            ApiAssertions.AssertSuccess(response);
            
            var isSuccess = await btcApi.IsOrderCreatedSuccessfullyAsync(response);
            Assert.True(isSuccess, "订单创建失败");
            
            var orderId = await btcApi.GetOrderIdFromResponseAsync(response);
            Assert.NotNull(orderId);
            
            _output.WriteLine($"✓ BTC订单创建成功，订单ID: {orderId}");
            _output.WriteLine($"响应状态码: {response.Status}");
            _output.WriteLine($"响应内容: {await response.TextAsync()}");
        }

        [Fact(DisplayName = "测试04 - 验证订单响应包含必要字段")]
        [Trait("Category", "Trade")]
        [Trait("Category", "Validation")]
        [Trait("Fast", "true")]
        [Trait("Priority", "Medium")]
        public async Task Test04_CreateBtcOrder_ShouldContainOrderId()
        {
            // 设置测试上下文
            SetTestContext("Test04_CreateBtcOrder_ShouldContainOrderId", "测试04 - 验证订单响应包含必要字段", 
                "市价买入测试", new List<string> { "Trade", "Validation" }, "Medium");
            
            // Arrange
            var cToken = await LoginAndGetTokenAsync();
            Assert.NotNull(cToken);

            var btcApi = new ShijiaBuy(_fixture.ApiContext!, _fixture.Logger);
            btcApi.SetCToken(cToken);

            // Act
            var response = await btcApi.CreateBtcOrderAsync();

            // Assert
            await ApiAssertions.AssertJsonFieldExistsAsync(response, "orderId");
            
            _output.WriteLine("✓ 订单响应包含 orderId 字段");
        }

        #endregion

        #region 闪电平仓测试

        [Fact(DisplayName = "测试05 - 执行闪电平仓")]
        [Trait("Category", "API")]
        [Trait("Category", "Trade")]
        [Trait("Category", "ClosePosition")]
        [Trait("Slow", "true")]
        [Trait("Priority", "High")]
        public async Task Test05_ClosePosition_ShouldSuccess()
        {
            // 设置测试上下文
            SetTestContext("Test05_ClosePosition_ShouldSuccess", "测试05 - 执行闪电平仓", 
                "闪电平仓测试", new List<string> { "API", "Trade", "ClosePosition" }, "High");
            
            // Arrange - 先登录获取 Token
            var cToken = await LoginAndGetTokenAsync();
            Assert.NotNull(cToken);

            var pingcangApi = new Pingcang(_fixture.ApiContext!, _fixture.Logger);
            pingcangApi.SetCToken(cToken);

            // Act
            var response = await pingcangApi.CreateBtcOrderAsync();

            // Assert
            ApiAssertions.AssertSuccess(response);
            
            var isSuccess = await pingcangApi.IsOrderCreatedSuccessfullyAsync(response);
            Assert.True(isSuccess, "平仓订单创建失败");
            
            var orderId = await pingcangApi.GetOrderIdFromResponseAsync(response);
            Assert.NotNull(orderId);
            
            _output.WriteLine($"✓ 平仓订单创建成功，订单ID: {orderId}");
            _output.WriteLine($"响应状态码: {response.Status}");
            _output.WriteLine($"响应内容: {await response.TextAsync()}");
        }

        [Fact(DisplayName = "测试06 - 验证平仓响应包含订单信息")]
        [Trait("Category", "Trade")]
        [Trait("Category", "Validation")]
        [Trait("Fast", "true")]
        [Trait("Priority", "Medium")]
        public async Task Test06_ClosePosition_ShouldContainOrderInfo()
        {
            // 设置测试上下文
            SetTestContext("Test06_ClosePosition_ShouldContainOrderInfo", "测试06 - 验证平仓响应包含订单信息", 
                "闪电平仓测试", new List<string> { "Trade", "Validation" }, "Medium");
            
            // Arrange
            var cToken = await LoginAndGetTokenAsync();
            Assert.NotNull(cToken);

            var pingcangApi = new Pingcang(_fixture.ApiContext!, _fixture.Logger);
            pingcangApi.SetCToken(cToken);

            // Act
            var response = await pingcangApi.CreateBtcOrderAsync();

            // Assert
            await ApiAssertions.AssertJsonFieldExistsAsync(response, "order.orderId");
            
            _output.WriteLine("✓ 平仓响应包含 order.orderId 字段");
        }

        #endregion

        #region 完整流程测试

        [Fact(DisplayName = "测试07 - 完整交易流程（登录→开仓→平仓）")]
        [Trait("Category", "API")]
        [Trait("Category", "E2E")]
        [Trait("Category", "FullFlow")]
        [Trait("Slow", "true")]
        [Trait("Priority", "Critical")]
        [Trait("Smoke", "true")]
        public async Task Test07_CompleteTradeFlow_ShouldSuccess()
        {
            // 设置测试上下文
            SetTestContext("Test07_CompleteTradeFlow_ShouldSuccess", "测试07 - 完整交易流程（登录→开仓→平仓）", 
                "完整流程测试", new List<string> { "API", "E2E", "FullFlow" }, "Critical");
            
            // Step 1: 登录
            _output.WriteLine("=== 步骤1: 执行登录 ===");
            var cToken = await LoginAndGetTokenAsync();
            Assert.NotNull(cToken);
            _output.WriteLine($"✓ 登录成功，Token: {cToken}");

            // Step 2: 创建买入订单
            _output.WriteLine("\n=== 步骤2: 创建BTC买入订单 ===");
            var btcApi = new ShijiaBuy(_fixture.ApiContext!, _fixture.Logger);
            btcApi.SetCToken(cToken);
            
            var buyResponse = await btcApi.CreateBtcOrderAsync();
            ApiAssertions.AssertSuccess(buyResponse);
            
            var buyOrderId = await btcApi.GetOrderIdFromResponseAsync(buyResponse);
            Assert.NotNull(buyOrderId);
            _output.WriteLine($"✓ 买入订单创建成功，订单ID: {buyOrderId}");
            _output.WriteLine($"买入响应: {await buyResponse.TextAsync()}");

            // Step 3: 执行平仓
            _output.WriteLine("\n=== 步骤3: 执行闪电平仓 ===");
            var pingcangApi = new Pingcang(_fixture.ApiContext!, _fixture.Logger);
            pingcangApi.SetCToken(cToken);
            
            var closeResponse = await pingcangApi.CreateBtcOrderAsync();
            ApiAssertions.AssertSuccess(closeResponse);
            
            var closeOrderId = await pingcangApi.GetOrderIdFromResponseAsync(closeResponse);
            Assert.NotNull(closeOrderId);
            _output.WriteLine($"✓ 平仓订单创建成功，订单ID: {closeOrderId}");
            _output.WriteLine($"平仓响应: {await closeResponse.TextAsync()}");

            _output.WriteLine("\n=== 完整流程测试通过 ===");
        }

        #endregion

        #region 异常场景测试

        [Fact(DisplayName = "测试08 - 未设置Token时创建订单应失败")]
        [Trait("Category", "API")]
        [Trait("Category", "Exception")]
        [Trait("Category", "Negative")]
        [Trait("Fast", "true")]
        [Trait("Priority", "Medium")]
        public async Task Test08_CreateOrderWithoutToken_ShouldThrowException()
        {
            // 设置测试上下文
            SetTestContext("Test08_CreateOrderWithoutToken_ShouldThrowException", "测试08 - 未设置Token时创建订单应失败", 
                "异常场景测试", new List<string> { "API", "Exception", "Negative" }, "Medium");
            
            // Arrange
            var btcApi = new ShijiaBuy(_fixture.ApiContext!, _fixture.Logger);
            // 故意不设置 Token

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await btcApi.CreateBtcOrderAsync()
            );
            
            _output.WriteLine("✓ 未设置Token时正确抛出异常");
        }

        [Fact(DisplayName = "测试09 - 使用空Token创建订单应失败")]
        [Trait("Category", "Exception")]
        [Trait("Category", "Negative")]
        [Trait("Fast", "true")]
        [Trait("Priority", "Medium")]
        public async Task Test09_CreateOrderWithEmptyToken_ShouldThrowException()
        {
            // 设置测试上下文
            SetTestContext("Test09_CreateOrderWithEmptyToken_ShouldThrowException", "测试09 - 使用空Token创建订单应失败", 
                "异常场景测试", new List<string> { "Exception", "Negative" }, "Medium");
            
            // Arrange
            var btcApi = new ShijiaBuy(_fixture.ApiContext!, _fixture.Logger);
            btcApi.SetCToken(""); // 设置空 Token

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await btcApi.CreateBtcOrderAsync()
            );
            
            _output.WriteLine("✓ 使用空Token时正确抛出异常");
        }

        #endregion
    }
}
