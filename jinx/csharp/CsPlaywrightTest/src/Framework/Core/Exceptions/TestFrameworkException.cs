namespace EnterpriseAutomationFramework.Core.Exceptions;

/// <summary>
/// 测试框架异常基类
/// </summary>
public class TestFrameworkException : Exception
{
    /// <summary>
    /// 测试名称
    /// </summary>
    public string TestName { get; }
    
    /// <summary>
    /// 组件名称
    /// </summary>
    public string Component { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="component">组件名称</param>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public TestFrameworkException(string testName, string component, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        TestName = testName;
        Component = component;
    }
    
    /// <summary>
    /// 构造函数 - 仅消息
    /// </summary>
    /// <param name="message">错误消息</param>
    public TestFrameworkException(string message) : base(message)
    {
        TestName = string.Empty;
        Component = string.Empty;
    }
    
    /// <summary>
    /// 构造函数 - 消息和内部异常
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public TestFrameworkException(string message, Exception innerException) : base(message, innerException)
    {
        TestName = string.Empty;
        Component = string.Empty;
    }
}