using BlogApi.Api.Extensions;
using BlogApi.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Database
builder.Services.AddDatabase(builder.Configuration);

// Configure Authentication & Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);

// Configure Application Services
builder.Services.AddApplicationServices();

// Configure Infrastructure Services
builder.Services.AddInfrastructureServices();

// Configure Validation
builder.Services.AddValidation();

// Configure Security
builder.Services.AddSecurity(builder.Configuration);

// Configure Swagger Documentation
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blog API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

// Add Security Headers Middleware (should be early in pipeline)
app.UseMiddleware<SecurityHeadersMiddleware>();

// Add Rate Limiting Middleware
app.UseMiddleware<RateLimitingMiddleware>();

// Add Global Exception Handling Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

app.Run();
