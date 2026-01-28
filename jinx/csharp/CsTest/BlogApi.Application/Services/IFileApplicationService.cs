using BlogApi.Application.Commands.File;
using BlogApi.Application.DTOs;
using BlogApi.Application.DTOs.Common;
using BlogApi.Application.Queries.File;
using BlogApi.Domain.Common;

namespace BlogApi.Application.Services;

/// <summary>
/// 文件应用服务接口
/// </summary>
public interface IFileApplicationService
{
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="command">上传文件命令</param>
    /// <returns>文件上传结果</returns>
    Task<OperationResult<FileUploadResultDto>> UploadFileAsync(UploadFileCommand command);

    /// <summary>
    /// 获取文件信息和下载流
    /// </summary>
    /// <param name="query">获取文件查询</param>
    /// <returns>文件下载结果</returns>
    Task<OperationResult<FileDownloadResultDto>> GetFileAsync(GetFileQuery query);

    /// <summary>
    /// 获取文件列表（支持分页）
    /// </summary>
    /// <param name="query">获取文件列表查询</param>
    /// <returns>分页文件列表</returns>
    Task<OperationResult<PagedResult<FileDto>>> GetFilesAsync(GetFilesQuery query);

    /// <summary>
    /// 获取用户上传的文件列表
    /// </summary>
    /// <param name="query">获取用户文件查询</param>
    /// <returns>分页文件列表</returns>
    Task<OperationResult<PagedResult<FileDto>>> GetFilesByUserAsync(GetFilesByUserQuery query);

    /// <summary>
    /// 更新文件可见性
    /// </summary>
    /// <param name="command">更新文件可见性命令</param>
    /// <returns>操作结果</returns>
    Task<OperationResult<FileDto>> UpdateFileVisibilityAsync(UpdateFileVisibilityCommand command);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="command">删除文件命令</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> DeleteFileAsync(DeleteFileCommand command);

    /// <summary>
    /// 验证用户是否有权限操作指定文件
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>验证结果</returns>
    Task<OperationResult<bool>> ValidateFilePermissionAsync(int fileId, int userId);

    /// <summary>
    /// 验证文件是否可以公开访问
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>验证结果</returns>
    Task<OperationResult<bool>> ValidatePublicAccessAsync(int fileId);
}