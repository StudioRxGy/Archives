namespace BlogApi.Domain.Common;

/// <summary>
/// 查询参数基类，包含分页功能
/// </summary>
public abstract class BaseQueryParameters
{
    private int _page = 1;
    private int _pageSize = 10;
    
    /// <summary>
    /// 页码（最小值为1）
    /// </summary>
    public int Page 
    { 
        get => _page; 
        set => _page = value < 1 ? 1 : value; 
    }
    
    /// <summary>
    /// 每页大小（最小值为1，最大值为100）
    /// </summary>
    public int PageSize 
    { 
        get => _pageSize; 
        set => _pageSize = value < 1 ? 10 : value > 100 ? 100 : value; 
    }
    
    /// <summary>
    /// 搜索关键词，用于文本过滤
    /// </summary>
    public string? SearchTerm { get; set; }
}

/// <summary>
/// 博客查询参数
/// </summary>
public class BlogQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// 按发布状态过滤
    /// </summary>
    public bool? IsPublished { get; set; }
    
    /// <summary>
    /// 按作者ID过滤
    /// </summary>
    public int? AuthorId { get; set; }
    
    /// <summary>
    /// 按标签过滤
    /// </summary>
    public List<string>? Tags { get; set; }
    
    /// <summary>
    /// 按创建日期过滤（此日期之后）
    /// </summary>
    public DateTime? CreatedAfter { get; set; }
    
    /// <summary>
    /// 按创建日期过滤（此日期之前）
    /// </summary>
    public DateTime? CreatedBefore { get; set; }
}

/// <summary>
/// 文件查询参数
/// </summary>
public class FileQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// 按上传者用户ID过滤
    /// </summary>
    public int? UploadedBy { get; set; }
    
    /// <summary>
    /// 按内容类型过滤
    /// </summary>
    public string? ContentType { get; set; }
    
    /// <summary>
    /// 按公开/私有状态过滤
    /// </summary>
    public bool? IsPublic { get; set; }
    
    /// <summary>
    /// 按上传日期过滤（此日期之后）
    /// </summary>
    public DateTime? UploadedAfter { get; set; }
    
    /// <summary>
    /// 按上传日期过滤（此日期之前）
    /// </summary>
    public DateTime? UploadedBefore { get; set; }
}