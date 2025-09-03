using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using SimpleIdentityServer.API.Configuration;
using SimpleIdentityServer.API.Middleware;
using SimpleIdentityServer.Data;
using SimpleIdentityServer.Services;
using System.Collections.ObjectModel;
using System.Data;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure secure connection strings from environment variables
ConfigureSecureConnectionStrings(builder);

// Validate all configuration early in the startup process
ValidateConfiguration(builder);

// Configure Serilog for Security Logging
ConfigureSerilog(builder);

// Configure ASP.NET Core logging to enable debug level for debug logging middleware
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
if (builder.Environment.IsDevelopment())
{
    // Enable debug logging in development
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    builder.Logging.AddFilter("SimpleIdentityServer.API.Middleware.DebugLoggingMiddleware", LogLevel.Debug);
}

// Configure CORS policies
ConfigureCors(builder);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Limit request body size to 1MB to prevent large payload attacks
    options.MaxModelBindingCollectionSize = 1000;
});

// Configure request size limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 1_048_576; // 1MB
});

// Configure Kestrel server options from configuration
ConfigureKestrel(builder);

// Only enable Swagger in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

// Add memory cache for security monitoring
builder.Services.AddMemoryCache();

// Configure load balancer options
builder.Services.Configure<LoadBalancerOptions>(
    builder.Configuration.GetSection(LoadBalancerOptions.SectionName));

// Configure forwarded headers for load balancer support
var loadBalancerConfig = builder.Configuration
    .GetSection(LoadBalancerOptions.SectionName)
    .Get<LoadBalancerOptions>() ?? new LoadBalancerOptions();

if (loadBalancerConfig.EnableForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        // Configure which headers to forward
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        
        // Clear the default networks and proxies
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        
        // Add trusted proxy IPs from configuration
        foreach (var proxyIp in loadBalancerConfig.TrustedProxies)
        {
            if (IPAddress.TryParse(proxyIp, out var parsedProxy))
            {
                options.KnownProxies.Add(parsedProxy);
            }
        }
        
        // Add trusted networks from configuration
        foreach (var networkCidr in loadBalancerConfig.TrustedNetworks)
        {
            try
            {
                var parts = networkCidr.Split('/');
                if (parts.Length == 2 && 
                    IPAddress.TryParse(parts[0], out var networkAddress) && 
                    int.TryParse(parts[1], out var prefixLength))
                {
                    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(networkAddress, prefixLength));
                }
            }
            catch (Exception ex)
            {
                // Log invalid network configuration but don't crash
                Console.WriteLine($"Warning: Invalid network configuration '{networkCidr}': {ex.Message}");
            }
        }
        
        // Configure security settings from configuration
        options.ForwardLimit = loadBalancerConfig.ForwardLimit;
        options.RequireHeaderSymmetry = loadBalancerConfig.RequireHeaderSymmetry;
    });
}

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

// Helper function to get partition key for rate limiting
static string GetPartitionKey(HttpContext httpContext)
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

// Helper function to get the real client IP address, handling load balancers
static string GetClientIpAddress(HttpContext httpContext)
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

// Helper function to extract client ID from request
static string? GetClientIdFromRequest(HttpContext httpContext)
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

// Configure Entity Framework Core with settings from configuration
ConfigureDatabase(builder);

// Configure OpenIddict with settings from configuration
ConfigureOpenIddict(builder);

// Register custom services
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IScopeService, ScopeService>();

// Helper methods for certificate management in load balanced scenarios
static System.Security.Cryptography.X509Certificates.X509Certificate2 GetOrCreateEncryptionCertificate(SimpleIdentityServer.API.Configuration.CertificateOptions certificateOptions)
{
    var certPath = certificateOptions.EncryptionCertificatePath;
    var certPassword = certificateOptions.Password;
    
    if (string.IsNullOrEmpty(certPassword))
    {
        certPassword = Environment.GetEnvironmentVariable("CERT_PASSWORD");
        if (string.IsNullOrEmpty(certPassword))
        {
            throw new InvalidOperationException("Certificate password is required. Set CERT_PASSWORD environment variable or Application:Certificates:Password in configuration.");
        }
    }
    
    if (File.Exists(certPath))
    {
        return new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, certPassword);
    }
    
    // If certificate doesn't exist, create a self-signed one and save it
    // This is a simplified approach - in production, use proper certificate management
    var cert = CreateSelfSignedCertificate("CN=SimpleIdentityServer-Encryption");
    
    // Ensure directory exists
    Directory.CreateDirectory(Path.GetDirectoryName(certPath)!);
    
    // Save certificate for other instances to use
    File.WriteAllBytes(certPath, cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx, certPassword));
    
    return cert;
}

