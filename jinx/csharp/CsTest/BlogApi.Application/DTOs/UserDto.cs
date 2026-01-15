namespace BlogApi.Application.DTOs;

/// <summary>
/// 用户数据传输对象
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// 用户简要信息DTO（用于博客作者等场景）
/// </summary>
public class UserSummaryDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
}