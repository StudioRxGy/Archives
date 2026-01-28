using BlogApi.Domain.Common;

namespace BlogApi.Application.DTOs.Common;

/// <summary>
/// 分页响应DTO
/// </summary>
/// <typeparam name="T">数据项类型</typeparam>
public class PagedResponse<T> : ApiResponse<PagedResult<T>>
{
    /// <summary>
    /// 分页信息
    /// </summary>
    public PaginationInfo Pagination { get; set; } = null!;

    /// <summary>
    /// 从分页结果创建分页响应
    /// </summary>
    /// <param name="pagedResult">分页结果</param>
    /// <param name="message">响应消息</param>
    /// <returns>分页响应</returns>
    public static new PagedResponse<T> CreateSuccess(PagedResult<T> pagedResult, string message = "获取成功")
    {
        return new PagedResponse<T>
        {
            Success = true,
            Message = message,
            Data = pagedResult,
            Pagination = new PaginationInfo
            {
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount,
                TotalPages = pagedResult.TotalPages,
                HasNextPage = pagedResult.HasNextPage,
                HasPreviousPage = pagedResult.HasPreviousPage
            }
        };
    }
}

/// <summary>
/// 分页信息
/// </summary>
public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}