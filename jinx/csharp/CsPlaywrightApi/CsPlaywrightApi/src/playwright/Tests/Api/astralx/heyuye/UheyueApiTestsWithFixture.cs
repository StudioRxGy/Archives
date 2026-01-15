using Microsoft.Playwright;
using CsPlaywrightApi.src.playwright.Flows.Api.Uheyue;
using CsPlaywrightApi.src.playwright.Core.Api;
using CsPlaywrightApi.src.playwright.Core.Logging;
using CsPlaywrightApi.src.playwright.Core.Config;
using Xunit.Abstractions;

namespace CsPlaywrightApi.src.playwright.Tests.astralx.heyuye
{
    /// <summary>
    /// 共享的测试 Fixture - 用于在多个测试间共享登录状态
    /// </summary>
    public class UheyueApiFixture : IAsyncLifetime
    {
        private readonly AppSettings _settings;
        
        public IPlaywright? Playwright { get; private set; }
        public IAPIRequestContext? ApiContext { get; private set; }
        public ApiLogger? Logger { get; private set; }
        public string? CToken { get; private set; }

        public UheyueApiFixture()
        {
            _settings = AppSettings.Instance;
        }

        public async Task InitializeAsync()
        {
            // 打印配置信息
            _settings.PrintConfigInfo();
            
            // 使用配置创建日志记录器
            Logger = new ApiLogger(enableConsoleLog: _settings.EnableConsoleLog);
            
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            ApiContext = await Playwright.APIRequest.NewContextAsync(new()
            {
                BaseURL = _settings.Config.BaseUrl,
                Timeout = _settings.Config.Timeout * 1000 // 转换为毫秒
            });

            Console.WriteLine($"Fixture 初始化完成 - 环境: {_settings.CurrentEnvironment}");
            Console.WriteLine($"Base URL: {_settings.Config.BaseUrl}");
            
            // 执行一次登录，获取 Token 供所有测试使用
            await LoginAsync();
        }

        public async Task DisposeAsync()
        {
            if (ApiContext != null)
            {
                await ApiContext.DisposeAsync();
            }
            Playwright?.Dispose();

            if (Logger != null)
            {
                await Logger.GenerateHtmlReportAsync();
            }
        }

        private async Task LoginAsync()
        {
            var loginApi = new Login(ApiContext!, Logger);
            var loginResponse = await loginApi.AuthorizeUserAsync();
            
            if (loginResponse.Headers.TryGetValue("set-cookie", out var setCookieHeader))
            {
                var cookies = setCookieHeader.Split('\n');
                foreach (var cookie in cookies)
                {
                    if (cookie.Trim().StartsWith("c_token="))
                    {
                        var tokenPart = cookie.Trim().Split(';')[0];
                        CToken = tokenPart["c_token=".Length..];
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 定义测试集合 - 使用共享的 Fixture
    /// </summary>
    [CollectionDefinition("Uheyue API Collection")]
    public class UheyueApiCollection : ICollectionFixture<UheyueApiFixture>
    {
        // 这个类不需要任何代码，仅用于定义集合
    }

    /// <summary>
    /// 使用共享 Fixture 的测试类 - 所有测试共享同一个登录状态
    /// 这样可以避免每个测试都重新登录，提高测试效率
    /// </summary>
    [Collection("Uheyue API Collection")]
    public class UheyueApiTestsWithFixture
    {
        private readonly UheyueApiFixture _fixture;
        private readonly ITestOutputHelper _output;

        public UheyueApiTestsWithFixture(UheyueApiFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact(DisplayName = "共享Fixture测试01 - 验证Token已获取")]
        [Trait("Category", "API")]
        [Trait("Category", "Login")]
        [Trait("Fast", "true")]
        [Trait("Priority", "High")]
        public void Test01_VerifyTokenExists()
        {
            // Assert
            Assert.NotNull(_fixture.CToken);
            Assert.NotEmpty(_fixture.CToken);
            
            _output.WriteLine($"✓ Token 已获取: {_fixture.CToken}");
        }

        [Fact(DisplayName = "共享Fixture测试02 - 快速创建BTC订单")]
        [Trait("Category", "API")]
        [Trait("Category", "Trade")]
        [Trait("Category", "BuyOrder")]
        [Trait("Fast", "true")]
        [Trait("Priority", "High")]
        public async Task Test02_QuickCreateBtcOrder()
        {
            // Arrange
            var btcApi = new ShijiaBuy(_fixture.ApiContext!, _fixture.Logger);
            btcApi.SetCToken(_fixture.CToken!);

            // Act
            var response = await btcApi.CreateBtcOrderAsync();

            // Assert
            ApiAssertions.AssertSuccess(response);
            var orderId = await btcApi.GetOrderIdFromResponseAsync(response);
            
            _output.WriteLine($"✓ 订单创建成功: {orderId}");
        }

        [Fact(DisplayName = "共享Fixture测试03 - 快速执行平仓")]
        [Trait("Category", "API")]
        [Trait("Category", "Trade")]
        [Trait("Category", "ClosePosition")]
        [Trait("Fast", "true")]
        [Trait("Priority", "High")]
        public async Task Test03_QuickClosePosition()
        {
            // Arrange
            var pingcangApi = new Pingcang(_fixture.ApiContext!, _fixture.Logger);
            pingcangApi.SetCToken(_fixture.CToken!);

            // Act
            var response = await pingcangApi.CreateBtcOrderAsync();

            // Assert
            ApiAssertions.AssertSuccess(response);
            var orderId = await pingcangApi.GetOrderIdFromResponseAsync(response);
            
            _output.WriteLine($"✓ 平仓成功: {orderId}");
        }

        [Theory(DisplayName = "共享Fixture测试04 - 批量创建订单")]
        [Trait("Category", "API")]
        [Trait("Category", "Trade")]
        [Trait("Category", "BuyOrder")]
        [Trait("Fast", "true")]
        [Trait("Priority", "Medium")]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task Test04_CreateMultipleOrders(int orderNumber)
        {
            // Arrange
            var btcApi = new ShijiaBuy(_fixture.ApiContext!, _fixture.Logger);
            btcApi.SetCToken(_fixture.CToken!);

            // Act
            var response = await btcApi.CreateBtcOrderAsync();

            // Assert
            ApiAssertions.AssertSuccess(response);
            var orderId = await btcApi.GetOrderIdFromResponseAsync(response);
            
            _output.WriteLine($"✓ 订单 #{orderNumber} 创建成功: {orderId}");
        }
    }
}
