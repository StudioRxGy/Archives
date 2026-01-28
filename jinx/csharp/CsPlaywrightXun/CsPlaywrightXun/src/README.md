# 企业级 C# + Playwright + xUnit 自动化测试框架

## 概述

这是一个企业级的自动化测试框架，基于 C# + Playwright + xUnit 构建，提供了完整的 Web UI 和 API 自动化测试解决方案。框架采用分层架构设计，具有高可维护性、稳定性和可扩展性。

## 🚀 主要特性

### 核心功能
- **分层架构**：Tests → Scenarios → Flows → Pages/Components → Playwright/HTTP
- **多测试类型支持**：UI 测试、API 测试、集成测试、端到端测试
- **数据驱动测试**：支持 CSV、JSON、YAML 数据源
- **多环境配置**：支持 dev、test、staging、prod 环境
- **并行执行**：支持测试并行执行，提高执行效率
- **智能重试**：可配置的重试策略和错误恢复机制

### 高级功能
- **Page Object 模式**：混合定位器管理策略
- **Flow 业务流程抽象**：封装复杂业务逻辑
- **测试分类标记**：支持测试过滤和选择性执行
- **全面日志记录**：结构化日志和详细操作记录
- **自动截图**：失败时自动截图，便于问题诊断
- **丰富报告**：HTML 报告、Allure 集成、历史趋势分析

## 📁 项目结构

```
CsPlaywrightXun/
├── src/
│   ├── playwright/                        # 框架核心
│   │   ├── Core/                         # 核心接口和基类
│   │   │   ├── Interfaces/               # 接口定义
│   │   │   ├── Base/                     # 基类实现
│   │   │   ├── Configuration/            # 配置管理
│   │   │   ├── Exceptions/               # 自定义异常
│   │   │   ├── Models/                   # 数据模型
│   │   │   └── Utilities/                # 工具类
│   │   ├── Pages/                        # 页面对象
│   │   ├── Flows/                        # 业务流程
│   │   ├── Services/                     # 服务层
│   │   │   ├── Browser/                  # 浏览器服务
│   │   │   ├── Api/                      # API服务
│   │   │   ├── Data/                     # 数据服务
│   │   │   └── Reporting/                # 报告服务
│   │   └── Tests/                        # 测试用例
│   │       ├── UI/                       # UI测试
│   │       ├── API/                      # API测试
│   │       └── Integration/              # 集成测试
│   ├── config/                           # 配置文件
│   │   ├── date/                         # 测试数据
│   │   ├── elements/                     # 页面元素配置
│   │   └── environments/                 # 环境配置
│   ├── conclusion/                       # 输出目录
│   │   ├── logs/                         # 日志文件
│   │   ├── reports/                      # 测试报告
│   │   └── screenshots/                  # 截图文件
│   └── docs/                             # 文档
└── CsPlaywrightXun.csproj               # 项目文件
```

## 🛠️ 快速开始

### 1. 环境要求

- .NET 6.0 或更高版本
- Visual Studio 2022 或 VS Code
- Playwright 浏览器驱动

### 2. 安装依赖

```bash
# 克隆项目
git clone <repository-url>
cd CsPlaywrightXun

# 还原 NuGet 包
dotnet restore

# 安装 Playwright 浏览器
pwsh bin/Debug/net6.0/playwright.ps1 install
```

### 3. 运行示例测试

```bash
# 运行所有测试
dotnet test

# 运行 UI 测试
dotnet test --filter "Type=UI"

# 运行 API 测试
dotnet test --filter "Type=API"

# 运行快速测试
dotnet test --filter "Speed=Fast"
```

## 📖 使用指南

### 创建页面对象

```csharp
[UITest]
[TestCategory(TestCategory.PageObject)]
public class LoginPage : BasePageObjectWithPlaywright
{
    private const string UsernameSelector = "#username";
    private const string PasswordSelector = "#password";
    private const string LoginButtonSelector = "#login-btn";
    
    public LoginPage(IPage page, ILogger logger) : base(page, logger) { }
    
    public async Task LoginAsync(string username, string password)
    {
        await TypeAsync(UsernameSelector, username);
        await TypeAsync(PasswordSelector, password);
        await ClickAsync(LoginButtonSelector);
    }
    
    public override async Task<bool> IsLoadedAsync()
    {
        return await IsElementExistAsync(LoginButtonSelector);
    }
    
    public override async Task WaitForLoadAsync(int timeoutMs = 30000)
    {
        await WaitForElementAsync(LoginButtonSelector, timeoutMs);
    }
}
```

