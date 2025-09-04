using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using SimpleIdentityServer.API.Utils;
using System.Net;
using System.Text.Json;

namespace SimpleIdentityServer.API.Middleware;

/// <summary>
/// Global exception handler that integrates with SecurityMonitoringMiddleware
/// for centralized exception handling and security logging.
/// Uses ASP.NET Core's UseExceptionHandler middleware for proper exception handling.
/// </summary>
public static class GlobalExceptionHandler
{
    /// <summary>
    /// Configures the global exception handler using ASP.NET Core's UseExceptionHandler middleware
    /// </summary>
    /// <param name="app">The web application</param>
    public static void ConfigureGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (exceptionFeature?.Error != null)
                {
                    await HandleExceptionAsync(context, exceptionFeature.Error, app.Environment);
                }
            });
        });
    }

    /// <summary>
    /// Handles exceptions with proper logging that integrates with SecurityMonitoringMiddleware
    /// </summary>
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, IWebHostEnvironment environment)
    {
        // Get RequestId from context (set by SecurityMonitoringMiddleware) or generate new one
        var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];
        var nodeName = Environment.GetEnvironmentVariable("NODE_NAME") ?? Environment.MachineName;
        
        // Determine status code and error details based on exception type
        var (statusCode, errorCode, userMessage) = GetErrorDetails(exception);
        
        // Set response details
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        // Log the exception using Serilog for structured logging (same format as SecurityMonitoringMiddleware)
        // Note: SecurityMonitoringMiddleware already logs exceptions with "REQUEST_EXCEPTION" event type
        // This provides additional "GLOBAL_EXCEPTION" logging for unhandled exceptions that bypass normal flow
        Log.ForContext("RequestId", requestId)
           .ForContext("EventType", "GLOBAL_EXCEPTION")
           .ForContext("IpAddress", HttpContextUtils.GetClientIpAddress(context))
           .ForContext("UserAgent", context.Request.Headers.UserAgent.ToString())
           .ForContext("Path", context.Request.Path.ToString())
           .ForContext("Method", context.Request.Method)
           .ForContext("StatusCode", context.Response.StatusCode)
           .ForContext("ErrorCode", errorCode)
           .ForContext("NodeName", nodeName)
           .Error(exception, "Global Exception Handler: {ExceptionType} - {ExceptionMessage}", 
                  exception.GetType().Name, exception.Message);

        // Create error response
        var errorResponse = new ErrorResponse
        {
            RequestId = requestId,
            ErrorCode = errorCode,
            Message = userMessage,
            Timestamp = DateTime.UtcNow,
            Details = exception.ToString()
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static (HttpStatusCode statusCode, string errorCode, string userMessage) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            // More specific exceptions first (ArgumentNullException inherits from ArgumentException)
            ArgumentNullException => (HttpStatusCode.BadRequest, "MISSING_PARAMETER", "Required parameter is missing."),
            ArgumentException => (HttpStatusCode.BadRequest, "INVALID_ARGUMENT", "Invalid request parameters."),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "UNAUTHORIZED", "Access denied."),
            InvalidOperationException => (HttpStatusCode.BadRequest, "INVALID_OPERATION", "The requested operation is not valid."),
            TimeoutException => (HttpStatusCode.RequestTimeout, "TIMEOUT", "The request timed out."),
            NotImplementedException => (HttpStatusCode.NotImplemented, "NOT_IMPLEMENTED", "This feature is not implemented."),
            
            // OAuth/OpenID specific exceptions
            _ when exception.GetType().Name.Contains("OpenIddict") => 
                (HttpStatusCode.BadRequest, "OAUTH_ERROR", "OAuth/OpenID Connect error occurred."),
            
            // Database related exceptions
            _ when exception.GetType().Name.Contains("Sql") || exception.GetType().Name.Contains("Database") => 
                (HttpStatusCode.InternalServerError, "DATABASE_ERROR", "A database error occurred."),
            
            // Default case
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An internal server error occurred.")
        };
    }
}

/// <summary>
/// Standard error response format for the API
/// </summary>
public class ErrorResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}
