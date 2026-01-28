# Trait 快速参考卡片

## 🎯 常用命令速查

### 按速度
```bash
# 快速测试（< 5秒）
dotnet test --filter "Fast=true"

# 慢速测试（> 5秒）
dotnet test --filter "Slow=true"
```

### 按类别
```bash
# 登录测试
dotnet test --filter "Category=Login"

# 交易测试
dotnet test --filter "Category=Trade"

# 买入订单
dotnet test --filter "Category=BuyOrder"

# 平仓测试
dotnet test --filter "Category=ClosePosition"

# 验证测试
dotnet test --filter "Category=Validation"

# 异常测试
dotnet test --filter "Category=Exception"

# 端到端测试
dotnet test --filter "Category=E2E"
```

### 按优先级
```bash
# 关键测试
dotnet test --filter "Priority=Critical"

# 高优先级
dotnet test --filter "Priority=High"

# 中优先级
dotnet test --filter "Priority=Medium"
```

### 冒烟测试
```bash
dotnet test --filter "Smoke=true"
```

## 🔥 推荐工作流

### 开发中（每5分钟）
```bash
dotnet test --filter "Fast=true"
```
⏱️ ~10秒

### 提交前（每次提交）
```bash
dotnet test --filter "Priority=High"
```
⏱️ ~20秒

### 功能完成（每个功能）
```bash
dotnet test --filter "Category=Trade"
```
⏱️ ~30秒

### 发布前（每次发布）
```bash
dotnet test
```
⏱️ ~60秒

### 部署后（生产验证）
```bash
dotnet test --filter "Smoke=true"
```
⏱️ ~15秒

## 📊 测试分布

| 类别 | 数量 | 速度 |
|------|------|------|
| Login | 3 | Fast |
| Trade | 6 | Mixed |
| BuyOrder | 3 | Mixed |
| ClosePosition | 2 | Mixed |
| Validation | 2 | Fast |
| Exception | 2 | Fast |
| E2E | 1 | Slow |

## 💡 组合技巧

```bash
# 快速的交易测试
dotnet test --filter "Category=Trade&Fast=true"

# 高优先级的快速测试
dotnet test --filter "Priority=High&Fast=true"

# 登录或异常测试
dotnet test --filter "Category=Login|Category=Exception"

# 非慢速测试（排除慢速）
dotnet test --filter "Fast=true"
```

## 🎨 Trait 标记模板

```csharp
[Fact(DisplayName = "测试描述")]
[Trait("Category", "主类别")]
[Trait("Fast", "true")]  // 或 Slow
[Trait("Priority", "High")]  // Critical/High/Medium/Low
public async Task TestMethod()
{
    // 测试代码
}
```

## 📱 保存为书签

将此页面保存为书签，随时查看常用命令！
