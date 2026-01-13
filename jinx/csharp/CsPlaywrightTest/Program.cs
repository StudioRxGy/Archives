using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using CsPlaywrightXun.src.playwright;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Flows.baidu;

namespace CsPlaywrightXun;

class Program
{
    static async Task Main(string[] args)
    {
        // 初始化框架
        Framework.Initialize();
        var logger = Framework.GetLogger("Program");
        
        logger.LogInformation("开始运行 C# Playwright 自动化框架演示");

        try
        {
            // 创建并初始化测试固件
            var fixture = new SimpleTestFixture();
            await fixture.InitializeAsync();

            // 创建搜索流程
            var searchFlow = new SearchFlow(fixture, logger);

            logger.LogInformation("执行百度搜索流程演示...");

            // 执行简单搜索
            await searchFlow.ExecuteSimpleSearchAsync("C# Playwright 自动化测试");

            logger.LogInformation("搜索流程执行完成！");

            // 等待用户观察结果
            Console.WriteLine("按任意键继续...");
            Console.ReadKey();

            // 清理资源
            await fixture.DisposeAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "程序执行过程中发生错误");
        }
        finally
        {
            Framework.CloseAndFlushLogs();
        }
    }
}

/// <summary>
/// 简单的测试固件实现
/// </summary>
public class SimpleTestFixture : ITestFixture
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;
    public IBrowserContext Context { get; private set; } = null!;
    public IPage Page { get; private set; } = null!;
    public TestConfiguration Configuration { get; private set; }

    public SimpleTestFixture()
    {
        Configuration = Framework.GetConfiguration();
        
        // 设置默认的百度URL
        if (string.IsNullOrEmpty(Configuration.Environment.BaseUrl))
        {
            Configuration.Environment.BaseUrl = "https://www.baidu.com";
        }
    }

    public async Task InitializeAsync()
    {
        // 创建 Playwright 实例
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        
        // 启动浏览器
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false, // 显示浏览器窗口
            SlowMo = 1000     // 每个操作间隔1秒，便于观察
        });

        // 创建浏览器上下文
        Context = await Browser.NewContextAsync();
        
        // 创建页面
        Page = await Context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Page != null) await Page.CloseAsync();
        if (Context != null) await Context.CloseAsync();
        if (Browser != null) await Browser.CloseAsync();
        Playwright?.Dispose();
    }
}