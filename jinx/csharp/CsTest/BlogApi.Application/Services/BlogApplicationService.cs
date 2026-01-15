using BlogApi.Application.Commands.Blog;
using BlogApi.Application.DTOs;
using BlogApi.Application.DTOs.Common;
using BlogApi.Application.Queries.Blog;
using BlogApi.Domain.Common;
using BlogApi.Domain.Entities;
using BlogApi.Domain.Interfaces;

namespace BlogApi.Application.Services;

/// <summary>
/// 博客应用服务实现
/// </summary>
public class BlogApplicationService : IBlogApplicationService
{
    private readonly IBlogRepository _blogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMarkdownService _markdownService;

    public BlogApplicationService(
        IBlogRepository blogRepository,
        IUserRepository userRepository,
        IMarkdownService markdownService)
    {
        _blogRepository = blogRepository;
        _userRepository = userRepository;
        _markdownService = markdownService;
    }

    public async Task<OperationResult<PagedResult<BlogListItemDto>>> GetBlogsAsync(GetBlogsQuery query)
    {
        try
        {
            var parameters = new BlogQueryParameters
            {
                Page = query.Page,
                PageSize = query.PageSize,
                SearchTerm = query.SearchTerm,
                IsPublished = query.OnlyPublished ? true : query.IsPublished,
                AuthorId = query.AuthorId
            };

            var pagedResult = await _blogRepository.GetPagedAsync(parameters);
            
            var blogListItems = pagedResult.Items.Select(MapToBlogListItemDto).ToList();
            
            var result = new PagedResult<BlogListItemDto>
            {
                Items = blogListItems,
                TotalCount = pagedResult.TotalCount,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize
            };

            return OperationResult<PagedResult<BlogListItemDto>>.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            return OperationResult<PagedResult<BlogListItemDto>>.CreateFailure("获取博客列表过程中发生错误", "GET_BLOGS_ERROR");
        }
    }

    public async Task<OperationResult<PagedResult<BlogListItemDto>>> GetBlogsByAuthorAsync(GetBlogsByAuthorQuery query)
    {
        try
        {
            // 验证作者是否存在
            var author = await _userRepository.GetByIdAsync(query.AuthorId);
            if (author == null)
            {
                return OperationResult<PagedResult<BlogListItemDto>>.CreateFailure("作者不存在", "AUTHOR_NOT_FOUND");
            }

            var parameters = new BlogQueryParameters
            {
                Page = query.Page,
                PageSize = query.PageSize,
                AuthorId = query.AuthorId,
                IsPublished = query.OnlyPublished ? true : query.IsPublished
            };

            var pagedResult = await _blogRepository.GetPagedAsync(parameters);
            
            var blogListItems = pagedResult.Items.Select(MapToBlogListItemDto).ToList();
            
            var result = new PagedResult<BlogListItemDto>
            {
                Items = blogListItems,
                TotalCount = pagedResult.TotalCount,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize
            };

            return OperationResult<PagedResult<BlogListItemDto>>.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            return OperationResult<PagedResult<BlogListItemDto>>.CreateFailure("获取作者博客列表过程中发生错误", "GET_AUTHOR_BLOGS_ERROR");
        }
    }

