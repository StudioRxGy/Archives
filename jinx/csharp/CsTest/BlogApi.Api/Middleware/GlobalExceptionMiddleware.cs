using System.Net;
using System.Text.Json;
using BlogApi.Domain.Common;

namespace BlogApi.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "An error occurred while processing your request.",
            Data = null,
            Errors = new List<string>()
        };

        switch (exception)
        {
            case BusinessException businessEx:
                response.Message = businessEx.Message;
                response.Errors.Add(businessEx.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;
                
            case ValidationException validationEx:
                response.Message = "Validation failed";
                response.Errors.AddRange(validationEx.Errors);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;
                
            case UnauthorizedException unauthorizedEx:
                response.Message = unauthorizedEx.Message;
                response.Errors.Add(unauthorizedEx.Message);
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;
                
            case NotFoundException notFoundEx:
                response.Message = notFoundEx.Message;
                response.Errors.Add(notFoundEx.Message);
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;
                
            default:
                response.Message = "An internal server error occurred";
                response.Errors.Add("Internal server error");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}