### 创建业务流程

```csharp
public class LoginFlow : BaseFlow
{
    private readonly LoginPage _loginPage;
    
    public LoginFlow(LoginPage loginPage, ILogger logger) : base(logger)
    {
        _loginPage = loginPage;
    }
    
    public override async Task ExecuteAsync(Dictionary<string, object> parameters = null)
    {
        var username = parameters?["username"]?.ToString();
        var password = parameters?["password"]?.ToString();
        
        Logger.LogInformation("开始执行登录流程");
        
        await _loginPage.LoginAsync(username, password);
        
        Logger.LogInformation("登录流程执行完成");
    }
}
```

### 编写测试用例

```csharp
[UITest]
[TestPriority(TestPriority.High)]
public class LoginTests : IClassFixture<BrowserFixture>
{
    private readonly BrowserFixture _fixture;
    private readonly LoginPage _loginPage;
    private readonly LoginFlow _loginFlow;
    
    public LoginTests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _loginPage = new LoginPage(_fixture.Page, _fixture.Logger);
        _loginFlow = new LoginFlow(_loginPage, _fixture.Logger);
    }
    
    [Theory]
    [CsvData("TestData/login_data.csv")]
    public async Task Login_WithValidCredentials_ShouldSucceed(LoginTestData data)
    {
        // Arrange
        await _loginPage.NavigateAsync(data.BaseUrl);
        await _loginPage.WaitForLoadAsync();
        
        // Act
        var parameters = new Dictionary<string, object>
        {
            ["username"] = data.Username,
            ["password"] = data.Password
        };
        await _loginFlow.ExecuteAsync(parameters);
        
        // Assert
        var result = await _loginPage.AssertEqualAsync(
            await _loginPage.GetTitleAsync(), 
            "Dashboard"
        );
        Assert.Equal("pass", result);
    }
}
```

### API 测试示例

```csharp
[APITest]
[TestCategory(TestCategory.ApiClient)]
public class UserApiTests : BaseApiTest
{
    public UserApiTests(IApiClient apiClient, TestConfiguration config, ILogger logger) 
        : base(apiClient, config, logger) { }
    
    [Fact]
    public async Task GetUser_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/api/users/1"
        };
        
        // Act
        var response = await ExecuteApiTestAsync<User>(request);
        
        // Assert
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Data);
        Assert.Equal(1, response.Data.Id);
    }
}
```

## 🔧 配置管理

### 环境配置

在 `src/config/environments/` 目录下创建环境配置文件：

```json
// appsettings.Development.json
{
  "Environment": {
    "Name": "Development",
    "BaseUrl": "https://dev.example.com",
    "ApiBaseUrl": "https://api-dev.example.com"
  },
  "Browser": {
    "Type": "Chromium",
    "Headless": false,
    "ViewportWidth": 1920,
    "ViewportHeight": 1080
  },
  "Logging": {
    "Level": "Debug",
    "FilePath": "src/conclusion/logs/test-{Date}.log"
  }
}
```

### 页面元素配置

在 `src/config/elements/` 目录下创建 YAML 元素配置：

```yaml
# HomePage.yaml
HomePage:
  SearchBox:
    selector: "#search-input"
    type: Input
    timeout: 5000
  SearchButton:
    selector: "#search-btn"
    type: Button
    timeout: 5000
  SearchResults:
    selector: ".search-result"
    type: Text
    timeout: 10000
```

### 测试数据配置

在 `src/config/date/` 目录下创建测试数据文件：

```csv
# login_data.csv
TestName,Username,Password,ExpectedResult,Environment
有效登录测试,testuser,password123,success,Development
无效用户名测试,invaliduser,password123,failure,Development
无效密码测试,testuser,wrongpassword,failure,Development
```

## 🧪 测试执行

### 基本执行命令

