# 安全功能实现说明

## 已实现的安全功能

### 1. FluentValidation 输入验证

#### 认证命令验证器
- **RegisterCommandValidator**: 用户注册验证
  - 用户名：3-50字符，只允许字母、数字、下划线和中文
  - 邮箱：标准邮箱格式验证，域名白名单检查
  - 密码：8-100字符，必须包含大小写字母、数字和特殊字符
  - 弱密码检测：防止常见弱密码模式

- **LoginCommandValidator**: 用户登录验证
  - 支持邮箱或用户名登录
  - 自动检测输入类型并应用相应验证规则

- **RefreshTokenCommandValidator**: 令牌刷新验证
  - JWT格式验证
  - 刷新令牌长度和格式检查

#### 博客命令验证器
- **CreateBlogCommandValidator**: 博客创建验证
  - 标题：1-200字符，安全内容检查
  - 内容：Markdown格式和安全性验证
  - 标签：数量限制（最多10个），格式验证
  - 危险内容检测

- **UpdateBlogCommandValidator**: 博客更新验证
  - 与创建验证相同的安全检查
  - 权限验证

#### 文件命令验证器
- **UploadFileCommandValidator**: 文件上传验证
  - 文件类型白名单检查
  - 文件大小限制（10MB）
  - 危险扩展名检测
  - 文件名安全性验证
  - 文件流一致性检查

### 2. Markdown 内容安全处理

#### 增强的安全功能
- **扩展的危险标签检测**: 包括 SVG、Math、Details 等标签
- **全面的事件处理器检测**: 检测所有可能的 JavaScript 事件处理器
- **多种脚本协议检测**: JavaScript、VBScript、LiveScript、Mocha 等
- **Markdown 语法验证**: 确保 Markdown 格式正确
- **内容清理**: 自动移除危险内容
- **纯文本提取**: 用于摘要生成，防止 HTML 注入

### 3. 文件上传安全检查

#### SecurityService 实现
- **文件魔数检测**: 验证文件真实类型与声明类型是否匹配
- **恶意内容扫描**: 检测文件中的脚本和恶意代码
- **图片安全验证**: 专门针对图片文件的安全检查
- **文件名清理**: 移除危险字符和路径遍历攻击
- **安全路径生成**: 基于用户ID和时间戳的安全存储路径

#### 文件类型检测
- 支持常见文件类型的魔数签名检测
- JPEG、PNG、GIF、PDF、ZIP 等格式验证
- 防止文件类型伪装攻击

### 4. API 访问权限控制

#### 授权策略
- **BlogAuthorizationHandler**: 博客资源授权
  - 读取权限：公开博客任何人可读，私有博客仅作者可读
  - 创建权限：任何认证用户可创建
  - 更新/删除权限：仅作者可操作
  - 发布权限：仅作者可发布

- **FileAuthorizationHandler**: 文件资源授权
  - 读取权限：公开文件任何人可读，私有文件仅上传者可读
  - 上传权限：任何认证用户可上传
  - 更新/删除权限：仅上传者可操作
  - 可见性更改：仅上传者可更改

#### 速率限制
- **RateLimitingMiddleware**: API 调用频率限制
  - 一般端点：100次/分钟
  - 认证端点：10次/分钟
  - 敏感端点：5次/分钟
  - 基于用户ID或IP地址的限制
  - 自动清理过期记录

#### 安全头设置
- **SecurityHeadersMiddleware**: HTTP 安全头
  - X-Content-Type-Options: nosniff
  - X-Frame-Options: DENY
  - X-XSS-Protection: 1; mode=block
  - Content-Security-Policy: 严格的CSP策略
  - Strict-Transport-Security: 强制HTTPS
  - Permissions-Policy: 限制浏览器功能
  - 移除服务器信息泄露头

## 配置说明

### appsettings.json 配置
```json
{
  "RateLimit": {
    "WindowSizeInSeconds": 60,
    "GeneralEndpointLimit": 100,
    "AuthEndpointLimit": 10,
    "SensitiveEndpointLimit": 5
  },
  "Security": {
    "EnableStrictValidation": true,
    "MaxContentLength": 100000,
    "AllowedImageTypes": ["image/jpeg", "image/png", "image/gif", "image/webp"],
    "BlockedFileExtensions": [".exe", ".bat", ".cmd", ".scr", ".vbs", ".js", ".php", ".asp"]
  }
}
```

## 使用示例

### 1. 注册用户（带验证）
```bash
POST /api/auth/register
{
  "username": "testuser",
  "email": "test@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}
```

### 2. 创建博客（带Markdown安全检查）
```bash
POST /api/blogs
Authorization: Bearer <token>
{
  "title": "我的博客标题",
  "content": "# 标题\n\n这是安全的Markdown内容",
  "summary": "博客摘要",
  "tags": ["技术", "编程"],
  "isPublished": true
}
```

### 3. 上传文件（带安全检查）
```bash
POST /api/files/upload
Authorization: Bearer <token>
Content-Type: multipart/form-data

file: <binary-data>
isPublic: false
```

## 安全特性总结

1. **输入验证**: 使用FluentValidation进行全面的输入验证
2. **内容安全**: Markdown内容的安全处理和清理
3. **文件安全**: 文件类型检测、恶意内容扫描、安全存储
4. **访问控制**: 基于资源的细粒度权限控制
5. **速率限制**: 防止API滥用和DDoS攻击
6. **安全头**: 全面的HTTP安全头设置
7. **认证授权**: JWT令牌认证和基于策略的授权

这些安全功能为博客API提供了全面的安全保护，确保系统的安全性和稳定性。