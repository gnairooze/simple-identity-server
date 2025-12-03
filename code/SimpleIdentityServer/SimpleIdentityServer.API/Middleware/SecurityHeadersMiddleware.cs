using System.Security.Cryptography;

namespace SimpleIdentityServer.API.Middleware;

/// <summary>
/// Middleware to add security headers to all responses
/// Implements OWASP security header recommendations with appropriate CSP
/// policies for both API endpoints and HTML pages
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    // Item key for storing nonce in HttpContext for use in views
    public const string CspNonceKey = "CspNonce";

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate a unique nonce for this request (used for inline scripts in HTML pages)
        var nonce = GenerateNonce();
        context.Items[CspNonceKey] = nonce;

        // Register callback to add headers just before response starts
        // This allows us to determine CSP based on response content type
        context.Response.OnStarting(() =>
        {
            AddSecurityHeaders(context, nonce);
            return Task.CompletedTask;
        });

        await _next(context);

        _logger.LogDebug("Security headers applied to response for {Path}", context.Request.Path);
    }

    private static string GenerateNonce()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static void AddSecurityHeaders(HttpContext context, string nonce)
    {
        var headers = context.Response.Headers;

        // Prevent clickjacking attacks
        headers["X-Frame-Options"] = "DENY";

        // Prevent MIME type sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Enable XSS protection in browsers (legacy, but still useful for older browsers)
        headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer policy - only send referrer for same origin
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Apply appropriate CSP based on endpoint type
        var cspPolicy = GetContentSecurityPolicy(context, nonce);
        headers["Content-Security-Policy"] = cspPolicy;

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
        headers["Permissions-Policy"] = permissionsPolicy;

        // Strict Transport Security - enforce HTTPS
        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // Cross-Origin policies - use less restrictive policy for HTML pages
        if (IsHtmlPageEndpoint(context))
        {
            // HTML pages need to load same-origin resources
            headers["Cross-Origin-Embedder-Policy"] = "unsafe-none";
            headers["Cross-Origin-Opener-Policy"] = "same-origin-allow-popups";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
        }
        else
        {
            // API endpoints use strict isolation
            headers["Cross-Origin-Embedder-Policy"] = "require-corp";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
        }

        // Cache control for sensitive endpoints
        if (IsSensitiveEndpoint(context.Request.Path))
        {
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";
        }

        // Remove server information disclosure
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
        headers.Remove("X-AspNetMvc-Version");
    }

    private static string GetContentSecurityPolicy(HttpContext context, string nonce)
    {
        // Check if this is an HTML page endpoint (views for login, register, consent, etc.)
        if (IsHtmlPageEndpoint(context))
        {
            // CSP for HTML pages - allows scripts, styles, and forms needed for UI
            // Uses nonce for inline scripts (more secure than 'unsafe-inline')
            return $"default-src 'none'; " +
                   $"script-src 'self' 'nonce-{nonce}'; " +
                   $"style-src 'self' 'unsafe-inline'; " +  // unsafe-inline for styles is generally acceptable
                   $"img-src 'self' data:; " +
                   $"font-src 'self'; " +
                   $"connect-src 'self'; " +
                   $"frame-ancestors 'none'; " +
                   $"base-uri 'self'; " +
                   $"form-action 'self'";
        }
        
        // Check if this is a static file (JS, CSS, images, etc.)
        if (IsStaticFileEndpoint(context))
        {
            // Static files don't need CSP execution permissions
            return "default-src 'none'; " +
                   "frame-ancestors 'none'";
        }

        // Strict CSP for API endpoints - no scripts, styles, or other content
        return "default-src 'none'; " +
               "script-src 'none'; " +
               "style-src 'none'; " +
               "img-src 'none'; " +
               "font-src 'none'; " +
               "connect-src 'self'; " +
               "frame-ancestors 'none'; " +
               "base-uri 'none'; " +
               "form-action 'none'";
    }

    private static bool IsHtmlPageEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        
        // Check response content type if available
        var contentType = context.Response.ContentType?.ToLowerInvariant() ?? string.Empty;
        if (contentType.Contains("text/html"))
        {
            return true;
        }

        // Account pages (login, register, logout, access denied)
        if (path.StartsWith("/account/"))
        {
            return true;
        }

        // Consent page
        if (path.StartsWith("/consent"))
        {
            return true;
        }

        // Error pages
        if (path.StartsWith("/error") || path.StartsWith("/home/error"))
        {
            return true;
        }

        // Home page
        if (path == "/" || path == "/index" || path == "/home")
        {
            return true;
        }

        return false;
    }

    private static bool IsStaticFileEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        
        return path.StartsWith("/js/") ||
               path.StartsWith("/css/") ||
               path.StartsWith("/lib/") ||
               path.StartsWith("/images/") ||
               path.StartsWith("/fonts/") ||
               path.EndsWith(".js") ||
               path.EndsWith(".css") ||
               path.EndsWith(".png") ||
               path.EndsWith(".jpg") ||
               path.EndsWith(".jpeg") ||
               path.EndsWith(".gif") ||
               path.EndsWith(".svg") ||
               path.EndsWith(".ico") ||
               path.EndsWith(".woff") ||
               path.EndsWith(".woff2");
    }

    private static bool IsSensitiveEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        
        return pathValue.Contains("/connect/token") ||
               pathValue.Contains("/connect/introspect") ||
               pathValue.Contains("/.well-known") ||
               pathValue.Contains("/api") ||
               pathValue.StartsWith("/account/");  // Account pages are also sensitive
    }
}
