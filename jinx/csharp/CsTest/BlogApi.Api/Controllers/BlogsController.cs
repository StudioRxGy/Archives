using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BlogApi.Application.Services;
using BlogApi.Application.Commands.Blog;
using BlogApi.Application.Queries.Blog;
using BlogApi.Application.DTOs;
using BlogApi.Application.DTOs.Common;
using BlogApi.Domain.Common;

namespace BlogApi.Api.Controllers;

/// <summary>
/// 博客控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BlogsController : ControllerBase
{
    private readonly IBlogApplicationService _blogApplicationService;
    private readonly ILogger<BlogsController> _logger;

    public BlogsController(
        IBlogApplicationService blogApplicationService,
        ILogger<BlogsController> logger)
    {
        _blogApplicationService = blogApplicationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取博客列表（支持分页和筛选）
    /// </summary>
    /// <param name="page">页码（默认1）</param>
    /// <param name="pageSize">每页大小（默认10）</param>
    /// <param name="searchTerm">搜索关键词</param>
    /// <param name="isPublished">是否已发布</param>
    /// <param name="authorId">作者ID</param>
    /// <returns>分页博客列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<PagedResult<BlogListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBlogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isPublished = null,
        [FromQuery] int? authorId = null)
    {
        try
        {
            // 获取当前用户ID（如果已认证）
            var currentUserId = GetCurrentUserId();

            var query = new GetBlogsQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm ?? string.Empty,
                IsPublished = isPublished,
                AuthorId = authorId,
                UserId = currentUserId,
                OnlyPublished = currentUserId == null || (authorId.HasValue && authorId.Value != currentUserId)
            };

            var result = await _blogApplicationService.GetBlogsAsync(query);

            if (result.Success)
            {
                _logger.LogInformation("获取博客列表成功，页码: {Page}, 每页大小: {PageSize}", page, pageSize);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<PagedResult<BlogListItemDto>>.CreateSuccess(result.Data!, "获取博客列表成功");
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("获取博客列表失败: {Error}", result.ErrorMessage);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode });
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取博客列表过程中发生异常");
            var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("获取博客列表过程中发生错误", new List<string> { "GET_BLOGS_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 根据ID获取单篇博客详情
    /// </summary>
    /// <param name="id">博客ID</param>
    /// <returns>博客详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<BlogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBlogById(int id)
    {
        try
        {
            if (id <= 0)
            {
                var validationResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateValidationFailure(new List<string> { "博客ID必须大于0" });
                return BadRequest(validationResponse);
            }

            // 获取当前用户ID（如果已认证）
            var currentUserId = GetCurrentUserId();

            var query = new GetBlogByIdQuery
            {
                Id = id,
                UserId = currentUserId,
                IncludeUnpublished = currentUserId.HasValue
            };

            var result = await _blogApplicationService.GetBlogByIdAsync(query);

            if (result.Success)
            {
                _logger.LogInformation("获取博客详情成功，博客ID: {BlogId}", id);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<BlogDto>.CreateSuccess(result.Data!, "获取博客详情成功");
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("获取博客详情失败，博客ID: {BlogId}, 错误: {Error}", id, result.ErrorMessage);
                
                if (result.ErrorCode == "BLOG_NOT_FOUND")
                {
                    var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode });
                    return NotFound(response);
                }
                else
                {
                    var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode });
                    return BadRequest(response);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取博客详情过程中发生异常，博客ID: {BlogId}", id);
            var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("获取博客详情过程中发生错误", new List<string> { "GET_BLOG_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 创建博客文章
    /// </summary>
    /// <param name="command">创建博客命令</param>
    /// <returns>创建的博客信息</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<BlogDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateBlog([FromBody] CreateBlogCommand command)
    {
        try
        {
            // 验证模型状态
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                    .ToList();

                var validationResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateValidationFailure(validationErrors);
                return BadRequest(validationResponse);
            }

            // 获取当前用户ID
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("用户未认证", new List<string> { "UNAUTHORIZED" });
                return Unauthorized(response);
            }

            // 设置作者ID为当前用户
            command.AuthorId = currentUserId.Value;

            var result = await _blogApplicationService.CreateBlogAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("创建博客成功，标题: {Title}, 作者ID: {AuthorId}", command.Title, command.AuthorId);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<BlogDto>.CreateSuccess(result.Data!, "博客创建成功");
                return CreatedAtAction(nameof(GetBlogById), new { id = result.Data!.Id }, response);
            }
            else
            {
                _logger.LogWarning("创建博客失败，标题: {Title}, 错误: {Error}", command.Title, result.ErrorMessage);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode });
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建博客过程中发生异常，标题: {Title}", command.Title);
            var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("创建博客过程中发生错误", new List<string> { "CREATE_BLOG_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 更新博客文章
    /// </summary>
    /// <param name="id">博客ID</param>
    /// <param name="command">更新博客命令</param>
    /// <returns>更新后的博客信息</returns>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<BlogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateBlog(int id, [FromBody] UpdateBlogCommand command)
    {
        try
        {
            if (id <= 0)
            {
                var validationResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateValidationFailure(new List<string> { "博客ID必须大于0" });
                return BadRequest(validationResponse);
            }

            // 验证模型状态
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                    .ToList();

                var validationResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateValidationFailure(validationErrors);
                return BadRequest(validationResponse);
            }

            // 获取当前用户ID
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("用户未认证", new List<string> { "UNAUTHORIZED" });
                return Unauthorized(response);
            }

            // 设置博客ID和用户ID
            command.Id = id;
            command.UserId = currentUserId.Value;

            var result = await _blogApplicationService.UpdateBlogAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("更新博客成功，博客ID: {BlogId}, 用户ID: {UserId}", id, currentUserId.Value);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<BlogDto>.CreateSuccess(result.Data!, "博客更新成功");
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("更新博客失败，博客ID: {BlogId}, 用户ID: {UserId}, 错误: {Error}", id, currentUserId.Value, result.ErrorMessage);
                
                return result.ErrorCode switch
                {
                    "BLOG_NOT_FOUND" => NotFound(BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode })),
                    "PERMISSION_DENIED" => StatusCode(StatusCodes.Status403Forbidden, BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode })),
                    _ => BadRequest(BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode }))
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新博客过程中发生异常，博客ID: {BlogId}", id);
            var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("更新博客过程中发生错误", new List<string> { "UPDATE_BLOG_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 删除博客文章
    /// </summary>
    /// <param name="id">博客ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteBlog(int id)
    {
        try
        {
            if (id <= 0)
            {
                var validationResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateValidationFailure(new List<string> { "博客ID必须大于0" });
                return BadRequest(validationResponse);
            }

            // 获取当前用户ID
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("用户未认证", new List<string> { "UNAUTHORIZED" });
                return Unauthorized(response);
            }

            var command = new DeleteBlogCommand
            {
                Id = id,
                UserId = currentUserId.Value
            };

            var result = await _blogApplicationService.DeleteBlogAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("删除博客成功，博客ID: {BlogId}, 用户ID: {UserId}", id, currentUserId.Value);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateSuccess(new object(), "博客删除成功");
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("删除博客失败，博客ID: {BlogId}, 用户ID: {UserId}, 错误: {Error}", id, currentUserId.Value, result.ErrorMessage);
                
                return result.ErrorCode switch
                {
                    "BLOG_NOT_FOUND" => NotFound(BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode })),
                    "PERMISSION_DENIED" => StatusCode(StatusCodes.Status403Forbidden, BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode })),
                    _ => BadRequest(BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode }))
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除博客过程中发生异常，博客ID: {BlogId}", id);
            var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("删除博客过程中发生错误", new List<string> { "DELETE_BLOG_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 获取当前认证用户的ID
    /// </summary>
    /// <returns>用户ID，如果未认证则返回null</returns>
    private int? GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
        }
        return null;
    }
}