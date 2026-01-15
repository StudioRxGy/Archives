namespace EnterpriseAutomationFramework.Core.Attributes;

/// <summary>
/// 测试类型枚举
/// </summary>
public enum TestType
{
    /// <summary>
    /// 单元测试
    /// </summary>
    Unit,

    /// <summary>
    /// UI 测试
    /// </summary>
    UI,

    /// <summary>
    /// API 测试
    /// </summary>
    API,

    /// <summary>
    /// 集成测试
    /// </summary>
    Integration,

    /// <summary>
    /// 端到端测试
    /// </summary>
    E2E,

    /// <summary>
    /// 性能测试
    /// </summary>
    Performance,

    /// <summary>
    /// 安全测试
    /// </summary>
    Security
}

/// <summary>
/// 测试分类枚举
/// </summary>
public enum TestCategory
{
    /// <summary>
    /// 核心功能测试
    /// </summary>
    Core,

    /// <summary>
    /// 页面对象测试
    /// </summary>
    PageObject,

    /// <summary>
    /// 业务流程测试
    /// </summary>
    Flow,

    /// <summary>
    /// 服务层测试
    /// </summary>
    Service,

    /// <summary>
    /// 配置管理测试
    /// </summary>
    Configuration,

    /// <summary>
    /// 数据管理测试
    /// </summary>
    Data,

    /// <summary>
    /// 浏览器服务测试
    /// </summary>
    Browser,

    /// <summary>
    /// API 客户端测试
    /// </summary>
    ApiClient,

    /// <summary>
    /// 错误恢复测试
    /// </summary>
    ErrorRecovery,

    /// <summary>
    /// 重试机制测试
    /// </summary>
    Retry,

    /// <summary>
    /// 日志记录测试
    /// </summary>
    Logging,

    /// <summary>
    /// 报告生成测试
    /// </summary>
    Reporting,

    /// <summary>
    /// 测试固件测试
    /// </summary>
    Fixture,

    /// <summary>
    /// 数据驱动测试
    /// </summary>
    DataDriven,

    /// <summary>
    /// 搜索功能测试
    /// </summary>
    Search,

    /// <summary>
    /// 用户界面测试
    /// </summary>
    UserInterface,

    /// <summary>
    /// 业务逻辑测试
    /// </summary>
    BusinessLogic
}

/// <summary>
/// 测试优先级枚举
/// </summary>
public enum TestPriority
{
    /// <summary>
    /// 低优先级
    /// </summary>
    Low,

    /// <summary>
    /// 中等优先级
    /// </summary>
    Medium,

    /// <summary>
    /// 高优先级
    /// </summary>
    High,

    /// <summary>
    /// 关键优先级
    /// </summary>
    Critical
}