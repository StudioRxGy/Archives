using Microsoft.Playwright;
using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Core.Interfaces;

/// <summary>
/// 浏览器服务接口
/// </summary>
public interface IBrowserService : IAsyncDisposable
{
    /// <summary>
    /// 创建页面实例
    /// </summary>
    /// <param name="settings">浏览器设置</param>
    /// <returns>页面实例</returns>
    Task<IPage> CreatePageAsync(BrowserSettings settings);
    
    /// <summary>
    /// 创建浏览器上下文
    /// </summary>
    /// <param name="settings">浏览器设置</param>
    /// <returns>浏览器上下文</returns>
    Task<IBrowserContext> CreateContextAsync(BrowserSettings settings);
    
    /// <summary>
    /// 截取屏幕截图
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="fileName">文件名</param>
    /// <returns>截图字节数组</returns>
    Task<byte[]> TakeScreenshotAsync(IPage page, string fileName);
    
    /// <summary>
    /// 截取屏幕截图并保存到文件
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="filePath">文件路径</param>
    Task TakeScreenshotToFileAsync(IPage page, string filePath);
    
    /// <summary>
    /// 获取浏览器实例
    /// </summary>
    /// <param name="browserType">浏览器类型</param>
    /// <returns>浏览器实例</returns>
    Task<IBrowser> GetBrowserAsync(string browserType);
    
    /// <summary>
    /// 关闭浏览器服务
    /// </summary>
    Task CloseAsync();
    
    /// <summary>
    /// Playwright 实例
    /// </summary>
    IPlaywright? Playwright { get; }
    
    /// <summary>
    /// 当前浏览器实例
    /// </summary>
    IBrowser? Browser { get; }
}