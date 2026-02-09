# 实施计划

- [x] 1. 项目结构和基础配置

  - 设置基础配置文件（appsettings.json、launchSettings.json）
  - _需求: 7.1, 7.2_

- [x] 2. 领域层核心实体

  - 实现User实体类及其业务规则
  - 实现Blog实体类及其业务规则
  - 实现FileEntity实体类及其业务规则
  - 定义领域服务接口（IPasswordHashingService、ITokenService）
  - _需求: 1.1, 2.1, 3.1, 4.1, 5.1_

- [x] 3. 领域层仓储接口定义

  - 定义IUserRepository接口
  - 定义IBlogRepository接口
  - 定义IFileRepository接口
  - 创建通用仓储接口和分页结果类
  - _需求: 1.1, 3.1, 4.1, 5.1_

- [x] 4. 应用层命令查询定义

  - 定义博客相关的Command和Query类
  - 定义文件相关的Command和Query类
  - 创建DTO类和响应模型
  - _需求: 1.1, 1.2, 1.3, 2.1, 2.2, 3.1, 3.2, 3.3, 4.1, 5.1_

- [x] 5. 应用层服务接口定义

  - 定义IAuthApplicationService接口
  - 定义IBlogApplicationService接口
  - 定义IFileApplicationService接口
  - 定义外部服务接口（IMarkdownService、IFileStorageService）
  - _需求: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1_

- [x] 6. 基础设施层数据库配置

  - 实现BlogDbContext数据库上下文
  - 配置实体关系和约束
  - 创建数据库迁移文件
  - 配置MySQL连接字符串
  - _需求: 7.1, 7.2, 7.3_

- [x] 7. 基础设施层仓储实现

  - 实现BlogRepository数据访问类
  - 实现FileRepository数据访问类
  - 为仓储实现编写单元测试
  - _需求: 1.1, 3.1, 4.1, 5.1, 7.3_

- [x] 8. 基础设施层服务实现

  - 实现BCryptPasswordHashingService密码哈希服务
  - 实现JwtTokenService JWT令牌服务
  - 实现MarkdownService Markdown处理服务
  - 实现LocalFileStorageService文件存储服务
  - _需求: 1.2, 1.3, 2.2, 6.1, 6.3, 8.4_

- [x] 9. 应用层服务实现

  - 实现AuthApplicationService认证服务
  - 实现BlogApplicationService博客服务
  - 实现FileApplicationService文件服务
  - _需求: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 5.4_

- [x] 10. 表现层基础配置

  - 配置依赖注入容器
  - 配置JWT认证中间件
  - 配置Swagger/OpenAPI文档
  - 实现全局异常处理中间件
  - _需求: 8.1, 8.2_

- [x] 11. 认证控制器实现

  - 实现AuthController用户注册功能
  - 实现AuthController用户登录功能
  - 实现AuthController令牌刷新功能
  - 添加输入验证和错误处理
  - _需求: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 2.4_

- [x] 12. 博客控制器实现

  - 实现BlogsController获取博客列表功能
  - 实现BlogsController获取单篇博客功能
  - 实现BlogsController创建博客功能
  - 实现BlogsController更新和删除博客功能
  - _需求: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [x] 13. 文件控制器实现

  - 实现FilesController文件上传功能
  - 实现FilesController文件下载功能
  - 实现FilesController文件删除功能
  - 添加文件安全验证和权限控制
  - _需求: 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 5.4_

- [x] 14. 输入验证和安全加固


  - 使用FluentValidation实现请求验证
  - 实现Markdown内容安全处理
  - 添加文件上传安全检查
  - 实现API访问权限控制
  - _需求: 4.2, 4.3, 6.2, 8.2, 8.3, 8.4_

- [ ] 15. 静态文件服务和默认页面配置


  - 配置静态文件中间件支持HTML、CSS、JS等文件
  - 创建wwwroot文件夹和index.html默认页面
  - 配置应用启动时默认打开http://localhost:5164/index.html
  - 在launchSettings.json中设置正确的端口和启动URL
  - _需求: 用户体验优化_

- [ ] 16. Markdown功能完善
  - 集成Markdig库实现Markdown转HTML
  - 实现代码语法高亮支持
  - 添加Markdown内容清理功能
  - _需求: 6.1, 6.2, 6.3, 6.4_

- [ ] 17. 最终优化和部署准备
  - 优化数据库查询性能
  - 配置生产环境设置
  - 完善API文档和使用说明
  - _需求: 7.3, 所有功能需求_
