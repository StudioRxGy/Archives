# 最佳实践指南

## 概述

本指南提供了使用企业级 C# + Playwright + xUnit 自动化测试框架的最佳实践建议。遵循这些实践可以帮助您构建更稳定、可维护和高效的自动化测试。

## 📋 目录

- [项目组织](#项目组织)
- [Page Object 设计](#page-object-设计)
- [Flow 业务流程](#flow-业务流程)
- [测试用例编写](#测试用例编写)
- [数据管理](#数据管理)
- [错误处理](#错误处理)
- [性能优化](#性能优化)
- [代码质量](#代码质量)
- [CI/CD 集成](#cicd-集成)

## 🗂️ 项目组织

### 目录结构最佳实践

```
CsPlaywrightXun/
├── src/
│   ├── playwright/
│   │   ├── Core/                     # 框架核心 - 稳定，很少修改
│   │   │   ├── Interfaces/           # 接口定义
│   │   │   ├── Base/                 # 基类实现
│   │   │   ├── Configuration/        # 配置管理
│   │   │   ├── Exceptions/           # 自定义异常
│   │   │   └── Utilities/            # 工具类
│   │   ├── Services/                 # 服务层 - 中等频率修改
│   │   │   ├── Browser/              # 浏览器服务
│   │   │   ├── Api/                  # API 服务
│   │   │   ├── Data/                 # 数据服务
│   │   │   └── Reporting/            # 报告服务
│   │   ├── Pages/                    # 页面对象 - 频繁修改
│   │   │   └── UI/
│   │   │       ├── common/           # 通用页面组件
│   │   │       ├── login/            # 登录相关页面
│   │   │       ├── dashboard/        # 仪表板页面
│   │   │       └── settings/         # 设置页面
│   │   ├── Flows/                    # 业务流程 - 频繁修改
│   │   │   ├── authentication/       # 认证流程
│   │   │   ├── user-management/      # 用户管理流程
│   │   │   └── reporting/            # 报告流程
│   │   └── Tests/                    # 测试用例 - 最频繁修改
│   │       ├── UI/                   # UI 测试
│   │       │   ├── smoke/            # 冒烟测试
│   │       │   ├── regression/       # 回归测试
│   │       │   └── feature/          # 功能测试
│   │       ├── API/                  # API 测试
│   │       └── Integration/          # 集成测试
│   ├── config/                       # 配置文件
│   │   ├── environments/             # 环境配置
│   │   ├── elements/                 # 页面元素配置
│   │   └── date/                     # 测试数据
│   └── conclusion/                   # 输出目录
│       ├── logs/                     # 日志文件
│       ├── reports/                  # 测试报告
│       └── screenshots/              # 截图文件
```

### 命名约定

#### 1. 文件和类命名

```csharp
// ✅ 好的命名
public class LoginPage : BasePageObjectWithPlaywright { }
public class UserRegistrationFlow : BaseFlow { }
public class AuthenticationTests : IClassFixture<BrowserFixture> { }

// ❌ 避免的命名
public class Page1 : BasePageObjectWithPlaywright { }
public class Flow : BaseFlow { }
public class Test : IClassFixture<BrowserFixture> { }
```

#### 2. 方法命名

```csharp
// ✅ 好的命名 - 清晰描述操作
public async Task LoginWithValidCredentialsAsync(string username, string password) { }
public async Task VerifyDashboardIsDisplayedAsync() { }
public async Task NavigateToUserSettingsAsync() { }

// ❌ 避免的命名 - 模糊不清
public async Task DoLoginAsync(string u, string p) { }
public async Task CheckAsync() { }
public async Task GoToAsync() { }
```

#### 3. 变量命名

```csharp
// ✅ 好的命名
private const string UsernameInputSelector = "#username";
private const string PasswordInputSelector = "#password";
private const string LoginButtonSelector = "#login-btn";

// ❌ 避免的命名
private const string Input1 = "#username";
private const string Input2 = "#password";
private const string Btn = "#login-btn";
```

## 🏗️ Page Object 设计

### 1. 单一职责原则

每个 Page Object 应该只负责一个页面或页面的一个逻辑区域。

```csharp
// ✅ 好的设计 - 职责单一
public class LoginPage : BasePageObjectWithPlaywright
{
    private const string UsernameSelector = "#username";
    private const string PasswordSelector = "#password";
    private const string LoginButtonSelector = "#login-btn";
    private const string ErrorMessageSelector = ".error-message";
    
    public LoginPage(IPage page, ILogger logger) : base(page, logger) { }
    
    // 只包含登录页面相关的操作
    public async Task EnterUsernameAsync(string username) { }
    public async Task EnterPasswordAsync(string password) { }
    public async Task ClickLoginButtonAsync() { }
    public async Task<string> GetErrorMessageAsync() { }
    public async Task<bool> IsLoginFormVisibleAsync() { }
}

// ❌ 避免的设计 - 职责过多
public class ApplicationPage : BasePageObjectWithPlaywright
{
    // 包含了登录、仪表板、设置等多个页面的操作
    public async Task LoginAsync() { }
    public async Task NavigateToDashboardAsync() { }
    public async Task UpdateUserSettingsAsync() { }
    public async Task GenerateReportAsync() { }
}
```

### 2. 封装页面操作

将复杂的页面操作封装成有意义的方法。

```csharp
public class ProductSearchPage : BasePageObjectWithPlaywright
{
    private const string SearchInputSelector = "#search-input";
    private const string SearchButtonSelector = "#search-btn";
    private const string FilterDropdownSelector = "#filter-dropdown";
    private const string ResultsContainerSelector = ".search-results";
    
    public ProductSearchPage(IPage page, ILogger logger) : base(page, logger) { }
    
    /// <summary>
    /// 执行产品搜索
    /// </summary>
    /// <param name="searchTerm">搜索关键词</param>
    /// <param name="category">产品分类</param>
    public async Task SearchProductsAsync(string searchTerm, string category = null)
    {
        Logger.LogInformation($"搜索产品: {searchTerm}, 分类: {category}");
        
        // 输入搜索关键词
        await ClearAndTypeAsync(SearchInputSelector, searchTerm);
        
        // 选择分类（如果提供）
        if (!string.IsNullOrEmpty(category))
        {
            await SelectCategoryAsync(category);
        }
        
        // 点击搜索按钮
        await ClickAsync(SearchButtonSelector);
        
        // 等待结果加载
        await WaitForElementAsync(ResultsContainerSelector);
        
        Logger.LogInformation("产品搜索完成");
    }
    
    /// <summary>
    /// 获取搜索结果数量
    /// </summary>
    public async Task<int> GetSearchResultCountAsync()
    {
        var results = await _page.QuerySelectorAllAsync(".search-result-item");
        return results.Count;
    }
    
    /// <summary>
    /// 获取搜索结果列表
    /// </summary>
    public async Task<List<ProductInfo>> GetSearchResultsAsync()
    {
        var results = new List<ProductInfo>();
        var resultElements = await _page.QuerySelectorAllAsync(".search-result-item");
        
        foreach (var element in resultElements)
        {
            var name = await element.QuerySelectorAsync(".product-name")?.InnerTextAsync();
            var price = await element.QuerySelectorAsync(".product-price")?.InnerTextAsync();
            
            results.Add(new ProductInfo { Name = name, Price = price });
        }
        
        return results;
    }
    
    private async Task SelectCategoryAsync(string category)
    {
        await ClickAsync(FilterDropdownSelector);
        await ClickAsync($"text={category}");
    }
    
    public override async Task<bool> IsLoadedAsync()
    {
        return await IsElementExistAsync(SearchInputSelector) && 
               await IsElementExistAsync(SearchButtonSelector);
    }
    
    public override async Task WaitForLoadAsync(int timeoutMs = 30000)
    {
        await WaitForElementAsync(SearchInputSelector, timeoutMs);
    }
}

public class ProductInfo
{
    public string Name { get; set; }
    public string Price { get; set; }
}
```

### 3. 使用页面组件

对于可复用的页面区域，创建独立的组件类。

```csharp
// 导航栏组件
public class NavigationComponent : BasePageObjectWithPlaywright
{
    private const string HomeMenuSelector = "#nav-home";
    private const string ProductsMenuSelector = "#nav-products";
    private const string AccountMenuSelector = "#nav-account";
    private const string LogoutButtonSelector = "#logout-btn";
    
    public NavigationComponent(IPage page, ILogger logger) : base(page, logger) { }
    
    public async Task NavigateToHomeAsync()
    {
        await ClickAsync(HomeMenuSelector);
    }
    
    public async Task NavigateToProductsAsync()
    {
        await ClickAsync(ProductsMenuSelector);
    }
    
    public async Task NavigateToAccountAsync()
    {
        await ClickAsync(AccountMenuSelector);
    }
    
    public async Task LogoutAsync()
    {
        await ClickAsync(AccountMenuSelector);
        await ClickAsync(LogoutButtonSelector);
    }
    
    public override async Task<bool> IsLoadedAsync()
    {
        return await IsElementExistAsync(HomeMenuSelector);
    }
    
    public override async Task WaitForLoadAsync(int timeoutMs = 30000)
    {
        await WaitForElementAsync(HomeMenuSelector, timeoutMs);
    }
}

// 在页面中使用组件
public class DashboardPage : BasePageObjectWithPlaywright
{
    private readonly NavigationComponent _navigation;
    
    public DashboardPage(IPage page, ILogger logger) : base(page, logger)
    {
        _navigation = new NavigationComponent(page, logger);
    }
    
    public NavigationComponent Navigation => _navigation;
    
    // 页面特有的操作
    public async Task ViewRecentOrdersAsync() { }
    public async Task CheckNotificationsAsync() { }
}
```

## 🔄 Flow 业务流程

### 1. 流程设计原则

每个 Flow 应该代表一个完整的业务操作，不包含断言逻辑。

```csharp
// ✅ 好的 Flow 设计
public class UserRegistrationFlow : BaseFlow
{
    private readonly RegistrationPage _registrationPage;
    private readonly EmailVerificationPage _emailPage;
    private readonly WelcomePage _welcomePage;
    
    public UserRegistrationFlow(
        RegistrationPage registrationPage,
        EmailVerificationPage emailPage,
        WelcomePage welcomePage,
        ILogger logger) : base(logger)
    {
        _registrationPage = registrationPage;
        _emailPage = emailPage;
        _welcomePage = welcomePage;
    }
    
    public override async Task ExecuteAsync(Dictionary<string, object> parameters = null)
    {
        // 验证必需参数
        ValidateParameters(parameters, "email", "password", "firstName", "lastName");
        
        var email = GetParameter<string>(parameters, "email");
        var password = GetParameter<string>(parameters, "password");
        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetParameter<string>(parameters, "lastName");
        
        Logger.LogInformation($"开始用户注册流程: {email}");
        
        // 步骤1：填写注册表单
        await _registrationPage.FillRegistrationFormAsync(email, password, firstName, lastName);
        await _registrationPage.SubmitRegistrationAsync();
        
        // 步骤2：验证邮箱
        await _emailPage.WaitForLoadAsync();
        var verificationCode = GetParameter<string>(parameters, "verificationCode");
        if (!string.IsNullOrEmpty(verificationCode))
        {
            await _emailPage.EnterVerificationCodeAsync(verificationCode);
            await _emailPage.VerifyEmailAsync();
        }
        
        // 步骤3：确认欢迎页面
        await _welcomePage.WaitForLoadAsync();
        
        Logger.LogInformation("用户注册流程完成");
    }
}

// ❌ 避免的 Flow 设计 - 包含断言
public class BadRegistrationFlow : BaseFlow
{
    public override async Task ExecuteAsync(Dictionary<string, object> parameters = null)
    {
        // ... 执行注册操作 ...
        
        // ❌ Flow 中不应该包含断言
        var welcomeMessage = await _welcomePage.GetWelcomeMessageAsync();
        Assert.Contains("欢迎", welcomeMessage);
    }
}
```

### 2. 流程参数管理

使用强类型参数对象来管理复杂的流程参数。

```csharp
// 定义参数类
public class UserRegistrationParameters
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string VerificationCode { get; set; }
    public bool AcceptTerms { get; set; } = true;
    public bool SubscribeNewsletter { get; set; } = false;
}

// 在 Flow 中使用强类型参数
public class UserRegistrationFlow : BaseFlow
{
    public async Task ExecuteAsync(UserRegistrationParameters parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));
        
        Logger.LogInformation($"开始用户注册流程: {parameters.Email}");
        
        await _registrationPage.FillRegistrationFormAsync(parameters);
        await _registrationPage.SubmitRegistrationAsync();
        
        if (!string.IsNullOrEmpty(parameters.VerificationCode))
        {
            await _emailPage.EnterVerificationCodeAsync(parameters.VerificationCode);
            await _emailPage.VerifyEmailAsync();
        }
        
        Logger.LogInformation("用户注册流程完成");
    }
    
    // 保持向后兼容的字典接口
    public override async Task ExecuteAsync(Dictionary<string, object> parameters = null)
    {
        var typedParameters = ConvertToTypedParameters(parameters);
        await ExecuteAsync(typedParameters);
    }
    
    private UserRegistrationParameters ConvertToTypedParameters(Dictionary<string, object> parameters)
    {
        return new UserRegistrationParameters
        {
            Email = GetParameter<string>(parameters, "email"),
            Password = GetParameter<string>(parameters, "password"),
            FirstName = GetParameter<string>(parameters, "firstName"),
            LastName = GetParameter<string>(parameters, "lastName"),
            PhoneNumber = GetParameter<string>(parameters, "phoneNumber"),
            VerificationCode = GetParameter<string>(parameters, "verificationCode"),
            AcceptTerms = GetParameter<bool>(parameters, "acceptTerms", true),
            SubscribeNewsletter = GetParameter<bool>(parameters, "subscribeNewsletter", false)
        };
    }
}
```

### 3. 流程组合

复杂的业务场景可以通过组合多个简单的 Flow 来实现。

```csharp
public class CompleteUserOnboardingFlow : BaseFlow
{
    private readonly UserRegistrationFlow _registrationFlow;
    private readonly ProfileSetupFlow _profileSetupFlow;
    private readonly PreferencesConfigurationFlow _preferencesFlow;
    
    public CompleteUserOnboardingFlow(
        UserRegistrationFlow registrationFlow,
        ProfileSetupFlow profileSetupFlow,
        PreferencesConfigurationFlow preferencesFlow,
        ILogger logger) : base(logger)
    {
        _registrationFlow = registrationFlow;
        _profileSetupFlow = profileSetupFlow;
        _preferencesFlow = preferencesFlow;
    }
    
    public override async Task ExecuteAsync(Dictionary<string, object> parameters = null)
    {
        Logger.LogInformation("开始完整的用户入职流程");
        
        try
        {
            // 步骤1：用户注册
            await _registrationFlow.ExecuteAsync(parameters);
            
            // 步骤2：设置个人资料
            await _profileSetupFlow.ExecuteAsync(parameters);
            
            // 步骤3：配置偏好设置
            await _preferencesFlow.ExecuteAsync(parameters);
            
            Logger.LogInformation("用户入职流程完成");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "用户入职流程失败");
            throw;
        }
    }
}
```

## ✅ 测试用例编写

### 1. AAA 模式

使用 Arrange-Act-Assert 模式组织测试代码。

```csharp
[UITest]
[TestPriority(TestPriority.High)]
public class LoginTests : IClassFixture<BrowserFixture>
{
    private readonly BrowserFixture _fixture;
    private readonly LoginPage _loginPage;
    private readonly DashboardPage _dashboardPage;
    private readonly LoginFlow _loginFlow;
    
    public LoginTests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _loginPage = new LoginPage(_fixture.Page, _fixture.Logger);
        _dashboardPage = new DashboardPage(_fixture.Page, _fixture.Logger);
        _loginFlow = new LoginFlow(_loginPage, _dashboardPage, _fixture.Logger);
    }
    
    [Theory]
    [CsvData("TestData/valid_login_data.csv")]
    public async Task Login_WithValidCredentials_ShouldSucceed(LoginTestData data)
    {
        // Arrange - 准备测试数据和环境
        await _loginPage.NavigateAsync(data.BaseUrl);
        await _loginPage.WaitForLoadAsync();
        
        var loginParameters = new Dictionary<string, object>
        {
            ["username"] = data.Username,
            ["password"] = data.Password
        };
        
        // Act - 执行被测试的操作
        await _loginFlow.ExecuteAsync(loginParameters);
        
        // Assert - 验证结果
        await _dashboardPage.WaitForLoadAsync();
        var isLoggedIn = await _dashboardPage.IsUserLoggedInAsync();
        Assert.True(isLoggedIn, "用户应该成功登录");
        
        var welcomeMessage = await _dashboardPage.GetWelcomeMessageAsync();
        Assert.Contains(data.ExpectedWelcomeText, welcomeMessage);
    }
    
    [Fact]
    [TestTag("NegativeTest")]
    public async Task Login_WithInvalidCredentials_ShouldShowError()
    {
        // Arrange
        await _loginPage.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _loginPage.WaitForLoadAsync();
        
        // Act
        await _loginPage.EnterUsernameAsync("invalid_user");
        await _loginPage.EnterPasswordAsync("wrong_password");
        await _loginPage.ClickLoginButtonAsync();
        
        // Assert
        var errorMessage = await _loginPage.GetErrorMessageAsync();
        Assert.NotEmpty(errorMessage);
        Assert.Contains("用户名或密码错误", errorMessage);
        
        // 确保没有跳转到仪表板
        var isOnLoginPage = await _loginPage.IsLoadedAsync();
        Assert.True(isOnLoginPage, "应该仍然在登录页面");
    }
}
```

### 2. 测试数据管理

使用数据驱动测试来提高测试覆盖率和可维护性。

```csharp
// 测试数据模型
public class LoginTestData
{
    public string TestName { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string BaseUrl { get; set; }
    public string ExpectedWelcomeText { get; set; }
    public bool ShouldSucceed { get; set; }
    public string ExpectedErrorMessage { get; set; }
}

// CSV 数据文件: TestData/login_scenarios.csv
/*
TestName,Username,Password,BaseUrl,ExpectedWelcomeText,ShouldSucceed,ExpectedErrorMessage
有效管理员登录,admin,admin123,https://app.example.com,欢迎管理员,true,
有效用户登录,user1,user123,https://app.example.com,欢迎用户,true,
无效用户名,invalid,password,https://app.example.com,,false,用户名不存在
无效密码,admin,wrongpass,https://app.example.com,,false,密码错误
空用户名,,password,https://app.example.com,,false,请输入用户名
空密码,admin,,https://app.example.com,,false,请输入密码
*/

// 使用数据驱动测试
[Theory]
[CsvData("TestData/login_scenarios.csv")]
public async Task Login_VariousScenarios_ShouldBehaveCorrectly(LoginTestData data)
{
    // Arrange
    await _loginPage.NavigateAsync(data.BaseUrl);
    await _loginPage.WaitForLoadAsync();
    
    // Act
    if (!string.IsNullOrEmpty(data.Username))
        await _loginPage.EnterUsernameAsync(data.Username);
    
    if (!string.IsNullOrEmpty(data.Password))
        await _loginPage.EnterPasswordAsync(data.Password);
    
    await _loginPage.ClickLoginButtonAsync();
    
    // Assert
    if (data.ShouldSucceed)
    {
        await _dashboardPage.WaitForLoadAsync();
        var welcomeMessage = await _dashboardPage.GetWelcomeMessageAsync();
        Assert.Contains(data.ExpectedWelcomeText, welcomeMessage);
    }
    else
    {
        var errorMessage = await _loginPage.GetErrorMessageAsync();
        Assert.Contains(data.ExpectedErrorMessage, errorMessage);
    }
}
```

### 3. 测试分类和标记

合理使用测试分类来组织和执行测试。

```csharp
[UITest]
[TestCategory(TestCategory.PageObject)]
[TestPriority(TestPriority.Critical)]
[SmokeTest]
public class CriticalUserJourneyTests : IClassFixture<BrowserFixture>
{
    [Fact]
    [TestTag("Authentication")]
    [FastTest]
    public async Task UserCanLoginSuccessfully()
    {
        // 关键用户路径测试
    }
    
    [Fact]
    [TestTag("Navigation")]
    [TestEnvironment("Production")]
    public async Task UserCanNavigateToMainFeatures()
    {
        // 导航测试
    }
}

[UITest]
[TestCategory(TestCategory.PageObject)]
[TestPriority(TestPriority.Medium)]
[RegressionTest]
public class DetailedFeatureTests : IClassFixture<BrowserFixture>
{
    [Theory]
    [JsonData("TestData/feature_test_data.json")]
    [TestTag("FeatureValidation")]
    [SlowTest]
    public async Task FeatureWorksWithVariousInputs(FeatureTestData data)
    {
        // 详细功能测试
    }
}
```

## 📊 数据管理

### 1. 测试数据分层

将测试数据按照不同的层次进行组织。

```
TestData/
├── Common/                    # 通用测试数据
│   ├── users.csv             # 用户数据
│   ├── products.json         # 产品数据
│   └── configurations.yaml   # 配置数据
├── UI/                       # UI 测试数据
│   ├── login_scenarios.csv   # 登录场景
│   ├── search_data.csv       # 搜索数据
│   └── form_validation.json  # 表单验证数据
├── API/                      # API 测试数据
│   ├── request_payloads.json # 请求负载
│   ├── response_schemas.json # 响应模式
│   └── error_scenarios.csv   # 错误场景
└── Integration/              # 集成测试数据
    ├── workflow_data.json    # 工作流数据
    └── end_to_end.csv        # 端到端测试数据
```

### 2. 数据生成策略

对于复杂的测试数据，使用数据生成器。

```csharp
public class TestDataGenerator
{
    private readonly Random _random = new();
    
    /// <summary>
    /// 生成随机用户数据
    /// </summary>
    public UserTestData GenerateRandomUser()
    {
        return new UserTestData
        {
            Username = $"user_{_random.Next(1000, 9999)}",
            Email = $"test{_random.Next(1000, 9999)}@example.com",
            FirstName = GenerateRandomName(),
            LastName = GenerateRandomName(),
            Password = GenerateRandomPassword(),
            DateOfBirth = GenerateRandomDate(),
            PhoneNumber = GenerateRandomPhoneNumber()
        };
    }
    
    /// <summary>
    /// 生成特定场景的用户数据
    /// </summary>
    public UserTestData GenerateUserForScenario(string scenario)
    {
        return scenario.ToLower() switch
        {
            "admin" => new UserTestData
            {
                Username = "admin_user",
                Email = "admin@example.com",
                Role = "Administrator",
                Permissions = new[] { "read", "write", "delete", "admin" }
            },
            "readonly" => new UserTestData
            {
                Username = "readonly_user",
                Email = "readonly@example.com",
                Role = "ReadOnly",
                Permissions = new[] { "read" }
            },
            "guest" => new UserTestData
            {
                Username = "guest_user",
                Email = "guest@example.com",
                Role = "Guest",
                Permissions = new string[0]
            },
            _ => GenerateRandomUser()
        };
    }
    
    private string GenerateRandomName()
    {
        var names = new[] { "张三", "李四", "王五", "赵六", "钱七", "孙八", "周九", "吴十" };
        return names[_random.Next(names.Length)];
    }
    
    private string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
    
    private DateTime GenerateRandomDate()
    {
        var start = new DateTime(1980, 1, 1);
        var range = (DateTime.Today.AddYears(-18) - start).Days;
        return start.AddDays(_random.Next(range));
    }
    
    private string GenerateRandomPhoneNumber()
    {
        return $"1{_random.Next(3, 9)}{_random.Next(100000000, 999999999)}";
    }
}

// 在测试中使用数据生成器
[Fact]
public async Task UserRegistration_WithGeneratedData_ShouldSucceed()
{
    // Arrange
    var dataGenerator = new TestDataGenerator();
    var userData = dataGenerator.GenerateRandomUser();
    
    // Act
    await _registrationFlow.ExecuteAsync(new Dictionary<string, object>
    {
        ["email"] = userData.Email,
        ["password"] = userData.Password,
        ["firstName"] = userData.FirstName,
        ["lastName"] = userData.LastName
    });
    
    // Assert
    var isRegistered = await _welcomePage.IsUserRegisteredAsync();
    Assert.True(isRegistered);
}
```

### 3. 环境特定数据

为不同环境准备不同的测试数据。

```csharp
public class EnvironmentDataProvider
{
    private readonly TestConfiguration _configuration;
    
    public EnvironmentDataProvider(TestConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    /// <summary>
    /// 获取环境特定的测试用户
    /// </summary>
    public UserTestData GetTestUser(string userType = "standard")
    {
        var environment = _configuration.Environment.Name.ToLower();
        
        return environment switch
        {
            "development" => GetDevelopmentUser(userType),
            "test" => GetTestUser(userType),
            "staging" => GetStagingUser(userType),
            "production" => GetProductionUser(userType),
            _ => throw new ArgumentException($"未知环境: {environment}")
        };
    }
    
    private UserTestData GetDevelopmentUser(string userType)
    {
        return userType switch
        {
            "admin" => new UserTestData { Username = "dev_admin", Password = "dev_pass123" },
            "standard" => new UserTestData { Username = "dev_user", Password = "dev_pass123" },
            _ => new UserTestData { Username = "dev_guest", Password = "dev_pass123" }
        };
    }
    
    private UserTestData GetTestUser(string userType)
    {
        return userType switch
        {
            "admin" => new UserTestData { Username = "test_admin", Password = "test_pass123" },
            "standard" => new UserTestData { Username = "test_user", Password = "test_pass123" },
            _ => new UserTestData { Username = "test_guest", Password = "test_pass123" }
        };
    }
    
    private UserTestData GetStagingUser(string userType)
    {
        // 使用更接近生产环境的数据
        return userType switch
        {
            "admin" => new UserTestData { Username = "staging_admin", Password = "StrongPass123!" },
            "standard" => new UserTestData { Username = "staging_user", Password = "StrongPass123!" },
            _ => new UserTestData { Username = "staging_guest", Password = "StrongPass123!" }
        };
    }
    
    private UserTestData GetProductionUser(string userType)
    {
        // 生产环境使用专门的测试账户
        return userType switch
        {
            "admin" => new UserTestData { Username = "prod_test_admin", Password = Environment.GetEnvironmentVariable("PROD_ADMIN_PASSWORD") },
            "standard" => new UserTestData { Username = "prod_test_user", Password = Environment.GetEnvironmentVariable("PROD_USER_PASSWORD") },
            _ => throw new InvalidOperationException("生产环境不支持访客用户测试")
        };
    }
}
```

## ⚠️ 错误处理

### 1. 异常处理策略

实现分层的异常处理机制。

```csharp
public class RobustLoginPage : BasePageObjectWithPlaywright
{
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMs = 1000;
    
    public RobustLoginPage(IPage page, ILogger logger) : base(page, logger) { }
    
    /// <summary>
    /// 带重试机制的登录操作
    /// </summary>
    public async Task LoginWithRetryAsync(string username, string password)
    {
        var retryPolicy = new RetryPolicy
        {
            MaxAttempts = MaxRetryAttempts,
            DelayBetweenAttempts = TimeSpan.FromMilliseconds(RetryDelayMs),
            RetryableExceptions = new List<Type>
            {
                typeof(ElementNotFoundException),
                typeof(TimeoutException),
                typeof(PlaywrightException)
            }
        };
        
        var retryExecutor = new RetryExecutor(retryPolicy, Logger);
        
        await retryExecutor.ExecuteAsync(async () =>
        {
            await EnterUsernameAsync(username);
            await EnterPasswordAsync(password);
            await ClickLoginButtonAsync();
        }, "登录操作");
    }
    
    /// <summary>
    /// 安全的元素操作
    /// </summary>
    public async Task<bool> SafeClickAsync(string selector, int timeoutMs = 5000)
    {
        try
        {
            await WaitForElementAsync(selector, timeoutMs);
            await ClickAsync(selector);
            return true;
        }
        catch (ElementNotFoundException ex)
        {
            Logger.LogWarning($"元素未找到，无法点击: {selector}, 错误: {ex.Message}");
            return false;
        }
        catch (TimeoutException ex)
        {
            Logger.LogWarning($"元素等待超时: {selector}, 错误: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"点击元素时发生未知错误: {selector}");
            throw;
        }
    }
    
    /// <summary>
    /// 带回退策略的文本输入
    /// </summary>
    public async Task TypeWithFallbackAsync(string selector, string text)
    {
        try
        {
            // 首先尝试标准输入
            await TypeAsync(selector, text);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"标准输入失败，尝试 JavaScript 输入: {ex.Message}");
            
            try
            {
                // 回退到 JavaScript 输入
                await _page.EvaluateAsync($@"
                    document.querySelector('{selector}').value = '{text}';
                    document.querySelector('{selector}').dispatchEvent(new Event('input', {{ bubbles: true }}));
                ");
            }
            catch (Exception jsEx)
            {
                Logger.LogError(jsEx, $"JavaScript 输入也失败: {selector}");
                throw new ElementNotFoundException("", selector, $"无法向元素输入文本: {selector}");
            }
        }
    }
}
```

### 2. 测试失败恢复

实现测试失败时的自动恢复机制。

```csharp
public class ResilientTestBase : IClassFixture<BrowserFixture>
{
    protected readonly BrowserFixture _fixture;
    protected readonly ILogger _logger;
    
    public ResilientTestBase(BrowserFixture fixture)
    {
        _fixture = fixture;
        _logger = fixture.Logger;
    }
    
    /// <summary>
    /// 执行带恢复机制的测试操作
    /// </summary>
    protected async Task<T> ExecuteWithRecoveryAsync<T>(
        Func<Task<T>> operation,
        Func<Task> recoveryAction = null,
        string operationName = "测试操作")
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"{operationName} 失败，尝试恢复: {ex.Message}");
            
            // 截图记录失败状态
            await TakeFailureScreenshotAsync(operationName);
            
            // 执行恢复操作
            if (recoveryAction != null)
            {
                try
                {
                    await recoveryAction();
                    _logger.LogInformation($"{operationName} 恢复成功，重新尝试");
                    
                    // 重新执行操作
                    return await operation();
                }
                catch (Exception recoveryEx)
                {
                    _logger.LogError(recoveryEx, $"{operationName} 恢复失败");
                }
            }
            
            // 如果恢复失败，重新抛出原始异常
            throw;
        }
    }
    
    /// <summary>
    /// 页面刷新恢复策略
    /// </summary>
    protected async Task RefreshPageRecoveryAsync()
    {
        _logger.LogInformation("执行页面刷新恢复");
        await _fixture.Page.ReloadAsync();
        await Task.Delay(2000); // 等待页面加载
    }
    
    /// <summary>
    /// 浏览器重启恢复策略
    /// </summary>
    protected async Task RestartBrowserRecoveryAsync()
    {
        _logger.LogInformation("执行浏览器重启恢复");
        
        // 关闭当前浏览器
        await _fixture.Context.CloseAsync();
        
        // 创建新的浏览器上下文
        var newContext = await _fixture.Browser.NewContextAsync();
        var newPage = await newContext.NewPageAsync();
        
        // 更新 fixture 中的实例（这需要 fixture 支持）
        // _fixture.UpdateContext(newContext, newPage);
    }
    
    /// <summary>
    /// 截取失败截图
    /// </summary>
    private async Task TakeFailureScreenshotAsync(string operationName)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"failure_{operationName}_{timestamp}.png";
            await _fixture.Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine("src/conclusion/screenshots", fileName),
                FullPage = true
            });
            _logger.LogInformation($"失败截图已保存: {fileName}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"保存失败截图时出错: {ex.Message}");
        }
    }
}

// 在测试中使用恢复机制
public class LoginTestsWithRecovery : ResilientTestBase
{
    public LoginTestsWithRecovery(BrowserFixture fixture) : base(fixture) { }
    
    [Fact]
    public async Task Login_WithRecovery_ShouldSucceed()
    {
        var loginPage = new RobustLoginPage(_fixture.Page, _logger);
        
        // 带恢复机制的导航
        await ExecuteWithRecoveryAsync(
            operation: async () =>
            {
                await loginPage.NavigateAsync("https://example.com/login");
                await loginPage.WaitForLoadAsync();
                return true;
            },
            recoveryAction: RefreshPageRecoveryAsync,
            operationName: "页面导航"
        );
        
        // 带恢复机制的登录
        await ExecuteWithRecoveryAsync(
            operation: async () =>
            {
                await loginPage.LoginWithRetryAsync("testuser", "password123");
                return true;
            },
            recoveryAction: async () =>
            {
                await RefreshPageRecoveryAsync();
                await loginPage.WaitForLoadAsync();
            },
            operationName: "用户登录"
        );
    }
}
```

## 🚀 性能优化

### 1. 并行执行优化

合理配置并行执行以提高测试效率。

```csharp
// 在 xunit.runner.json 中配置并行执行
{
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4
}

// 对于需要串行执行的测试，使用 Collection
[Collection("SerialTests")]
public class DatabaseTests : IClassFixture<DatabaseFixture>
{
    // 这些测试将串行执行
}

[CollectionDefinition("SerialTests", DisableParallelization = true)]
public class SerialTestsCollection : ICollectionFixture<DatabaseFixture>
{
    // 定义串行测试集合
}
```

### 2. 资源管理优化

优化浏览器和页面资源的使用。

```csharp
public class OptimizedBrowserFixture : IAsyncLifetime
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private readonly List<IBrowserContext> _contexts = new();
    private readonly SemaphoreSlim _contextSemaphore;
    
    public OptimizedBrowserFixture()
    {
        // 限制并发上下文数量
        _contextSemaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
    }
    
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        
        // 使用连接池模式
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[]
            {
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--disable-gpu",
                "--disable-extensions",
                "--disable-background-timer-throttling",
                "--disable-backgrounding-occluded-windows",
                "--disable-renderer-backgrounding"
            }
        });
    }
    
    /// <summary>
    /// 获取优化的浏览器上下文
    /// </summary>
    public async Task<IBrowserContext> GetOptimizedContextAsync()
    {
        await _contextSemaphore.WaitAsync();
        
        try
        {
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                // 禁用图片加载以提高速度
                JavaScriptEnabled = true,
                // 设置较短的超时时间
                Timeout = 30000
            });
            
            // 禁用不必要的资源加载
            await context.RouteAsync("**/*.{png,jpg,jpeg,gif,svg,ico,woff,woff2}", route => route.AbortAsync());
            
            _contexts.Add(context);
            return context;
        }
        finally
        {
            _contextSemaphore.Release();
        }
    }
    
    public async Task DisposeAsync()
    {
        // 并行关闭所有上下文
        var closeTasks = _contexts.Select(context => context.CloseAsync());
        await Task.WhenAll(closeTasks);
        
        await _browser?.CloseAsync();
        _playwright?.Dispose();
        _contextSemaphore?.Dispose();
    }
}
```

### 3. 等待策略优化

使用智能等待策略减少不必要的等待时间。

```csharp
public class SmartWaitPage : BasePageObjectWithPlaywright
{
    public SmartWaitPage(IPage page, ILogger logger) : base(page, logger) { }
    
    /// <summary>
    /// 智能等待元素可见
    /// </summary>
    public async Task<bool> SmartWaitForElementAsync(string selector, int timeoutMs = 10000)
    {
        var startTime = DateTime.Now;
        var checkInterval = 100; // 100ms 检查间隔
        
        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
        {
            try
            {
                // 检查元素是否存在且可见
                var element = await _page.QuerySelectorAsync(selector);
                if (element != null)
                {
                    var isVisible = await element.IsVisibleAsync();
                    if (isVisible)
                    {
                        Logger.LogDebug($"元素在 {(DateTime.Now - startTime).TotalMilliseconds}ms 后变为可见: {selector}");
                        return true;
                    }
                }
                
                // 动态调整检查间隔
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                if (elapsed > timeoutMs * 0.8) // 超过80%时间时，增加检查频率
                {
                    checkInterval = 50;
                }
                
                await Task.Delay(checkInterval);
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"等待元素时出现异常: {ex.Message}");
                await Task.Delay(checkInterval);
            }
        }
        
        Logger.LogWarning($"元素在 {timeoutMs}ms 内未变为可见: {selector}");
        return false;
    }
    
    /// <summary>
    /// 等待页面稳定（网络空闲）
    /// </summary>
    public async Task WaitForPageStableAsync(int networkIdleTimeMs = 500)
    {
        try
        {
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
            {
                Timeout = 30000
            });
            
            // 额外等待确保页面完全稳定
            await Task.Delay(networkIdleTimeMs);
            
            Logger.LogDebug("页面已稳定");
        }
        catch (TimeoutException)
        {
            Logger.LogWarning("等待页面稳定超时，继续执行");
        }
    }
    
    /// <summary>
    /// 条件等待
    /// </summary>
    public async Task<T> WaitForConditionAsync<T>(
        Func<Task<T>> condition,
        Func<T, bool> predicate,
        int timeoutMs = 10000,
        int intervalMs = 500)
    {
        var startTime = DateTime.Now;
        
        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
        {
            try
            {
                var result = await condition();
                if (predicate(result))
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"条件检查异常: {ex.Message}");
            }
            
            await Task.Delay(intervalMs);
        }
        
        throw new TimeoutException($"条件在 {timeoutMs}ms 内未满足");
    }
}
```

## 📏 代码质量

### 1. 代码审查检查清单

建立代码审查标准：

```markdown
## 代码审查检查清单

### 基本要求
- [ ] 代码遵循命名约定
- [ ] 方法长度不超过 50 行
- [ ] 类长度不超过 500 行
- [ ] 圈复杂度不超过 10
- [ ] 没有重复代码

### Page Object 检查
- [ ] 每个 Page Object 职责单一
- [ ] 选择器定义为常量
- [ ] 实现了 IsLoadedAsync 和 WaitForLoadAsync 方法
- [ ] 包含适当的日志记录
- [ ] 异常处理得当

### Flow 检查
- [ ] Flow 不包含断言逻辑
- [ ] 参数验证完整
- [ ] 包含详细的日志记录
- [ ] 错误处理适当

### 测试检查
- [ ] 使用 AAA 模式组织
- [ ] 测试名称清晰描述测试意图
- [ ] 包含适当的测试标记
- [ ] 断言明确且有意义
- [ ] 测试数据外部化

### 性能检查
- [ ] 避免不必要的等待
- [ ] 合理使用并行执行
- [ ] 资源正确释放
- [ ] 没有内存泄漏风险
```

### 2. 静态代码分析

使用工具进行自动化代码质量检查：

```xml
<!-- 在 .csproj 文件中添加代码分析包 -->
<PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
<PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="7.0.0" PrivateAssets="all" />
<PackageReference Include="SonarAnalyzer.CSharp" Version="8.56.0.67649" PrivateAssets="all" />

<!-- 启用代码分析 -->
<PropertyGroup>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisLevel>latest</AnalysisLevel>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
</PropertyGroup>
```

### 3. 单元测试覆盖率

确保核心组件有足够的单元测试覆盖率：

```csharp
[TestClass]
public class CsvDataReaderTests
{
    private readonly CsvDataReader _csvReader;
    private readonly ILogger _logger;
    
    public CsvDataReaderTests()
    {
        _logger = new Mock<ILogger>().Object;
        _csvReader = new CsvDataReader(_logger);
    }
    
    [TestMethod]
    public void ReadData_WithValidFile_ShouldReturnData()
    {
        // Arrange
        var testData = "Name,Age,Email\nJohn,25,john@example.com\nJane,30,jane@example.com";
        var filePath = CreateTempCsvFile(testData);
        
        // Act
        var result = _csvReader.ReadData<TestUser>(filePath).ToList();
        
        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("John", result[0].Name);
        Assert.AreEqual(25, result[0].Age);
        Assert.AreEqual("john@example.com", result[0].Email);
    }
    
    [TestMethod]
    [ExpectedException(typeof(FileNotFoundException))]
    public void ReadData_WithNonExistentFile_ShouldThrowException()
    {
        // Act & Assert
        _csvReader.ReadData<TestUser>("nonexistent.csv").ToList();
    }
    
    private string CreateTempCsvFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        return tempFile;
    }
}

public class TestUser
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}
```

## 🔄 CI/CD 集成

### 1. GitHub Actions 配置

```yaml
# .github/workflows/test.yml
name: Automated Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    strategy:
      matrix:
        test-type: [unit, api, ui-smoke, ui-regression]
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '6.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Install Playwright
      run: |
        dotnet build
        pwsh bin/Debug/net6.0/playwright.ps1 install --with-deps
    
    - name: Run Unit Tests
      if: matrix.test-type == 'unit'
      run: dotnet test --filter "Type=Unit" --logger trx --results-directory TestResults
    
    - name: Run API Tests
      if: matrix.test-type == 'api'
      run: dotnet test --filter "Type=API" --logger trx --results-directory TestResults
    
    - name: Run UI Smoke Tests
      if: matrix.test-type == 'ui-smoke'
      run: dotnet test --filter "Type=UI&Suite=Smoke" --logger trx --results-directory TestResults
    
    - name: Run UI Regression Tests
      if: matrix.test-type == 'ui-regression'
      run: dotnet test --filter "Type=UI&Suite=Regression" --logger trx --results-directory TestResults
    
    - name: Upload Test Results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results-${{ matrix.test-type }}
        path: TestResults/
    
    - name: Upload Screenshots
      uses: actions/upload-artifact@v3
      if: failure()
      with:
        name: screenshots-${{ matrix.test-type }}
        path: src/conclusion/screenshots/
    
    - name: Publish Test Report
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: Test Results (${{ matrix.test-type }})
        path: TestResults/*.trx
        reporter: dotnet-trx
```

### 2. Azure DevOps 管道

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
    - main
    - develop

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

stages:
- stage: Build
  jobs:
  - job: Build
    steps:
    - task: UseDotNet@2
      inputs:
        packageType: 'sdk'
        version: '6.0.x'
    
    - task: DotNetCoreCLI@2
      displayName: 'Restore packages'
      inputs:
        command: 'restore'
        projects: '**/*.csproj'
    
    - task: DotNetCoreCLI@2
      displayName: 'Build project'
      inputs:
        command: 'build'
        projects: '**/*.csproj'
        arguments: '--configuration $(buildConfiguration)'

- stage: Test
  dependsOn: Build
  jobs:
  - job: UnitTests
    displayName: 'Unit Tests'
    steps:
    - template: test-template.yml
      parameters:
        testFilter: 'Type=Unit'
        displayName: 'Unit Tests'
  
  - job: APITests
    displayName: 'API Tests'
    steps:
    - template: test-template.yml
      parameters:
        testFilter: 'Type=API'
        displayName: 'API Tests'
  
  - job: UITests
    displayName: 'UI Tests'
    steps:
    - script: |
        sudo apt-get update
        sudo apt-get install -y xvfb
      displayName: 'Install dependencies'
    
    - template: test-template.yml
      parameters:
        testFilter: 'Type=UI&Suite=Smoke'
        displayName: 'UI Smoke Tests'
        useXvfb: true

# test-template.yml
parameters:
- name: testFilter
  type: string
- name: displayName
  type: string
- name: useXvfb
  type: boolean
  default: false

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '6.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    arguments: '--configuration Release'

- script: |
    pwsh bin/Release/net6.0/playwright.ps1 install --with-deps
  displayName: 'Install Playwright browsers'

- task: DotNetCoreCLI@2
  displayName: '${{ parameters.displayName }}'
  inputs:
    command: 'test'
    arguments: '--filter "${{ parameters.testFilter }}" --logger trx --results-directory $(Agent.TempDirectory)/TestResults'
  ${{ if parameters.useXvfb }}:
    env:
      DISPLAY: ':99'
  continueOnError: true

- task: PublishTestResults@2
  displayName: 'Publish test results'
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '$(Agent.TempDirectory)/TestResults/*.trx'
    testRunTitle: '${{ parameters.displayName }}'
  condition: always()

- task: PublishBuildArtifacts@1
  displayName: 'Publish screenshots'
  inputs:
    pathToPublish: 'src/conclusion/screenshots'
    artifactName: 'screenshots-${{ parameters.displayName }}'
  condition: failed()
```

### 3. Docker 容器化

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# 复制项目文件
COPY *.csproj ./
RUN dotnet restore

# 复制源代码
COPY . ./
RUN dotnet build -c Release

# 安装 Playwright
RUN pwsh bin/Release/net6.0/playwright.ps1 install --with-deps

# 运行时镜像
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app

# 安装浏览器依赖
RUN apt-get update && apt-get install -y \
    libnss3 \
    libatk-bridge2.0-0 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    libgbm1 \
    libxss1 \
    libasound2 \
    && rm -rf /var/lib/apt/lists/*

# 复制构建结果
COPY --from=build /app/bin/Release/net6.0 ./

# 设置入口点
ENTRYPOINT ["dotnet", "CsPlaywrightXun.dll"]
```

```yaml
# docker-compose.yml
version: '3.8'

services:
  test-runner:
    build: .
    environment:
      - ASPNETCORE_ENVIRONMENT=Test
      - Browser__Headless=true
    volumes:
      - ./src/conclusion:/app/src/conclusion
    command: dotnet test --filter "Type=UI&Suite=Smoke"
  
  api-tests:
    build: .
    environment:
      - ASPNETCORE_ENVIRONMENT=Test
    command: dotnet test --filter "Type=API"
    depends_on:
      - test-api
  
  test-api:
    image: mockserver/mockserver:latest
    ports:
      - "1080:1080"
    environment:
      MOCKSERVER_INITIALIZATION_JSON_PATH: /config/mock-config.json
    volumes:
      - ./test-config:/config
```

通过遵循这些最佳实践，您可以构建出高质量、可维护、高效的自动化测试框架。记住，最佳实践是一个持续改进的过程，需要根据项目的具体需求和团队的经验不断调整和优化。