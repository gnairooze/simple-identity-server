using System.Net;
using System.Threading.RateLimiting;

namespace SimpleIdentityServer.API.Configuration;

public static class RateLimitingConfiguration
{
    public static void ConfigureRateLimiting(WebApplicationBuilder builder)
    {
        // Configure rate limiting options
        builder.Services.Configure<RateLimitingOptions>(
            builder.Configuration.GetSection(RateLimitingOptions.SectionName));

        // Configure Rate Limiting with configuration-based settings
        var rateLimitingConfig = builder.Configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        builder.Services.AddRateLimiter(options =>
        {
            // Global rate limiter - applies to all endpoints
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = rateLimitingConfig.Global.PermitLimit,
                        Window = TimeSpan.FromMinutes(rateLimitingConfig.Global.WindowMinutes)
                    }));

            // Token endpoint specific rate limiter - more restrictive
            options.AddPolicy("TokenPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = rateLimitingConfig.TokenEndpoint.PermitLimit,
                        Window = TimeSpan.FromMinutes(rateLimitingConfig.TokenEndpoint.WindowMinutes)
                    }));

            // Introspection endpoint rate limiter
            options.AddPolicy("IntrospectionPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = rateLimitingConfig.IntrospectionEndpoint.PermitLimit,
                        Window = TimeSpan.FromMinutes(rateLimitingConfig.IntrospectionEndpoint.WindowMinutes)
                    }));

            // Configure what happens when rate limit is exceeded
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429; // Too Many Requests
                
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = 
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                context.HttpContext.Response.ContentType = "application/json";
                
                var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) ? 
                    ((int)retry.TotalSeconds).ToString() : "60";
                    
                var errorResponse = $$"""
                    {
                        "error": "too_many_requests",
                        "error_description": "Rate limit exceeded. Please retry after the specified time.",
                        "retry_after_seconds": "{{retryAfterSeconds}}"
                    }
                    """;
                    
                await context.HttpContext.Response.WriteAsync(errorResponse, cancellationToken: token);
            };
        });
    }

    private static string GetPartitionKey(HttpContext httpContext)
    {
        // Use client ID if available (from Authorization header or form data)
        var clientId = GetClientIdFromRequest(httpContext);
        if (!string.IsNullOrEmpty(clientId))
        {
            return $"client:{clientId}";
        }

        // Fall back to IP address with proper forwarded header support
        var ipAddress = GetClientIpAddress(httpContext);
        return $"ip:{ipAddress}";
    }

    private static string GetClientIpAddress(HttpContext httpContext)
    {
        // After UseForwardedHeaders() middleware, RemoteIpAddress should contain the real client IP
        // The middleware processes X-Forwarded-For and updates Connection.RemoteIpAddress
        var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
        
        if (remoteIpAddress != null)
        {
            // Handle IPv6 mapped IPv4 addresses
            if (remoteIpAddress.IsIPv4MappedToIPv6)
            {
                return remoteIpAddress.MapToIPv4().ToString();
            }
            return remoteIpAddress.ToString();
        }
        
        // Fallback: manually check X-Forwarded-For header if middleware didn't process it
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs: "client, proxy1, proxy2"
            // Take the first one (closest to the original client)
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out var parsedIp))
            {
                return parsedIp.ToString();
            }
        }
        
        // Final fallback
        return "unknown";
    }

    private static string? GetClientIdFromRequest(HttpContext httpContext)
    {
        // Try to get client_id from form data (token requests)
        if (httpContext.Request.HasFormContentType && 
            httpContext.Request.Form.TryGetValue("client_id", out var clientIdForm))
        {
            return clientIdForm.FirstOrDefault();
        }

        // Try to get from query parameters
        if (httpContext.Request.Query.TryGetValue("client_id", out var clientIdQuery))
        {
            return clientIdQuery.FirstOrDefault();
        }

        return null;
    }
}
