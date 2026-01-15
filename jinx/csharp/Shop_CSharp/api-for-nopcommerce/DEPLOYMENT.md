# nopCommerce API Plugin 部署指南

## 部署状态

✅ **构建完成** - 插件已成功构建并准备部署

## 构建输出位置

插件已自动部署到nopCommerce的Plugins目录：
```
..\nopCommerce\src\Presentation\Nop.Web\Plugins\Nop.Plugin.Api\
```

## 部署步骤

### 1. 验证构建输出

确保以下文件存在于插件目录中：
- ✅ `Nop.Plugin.Api.dll` (746KB)
- ✅ `Nop.Plugin.Api.deps.json`
- ✅ `plugin.json`
- ✅ 所有必要的依赖DLL

### 2. 启动nopCommerce

```bash
# 进入nopCommerce目录
cd ..\nopCommerce

# 构建nopCommerce解决方案
dotnet build

# 运行Nop.Web项目
dotnet run --project src\Presentation\Nop.Web
```

### 3. 安装插件

1. 访问nopCommerce管理面板
2. 进入 **Configuration > Plugins > Local Plugins**
3. 找到 **API Plugin** 并点击 **Install**
4. 等待安装完成

### 4. 配置插件

#### 基本设置
1. 在插件列表中点击 **Configure**
2. 启用 **Enable API** 选项
3. 设置 **Token Expiry** (访问令牌过期时间，建议30天)
4. 保存设置

#### 权限配置
1. 进入 **Customers > Customer Roles**
2. 创建新的角色或编辑现有角色
3. 分配以下权限：
   - `Configuration > Manage Shipping Settings`
   - `Catalog > Categories Create/Edit/Delete`
   - `Catalog > Products Create/Edit/Delete`
   - `Customers > Customers Create/Edit/Delete`
   - `Orders > Orders Create/Edit/Delete`

### 5. 创建API用户

1. 进入 **Customers > Customers**
2. 创建新客户或编辑现有客户
3. 分配具有API权限的角色
4. 记录客户邮箱和密码

## API使用

### 1. 获取访问令牌

```bash
POST /api/token
Content-Type: application/json

{
  "email": "your-email@example.com",
  "password": "your-password"
}
```

响应示例：
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 2592000
}
```

### 2. 使用API端点

#### 获取分类列表
```bash
GET /api/categories
Authorization: Bearer <your-access-token>
```

#### 创建产品
```bash
POST /api/products
Authorization: Bearer <your-access-token>
Content-Type: application/json

{
  "name": "Sample Product",
  "price": 99.99,
  "categoryIds": [1, 2],
  "published": true
}
```

#### 获取客户信息
```bash
GET /api/customers
Authorization: Bearer <your-access-token>
```

### 3. Swagger文档

访问 `/api/swagger` 查看完整的API文档和测试界面。

## 测试验证

### 1. 基本功能测试

```bash
# 测试认证
curl -X POST "http://localhost:5000/api/token" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password"}'

# 测试API访问
curl -X GET "http://localhost:5000/api/categories" \
  -H "Authorization: Bearer <token>"
```

### 2. 权限测试

- 尝试访问未授权的端点
- 验证角色权限是否正确应用
- 测试令牌过期机制

## 故障排除

### 常见问题

#### 1. 插件未显示
- 检查插件DLL是否正确复制
- 重启nopCommerce应用程序
- 检查插件目录权限

#### 2. API端点404错误
- 确认插件已安装并启用
- 检查路由配置
- 验证URL路径

#### 3. 认证失败
- 检查用户凭据
- 验证用户角色权限
- 检查令牌是否过期

#### 4. 权限错误
- 确认用户角色包含必要权限
- 检查API设置配置
- 验证权限属性设置

### 日志检查

1. 启用详细日志记录
2. 检查nopCommerce日志文件
3. 查看API插件的调试信息

## 性能优化

### 1. 缓存策略
- 启用Redis缓存
- 配置API响应缓存
- 优化数据库查询

### 2. 安全配置
- 设置适当的令牌过期时间
- 启用HTTPS
- 配置IP白名单（如需要）

## 监控和维护

### 1. 健康检查
- 定期测试API端点
- 监控响应时间
- 检查错误率

### 2. 更新维护
- 定期更新依赖包
- 监控nopCommerce版本兼容性
- 备份插件配置

## 支持资源

### 1. 文档
- [nopCommerce官方文档](https://docs.nopcommerce.com/)
- [ASP.NET Core Web API文档](https://docs.microsoft.com/en-us/aspnet/core/web-api/)

### 2. 社区
- [nopCommerce论坛](https://www.nopcommerce.com/boards/)
- [GitHub Issues](https://github.com/your-repo/issues)

---

**部署完成时间**: $(Get-Date)
**插件版本**: 0.0.1-local
**目标框架**: .NET 9.0
**兼容性**: nopCommerce 5.0+ 