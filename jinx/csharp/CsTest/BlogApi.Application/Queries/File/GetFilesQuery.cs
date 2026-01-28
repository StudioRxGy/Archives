using BlogApi.Domain.Common;

namespace BlogApi.Application.Queries.File;

/// <summary>
/// 获取文件列表查询
/// </summary>
public class GetFilesQuery : FileQueryParameters
{
    /// <summary>
    /// 请求用户ID（用于权限过滤）
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 是否只返回公开文件
    /// </summary>
    public bool OnlyPublic { get; set; } = false;
}