```bash
# 执行所有测试
dotnet test

# 执行特定类型的测试
dotnet test --filter "Type=UI"
dotnet test --filter "Type=API"
dotnet test --filter "Type=Integration"

# 执行特定优先级的测试
dotnet test --filter "Priority=High"
dotnet test --filter "Priority=Critical"

# 执行快速测试
dotnet test --filter "Speed=Fast"

# 执行冒烟测试
dotnet test --filter "Suite=Smoke"
```

### 高级过滤

```bash
# 组合条件：UI 测试且高优先级
dotnet test --filter "Type=UI&Priority=High"

# 或条件：UI 测试或 API 测试
dotnet test --filter "(Type=UI|Type=API)"

# 排除慢速测试
dotnet test --filter "!Speed=Slow"

# 特定环境的测试
dotnet test --filter "Environment=Production"

# 特定标签的测试
dotnet test --filter "Tag=Authentication"
```

### 并行执行

```bash
# 设置并行度
dotnet test --parallel --max-cpucount:4

# 禁用并行执行
dotnet test --parallel --max-cpucount:1
```

## 📊 报告和日志

### 查看测试报告

测试执行完成后，可以在以下位置查看报告：

- **HTML 报告**：`src/conclusion/reports/test-report.html`
- **Allure 报告**：`src/conclusion/reports/allure/`
- **日志文件**：`src/conclusion/logs/`
- **截图文件**：`src/conclusion/screenshots/`

### 生成详细报告

```bash
# 生成详细的测试报告
dotnet test --logger "html;LogFileName=detailed-report.html"

# 生成 Allure 报告
dotnet test --logger "allure;LogFileName=allure-results"
```

## 🔍 故障排除

### 常见问题

#### 1. 浏览器驱动问题

```bash
# 重新安装 Playwright 浏览器
pwsh bin/Debug/net6.0/playwright.ps1 install

# 检查浏览器版本
pwsh bin/Debug/net6.0/playwright.ps1 --version
```

#### 2. 元素定位失败

- 检查元素选择器是否正确
- 确认页面是否完全加载
- 增加等待时间
- 使用浏览器开发者工具验证选择器

#### 3. 测试数据问题

- 检查 CSV/JSON/YAML 文件格式
- 确认文件路径是否正确
- 验证数据类型匹配

#### 4. 配置问题

- 检查环境配置文件是否存在
- 验证配置文件格式是否正确
- 确认环境变量设置

### 调试技巧

#### 1. 启用详细日志

```json
{
  "Logging": {
    "Level": "Debug"
  }
}
```

#### 2. 禁用无头模式

```json
{
  "Browser": {
    "Headless": false
  }
}
```

#### 3. 增加超时时间

```json
{
  "Browser": {
    "Timeout": 60000
  }
}
```

#### 4. 启用截图

```csharp
// 在测试失败时自动截图
await TakeScreenshotAsync("failure-screenshot.png");
```

## 🤝 贡献指南

### 代码规范

1. **命名约定**：使用 PascalCase 命名类和方法，camelCase 命名变量
2. **注释规范**：使用 XML 文档注释
3. **异步方法**：所有 I/O 操作使用异步方法
4. **异常处理**：使用自定义异常类型
5. **日志记录**：记录关键操作和错误信息

### 提交规范

1. **分支命名**：feature/功能名称、bugfix/问题描述
2. **提交信息**：使用清晰的提交信息描述变更
3. **代码审查**：所有代码变更需要经过审查
4. **测试覆盖**：新功能需要包含相应的测试用例

## 📚 更多文档

- [架构设计文档](design.md)
- [Playwright 基类使用指南](PlaywrightBaseClassGuide.md)
- [测试分类标记指南](TestCategoryGuide.md)
- [API 参考文档](api-reference.md)
- [最佳实践指南](best-practices.md)
- [常见问题解答](faq.md)

## 📞 支持

如果您在使用过程中遇到问题，可以通过以下方式获取帮助：

1. 查看文档和示例代码
2. 检查日志文件和错误信息
3. 在项目仓库中提交 Issue
4. 联系开发团队

## 📄 许可证

本项目采用 MIT 许可证，详情请参见 [LICENSE](LICENSE) 文件。