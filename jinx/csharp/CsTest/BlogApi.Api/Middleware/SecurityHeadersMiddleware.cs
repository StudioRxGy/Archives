namespace BlogApi.Api.Middleware;

/// <summary>
/// 安全头中间件
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 添加安全头
        AddSecurityHeaders(context.Response);

        await _next(context);
    }

    private void AddSecurityHeaders(HttpResponse response)
    {
        // X-Content-Type-Options: 防止MIME类型嗅探
        if (!response.Headers.ContainsKey("X-Content-Type-Options"))
        {
            response.Headers["X-Content-Type-Options"] = "nosniff";
        }

        // X-Frame-Options: 防止点击劫持
        if (!response.Headers.ContainsKey("X-Frame-Options"))
        {
            response.Headers["X-Frame-Options"] = "DENY";
        }

        // X-XSS-Protection: 启用XSS过滤
        if (!response.Headers.ContainsKey("X-XSS-Protection"))
        {
            response.Headers["X-XSS-Protection"] = "1; mode=block";
        }

        // Referrer-Policy: 控制引用信息
        if (!response.Headers.ContainsKey("Referrer-Policy"))
        {
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }

        // Content-Security-Policy: 内容安全策略
        if (!response.Headers.ContainsKey("Content-Security-Policy"))
        {
            var csp = "default-src 'self'; " +
                     "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                     "style-src 'self' 'unsafe-inline'; " +
                     "img-src 'self' data: https:; " +
                     "font-src 'self'; " +
                     "connect-src 'self'; " +
                     "media-src 'self'; " +
                     "object-src 'none'; " +
                     "child-src 'none'; " +
                     "frame-ancestors 'none'; " +
                     "form-action 'self'; " +
                     "base-uri 'self'";
            
            response.Headers["Content-Security-Policy"] = csp;
        }

        // Strict-Transport-Security: 强制HTTPS
        if (!response.Headers.ContainsKey("Strict-Transport-Security"))
        {
            response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // Permissions-Policy: 权限策略
        if (!response.Headers.ContainsKey("Permissions-Policy"))
        {
            var permissionsPolicy = "camera=(), " +
                                   "microphone=(), " +
                                   "geolocation=(), " +
                                   "interest-cohort=(), " +
                                   "payment=(), " +
                                   "usb=()";
            
            response.Headers["Permissions-Policy"] = permissionsPolicy;
        }

        // Cache-Control: 缓存控制
        if (!response.Headers.ContainsKey("Cache-Control"))
        {
            response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        }

        // Pragma: HTTP/1.0 缓存控制
        if (!response.Headers.ContainsKey("Pragma"))
        {
            response.Headers["Pragma"] = "no-cache";
        }

        // X-Permitted-Cross-Domain-Policies: 跨域策略
        if (!response.Headers.ContainsKey("X-Permitted-Cross-Domain-Policies"))
        {
            response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
        }

        // 移除可能泄露服务器信息的头
        response.Headers.Remove("Server");
        response.Headers.Remove("X-Powered-By");
        response.Headers.Remove("X-AspNet-Version");
        response.Headers.Remove("X-AspNetMvc-Version");
    }
}