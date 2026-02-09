# 设计文档

## 概述

博客API是一个基于ASP.NET Core的RESTful Web API，提供完整的博客管理功能。系统采用洋葱架构设计，使用Entity Framework Core作为ORM框架连接MySQL数据库，JWT进行身份认证，支持Markdown格式的文章编写和文件管理。

## 架构

### 整体架构
系统采用洋葱架构（Onion Architecture）模式，实现依赖倒置和关注点分离：

```
┌─────────────────────────────────────┐
│         Infrastructure Layer        │  ← 基础设施层
│  ┌─────────────────────────────────┐ │
│  │        Application Layer        │ │  ← 应用层
│  │  ┌─────────────────────────────┐ │ │
│  │  │       Domain Layer          │ │ │  ← 领域层
│  │  │  ┌─────────────────────────┐ │ │ │
│  │  │  │      Core Entities      │ │ │ │  ← 核心实体
│  │  │  └─────────────────────────┘ │ │ │
│  │  └─────────────────────────────┘ │ │
│  └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

### 层级说明
- **Core/Domain Layer**: 包含实体、值对象、领域服务和接口
- **Application Layer**: 包含应用服务、用例、DTO和应用接口
- **Infrastructure Layer**: 包含数据访问、外部服务、文件系统等实现
- **Presentation Layer**: 包含API控制器、中间件等

### 技术栈
- **框架**: ASP.NET Core 8.0
- **数据库**: MySQL 8.0
- **ORM**: Entity Framework Core
- **认证**: JWT Bearer Token
- **Markdown处理**: Markdig
- **文件存储**: 本地文件系统
- **API文档**: Swagger/OpenAPI

## 组件和接口

### 1. Domain Layer (领域层)

#### 领域实体
- **User**: 用户聚合根
- **Blog**: 博客聚合根  
- **FileEntity**: 文件实体

#### 领域服务接口
```csharp
public interface IPasswordHashingService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal ValidateToken(string token);
}
```

#### 仓储接口 (在Domain层定义)
```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
    Task<User> GetByEmailAsync(string email);
    Task<User> GetByUsernameAsync(string username);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
}

public interface IBlogRepository
{
    Task<PagedResult<Blog>> GetPagedAsync(BlogQueryParameters parameters);
    Task<Blog> GetByIdAsync(int id);
    Task<Blog> CreateAsync(Blog blog);
    Task<Blog> UpdateAsync(Blog blog);
    Task<bool> DeleteAsync(int id);
}

public interface IFileRepository
{
    Task<FileEntity> GetByIdAsync(int id);
    Task<FileEntity> CreateAsync(FileEntity file);
    Task<bool> DeleteAsync(int id);
    Task<List<FileEntity>> GetByUserIdAsync(int userId);
}
```

### 2. Application Layer (应用层)

#### 应用服务接口
```csharp
public interface IAuthApplicationService
{
    Task<AuthResult> RegisterAsync(RegisterCommand command);
    Task<AuthResult> LoginAsync(LoginCommand command);
    Task<AuthResult> RefreshTokenAsync(RefreshTokenCommand command);
}

public interface IBlogApplicationService
{
    Task<PagedResult<BlogDto>> GetBlogsAsync(GetBlogsQuery query);
    Task<BlogDto> GetBlogByIdAsync(GetBlogByIdQuery query);
    Task<BlogDto> CreateBlogAsync(CreateBlogCommand command);
    Task<BlogDto> UpdateBlogAsync(UpdateBlogCommand command);
    Task<bool> DeleteBlogAsync(DeleteBlogCommand command);
}

public interface IFileApplicationService
{
    Task<FileUploadResult> UploadFileAsync(UploadFileCommand command);
    Task<FileDownloadResult> GetFileAsync(GetFileQuery query);
    Task<bool> DeleteFileAsync(DeleteFileCommand command);
}
```

#### 命令和查询 (CQRS模式)
```csharp
// Commands
public record RegisterCommand(string Username, string Email, string Password);
public record LoginCommand(string EmailOrUsername, string Password);
public record CreateBlogCommand(string Title, string Content, string Summary, List<string> Tags, bool IsPublished, int AuthorId);

