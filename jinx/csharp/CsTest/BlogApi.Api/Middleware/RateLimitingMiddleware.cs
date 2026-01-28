using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Options;

namespace BlogApi.Api.Middleware;

/// <summary>
/// 速率限制中间件
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;
    
    // 存储客户端请求记录
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
    
    // 清理任务的取消令牌
    private static readonly Timer _cleanupTimer = new(CleanupExpiredEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IOptions<RateLimitOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var endpoint = GetEndpointIdentifier(context);
        
        if (await IsRateLimitExceededAsync(clientId, endpoint))
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}", clientId, endpoint);
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = _options.WindowSizeInSeconds.ToString();
            
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // 优先使用用户ID（如果已认证）
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }
        }

        // 使用IP地址
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            return $"ip:{ipAddress}";
        }

        // 回退到连接ID
        return $"conn:{context.Connection.Id}";
    }

    private string GetEndpointIdentifier(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        
        // 对于某些敏感端点使用更严格的限制
        if (IsSensitiveEndpoint(path))
        {
            return $"sensitive:{method}:{path}";
        }

        // 对于认证端点使用特殊标识
        if (IsAuthEndpoint(path))
        {
            return $"auth:{method}:{path}";
        }

        return $"general:{method}";
    }

    private bool IsSensitiveEndpoint(string path)
    {
        var sensitivePaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/files/upload"
        };

        return sensitivePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsAuthEndpoint(string path)
    {
        return path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> IsRateLimitExceededAsync(string clientId, string endpoint)
    {
        var key = $"{clientId}:{endpoint}";
        var now = DateTimeOffset.UtcNow;
        
        var clientInfo = _clients.AddOrUpdate(key, 
            new ClientRequestInfo { LastRequestTime = now, RequestCount = 1 },
            (k, existing) =>
            {
                // 如果时间窗口已过期，重置计数
                if (now - existing.LastRequestTime > TimeSpan.FromSeconds(_options.WindowSizeInSeconds))
                {
                    existing.RequestCount = 1;
                    existing.LastRequestTime = now;
                }
                else
                {
                    existing.RequestCount++;
                    existing.LastRequestTime = now;
                }
                return existing;
            });

        var limit = GetLimitForEndpoint(endpoint);
        return clientInfo.RequestCount > limit;
    }

    private int GetLimitForEndpoint(string endpoint)
    {
        if (endpoint.StartsWith("sensitive:"))
        {
            return _options.SensitiveEndpointLimit;
        }
        
        if (endpoint.StartsWith("auth:"))
        {
            return _options.AuthEndpointLimit;
        }

        return _options.GeneralEndpointLimit;
    }

    private static void CleanupExpiredEntries(object? state)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-10); // 清理10分钟前的记录
        var expiredKeys = _clients
            .Where(kvp => kvp.Value.LastRequestTime < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _clients.TryRemove(key, out _);
        }
    }
}

/// <summary>
/// 客户端请求信息
/// </summary>
public class ClientRequestInfo
{
    public DateTimeOffset LastRequestTime { get; set; }
    public int RequestCount { get; set; }
}

/// <summary>
/// 速率限制选项
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// 时间窗口大小（秒）
    /// </summary>
    public int WindowSizeInSeconds { get; set; } = 60;

    /// <summary>
    /// 一般端点限制（每分钟请求数）
    /// </summary>
    public int GeneralEndpointLimit { get; set; } = 100;

    /// <summary>
    /// 认证端点限制（每分钟请求数）
    /// </summary>
    public int AuthEndpointLimit { get; set; } = 10;

    /// <summary>
    /// 敏感端点限制（每分钟请求数）
    /// </summary>
    public int SensitiveEndpointLimit { get; set; } = 5;
}