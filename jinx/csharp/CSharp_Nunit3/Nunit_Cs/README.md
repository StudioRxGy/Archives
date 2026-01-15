# NUnit_Cs 自动化测试框架

## 项目简介

NUnit_Cs 是一个基于 .NET 8 和 NUnit 4 的自动化测试框架，旨在整合多种工具和技术，提供全面的API和UI自动化测试解决方案。通过使用C#、NUnit、Selenium WebDriver、EPPlus、Newtonsoft.Json等技术，本项目能够帮助开发者高效地进行自动化测试，并生成详细的测试报告。

## 关键特性

- **多技术整合**：结合.NET 8、NUnit 4、Selenium WebDriver等多种工具，实现API和UI自动化测试。
- **详细报告**：使用NUnit测试框架生成美观且详细的测试报告。
- **数据处理**：利用EPPlus处理Excel数据，方便测试数据管理和结果分析。
- **持续集成**：可与CI/CD工具集成，确保项目代码始终处于良好状态。

## 项目结构

```plaintext
NUnit_Cs
├── BasePage           # UI自动化基础类封装目录 
│   └── Pages          # UI页面封装目录
├── Case               # 测试数据目录
│   ├── API            # 接口测试数据目录
│   └── UI             # UI测试数据目录
├── Common             # 全局公共模块
├── Config             # 配置文件
│   ├── AppSettings.cs # 配置管理类
│   └── appSettings.json # 配置文件
├── Logs               # 日志目录
├── Reports            # 测试报告
│   └── img            # 截图目录
├── TestCase           # 测试用例目录
│   ├── API            # 接口测试用例目录
│   └── UI             # UI测试用例目录
│       ├── UiTests.cs     # UI测试用例
│       └── UiTestSetup.cs # UI测试初始化类
├── Tools              # 工具类目录
├── NUnit.runsettings  # NUnit测试配置文件
├── run_tests.ps1      # PowerShell测试运行脚本
├── run_tests.bat      # 批处理测试运行脚本
├── Program.cs         # 测试主入口
├── README.md          # 项目说明文档
└── Nunit_Cs.csproj    # 项目文件
```

## 快速开始

1. 工具和环境要求：

- .NET 8 SDK: <https://dotnet.microsoft.com/download/dotnet/8.0>
- Visual Studio 2022: <https://visualstudio.microsoft.com/vs/>
- 浏览器驱动（如ChromeDriver）: 根据实际使用的浏览器版本下载

2. 搭建步骤

- 2.1 拉取代码
  - `gitclone https://BillionaireDailyStudio@dev.azure.com/BillionaireDailyStudio/CSharp_Nunit3/_git/CSharp_Nunit3`
  - 查看本地和远程所有分支： `git branch -a`
  - 切换分支：`git checkout [branch-name]`

- 2.2 恢复项目依赖
  - `dotnet restore`

- 2.3 构建项目
  - `dotnet build`

- 2.4 运行测试
  - 使用PowerShell运行所有测试：`.\run_tests.ps1`
  - 使用批处理文件运行所有测试：`run_tests.bat`
  - 运行API测试：`.\run_tests.ps1 -t API`
  - 运行UI测试：`.\run_tests.ps1 -t UI`
  - 运行具体类别的测试：`.\run_tests.ps1 -f "Name~TestLogin"`
  - 详细输出：`.\run_tests.ps1 -v`
  - 失败重跑：`.\run_tests.ps1 -r`

## pytest.ini 与 NUnit.runsettings 对比

Python的pytest.ini文件被转换为NUnit测试框架的NUnit.runsettings XML文件。以下是关键配置的对应关系：

| pytest.ini 配置 | NUnit.runsettings 对应 | 说明 |
|----------------|----------------------|------|
| addopts        | \<NUnit\> 元素下的多个设置 | 测试运行选项 |
| testpaths      | --filter 命令行参数       | 测试路径过滤 |
| python_files   | 不适用                    | C#中通过命名约定识别测试文件 |
| python_classes | 不适用                    | C#中使用[TestFixture]特性 |
| python_functions | 不适用                  | C#中使用[Test]特性 |
| log_file_level | \<LoggerRunSettings\>   | 日志级别设置 |
| log_file_format | \<LoggerRunSettings\>  | 日志格式设置 |
| log_cli         | \<LoggerRunSettings\>  | 控制台日志输出 |

## 主要功能模块

### 配置管理

使用`appSettings.json`管理项目配置，包括浏览器类型、API基础URL、超时时间等。`AppSettings.cs`类提供了统一的配置访问接口。

### API测试

支持通过Excel或YAML文件配置API测试用例，包括请求URL、方法、头信息、请求体、预期结果等。

### UI测试

基于页面对象模式（POM）实现UI自动化测试，使用Selenium WebDriver与网页交互，支持元素定位、点击、输入、截图等操作。

### 数据驱动

支持使用Excel、YAML和CSV文件作为测试数据源，实现数据驱动测试。

### 报告生成

利用NUnit内置的测试报告功能生成HTML测试报告，展示测试结果、通过率等信息。

## 测试用例编写

### API测试用例

API测试用例可以通过Excel或YAML文件配置，包含以下字段：
- `name`: 用例名称
- `run`: 是否执行（yes/no）
- `request`: 请求信息，包含URL、方法、头信息、请求体等
- `expected`: 预期结果，包含状态码、消息、数据等
- `type`: 数据类型（json/form）

### UI测试用例

UI测试用例通过CSV文件配置，根据测试场景包含不同的字段。例如登录测试：
- `test_case`: 用例ID
- `username`: 用户名
- `password`: 密码
- `descr`: 描述（如"登录成功"、"登录失败"）

## 注意事项

1. 浏览器驱动需要根据实际使用的浏览器版本下载，并放置在正确的位置。
2. 运行UI测试需要保证网络连接正常，能够访问相应的网站。
3. 测试数据需要按照规定的格式进行编写，确保能够被正确解析。 