// Queries  
public record GetBlogsQuery(int Page, int PageSize, string SearchTerm, bool? IsPublished, int? AuthorId);
public record GetBlogByIdQuery(int Id, int? UserId);
```

#### 外部服务接口
```csharp
public interface IMarkdownService
{
    string ConvertToHtml(string markdown);
    string SanitizeMarkdown(string markdown);
}

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName);
    Task<Stream> GetFileAsync(string filePath);
    Task<bool> DeleteFileAsync(string filePath);
}
```

### 3. Infrastructure Layer (基础设施层)

#### 仓储实现
- **UserRepository**: 用户数据访问实现
- **BlogRepository**: 博客数据访问实现
- **FileRepository**: 文件数据访问实现

#### 外部服务实现
- **MarkdownService**: Markdown处理服务实现
- **LocalFileStorageService**: 本地文件存储实现
- **JwtTokenService**: JWT令牌服务实现
- **BCryptPasswordHashingService**: 密码哈希服务实现

### 4. Presentation Layer (表现层)

#### API控制器
```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // POST /api/auth/register - 用户注册
    // POST /api/auth/login - 用户登录  
    // POST /api/auth/refresh - 刷新令牌
}

[ApiController]
[Route("api/[controller]")]
public class BlogsController : ControllerBase
{
    // GET /api/blogs - 获取博客列表（支持分页和筛选）
    // GET /api/blogs/{id} - 获取单篇博客
    // POST /api/blogs - 创建博客文章
    // PUT /api/blogs/{id} - 更新博客文章
    // DELETE /api/blogs/{id} - 删除博客文章
}

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    // POST /api/files/upload - 上传文件
    // GET /api/files/{id} - 下载文件
    // DELETE /api/files/{id} - 删除文件
}
```

## 数据模型

### 用户模型 (User)
```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    
    // 导航属性
    public ICollection<Blog> Blogs { get; set; }
    public ICollection<FileEntity> Files { get; set; }
}
```

### 博客模型 (Blog)
```csharp
public class Blog
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; } // Markdown内容
    public string Summary { get; set; }
    public string Tags { get; set; } // JSON格式存储标签
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int AuthorId { get; set; }
    
    // 导航属性
    public User Author { get; set; }
}
```

### 文件模型 (FileEntity)
```csharp
public class FileEntity
{
    public int Id { get; set; }
    public string OriginalName { get; set; }
    public string StoredName { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public string FilePath { get; set; }
    public DateTime UploadedAt { get; set; }
    public int UploadedBy { get; set; }
    public bool IsPublic { get; set; }
    
    // 导航属性
    public User Uploader { get; set; }
}
```

### 数据库上下文 (BlogDbContext)
```csharp
public class BlogDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<FileEntity> Files { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 配置实体关系和约束
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
            
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
            
        modelBuilder.Entity<Blog>()
            .HasOne(b => b.Author)
            .WithMany(u => u.Blogs)
            .HasForeignKey(b => b.AuthorId);
            
        modelBuilder.Entity<FileEntity>()
            .HasOne(f => f.Uploader)
            .WithMany(u => u.Files)
            .HasForeignKey(f => f.UploadedBy);
    }
}
```

## 错误处理

### 全局异常处理中间件
```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

### 自定义异常类型
- `BusinessException` - 业务逻辑异常
- `ValidationException` - 数据验证异常
- `UnauthorizedException` - 未授权异常
- `NotFoundException` - 资源未找到异常

### 统一响应格式
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; }
}
```

## 测试策略

### 单元测试
- **控制器测试**: 使用xUnit和Moq框架测试API端点
- **服务层测试**: 测试业务逻辑和数据转换
- **仓储层测试**: 使用内存数据库测试数据访问逻辑

### 集成测试
- **API集成测试**: 使用TestServer测试完整的HTTP请求流程
- **数据库集成测试**: 使用测试数据库验证数据持久化

### 测试工具
- **xUnit**: 测试框架
- **Moq**: 模拟框架
- **FluentAssertions**: 断言库
- **Microsoft.AspNetCore.Mvc.Testing**: Web API测试

### 测试覆盖率目标
- 控制器: 90%以上
- 服务层: 95%以上
- 仓储层: 90%以上

## 安全考虑

### 认证和授权
- JWT令牌有效期设置为1小时
- 实施刷新令牌机制
- 使用HTTPS确保传输安全

### 数据验证
- 输入数据验证使用FluentValidation
- SQL注入防护通过Entity Framework参数化查询
- XSS防护通过输入清理和输出编码

### 文件安全
- 文件类型白名单验证
- 文件大小限制（默认10MB）
- 文件存储路径安全检查

### 密码安全
- 使用BCrypt进行密码哈希
- 密码强度要求：至少8位，包含大小写字母和数字