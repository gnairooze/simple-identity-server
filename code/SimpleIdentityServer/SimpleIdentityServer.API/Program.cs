using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using SimpleIdentityServer.API.Configuration;
using SimpleIdentityServer.API.Middleware;
using SimpleIdentityServer.Data;
using SimpleIdentityServer.Services;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 1_048_576; // 1MB
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxConcurrentUpgradedConnections = 100;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Configure Entity Framework Core with timeout settings
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
    {
        // Set command timeout to 30 seconds to prevent long-running queries
        sqlOptions.CommandTimeout(30);
        // Enable connection resiliency
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
    });
    
    // Configure OpenIddict to use Entity Framework Core as the default store
    options.UseOpenIddict();
});

// Configure OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options
            .SetTokenEndpointUris("/connect/token")
            .SetIntrospectionEndpointUris("/connect/introspect")
            .SetConfigurationEndpointUris("/.well-known/openid-configuration");

        // Enable the client credentials flow
        options.AllowClientCredentialsFlow();

        // Register the signing and encryption credentials
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core options
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough();

        // Configure the JWT handler
        options.UseAspNetCore()
               .DisableTransportSecurityRequirement();

        // Configure the OpenIddict server to issue JWT tokens
        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey()
               .DisableAccessTokenEncryption()
               .SetAccessTokenLifetime(TimeSpan.FromHours(1))
               .SetRefreshTokenLifetime(TimeSpan.FromDays(14));
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Register custom services
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IScopeService, ScopeService>();

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

app.UseHttpsRedirection();

// Add security monitoring middleware - should be early in pipeline
app.UseMiddleware<SecurityMonitoringMiddleware>();

// Add rate limiting middleware - must be before authentication
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
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

app.Run(); 