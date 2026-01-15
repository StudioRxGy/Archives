namespace BlogApi.Domain.Common;

/// <summary>
/// 表示分页结果
/// </summary>
/// <typeparam name="T">结果中项目的类型</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 当前页的项目列表
    /// </summary>
    public List<T> Items { get; set; } = new();
    
    /// <summary>
    /// 总项目数
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// 当前页码
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// 每页项目数
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
    
    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => Page > 1;
    
    /// <summary>
    /// 创建空的分页结果
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页项目数</param>
    /// <returns>空的分页结果</returns>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 10)
    {
        return new PagedResult<T>
        {
            Items = new List<T>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };
    }
    
    /// <summary>
    /// 从项目列表创建分页结果
    /// </summary>
    /// <param name="items">项目列表</param>
    /// <param name="totalCount">总项目数</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页项目数</param>
    /// <returns>分页结果</returns>
    public static PagedResult<T> Create(List<T> items, int totalCount, int page, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}