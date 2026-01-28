namespace EnterpriseAutomationFramework.Core.Exceptions;

/// <summary>
/// YAML 数据异常
/// </summary>
public class YamlDataException : TestFrameworkException
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误消息</param>
    public YamlDataException(string message) 
        : base("YamlElementReader", "DataService", message)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public YamlDataException(string message, Exception innerException) 
        : base("YamlElementReader", "DataService", message, innerException)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="message">错误消息</param>
    public YamlDataException(string filePath, string message) 
        : base("YamlElementReader", "DataService", message)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public YamlDataException(string filePath, string message, Exception innerException) 
        : base("YamlElementReader", "DataService", message, innerException)
    {
        FilePath = filePath;
    }
}