# Trait 标记使用指南

## 📋 概述

所有测试已使用 `[Trait]` 特性进行分类标记，可以灵活地按类别、速度、优先级等维度运行测试。

## 🏷️ Trait 分类体系

### 1. Category（类别）

#### Login - 登录相关
```csharp
[Trait("Category", "Login")]
```
- 登录功能测试
- Token 提取测试

#### Trade - 交易相关
```csharp
[Trait("Category", "Trade")]
```
- 所有交易操作测试

#### BuyOrder - 买入订单
```csharp
[Trait("Category", "BuyOrder")]
```
- 市价买入订单创建

#### ClosePosition - 平仓
```csharp
[Trait("Category", "ClosePosition")]
```
- 闪电平仓操作

#### Validation - 验证测试
```csharp
[Trait("Category", "Validation")]
```
- 字段验证
- 数据格式验证

#### Exception - 异常测试
```csharp
[Trait("Category", "Exception")]
```
- 异常场景测试

#### Negative - 负面测试
```csharp
[Trait("Category", "Negative")]
```
- 错误输入测试
- 边界条件测试

#### E2E - 端到端测试
```csharp
[Trait("Category", "E2E")]
```
- 完整业务流程测试

#### FullFlow - 完整流程
```csharp
[Trait("Category", "FullFlow")]
```
- 多步骤集成测试

### 2. Speed（速度）

#### Fast - 快速测试
```csharp
[Trait("Fast", "true")]
```
- 执行时间 < 5秒
- 适合频繁运行
- 包括：登录、验证、异常测试

#### Slow - 慢速测试
```csharp
[Trait("Slow", "true")]
```
- 执行时间 > 5秒
- 包括：完整流程、实际交易操作

### 3. Priority（优先级）

#### Critical - 关键测试
```csharp
[Trait("Priority", "Critical")]
```
- 核心业务流程
- 必须通过的测试

#### High - 高优先级
```csharp
[Trait("Priority", "High")]
```
- 重要功能测试
- 登录、核心交易功能

#### Medium - 中优先级
```csharp
[Trait("Priority", "Medium")]
```
- 辅助功能测试
- 验证测试

#### Low - 低优先级
```csharp
[Trait("Priority", "Low")]
```
- 边缘场景测试

### 4. Smoke（冒烟测试）

```csharp
[Trait("Smoke", "true")]
```
- 最基本的功能验证
- 部署后首先运行
- 快速验证系统可用性

## 🚀 运行测试命令

### 按类别运行

#### 运行所有登录测试
```bash
dotnet test --filter "Category=Login"
```

#### 运行所有交易测试
```bash
dotnet test --filter "Category=Trade"
```

#### 运行买入订单测试
```bash
dotnet test --filter "Category=BuyOrder"
```

#### 运行平仓测试
```bash
dotnet test --filter "Category=ClosePosition"
```

#### 运行验证测试
```bash
dotnet test --filter "Category=Validation"
```

#### 运行异常测试
```bash
dotnet test --filter "Category=Exception"
```

#### 运行端到端测试
```bash
dotnet test --filter "Category=E2E"
```

### 按速度运行

#### 只运行快速测试
```bash
dotnet test --filter "Fast=true"
```

#### 只运行慢速测试
```bash
dotnet test --filter "Slow=true"
```

### 按优先级运行

#### 运行关键测试
```bash
dotnet test --filter "Priority=Critical"
```

#### 运行高优先级测试
```bash
dotnet test --filter "Priority=High"
```

#### 运行中优先级测试
```bash
dotnet test --filter "Priority=Medium"
```

### 运行冒烟测试

```bash
dotnet test --filter "Smoke=true"
```

## 🎯 组合过滤

### AND 操作（同时满足）

#### 快速且高优先级的测试
```bash
dotnet test --filter "Fast=true&Priority=High"
```

#### 交易类且快速的测试
```bash
dotnet test --filter "Category=Trade&Fast=true"
```

#### 登录类且高优先级的测试
```bash
dotnet test --filter "Category=Login&Priority=High"
```

### OR 操作（满足任一）

#### 登录或异常测试
```bash
dotnet test --filter "Category=Login|Category=Exception"
```

#### 快速或高优先级测试
```bash
dotnet test --filter "Fast=true|Priority=High"
```

#### 买入或平仓测试
```bash
dotnet test --filter "Category=BuyOrder|Category=ClosePosition"
```

### 复杂组合

#### 快速的交易测试或所有登录测试
```bash
dotnet test --filter "(Category=Trade&Fast=true)|Category=Login"
```

#### 高优先级的快速测试
```bash
dotnet test --filter "Priority=High&Fast=true"
```

#### 非慢速的测试（排除慢速测试）
```bash
dotnet test --filter "Fast=true"
```

## 📊 测试分布

### UheyueApiTests.cs（标准测试）

