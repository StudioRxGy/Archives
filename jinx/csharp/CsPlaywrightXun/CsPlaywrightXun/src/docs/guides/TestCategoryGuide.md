# 测试分类标记使用指南

## 概述

本文档介绍如何在企业自动化测试框架中使用测试分类标记功能。通过使用 xUnit Trait 属性，您可以对测试进行分类、过滤和有选择地执行。

## 测试分类标记

### 基本属性

#### 1. 测试类型标记

使用 `TestTypeAttribute` 或便捷属性标记测试类型：

```csharp
// 使用基础属性
[TestType(TestType.UI)]
public class MyUITests { }

// 使用便捷属性
[UITest]
public class MyUITests { }

[APITest]
public class MyAPITests { }

[IntegrationTest]
public class MyIntegrationTests { }

[UnitTest]
public class MyUnitTests { }

[E2ETest]
public class MyE2ETests { }
```

#### 2. 测试分类标记

使用 `TestCategoryAttribute` 标记测试的功能分类：

```csharp
[TestCategory(TestCategory.PageObject)]
public class HomePageTests { }

[TestCategory(TestCategory.ApiClient)]
public class ApiClientTests { }

[TestCategory(TestCategory.ErrorRecovery)]
public class ErrorRecoveryTests { }
```

#### 3. 测试优先级标记

使用 `TestPriorityAttribute` 标记测试优先级：

```csharp
[TestPriority(TestPriority.Critical)]
public class CriticalTests { }

[TestPriority(TestPriority.High)]
public class HighPriorityTests { }

[TestPriority(TestPriority.Medium)]
public class MediumPriorityTests { }
```

#### 4. 其他标记

```csharp
// 测试速度标记
[FastTest]
public class QuickTests { }

[SlowTest]
public class LongRunningTests { }

// 测试套件标记
[SmokeTest]
public class SmokeTests { }

[RegressionTest]
public class RegressionTests { }

// 环境标记
[TestEnvironment("Development")]
public class DevTests { }

[TestEnvironment("Staging")]
public class StagingTests { }

// 自定义标签
[TestTag("Search")]
public class SearchTests { }

[TestTag("Authentication")]
public class AuthTests { }
```

### 组合使用

可以在同一个测试类上使用多个标记：

```csharp
[UITest]
[TestCategory(TestCategory.PageObject)]
[TestPriority(TestPriority.High)]
[SmokeTest]
[FastTest]
public class HomePageTests
{
    [Fact]
    [TestTag("Navigation")]
    public void NavigateToHomePage_ShouldLoadSuccessfully()
    {
        // 测试实现
    }

    [Fact]
    [TestTag("Search")]
    [TestEnvironment("Production")]
    public void SearchFunctionality_ShouldReturnResults()
    {
        // 测试实现
    }
}
```

## 测试过滤和执行

### 使用 TestFilter 工具类

框架提供了 `TestFilter` 工具类来生成过滤器表达式：

```csharp
// 基本过滤器
var uiFilter = TestFilter.ByType(TestType.UI);
var apiFilter = TestFilter.ByType(TestType.API);
var categoryFilter = TestFilter.ByCategory(TestCategory.PageObject);

// 多条件过滤器
var multiTypeFilter = TestFilter.ByTypes(TestType.UI, TestType.API);
var multiCategoryFilter = TestFilter.ByCategories(TestCategory.PageObject, TestCategory.Flow);

// 组合过滤器
var combinedFilter = TestFilter.And(
    TestFilter.ByType(TestType.UI),
    TestFilter.ByPriority(TestPriority.High)
);

var orFilter = TestFilter.Or(
    TestFilter.ByType(TestType.UI),
    TestFilter.ByType(TestType.API)
);

// 排除过滤器
var excludeSlowTests = TestFilter.Not(TestFilter.BySpeed("Slow"));
```

### 预定义过滤器

框架提供了常用的预定义过滤器：

```csharp
// 按类型过滤
TestFilter.UITestsOnly          // "Type=UI"
TestFilter.APITestsOnly         // "Type=API"
TestFilter.IntegrationTestsOnly // "Type=Integration"
TestFilter.UnitTestsOnly        // "Type=Unit"

// 组合类型过滤
TestFilter.UIAndAPITests        // "(Type=UI|Type=API)"

// 按速度过滤
TestFilter.FastTestsOnly        // "Speed=Fast"
TestFilter.SlowTestsOnly        // "Speed=Slow"

// 按套件过滤
TestFilter.SmokeTestsOnly       // "Suite=Smoke"
TestFilter.RegressionTestsOnly  // "Suite=Regression"

// 按优先级过滤
TestFilter.CriticalTestsOnly    // "Priority=Critical"
TestFilter.HighPriorityTestsOnly // "Priority=High"
```

### 命令行执行

#### 1. 直接使用 dotnet test

```bash
# 执行所有 UI 测试
dotnet test --filter "Type=UI"

# 执行所有 API 测试
dotnet test --filter "Type=API"

# 执行高优先级测试
dotnet test --filter "Priority=High"

# 执行快速测试
dotnet test --filter "Speed=Fast"

# 组合条件：UI 测试且高优先级
dotnet test --filter "Type=UI&Priority=High"

# 或条件：UI 测试或 API 测试
dotnet test --filter "(Type=UI|Type=API)"

# 排除慢速测试
dotnet test --filter "!Speed=Slow"
```

#### 2. 使用 TestFilter 生成命令

