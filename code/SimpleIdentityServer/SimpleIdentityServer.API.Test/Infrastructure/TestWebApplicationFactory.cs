using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleIdentityServer.Data;
using OpenIddict.Abstractions;
using SimpleIdentityServer.API.Controllers;

namespace SimpleIdentityServer.API.Test.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<TokenController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); // Use Development instead of Test to avoid production checks
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true);
            
            // Override configuration values for testing
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["ConnectionStrings:SecurityLogsConnection"] = "DataSource=:memory:",
                ["Application:Certificates:Password"] = "",
                ["Application:Certificates:EncryptionCertificatePath"] = "",
                ["Application:Certificates:SigningCertificatePath"] = ""
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing database context registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
                options.UseOpenIddict();
            });

            // Reduce logging noise during tests
            services.Configure<LoggerFilterOptions>(options =>
            {
                options.MinLevel = LogLevel.Warning;
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    }

    public async Task SeedTestDataAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        await context.Database.EnsureCreatedAsync();

        // Seed test applications
        await SeedTestApplicationsAsync(applicationManager);
        
        // Seed test scopes
        await SeedTestScopesAsync(scopeManager);
    }

    private async Task SeedTestApplicationsAsync(IOpenIddictApplicationManager applicationManager)
    {
        // Service API Client
        if (await applicationManager.FindByClientIdAsync("service-api") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "service-api",
                ClientSecret = "supersecret",
                DisplayName = "Service API",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.Introspection,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api1.read",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api1.write"
                }
            });
        }

        // Web Application Client
        if (await applicationManager.FindByClientIdAsync("web-app") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "web-app",
                ClientSecret = "webapp-secret",
                DisplayName = "Web Application",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.Introspection,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api1.read"
                }
            });
        }

        // Mobile Application Client
        if (await applicationManager.FindByClientIdAsync("mobile-app") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "mobile-app",
                ClientSecret = "mobile-secret",
                DisplayName = "Mobile Application",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.Introspection,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api1.read",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api1.write"
                }
            });
        }

        // Admin Client (for testing introspection)
        if (await applicationManager.FindByClientIdAsync("admin-client") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "admin-client",
                ClientSecret = "admin-secret",
                DisplayName = "Admin Client",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.Introspection,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api1.read",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api1.write"
                }
            });
        }

        // Invalid client for testing
        if (await applicationManager.FindByClientIdAsync("invalid-client") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "invalid-client",
                ClientSecret = "invalid-secret",
                DisplayName = "Invalid Client",
                // No permissions - should fail token requests
                Permissions = { }
            });
        }
    }

    private async Task SeedTestScopesAsync(IOpenIddictScopeManager scopeManager)
    {
        if (await scopeManager.FindByNameAsync("api1.read") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "api1.read",
                DisplayName = "API1 Read Access",
                Resources = { "https://api.example.com" }
            });
        }

        if (await scopeManager.FindByNameAsync("api1.write") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "api1.write",
                DisplayName = "API1 Write Access",
                Resources = { "https://api.example.com" }
            });
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureDeleted();
        }
        base.Dispose(disposing);
    }
}
