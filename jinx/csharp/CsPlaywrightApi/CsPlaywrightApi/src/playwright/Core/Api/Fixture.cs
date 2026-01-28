using Microsoft.Playwright;
using CsPlaywrightApi.src.playwright.Core.Logging;
using CsPlaywrightApi.src.playwright.Core.Config;

namespace CsPlaywrightApi.src.playwright.Tests
{
    /// <summary>
    /// Uheyue API 测试的共享 Fixture
    /// 用于在整个测试类中共享 Logger 和其他资源
    /// </summary>
    public class Fixture : IAsyncLifetime
    {
        public IPlaywright? Playwright { get; private set; }
        public IAPIRequestContext? ApiContext { get; private set; }
        public ApiLogger? Logger { get; private set; }
        public AppSettings Settings { get; private set; }

        public Fixture()
        {
            Settings = AppSettings.Instance;
        }

        /// <summary>
        /// 在所有测试开始前初始化（只执行一次）
        /// </summary>
        public async Task InitializeAsync()
        {
            // 打印配置信息
            Settings.PrintConfigInfo();
            
            // 创建共享的 Logger（整个测试类只创建一次）
            Logger = new ApiLogger(enableConsoleLog: Settings.EnableConsoleLog);
            Console.WriteLine($"\n✓ 共享日志记录器已创建");
            
            // 创建 Playwright 和 API Context
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            ApiContext = await Playwright.APIRequest.NewContextAsync(new()
            {
                BaseURL = Settings.Config.BaseUrl,
                Timeout = Settings.Config.Timeout * 1000
            });

            Console.WriteLine($"✓ 测试环境初始化完成 - 环境: {Settings.CurrentEnvironment}");
            Console.WriteLine($"✓ Base URL: {Settings.Config.BaseUrl}");
            Console.WriteLine($"✓ 日志目录: {Settings.LogDirectory}\n");
        }

        /// <summary>
        /// 在所有测试结束后清理（只执行一次）
        /// </summary>
        public async Task DisposeAsync()
        {
            // 生成 HTML 报告（所有测试完成后只生成一次）
            if (Logger != null)
            {
                await Logger.GenerateHtmlReportAsync();
                Console.WriteLine("\n✓ 所有测试完成，HTML 报告已生成");
            }

            // 清理资源
            if (ApiContext != null)
            {
                await ApiContext.DisposeAsync();
            }
            Playwright?.Dispose();
        }
    }
}
