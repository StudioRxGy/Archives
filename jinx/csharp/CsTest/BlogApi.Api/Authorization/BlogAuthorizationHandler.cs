using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BlogApi.Api.Authorization;

/// <summary>
/// 博客授权处理器
/// </summary>
public class BlogAuthorizationHandler : AuthorizationHandler<BlogOperationRequirement, BlogResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BlogOperationRequirement requirement,
        BlogResource resource)
    {
        var userId = GetUserId(context.User);
        if (!userId.HasValue)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        switch (requirement.Operation)
        {
            case BlogOperation.Read:
                // 公开博客任何人都可以读取，私有博客只有作者可以读取
                if (resource.IsPublished || resource.AuthorId == userId.Value)
                {
                    context.Succeed(requirement);
                }
                break;

            case BlogOperation.Create:
                // 任何认证用户都可以创建博客
                context.Succeed(requirement);
                break;

            case BlogOperation.Update:
            case BlogOperation.Delete:
                // 只有作者可以更新或删除博客
                if (resource.AuthorId == userId.Value)
                {
                    context.Succeed(requirement);
                }
                break;

            case BlogOperation.Publish:
                // 只有作者可以发布博客
                if (resource.AuthorId == userId.Value)
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
/// 博客操作要求
/// </summary>
public class BlogOperationRequirement : IAuthorizationRequirement
{
    public BlogOperation Operation { get; }

    public BlogOperationRequirement(BlogOperation operation)
    {
        Operation = operation;
    }
}

/// <summary>
/// 博客操作类型
/// </summary>
public enum BlogOperation
{
    Read,
    Create,
    Update,
    Delete,
    Publish
}

/// <summary>
/// 博客资源
/// </summary>
public class BlogResource
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public bool IsPublished { get; set; }
    public string Title { get; set; } = string.Empty;
}