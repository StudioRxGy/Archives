# 邮箱通知集成指南

## 概述

邮箱通知集成功能为 CsPlaywrightXun 企业自动化框架提供了全面的邮件通知能力。该系统可以在测试执行的关键阶段自动发送邮件通知，包括测试开始、成功、失败和报告生成等事件。

## 功能特性

- ✅ 测试执行状态实时通知
- ✅ 支持 HTML 格式的邮件模板
- ✅ 灵活的收件人管理和通知规则
- ✅ SMTP 服务器配置和身份验证
- ✅ 邮件发送失败重试机制
- ✅ 通知频率限制防止邮件轰炸
- ✅ 连续失败升级通知
- ✅ 详细的错误日志和状态跟踪

## 快速开始

### 1. 配置 SMTP 设置

在项目的配置文件中添加 SMTP 服务器设置：

```json
{
  "Notifications": {
    "Enabled": true,
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "EnableSsl": true,
      "Username": "your-email@example.com",
      "Password": "your-password",
      "FromEmail": "noreply@example.com",
      "FromDisplayName": "Test Automation Framework",
      "TimeoutSeconds": 30,
      "MaxRetryAttempts": 3
    }
  }
}
```

### 2. 配置通知规则

定义不同类型通知的收件人和规则：

```json
{
  "Notifications": {
    "DefaultRecipients": [
      "qa-team@example.com"
    ],
    "Rules": [
      {
        "Id": "test-failure-notification",
        "Type": "TestFailure",
        "Recipients": [
          "qa-team@example.com",
          "dev-team@example.com"
        ],
        "IsEnabled": true,
        "CooldownPeriod": null
      }
    ]
  }
}
```

### 3. 在代码中启用通知

使用依赖注入配置通知服务：

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CsPlaywrightXun.src.playwright.Core.Configuration;

// 构建配置
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Notifications.json")
    .Build();

// 配置服务
var services = new ServiceCollection();
services.AddFrameworkServices(configuration);

// 构建服务提供者
var serviceProvider = services.BuildServiceProvider();

// 获取测试执行管理器（已集成通知功能）
var testExecutionManager = serviceProvider.GetRequiredService<TestExecutionManager>();
```

## 配置详解

### 配置文件位置

配置文件位于 `CsPlaywrightXun/src/config/` 目录：

- **appsettings.Notifications.json** - 基础通知配置
- **appsettings.Notifications.Development.json** - 开发环境配置
- **appsettings.Notifications.Production.json** - 生产环境配置
- **appsettings.Notifications.Example.json** - 完整配置示例
- **appsettings.Notifications.Minimal.json** - 最小配置示例

### 配置文件加载顺序

配置文件按以下顺序加载（后面的文件会覆盖前面的）：

1. `appsettings.json`
2. `appsettings.Notifications.json`
3. `appsettings.Notifications.{Environment}.json`
4. 环境变量

### SMTP 配置

| 配置项 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| Host | string | 是 | - | SMTP 服务器地址 |
| Port | int | 是 | 587 | SMTP 服务器端口 |
| EnableSsl | bool | 否 | true | 是否启用 SSL/TLS 加密 |
| Username | string | 否 | - | SMTP 认证用户名 |
| Password | string | 否 | - | SMTP 认证密码 |
| FromEmail | string | 是 | - | 发件人邮箱地址 |
| FromDisplayName | string | 否 | - | 发件人显示名称 |
| TimeoutSeconds | int | 否 | 30 | 连接超时时间（秒） |
| MaxRetryAttempts | int | 否 | 3 | 最大重试次数 |

### 通知规则配置

| 配置项 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| Id | string | 是 | 规则唯一标识符 |
| Type | enum | 是 | 通知类型（TestStart, TestSuccess, TestFailure, CriticalFailure, ReportGenerated） |
| Recipients | string[] | 是 | 收件人邮箱地址列表 |
| IsEnabled | bool | 否 | 是否启用该规则（默认 true） |
| CooldownPeriod | TimeSpan? | 否 | 通知冷却期，防止频繁发送 |
| Conditions | object | 否 | 额外的触发条件 |

### 通知类型说明

- **TestStart**: 测试套件开始执行时发送
- **TestSuccess**: 测试套件成功完成时发送
- **TestFailure**: 测试套件失败时发送
- **CriticalFailure**: 关键测试用例失败时立即发送
- **ReportGenerated**: 测试报告生成完成时发送

## 环境特定配置

### 开发环境

使用本地 SMTP 服务器（如 MailHog）进行测试：

```json
{
  "Notifications": {
    "Enabled": true,
    "Smtp": {
      "Host": "localhost",
      "Port": 1025,
      "EnableSsl": false,
      "Username": "",
      "Password": "",
      "FromEmail": "dev-tests@localhost",
      "FromDisplayName": "Development Test Framework"
    }
  }
}
```

### 生产环境

使用环境变量保护敏感信息：

```json
{
  "Notifications": {
    "Enabled": true,
    "Smtp": {
      "Host": "${SMTP_HOST}",
      "Port": 587,
      "EnableSsl": true,
      "Username": "${SMTP_USERNAME}",
      "Password": "${SMTP_PASSWORD}",
      "FromEmail": "${SMTP_FROM_EMAIL}",
      "FromDisplayName": "Production Test Automation"
    }
  }
}
```

## 高级用法

### 自定义邮件模板

系统支持自定义 HTML 邮件模板：

```csharp
var templateEngine = serviceProvider.GetRequiredService<IEmailTemplateEngine>();

