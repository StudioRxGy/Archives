using Microsoft.EntityFrameworkCore;
using BlogApi.Domain.Entities;

namespace BlogApi.Infrastructure.Data;

public class BlogDbContext : DbContext
{
    public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<FileEntity> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            
            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(u => u.PasswordHash)
                .IsRequired();
            
            entity.Property(u => u.CreatedAt)
                .IsRequired();
            
            entity.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Create unique indexes
            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");
            
            entity.HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("IX_Users_Username");
        });

        // Configure Blog entity
        modelBuilder.Entity<Blog>(entity =>
        {
            entity.HasKey(b => b.Id);
            
            entity.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(b => b.Content)
                .IsRequired()
                .HasColumnType("LONGTEXT");
            
            entity.Property(b => b.Summary)
                .HasMaxLength(500);
            
            entity.Property(b => b.Tags)
                .IsRequired()
                .HasDefaultValue("[]")
                .HasColumnType("JSON");
            
            entity.Property(b => b.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);
            
            entity.Property(b => b.CreatedAt)
                .IsRequired();
            
            entity.Property(b => b.UpdatedAt)
                .IsRequired();
            
            entity.Property(b => b.AuthorId)
                .IsRequired();

            // Configure relationship with User
            entity.HasOne(b => b.Author)
                .WithMany(u => u.Blogs)
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Blogs_Users_AuthorId");

            // Create indexes for better query performance
            entity.HasIndex(b => b.AuthorId)
                .HasDatabaseName("IX_Blogs_AuthorId");
            
            entity.HasIndex(b => b.IsPublished)
                .HasDatabaseName("IX_Blogs_IsPublished");
            
            entity.HasIndex(b => b.CreatedAt)
                .HasDatabaseName("IX_Blogs_CreatedAt");
        });

        // Configure FileEntity
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.HasKey(f => f.Id);
            
            entity.Property(f => f.OriginalName)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(f => f.StoredName)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(f => f.ContentType)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(f => f.Size)
                .IsRequired();
            
            entity.Property(f => f.FilePath)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(f => f.UploadedAt)
                .IsRequired();
            
            entity.Property(f => f.UploadedBy)
                .IsRequired();
            
            entity.Property(f => f.IsPublic)
                .IsRequired()
                .HasDefaultValue(false);

            // Configure relationship with User
            entity.HasOne(f => f.Uploader)
                .WithMany(u => u.Files)
                .HasForeignKey(f => f.UploadedBy)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Files_Users_UploadedBy");

            // Create indexes for better query performance
            entity.HasIndex(f => f.UploadedBy)
                .HasDatabaseName("IX_Files_UploadedBy");
            
            entity.HasIndex(f => f.IsPublic)
                .HasDatabaseName("IX_Files_IsPublic");
            
            entity.HasIndex(f => f.UploadedAt)
                .HasDatabaseName("IX_Files_UploadedAt");
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This will be overridden by dependency injection configuration
            // For design-time, use a simple connection string without auto-detection
            optionsBuilder.UseMySql(
                "Server=localhost;Database=BlogApiDb;Uid=root;Pwd=password;",
                new MySqlServerVersion(new Version(8, 0, 21))
            );
        }
    }
}