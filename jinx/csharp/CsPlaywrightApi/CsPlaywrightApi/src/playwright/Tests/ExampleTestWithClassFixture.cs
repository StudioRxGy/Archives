using Xunit;
using Xunit.Abstractions;

namespace CsPlaywrightApi.src.playwright.Tests
{
    /// <summary>
    /// 使用 IClassFixture 的示例测试类
    /// 
    /// 优点：
    /// 1. Logger 在整个测试类中只创建一次（InitializeAsync）
    /// 2. 所有测试方法共享同一个 Logger 实例
    /// 3. 测试类结束后自动清理资源（DisposeAsync）
    /// 4. 避免了每个测试都创建新的 Logger，提高性能
    /// </summary>
    public class ExampleTestWithClassFixture : IClassFixture<UheyueApiTestFixture>
    {
        private readonly UheyueApiTestFixture _fixture;
        private readonly ITestOutputHelper _output;

        /// <summary>
        /// 构造函数 - xUnit 会自动注入 Fixture 实例
        /// </summary>
        public ExampleTestWithClassFixture(UheyueApiTestFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact(DisplayName = "测试01 - 验证 Logger 已初始化")]
        [Trait("Category", "Example")]
        public void Test01_VerifyLoggerInitialized()
        {
            // 使用共享的 Logger
            Assert.NotNull(_fixture.Logger);
            Assert.NotNull(_fixture.ApiContext);
            Assert.NotNull(_fixture.Playwright);
            
            _output.WriteLine("✓ Logger 和其他资源已成功初始化");
        }

        [Fact(DisplayName = "测试02 - 验证配置已加载")]
        [Trait("Category", "Example")]
        public void Test02_VerifySettingsLoaded()
        {
            // 使用共享的配置
            Assert.NotNull(_fixture.Settings);
            Assert.NotEmpty(_fixture.Settings.Config.BaseUrl);
            
            _output.WriteLine($"✓ 当前环境: {_fixture.Settings.CurrentEnvironment}");
            _output.WriteLine($"✓ Base URL: {_fixture.Settings.Config.BaseUrl}");
        }

        [Theory(DisplayName = "测试03 - 多次使用共享 Logger")]
        [Trait("Category", "Example")]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Test03_UseSharedLoggerMultipleTimes(int testNumber)
        {
            // 所有这些测试都使用同一个 Logger 实例
            Assert.NotNull(_fixture.Logger);
            
            _output.WriteLine($"✓ 测试 #{testNumber} 使用共享 Logger");
        }
    }
}
