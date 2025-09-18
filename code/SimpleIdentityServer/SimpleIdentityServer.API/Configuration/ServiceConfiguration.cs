using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using SimpleIdentityServer.API.Services;
using SimpleIdentityServer.Data;
using SimpleIdentityServer.Services;

namespace SimpleIdentityServer.API.Configuration;

public static class ServiceConfiguration
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        // Add controllers with configuration
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

        // Configure database
        ConfigureDatabase(builder);

        // Configure OpenIddict
        ConfigureOpenIddict(builder);

        // Register custom services
        builder.Services.AddScoped<IClientService, ClientService>();
        builder.Services.AddScoped<IScopeService, ScopeService>();
    }

    private static void ConfigureKestrel(WebApplicationBuilder builder)
    {
        var kestrelOptions = builder.Configuration.GetSection(AppSettingsNames.Kestrel).Get<KestrelOptions>();
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

    private static void ConfigureDatabase(WebApplicationBuilder builder)
    {
        var databaseOptions = builder.Configuration.GetSection(AppSettingsNames.ApplicationDatabase).Get<DatabaseOptions>();
        if (databaseOptions == null)
        {
            throw new InvalidOperationException($"{AppSettingsNames.ApplicationDatabase} configuration section is required");
        }

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString(AppSettingsNames.DefaultConnection), sqlOptions =>
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

    private static void ConfigureOpenIddict(WebApplicationBuilder builder)
    {
        var openIddictOptions = builder.Configuration.GetSection(AppSettingsNames.ApplicationOpenIddict).Get<OpenIddictOptions>();
        if (openIddictOptions == null)
        {
            throw new InvalidOperationException($"{AppSettingsNames.ApplicationOpenIddict} configuration section is required");
        }

        var certificateOptions = builder.Configuration.GetSection(AppSettingsNames.ApplicationCertificates).Get<CertificateOptions>();
        if (certificateOptions == null)
        {
            throw new InvalidOperationException($"{AppSettingsNames.ApplicationCertificates} configuration section is required");
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

                // use certificates from configuration
                options.AddEncryptionCertificate(CertificateManager.GetOrCreateEncryptionCertificate(certificateOptions))
                    .AddSigningCertificate(CertificateManager.GetOrCreateSigningCertificate(certificateOptions));

                // Register the ASP.NET Core host and configure the ASP.NET Core options
                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough();

                // Configure the JWT handler
                options.UseAspNetCore();

                // Configure token lifetimes from configuration
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(openIddictOptions.AccessTokenLifetimeMinutes))
                       .SetRefreshTokenLifetime(TimeSpan.FromDays(openIddictOptions.RefreshTokenLifetimeDays));
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });
    }
}
