using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Exceptions;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Core.Interfaces;

namespace CsPlaywrightXun.src.playwright.Services.Browser;

/// <summary>
/// 浏览器服务实现
/// </summary>
public class BrowserService : IBrowserService
{
    private readonly ILogger<BrowserService> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly Dictionary<string, IBrowser> _browsers = new();
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public BrowserService(ILogger<BrowserService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Playwright 实例
    /// </summary>
    public IPlaywright? Playwright => _playwright;

    /// <summary>
    /// 当前浏览器实例
    /// </summary>
    public IBrowser? Browser => _browser;

    /// <summary>
    /// 初始化 Playwright
    /// </summary>
    private async Task InitializePlaywrightAsync()
    {
        if (_playwright == null)
        {
            _logger.LogInformation("正在初始化 Playwright...");
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _logger.LogInformation("Playwright 初始化完成");
        }
    }

    /// <summary>
    /// 获取浏览器实例
    /// </summary>
    /// <param name="browserType">浏览器类型</param>
    /// <returns>浏览器实例</returns>
    public async Task<IBrowser> GetBrowserAsync(string browserType)
    {
        if (string.IsNullOrWhiteSpace(browserType))
        {
            throw new ArgumentException("浏览器类型不能为空", nameof(browserType));
        }

        await InitializePlaywrightAsync();

        if (_browsers.TryGetValue(browserType, out var existingBrowser) && existingBrowser.IsConnected)
        {
            _logger.LogDebug($"返回现有的 {browserType} 浏览器实例");
            return existingBrowser;
        }

        _logger.LogInformation($"正在启动 {browserType} 浏览器...");

        IBrowser browser = browserType.ToLowerInvariant() switch
        {
            "chromium" => await _playwright!.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false
            }),
            "firefox" => await _playwright!.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false
            }),
            "webkit" => await _playwright!.Webkit.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false
            }),
            _ => throw new TestFrameworkException("BrowserService", "BrowserService", 
                $"不支持的浏览器类型: {browserType}")
        };

        _browsers[browserType] = browser;
        _browser = browser;

        _logger.LogInformation($"{browserType} 浏览器启动成功");
        return browser;
    }

    /// <summary>
    /// 创建浏览器上下文
    /// </summary>
    /// <param name="settings">浏览器设置</param>
    /// <returns>浏览器上下文</returns>
    public async Task<IBrowserContext> CreateContextAsync(BrowserSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var browser = await GetBrowserAsync(settings.Type);

        _logger.LogInformation($"正在创建浏览器上下文，视口大小: {settings.ViewportWidth}x{settings.ViewportHeight}");

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Width = settings.ViewportWidth,
                Height = settings.ViewportHeight
            },
            IgnoreHTTPSErrors = true,
            AcceptDownloads = true
        });

        // 设置默认超时时间
        context.SetDefaultTimeout(settings.Timeout);
        context.SetDefaultNavigationTimeout(settings.Timeout);

        _logger.LogInformation("浏览器上下文创建成功");
        return context;
    }

    /// <summary>
    /// 创建页面实例
    /// </summary>
    /// <param name="settings">浏览器设置</param>
    /// <returns>页面实例</returns>
    public async Task<IPage> CreatePageAsync(BrowserSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var context = await CreateContextAsync(settings);

        _logger.LogInformation("正在创建新页面...");
        var page = await context.NewPageAsync();

        _logger.LogInformation("页面创建成功");
        return page;
    }

    /// <summary>
    /// 截取屏幕截图
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="fileName">文件名</param>
    /// <returns>截图字节数组</returns>
    public async Task<byte[]> TakeScreenshotAsync(IPage page, string fileName)
    {
        if (page == null)
        {
            throw new ArgumentNullException(nameof(page));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        }

        try
        {
            _logger.LogInformation($"正在截取屏幕截图: {fileName}");

            var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                FullPage = true,
                Type = ScreenshotType.Png
            });

            _logger.LogInformation($"屏幕截图完成: {fileName}，大小: {screenshot.Length} 字节");
            return screenshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"截取屏幕截图失败: {fileName}");
            throw new TestFrameworkException("BrowserService", "BrowserService", 
                $"截取屏幕截图失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 截取屏幕截图并保存到文件
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="filePath">文件路径</param>
    public async Task TakeScreenshotToFileAsync(IPage page, string filePath)
    {
        if (page == null)
        {
            throw new ArgumentNullException(nameof(page));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        }

        try
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                PathConfiguration.EnsureDirectoryExists(directory);
                _logger.LogDebug($"确保截图目录存在: {directory}");
            }

            _logger.LogInformation($"正在保存屏幕截图到文件: {filePath}");

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = filePath,
                FullPage = true,
                Type = ScreenshotType.Png
            });

            _logger.LogInformation($"屏幕截图已保存: {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"保存屏幕截图失败: {filePath}");
            throw new TestFrameworkException("BrowserService", "BrowserService", 
                $"保存屏幕截图失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 截取屏幕截图并保存到文件（使用PathConfiguration）
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="testName">测试名称</param>
    /// <param name="browserType">浏览器类型</param>
    public async Task TakeScreenshotToFileAsync(IPage page, string testName, string browserType = "chromium")
    {
        if (page == null)
        {
            throw new ArgumentNullException(nameof(page));
        }

        if (string.IsNullOrWhiteSpace(testName))
        {
            throw new ArgumentException("测试名称不能为空", nameof(testName));
        }

        try
        {
            // 使用PathConfiguration获取截图文件路径
            var filePath = PathConfiguration.GetScreenshotPath(testName, browserType);
            
            // 调用原有的方法
            await TakeScreenshotToFileAsync(page, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"保存屏幕截图失败: {testName}");
            throw new TestFrameworkException("BrowserService", "BrowserService", 
                $"保存屏幕截图失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 关闭浏览器服务
    /// </summary>
    public async Task CloseAsync()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("正在关闭浏览器服务...");

        try
        {
            // 关闭所有浏览器实例
            foreach (var browser in _browsers.Values)
            {
                if (browser.IsConnected)
                {
                    await browser.CloseAsync();
                }
            }
            _browsers.Clear();

            // 释放 Playwright 资源
            _playwright?.Dispose();
            _playwright = null;
            _browser = null;

            _logger.LogInformation("浏览器服务已关闭");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关闭浏览器服务时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await CloseAsync();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}