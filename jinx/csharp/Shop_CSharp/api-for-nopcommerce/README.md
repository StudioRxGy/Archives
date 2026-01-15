# nopCommerce API Plugin

这是一个为nopCommerce电子商务平台提供RESTful API的插件项目。

完整的一体化安装与使用说明请参见仓库根目录的 `README.md`。

## 项目结构

- **Nop.Plugin.Api**: 主要的API插件项目
- **ClientApp**: 客户端应用程序示例
- **ApiBindingsGenerator**: C#源代码生成器，用于从Swagger文件生成DTO和API客户端类
- **Nop.Plugin.Api.Tests**: 测试项目

## 系统要求

- .NET 9.0 SDK
- nopCommerce 5.0+ 源代码

## 构建说明

### 1. 克隆项目

```bash
git clone <repository-url>
cd api-for-nopcommerce
```

### 2. 确保nopCommerce源代码在同一目录级别

项目结构应该是：
```
/
├── api-for-nopcommerce/
│   ├── Nop.Plugin.Api/
│   ├── ClientApp/
│   └── ...
└── nopCommerce/
    └── src/
        └── Libraries/
```

### 3. 构建项目

```bash
# 恢复NuGet包
dotnet restore

# 构建整个解决方案
dotnet build

# 运行测试
dotnet test
```

## 主要功能

### API端点

该插件提供以下主要API端点：

- **Categories**: 分类管理
- **Products**: 产品管理
- **Customers**: 客户管理
- **Orders**: 订单管理
- **Manufacturers**: 制造商管理
- **Warehouses**: 仓库管理
- **Shopping Cart**: 购物车管理
- **Authentication**: JWT令牌认证

### 特性

- RESTful API设计
- JWT令牌认证
- 基于角色的权限控制
- 自动映射和验证
- Swagger文档支持
- 分页和过滤支持

## 配置

### 1. 启用API插件

在nopCommerce管理面板中：
1. 进入 **Configuration > Plugins > Local Plugins**
2. 找到 **API Plugin** 并点击 **Install**
3. 配置API设置

### 2. API设置

- **Enable API**: 启用/禁用API
- **Token Expiry**: 访问令牌过期时间（天）
- **Permissions**: 配置API访问权限

## 使用示例

### 获取分类列表

```bash
GET /api/categories
Authorization: Bearer <your-jwt-token>
```

### 创建产品

```bash
POST /api/products
Authorization: Bearer <your-jwt-token>
Content-Type: application/json

{
  "name": "Sample Product",
  "price": 99.99,
  "categoryIds": [1, 2]
}
```

## 开发说明

### 添加新的API端点

1. 在 `Controllers` 文件夹中创建新的控制器
2. 继承 `BaseApiController`
3. 添加适当的权限属性
4. 实现CRUD操作
5. 添加相应的DTO和映射

### 自定义验证

1. 在 `Validators` 文件夹中创建验证器
2. 继承 `BaseDtoValidator<T>`
3. 实现自定义验证规则

## 故障排除

### 常见问题

1. **构建失败**: 确保nopCommerce源代码路径正确
2. **权限错误**: 检查API设置和用户角色权限
3. **认证失败**: 验证JWT令牌是否有效

### 日志

启用详细日志记录以调试API问题：
- 检查nopCommerce日志文件
- 启用API插件的调试模式

## 贡献

欢迎提交问题和拉取请求！

## 许可证

本项目遵循nopCommerce的许可证条款。

## 支持

如有问题，请：
1. 检查项目文档
2. 搜索现有问题
3. 创建新的问题报告
