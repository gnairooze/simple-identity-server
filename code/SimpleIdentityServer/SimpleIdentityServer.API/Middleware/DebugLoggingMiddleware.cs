using System.Text;
using System.Text.Json;

namespace SimpleIdentityServer.API.Middleware;

/// <summary>
/// Middleware for comprehensive debug logging of all HTTP requests and responses
/// Logs headers, body content, and timing information at debug level
/// </summary>
public class DebugLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DebugLoggingMiddleware> _logger;
    private readonly IConfiguration _configuration;
    
    // Maximum body size to log (to prevent memory issues with large payloads)
    private const int MaxBodySizeToLog = 64 * 1024; // 64KB

    public DebugLoggingMiddleware(
        RequestDelegate next, 
        ILogger<DebugLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only log if debug logging is enabled
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            await _next(context);
            return;
        }

        var startTime = DateTime.UtcNow;
        var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];
        
        // Log request details
        await LogRequestAsync(context, requestId);

        // Capture response body by replacing the response stream
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        finally
        {
            // Log response details
            var duration = DateTime.UtcNow - startTime;
            await LogResponseAsync(context, requestId, responseBodyStream, duration);

            // Copy the response body back to the original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
            context.Response.Body = originalResponseBodyStream;
        }
    }

    private async Task LogRequestAsync(HttpContext context, string requestId)
    {
        try
        {
            var request = context.Request;
            var logData = new
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                Method = request.Method,
                Path = request.Path.ToString(),
                QueryString = request.QueryString.ToString(),
                Scheme = request.Scheme,
                Host = request.Host.ToString(),
                ContentType = request.ContentType,
                ContentLength = request.ContentLength,
                Headers = GetRequestHeaders(request),
                Body = await GetRequestBodyAsync(request)
            };

            _logger.LogDebug("HTTP Request Details: {RequestData}", 
                JsonSerializer.Serialize(logData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log request details for RequestId: {RequestId}", requestId);
        }
    }

    private async Task LogResponseAsync(HttpContext context, string requestId, MemoryStream responseBodyStream, TimeSpan duration)
    {
        try
        {
            var response = context.Response;
            var responseBody = await GetResponseBodyAsync(responseBodyStream);
            
            var logData = new
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                StatusCode = response.StatusCode,
                ContentType = response.ContentType,
                ContentLength = response.ContentLength ?? responseBodyStream.Length,
                DurationMs = duration.TotalMilliseconds,
                Headers = GetResponseHeaders(response),
                Body = responseBody
            };

            var logLevel = response.StatusCode >= 400 ? LogLevel.Debug : LogLevel.Debug;
            
            _logger.Log(logLevel, "HTTP Response Details: {ResponseData}", 
                JsonSerializer.Serialize(logData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log response details for RequestId: {RequestId}", requestId);
        }
    }

    private static Dictionary<string, string[]> GetRequestHeaders(HttpRequest request)
    {
        var headers = new Dictionary<string, string[]>();
        
        foreach (var header in request.Headers)
        {
            // Mask sensitive headers for security
            if (IsSensitiveHeader(header.Key))
            {
                headers[header.Key] = new[] { "[MASKED]" };
            }
            else
            {
                headers[header.Key] = header.Value.ToArray();
            }
        }
        
        return headers;
    }

    private static Dictionary<string, string[]> GetResponseHeaders(HttpResponse response)
    {
        var headers = new Dictionary<string, string[]>();
        
        foreach (var header in response.Headers)
        {
            // Mask sensitive headers for security
            if (IsSensitiveHeader(header.Key))
            {
                headers[header.Key] = new[] { "[MASKED]" };
            }
            else
            {
                headers[header.Key] = header.Value.ToArray();
            }
        }
        
        return headers;
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "authorization",
            "cookie",
            "set-cookie",
            "x-api-key",
            "x-auth-token",
            "authentication",
            "proxy-authorization",
            "bearer",
            "x-access-token",
            "x-refresh-token",
            "x-jwt-token",
            "x-oauth-token",
            "x-session-token",
            "x-csrf-token",
            "x-xsrf-token"
        };
        
        return sensitiveHeaders.Contains(headerName.ToLowerInvariant());
    }

    private static async Task<string> GetRequestBodyAsync(HttpRequest request)
    {
        try
        {
            // Only log body for certain content types and if not too large
            if (!ShouldLogBody(request.ContentType, request.ContentLength))
            {
                return GetBodySkipReason(request.ContentType, request.ContentLength);
            }

            // Enable request body buffering so we can read it multiple times
            request.EnableBuffering();
            
            // Read the request body
            request.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            
            // Reset the stream position for the next middleware
            request.Body.Seek(0, SeekOrigin.Begin);
            
            return string.IsNullOrEmpty(body) ? "[EMPTY]" : MaskSensitiveData(body);
        }
        catch (Exception ex)
        {
            return $"[ERROR_READING_BODY: {ex.Message}]";
        }
    }

    private static async Task<string> GetResponseBodyAsync(MemoryStream responseBodyStream)
    {
        try
        {
            if (responseBodyStream.Length == 0)
            {
                return "[EMPTY]";
            }

            if (responseBodyStream.Length > MaxBodySizeToLog)
            {
                return $"[BODY_TOO_LARGE: {responseBodyStream.Length} bytes, max: {MaxBodySizeToLog} bytes]";
            }

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            
            return string.IsNullOrEmpty(body) ? "[EMPTY]" : MaskSensitiveData(body);
        }
        catch (Exception ex)
        {
            return $"[ERROR_READING_RESPONSE_BODY: {ex.Message}]";
        }
    }

    private static bool ShouldLogBody(string? contentType, long? contentLength)
    {
        // Don't log if content is too large
        if (contentLength > MaxBodySizeToLog)
        {
            return false;
        }

        // Don't log binary content types
        if (string.IsNullOrEmpty(contentType))
        {
            return true; // Log if we don't know the content type
        }

        var lowerContentType = contentType.ToLowerInvariant();
        
        // Log text-based content types
        var textContentTypes = new[]
        {
            "application/json",
            "application/xml",
            "application/x-www-form-urlencoded",
            "text/",
            "application/javascript",
            "application/typescript"
        };

        // Skip binary content types
        var binaryContentTypes = new[]
        {
            "image/",
            "video/",
            "audio/",
            "application/pdf",
            "application/zip",
            "application/octet-stream",
            "multipart/form-data"
        };

        if (binaryContentTypes.Any(binary => lowerContentType.StartsWith(binary)))
        {
            return false;
        }

        return textContentTypes.Any(text => lowerContentType.StartsWith(text));
    }

    private static string GetBodySkipReason(string? contentType, long? contentLength)
    {
        if (contentLength > MaxBodySizeToLog)
        {
            return $"[BODY_TOO_LARGE: {contentLength} bytes, max: {MaxBodySizeToLog} bytes]";
        }

        if (!string.IsNullOrEmpty(contentType))
        {
            var lowerContentType = contentType.ToLowerInvariant();
            var binaryContentTypes = new[]
            {
                "image/", "video/", "audio/", "application/pdf", 
                "application/zip", "application/octet-stream", "multipart/form-data"
            };

            if (binaryContentTypes.Any(binary => lowerContentType.StartsWith(binary)))
            {
                return $"[BINARY_CONTENT: {contentType}]";
            }
        }

        return "[BODY_NOT_LOGGED]";
    }

    /// <summary>
    /// Masks sensitive data in request/response bodies to prevent logging of tokens and passwords
    /// </summary>
    /// <param name="content">The content to sanitize</param>
    /// <returns>Content with sensitive data masked</returns>
    private static string MaskSensitiveData(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        try
        {
            // Try to parse as JSON and mask sensitive fields
            if (IsJsonContent(content))
            {
                return MaskJsonSensitiveData(content);
            }
            
            // For non-JSON content, use regex patterns to mask common sensitive patterns
            return MaskTextSensitiveData(content);
        }
        catch
        {
            // If any error occurs during masking, fall back to text-based masking
            return MaskTextSensitiveData(content);
        }
    }

    private static bool IsJsonContent(string content)
    {
        var trimmed = content.Trim();
        return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
               (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
    }

    private static string MaskJsonSensitiveData(string jsonContent)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var maskedJson = MaskJsonElement(document.RootElement);
            return JsonSerializer.Serialize(maskedJson, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch
        {
            // If JSON parsing fails, fall back to text-based masking
            return MaskTextSensitiveData(jsonContent);
        }
    }

    private static object? MaskJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    var key = property.Name.ToLowerInvariant();
                    if (IsSensitiveJsonProperty(key))
                    {
                        obj[property.Name] = "[MASKED]";
                    }
                    else
                    {
                        obj[property.Name] = MaskJsonElement(property.Value);
                    }
                }
                return obj;

            case JsonValueKind.Array:
                var array = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(MaskJsonElement(item));
                }
                return array;

            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                return element.GetDecimal();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            default:
                return element.ToString();
        }
    }

    private static bool IsSensitiveJsonProperty(string propertyName)
    {
        var sensitiveProperties = new[]
        {
            "password",
            "pwd",
            "secret",
            "token",
            "access_token",
            "refresh_token",
            "id_token",
            "jwt",
            "bearer",
            "authorization",
            "auth",
            "api_key",
            "apikey",
            "client_secret",
            "clientsecret",
            "private_key",
            "privatekey",
            "session_token",
            "sessiontoken",
            "csrf_token",
            "csrftoken",
            "xsrf_token",
            "xsrftoken",
            "oauth_token",
            "oauthtoken",
            "key",
            "passphrase",
            "credentials",
            "credential"
        };

        return sensitiveProperties.Any(sensitive => 
            propertyName.Contains(sensitive, StringComparison.OrdinalIgnoreCase));
    }

    private static string MaskTextSensitiveData(string content)
    {
        // Mask common patterns for tokens and passwords in text content
        var patterns = new[]
        {
            // Token parameter in form data (highest priority - must come first)
            (@"token=[^&\s]*", "token=[MASKED]"),
            // Bearer tokens
            (@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", "Bearer [MASKED]"),
            // JWT tokens (basic pattern)
            (@"eyJ[A-Za-z0-9\-._~+/]*\.eyJ[A-Za-z0-9\-._~+/]*\.[A-Za-z0-9\-._~+/]*", "[MASKED_JWT]"),
            // Encrypted tokens (OpenIddict format) - sequences with dots, hyphens, underscores
            (@"[A-Za-z0-9\-._~+/]{50,}", "[MASKED_TOKEN]"),
            // API keys (basic pattern - sequences of 20+ alphanumeric characters)
            (@"\b[A-Za-z0-9]{20,}\b", "[MASKED_KEY]"),
            // Password patterns in form data
            (@"password=[^&\s]*", "password=[MASKED]"),
            (@"pwd=[^&\s]*", "pwd=[MASKED]"),
            // Client secret patterns
            (@"client_secret=[^&\s]*", "client_secret=[MASKED]"),
            (@"clientSecret[""']?\s*[:=]\s*[""'][^""']*[""']", "clientSecret: \"[MASKED]\"")
        };

        var maskedContent = content;
        foreach (var (pattern, replacement) in patterns)
        {
            maskedContent = System.Text.RegularExpressions.Regex.Replace(
                maskedContent, pattern, replacement, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return maskedContent;
    }
}
