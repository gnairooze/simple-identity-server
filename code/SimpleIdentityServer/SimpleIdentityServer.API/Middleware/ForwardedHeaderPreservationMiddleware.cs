namespace SimpleIdentityServer.API.Middleware;

/// <summary>
/// Middleware to preserve the original X-Forwarded-For header value before UseForwardedHeaders processes it
/// This allows downstream middleware to access the original forwarded header information
/// </summary>
public class ForwardedHeaderPreservationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ForwardedHeaderPreservationMiddleware> _logger;

    public ForwardedHeaderPreservationMiddleware(
        RequestDelegate next, 
        ILogger<ForwardedHeaderPreservationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Preserve the original X-Forwarded-For header value before UseForwardedHeaders processes it
        var originalXForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var originalXRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        var originalXClientIp = context.Request.Headers["X-Client-IP"].FirstOrDefault();

        // Store original values in HttpContext.Items for downstream middleware
        if (!string.IsNullOrEmpty(originalXForwardedFor))
        {
            context.Items["OriginalXForwardedFor"] = originalXForwardedFor;
        }
        if (!string.IsNullOrEmpty(originalXRealIp))
        {
            context.Items["OriginalXRealIp"] = originalXRealIp;
        }
        if (!string.IsNullOrEmpty(originalXClientIp))
        {
            context.Items["OriginalXClientIp"] = originalXClientIp;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for ForwardedHeaderPreservationMiddleware
/// </summary>
public static class ForwardedHeaderPreservationExtensions
{
    /// <summary>
    /// Gets the original X-Forwarded-For header value before it was processed by UseForwardedHeaders
    /// </summary>
    public static string? GetOriginalXForwardedFor(this HttpContext context)
    {
        return context.Items["OriginalXForwardedFor"]?.ToString();
    }

    /// <summary>
    /// Gets the original X-Real-IP header value before it was processed by UseForwardedHeaders
    /// </summary>
    public static string? GetOriginalXRealIp(this HttpContext context)
    {
        return context.Items["OriginalXRealIp"]?.ToString();
    }

    /// <summary>
    /// Gets the original X-Client-IP header value before it was processed by UseForwardedHeaders
    /// </summary>
    public static string? GetOriginalXClientIp(this HttpContext context)
    {
        return context.Items["OriginalXClientIp"]?.ToString();
    }

    /// <summary>
    /// Gets all original forwarded header information
    /// </summary>
    public static Dictionary<string, string> GetOriginalForwardedHeaders(this HttpContext context)
    {
        var headers = new Dictionary<string, string>();
        
        var xForwardedFor = context.GetOriginalXForwardedFor();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            headers["X-Forwarded-For"] = xForwardedFor;
        }
        
        var xRealIp = context.GetOriginalXRealIp();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            headers["X-Real-IP"] = xRealIp;
        }
        
        var xClientIp = context.GetOriginalXClientIp();
        if (!string.IsNullOrEmpty(xClientIp))
        {
            headers["X-Client-IP"] = xClientIp;
        }
        
        return headers;
    }
}