// 注册自定义模板
templateEngine.RegisterTemplate("CustomFailure", @"
<html>
<body>
    <h1>Test Failed: {{TestSuiteName}}</h1>
    <p>Failed Tests: {{FailedTests}}</p>
    <p>Pass Rate: {{PassRate}}%</p>
</body>
</html>
");
```

### 条件通知规则

根据测试结果的特定条件发送通知：

```json
{
  "Id": "high-priority-failure",
  "Type": "TestFailure",
  "Recipients": ["manager@example.com"],
  "IsEnabled": true,
  "Conditions": {
    "Environment": "Production",
    "PassRate": "<80"
  }
}
```

### 通知频率限制

设置冷却期防止邮件轰炸：

```json
{
  "Id": "success-notification",
  "Type": "TestSuccess",
  "Recipients": ["qa-team@example.com"],
  "IsEnabled": true,
  "CooldownPeriod": "00:15:00"
}
```

## 配置验证

系统提供多种配置验证工具确保配置正确：

### 使用命令行验证工具

最简单的验证方法是使用内置的配置验证工具：

```bash
# 验证配置文件
dotnet run --project CsPlaywrightXun/src/playwright/Services/Notifications/ConfigurationValidationTool.cs -- appsettings.Notifications.json

# 验证开发环境配置
dotnet run --project ConfigurationValidationTool -- appsettings.Notifications.Development.json

# 验证生产环境配置
dotnet run --project ConfigurationValidationTool -- appsettings.Notifications.Production.json
```

验证工具会检查：
- ✓ 配置文件格式是否正确
- ✓ 所有必填字段是否存在
- ✓ 邮箱地址格式是否有效
- ✓ 端口号和超时值是否在有效范围内
- ✓ 通知规则是否有重复ID
- ✓ 冷却期格式是否正确

### 使用 ConfigurationValidator 类

在代码中验证配置：

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.Services.Notifications;

// 加载配置
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Notifications.json")
    .Build();

// 创建验证器
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ConfigurationValidator>();
var validator = new ConfigurationValidator(logger);

// 验证配置
var result = validator.ValidateConfiguration(configuration);

if (result.IsValid)
{
    Console.WriteLine("✓ Configuration is valid!");
}
else
{
    Console.WriteLine($"✗ Configuration has {result.Errors.Count} error(s):");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

### 验证特定配置组件

```csharp
// 验证 SMTP 配置
var smtpConfig = configuration.GetSection("Notifications:Smtp").Get<SmtpConfiguration>();
var smtpResult = validator.ValidateSmtpConfiguration(smtpConfig);

// 验证通知规则
var rule = configuration.GetSection("Notifications:Rules:0").Get<NotificationRule>();
var ruleResult = validator.ValidateNotificationRule(rule);

// 验证邮件消息
var message = new EmailMessage
{
    ToAddresses = new List<string> { "test@example.com" },
    Subject = "Test",
    HtmlBody = "<p>Test</p>"
};
var messageResult = validator.ValidateEmailMessage(message);
```

### 测试 SMTP 连接

验证配置后，测试实际的 SMTP 连接：

```csharp
var notificationService = serviceProvider.GetRequiredService<IEmailNotificationService>();

// 验证 SMTP 配置
var isValid = await notificationService.ValidateSmtpConfigurationAsync();
if (!isValid)
{
    Console.WriteLine("SMTP configuration is invalid!");
}

// 测试 SMTP 连接
var smtpClient = serviceProvider.GetRequiredService<ISmtpClient>();
var canConnect = await smtpClient.TestConnectionAsync();
if (!canConnect)
{
    Console.WriteLine("Cannot connect to SMTP server!");
}
else
{
    Console.WriteLine("✓ SMTP connection successful!");
}
```

### 配置验证最佳实践

1. **在部署前验证** - 始终在部署到生产环境前验证配置
2. **使用 CI/CD 集成** - 将配置验证添加到 CI/CD 流程中
3. **定期测试连接** - 定期测试 SMTP 连接以确保服务可用
4. **记录验证结果** - 保存验证日志以便排查问题
5. **自动化验证** - 在应用启动时自动验证配置

## 常见 SMTP 服务器配置

### Gmail

```json
{
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "your-email@gmail.com",
  "Password": "your-app-password"
}
```

**注意**: Gmail 需要使用应用专用密码，不能使用账户密码。

### Outlook/Office 365

```json
{
  "Host": "smtp.office365.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "your-email@outlook.com",
  "Password": "your-password"
}
```

### SendGrid

```json
{
  "Host": "smtp.sendgrid.net",
  "Port": 587,
  "EnableSsl": true,
  "Username": "apikey",
  "Password": "your-sendgrid-api-key"
}
```

### Amazon SES

```json
{
  "Host": "email-smtp.us-east-1.amazonaws.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "your-smtp-username",
  "Password": "your-smtp-password"
}
```

## 故障排查

### 邮件发送失败

1. **检查 SMTP 配置**: 确保主机地址、端口和认证信息正确
2. **验证网络连接**: 确保可以访问 SMTP 服务器
3. **检查防火墙**: 确保 SMTP 端口未被阻止
4. **查看日志**: 检查详细的错误日志信息

### 未收到邮件

1. **检查垃圾邮件文件夹**: 邮件可能被标记为垃圾邮件
2. **验证收件人地址**: 确保邮箱地址格式正确
3. **检查通知规则**: 确保规则已启用且条件匹配
4. **查看冷却期**: 检查是否在冷却期内

### 配置验证错误

使用配置验证工具获取详细错误信息：

```csharp
var settings = configuration.GetSection("Notifications").Get<NotificationSettings>();
var errors = settings.GetValidationErrors();
foreach (var error in errors)
{
    Console.WriteLine($"Configuration error: {error}");
}
```

## 最佳实践

1. **使用环境变量**: 在生产环境中使用环境变量保护敏感信息
2. **设置冷却期**: 为非关键通知设置合理的冷却期
3. **分级通知**: 根据严重程度配置不同的收件人列表
4. **测试配置**: 在部署前验证 SMTP 配置和连接
5. **监控日志**: 定期检查通知发送日志
6. **使用专用邮箱**: 使用专门的邮箱账户发送通知
7. **限制收件人**: 避免向过多收件人发送通知

## 安全建议

1. **不要在代码中硬编码密码**: 使用配置文件或环境变量
2. **启用 SSL/TLS**: 始终使用加密连接
3. **使用应用专用密码**: 对于支持的服务（如 Gmail）
4. **定期更换密码**: 定期更新 SMTP 认证密码
5. **限制访问权限**: 限制对配置文件的访问权限
6. **使用密钥管理服务**: 在云环境中使用 Azure Key Vault 或 AWS Secrets Manager

## 示例代码

### 完整的集成示例

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Core.Utilities;

public class NotificationIntegrationExample
{
    public static async Task Main(string[] args)
    {
        // 1. 构建配置
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Notifications.json", optional: true)
            .AddJsonFile($"appsettings.Notifications.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // 2. 配置服务
        var services = new ServiceCollection();
        services.AddFrameworkServices(configuration);

        // 3. 构建服务提供者
        var serviceProvider = services.BuildServiceProvider();

        // 4. 获取日志记录器
        var logger = serviceProvider.GetRequiredService<ILogger<TestExecutionManager>>();

        // 5. 获取测试执行管理器（已集成通知功能）
        var testExecutionManager = serviceProvider.GetRequiredService<TestExecutionManager>();

        // 6. 执行测试（通知将自动发送）
        var result = await testExecutionManager.ExecuteUITestsOnlyAsync();

        Console.WriteLine($"Test execution completed: {result.Success}");
        Console.WriteLine($"Total tests: {result.TotalTests}");
        Console.WriteLine($"Passed: {result.PassedTests}");
        Console.WriteLine($"Failed: {result.FailedTests}");
    }
}
```

## 相关文档

- [API 参考文档](./api-reference.md)
- [测试执行管理器指南](./TestExecutionManager.md)
- [配置管理指南](./ConfigurationGuide.md)
- [故障排查指南](./troubleshooting-guide.md)

## 支持

如有问题或需要帮助，请：

1. 查看故障排查部分
2. 检查日志文件获取详细错误信息
3. 联系开发团队获取支持

## 更新日志

### v1.0.0 (2026-01-14)
- 初始版本发布
- 支持基本的邮件通知功能
- 支持 SMTP 配置和身份验证
- 支持通知规则和收件人管理
- 支持邮件模板和内容定制
