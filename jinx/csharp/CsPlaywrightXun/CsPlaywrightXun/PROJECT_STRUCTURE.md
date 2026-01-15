# CsPlaywrightXun 项目结构说明

## 📁 目录结构

```
CsPlaywrightXun/
├── src/
│   ├── Core/                          # 核心框架（稳定，很少修改）
│   │   ├── Attributes/                # 测试属性和标记
│   │   ├── Base/                      # 基类实现
│   │   ├── Configuration/             # 配置管理
│   │   ├── Exceptions/                # 自定义异常
│   │   ├── Extensions/                # 扩展方法
│   │   ├── Fixtures/                  # 测试固件
│   │   ├── Interfaces/                # 接口定义
│   │   ├── Logging/                   # 日志配置
│   │   ├── Models/                    # 数据模型
│   │   └── Utilities/                 # 工具类
│   │
│   ├── Services/                      # 服务层（中等频率修改）
│   │   ├── Api/                       # API 服务
│   │   ├── Browser/                   # 浏览器服务
│   │   ├── Data/                      # 数据服务
│   │   ├── Notifications/             # 通知服务
│   │   └── Reporting/                 # 报告服务
│   │
│   ├── Tests/                         # 测试实现（频繁修改）
│   │   ├── PageObjects/               # 页面对象
│   │   │   ├── UI/                    # UI 页面对象
│   │   │   └── API/                   # API 页面对象
│   │   ├── Flows/                     # 业务流程
│   │   │   ├── UI/                    # UI 业务流程
│   │   │   └── API/                   # API 业务流程
│   │   ├── TestCases/                 # 测试用例
│   │   │   ├── UI/                    # UI 测试
│   │   │   ├── API/                   # API 测试
│   │   │   ├── Integration/           # 集成测试
│   │   │   └── Unit/                  # 单元测试
│   │   └── TestData/                  # 测试数据
│   │       ├── csv/                   # CSV 数据文件
│   │       ├── json/                  # JSON 数据文件
│   │       └── yaml/                  # YAML 数据文件
│   │
│   ├── config/                        # 配置文件（集中管理）
│   │   ├── environments/              # 环境配置
│   │   ├── notifications/             # 通知配置
│   │   ├── elements/                  # 页面元素配置
│   │   ├── bin/                       # 配置工具
│   │   └── date/                      # 日期相关配置
│   │
│   ├── output/                        # 输出目录（统一管理）
│   │   ├── logs/                      # 日志文件
│   │   ├── reports/                   # 测试报告
│   │   ├── screenshots/               # 截图文件
│   │   └── artifacts/                 # 其他产物
│   │
│   ├── docs/                          # 文档（集中管理）
│   │   ├── guides/                    # 使用指南
│   │   ├── api/                       # API 文档
│   │   ├── examples/                  # 示例代码
│   │   └── 文档.md                    # 主文档
│   │
│   └── playwright/                    # 旧目录（待清理）
│
├── docker/                            # Docker 配置
├── k8s/                              # Kubernetes 配置
├── azure/                            # Azure Pipelines
├── scripts/                          # 脚本工具
├── docs/                             # 项目级文档
└── .kiro/                            # Kiro 配置

```

## 🎯 目录职责说明

### Core/ - 核心框架
**修改频率：低**
- 包含框架的核心功能和基础设施
- 提供基类、接口、工具类等
- 修改需要谨慎，影响范围大

### Services/ - 服务层
**修改频率：中**
- 封装各种服务功能
- 提供可复用的服务组件
- 新增功能时可能需要扩展

### Tests/ - 测试实现
**修改频率：高**
- 包含所有测试相关代码
- 按类型组织：PageObjects、Flows、TestCases
- 日常开发主要在这里进行

### config/ - 配置管理
**修改频率：中**
- 集中管理所有配置文件
- 按功能分类：环境、通知、元素等
- 支持多环境配置

### output/ - 输出目录
**修改频率：高（自动生成）**
- 存放测试执行产生的文件
- 包括日志、报告、截图等
- 通常不纳入版本控制

### docs/ - 文档
**修改频率：中**
- 项目文档和指南
- 按类型分类：guides、api、examples
- 帮助团队理解和使用框架

## 🔄 迁移说明

### 从旧结构迁移

旧的 `src/playwright/` 目录已被重组为新的结构：

| 旧路径 | 新路径 |
|--------|--------|
| `playwright/Core/` | `Core/` |
| `playwright/Services/` | `Services/` |
| `playwright/Pages/` | `Tests/PageObjects/` |
| `playwright/Flows/` | `Tests/Flows/` |
| `playwright/Tests/` | `Tests/TestCases/` |

### 命名空间更新

代码中的命名空间需要相应更新：

```csharp
// 旧命名空间
using CsPlaywrightXun.playwright.Core.Base;
using CsPlaywrightXun.playwright.Services.Api;

// 新命名空间
using CsPlaywrightXun.Core.Base;
using CsPlaywrightXun.Services.Api;
```

## 📝 最佳实践

### 1. 代码组织
- **Core**: 只放稳定的、通用的代码
- **Services**: 封装可复用的服务功能
- **Tests**: 按测试类型清晰分类

### 2. 配置管理
- 环境配置放在 `config/environments/`
- 通知配置放在 `config/notifications/`
- 元素配置放在 `config/elements/`

### 3. 测试数据
- CSV 数据放在 `Tests/TestData/csv/`
- JSON 数据放在 `Tests/TestData/json/`
- YAML 数据放在 `Tests/TestData/yaml/`

### 4. 输出管理
- 日志自动输出到 `output/logs/`
- 报告自动输出到 `output/reports/`
- 截图自动输出到 `output/screenshots/`

## 🚀 下一步

1. **更新项目文件**: 更新 `.csproj` 文件以反映新的目录结构
2. **更新命名空间**: 批量更新代码中的命名空间引用
3. **清理旧目录**: 确认迁移完成后删除 `playwright/` 目录
4. **更新文档**: 更新所有文档中的路径引用
5. **更新 CI/CD**: 更新构建脚本中的路径配置

## 📞 支持

如有问题，请参考：
- 使用指南：`src/docs/guides/`
- API 文档：`src/docs/api/`
- 示例代码：`src/docs/examples/`
