using Microsoft.Extensions.Caching.Memory;
using Serilog;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.Data.SqlClient;

namespace SimpleIdentityServer.API.Middleware;

/// <summary>
/// Middleware for monitoring security-related activities and detecting anomalies
/// </summary>
public class SecurityMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMonitoringMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    
    // In-memory storage for tracking token requests per client
    private static readonly ConcurrentDictionary<string, TokenRequestTracker> _tokenTrackers = new();
    
    // Static flag to ensure database initialization runs only once
    private static bool _databaseInitialized = false;
    private static readonly object _initializationLock = new();

    public SecurityMonitoringMiddleware(
        RequestDelegate next, 
        ILogger<SecurityMonitoringMiddleware> logger,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Initialize database on first access
        await EnsureDatabaseInitializedAsync();
        
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString("N")[..8];
        
        // Add request ID to context for correlation
        context.Items["RequestId"] = requestId;

        try
        {
            // Monitor token endpoint requests
            if (IsTokenEndpoint(context))
            {
                await MonitorTokenRequest(context, requestId);
            }

            // Monitor introspection endpoint requests
            if (IsIntrospectionEndpoint(context))
            {
                await MonitorIntrospectionRequest(context, requestId);
            }

            await _next(context);

            // Log successful requests
            var duration = DateTime.UtcNow - startTime;
            LogSecurityEvent(context, requestId, "REQUEST_COMPLETED", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            LogSecurityException(context, requestId, ex, duration);
            throw;
        }
    }

    private async Task MonitorTokenRequest(HttpContext context, string requestId)
    {
        var clientId = await GetClientIdFromRequest(context);
        if (string.IsNullOrEmpty(clientId))
        {
            LogSecurityEvent(context, requestId, "TOKEN_REQUEST_NO_CLIENT_ID");
            return;
        }

        var tracker = _tokenTrackers.GetOrAdd(clientId, _ => new TokenRequestTracker());
        var now = DateTime.UtcNow;
        
        // Clean old entries (older than 1 hour)
        tracker.CleanOldEntries(now.AddHours(-1));
        
        // Add current request
        tracker.AddRequest(now);
        
        // Check for suspicious activity
        var recentRequests = tracker.GetRecentRequests(now.AddMinutes(-5)); // Last 5 minutes
        if (recentRequests > 10) // More than 10 requests in 5 minutes
        {
            LogSecurityEvent(context, requestId, "SUSPICIOUS_TOKEN_FREQUENCY", 
                new { ClientId = clientId, RequestsInLast5Min = recentRequests });
        }

        var hourlyRequests = tracker.GetRecentRequests(now.AddHours(-1)); // Last hour
        if (hourlyRequests > 100) // More than 100 requests in 1 hour
        {
            LogSecurityEvent(context, requestId, "HIGH_TOKEN_FREQUENCY", 
                new { ClientId = clientId, RequestsInLastHour = hourlyRequests });
        }

        LogSecurityEvent(context, requestId, "TOKEN_REQUEST_MONITORED", 
            new { ClientId = clientId, RecentRequests = recentRequests });
    }

    private async Task MonitorIntrospectionRequest(HttpContext context, string requestId)
    {
        var clientId = await GetClientIdFromRequest(context);
        LogSecurityEvent(context, requestId, "INTROSPECTION_REQUEST", 
            new { ClientId = clientId ?? "unknown" });
    }

    private async Task<string?> GetClientIdFromRequest(HttpContext context)
    {
        if (context.Request.HasFormContentType)
        {
            try
            {
                var form = await context.Request.ReadFormAsync();
                return form["client_id"].FirstOrDefault();
            }
            catch
            {
                // If we can't read the form, that's suspicious too
                return null;
            }
        }

        return context.Request.Query["client_id"].FirstOrDefault();
    }

    private static bool IsTokenEndpoint(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/connect/token") && 
        context.Request.Method == "POST";

    private static bool IsIntrospectionEndpoint(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/connect/introspect") && 
        context.Request.Method == "POST";

    private void LogSecurityEvent(HttpContext context, string requestId, string eventType, object? additionalData = null)
    {
        var clientId = ExtractClientIdFromAdditionalData(additionalData);
        var nodeName = Environment.GetEnvironmentVariable("NODE_NAME") ?? Environment.MachineName;
        
        // Use Serilog for structured logging to SQL Server
        Log.ForContext("RequestId", requestId)
           .ForContext("EventType", eventType)
           .ForContext("IpAddress", GetClientIpAddress(context))
           .ForContext("UserAgent", context.Request.Headers.UserAgent.ToString())
           .ForContext("Path", context.Request.Path.ToString())
           .ForContext("Method", context.Request.Method)
           .ForContext("StatusCode", context.Response.StatusCode)
           .ForContext("ClientId", clientId)
           .ForContext("NodeName", nodeName)
           .Information("Security Event: {EventType} - {AdditionalData}", eventType, additionalData);

        // Also log to ASP.NET Core logger for console output
        _logger.LogInformation("Security Event: {EventType} | RequestId: {RequestId} | IP: {IpAddress} | ClientId: {ClientId}", 
            eventType, requestId, GetClientIpAddress(context), clientId);
    }

    private void LogSecurityEvent(HttpContext context, string requestId, string eventType, TimeSpan duration)
    {
        var nodeName = Environment.GetEnvironmentVariable("NODE_NAME") ?? Environment.MachineName;
        var durationMs = duration.TotalMilliseconds;
        
        // Use Serilog for structured logging to SQL Server
        var logEvent = Log.ForContext("RequestId", requestId)
                         .ForContext("EventType", eventType)
                         .ForContext("IpAddress", GetClientIpAddress(context))
                         .ForContext("UserAgent", context.Request.Headers.UserAgent.ToString())
                         .ForContext("Path", context.Request.Path.ToString())
                         .ForContext("Method", context.Request.Method)
                         .ForContext("StatusCode", context.Response.StatusCode)
                         .ForContext("DurationMs", durationMs)
                         .ForContext("NodeName", nodeName);

        if (duration.TotalSeconds > 5) // Log slow requests as warnings
        {
            logEvent.Warning("Slow Security Request: {EventType} - Duration: {DurationMs}ms", eventType, durationMs);
            _logger.LogWarning("Slow Security Request: {EventType} | Duration: {Duration}ms | RequestId: {RequestId}", 
                eventType, durationMs, requestId);
        }
        else
        {
            logEvent.Information("Security Event: {EventType} - Duration: {DurationMs}ms", eventType, durationMs);
            _logger.LogInformation("Security Event: {EventType} | Duration: {Duration}ms | RequestId: {RequestId}", 
                eventType, durationMs, requestId);
        }
    }

    private void LogSecurityException(HttpContext context, string requestId, Exception ex, TimeSpan duration)
    {
        var nodeName = Environment.GetEnvironmentVariable("NODE_NAME") ?? Environment.MachineName;
        var durationMs = duration.TotalMilliseconds;
        
        // Use Serilog for structured logging to SQL Server
        Log.ForContext("RequestId", requestId)
           .ForContext("EventType", "REQUEST_EXCEPTION")
           .ForContext("IpAddress", GetClientIpAddress(context))
           .ForContext("UserAgent", context.Request.Headers.UserAgent.ToString())
           .ForContext("Path", context.Request.Path.ToString())
           .ForContext("Method", context.Request.Method)
           .ForContext("DurationMs", durationMs)
           .ForContext("NodeName", nodeName)
           .Error(ex, "Security Exception: {ExceptionType} - {ExceptionMessage}", ex.GetType().Name, ex.Message);

        // Also log to ASP.NET Core logger for console output
        _logger.LogError(ex, "Security Exception: {ExceptionType} | RequestId: {RequestId} | Duration: {Duration}ms", 
            ex.GetType().Name, requestId, durationMs);
    }

    /// <summary>
    /// Helper method to extract ClientId from additional data object
    /// </summary>
    private static string? ExtractClientIdFromAdditionalData(object? additionalData)
    {
        if (additionalData == null) return null;
        
        // Use reflection to get ClientId property if it exists
        var clientIdProperty = additionalData.GetType().GetProperty("ClientId");
        return clientIdProperty?.GetValue(additionalData)?.ToString();
    }

    /// <summary>
    /// Gets the real client IP address, handling load balancers and forwarded headers
    /// </summary>
    private static string GetClientIpAddress(HttpContext httpContext)
    {
        // After UseForwardedHeaders() middleware, RemoteIpAddress should contain the real client IP
        // The middleware processes X-Forwarded-For and updates Connection.RemoteIpAddress
        // manually check X-Forwarded-For header if middleware didn't process it
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs: "client, proxy1, proxy2"
            // Take the first one (closest to the original client)
            return forwardedFor;
            /*
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out var parsedIp))
            {
                return parsedIp.ToString();
            }
            */
        }

        // Fallback to RemoteIpAddress
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
        
        
        // Final fallback
        return "unknown";
    }

    /// <summary>
    /// Ensures the security database is initialized by running the CreateSecurityDB.sql script on first access
    /// </summary>
    private async Task EnsureDatabaseInitializedAsync()
    {
        if (_databaseInitialized)
            return;

        // Use TaskCompletionSource for async-safe one-time initialization
        await Task.Run(() =>
        {
            lock (_initializationLock)
            {
                if (_databaseInitialized)
                    return;

                try
                {
                    _logger.LogInformation("Initializing security database on first access...");
                    
                    // Get the connection string for SecurityLogsConnection
                    var connectionString = _configuration.GetConnectionString("SecurityLogsConnection");
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        _logger.LogError("SecurityLogsConnection string not found in configuration");
                        return;
                    }

                    // Read the SQL script
                    var scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "CreateSecurityDB.sql");
                    if (!File.Exists(scriptPath))
                    {
                        _logger.LogError("CreateSecurityDB.sql script not found at path: {ScriptPath}", scriptPath);
                        return;
                    }

                    var sqlScript = File.ReadAllText(scriptPath);
                    
                    // Execute the SQL script
                    using var connection = new SqlConnection(connectionString);
                    connection.Open();
                    
                    // Split the script by GO statements and execute each batch
                    var batches = sqlScript.Split(new[] { "\nGO\n", "\nGO\r\n", "\r\nGO\r\n", "\r\nGO\n" }, 
                        StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var batch in batches)
                    {
                        var trimmedBatch = batch.Trim();
                        if (!string.IsNullOrEmpty(trimmedBatch))
                        {
                            using var command = new SqlCommand(trimmedBatch, connection);
                            command.ExecuteNonQuery();
                        }
                    }
                    
                    _logger.LogInformation("Security database initialized successfully");
                    _databaseInitialized = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize security database");
                    // Don't prevent the application from starting if database initialization fails
                    // The application can still function, but security logging might not work properly
                }
            }
        });
    }
}

/// <summary>
/// Helper class to track token requests per client
/// </summary>
public class TokenRequestTracker
{
    private readonly List<DateTime> _requests = new();
    private readonly object _lock = new();

    public void AddRequest(DateTime timestamp)
    {
        lock (_lock)
        {
            _requests.Add(timestamp);
        }
    }

    public void CleanOldEntries(DateTime cutoffTime)
    {
        lock (_lock)
        {
            _requests.RemoveAll(r => r < cutoffTime);
        }
    }

    public int GetRecentRequests(DateTime sinceTime)
    {
        lock (_lock)
        {
            return _requests.Count(r => r >= sinceTime);
        }
    }
}
