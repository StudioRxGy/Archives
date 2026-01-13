using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BlogApi.Infrastructure.Data;

public class BlogDbContextFactory : IDesignTimeDbContextFactory<BlogDbContext>
{
    public BlogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BlogDbContext>();
        
        // Use a design-time connection string that doesn't require an actual database connection
        optionsBuilder.UseMySql(
            "Server=localhost;Database=BlogApiDb;Uid=root;Pwd=password;",
            new MySqlServerVersion(new Version(8, 0, 21)),
            options => options.MigrationsAssembly("BlogApi.Infrastructure")
        );

        return new BlogDbContext(optionsBuilder.Options);
    }
}