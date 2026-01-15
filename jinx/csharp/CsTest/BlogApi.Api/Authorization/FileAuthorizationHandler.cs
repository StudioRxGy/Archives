using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BlogApi.Api.Authorization;

/// <summary>
/// 文件授权处理器
/// </summary>
public class FileAuthorizationHandler : AuthorizationHandler<FileOperationRequirement, FileResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FileOperationRequirement requirement,
        FileResource resource)
    {
        var userId = GetUserId(context.User);
        if (!userId.HasValue)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        switch (requirement.Operation)
        {
            case FileOperation.Read:
                // 公开文件任何人都可以读取，私有文件只有上传者可以读取
                if (resource.IsPublic || resource.UploadedBy == userId.Value)
                {
                    context.Succeed(requirement);
                }
                break;

            case FileOperation.Upload:
                // 任何认证用户都可以上传文件
                context.Succeed(requirement);
                break;

            case FileOperation.Update:
            case FileOperation.Delete:
                // 只有上传者可以更新或删除文件
                if (resource.UploadedBy == userId.Value)
                {
                    context.Succeed(requirement);
                }
                break;

            case FileOperation.ChangeVisibility:
                // 只有上传者可以更改文件可见性
                if (resource.UploadedBy == userId.Value)
                {
                    context.Succeed(requirement);
                }
                break;
        }

        return Task.CompletedTask;
    }

    private int? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

/// <summary>
/// 文件操作要求
/// </summary>
public class FileOperationRequirement : IAuthorizationRequirement
{
    public FileOperation Operation { get; }

    public FileOperationRequirement(FileOperation operation)
    {
        Operation = operation;
    }
}

/// <summary>
/// 文件操作类型
/// </summary>
public enum FileOperation
{
    Read,
    Upload,
    Update,
    Delete,
    ChangeVisibility
}

/// <summary>
/// 文件资源
/// </summary>
public class FileResource
{
    public int Id { get; set; }
    public int UploadedBy { get; set; }
    public bool IsPublic { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}