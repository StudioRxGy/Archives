using Microsoft.EntityFrameworkCore;
using BlogApi.Infrastructure.Data;
using BlogApi.Domain.Entities;

namespace BlogApi.Infrastructure.Tests.TestHelpers;

/// <summary>
/// 测试数据库上下文工厂，用于创建内存数据库
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// 创建内存数据库上下文
    /// </summary>
    public static BlogDbContext CreateInMemoryContext(string databaseName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var context = new BlogDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// 为测试数据库添加种子数据
    /// </summary>
    public static void SeedTestData(BlogDbContext context)
    {
        // 添加测试用户
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                Username = "testuser1",
                Email = "test1@example.com",
                PasswordHash = "hashedpassword1",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                IsActive = true
            },
            new User
            {
                Id = 2,
                Username = "testuser2",
                Email = "test2@example.com",
                PasswordHash = "hashedpassword2",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                IsActive = true
            }
        };

        context.Users.AddRange(users);

        // 添加测试博客
        var blogs = new List<Blog>
        {
            new Blog
            {
                Id = 1,
                Title = "Test Blog 1",
                Content = "This is test content for blog 1",
                Summary = "Test summary 1",
                Tags = "[\"tag1\", \"tag2\"]",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10),
                AuthorId = 1
            },
            new Blog
            {
                Id = 2,
                Title = "Test Blog 2",
                Content = "This is test content for blog 2",
                Summary = "Test summary 2",
                Tags = "[\"tag2\", \"tag3\"]",
                IsPublished = false,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                AuthorId = 2
            }
        };

        context.Blogs.AddRange(blogs);

        // 添加测试文件
        var files = new List<FileEntity>
        {
            new FileEntity
            {
                Id = 1,
                OriginalName = "test1.jpg",
                StoredName = "stored_test1.jpg",
                ContentType = "image/jpeg",
                Size = 1024,
                FilePath = "/uploads/stored_test1.jpg",
                UploadedAt = DateTime.UtcNow.AddDays(-7),
                UploadedBy = 1,
                IsPublic = true
            },
            new FileEntity
            {
                Id = 2,
                OriginalName = "test2.pdf",
                StoredName = "stored_test2.pdf",
                ContentType = "application/pdf",
                Size = 2048,
                FilePath = "/uploads/stored_test2.pdf",
                UploadedAt = DateTime.UtcNow.AddDays(-3),
                UploadedBy = 2,
                IsPublic = false
            }
        };

        context.Files.AddRange(files);
        context.SaveChanges();
    }
}