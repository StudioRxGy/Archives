namespace CsPlaywrightXun.src.playwright.Core.Exceptions;

/// <summary>
/// 元素未找到异常
/// </summary>
public class ElementNotFoundException : TestFrameworkException
{
    /// <summary>
    /// 元素选择器
    /// </summary>
    public string Selector { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="selector">元素选择器</param>
    /// <param name="message">错误消息</param>
    public ElementNotFoundException(string testName, string selector, string message)
        : base(testName, "PageObject", message)
    {
        Selector = selector;
    }
    
    /// <summary>
    /// 构造函数 - 仅选择器
    /// </summary>
    /// <param name="selector">元素选择器</param>
    public ElementNotFoundException(string selector)
        : base($"元素未找到: {selector}")
    {
        Selector = selector;
    }
    
    /// <summary>
    /// 构造函数 - 选择器和消息
    /// </summary>
    /// <param name="selector">元素选择器</param>
    /// <param name="message">错误消息</param>
    public ElementNotFoundException(string selector, string message)
        : base(message)
    {
        Selector = selector;
    }
    
    /// <summary>
    /// 构造函数 - 选择器、消息和内部异常
    /// </summary>
    /// <param name="selector">元素选择器</param>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public ElementNotFoundException(string selector, string message, Exception innerException)
        : base(message, innerException)
    {
        Selector = selector;
    }
}