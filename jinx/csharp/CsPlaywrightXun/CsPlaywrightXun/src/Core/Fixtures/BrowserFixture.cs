using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Services.Browser;

namespace CsPlaywrightXun.src.playwright.Core.Fixtures;

/// <summary>
/// 浏览器测试固件，确保每个测试用例使用独立的 BrowserContext
/// 支持并行执行和测试隔离
/// </summary>
public class BrowserFixture : BaseTestFixture
{
    private readonly IBrowserService _browserService;
    private readonly ConfigurationService _configurationService;
    private readonly string _environment;
    private bool _initialized = false;
    private readonly object _initLock = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public BrowserFixture() 
        : base(CreateDefaultLogger())
    {
        _environment = "Development";
        _browserService = new BrowserService(CreateBrowserServiceLogger());
        _configurationService = new ConfigurationService();
    }

    /// <summary>
    /// 初始化测试固件
    /// </summary>
    public override async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        lock (_initLock)
        {
            if (_initialized)
            {
                return;
            }

            _logger.LogInformation($"正在初始化 BrowserFixture，环境: {_environment}");
        }

        try
        {
            // 调用基类的初始化方法
            await base.InitializeAsync();

            lock (_initLock)
            {
                _initialized = true;
            }

            _logger.LogInformation("BrowserFixture 初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BrowserFixture 初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 创建新的浏览器上下文（用于测试隔离）
    /// </summary>
    /// <returns>新的浏览器上下文</returns>
    public async Task<IBrowserContext> CreateNewContextAsync()
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }

        _logger.LogInformation("正在创建新的浏览器上下文以确保测试隔离");

        var newContext = await CreateBrowserContextAsync();
        
        _logger.LogInformation("新的浏览器上下文创建完成");
        return newContext;
    }

    /// <summary>
    /// 创建新的页面实例
    /// </summary>
    /// <param name="context">浏览器上下文，如果为null则使用当前上下文</param>
    /// <returns>新的页面实例</returns>
    public async Task<IPage> CreateNewPageAsync(IBrowserContext? context = null)
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }

        var targetContext = context ?? Context;
        _logger.LogInformation("正在创建新的页面实例");

        var newPage = await targetContext.NewPageAsync();
        
        _logger.LogInformation("新的页面实例创建完成");
        return newPage;
    }

    /// <summary>
    /// 获取测试专用的浏览器上下文和页面
    /// 每次调用都会创建新的上下文和页面，确保测试隔离
    /// </summary>
    /// <returns>包含上下文和页面的元组</returns>
    public async Task<(IBrowserContext Context, IPage Page)> GetIsolatedBrowserAsync()
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }

        _logger.LogInformation("正在创建隔离的浏览器上下文和页面");

        var isolatedContext = await CreateNewContextAsync();
        var isolatedPage = await isolatedContext.NewPageAsync();

        _logger.LogInformation("隔离的浏览器上下文和页面创建完成");
        return (isolatedContext, isolatedPage);
    }

    /// <summary>
    /// 截取当前页面截图
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>截图字节数组</returns>
    public async Task<byte[]> TakeScreenshotAsync(string fileName)
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }

        return await _browserService.TakeScreenshotAsync(Page, fileName);
    }

    /// <summary>
    /// 截取指定页面截图
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="fileName">文件名</param>
    /// <returns>截图字节数组</returns>
    public async Task<byte[]> TakeScreenshotAsync(IPage page, string fileName)
    {
        return await _browserService.TakeScreenshotAsync(page, fileName);
    }

    /// <summary>
    /// 加载测试配置
    /// </summary>
    protected internal override TestConfiguration LoadConfiguration()
    {
        try
        {
            _logger.LogInformation($"正在加载环境配置: {_environment}");
            
            var config = _configurationService.LoadConfiguration(_environment);
            
            // 验证配置
            if (!config.IsValid())
            {
                var errors = config.GetValidationErrors();
                var errorMessage = $"配置验证失败: {string.Join(", ", errors)}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation($"环境配置加载完成: {_environment}");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"加载环境配置失败: {_environment}");
            
            // 返回默认配置以确保测试能够继续运行
            _logger.LogWarning("使用默认配置");
            return CreateDefaultConfiguration();
        }
    }

    /// <summary>
    /// 创建默认配置
    /// </summary>
    /// <returns>默认测试配置</returns>
    private TestConfiguration CreateDefaultConfiguration()
    {
        return new TestConfiguration
        {
            Environment = new EnvironmentSettings
            {
                Name = _environment,
                BaseUrl = "https://www.baidu.com",
                ApiBaseUrl = "https://www.baidu.com/api"
            },
            Browser = new BrowserSettings
            {
                Type = "Chromium",
                Headless = false,
                ViewportWidth = 1920,
                ViewportHeight = 1080,
                Timeout = 30000
            },
            Api = new ApiSettings
            {
                Timeout = 30000,
                RetryCount = 3,
                RetryDelay = 1000
            },
            Reporting = new ReportingSettings
            {
                OutputPath = "Reports",
                Format = "Html",
                IncludeScreenshots = true
            },
            Logging = new LoggingSettings
            {
                Level = "Information",
                FilePath = "Logs/test-{Date}.log"
            }
        };
    }

    /// <summary>
    /// 创建默认日志记录器
    /// </summary>
    /// <returns>默认日志记录器</returns>
    private static ILogger CreateDefaultLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        return loggerFactory.CreateLogger<BrowserFixture>();
    }

    /// <summary>
    /// 创建 BrowserService 专用的日志记录器
    /// </summary>
    /// <returns>BrowserService 日志记录器</returns>
    private ILogger<BrowserService> CreateBrowserServiceLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        return loggerFactory.CreateLogger<BrowserService>();
    }

    /// <summary>
    /// 清理测试固件
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        if (!_initialized)
        {
            return;
        }

        _logger.LogInformation("正在清理 BrowserFixture");

        try
        {
            // 调用基类的清理方法
            await base.DisposeAsync();

            // 关闭浏览器服务
            await _browserService.DisposeAsync();
            _logger.LogDebug("浏览器服务已关闭");

            lock (_initLock)
            {
                _initialized = false;
            }

            _logger.LogInformation("BrowserFixture 清理完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理 BrowserFixture 时发生错误");
            throw;
        }

        GC.SuppressFinalize(this);
    }
}