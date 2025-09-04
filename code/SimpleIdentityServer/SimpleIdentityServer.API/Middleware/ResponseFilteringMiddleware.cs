using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text;

namespace SimpleIdentityServer.API.Middleware;

/// <summary>
/// Middleware to filter API responses based on client permissions
/// </summary>
public class ResponseFilteringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseFilteringMiddleware> _logger;

    public ResponseFilteringMiddleware(RequestDelegate next, ILogger<ResponseFilteringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process API responses (not token endpoints or static content)
        if (ShouldFilterResponse(context))
        {
            var originalBodyStream = context.Response.Body;

            try
            {
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                await _next(context);

                // Only filter successful JSON responses
                if (context.Response.StatusCode == 200 && 
                    context.Response.ContentType?.Contains("application/json") == true)
                {
                    await FilterResponse(context, responseBody, originalBodyStream);
                }
                else
                {
                    await CopyResponseToOriginalStream(responseBody, originalBodyStream);
                }
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
        else
        {
            await _next(context);
        }
    }

    private async Task FilterResponse(HttpContext context, MemoryStream responseBody, Stream originalBodyStream)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseBody).ReadToEndAsync();

        if (string.IsNullOrEmpty(responseText))
        {
            await CopyResponseToOriginalStream(responseBody, originalBodyStream);
            return;
        }

        try
        {
            var jsonDocument = JsonDocument.Parse(responseText);
            var filteredJson = FilterJsonBasedOnClaims(jsonDocument, context.User);
            
            var filteredResponse = JsonSerializer.Serialize(filteredJson, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            var filteredBytes = Encoding.UTF8.GetBytes(filteredResponse);
            context.Response.ContentLength = filteredBytes.Length;
            
            await originalBodyStream.WriteAsync(filteredBytes);

            _logger.LogInformation("Response filtered for client: {ClientId}", 
                context.User.FindFirst("client_id")?.Value ?? "unknown");
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return original response
            await CopyResponseToOriginalStream(responseBody, originalBodyStream);
        }
    }

    private async Task CopyResponseToOriginalStream(MemoryStream responseBody, Stream originalBodyStream)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }

    private object FilterJsonBasedOnClaims(JsonDocument jsonDocument, ClaimsPrincipal user)
    {
        var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var clientId = user.FindFirst("client_id")?.Value;

        return FilterJsonElement(jsonDocument.RootElement, userRoles, clientId);
    }

    private object? FilterJsonElement(JsonElement element, List<string> userRoles, string? clientId)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => FilterJsonObject(element, userRoles, clientId),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(item => FilterJsonElement(item, userRoles, clientId))
                .ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString() ?? string.Empty
        };
    }

    private Dictionary<string, object?> FilterJsonObject(JsonElement element, List<string> userRoles, string? clientId)
    {
        var filteredObject = new Dictionary<string, object?>();

        foreach (var property in element.EnumerateObject())
        {
            if (ShouldIncludeProperty(property.Name, userRoles, clientId))
            {
                filteredObject[property.Name] = FilterJsonElement(property.Value, userRoles, clientId);
            }
        }

        return filteredObject;
    }

    private bool ShouldIncludeProperty(string propertyName, List<string> userRoles, string? clientId)
    {
        // Define field-level authorization rules (case-insensitive)
        var sensitiveFields = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // Temperature data only for service and admin roles
            { "temperatureC", new[] { "service", "admin" } },
            { "temperatureF", new[] { "service", "admin" } },
            // Summary and Date available to all authenticated users
            { "summary", new[] { "web_user", "mobile_user", "service", "admin" } },
            { "date", new[] { "web_user", "mobile_user", "service", "admin" } }
        };

        // Allow all properties if no specific rules defined
        if (!sensitiveFields.ContainsKey(propertyName))
        {
            return true;
        }

        var allowedRoles = sensitiveFields[propertyName];
        
        // Check if user has required roles or client permissions
        return userRoles.Any(role => allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase)) ||
               (clientId != null && allowedRoles.Contains(clientId, StringComparer.OrdinalIgnoreCase));
    }

    private static bool ShouldFilterResponse(HttpContext context)
    {
        // Only filter authenticated API responses, not identity server endpoints
        return context.User.Identity?.IsAuthenticated == true &&
               !context.Request.Path.StartsWithSegments("/connect") &&
               !context.Request.Path.StartsWithSegments("/.well-known") &&
               !context.Request.Path.StartsWithSegments("/swagger");
    }
}