| 测试 | Category | Speed | Priority | Smoke |
|------|----------|-------|----------|-------|
| Test01 - 登录成功 | Login | Fast | High | - |
| Test02 - 提取Token | Login | Fast | High | - |
| Test03 - 创建BTC订单 | Trade, BuyOrder | Slow | High | - |
| Test04 - 验证订单字段 | Trade, Validation | Fast | Medium | - |
| Test05 - 执行平仓 | Trade, ClosePosition | Slow | High | - |
| Test06 - 验证平仓字段 | Trade, Validation | Fast | Medium | - |
| Test07 - 完整流程 | E2E, FullFlow | Slow | Critical | ✓ |
| Test08 - 未设置Token | Exception, Negative | Fast | Medium | - |
| Test09 - 空Token | Exception, Negative | Fast | Medium | - |

### UheyueApiTestsWithFixture.cs（快速测试）

| 测试 | Category | Speed | Priority |
|------|----------|-------|----------|
| Test01 - 验证Token | Login | Fast | High |
| Test02 - 快速创建订单 | Trade, BuyOrder | Fast | High |
| Test03 - 快速平仓 | Trade, ClosePosition | Fast | High |
| Test04 - 批量订单 | Trade, BuyOrder | Fast | Medium |

## 🎨 使用场景

### 场景1：开发阶段 - 快速反馈
```bash
# 只运行快速测试
dotnet test --filter "Fast=true"
```
**预计时间：** 10-15秒

### 场景2：提交前验证 - 高优先级测试
```bash
# 运行所有高优先级测试
dotnet test --filter "Priority=High"
```
**预计时间：** 20-30秒

### 场景3：冒烟测试 - 部署后验证
```bash
# 运行冒烟测试
dotnet test --filter "Smoke=true"
```
**预计时间：** 15-20秒

### 场景4：完整回归 - 所有测试
```bash
# 运行所有测试
dotnet test
```
**预计时间：** 40-60秒

### 场景5：功能验证 - 特定类别
```bash
# 只验证登录功能
dotnet test --filter "Category=Login"

# 只验证交易功能
dotnet test --filter "Category=Trade"
```

### 场景6：异常测试 - 错误处理验证
```bash
# 运行所有异常测试
dotnet test --filter "Category=Exception"
```

### 场景7：CI/CD - 分阶段测试

#### 第一阶段：快速验证
```bash
dotnet test --filter "Fast=true"
```

#### 第二阶段：关键功能
```bash
dotnet test --filter "Priority=Critical|Priority=High"
```

#### 第三阶段：完整测试
```bash
dotnet test
```

## 💡 最佳实践

### 1. 本地开发
```bash
# 频繁运行快速测试
dotnet test --filter "Fast=true"
```

### 2. 提交代码前
```bash
# 运行高优先级测试
dotnet test --filter "Priority=High"
```

### 3. 功能开发完成
```bash
# 运行相关类别的所有测试
dotnet test --filter "Category=Trade"
```

### 4. 发布前
```bash
# 运行所有测试
dotnet test
```

### 5. 生产部署后
```bash
# 运行冒烟测试
dotnet test --filter "Smoke=true"
```

## 📈 性能优化建议

### 快速反馈循环
```bash
# 1. 开发时只运行快速测试
dotnet test --filter "Fast=true"

# 2. 功能完成后运行相关类别
dotnet test --filter "Category=Login"

# 3. 提交前运行高优先级
dotnet test --filter "Priority=High"

# 4. 最后运行完整测试
dotnet test
```

### 并行执行
Xunit 默认并行运行测试类，快速测试可以充分利用并行优势。

## 🔍 调试特定测试

### 调试单个类别
```bash
# 详细输出登录测试
dotnet test --filter "Category=Login" --logger "console;verbosity=detailed"
```

### 调试失败的测试
```bash
# 只运行上次失败的测试
dotnet test --filter "Category=Exception" --logger "console;verbosity=detailed"
```

## 📝 添加新测试时的标记建议

### 标准模板
```csharp
[Fact(DisplayName = "测试XX - 描述")]
[Trait("Category", "主类别")]
[Trait("Category", "子类别")]  // 可选
[Trait("Fast", "true")]  // 或 [Trait("Slow", "true")]
[Trait("Priority", "High")]  // Critical/High/Medium/Low
[Trait("Smoke", "true")]  // 可选，仅关键测试
public async Task TestXX_Description()
{
    // 测试代码
}
```

### 示例
```csharp
[Fact(DisplayName = "测试10 - 查询订单状态")]
[Trait("Category", "Trade")]
[Trait("Category", "Query")]
[Trait("Fast", "true")]
[Trait("Priority", "Medium")]
public async Task Test10_QueryOrderStatus()
{
    // 测试代码
}
```

## 🎉 总结

使用 Trait 标记后，你可以：

✅ **灵活运行** - 按需选择运行哪些测试
✅ **快速反馈** - 开发时只运行快速测试
✅ **分层测试** - CI/CD 中分阶段运行
✅ **精准定位** - 快速找到特定类型的测试
✅ **优化效率** - 避免每次都运行所有测试

## 🚀 立即尝试

```bash
# 快速验证
dotnet test --filter "Fast=true"

# 冒烟测试
dotnet test --filter "Smoke=true"

# 完整测试
dotnet test
```