static System.Security.Cryptography.X509Certificates.X509Certificate2 GetOrCreateSigningCertificate(SimpleIdentityServer.API.Configuration.CertificateOptions certificateOptions)
{
    var certPath = certificateOptions.SigningCertificatePath;
    var certPassword = certificateOptions.Password;
    
    if (string.IsNullOrEmpty(certPassword))
    {
        certPassword = Environment.GetEnvironmentVariable("CERT_PASSWORD");
        if (string.IsNullOrEmpty(certPassword))
        {
            throw new InvalidOperationException("Certificate password is required. Set CERT_PASSWORD environment variable or Application:Certificates:Password in configuration.");
        }
    }
    
    if (File.Exists(certPath))
    {
        return new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, certPassword);
    }
    
    // If certificate doesn't exist, create a self-signed one and save it
    var cert = CreateSelfSignedCertificate("CN=SimpleIdentityServer-Signing");
    
    // Ensure directory exists
    Directory.CreateDirectory(Path.GetDirectoryName(certPath)!);
    
    // Save certificate for other instances to use
    File.WriteAllBytes(certPath, cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx, certPassword));
    
    return cert;
}

static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateSelfSignedCertificate(string subjectName)
{
    using var rsa = System.Security.Cryptography.RSA.Create(2048);
    var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
        subjectName, 
        rsa, 
        System.Security.Cryptography.HashAlgorithmName.SHA256, 
        System.Security.Cryptography.RSASignaturePadding.Pkcs1);

    request.CertificateExtensions.Add(
        new System.Security.Cryptography.X509Certificates.X509KeyUsageExtension(
            System.Security.Cryptography.X509Certificates.X509KeyUsageFlags.DigitalSignature | 
            System.Security.Cryptography.X509Certificates.X509KeyUsageFlags.KeyEncipherment, 
            critical: false));

    var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(2));
    
    // Return a new certificate with the private key
    return new System.Security.Cryptography.X509Certificates.X509Certificate2(
        certificate.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx), 
        (string?)null, 
        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable);
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure forwarded headers - MUST be first in pipeline for load balancer support
if (loadBalancerConfig.EnableForwardedHeaders)
{
    app.UseForwardedHeaders();
}

// Add security headers middleware - should be early in pipeline
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseHttpsRedirection();

// Add CORS middleware - must be after UseHttpsRedirection
app.UseCors("ProductionCorsPolicy");

// Add debug logging middleware - should be very early in pipeline to capture all requests/responses
app.UseMiddleware<DebugLoggingMiddleware>();

// Add security monitoring middleware - should be early in pipeline
app.UseMiddleware<SecurityMonitoringMiddleware>();

// Add rate limiting middleware - must be before authentication
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Add response filtering middleware for field-level authorization
app.UseMiddleware<ResponseFilteringMiddleware>();

app.MapControllers();

// Seed the database with initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    
    var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
    var scopeService = scope.ServiceProvider.GetRequiredService<IScopeService>();
    
    await clientService.SeedClientsAsync();
    await scopeService.SeedScopesAsync();
}

// Start the log cleanup service
StartLogCleanupService(app);

app.Run();

