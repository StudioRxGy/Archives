# Playwright 基类使用指南

## 概述

`BasePageObjectWithPlaywright` 是一个 C# Playwright 封装基类，提供了与 Python Selenium 基类等价的功能。该基类封装了常用的页面操作方法，包括元素定位、输入、点击、断言、截图等功能。

## 主要特性

### 🚀 核心功能
- **页面导航**：打开网址、刷新页面、获取当前URL
- **元素操作**：点击、输入、清除、悬停、拖拽
- **等待机制**：显式等待、元素存在检查、强制等待
- **JavaScript执行**：执行自定义脚本、JS点击、滚动操作
- **截图功能**：普通截图、失败截图、断言截图
- **断言方法**：相等断言、不相等断言、文本包含断言
- **统计功能**：测试通过/失败计数

### 🎯 设计优势
- **类型安全**：完全的 C# 类型安全
- **异步支持**：所有操作都是异步的，性能更好
- **日志集成**：详细的操作日志记录
- **异常处理**：统一的异常处理和错误恢复
- **截图支持**：失败时自动截图
- **统计跟踪**：自动跟踪测试通过/失败次数

## 快速开始

### 1. 创建页面对象类

```csharp
public class MyPage : BasePageObjectWithPlaywright
{
    private const string LoginButtonSelector = "#login-btn";
    private const string UsernameInputSelector = "#username";
    
    public MyPage(IPage page, ILogger logger, YamlElementReader elementReader = null) 
        : base(page, logger, elementReader)
    {
    }
    
    public async Task LoginAsync(string username, string password)
    {
        await TypeAsync(UsernameInputSelector, username);
        await TypeAsync("#password", password);
        await ClickAsync(LoginButtonSelector);
    }
    
    // 实现抽象方法
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

### 2. 在测试中使用

```csharp
[Fact]
public async Task LoginTest()
{
    var myPage = new MyPage(_fixture.Page, _fixture.Logger);
    
    await myPage.NavigateAsync("https://example.com");
    await myPage.WaitForLoadAsync();
    await myPage.LoginAsync("testuser", "password123");
    
    var result = await myPage.AssertEqualAsync(await myPage.GetTitleAsync(), "Dashboard");
    Assert.Equal("pass", result);
}
```

## API 参考

### 导航和页面操作

| 方法 | 描述 | 示例 |
|------|------|------|
| `NavigateAsync(url)` | 导航到指定URL | `await NavigateAsync("https://example.com")` |
| `RefreshAsync()` | 刷新当前页面 | `await RefreshAsync()` |
| `GetCurrentUrl()` | 获取当前页面URL | `var url = GetCurrentUrl()` |
| `CloseAsync()` | 关闭当前页面 | `await CloseAsync()` |

### 元素定位和等待

| 方法 | 描述 | 示例 |
|------|------|------|
| `WaitForElementAsync(selector, timeout)` | 等待元素出现 | `await WaitForElementAsync("#button", 5000)` |
| `IsElementExistAsync(selector, timeout)` | 检查元素是否存在 | `var exists = await IsElementExistAsync("#element")` |
| `SleepAsync(seconds)` | 强制等待 | `await SleepAsync(2)` |

### 输入操作

| 方法 | 描述 | 示例 |
|------|------|------|
| `TypeAsync(selector, text)` | 输入文本 | `await TypeAsync("#input", "hello")` |
| `ClearAndTypeAsync(selector, text)` | 清除并输入文本 | `await ClearAndTypeAsync("#input", "new text")` |
| `TypeAndEnterAsync(selector, text, delay)` | 输入文本并按回车 | `await TypeAndEnterAsync("#search", "query")` |

### 点击操作

| 方法 | 描述 | 示例 |
|------|------|------|
| `ClickAsync(selector)` | 点击元素 | `await ClickAsync("#button")` |
| `RightClickAsync(selector)` | 右键点击 | `await RightClickAsync("#menu")` |
| `DoubleClickAsync(selector)` | 双击元素 | `await DoubleClickAsync("#item")` |
| `ClickLinkTextAsync(text)` | 点击链接文本 | `await ClickLinkTextAsync("登录")` |

### 鼠标操作

| 方法 | 描述 | 示例 |
|------|------|------|
| `HoverAsync(selector)` | 悬停到元素 | `await HoverAsync("#menu")` |
| `DragAndDropAsync(source, target)` | 拖拽元素 | `await DragAndDropAsync("#item", "#target")` |

### 获取元素信息

| 方法 | 描述 | 示例 |
|------|------|------|
| `GetTextAsync(selector)` | 获取元素文本 | `var text = await GetTextAsync("#title")` |
| `GetAttributeAsync(selector, attr)` | 获取元素属性 | `var href = await GetAttributeAsync("a", "href")` |
| `GetTitleAsync()` | 获取页面标题 | `var title = await GetTitleAsync()` |
| `GetUrl()` | 获取页面URL | `var url = GetUrl()` |

### JavaScript 执行

| 方法 | 描述 | 示例 |
|------|------|------|
| `ExecuteJavaScriptAsync(script)` | 执行JS脚本 | `await ExecuteJavaScriptAsync("alert('hello')")` |
| `ClickByJavaScriptAsync(selector)` | JS点击元素 | `await ClickByJavaScriptAsync("#button")` |
| `ScrollToAsync(x, y)` | 滚动到指定位置 | `await ScrollToAsync(0, 1000)` |

### 截图功能

| 方法 | 描述 | 示例 |
|------|------|------|
| `TakeScreenshotAsync(fileName)` | 截取屏幕截图 | `await TakeScreenshotAsync("test.png")` |

### 断言方法

| 方法 | 描述 | 示例 | 返回值 |
|------|------|------|-------|
| `AssertEqualAsync(actual, expected)` | 断言相等 | `await AssertEqualAsync(result, "expected")` | "pass"/"fail" |
| `AssertNotEqualAsync(actual, expected)` | 断言不相等 | `await AssertNotEqualAsync(result, "wrong")` | "pass"/"fail" |
| `IsTextInElementAsync(selector, text)` | 检查文本在元素中 | `await IsTextInElementAsync("#div", "hello")` | "pass"/"fail" |
| `IsTitleEqualAsync(title)` | 检查标题相等 | `await IsTitleEqualAsync("Home Page")` | "pass"/"fail" |
| `IsTitleContainsAsync(text)` | 检查标题包含文本 | `await IsTitleContainsAsync("Home")` | "pass"/"fail" |

### 统计功能

| 方法 | 描述 | 示例 |
|------|------|------|
| `GetPassCount()` | 获取通过测试数量 | `var passed = GetPassCount()` |
| `GetFailCount()` | 获取失败测试数量 | `var failed = GetFailCount()` |
| `ResetCounts()` | 重置统计计数 | `ResetCounts()` |

## 与 Python Selenium 基类的对比

| 功能 | Python Selenium | C# Playwright | 说明 |
|------|----------------|---------------|------|
| 页面导航 | `open_url(url)` | `NavigateAsync(url)` | 功能相同 |
| 元素点击 | `click(css)` | `ClickAsync(selector)` | 选择器格式略有不同 |
| 文本输入 | `text_input(css, text)` | `TypeAsync(selector, text)` | 功能相同 |
| 清除输入 | `clear_type(css, text)` | `ClearAndTypeAsync(selector, text)` | 功能相同 |
| 元素等待 | `_element_wait(css, secs)` | `WaitForElementAsync(selector, ms)` | 时间单位不同 |
| 获取文本 | `get_text(css)` | `GetTextAsync(selector)` | 异步版本 |
| 截图 | `take_nowpage_screenshot()` | `TakeScreenshotAsync()` | 异步版本 |
| 断言 | `assert_equal(loc, text)` | `AssertEqualAsync(actual, expected)` | 异步版本 |
| JS执行 | `js(script)` | `ExecuteJavaScriptAsync(script)` | 异步版本 |

## 最佳实践

### 1. 页面对象设计

```csharp
public class LoginPage : BasePageObjectWithPlaywright
{
    // 使用常量定义选择器
    private const string UsernameSelector = "#username";
    private const string PasswordSelector = "#password";
    private const string LoginButtonSelector = "#login-btn";
    
