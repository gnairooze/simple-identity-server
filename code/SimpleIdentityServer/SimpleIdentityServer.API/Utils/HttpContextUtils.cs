using System.Net;

namespace SimpleIdentityServer.API.Utils;

/// <summary>
/// Utility class for common HTTP context operations
/// </summary>
public static class HttpContextUtils
{
    /// <summary>
    /// Gets the real client IP address, handling load balancers and forwarded headers
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <returns>The client IP address as a string</returns>
    public static string GetClientIpAddress(HttpContext httpContext)
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
        
        // fallback
        return "unknown";
    }
}
