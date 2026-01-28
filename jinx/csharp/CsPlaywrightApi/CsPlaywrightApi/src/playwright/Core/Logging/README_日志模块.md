# API日志模块使用说明

## 功能特性

这个日志模块会自动记录每次API请求的以下信息：

- ✅ 请求地址（URL）
- ✅ 请求方法（GET/POST等）
- ✅ 请求参数（Request Body）
- ✅ 请求头（Request Headers）
- ✅ HTTP状态码（Status Code）
- ✅ 响应Code（从JSON响应中提取的code字段）
- ✅ 响应结果（Response Body）
- ✅ 响应头（Response Headers）
- ✅ 请求耗时（Duration，单位：毫秒）
- ✅ 时间戳（Timestamp）

## 使用方法

### 1. 创建日志记录器

```csharp
// 使用默认配置（日志目录：程序运行目录/Logs，启用控制台输出）
var logger = new ApiLogger();

// 自定义相对路径（相对于程序运行目录）
var logger = new ApiLogger("MyLogs", enableConsoleLog: true);

// 使用绝对路径
var logger = new ApiLogger(@"C:\MyProject\Logs", enableConsoleLog: true);
```

**重要说明：**
- 默认情况下，日志会保存在程序运行目录下的 `Logs` 文件夹
- 对于Debug模式，通常是：`CsPlaywrightApi/bin/Debug/net10.0/Logs/`
- 程序启动时会在控制台显示实际的日志目录路径

### 2. 在API类中使用

日志记录器已经集成到 `LoginApi` 和 `BtcApi` 类中：

```csharp
// 创建API实例时传入logger
var loginApi = new LoginApi(apiContext, logger);
var btcApi = new BtcApi(apiContext, logger);

// 如果不传logger，会自动使用默认配置
var loginApi = new LoginApi(apiContext);
```

### 3. 日志输出

#### 控制台输出示例：

```
日志目录: C:\Projects\CsPlaywrightApi\bin\Debug\net10.0\Logs

================================================================================
[2026-01-14 10:30:45.123] API请求日志
================================================================================
请求地址: https://www.ast1001.com/api/user/authorize
请求方法: POST
请求参数: verify_code=&type=0&login_type=email...
响应状态码: 200
响应Code: 0
响应结果: {"code":0,"message":"success","data":{...}}
耗时: 245ms
================================================================================

✓ 日志已保存到: C:\Projects\CsPlaywrightApi\bin\Debug\net10.0\Logs\api_log_20260114.json
```

#### 文件输出：

日志会自动保存到 `Logs/api_log_yyyyMMdd.json` 文件中，每天一个文件。

文件格式为JSON数组，包含所有请求的详细信息：

```json
{
  "Timestamp": "2026-01-14T10:30:45.123",
  "Url": "https://www.ast1001.com/api/user/authorize",
  "Method": "POST",
  "RequestBody": "verify_code=&type=0&login_type=email...",
  "StatusCode": 200,
  "ResponseCode": "0",
  "ResponseBody": "{\"code\":0,\"message\":\"success\"}",
  "Duration": 245,
  "RequestHeaders": {
    "Content-Type": "application/x-www-form-urlencoded"
  },
  "ResponseHeaders": {
    "content-type": "application/json",
    "set-cookie": "c_token=xxx..."
  }
}
```

## 配置选项

### ApiLogger 构造函数参数

- `logDirectory`: 日志文件保存目录（默认：`Logs`）
- `enableConsoleLog`: 是否在控制台输出日志（默认：`true`）

### 示例

```csharp
// 只保存到文件，不在控制台输出
var logger = new ApiLogger("Logs", enableConsoleLog: false);

// 自定义日志目录
var logger = new ApiLogger("MyCustomLogs/API", enableConsoleLog: true);
```

## 注意事项

1. 日志文件按日期自动分割，格式为 `api_log_yyyyMMdd.json`
2. 日志目录不存在时会自动创建
3. 响应Code字段会自动从JSON响应中提取（如果存在）
4. 所有时间戳使用本地时间
5. 日志文件使用UTF-8编码，支持中文字符
6. **日志默认保存在程序运行目录**（通常是 `bin/Debug/net10.0/Logs/`）
7. 程序启动时会在控制台显示实际的日志保存路径
8. 每次API请求完成后会显示日志文件的完整路径

## 查找日志文件

如果不确定日志保存在哪里，运行程序时注意查看控制台输出：
- 程序启动时会显示：`日志目录: [完整路径]`
- 每次请求后会显示：`✓ 日志已保存到: [完整文件路径]`