    public LoginPage(IPage page, ILogger logger) : base(page, logger) { }
    
    // 提供业务级别的方法
    public async Task LoginAsync(string username, string password)
    {
        await TypeAsync(UsernameSelector, username);
        await TypeAsync(PasswordSelector, password);
        await ClickAsync(LoginButtonSelector);
    }
    
    // 实现页面加载检查
    public override async Task<bool> IsLoadedAsync()
    {
        return await IsElementExistAsync(LoginButtonSelector) && 
               await IsElementExistAsync(UsernameSelector);
    }
    
    public override async Task WaitForLoadAsync(int timeoutMs = 30000)
    {
        await WaitForElementAsync(LoginButtonSelector, timeoutMs);
    }
}
```

### 2. 错误处理

```csharp
public async Task SafeOperationAsync()
{
    try
    {
        await ClickAsync("#button");
    }
    catch (ElementNotFoundException ex)
    {
        _logger.LogError("元素未找到: {Error}", ex.Message);
        // 可以进行重试或其他恢复操作
        throw;
    }
}
```

### 3. 测试组织

```csharp
[Trait("Type", "UI")]
public class MyPageTests : IClassFixture<BrowserFixture>
{
    private readonly MyPage _page;
    
    public MyPageTests(BrowserFixture fixture)
    {
        _page = new MyPage(fixture.Page, fixture.Logger);
    }
    
    [Fact]
    public async Task TestScenario()
    {
        // Arrange
        await _page.NavigateAsync("https://example.com");
        await _page.WaitForLoadAsync();
        
        // Act
        await _page.PerformAction();
        
        // Assert
        var result = await _page.AssertEqualAsync(expected, actual);
        Assert.Equal("pass", result);
    }
}
```

## 注意事项

1. **异步操作**：所有方法都是异步的，必须使用 `await` 关键字
2. **选择器格式**：Playwright 支持多种选择器格式（CSS、XPath、文本等）
3. **超时设置**：默认超时为30秒，可以根据需要调整
4. **日志记录**：所有操作都会记录详细日志，便于调试
5. **截图功能**：失败时会自动截图，截图文件保存在 Screenshots 目录
6. **统计功能**：自动跟踪断言的通过/失败次数

## 扩展功能

如果需要添加新的功能，可以继承 `BasePageObjectWithPlaywright` 类并添加自定义方法：

```csharp
public class ExtendedBasePage : BasePageObjectWithPlaywright
{
    public ExtendedBasePage(IPage page, ILogger logger) : base(page, logger) { }
    
    // 添加自定义方法
    public async Task SelectDropdownByTextAsync(string selector, string text)
    {
        await ClickAsync(selector);
        await ClickAsync($"text={text}");
    }
    
    public async Task UploadFileAsync(string selector, string filePath)
    {
        await _page.SetInputFilesAsync(selector, filePath);
    }
}
```

这个基类提供了完整的页面操作功能，可以满足大部分 Web UI 自动化测试的需求。通过合理使用这些方法，可以创建稳定、可维护的自动化测试。