// Configure secure connection strings from environment variables
static void ConfigureSecureConnectionStrings(WebApplicationBuilder builder)
{
    // Get connection strings from environment variables
    var defaultConnection = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION_STRING");
    var securityLogsConnection = Environment.GetEnvironmentVariable("SECURITY_LOGS_CONNECTION_STRING");
    
    if (!string.IsNullOrEmpty(defaultConnection))
    {
        builder.Configuration["ConnectionStrings:DefaultConnection"] = defaultConnection;
    }
    else if (builder.Environment.IsDevelopment())
    {
        // Fallback for development - use local development database
        builder.Configuration["ConnectionStrings:DefaultConnection"] = 
            "Server=localhost;Database=SimpleIdentityServer_Dev;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true";
    }
    else
    {
        throw new InvalidOperationException("DEFAULT_CONNECTION_STRING environment variable is required in production");
    }
    
    if (!string.IsNullOrEmpty(securityLogsConnection))
    {
        builder.Configuration["ConnectionStrings:SecurityLogsConnection"] = securityLogsConnection;
    }
    else if (builder.Environment.IsDevelopment())
    {
        // Fallback for development - use local development database
        builder.Configuration["ConnectionStrings:SecurityLogsConnection"] = 
            "Server=localhost;Database=SimpleIdentityServer_SecurityLogs_Dev;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true";
    }
    else
    {
        throw new InvalidOperationException("SECURITY_LOGS_CONNECTION_STRING environment variable is required in production");
    }
}

