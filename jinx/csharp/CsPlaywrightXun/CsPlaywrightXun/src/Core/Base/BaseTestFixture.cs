using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Core.Interfaces;

namespace CsPlaywrightXun.src.playwright.Core.Base;

/// <summary>
/// 测试固件基类
/// </summary>
public abstract class BaseTestFixture : ITestFixture
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;
    public IBrowserContext Context { get; private set; } = null!;
    public IPage Page { get; private set; } = null!;
    public TestConfiguration Configuration { get; private set; } = null!;

    protected readonly ILogger _logger;

    protected BaseTestFixture(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 初始化测试固件
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        _logger.LogInformation("初始化测试固件");
        
        // 加载配置
        Configuration = LoadConfiguration();
        
        // 初始化 Playwright
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        
        // 创建浏览器
        Browser = await CreateBrowserAsync();
        
        // 创建浏览器上下文
        Context = await CreateBrowserContextAsync();
        
        // 创建页面
        Page = await Context.NewPageAsync();
        
        _logger.LogInformation("测试固件初始化完成");
    }

    /// <summary>
    /// 清理测试固件
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        _logger.LogInformation("清理测试固件");
        
        if (Page != null)
            await Page.CloseAsync();
            
        if (Context != null)
            await Context.CloseAsync();
            
        if (Browser != null)
            await Browser.CloseAsync();
            
        Playwright?.Dispose();
        
        _logger.LogInformation("测试固件清理完成");
    }

    /// <summary>
    /// 加载测试配置
    /// </summary>
    protected internal virtual TestConfiguration LoadConfiguration()
    {
        // 默认配置，子类可以重写以加载自定义配置
        return new TestConfiguration();
    }

    /// <summary>
    /// 创建浏览器实例
    /// </summary>
    protected virtual async Task<IBrowser> CreateBrowserAsync()
    {
        var browserType = Configuration.Browser.Type.ToLower() switch
        {
            "firefox" => Playwright.Firefox,
            "webkit" => Playwright.Webkit,
            _ => Playwright.Chromium
        };

        return await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Configuration.Browser.Headless,
            Timeout = Configuration.Browser.Timeout
        });
    }

    /// <summary>
    /// 创建浏览器上下文
    /// </summary>
    protected virtual async Task<IBrowserContext> CreateBrowserContextAsync()
    {
        return await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Width = Configuration.Browser.ViewportWidth,
                Height = Configuration.Browser.ViewportHeight
            }
        });
    }
}