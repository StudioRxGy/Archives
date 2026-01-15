# src 目录说明

## 📁 目录结构

```
src/
├── Docs/                    # 文档目录
│   ├── book/               # Jupyter Notebook 示例
│   │   ├── testcs.ipynb   # C# 测试示例
│   │   ├── testmd.ipynb   # Markdown 测试示例
│   │   └── testpy.ipynb   # Python 测试示例
│   └── explain/            # 说明文档
│       ├── Trait快速参考.md
│       ├── Trait标记使用指南.md
│       └── Trait标记实现总结.md
│
├── output/                  # 输出目录（自动生成）
│   ├── logs/               # 测试日志
│   │   └── YYYYMMDD_HHMMSS/
│   │       ├── api_log.json      # JSON 格式日志
│   │       └── api_report.html   # HTML 可视化报告
│   └── screenshots/        # 截图目录
│
└── playwright/             # Playwright 测试框架
    ├── Core/              # 核心模块
    │   ├── Api/          # API 工具类
    │   ├── Config/       # 配置管理
    │   └── Logging/      # 日志模块
    ├── Flows/            # 业务流程
    │   └── Api/
    │       └── astralx/
    │           ├── c2c/        # C2C 接口
    │           └── Uheyue/     # Uheyue 接口
    └── Tests/            # 测试用例
        ├── UheyueApiTests.cs
        └── UheyueApiTestsWithFixture.cs
```

## 🎯 核心模块

### 1. Core - 核心模块

#### Api - API 工具类
- **ApiClient.cs** - API 客户端基类
- **ApiAssertions.cs** - API 断言工具
- 📖 [ApiClient 使用说明](./playwright/Core/Api/README_ApiClient使用说明.md)
- 📖 [快速参考](./playwright/Core/Api/快速参考.md)

**主要功能：**
- 统一的 API 请求封装
- 丰富的断言方法（状态码、JSON 字段、响应头等）
- 自动日志记录
- Token 管理

#### Config - 配置管理
- **AppSettings.cs** - 应用配置单例
- 📖 [配置使用说明](./playwright/Core/Config/配置使用说明.md)

**主要功能：**
- 多环境支持（Development、Test、Staging、Production）
- 自动路径管理
- 统一配置访问

**配置项：**
```csharp
var settings = AppSettings.Instance;
settings.Config.BaseUrl      // 基础 URL
settings.Config.Timeout      // 超时时间
settings.Config.Headless     // 无头模式
settings.LogDirectory        // 日志目录
settings.CurrentEnvironment  // 当前环境
```

#### Logging - 日志模块
- **ApiLogger.cs** - API 日志记录器
- **ApiLoggerExample.cs** - 使用示例
- 📖 [日志模块说明](./playwright/Core/Logging/README_日志模块.md)

**主要功能：**
- 自动记录 API 请求/响应
- 生成 JSON 格式日志
- 生成 HTML 可视化报告
- 按时间戳分组（精确到秒）

**日志内容：**
- 请求地址、方法、参数
- 响应状态码、响应体
- 执行时间
- 测试结果统计

### 2. Flows - 业务流程

#### astralx/Uheyue - Uheyue 接口实现
- **Login.cs** - 登录接口
- **ShijiaBuy.cs** - 市价买入接口
- **Pingcang.cs** - 闪电平仓接口
- **Program.cs** - 程序入口

**接口功能：**
- ✅ 用户登录认证
- ✅ Token 自动提取
- ✅ BTC 市价买入
- ✅ 闪电平仓
- ✅ 订单 ID 提取

#### astralx/c2c - C2C 接口实现
- **LoginApi.cs** - C2C 登录接口

### 3. Tests - 测试用例