// Configure Serilog with SQL Server sink and structured logging
static void ConfigureSerilog(WebApplicationBuilder builder)
{
    // Get connection string from environment variables or configuration
    var connectionString = builder.Configuration.GetConnectionString("SecurityLogsConnection");
    
    // If connection string is empty, it means we need to use environment variables
    if (string.IsNullOrEmpty(connectionString))
    {
        var securityLogsConnection = Environment.GetEnvironmentVariable("SECURITY_LOGS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(securityLogsConnection))
        {
            connectionString = securityLogsConnection;
        }
        else if (builder.Environment.IsDevelopment())
        {
            // Fallback for development
            connectionString = "Server=localhost;Database=SimpleIdentityServer_SecurityLogs_Dev;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true";
        }
        else
        {
            // In production, we'll configure console-only logging if no connection string is available
            connectionString = null;
        }
    }
    
    var nodeName = Environment.GetEnvironmentVariable("NODE_NAME") ?? Environment.MachineName;
    
    // Define custom columns for security logs
    var columnOptions = new ColumnOptions
    {
        DisableTriggers = true,
        ClusteredColumnstoreIndex = false
    };
    
    // Keep essential columns but remove Properties and MessageTemplate
    columnOptions.Store.Remove(StandardColumn.Properties);
        
    // Ensure LogEvent column is included (this contains the actual log message)
    columnOptions.Store.Add(StandardColumn.LogEvent);
    
    // Add custom columns for security data
    columnOptions.AdditionalColumns = new Collection<SqlColumn>
    {
        new SqlColumn { ColumnName = "RequestId", DataType = SqlDbType.NVarChar, DataLength = 100, AllowNull = true },
        new SqlColumn { ColumnName = "EventType", DataType = SqlDbType.NVarChar, DataLength = 50, AllowNull = true },
        new SqlColumn { ColumnName = "IpAddress", DataType = SqlDbType.NVarChar, DataLength = 45, AllowNull = true },
        new SqlColumn { ColumnName = "UserAgent", DataType = SqlDbType.NVarChar, DataLength = 500, AllowNull = true },
        new SqlColumn { ColumnName = "Path", DataType = SqlDbType.NVarChar, DataLength = 200, AllowNull = true },
        new SqlColumn { ColumnName = "Method", DataType = SqlDbType.NVarChar, DataLength = 10, AllowNull = true },
        new SqlColumn { ColumnName = "StatusCode", DataType = SqlDbType.Int, AllowNull = true },
        new SqlColumn { ColumnName = "DurationMs", DataType = SqlDbType.Float, AllowNull = true },
        new SqlColumn { ColumnName = "ClientId", DataType = SqlDbType.NVarChar, DataLength = 100, AllowNull = true },
        new SqlColumn { ColumnName = "NodeName", DataType = SqlDbType.NVarChar, DataLength = 50, AllowNull = true }
    };

    // Configure Serilog
    var loggerConfig = new LoggerConfiguration()
        .Enrich.WithProperty("NodeName", nodeName)
        .Enrich.FromLogContext()
        .MinimumLevel.Debug() // Enable debug level logging
        .WriteTo.Console(
            restrictedToMinimumLevel: builder.Environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

    // Only add SQL Server sink if we have a valid connection string
    if (!string.IsNullOrEmpty(connectionString))
    {
        loggerConfig.WriteTo.MSSqlServer(
            connectionString: connectionString,
            sinkOptions: new MSSqlServerSinkOptions
            {
                TableName = "SecurityLogs",
                SchemaName = "dbo",
                AutoCreateSqlTable = true,
                BatchPostingLimit = GetSecurityLoggingBatchLimit(builder),
                BatchPeriod = TimeSpan.FromSeconds(GetSecurityLoggingBatchPeriod(builder))
            },
        tion,
            columnOptions: columnOptions);
    }
    else
    {
        // Log warning that SQL Server logging is not available
        Console.WriteLine("WARNING: No SQL Server connection string available for security logging. Using console logging only.");
    }

    Log.Logger = loggerConfig.CreateLogger();

    // Use Serilog for ASP.NET Core logging
    builder.Host.UseSerilog();
}

// Start background service to clean up old security logs (30-day retention)
static void StartLogCleanupService(WebApplication app)
{
    // Get connection string from environment variables or configuration
    var connectionString = app.Configuration.GetConnectionString("SecurityLogsConnection");
    
    // If connection string is empty, try environment variables
    if (string.IsNullOrEmpty(connectionString))
    {
        connectionString = Environment.GetEnvironmentVariable("SECURITY_LOGS_CONNECTION_STRING");
    }
    
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    
    // Only start cleanup service if we have a valid connection string
    if (string.IsNullOrEmpty(connectionString))
    {
        logger.LogWarning("No SQL Server connection string available for security logs cleanup service. Cleanup service will not start.");
        return;
    }
    
    Task.Run(async () =>
    {
        while (!app.Lifetime.ApplicationStopping.IsCancellationRequested)
        {
            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();
                
                // Delete logs older than configured retention period
                var retentionDays = GetSecurityLoggingRetentionDays(builder);
                var deleteCommand = $@"
                    DELETE FROM SecurityLogs 
                    WHERE TimeStamp < DATEADD(day, -{retentionDays}, GETUTCDATE())";
                    
                using var command = new Microsoft.Data.SqlClient.SqlCommand(deleteCommand, connection);
                var deletedRows = await command.ExecuteNonQueryAsync();
                
                if (deletedRows > 0)
                {
                    logger.LogInformation("Security Log Cleanup: Deleted {DeletedRows} old security log records", deletedRows);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during security log cleanup");
            }
            
            // Run cleanup every 24 hours
            var cleanupInterval = builder.Configuration.GetValue<int>("Application:SecurityLogging:CleanupIntervalHours", 24);
            await Task.Delay(TimeSpan.FromHours(cleanupInterval), app.Lifetime.ApplicationStopping);
        }
    });
}

// Validate all configuration
static void ValidateConfiguration(WebApplicationBuilder builder)
{
    var validationService = new SimpleIdentityServer.Services.ConfigurationValidationService(
        builder.Configuration, 
        builder.Environment);
    
    try
    {
        validationService.ValidateConfiguration();
    }
    catch (InvalidOperationException ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(ex.Message);
        Console.ResetColor();
        Environment.Exit(1);
    }
}

// Configure CORS policies
static void ConfigureCors(WebApplicationBuilder builder)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ProductionCorsPolicy", policy =>
        {
            if (builder.Environment.IsProduction())
            {
                // In production, only allow specific origins from environment variables
                var allowedOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")?.Split(';') ?? Array.Empty<string>();
                if (allowedOrigins.Length > 0 && !string.IsNullOrWhiteSpace(allowedOrigins[0]))
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    throw new InvalidOperationException("CORS_ALLOWED_ORIGINS environment variable is required in production");
                }
            }
            else
            {
                // In development, use origins from configuration
                var developmentOptions = builder.Configuration.GetSection("Application:Development").Get<SimpleIdentityServer.API.Configuration.DevelopmentOptions>();
                if (developmentOptions?.CorsOrigins?.Length > 0)
                {
                    policy.WithOrigins(developmentOptions.CorsOrigins);
                }
                else
                {
                    throw new InvalidOperationException("Application:Development:CorsOrigins configuration is required in development");
                }
            }
            
            policy.WithMethods("GET", "POST", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    });
}

