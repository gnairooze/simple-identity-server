using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using SimpleIdentityServer.API.Data;
using SimpleIdentityServer.API.Services;
using SimpleIdentityServer.Services;

namespace SimpleIdentityServer.API.Configuration;

public static class ServiceConfiguration
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        // Add controllers with views for authentication pages
        builder.Services.AddControllersWithViews(options =>
        {
            // Limit request body size to 1MB to prevent large payload attacks
            options.MaxModelBindingCollectionSize = 1000;
        });

        // Add antiforgery token support
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
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

        // Configure email options
        builder.Services.Configure<EmailOptions>(
            builder.Configuration.GetSection(EmailOptions.SectionName));

        // Register custom services
        builder.Services.AddScoped<IClientService, ClientService>();
        builder.Services.AddScoped<IScopeService, ScopeService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
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
                // Configure endpoints
                options
                    .SetAuthorizationEndpointUris(openIddictOptions.AuthorizationEndpointUri)
                    .SetTokenEndpointUris(openIddictOptions.TokenEndpointUri)
                    .SetIntrospectionEndpointUris(openIddictOptions.IntrospectionEndpointUri)
                    .SetConfigurationEndpointUris(openIddictOptions.ConfigurationEndpointUri);

                // Enable flows
                options.AllowClientCredentialsFlow()
                       .AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange(); // Enforce PKCE

                // Register the signing and encryption credentials
                options.AddEncryptionCertificate(CertificateManager.GetEncryptionCertificate(certificateOptions))
                    .AddSigningCertificate(CertificateManager.GetSigningCertificate(certificateOptions));

                // Register the ASP.NET Core host and configure options
                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough();

                // Configure token lifetimes
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(openIddictOptions.AccessTokenLifetimeMinutes))
                       .SetRefreshTokenLifetime(TimeSpan.FromDays(openIddictOptions.RefreshTokenLifetimeDays))
                       .SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(openIddictOptions.AuthorizationCodeLifetimeMinutes));
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // Add Identity services for user management
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
    }
}
