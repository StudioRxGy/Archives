using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BlogApi.Application.Services;
using BlogApi.Application.Commands.File;
using BlogApi.Application.Queries.File;
using BlogApi.Application.DTOs;
using BlogApi.Domain.Common;

namespace BlogApi.Api.Controllers;

/// <summary>
/// 文件控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FilesController : ControllerBase
{
    private readonly IFileApplicationService _fileApplicationService;
    private readonly ILogger<FilesController> _logger;

    // 允许的文件类型
    private static readonly HashSet<string> AllowedContentTypes = new()
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp",
        "application/pdf", "text/plain", "text/markdown",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/zip", "application/x-zip-compressed"
    };

    // 最大文件大小 (10MB)
    private const long MaxFileSize = 10 * 1024 * 1024;

    public FilesController(
        IFileApplicationService fileApplicationService,
        ILogger<FilesController> logger)
    {
        _fileApplicationService = fileApplicationService;
        _logger = logger;
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <param name="isPublic">是否公开文件</param>
    /// <returns>文件上传结果</returns>
    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<FileUploadResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] bool isPublic = false)
    {
        try
        {
            // 验证文件是否存在
            if (file == null || file.Length == 0)
            {
                var validationResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateValidationFailure(new List<string> { "请选择要上传的文件" });
                return BadRequest(validationResponse);
            }

            // 验证文件大小
            if (file.Length > MaxFileSize)
            {
                var sizeResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure($"文件大小不能超过 {MaxFileSize / (1024 * 1024)} MB", new List<string> { "FILE_TOO_LARGE" });
                return StatusCode(StatusCodes.Status413PayloadTooLarge, sizeResponse);
            }

            // 验证文件类型
            if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                var typeResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("不支持的文件类型", new List<string> { "UNSUPPORTED_FILE_TYPE" });
                return BadRequest(typeResponse);
            }

            // 验证文件名
            if (string.IsNullOrWhiteSpace(file.FileName) || file.FileName.Length > 255)
            {
                var nameResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateValidationFailure(new List<string> { "文件名无效或过长" });
                return BadRequest(nameResponse);
            }

            // 获取当前用户ID
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("用户未认证", new List<string> { "UNAUTHORIZED" });
                return Unauthorized(response);
            }

            // 创建上传命令
            var command = new UploadFileCommand
            {
                FileStream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                IsPublic = isPublic,
                UploadedBy = currentUserId.Value
            };

            var result = await _fileApplicationService.UploadFileAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("文件上传成功，文件名: {FileName}, 用户ID: {UserId}, 大小: {Size} bytes", 
                    file.FileName, currentUserId.Value, file.Length);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<FileUploadResultDto>.CreateSuccess(result.Data!, "文件上传成功");
                return CreatedAtAction(nameof(GetFile), new { id = result.Data!.Id }, response);
            }
            else
            {
                _logger.LogWarning("文件上传失败，文件名: {FileName}, 用户ID: {UserId}, 错误: {Error}", 
                    file.FileName, currentUserId.Value, result.ErrorMessage);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode });
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传过程中发生异常，文件名: {FileName}", file?.FileName);
            var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("文件上传过程中发生错误", new List<string> { "UPLOAD_FILE_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件流</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFile(int id)
    {
        try
        {
            if (id <= 0)
            {
                var validationResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateValidationFailure(new List<string> { "文件ID必须大于0" });
                return BadRequest(validationResponse);
            }

            // 获取当前用户ID（如果已认证）
            var currentUserId = GetCurrentUserId();

            var query = new GetFileQuery
            {
                Id = id,
                UserId = currentUserId
            };

            var result = await _fileApplicationService.GetFileAsync(query);

            if (result.Success)
            {
                _logger.LogInformation("文件下载成功，文件ID: {FileId}, 用户ID: {UserId}", id, currentUserId);
                
                var fileResult = result.Data!;
                return File(fileResult.FileStream, fileResult.ContentType, fileResult.FileName);
            }
            else
            {
                _logger.LogWarning("文件下载失败，文件ID: {FileId}, 用户ID: {UserId}, 错误: {Error}", 
                    id, currentUserId, result.ErrorMessage);
                
                return result.ErrorCode switch
                {
                    "FILE_NOT_FOUND" => NotFound(BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode })),
                    "PERMISSION_DENIED" => StatusCode(StatusCodes.Status403Forbidden, BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode })),
                    _ => BadRequest(BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode }))
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件下载过程中发生异常，文件ID: {FileId}", id);
            var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("文件下载过程中发生错误", new List<string> { "DOWNLOAD_FILE_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 获取文件列表（支持分页和筛选）
    /// </summary>
    /// <param name="page">页码（默认1）</param>
    /// <param name="pageSize">每页大小（默认10）</param>
    /// <param name="searchTerm">搜索关键词</param>
    /// <param name="contentType">内容类型过滤</param>
    /// <param name="isPublic">是否公开</param>
    /// <param name="uploadedBy">上传者ID</param>
    /// <returns>分页文件列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<PagedResult<FileDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFiles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? contentType = null,
        [FromQuery] bool? isPublic = null,
        [FromQuery] int? uploadedBy = null)
    {
        try
        {
            // 获取当前用户ID（如果已认证）
            var currentUserId = GetCurrentUserId();

            var query = new GetFilesQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm ?? string.Empty,
                ContentType = contentType,
                IsPublic = isPublic,
                UploadedBy = uploadedBy,
                UserId = currentUserId,
                OnlyPublic = currentUserId == null || (uploadedBy.HasValue && uploadedBy.Value != currentUserId)
            };

            var result = await _fileApplicationService.GetFilesAsync(query);

            if (result.Success)
            {
                _logger.LogInformation("获取文件列表成功，页码: {Page}, 每页大小: {PageSize}", page, pageSize);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<PagedResult<FileDto>>.CreateSuccess(result.Data!, "获取文件列表成功");
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("获取文件列表失败: {Error}", result.ErrorMessage);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode });
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件列表过程中发生异常");
            var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("获取文件列表过程中发生错误", new List<string> { "GET_FILES_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 获取用户上传的文件列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="page">页码（默认1）</param>
    /// <param name="pageSize">每页大小（默认10）</param>
    /// <param name="contentType">内容类型过滤</param>
    /// <param name="isPublic">是否公开</param>
    /// <returns>分页文件列表</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<PagedResult<FileDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFilesByUser(
        int userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? contentType = null,
        [FromQuery] bool? isPublic = null)
    {
        try
        {
            if (userId <= 0)
            {
                var validationResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateValidationFailure(new List<string> { "用户ID必须大于0" });
                return BadRequest(validationResponse);
            }

            // 获取当前用户ID（如果已认证）
            var currentUserId = GetCurrentUserId();

            var query = new GetFilesByUserQuery
            {
                UserId = userId,
                RequestUserId = currentUserId,
                Page = page,
                PageSize = pageSize,
                ContentType = contentType,
                IsPublic = isPublic
            };

            var result = await _fileApplicationService.GetFilesByUserAsync(query);

            if (result.Success)
            {
                _logger.LogInformation("获取用户文件列表成功，用户ID: {UserId}, 页码: {Page}", userId, page);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<PagedResult<FileDto>>.CreateSuccess(result.Data!, "获取用户文件列表成功");
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("获取用户文件列表失败，用户ID: {UserId}, 错误: {Error}", userId, result.ErrorMessage);
                
                return result.ErrorCode switch
                {
                    "PERMISSION_DENIED" => StatusCode(StatusCodes.Status403Forbidden, BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode })),
                    _ => BadRequest(BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode }))
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户文件列表过程中发生异常，用户ID: {UserId}", userId);
            var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("获取用户文件列表过程中发生错误", new List<string> { "GET_USER_FILES_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 更新文件可见性
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="command">更新文件可见性命令</param>
    /// <returns>更新后的文件信息</returns>
    [HttpPut("{id}/visibility")]
    [Authorize]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<FileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateFileVisibility(int id, [FromBody] UpdateFileVisibilityCommand command)
    {
        try
        {
            if (id <= 0)
            {
                var validationResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateValidationFailure(new List<string> { "文件ID必须大于0" });
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

            // 设置文件ID和用户ID
            command.Id = id;
            command.UserId = currentUserId.Value;

            var result = await _fileApplicationService.UpdateFileVisibilityAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("更新文件可见性成功，文件ID: {FileId}, 用户ID: {UserId}, 公开状态: {IsPublic}", 
                    id, currentUserId.Value, command.IsPublic);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<FileDto>.CreateSuccess(result.Data!, "文件可见性更新成功");
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("更新文件可见性失败，文件ID: {FileId}, 用户ID: {UserId}, 错误: {Error}", 
                    id, currentUserId.Value, result.ErrorMessage);
                
                return result.ErrorCode switch
                {
                    "FILE_NOT_FOUND" => NotFound(BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode })),
                    "PERMISSION_DENIED" => StatusCode(StatusCodes.Status403Forbidden, BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode })),
                    _ => BadRequest(BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode }))
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新文件可见性过程中发生异常，文件ID: {FileId}", id);
            var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("更新文件可见性过程中发生错误", new List<string> { "UPDATE_FILE_VISIBILITY_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BlogApi.Application.DTOs.Common.ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteFile(int id)
    {
        try
        {
            if (id <= 0)
            {
                var validationResponse = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateValidationFailure(new List<string> { "文件ID必须大于0" });
                return BadRequest(validationResponse);
            }

            // 获取当前用户ID
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("用户未认证", new List<string> { "UNAUTHORIZED" });
                return Unauthorized(response);
            }

            var command = new DeleteFileCommand
            {
                Id = id,
                UserId = currentUserId.Value
            };

            var result = await _fileApplicationService.DeleteFileAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("删除文件成功，文件ID: {FileId}, 用户ID: {UserId}", id, currentUserId.Value);
                var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateSuccess(new object(), "文件删除成功");
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("删除文件失败，文件ID: {FileId}, 用户ID: {UserId}, 错误: {Error}", 
                    id, currentUserId.Value, result.ErrorMessage);
                
                return result.ErrorCode switch
                {
                    "FILE_NOT_FOUND" => NotFound(BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode })),
                    "PERMISSION_DENIED" => StatusCode(StatusCodes.Status403Forbidden, BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode })),
                    _ => BadRequest(BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode }))
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除文件过程中发生异常，文件ID: {FileId}", id);
            var response = BlogApi.Application.DTOs.Common.ApiResponse<object>.CreateFailure("删除文件过程中发生错误", new List<string> { "DELETE_FILE_ERROR" });
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