// Configure Kestrel server options
static void ConfigureKestrel(WebApplicationBuilder builder)
{
    var kestrelOptions = builder.Configuration.GetSection("Kestrel").Get<SimpleIdentityServer.API.Configuration.KestrelOptions>();
    if (kestrelOptions == null)
    {
        throw new InvalidOperationException("Kestrel configuration section is required");
    }

    builder.Services.Configure<KestrelServerOptions>(options =>
    {
        options.Limits.MaxRequestBodySize = kestrelOptions.MaxRequestBodySize;
        options.Limits.MaxConcurrentConnections = kestrelOptions.MaxConcurrentConnections;
        options.Limits.MaxConcurrentUpgradedConnections = kestrelOptions.MaxConcurrentConnections;
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(kestrelOptions.RequestHeadersTimeoutSeconds);
        options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(kestrelOptions.KeepAliveTimeoutMinutes);
    });
}

// Configure database
static void ConfigureDatabase(WebApplicationBuilder builder)
{
    var databaseOptions = builder.Configuration.GetSection("Application:Database").Get<SimpleIdentityServer.API.Configuration.DatabaseOptions>();
    if (databaseOptions == null)
    {
        throw new InvalidOperationException("Application:Database configuration section is required");
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
        {
            sqlOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: databaseOptions.MaxRetryCount, 
                maxRetryDelay: TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds), 
                errorNumbersToAdd: null);
        });
        
        // Configure OpenIddict to use Entity Framework Core as the default store
        options.UseOpenIddict();
    });
}

// Configure OpenIddict
static void ConfigureOpenIddict(WebApplicationBuilder builder)
{
    var openIddictOptions = builder.Configuration.GetSection("Application:OpenIddict").Get<SimpleIdentityServer.API.Configuration.OpenIddictOptions>();
    if (openIddictOptions == null)
    {
        throw new InvalidOperationException("Application:OpenIddict configuration section is required");
    }

    var certificateOptions = builder.Configuration.GetSection("Application:Certificates").Get<SimpleIdentityServer.API.Configuration.CertificateOptions>();
    if (certificateOptions == null)
    {
        throw new InvalidOperationException("Application:Certificates configuration section is required");
    }

    builder.Services.AddOpenIddict()
        .AddCore(options =>
        {
            options.UseEntityFrameworkCore()
                   .UseDbContext<ApplicationDbContext>();
        })
        .AddServer(options =>
        {
            options
                .SetTokenEndpointUris(openIddictOptions.TokenEndpointUri)
                .SetIntrospectionEndpointUris(openIddictOptions.IntrospectionEndpointUri)
                .SetConfigurationEndpointUris(openIddictOptions.ConfigurationEndpointUri);

            // Enable the client credentials flow
            options.AllowClientCredentialsFlow();

            // Register the signing and encryption credentials
            if (builder.Environment.IsProduction())
            {
                // In production, use certificates from configuration
                options.AddEncryptionCertificate(GetOrCreateEncryptionCertificate(certificateOptions))
                       .AddSigningCertificate(GetOrCreateSigningCertificate(certificateOptions));
            }
            else
            {
                // In development, use development certificates
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();
            }

            // Register the ASP.NET Core host and configure the ASP.NET Core options
            options.UseAspNetCore()
                   .EnableTokenEndpointPassthrough();

            // Configure the JWT handler
            options.UseAspNetCore()
                   .DisableTransportSecurityRequirement();

            // Configure token lifetimes from configuration
            options.DisableAccessTokenEncryption()
                   .SetAccessTokenLifetime(TimeSpan.FromMinutes(openIddictOptions.AccessTokenLifetimeMinutes))
                   .SetRefreshTokenLifetime(TimeSpan.FromDays(openIddictOptions.RefreshTokenLifetimeDays));
        })
        .AddValidation(options =>
        {
            options.UseLocalServer();
            options.UseAspNetCore();
        });
}

// Helper methods for security logging configuration
static int GetSecurityLoggingBatchLimit(WebApplicationBuilder builder)
{
    return builder.Configuration.GetValue<int>("Application:SecurityLogging:BatchPostingLimit", 50);
}

static int GetSecurityLoggingBatchPeriod(WebApplicationBuilder builder)
{
    return builder.Configuration.GetValue<int>("Application:SecurityLogging:BatchPeriodSeconds", 10);
}

static int GetSecurityLoggingRetentionDays(WebApplicationBuilder builder)
{
    return builder.Configuration.GetValue<int>("Application:SecurityLogging:RetentionDays", 30);
} 