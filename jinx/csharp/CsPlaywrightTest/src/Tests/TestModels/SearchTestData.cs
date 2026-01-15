namespace EnterpriseAutomationFramework.Tests.TestModels;

/// <summary>
/// 搜索测试数据模型
/// </summary>
public class SearchTestData
{
    /// <summary>
    /// 测试名称
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string SearchQuery { get; set; } = string.Empty;

    /// <summary>
    /// 期望结果数量
    /// </summary>
    public int ExpectedResultCount { get; set; }

    /// <summary>
    /// 环境名称
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}