using BlogApi.Application.Commands.File;
using BlogApi.Application.DTOs;
using BlogApi.Application.DTOs.Common;
using BlogApi.Application.Queries.File;
using BlogApi.Domain.Common;
using BlogApi.Domain.Entities;
using BlogApi.Domain.Interfaces;

namespace BlogApi.Application.Services;

/// <summary>
/// 文件应用服务实现
/// </summary>
public class FileApplicationService : IFileApplicationService
{
    private readonly IFileRepository _fileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;

    public FileApplicationService(
        IFileRepository fileRepository,
        IUserRepository userRepository,
        IFileStorageService fileStorageService)
    {
        _fileRepository = fileRepository;
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<OperationResult<FileUploadResultDto>> UploadFileAsync(UploadFileCommand command)
    {
        try
        {
            // 验证用户是否存在且有权限
            var user = await _userRepository.GetByIdAsync(command.UploadedBy);
            if (user == null)
            {
                return OperationResult<FileUploadResultDto>.CreateFailure("用户不存在", "USER_NOT_FOUND");
            }

            if (!user.CanUploadFile())
            {
                return OperationResult<FileUploadResultDto>.CreateFailure("无权限上传文件", "UPLOAD_PERMISSION_DENIED");
            }

            // 验证文件类型
            if (!_fileStorageService.IsFileTypeAllowed(command.FileName, command.ContentType))
            {
                return OperationResult<FileUploadResultDto>.CreateFailure("不支持的文件类型", "UNSUPPORTED_FILE_TYPE");
            }

            // 验证文件大小
            if (!_fileStorageService.IsFileSizeAllowed(command.Size))
            {
                return OperationResult<FileUploadResultDto>.CreateFailure("文件大小超出限制", "FILE_SIZE_EXCEEDED");
            }

            // 生成唯一文件名
            var storedFileName = _fileStorageService.GenerateUniqueFileName(command.FileName);

            // 保存文件到存储系统
            var filePath = await _fileStorageService.SaveFileAsync(command.FileStream, storedFileName, command.ContentType);

            // 创建文件实体
            var fileEntity = new FileEntity
            {
                OriginalName = command.FileName,
                StoredName = storedFileName,
                ContentType = command.ContentType,
                Size = command.Size,
                FilePath = filePath,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = command.UploadedBy,
                IsPublic = command.IsPublic
            };

            var createdFile = await _fileRepository.CreateAsync(fileEntity);

            var result = new FileUploadResultDto
            {
                Id = createdFile.Id,
                OriginalName = createdFile.OriginalName,
                ContentType = createdFile.ContentType,
                Size = createdFile.Size,
                FormattedSize = createdFile.GetFormattedSize(),
                UploadedAt = createdFile.UploadedAt,
                IsPublic = createdFile.IsPublic,
                DownloadUrl = $"/api/files/{createdFile.Id}" // 这里应该根据实际路由配置
            };

            return OperationResult<FileUploadResultDto>.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            return OperationResult<FileUploadResultDto>.CreateFailure("文件上传过程中发生错误", "UPLOAD_ERROR");
        }
    }

    public async Task<OperationResult<FileDownloadResultDto>> GetFileAsync(GetFileQuery query)
    {
        try
        {
            var file = await _fileRepository.GetByIdAsync(query.Id);
            if (file == null)
            {
                return OperationResult<FileDownloadResultDto>.CreateFailure("文件不存在", "FILE_NOT_FOUND");
            }

            // 检查访问权限
            if (!file.CanBeAccessedBy(query.UserId))
            {
                return OperationResult<FileDownloadResultDto>.CreateFailure("无权限访问此文件", "ACCESS_DENIED");
            }

            // 检查文件是否存在于存储系统中
            if (!await _fileStorageService.FileExistsAsync(file.FilePath))
            {
                return OperationResult<FileDownloadResultDto>.CreateFailure("文件不存在", "FILE_NOT_FOUND_IN_STORAGE");
            }

            // 获取文件流
            var fileStream = await _fileStorageService.GetFileStreamAsync(file.FilePath);

            var result = new FileDownloadResultDto
            {
                FileStream = fileStream,
                FileName = file.OriginalName,
                ContentType = file.ContentType,
                Size = file.Size
            };

            return OperationResult<FileDownloadResultDto>.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            return OperationResult<FileDownloadResultDto>.CreateFailure("获取文件过程中发生错误", "GET_FILE_ERROR");
        }
    }

    public async Task<OperationResult<PagedResult<FileDto>>> GetFilesAsync(GetFilesQuery query)
    {
        try
        {
            var parameters = new FileQueryParameters
            {
                Page = query.Page,
                PageSize = query.PageSize,
                SearchTerm = query.SearchTerm,
                ContentType = query.ContentType,
                IsPublic = query.IsPublic,
                UploadedBy = query.UploadedBy,
                UploadedAfter = query.UploadedAfter,
                UploadedBefore = query.UploadedBefore
            };

            var pagedResult = await _fileRepository.GetPagedAsync(parameters);
            
            var fileDtos = pagedResult.Items.Select(MapToFileDto).ToList();
            
            var result = new PagedResult<FileDto>
            {
                Items = fileDtos,
                TotalCount = pagedResult.TotalCount,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize
            };

            return OperationResult<PagedResult<FileDto>>.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            return OperationResult<PagedResult<FileDto>>.CreateFailure("获取文件列表过程中发生错误", "GET_FILES_ERROR");
        }
    }

    public async Task<OperationResult<PagedResult<FileDto>>> GetFilesByUserAsync(GetFilesByUserQuery query)
    {
        try
        {
            // 验证用户是否存在
            var user = await _userRepository.GetByIdAsync(query.UserId);
            if (user == null)
            {
                return OperationResult<PagedResult<FileDto>>.CreateFailure("用户不存在", "USER_NOT_FOUND");
            }

            var parameters = new FileQueryParameters
            {
                Page = query.Page,
                PageSize = query.PageSize,
                SearchTerm = query.SearchTerm,
                UploadedBy = query.UserId,
                ContentType = query.ContentType,
                IsPublic = query.IsPublic
            };

            var pagedResult = await _fileRepository.GetPagedAsync(parameters);
            
            var fileDtos = pagedResult.Items.Select(MapToFileDto).ToList();
            
            var result = new PagedResult<FileDto>
            {
                Items = fileDtos,
                TotalCount = pagedResult.TotalCount,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize
            };

            return OperationResult<PagedResult<FileDto>>.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            return OperationResult<PagedResult<FileDto>>.CreateFailure("获取用户文件列表过程中发生错误", "GET_USER_FILES_ERROR");
        }
    }

    public async Task<OperationResult<FileDto>> UpdateFileVisibilityAsync(UpdateFileVisibilityCommand command)
    {
        try
        {
            var file = await _fileRepository.GetByIdAsync(command.Id);
            if (file == null)
            {
                return OperationResult<FileDto>.CreateFailure("文件不存在", "FILE_NOT_FOUND");
            }

            // 检查权限：只有文件上传者可以修改可见性
            if (file.UploadedBy != command.UserId)
            {
                return OperationResult<FileDto>.CreateFailure("无权限操作此文件", "PERMISSION_DENIED");
            }

            // 更新可见性
            if (command.IsPublic)
            {
                file.MakePublic();
            }
            else
            {
                file.MakePrivate();
            }

            var updatedFile = await _fileRepository.UpdateAsync(file);
            var fileDto = MapToFileDto(updatedFile);

            return OperationResult<FileDto>.CreateSuccess(fileDto);
        }
        catch (Exception ex)
        {
            return OperationResult<FileDto>.CreateFailure("更新文件可见性过程中发生错误", "UPDATE_VISIBILITY_ERROR");
        }
    }

    public async Task<OperationResult> DeleteFileAsync(DeleteFileCommand command)
    {
        try
        {
            var file = await _fileRepository.GetByIdAsync(command.Id);
            if (file == null)
            {
                return OperationResult.CreateFailure("文件不存在", "FILE_NOT_FOUND");
            }

            // 检查权限
            if (!file.CanBeDeletedBy(command.UserId))
            {
                return OperationResult.CreateFailure("无权限删除此文件", "DELETE_PERMISSION_DENIED");
            }

            // 从存储系统中删除文件
            var storageDeleted = await _fileStorageService.DeleteFileAsync(file.FilePath);
            if (!storageDeleted)
            {
                // 继续删除数据库记录，即使存储文件删除失败
            }

            // 从数据库中删除文件记录
            var deleted = await _fileRepository.DeleteAsync(command.Id);
            if (!deleted)
            {
                return OperationResult.CreateFailure("删除文件失败", "DELETE_FAILED");
            }

            return OperationResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            return OperationResult.CreateFailure("删除文件过程中发生错误", "DELETE_FILE_ERROR");
        }
    }

    public async Task<OperationResult<bool>> ValidateFilePermissionAsync(int fileId, int userId)
    {
        try
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                return OperationResult<bool>.CreateFailure("文件不存在", "FILE_NOT_FOUND");
            }

            var hasPermission = file.CanBeDeletedBy(userId); // 使用删除权限作为操作权限
            
            return OperationResult<bool>.CreateSuccess(hasPermission);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.CreateFailure("验证文件权限过程中发生错误", "VALIDATE_PERMISSION_ERROR");
        }
    }

    public async Task<OperationResult<bool>> ValidatePublicAccessAsync(int fileId)
    {
        try
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                return OperationResult<bool>.CreateFailure("文件不存在", "FILE_NOT_FOUND");
            }

            var isPublicAccessible = file.IsPublic;
            
            return OperationResult<bool>.CreateSuccess(isPublicAccessible);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.CreateFailure("验证文件公开访问过程中发生错误", "VALIDATE_PUBLIC_ACCESS_ERROR");
        }
    }

    private static FileDto MapToFileDto(FileEntity file)
    {
        return new FileDto
        {
            Id = file.Id,
            OriginalName = file.OriginalName,
            ContentType = file.ContentType,
            Size = file.Size,
            FormattedSize = file.GetFormattedSize(),
            UploadedAt = file.UploadedAt,
            IsPublic = file.IsPublic,
            Uploader = new UserSummaryDto
            {
                Id = file.Uploader.Id,
                Username = file.Uploader.Username
            }
        };
    }
}