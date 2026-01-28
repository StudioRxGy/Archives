using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using FluentValidation.AspNetCore;
using BlogApi.Application.Services;
using BlogApi.Application.Validators.Auth;
using BlogApi.Application.Validators.Blog;
using BlogApi.Application.Validators.File;
using BlogApi.Domain.Interfaces;
using BlogApi.Infrastructure.Data;
using BlogApi.Infrastructure.Repositories;
using BlogApi.Infrastructure.Services;
using BlogApi.Api.Authorization;
using BlogApi.Api.Middleware;

namespace BlogApi.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application Services
        services.AddScoped<IAuthApplicationService, AuthApplicationService>();
        services.AddScoped<IBlogApplicationService, BlogApplicationService>();
        services.AddScoped<IFileApplicationService, FileApplicationService>();

        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        // Add FluentValidation
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();

        // Register validators
        services.AddScoped<IValidator<BlogApi.Application.Commands.Auth.RegisterCommand>, RegisterCommandValidator>();
        services.AddScoped<IValidator<BlogApi.Application.Commands.Auth.LoginCommand>, LoginCommandValidator>();
        services.AddScoped<IValidator<BlogApi.Application.Commands.Auth.RefreshTokenCommand>, RefreshTokenCommandValidator>();
        services.AddScoped<IValidator<BlogApi.Application.Commands.Blog.CreateBlogCommand>, CreateBlogCommandValidator>();
        services.AddScoped<IValidator<BlogApi.Application.Commands.Blog.UpdateBlogCommand>, UpdateBlogCommandValidator>();
        services.AddScoped<IValidator<BlogApi.Application.Commands.File.UploadFileCommand>, UploadFileCommandValidator>();

        return services;
    }

    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Rate limiting configuration
        services.Configure<RateLimitOptions>(options =>
        {
            options.WindowSizeInSeconds = configuration.GetValue<int>("RateLimit:WindowSizeInSeconds", 60);
            options.GeneralEndpointLimit = configuration.GetValue<int>("RateLimit:GeneralEndpointLimit", 100);
            options.AuthEndpointLimit = configuration.GetValue<int>("RateLimit:AuthEndpointLimit", 10);
            options.SensitiveEndpointLimit = configuration.GetValue<int>("RateLimit:SensitiveEndpointLimit", 5);
        });

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Domain Services
        services.AddScoped<IPasswordHashingService, BCryptPasswordHashingService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        
        // External Services
        services.AddScoped<IMarkdownService, MarkdownService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ISecurityService, SecurityService>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBlogRepository, BlogRepository>();
        services.AddScoped<IFileRepository, FileRepository>();

        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BlogDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
                ClockSkew = TimeSpan.Zero
            };
        });

        // Add Authorization with custom policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("BlogRead", policy => 
                policy.Requirements.Add(new BlogOperationRequirement(BlogOperation.Read)));
            options.AddPolicy("BlogCreate", policy => 
                policy.Requirements.Add(new BlogOperationRequirement(BlogOperation.Create)));
            options.AddPolicy("BlogUpdate", policy => 
                policy.Requirements.Add(new BlogOperationRequirement(BlogOperation.Update)));
            options.AddPolicy("BlogDelete", policy => 
                policy.Requirements.Add(new BlogOperationRequirement(BlogOperation.Delete)));
            options.AddPolicy("BlogPublish", policy => 
                policy.Requirements.Add(new BlogOperationRequirement(BlogOperation.Publish)));

            options.AddPolicy("FileRead", policy => 
                policy.Requirements.Add(new FileOperationRequirement(FileOperation.Read)));
            options.AddPolicy("FileUpload", policy => 
                policy.Requirements.Add(new FileOperationRequirement(FileOperation.Upload)));
            options.AddPolicy("FileUpdate", policy => 
                policy.Requirements.Add(new FileOperationRequirement(FileOperation.Update)));
            options.AddPolicy("FileDelete", policy => 
                policy.Requirements.Add(new FileOperationRequirement(FileOperation.Delete)));
            options.AddPolicy("FileChangeVisibility", policy => 
                policy.Requirements.Add(new FileOperationRequirement(FileOperation.ChangeVisibility)));
        });

        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, BlogAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, FileAuthorizationHandler>();

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }
}