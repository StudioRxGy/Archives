using Xunit;

namespace EnterpriseAutomationFramework.Core.Attributes;

/// <summary>
/// UI 测试属性组合
/// 标记为 UI 测试类型
/// </summary>
[Trait("Type", "UI")]
public class UITestAttribute : Attribute
{
}

/// <summary>
/// API 测试属性组合
/// 标记为 API 测试类型
/// </summary>
[Trait("Type", "API")]
public class APITestAttribute : Attribute
{
}

/// <summary>
/// 集成测试属性组合
/// 标记为集成测试类型
/// </summary>
[Trait("Type", "Integration")]
public class IntegrationTestAttribute : Attribute
{
}

/// <summary>
/// 单元测试属性组合
/// 标记为单元测试类型
/// </summary>
[Trait("Type", "Unit")]
public class UnitTestAttribute : Attribute
{
}

/// <summary>
/// 端到端测试属性组合
/// 标记为端到端测试类型
/// </summary>
[Trait("Type", "E2E")]
public class E2ETestAttribute : Attribute
{
}

/// <summary>
/// 性能测试属性组合
/// 标记为性能测试类型
/// </summary>
[Trait("Type", "Performance")]
public class PerformanceTestAttribute : Attribute
{
}

/// <summary>
/// 快速测试属性组合
/// 标记为快速执行的测试
/// </summary>
[Trait("Speed", "Fast")]
public class FastTestAttribute : Attribute
{
}

/// <summary>
/// 慢速测试属性组合
/// 标记为慢速执行的测试
/// </summary>
[Trait("Speed", "Slow")]
public class SlowTestAttribute : Attribute
{
}

/// <summary>
/// 冒烟测试属性组合
/// 标记为冒烟测试
/// </summary>
[Trait("Suite", "Smoke")]
public class SmokeTestAttribute : Attribute
{
}

/// <summary>
/// 回归测试属性组合
/// 标记为回归测试
/// </summary>
[Trait("Suite", "Regression")]
public class RegressionTestAttribute : Attribute
{
}