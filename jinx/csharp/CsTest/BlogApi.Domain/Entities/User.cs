using System.ComponentModel.DataAnnotations;

namespace BlogApi.Domain.Entities;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? LastLoginAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Blog> Blogs { get; set; } = new List<Blog>();
    public ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
    
    // Business rules
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
    
    public void Deactivate()
    {
        IsActive = false;
    }
    
    public void Activate()
    {
        IsActive = true;
    }
    
    public bool CanCreateBlog()
    {
        return IsActive;
    }
    
    public bool CanUploadFile()
    {
        return IsActive;
    }
}