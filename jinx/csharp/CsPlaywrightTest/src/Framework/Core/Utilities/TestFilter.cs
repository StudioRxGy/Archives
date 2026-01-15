using System.Text;
using EnterpriseAutomationFramework.Core.Attributes;

namespace EnterpriseAutomationFramework.Core.Utilities;

/// <summary>
/// 测试过滤器工具类
/// 用于生成 xUnit 测试过滤表达式
/// </summary>
public static class TestFilter
{
    /// <summary>
    /// 创建类型过滤器
    /// </summary>
    /// <param name="testType">测试类型</param>
    /// <returns>过滤器表达式</returns>
    public static string ByType(TestType testType)
    {
        return $"Type={testType}";
    }

    /// <summary>
    /// 创建多类型过滤器
    /// </summary>
    /// <param name="testTypes">测试类型数组</param>
    /// <returns>过滤器表达式</returns>
    public static string ByTypes(params TestType[] testTypes)
    {
        if (testTypes == null || testTypes.Length == 0)
            return string.Empty;

        if (testTypes.Length == 1)
            return ByType(testTypes[0]);

        var conditions = testTypes.Select(type => $"Type={type}");
        return $"({string.Join("|", conditions)})";
    }

    /// <summary>
    /// 创建分类过滤器
    /// </summary>
    /// <param name="category">测试分类</param>
    /// <returns>过滤器表达式</returns>
    public static string ByCategory(TestCategory category)
    {
        return $"Category={category}";
    }

    /// <summary>
    /// 创建多分类过滤器
    /// </summary>
    /// <param name="categories">测试分类数组</param>
    /// <returns>过滤器表达式</returns>
    public static string ByCategories(params TestCategory[] categories)
    {
        if (categories == null || categories.Length == 0)
            return string.Empty;

        if (categories.Length == 1)
            return ByCategory(categories[0]);

        var conditions = categories.Select(category => $"Category={category}");
        return $"({string.Join("|", conditions)})";
    }

    /// <summary>
    /// 创建优先级过滤器
    /// </summary>
    /// <param name="priority">测试优先级</param>
    /// <returns>过滤器表达式</returns>
    public static string ByPriority(TestPriority priority)
    {
        return $"Priority={priority}";
    }

    /// <summary>
    /// 创建环境过滤器
    /// </summary>
    /// <param name="environment">测试环境</param>
    /// <returns>过滤器表达式</returns>
    public static string ByEnvironment(string environment)
    {
        return $"Environment={environment}";
    }

    /// <summary>
    /// 创建标签过滤器
    /// </summary>
    /// <param name="tag">标签</param>
    /// <returns>过滤器表达式</returns>
    public static string ByTag(string tag)
    {
        return $"Tag={tag}";
    }

    /// <summary>
    /// 创建速度过滤器
    /// </summary>
    /// <param name="speed">测试速度（Fast/Slow）</param>
    /// <returns>过滤器表达式</returns>
    public static string BySpeed(string speed)
    {
        return $"Speed={speed}";
    }

    /// <summary>
    /// 创建测试套件过滤器
    /// </summary>
    /// <param name="suite">测试套件（Smoke/Regression）</param>
    /// <returns>过滤器表达式</returns>
    public static string BySuite(string suite)
    {
        return $"Suite={suite}";
    }

    /// <summary>
    /// 组合多个过滤条件（AND 逻辑）
    /// </summary>
    /// <param name="filters">过滤条件数组</param>
    /// <returns>组合过滤器表达式</returns>
    public static string And(params string[] filters)
    {
        var validFilters = filters?.Where(f => !string.IsNullOrWhiteSpace(f)).ToArray();
        if (validFilters == null || validFilters.Length == 0)
            return string.Empty;

        if (validFilters.Length == 1)
            return validFilters[0];

        return string.Join("&", validFilters);
    }

    /// <summary>
    /// 组合多个过滤条件（OR 逻辑）
    /// </summary>
    /// <param name="filters">过滤条件数组</param>
    /// <returns>组合过滤器表达式</returns>
    public static string Or(params string[] filters)
    {
        var validFilters = filters?.Where(f => !string.IsNullOrWhiteSpace(f)).ToArray();
        if (validFilters == null || validFilters.Length == 0)
            return string.Empty;

        if (validFilters.Length == 1)
            return validFilters[0];

        return $"({string.Join("|", validFilters)})";
    }

    /// <summary>
    /// 排除特定条件（NOT 逻辑）
    /// </summary>
    /// <param name="filter">要排除的过滤条件</param>
    /// <returns>排除过滤器表达式</returns>
    public static string Not(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return string.Empty;

        return $"!{filter}";
    }

    /// <summary>
    /// 预定义过滤器：仅 UI 测试
    /// </summary>
    public static string UITestsOnly => ByType(TestType.UI);

    /// <summary>
    /// 预定义过滤器：仅 API 测试
    /// </summary>
    public static string APITestsOnly => ByType(TestType.API);

    /// <summary>
    /// 预定义过滤器：仅集成测试
    /// </summary>
    public static string IntegrationTestsOnly => ByType(TestType.Integration);

    /// <summary>
    /// 预定义过滤器：仅单元测试
    /// </summary>
    public static string UnitTestsOnly => ByType(TestType.Unit);

    /// <summary>
    /// 预定义过滤器：UI 和 API 测试
    /// </summary>
    public static string UIAndAPITests => ByTypes(TestType.UI, TestType.API);

    /// <summary>
    /// 预定义过滤器：快速测试
    /// </summary>
    public static string FastTestsOnly => BySpeed("Fast");

    /// <summary>
    /// 预定义过滤器：慢速测试
    /// </summary>
    public static string SlowTestsOnly => BySpeed("Slow");

    /// <summary>
    /// 预定义过滤器：冒烟测试
    /// </summary>
    public static string SmokeTestsOnly => BySuite("Smoke");

    /// <summary>
    /// 预定义过滤器：回归测试
    /// </summary>
    public static string RegressionTestsOnly => BySuite("Regression");

    /// <summary>
    /// 预定义过滤器：关键优先级测试
    /// </summary>
    public static string CriticalTestsOnly => ByPriority(TestPriority.Critical);

    /// <summary>
    /// 预定义过滤器：高优先级测试
    /// </summary>
    public static string HighPriorityTestsOnly => ByPriority(TestPriority.High);

    /// <summary>
    /// 生成测试执行命令
    /// </summary>
    /// <param name="filter">过滤器表达式</param>
    /// <param name="projectPath">项目路径（可选）</param>
    /// <returns>dotnet test 命令</returns>
    public static string GenerateTestCommand(string filter, string? projectPath = null)
    {
        var command = new StringBuilder("dotnet test");

        if (!string.IsNullOrWhiteSpace(projectPath))
        {
            command.Append($" \"{projectPath}\"");
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            command.Append($" --filter \"{filter}\"");
        }

        return command.ToString();
    }

    /// <summary>
    /// 生成测试执行命令（带详细输出）
    /// </summary>
    /// <param name="filter">过滤器表达式</param>
    /// <param name="projectPath">项目路径（可选）</param>
    /// <returns>dotnet test 命令</returns>
    public static string GenerateVerboseTestCommand(string filter, string? projectPath = null)
    {
        var baseCommand = GenerateTestCommand(filter, projectPath);
        return $"{baseCommand} --verbosity normal --logger console";
    }
}