using Microsoft.Playwright;
using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Core.Interfaces;

/// <summary>
/// 测试固件接口，管理 Playwright 生命周期
/// </summary>
public interface ITestFixture : IAsyncDisposable
{
    /// <summary>
    /// 初始化测试固件
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Playwright 实例
    /// </summary>
    IPlaywright Playwright { get; }
    
    /// <summary>
    /// 浏览器实例
    /// </summary>
    IBrowser Browser { get; }
    
    /// <summary>
    /// 浏览器上下文
    /// </summary>
    IBrowserContext Context { get; }
    
    /// <summary>
    /// 页面实例
    /// </summary>
    IPage Page { get; }
    
    /// <summary>
    /// 测试配置
    /// </summary>
    TestConfiguration Configuration { get; }
}