using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using SimpleIdentityServer.Data;

namespace SampleSPA;

/// <summary>
/// Helper class to programmatically register the SampleSPA OAuth client
/// Run this as a console app or add to your Identity Server startup
/// </summary>
public class RegisterClient
{
    public static async Task RegisterSampleSPAClient(
        IOpenIddictApplicationManager applicationManager)
    {
        // Check if client already exists
        var existingClient = await applicationManager.FindByClientIdAsync("web-app");
        
        if (existingClient != null)
        {
            Console.WriteLine("Client 'web-app' already exists. Updating...");
            
            // Delete existing client to recreate with correct settings
            await applicationManager.DeleteAsync(existingClient);
            Console.WriteLine("Existing client deleted.");
        }

        // Create the SampleSPA client with proper configuration
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "web-app",
            DisplayName = "Sample SPA Web Application",
            Type = OpenIddictConstants.ClientTypes.Public, // Public client (no secret)
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit, // Implicit consent for testing
            
            // Redirect URIs - where the user is redirected after login
            RedirectUris =
            {
                new Uri("http://localhost:8080/callback.html"),
                new Uri("https://localhost:8080/callback.html")
            },
            
            // Post-logout redirect URIs - where to redirect after logout
            PostLogoutRedirectUris =
            {
                new Uri("http://localhost:8080/index.html"),
                new Uri("https://localhost:8080/index.html")
            },
            
            // Permissions - what this client is allowed to do
            Permissions =
            {
                // Endpoints
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Logout,
                OpenIddictConstants.Permissions.Endpoints.Token,
                
                // Grant Types
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                
                // Response Types
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                
                // Scopes
                OpenIddictConstants.Permissions.Scopes.OpenId,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Roles
            },
            
            // Requirements
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange // Require PKCE
            }
        };

        await applicationManager.CreateAsync(descriptor);
        
        Console.WriteLine("✓ Client 'web-app' registered successfully!");
        Console.WriteLine();
        Console.WriteLine("Configuration:");
        Console.WriteLine($"  Client ID: web-app");
        Console.WriteLine($"  Type: Public (PKCE required)");
        Console.WriteLine($"  Redirect URIs:");
        Console.WriteLine($"    - http://localhost:8080/callback.html");
        Console.WriteLine($"    - https://localhost:8080/callback.html");
        Console.WriteLine();
    }

    // Example usage in Program.cs or Startup
    public static async Task Main(string[] args)
    {
        // This is a standalone example - you would typically add this to your
        // Identity Server's startup or create a migration/seeding script

        /*
        // In your Identity Server's Program.cs, add this during startup:
        
        using (var scope = app.Services.CreateScope())
        {
            var applicationManager = scope.ServiceProvider
                .GetRequiredService<IOpenIddictApplicationManager>();
            
            await RegisterSampleSPAClient(applicationManager);
        }
        */

        Console.WriteLine("To use this client registration:");
        Console.WriteLine("1. Add this code to your Identity Server's Program.cs");
        Console.WriteLine("2. Or run this as a console app with proper DI setup");
        Console.WriteLine("3. Or use the provided SQL script: setup-client.sql");
    }
}

/// <summary>
/// Add this to your Identity Server's Program.cs to register the client on startup
/// </summary>
public static class SampleSPAClientSeeder
{
    public static async Task SeedSampleSPAClientAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        
        var applicationManager = scope.ServiceProvider
            .GetRequiredService<IOpenIddictApplicationManager>();
        
        // Check if client exists
        var client = await applicationManager.FindByClientIdAsync("web-app");
        
        if (client == null)
        {
            Console.WriteLine("Registering SampleSPA client...");
            await RegisterClient.RegisterSampleSPAClient(applicationManager);
        }
        else
        {
            Console.WriteLine("SampleSPA client already registered.");
            
            // Optionally, verify and update redirect URIs
            var redirectUris = await applicationManager.GetRedirectUrisAsync(client);
            var expectedUri = "http://localhost:8080/callback.html";
            
            if (!redirectUris.Contains(expectedUri))
            {
                Console.WriteLine($"Warning: Redirect URI '{expectedUri}' not found in client configuration.");
                Console.WriteLine("Consider running the SQL setup script to update the client.");
            }
        }
    }
}

// =============================================================================
// INTEGRATION INSTRUCTIONS
// =============================================================================
// 
// Add this to your Identity Server's Program.cs:
// 
// var builder = WebApplication.CreateBuilder(args);
// 
// // ... your existing configuration ...
// 
// var app = builder.Build();
// 
// // Seed the SampleSPA client
// using (var scope = app.Services.CreateScope())
// {
//     await SampleSPAClientSeeder.SeedSampleSPAClientAsync(scope.ServiceProvider);
// }
// 
// app.Run();
// 
// =============================================================================

