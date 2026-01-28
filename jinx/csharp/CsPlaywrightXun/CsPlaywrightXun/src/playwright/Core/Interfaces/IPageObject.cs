namespace CsPlaywrightXun.src.playwright.Core.Interfaces;

/// <summary>
/// 页面对象基础接口
/// </summary>
public interface IPageObject
{
    /// <summary>
    /// 导航到指定URL
    /// </summary>
    /// <param name="url">目标URL</param>
    Task NavigateAsync(string url);
    
    /// <summary>
    /// 检查页面是否已加载
    /// </summary>
    /// <returns>页面加载状态</returns>
    Task<bool> IsLoadedAsync();
    
    /// <summary>
    /// 等待页面加载完成
    /// </summary>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    Task WaitForLoadAsync(int timeoutMs = 30000);
}