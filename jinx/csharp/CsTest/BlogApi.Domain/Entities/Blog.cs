using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace BlogApi.Domain.Entities;

public class Blog
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty; // Markdown content
    
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;
    
    public string Tags { get; set; } = "[]"; // JSON format for storing tags
    
    public bool IsPublished { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    [Required]
    public int AuthorId { get; set; }
    
    // Navigation property
    public User Author { get; set; } = null!;
    
    // Business rules and methods
    public void Publish()
    {
        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Content))
        {
            throw new InvalidOperationException("Cannot publish blog without title and content");
        }
        
        IsPublished = true;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Unpublish()
    {
        IsPublished = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateContent(string title, string content, string summary)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));
        
        Title = title;
        Content = content;
        Summary = summary;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetTags(List<string> tags)
    {
        Tags = JsonSerializer.Serialize(tags);
        UpdatedAt = DateTime.UtcNow;
    }
    
    public List<string> GetTags()
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
    
    public bool CanBeEditedBy(int userId)
    {
        return AuthorId == userId;
    }
    
    public bool CanBeDeletedBy(int userId)
    {
        return AuthorId == userId;
    }
}