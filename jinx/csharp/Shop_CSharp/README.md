api-for-nopcommerce × nopCommerce 一体化说明
===========================================

本仓库包含两个并列目录：

- `api-for-nopcommerce/`：第三方 Web API 插件解决方案（插件本体、示例客户端、源码生成器与测试）
- `nopCommerce/`：nopCommerce 官方开源商城源码（宿主站点与核心库）

目标：在宿主站点中安装并运行 API 插件，向外提供 RESTful 接口，便于与第三方系统或 App 集成。

目录结构
--------
```
/
├─ api-for-nopcommerce/
│  ├─ Nop.Plugin.Api/                # 插件本体（构建后自动输出到 Nop.Web/Plugins/Nop.Plugin.Api）
│  ├─ ClientApp/                     # 示例客户端（演示登录、读写资源、购物车增删改）
│  ├─ ApiBindingsGenerator/          # 从 Swagger 生成 DTO 与强类型客户端的源码生成器
│  └─ Nop.Plugin.Api.Tests/          # 测试
└─ nopCommerce/
   └─ src/
      ├─ Libraries/                  # Nop.Core / Nop.Data / Nop.Services 等
      └─ Presentation/
         └─ Nop.Web/                 # 宿主站点（后台安装本插件）
```

环境要求
--------
- .NET SDK 9.0
- Windows/WSL/Linux/Mac 任一开发环境
- SQL Server / PostgreSQL / MySQL 三选一（按 nopCommerce 官方文档准备）

快速开始（本地调试）
------------------
1) 还原与构建插件（默认 Debug）
```powershell
cd api-for-nopcommerce
pwsh -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

2) 启动宿主站点
```powershell
cd ..\nopCommerce\src\Presentation\Nop.Web
dotnet run --configuration Debug
```

3) 在后台安装插件
- 访问 `http://localhost:5000/admin`
- 进入 Configuration → Plugins → Local plugins
- 找到 “API Plugin” → Install → 按需配置

4) 验证接口
- 例如获取分类列表：
```http
GET /api/categories
Authorization: Bearer <your-jwt-token>
```

常用命令
--------
- 仅构建解决方案（Debug）：
```powershell
dotnet build --configuration Debug
```
- 运行测试：
```powershell
dotnet test --configuration Debug
```
- 发布宿主站点（示例）：
```powershell
dotnet publish ..\nopCommerce\src\Presentation\Nop.Web\Nop.Web.csproj -c Release -o ..\..\..\publish\Nop.Web
```

功能亮点
--------
- 覆盖核心资源：分类、产品、客户、订单、制造商、仓库、购物车等
- 安全：JWT 认证，基于角色/权限控制
- 体验：Swagger 文档、DTO 映射、参数验证、分页与过滤
- 工具：从 Swagger 自动生成强类型客户端

常见问题与排查
--------------
- 构建失败：确认仓库与 `nopCommerce/` 为同级；.NET SDK 为 9.0
- 插件未出现：确认插件已输出至 `Nop.Web/Plugins/Nop.Plugin.Api/`，并刷新后台 “Local plugins”
- 运行时报缺库：将依赖添加到宿主 `Nop.Web.csproj`（例如 `AutoMapper`、`Microsoft.IdentityModel.*`），还原后重启
- 文件占用：如热启动失败，先结束残留 `dotnet` 进程或重启开发环境

开发指引
--------
- 新增端点：在 `Nop.Plugin.Api/Controllers` 新建控制器，继承 `BaseApiController`，添加权限特性，完善 DTO/验证/映射
- DTO 与映射：位于 `Nop.Plugin.Api.DTO.*` 与 AutoMapper 配置，保持输入输出契约稳定
- 源码生成：`ApiBindingsGenerator` 可从 Swagger 生成 DTO 与客户端，便于外部系统接入

许可证与贡献
------------
- 遵循 nopCommerce 许可条款
- 欢迎提交 Issue / PR 改进插件与文档