    public async Task<OperationResult<PagedResult<BlogListItemDto>>> SearchBlogsAsync(SearchBlogsQuery query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query.Keyword))
            {
                return OperationResult<PagedResult<BlogListItemDto>>.CreateFailure("搜索关键词不能为空", "EMPTY_SEARCH_TERM");
            }

            var parameters = new BlogQueryParameters
            {
                Page = query.Page,
                PageSize = query.PageSize,
                SearchTerm = query.Keyword,
                IsPublished = true // 搜索只返回已发布的博客
            };

            var pagedResult = await _blogRepository.GetPagedAsync(parameters);
            
            var blogListItems = pagedResult.Items.Select(MapToBlogListItemDto).ToList();
            
            var result = new PagedResult<BlogListItemDto>
            {
                Items = blogListItems,
                TotalCount = pagedResult.TotalCount,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize
            };

            return OperationResult<PagedResult<BlogListItemDto>>.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            return OperationResult<PagedResult<BlogListItemDto>>.CreateFailure("搜索博客过程中发生错误", "SEARCH_BLOGS_ERROR");
        }
    }

    public async Task<OperationResult<BlogDto>> GetBlogByIdAsync(GetBlogByIdQuery query)
    {
        try
        {
            var blog = await _blogRepository.GetByIdAsync(query.Id);
            if (blog == null)
            {
                return OperationResult<BlogDto>.CreateFailure("博客不存在", "BLOG_NOT_FOUND");
            }

            // 检查访问权限：未发布的博客只有作者可以查看
            if (!blog.IsPublished && (!query.UserId.HasValue || blog.AuthorId != query.UserId.Value))
            {
                return OperationResult<BlogDto>.CreateFailure("无权限访问此博客", "ACCESS_DENIED");
            }

            var blogDto = MapToBlogDto(blog);
            
            return OperationResult<BlogDto>.CreateSuccess(blogDto);
        }
        catch (Exception ex)
        {
            return OperationResult<BlogDto>.CreateFailure("获取博客详情过程中发生错误", "GET_BLOG_ERROR");
        }
    }

    public async Task<OperationResult<BlogDto>> CreateBlogAsync(CreateBlogCommand command)
    {
        try
        {
            // 验证作者是否存在且有权限
            var author = await _userRepository.GetByIdAsync(command.AuthorId);
            if (author == null)
            {
                return OperationResult<BlogDto>.CreateFailure("作者不存在", "AUTHOR_NOT_FOUND");
            }

            if (!author.CanCreateBlog())
            {
                return OperationResult<BlogDto>.CreateFailure("无权限创建博客", "CREATE_PERMISSION_DENIED");
            }

            // 验证和清理Markdown内容
            var sanitizedContent = _markdownService.SanitizeMarkdown(command.Content);
            if (!_markdownService.IsMarkdownSafe(sanitizedContent))
            {
                return OperationResult<BlogDto>.CreateFailure("博客内容包含不安全元素", "UNSAFE_CONTENT");
            }

            // 如果没有提供摘要，从内容中自动生成
            var summary = string.IsNullOrWhiteSpace(command.Summary) 
                ? _markdownService.ExtractPlainText(sanitizedContent, 200)
                : command.Summary;

            // 创建博客实体
            var blog = new Blog
            {
                Title = command.Title,
                Content = sanitizedContent,
                Summary = summary,
                AuthorId = command.AuthorId,
                IsPublished = command.IsPublished,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 设置标签
            if (command.Tags != null && command.Tags.Any())
            {
                blog.SetTags(command.Tags);
            }

            var createdBlog = await _blogRepository.CreateAsync(blog);
            var blogDto = MapToBlogDto(createdBlog);

            return OperationResult<BlogDto>.CreateSuccess(blogDto);
        }
        catch (Exception ex)
        {
            return OperationResult<BlogDto>.CreateFailure("创建博客过程中发生错误", "CREATE_BLOG_ERROR");
        }
    }

    public async Task<OperationResult<BlogDto>> UpdateBlogAsync(UpdateBlogCommand command)
    {
        try
        {
            var blog = await _blogRepository.GetByIdAsync(command.Id);
            if (blog == null)
            {
                return OperationResult<BlogDto>.CreateFailure("博客不存在", "BLOG_NOT_FOUND");
            }

            // 检查权限
            if (!blog.CanBeEditedBy(command.UserId))
            {
                return OperationResult<BlogDto>.CreateFailure("无权限编辑此博客", "EDIT_PERMISSION_DENIED");
            }

            // 验证和清理Markdown内容
            var sanitizedContent = _markdownService.SanitizeMarkdown(command.Content);
            if (!_markdownService.IsMarkdownSafe(sanitizedContent))
            {
                return OperationResult<BlogDto>.CreateFailure("博客内容包含不安全元素", "UNSAFE_CONTENT");
            }

            // 如果没有提供摘要，从内容中自动生成
            var summary = string.IsNullOrWhiteSpace(command.Summary) 
                ? _markdownService.ExtractPlainText(sanitizedContent, 200)
                : command.Summary;

            // 更新博客内容
            blog.UpdateContent(command.Title, sanitizedContent, summary);

            // 更新标签
            if (command.Tags != null)
            {
                blog.SetTags(command.Tags);
            }

            var updatedBlog = await _blogRepository.UpdateAsync(blog);
            var blogDto = MapToBlogDto(updatedBlog);

            return OperationResult<BlogDto>.CreateSuccess(blogDto);
        }
        catch (Exception ex)
        {
            return OperationResult<BlogDto>.CreateFailure("更新博客过程中发生错误", "UPDATE_BLOG_ERROR");
        }
    }

    public async Task<OperationResult<BlogDto>> PublishBlogAsync(PublishBlogCommand command)
    {
        try
        {
            var blog = await _blogRepository.GetByIdAsync(command.Id);
            if (blog == null)
            {
                return OperationResult<BlogDto>.CreateFailure("博客不存在", "BLOG_NOT_FOUND");
            }

            // 检查权限
            if (!blog.CanBeEditedBy(command.UserId))
            {
                return OperationResult<BlogDto>.CreateFailure("无权限操作此博客", "PUBLISH_PERMISSION_DENIED");
            }

            // 发布博客
            blog.Publish();

            var updatedBlog = await _blogRepository.UpdateAsync(blog);
            var blogDto = MapToBlogDto(updatedBlog);

            return OperationResult<BlogDto>.CreateSuccess(blogDto);
        }
        catch (Exception ex)
        {
            return OperationResult<BlogDto>.CreateFailure("发布博客过程中发生错误", "PUBLISH_BLOG_ERROR");
        }
    }

    public async Task<OperationResult> DeleteBlogAsync(DeleteBlogCommand command)
    {
        try
        {
            var blog = await _blogRepository.GetByIdAsync(command.Id);
            if (blog == null)
            {
                return OperationResult.CreateFailure("博客不存在", "BLOG_NOT_FOUND");
            }

            // 检查权限
            if (!blog.CanBeDeletedBy(command.UserId))
            {
                return OperationResult.CreateFailure("无权限删除此博客", "DELETE_PERMISSION_DENIED");
            }

            var deleted = await _blogRepository.DeleteAsync(command.Id);
            if (!deleted)
            {
                return OperationResult.CreateFailure("删除博客失败", "DELETE_FAILED");
            }

            return OperationResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            return OperationResult.CreateFailure("删除博客过程中发生错误", "DELETE_BLOG_ERROR");
        }
    }

    public async Task<OperationResult<bool>> ValidateBlogPermissionAsync(int blogId, int userId)
    {
        try
        {
            var blog = await _blogRepository.GetByIdAsync(blogId);
            if (blog == null)
            {
                return OperationResult<bool>.CreateFailure("博客不存在", "BLOG_NOT_FOUND");
            }

            var hasPermission = blog.CanBeEditedBy(userId);
            
            return OperationResult<bool>.CreateSuccess(hasPermission);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.CreateFailure("验证博客权限过程中发生错误", "VALIDATE_PERMISSION_ERROR");
        }
    }

    private static BlogDto MapToBlogDto(Blog blog)
    {
        return new BlogDto
        {
            Id = blog.Id,
            Title = blog.Title,
            Content = blog.Content,
            Summary = blog.Summary,
            Tags = blog.GetTags(),
            IsPublished = blog.IsPublished,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt,
            Author = new UserSummaryDto
            {
                Id = blog.Author.Id,
                Username = blog.Author.Username
            }
        };
    }

    private static BlogListItemDto MapToBlogListItemDto(Blog blog)
    {
        return new BlogListItemDto
        {
            Id = blog.Id,
            Title = blog.Title,
            Summary = blog.Summary,
            Tags = blog.GetTags(),
            IsPublished = blog.IsPublished,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt,
            Author = new UserSummaryDto
            {
                Id = blog.Author.Id,
                Username = blog.Author.Username
            }
        };
    }
}