#### UheyueApiTests.cs - 标准测试套件
**测试用例：**
- Test01 - 用户登录成功
- Test02 - 登录后能提取到 Token
- Test03 - 创建 BTC 市价买入订单
- Test04 - 验证订单响应包含必要字段
- Test05 - 执行闪电平仓
- Test06 - 验证平仓响应包含订单信息
- Test07 - 完整交易流程（登录→开仓→平仓）
- Test08 - 未设置 Token 时创建订单应失败
- Test09 - 使用空 Token 创建订单应失败

**Trait 标记：**
- Category: API, Login, Trade, BuyOrder, ClosePosition, Exception, E2E
- Speed: Fast, Slow
- Priority: Critical, High, Medium
- Smoke: true

#### UheyueApiTestsWithFixture.cs - Fixture 测试套件
使用共享 Fixture 优化测试性能，包含参数化测试。

## 🚀 快速开始

### 1. 运行测试
```bash
# 运行所有测试
dotnet test

# 运行快速测试
dotnet test --filter "Fast=true"

# 运行登录相关测试
dotnet test --filter "Category=Login"

# 运行冒烟测试
dotnet test --filter "Smoke=true"
```

### 2. 查看日志
测试完成后，日志会自动保存到：
```
src/output/logs/YYYYMMDD_HHMMSS/
├── api_log.json      # JSON 格式日志
└── api_report.html   # HTML 可视化报告
```

### 3. 打开 HTML 报告
```bash
# Windows
start src\output\logs\20260114_173012\api_report.html
```

## 📊 HTML 报告内容

报告包含以下内容：
- ✅ 测试摘要（总请求数、成功/失败数、总执行时间）
- ✅ 可视化图表（饼图、柱状图、折线图）
- ✅ 详细测试结果（请求/响应详情）
- ✅ 请求头、响应头、请求体、响应体

## 🔧 配置环境

### 切换测试环境
```bash
# 设置环境变量
set ENV=Test
dotnet test

# 或在代码中设置
Environment.SetEnvironmentVariable("ENV", "Test");
```

### 支持的环境
- **Development** - 开发环境（默认）
- **Test** - 测试环境
- **Staging** - 预发布环境
- **Production** - 生产环境

## 📖 相关文档

### 项目根目录文档
- [README.md](../README.md) - 项目总览
- [测试日志功能.md](../测试日志功能.md) - 日志功能验证
- [日志改造说明.md](../日志改造说明.md) - 日志系统改造详情
- [配置使用说明.md](../配置使用说明.md) - 配置管理详细说明
- [Uheyue接口自动化测试指南.md](../Uheyue接口自动化测试指南.md) - 完整测试指南
- [快速开始-Uheyue接口测试.md](../快速开始-Uheyue接口测试.md) - 5分钟快速上手
- [Uheyue接口测试-实现总结.md](../Uheyue接口测试-实现总结.md) - 实现总结

### 模块文档
- [ApiClient 使用说明](./playwright/Core/Api/README_ApiClient使用说明.md)
- [日志模块说明](./playwright/Core/Logging/README_日志模块.md)
- [配置使用说明](./playwright/Core/Config/配置使用说明.md)
- [Trait 标记使用指南](./Docs/explain/Trait标记使用指南.md)

## 🎨 特色功能

### 1. 自动日志记录
所有 API 请求自动记录，无需手动编写日志代码。

### 2. 可视化报告
自动生成包含图表的 HTML 报告，直观展示测试结果。

### 3. 多环境配置
一键切换测试环境，无需修改代码。

### 4. 丰富的断言
提供多种断言方法，简化测试编写。

### 5. Token 管理
自动提取和管理认证 Token。

### 6. 按时间分组
每次测试运行创建独立的时间戳文件夹，便于历史追溯。

## ✅ 测试统计

- **总测试数：** 20+
- **接口覆盖：** 3个（Login, ShijiaBuy, Pingcang）
- **测试类型：** 功能测试、集成测试、异常测试、E2E 测试
- **快速测试：** 11个
- **慢速测试：** 3个
- **冒烟测试：** 1个

---

**提示：** 更多详细信息请查看各模块的 README 文档。
