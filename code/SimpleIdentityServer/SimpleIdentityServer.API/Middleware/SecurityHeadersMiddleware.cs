using Microsoft.Extensions.Primitives;

namespace SimpleIdentityServer.API.Middleware;

/// <summary>
/// Middleware to add security headers to all responses
/// Implements OWASP security header recommendations
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context);

        await _next(context);

        // Log security header application
        _logger.LogDebug("Security headers applied to response for {Path}", context.Request.Path);
    }

    private static void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent clickjacking attacks
        headers.Append("X-Frame-Options", "DENY");

        // Prevent MIME type sniffing
        headers.Append("X-Content-Type-Options", "nosniff");

        // Enable XSS protection in browsers
        headers.Append("X-XSS-Protection", "1; mode=block");

        // Referrer policy - only send referrer for same origin
        headers.Append("Referrer-Policy", "same-origin");

        // Content Security Policy - strict policy for API
        var cspPolicy = "default-src 'none'; " +
                       "script-src 'none'; " +
                       "style-src 'none'; " +
                       "img-src 'none'; " +
                       "font-src 'none'; " +
                       "connect-src 'self'; " +
                       "frame-ancestors 'none'; " +
                       "base-uri 'none'; " +
                       "form-action 'none'";
        headers.Append("Content-Security-Policy", cspPolicy);

        // Permissions policy - disable unnecessary browser features
        var permissionsPolicy = "accelerometer=(), " +
                               "ambient-light-sensor=(), " +
                               "autoplay=(), " +
                               "battery=(), " +
                               "camera=(), " +
                               "cross-origin-isolated=(), " +
                               "display-capture=(), " +
                               "document-domain=(), " +
                               "encrypted-media=(), " +
                               "execution-while-not-rendered=(), " +
                               "execution-while-out-of-viewport=(), " +
                               "fullscreen=(), " +
                               "geolocation=(), " +
                               "gyroscope=(), " +
                               "magnetometer=(), " +
                               "microphone=(), " +
                               "midi=(), " +
                               "navigation-override=(), " +
                               "payment=(), " +
                               "picture-in-picture=(), " +
                               "publickey-credentials-get=(), " +
                               "screen-wake-lock=(), " +
                               "sync-xhr=(), " +
                               "usb=(), " +
                               "web-share=(), " +
                               "xr-spatial-tracking=()";
        headers.Append("Permissions-Policy", permissionsPolicy);

        // Strict Transport Security - enforce HTTPS
        if (context.Request.IsHttps)
        {
            headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        }

        // Cross-Origin policies for additional security
        headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
        headers.Append("Cross-Origin-Opener-Policy", "same-origin");
        headers.Append("Cross-Origin-Resource-Policy", "same-origin");

        // Cache control for sensitive endpoints
        if (IsSensitiveEndpoint(context.Request.Path))
        {
            headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
            headers.Append("Pragma", "no-cache");
            headers.Append("Expires", "0");
        }

        // Remove server information disclosure
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
        headers.Remove("X-AspNetMvc-Version");
    }

    private static bool IsSensitiveEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        
        return pathValue.Contains("/connect/token") ||
               pathValue.Contains("/connect/introspect") ||
               pathValue.Contains("/.well-known") ||
               pathValue.Contains("/api");
    }
}
