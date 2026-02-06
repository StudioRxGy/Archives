// ---------------------------------------------------------------
// 文件描述：C2C 买入测试
// 创建时间：
// 创建人：eleven
// 修改历史：
// ---------------------------------------------------------------

using CsPlaywrightApi.src.playwright.Core.Api;
using CsPlaywrightApi.src.playwright.Flows.Api.astralx.c2c;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace CsPlaywrightApi.src.playwright.Tests.Api.astralx.c2c;

/// <summary>
/// C2C API 自动化测试
/// 使用 IClassFixture 共享 Logger 和其他资源，所有测试共享同一个日志会话
/// </summary>
public class C2cBuyTests : IClassFixture<Fixture>
    {
        private readonly Fixture _fixture;
        private readonly ITestOutputHelper _output;

        public C2cBuyTests(Fixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        /// <summary>
        /// 设置测试上下文信息到Logger
        /// </summary>
        private void SetTestContext(
            string methodName,
            string displayName,
            string scenario,
            List<string> categories,
            string priority
        )
        {
            var sourceFile = @"CsPlaywrightApi\src\playwright\Tests\Api\astralx\c2c\c2cbuyTest.cs";

            _fixture.Logger.CurrentTestMethod = methodName;
            _fixture.Logger.CurrentTestClass = "CsPlaywrightApi.src.playwright.Tests.Api.astralx.c2c.C2cBuyTests";
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
            var loginApi = new LoginApi(_fixture.ApiContext!, _fixture.Logger);
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

        [Fact(DisplayName = "测试01 - C2C用户登录成功")]
        [Trait("Category", "API")]
        [Trait("Category", "Login")]
        [Trait("Category", "C2C")]
        [Trait("Fast", "true")]
        [Trait("Priority", "High")]
        public async Task Test01_C2cLogin_ShouldReturnSuccess()
        {
            // 设置测试上下文
            SetTestContext(
                "Test01_C2cLogin_ShouldReturnSuccess",
                "测试01 - C2C用户登录成功",
                "C2C登录测试",
                new List<string> { "API", "Login", "C2C" },
                "High"
            );

            // Arrange
            var loginApi = new LoginApi(_fixture.ApiContext!, _fixture.Logger);

            // Act
            var response = await loginApi.AuthorizeUserAsync();

            // Assert
            ApiAssertions.AssertSuccess(response);
            Assert.Equal(200, response.Status);

            _output.WriteLine("✓ C2C登录成功");
            _output.WriteLine($"响应状态码: {response.Status}");
            _output.WriteLine($"响应内容: {await response.TextAsync()}");
        }

        [Fact(DisplayName = "测试02 - C2C登录后能提取到Token")]
        [Trait("Category", "Login")]
        [Trait("Category", "C2C")]
        [Trait("Fast", "true")]
        [Trait("Priority", "High")]
        public async Task Test02_C2cLogin_ShouldExtractToken()
        {
            // 设置测试上下文
            SetTestContext(
                "Test02_C2cLogin_ShouldExtractToken",
                "测试02 - C2C登录后能提取到Token",
                "C2C登录测试",
                new List<string> { "Login", "C2C" },
                "High"
            );

            // Arrange
            var loginApi = new LoginApi(_fixture.ApiContext!, _fixture.Logger);

            // Act
            var response = await loginApi.AuthorizeUserAsync();
            var token = await LoginAndGetTokenAsync();

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            _output.WriteLine($"✓ 成功提取 C2C Token: {token}");
        }

        #endregion

        #region C2C下单测试

        [Fact(DisplayName = "测试03 - 创建C2C购买订单")]
        [Trait("Category", "API")]
        [Trait("Category", "Trade")]
        [Trait("Category", "C2C")]
        [Trait("Category", "BuyOrder")]
        [Trait("Slow", "true")]
        [Trait("Priority", "High")]
        public async Task Test03_CreateC2cOrder_ShouldSuccess()
        {
            // 设置测试上下文
            SetTestContext(
                "Test03_CreateC2cOrder_ShouldSuccess",
                "测试03 - 创建C2C购买订单",
                "C2C下单测试",
                new List<string> { "API", "Trade", "C2C", "BuyOrder" },
                "High"
            );

            // Arrange - 先登录获取 Token
            var cToken = await LoginAndGetTokenAsync();
            Assert.NotNull(cToken);

            var c2cApi = new Buyc2c(_fixture.ApiContext!, _fixture.Logger);
            c2cApi.SetCToken(cToken);

            // Act
            var response = await c2cApi.CreateC2cOrderAsync();

            // Assert
            ApiAssertions.AssertSuccess(response);

            var isSuccess = await c2cApi.IsOrderCreatedSuccessfullyAsync(response);
            Assert.True(isSuccess, "C2C订单创建失败");

            var orderId = await c2cApi.GetOrderIdFromResponseAsync(response);
            Assert.NotNull(orderId);

            _output.WriteLine($"✓ C2C订单创建成功，订单ID: {orderId}");
            _output.WriteLine($"响应状态码: {response.Status}");
            _output.WriteLine($"响应内容: {await response.TextAsync()}");
        }

        [Fact(DisplayName = "测试04 - 验证C2C订单响应包含必要字段")]
        [Trait("Category", "Trade")]
        [Trait("Category", "C2C")]
        [Trait("Category", "Validation")]
        [Trait("Fast", "true")]
        [Trait("Priority", "Medium")]
        public async Task Test04_CreateC2cOrder_ShouldContainOrderData()
        {
            // 设置测试上下文
            SetTestContext(
                "Test04_CreateC2cOrder_ShouldContainOrderData",
                "测试04 - 验证C2C订单响应包含必要字段",
                "C2C下单测试",
                new List<string> { "Trade", "C2C", "Validation" },
                "Medium"
            );

            // Arrange
            var cToken = await LoginAndGetTokenAsync();
            Assert.NotNull(cToken);

            var c2cApi = new Buyc2c(_fixture.ApiContext!, _fixture.Logger);
            c2cApi.SetCToken(cToken);

            // Act
            var response = await c2cApi.CreateC2cOrderAsync();

            // Assert
            await ApiAssertions.AssertJsonFieldExistsAsync(response, "data");

            _output.WriteLine("✓ C2C订单响应包含 data 字段");
        }

        [Fact(DisplayName = "测试05 - C2C订单参数验证")]
        [Trait("Category", "Trade")]
        [Trait("Category", "C2C")]
        [Trait("Category", "Validation")]
        [Trait("Fast", "true")]
        [Trait("Priority", "Medium")]
        public async Task Test05_CreateC2cOrder_ShouldValidateParameters()
        {
            // 设置测试上下文
            SetTestContext(
                "Test05_CreateC2cOrder_ShouldValidateParameters",
                "测试05 - C2C订单参数验证",
                "C2C参数验证测试",
                new List<string> { "Trade", "C2C", "Validation" },
                "Medium"
            );

            // Arrange
            var cToken = await LoginAndGetTokenAsync();
            Assert.NotNull(cToken);

            var c2cApi = new Buyc2c(_fixture.ApiContext!, _fixture.Logger);
            c2cApi.SetCToken(cToken);

            // Act
            var response = await c2cApi.CreateC2cOrderAsync();

            // Assert - 验证响应结构
            ApiAssertions.AssertSuccess(response);

            var responseText = await response.TextAsync();
            Assert.NotNull(responseText);
            Assert.NotEmpty(responseText);

            _output.WriteLine("✓ C2C订单参数验证通过");
            _output.WriteLine($"响应内容: {responseText}");
        }

        #endregion

        #region 异常场景测试

        [Fact(DisplayName = "测试06 - 未设置Token时创建C2C订单应失败")]
        [Trait("Category", "API")]
        [Trait("Category", "C2C")]
        [Trait("Category", "Exception")]
        [Trait("Category", "Negative")]
        [Trait("Fast", "true")]
        [Trait("Priority", "Medium")]
        public async Task Test06_CreateC2cOrderWithoutToken_ShouldThrowException()
        {
            // 设置测试上下文
            SetTestContext(
                "Test06_CreateC2cOrderWithoutToken_ShouldThrowException",
                "测试06 - 未设置Token时创建C2C订单应失败",
                "C2C异常场景测试",
                new List<string> { "API", "C2C", "Exception", "Negative" },
                "Medium"
            );

            // Arrange
            var c2cApi = new Buyc2c(_fixture.ApiContext!, _fixture.Logger);
            // 故意不设置 Token

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await c2cApi.CreateC2cOrderAsync());

            _output.WriteLine("✓ 未设置Token时正确抛出异常");
        }

        [Fact(DisplayName = "测试07 - 使用空Token创建C2C订单应失败")]
        [Trait("Category", "C2C")]
        [Trait("Category", "Exception")]
        [Trait("Category", "Negative")]
        [Trait("Fast", "true")]
        [Trait("Priority", "Medium")]
        public async Task Test07_CreateC2cOrderWithEmptyToken_ShouldThrowException()
        {
            // 设置测试上下文
            SetTestContext(
                "Test07_CreateC2cOrderWithEmptyToken_ShouldThrowException",
                "测试07 - 使用空Token创建C2C订单应失败",
                "C2C异常场景测试",
                new List<string> { "C2C", "Exception", "Negative" },
                "Medium"
            );

            // Arrange
            var c2cApi = new Buyc2c(_fixture.ApiContext!, _fixture.Logger);
            c2cApi.SetCToken(""); // 设置空 Token

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await c2cApi.CreateC2cOrderAsync());

            _output.WriteLine("✓ 使用空Token时正确抛出异常");
        }

        #endregion

        #region 完整流程测试

        [Fact(DisplayName = "测试08 - 完整C2C交易流程（登录→下单）")]
        [Trait("Category", "API")]
        [Trait("Category", "C2C")]
        [Trait("Category", "E2E")]
        [Trait("Category", "FullFlow")]
        [Trait("Slow", "true")]
        [Trait("Priority", "Critical")]
        [Trait("Smoke", "true")]
        public async Task Test08_CompleteC2cTradeFlow_ShouldSuccess()
        {
            // 设置测试上下文
            SetTestContext(
                "Test08_CompleteC2cTradeFlow_ShouldSuccess",
                "测试08 - 完整C2C交易流程（登录→下单）",
                "C2C完整流程测试",
                new List<string> { "API", "C2C", "E2E", "FullFlow" },
                "Critical"
            );

            // Step 1: 登录
            _output.WriteLine("=== 步骤1: 执行C2C登录 ===");
            var cToken = await LoginAndGetTokenAsync();
            Assert.NotNull(cToken);
            _output.WriteLine($"✓ C2C登录成功，Token: {cToken}");

            // Step 2: 创建C2C订单
            _output.WriteLine("\n=== 步骤2: 创建C2C购买订单 ===");
            var c2cApi = new Buyc2c(_fixture.ApiContext!, _fixture.Logger);
            c2cApi.SetCToken(cToken);

            var orderResponse = await c2cApi.CreateC2cOrderAsync();
            ApiAssertions.AssertSuccess(orderResponse);

            var orderId = await c2cApi.GetOrderIdFromResponseAsync(orderResponse);
            Assert.NotNull(orderId);
            _output.WriteLine($"✓ C2C订单创建成功，订单ID: {orderId}");
            _output.WriteLine($"订单响应: {await orderResponse.TextAsync()}");

            _output.WriteLine("\n=== C2C完整流程测试通过 ===");
        }

        #endregion

        #region 性能测试

        [Fact(DisplayName = "测试09 - C2C订单创建性能测试")]
        [Trait("Category", "C2C")]
        [Trait("Category", "Performance")]
        [Trait("Slow", "true")]
        [Trait("Priority", "Low")]
        public async Task Test09_C2cOrderCreation_PerformanceTest()
        {
            // 设置测试上下文
            SetTestContext(
                "Test09_C2cOrderCreation_PerformanceTest",
                "测试09 - C2C订单创建性能测试",
                "C2C性能测试",
                new List<string> { "C2C", "Performance" },
                "Low"
            );

            // Arrange
            var cToken = await LoginAndGetTokenAsync();
            Assert.NotNull(cToken);

            var c2cApi = new Buyc2c(_fixture.ApiContext!, _fixture.Logger);
            c2cApi.SetCToken(cToken);

            // Act - 测量响应时间
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await c2cApi.CreateC2cOrderAsync();
            stopwatch.Stop();

            // Assert
            ApiAssertions.AssertSuccess(response);

            // 验证响应时间在合理范围内（例如小于5秒）
            Assert.True(
                stopwatch.ElapsedMilliseconds < 5000,
                $"C2C订单创建耗时过长: {stopwatch.ElapsedMilliseconds}ms"
            );

            _output.WriteLine($"✓ C2C订单创建耗时: {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion
    }