```csharp
// 生成测试命令
var filter = TestFilter.UITestsOnly;
var command = TestFilter.GenerateTestCommand(filter, "MyProject.Tests.csproj");
// 结果: dotnet test "MyProject.Tests.csproj" --filter "Type=UI"

// 生成详细输出命令
var verboseCommand = TestFilter.GenerateVerboseTestCommand(filter, "MyProject.Tests.csproj");
// 结果: dotnet test "MyProject.Tests.csproj" --filter "Type=UI" --verbosity normal --logger console
```

## 测试执行策略

### 使用 TestExecutionSettings

```csharp
// 创建 UI 测试执行设置
var uiSettings = TestExecutionSettings.CreateForUITests();

// 创建 API 测试执行设置
var apiSettings = TestExecutionSettings.CreateForAPITests();

// 创建集成测试执行设置
var integrationSettings = TestExecutionSettings.CreateForIntegrationTests();

// 创建混合测试执行设置
var mixedSettings = TestExecutionSettings.CreateForMixedTests();

// 创建快速测试执行设置
var fastSettings = TestExecutionSettings.CreateForFastTests();

// 创建冒烟测试执行设置
var smokeSettings = TestExecutionSettings.CreateForSmokeTests();
```

### 自定义执行设置

```csharp
var customSettings = new TestExecutionSettings
{
    TestTypes = new List<TestType> { TestType.UI, TestType.API },
    TestCategories = new List<TestCategory> { TestCategory.PageObject, TestCategory.ApiClient },
    TestPriorities = new List<TestPriority> { TestPriority.High, TestPriority.Critical },
    TestTags = new List<string> { "Search", "Authentication" },
    ParallelExecution = true,
    MaxParallelism = 4,
    TestTimeout = 300000,
    VerboseOutput = true,
    CollectCodeCoverage = true
};
```

### 使用 TestExecutionStrategy

```csharp
var logger = LoggerFactory.Create(builder => builder.AddConsole())
    .CreateLogger<TestExecutionStrategy>();

// 创建执行策略
var strategy = new TestExecutionStrategy(customSettings, logger);

// 生成过滤器表达式
var filter = strategy.GenerateFilterExpression();

// 生成执行命令
var command = strategy.GenerateExecutionCommand("MyProject.Tests.csproj");

// 执行测试
var result = await strategy.ExecuteTestsAsync("MyProject.Tests.csproj");

// 查看结果
Console.WriteLine(result.GetSummary());
Console.WriteLine(result.GetDetailedReport());
```

## 最佳实践

### 1. 测试类标记

- 每个测试类都应该有明确的类型标记（UI、API、Integration 等）
- 使用分类标记来组织相关的测试
- 为重要测试设置适当的优先级

```csharp
[UITest]
[TestCategory(TestCategory.PageObject)]
[TestPriority(TestPriority.High)]
public class HomePageTests
{
    // 测试方法
}
```

### 2. 测试方法标记

- 为特定的测试方法添加标签，便于细粒度过滤
- 使用环境标记来区分不同环境的测试

```csharp
[Fact]
[TestTag("Navigation")]
[TestEnvironment("Production")]
public void NavigateToHomePage_ShouldWork()
{
    // 测试实现
}
```

### 3. 执行策略

- 开发阶段：主要执行快速测试和单元测试
- CI/CD 管道：分阶段执行不同类型的测试
- 生产验证：执行冒烟测试和关键功能测试

```csharp
// 开发阶段
var devStrategy = TestExecutionStrategy.CreateForFastTests(logger);

// CI 阶段 1：单元测试和 API 测试
var ciStage1 = new TestExecutionSettings
{
    TestTypes = new List<TestType> { TestType.Unit, TestType.API },
    TestSpeeds = new List<string> { "Fast" }
};

// CI 阶段 2：UI 测试和集成测试
var ciStage2 = new TestExecutionSettings
{
    TestTypes = new List<TestType> { TestType.UI, TestType.Integration }
};

// 生产验证
var prodStrategy = TestExecutionStrategy.CreateForSmokeTests(logger);
```

### 4. 组织结构

建议的测试项目结构：

```
Tests/
├── Unit/           # 单元测试
│   ├── Core/       # 核心功能测试
│   ├── Services/   # 服务层测试
│   └── Utilities/  # 工具类测试
├── API/            # API 测试
│   ├── Controllers/
│   └── Integration/
├── UI/             # UI 测试
│   ├── Pages/      # 页面对象测试
│   └── Flows/      # 业务流程测试
├── Integration/    # 集成测试
└── E2E/           # 端到端测试
```

## 示例配置文件

### appsettings.Test.json

```json
{
  "TestExecution": {
    "DefaultSettings": {
      "ParallelExecution": true,
      "MaxParallelism": 4,
      "TestTimeout": 300000,
      "VerboseOutput": false,
      "CollectCodeCoverage": false
    },
    "UITestSettings": {
      "ParallelExecution": true,
      "MaxParallelism": 2,
      "TestTimeout": 600000
    },
    "APITestSettings": {
      "ParallelExecution": true,
      "MaxParallelism": 8,
      "TestTimeout": 120000
    },
    "IntegrationTestSettings": {
      "ParallelExecution": false,
      "TestTimeout": 900000
    }
  }
}
```

## 总结

测试分类标记功能提供了强大的测试组织和执行控制能力：

1. **灵活的分类系统**：支持多维度的测试分类
2. **强大的过滤功能**：支持复杂的过滤条件组合
3. **便捷的执行策略**：预定义常用的执行场景
4. **完整的工具支持**：提供工具类简化使用

通过合理使用这些功能，可以显著提高测试执行的效率和灵活性。