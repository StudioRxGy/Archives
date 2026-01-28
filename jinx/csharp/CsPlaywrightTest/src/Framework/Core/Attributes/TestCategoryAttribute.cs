using Xunit;

namespace EnterpriseAutomationFramework.Core.Attributes;

/// <summary>
/// 测试分类属性
/// 用于标记测试的类型和分类，支持测试过滤和执行控制
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class TestCategoryAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="category">测试分类</param>
    public TestCategoryAttribute(TestCategory category)
    {
        Category = category;
    }

    /// <summary>
    /// 测试分类
    /// </summary>
    public TestCategory Category { get; }

    /// <summary>
    /// Trait 名称
    /// </summary>
    public string Name => "Category";

    /// <summary>
    /// Trait 值
    /// </summary>
    public string Value => Category.ToString();
}

/// <summary>
/// 测试类型属性
/// 用于标记测试的主要类型
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class TestTypeAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="type">测试类型</param>
    public TestTypeAttribute(TestType type)
    {
        Type = type;
    }

    /// <summary>
    /// 测试类型
    /// </summary>
    public TestType Type { get; }

    /// <summary>
    /// Trait 名称
    /// </summary>
    public string Name => "Type";

    /// <summary>
    /// Trait 值
    /// </summary>
    public string Value => Type.ToString();
}

/// <summary>
/// 测试优先级属性
/// 用于标记测试的执行优先级
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class TestPriorityAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="priority">测试优先级</param>
    public TestPriorityAttribute(TestPriority priority)
    {
        Priority = priority;
    }

    /// <summary>
    /// 测试优先级
    /// </summary>
    public TestPriority Priority { get; }

    /// <summary>
    /// Trait 名称
    /// </summary>
    public string Name => "Priority";

    /// <summary>
    /// Trait 值
    /// </summary>
    public string Value => Priority.ToString();
}

/// <summary>
/// 测试环境属性
/// 用于标记测试适用的环境
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class TestEnvironmentAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="environment">测试环境</param>
    public TestEnvironmentAttribute(string environment)
    {
        Environment = environment;
    }

    /// <summary>
    /// 测试环境
    /// </summary>
    public string Environment { get; }

    /// <summary>
    /// Trait 名称
    /// </summary>
    public string Name => "Environment";

    /// <summary>
    /// Trait 值
    /// </summary>
    public string Value => Environment;
}

/// <summary>
/// 测试标签属性
/// 用于添加自定义标签
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class TestTagAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tag">标签名称</param>
    public TestTagAttribute(string tag)
    {
        Tag = tag;
    }

    /// <summary>
    /// 标签名称
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Trait 名称
    /// </summary>
    public string Name => "Tag";

    /// <summary>
    /// Trait 值
    /// </summary>
    public string Value => Tag;
}