namespace EnterpriseAutomationFramework.Services.Data;

/// <summary>
/// 页面元素
/// </summary>
public class PageElement
{
    /// <summary>
    /// 元素名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 元素选择器
    /// </summary>
    public string Selector { get; set; } = string.Empty;
    
    /// <summary>
    /// 元素类型
    /// </summary>
    public ElementType Type { get; set; }
    
    /// <summary>
    /// 超时时间（毫秒）
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// 元素属性
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = new();
}

/// <summary>
/// 元素类型枚举
/// </summary>
public enum ElementType
{
    /// <summary>
    /// 按钮
    /// </summary>
    Button,
    
    /// <summary>
    /// 输入框
    /// </summary>
    Input,
    
    /// <summary>
    /// 链接
    /// </summary>
    Link,
    
    /// <summary>
    /// 文本
    /// </summary>
    Text,
    
    /// <summary>
    /// 下拉框
    /// </summary>
    Dropdown,
    
    /// <summary>
    /// 复选框
    /// </summary>
    Checkbox,
    
    /// <summary>
    /// 单选框
    /// </summary>
    Radio
}