# ApiClient 使用说明

## 简介

`ApiClient` 是一个封装了 Playwright API 自动化常用方法的基础类，提供了简洁易用的接口来进行 API 测试。

## 主要功能

### 1. 支持的HTTP方法
- ✅ GET 请求
- ✅ POST 请求（JSON / Form表单）
- ✅ PUT 请求
- ✅ DELETE 请求
- ✅ PATCH 请求

### 2. 核心特性
- ✅ 自动日志记录
- ✅ 默认请求头管理
- ✅ 查询参数构建
- ✅ JSON响应解析
- ✅ 响应断言
- ✅ 字段提取

## 基础使用

### 创建 ApiClient 实例

```csharp
using var playwright = await Playwright.CreateAsync();
var apiContext = await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
{
    BaseURL = "https://api.example.com"
});

// 创建ApiClient（自动使用默认logger）
var client = new ApiClient(apiContext);

// 或者传入自定义logger
var logger = new ApiLogger("Logs", enableConsoleLog: true);
var client = new ApiClient(apiContext, logger);
```

### 设置默认请求头

```csharp
// 设置所有请求都会携带的请求头
client.SetDefaultHeaders(new Dictionary<string, string>
{
    ["Authorization"] = "Bearer your_token",
    ["User-Agent"] = "MyApp/1.0"
});

// 或者添加单个请求头
client.AddDefaultHeader("X-Custom-Header", "value");
```

## 请求方法示例

### GET 请求

```csharp
// 简单GET请求
var response = await client.GetAsync("https://api.example.com/users");

// 带查询参数的GET请求
var queryParams = new Dictionary<string, string>
{
    ["page"] = "1",
    ["limit"] = "10"
};
var response = await client.GetAsync("https://api.example.com/users", queryParams: queryParams);

// 带自定义请求头
var headers = new Dictionary<string, string>
{
    ["Accept"] = "application/json"
};
var response = await client.GetAsync("https://api.example.com/users", headers: headers);
```

### POST 请求（JSON）

```csharp
// 发送JSON数据
var data = new
{
    username = "test@example.com",
    password = "password123"
};

var response = await client.PostJsonAsync("https://api.example.com/login", data);
```

### POST 请求（表单）

```csharp
// 发送表单数据
var formData = new Dictionary<string, string>
{
    ["username"] = "test@example.com",
    ["password"] = "password123",
    ["remember"] = "true"
};

var response = await client.PostFormAsync("https://api.example.com/login", formData);
```

### PUT 请求

```csharp
var updateData = new
{
    name = "New Name",
    email = "newemail@example.com"
};

var response = await client.PutJsonAsync("https://api.example.com/users/123", updateData);
```

### DELETE 请求

```csharp
var response = await client.DeleteAsync("https://api.example.com/users/123");
```

### PATCH 请求

```csharp
var patchData = new
{
    status = "active"
};

var response = await client.PatchJsonAsync("https://api.example.com/users/123", patchData);
```

## 响应处理

### 获取响应文本

```csharp
var response = await client.GetAsync("https://api.example.com/users");
var responseText = await client.GetTextResponseAsync(response);
Console.WriteLine(responseText);
```

### 解析JSON响应

```csharp
// 定义响应模型
public class LoginResponse
{
    public int code { get; set; }
    public string message { get; set; }
    public string token { get; set; }
}

// 解析响应
var response = await client.PostJsonAsync("https://api.example.com/login", loginData);
var loginResult = await client.GetJsonResponseAsync<LoginResponse>(response);

Console.WriteLine($"Token: {loginResult.token}");
```

### 提取JSON字段

```csharp
var response = await client.GetAsync("https://api.example.com/user/profile");

// 提取顶层字段
var userId = await client.ExtractJsonFieldAsync(response, "id");

// 提取嵌套字段（使用点号分隔）
var userName = await client.ExtractJsonFieldAsync(response, "data.user.name");
```

## 断言验证

### 使用 ApiClient 内置断言

```csharp
var response = await client.GetAsync("https://api.example.com/users");

// 验证状态码
client.AssertStatusCode(response, 200);

// 验证响应成功（2xx）
client.AssertSuccess(response);
```

