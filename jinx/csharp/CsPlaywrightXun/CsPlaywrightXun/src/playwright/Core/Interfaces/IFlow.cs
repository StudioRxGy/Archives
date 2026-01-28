namespace CsPlaywrightXun.src.playwright.Core.Interfaces;

/// <summary>
/// 业务流程接口
/// </summary>
public interface IFlow
{
    /// <summary>
    /// 执行业务流程
    /// </summary>
    /// <param name="parameters">流程参数</param>
    Task ExecuteAsync(Dictionary<string, object>? parameters = null);
}