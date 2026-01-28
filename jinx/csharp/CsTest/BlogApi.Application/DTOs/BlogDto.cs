namespace BlogApi.Application.DTOs;

/// <summary>
/// 博客数据传输对象
/// </summary>
public class BlogDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserSummaryDto Author { get; set; } = null!;
}

/// <summary>
/// 博客列表项DTO（不包含完整内容）
/// </summary>
public class BlogListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserSummaryDto Author { get; set; } = null!;
}