### 使用 ApiAssertions 静态类

```csharp
using CsPlaywrightApi.src.playwright.Core.Api;

var response = await client.GetAsync("https://api.example.com/users");

// 断言状态码
ApiAssertions.AssertStatusCode(response, 200);

// 断言响应成功
ApiAssertions.AssertSuccess(response);

// 断言响应包含文本
await ApiAssertions.AssertContainsTextAsync(response, "success");

// 断言JSON字段值
await ApiAssertions.AssertJsonFieldAsync(response, "code", "0");

// 断言JSON字段存在
await ApiAssertions.AssertJsonFieldExistsAsync(response, "data.token");

// 断言响应头存在
ApiAssertions.AssertHeaderExists(response, "Content-Type");

// 断言响应头值
ApiAssertions.AssertHeaderValue(response, "Content-Type", "application/json");

// 断言响应时间
ApiAssertions.AssertResponseTime(245, 1000); // 实际245ms，最大1000ms
```

## 完整示例

### 示例1：用户登录流程

```csharp
using var playwright = await Playwright.CreateAsync();
var apiContext = await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
{
    BaseURL = "https://api.example.com"
});

var client = new ApiClient(apiContext);

// 1. 登录
var loginData = new
{
    username = "test@example.com",
    password = "password123"
};

var loginResponse = await client.PostJsonAsync("/api/login", loginData);
client.AssertStatusCode(loginResponse, 200);

// 2. 提取token
var token = await client.ExtractJsonFieldAsync(loginResponse, "data.token");
Console.WriteLine($"登录成功，Token: {token}");

// 3. 使用token访问受保护的API
client.AddDefaultHeader("Authorization", $"Bearer {token}");

var profileResponse = await client.GetAsync("/api/user/profile");
client.AssertSuccess(profileResponse);

var userName = await client.ExtractJsonFieldAsync(profileResponse, "data.name");
Console.WriteLine($"用户名: {userName}");
```

### 示例2：CRUD操作

```csharp
var client = new ApiClient(apiContext);

// Create - 创建用户
var createData = new
{
    name = "张三",
    email = "zhangsan@example.com",
    age = 25
};
var createResponse = await client.PostJsonAsync("/api/users", createData);
ApiAssertions.AssertStatusCode(createResponse, 201);
var userId = await client.ExtractJsonFieldAsync(createResponse, "data.id");

// Read - 读取用户
var readResponse = await client.GetAsync($"/api/users/{userId}");
ApiAssertions.AssertSuccess(readResponse);
await ApiAssertions.AssertJsonFieldAsync(readResponse, "data.name", "张三");

// Update - 更新用户
var updateData = new { name = "李四" };
var updateResponse = await client.PutJsonAsync($"/api/users/{userId}", updateData);
ApiAssertions.AssertSuccess(updateResponse);

// Delete - 删除用户
var deleteResponse = await client.DeleteAsync($"/api/users/{userId}");
ApiAssertions.AssertStatusCode(deleteResponse, 204);
```

## 继承使用

你可以继承 `ApiClient` 来创建特定业务的API类：

```csharp
public class UserApi : ApiClient
{
    public UserApi(IAPIRequestContext apiContext, ApiLogger? logger = null) 
        : base(apiContext, logger)
    {
    }

    public async Task<IAPIResponse> LoginAsync(string username, string password)
    {
        var loginData = new
        {
            username = username,
            password = password
        };
        
        return await PostJsonAsync("/api/login", loginData);
    }

    public async Task<IAPIResponse> GetUserProfileAsync(string token)
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {token}"
        };
        
        return await GetAsync("/api/user/profile", headers: headers);
    }
}

// 使用
var userApi = new UserApi(apiContext);
var response = await userApi.LoginAsync("test@example.com", "password123");
```

## 注意事项

1. 所有请求都会自动记录日志（包括请求参数、响应结果、耗时等）
2. 默认请求头会应用到所有请求，但可以被单次请求的自定义请求头覆盖
3. JSON序列化使用 `System.Text.Json`，确保你的数据模型兼容
4. 断言失败会抛出 `AssertionException` 异常
5. 响应体只能读取一次，如果需要多次使用，请先保存到变量中
