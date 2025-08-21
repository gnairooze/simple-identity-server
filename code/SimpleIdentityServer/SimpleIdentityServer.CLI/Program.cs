using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using Microsoft.EntityFrameworkCore;
using System.CommandLine;
using System.CommandLine.Invocation;
using SimpleIdentityServer.CLI.Data;

namespace SimpleIdentityServer.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("OpenIddict Management CLI - Manage applications and scopes");

        // Application commands
        var appCommand = new Command("app", "Manage OpenIddict applications");
        var listAppsCommand = new Command("list", "List all applications");
        var getAppCommand = new Command("get", "Get application details");
        var addAppCommand = new Command("add", "Add a new application");
        var updateAppCommand = new Command("update", "Update an existing application");
        var deleteAppCommand = new Command("delete", "Delete an application");

        // Scope commands
        var scopeCommand = new Command("scope", "Manage OpenIddict scopes");
        var listScopesCommand = new Command("list", "List all scopes");
        var getScopeCommand = new Command("get", "Get scope details");
        var addScopeCommand = new Command("add", "Add a new scope");
        var updateScopeCommand = new Command("update", "Update an existing scope");
        var deleteScopeCommand = new Command("delete", "Delete a scope");

        // Add options for commands
        getAppCommand.AddOption(new Option<string>("--client-id", "Client ID of the application") { IsRequired = true });
        getScopeCommand.AddOption(new Option<string>("--name", "Name of the scope") { IsRequired = true });

        addAppCommand.AddOption(new Option<string>("--client-id", "Client ID") { IsRequired = true });
        addAppCommand.AddOption(new Option<string>("--client-secret", "Client secret") { IsRequired = true });
        addAppCommand.AddOption(new Option<string>("--display-name", "Display name") { IsRequired = true });
        addAppCommand.AddOption(new Option<string[]>("--permissions", "Permissions (space-separated)") { IsRequired = true });

        addScopeCommand.AddOption(new Option<string>("--name", "Scope name") { IsRequired = true });
        addScopeCommand.AddOption(new Option<string>("--display-name", "Display name") { IsRequired = true });
        addScopeCommand.AddOption(new Option<string[]>("--resources", "Resources (space-separated)") { IsRequired = true });

        updateAppCommand.AddOption(new Option<string>("--client-id", "Client ID") { IsRequired = true });
        updateAppCommand.AddOption(new Option<string>("--client-secret", "Client secret"));
        updateAppCommand.AddOption(new Option<string>("--display-name", "Display name"));
        updateAppCommand.AddOption(new Option<string[]>("--permissions", "Permissions (space-separated)"));

        updateScopeCommand.AddOption(new Option<string>("--name", "Scope name") { IsRequired = true });
        updateScopeCommand.AddOption(new Option<string>("--display-name", "Display name"));
        updateScopeCommand.AddOption(new Option<string[]>("--resources", "Resources (space-separated)"));

        deleteAppCommand.AddOption(new Option<string>("--client-id", "Client ID") { IsRequired = true });
        deleteScopeCommand.AddOption(new Option<string>("--name", "Scope name") { IsRequired = true });

        // Set handlers
        listAppsCommand.SetHandler(ListApplications);
        getAppCommand.SetHandler(GetApplication, getAppCommand.Options.OfType<Option<string>>().First());
        addAppCommand.SetHandler(AddApplication, 
            addAppCommand.Options.OfType<Option<string>>().ElementAt(0),
            addAppCommand.Options.OfType<Option<string>>().ElementAt(1),
            addAppCommand.Options.OfType<Option<string>>().ElementAt(2),
            addAppCommand.Options.OfType<Option<string[]>>().First());
        updateAppCommand.SetHandler(UpdateApplication,
            updateAppCommand.Options.OfType<Option<string>>().ElementAt(0),
            updateAppCommand.Options.OfType<Option<string>>().ElementAt(1),
            updateAppCommand.Options.OfType<Option<string>>().ElementAt(2),
            updateAppCommand.Options.OfType<Option<string[]>>().First());
        deleteAppCommand.SetHandler(DeleteApplication, deleteAppCommand.Options.OfType<Option<string>>().First());

        listScopesCommand.SetHandler(ListScopes);
        getScopeCommand.SetHandler(GetScope, getScopeCommand.Options.OfType<Option<string>>().First());
        addScopeCommand.SetHandler(AddScope,
            addScopeCommand.Options.OfType<Option<string>>().ElementAt(0),
            addScopeCommand.Options.OfType<Option<string>>().ElementAt(1),
            addScopeCommand.Options.OfType<Option<string[]>>().First());
        updateScopeCommand.SetHandler(UpdateScope,
            updateScopeCommand.Options.OfType<Option<string>>().ElementAt(0),
            updateScopeCommand.Options.OfType<Option<string>>().ElementAt(1),
            updateScopeCommand.Options.OfType<Option<string[]>>().First());
        deleteScopeCommand.SetHandler(DeleteScope, deleteScopeCommand.Options.OfType<Option<string>>().First());

        // Add commands to root
        appCommand.AddCommand(listAppsCommand);
        appCommand.AddCommand(getAppCommand);
        appCommand.AddCommand(addAppCommand);
        appCommand.AddCommand(updateAppCommand);
        appCommand.AddCommand(deleteAppCommand);

        scopeCommand.AddCommand(listScopesCommand);
        scopeCommand.AddCommand(getScopeCommand);
        scopeCommand.AddCommand(addScopeCommand);
        scopeCommand.AddCommand(updateScopeCommand);
        scopeCommand.AddCommand(deleteScopeCommand);

        rootCommand.AddCommand(appCommand);
        rootCommand.AddCommand(scopeCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task ListApplications()
    {
        try
        {
            var host = CreateHostBuilder().Build();
            var applicationManager = host.Services.GetRequiredService<IOpenIddictApplicationManager>();

            Console.WriteLine("OpenIddict Applications:");
            Console.WriteLine("=======================");

            await foreach (var application in applicationManager.ListAsync())
            {
                var clientId = await applicationManager.GetClientIdAsync(application);
                var displayName = await applicationManager.GetDisplayNameAsync(application);
                var permissions = await applicationManager.GetPermissionsAsync(application);

                Console.WriteLine($"Client ID: {clientId}");
                Console.WriteLine($"Display Name: {displayName}");
                Console.WriteLine($"Permissions: {string.Join(", ", permissions)}");
                Console.WriteLine("---");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing applications: {ex.Message}");
        }
    }

    static async Task GetApplication(string clientId)
    {
        try
        {
            var host = CreateHostBuilder().Build();
            var applicationManager = host.Services.GetRequiredService<IOpenIddictApplicationManager>();

            var application = await applicationManager.FindByClientIdAsync(clientId);
            if (application == null)
            {
                Console.WriteLine($"Application with Client ID '{clientId}' not found.");
                return;
            }

            var displayName = await applicationManager.GetDisplayNameAsync(application);
            var permissions = await applicationManager.GetPermissionsAsync(application);

            Console.WriteLine($"Application Details for '{clientId}':");
            Console.WriteLine("=================================");
            Console.WriteLine($"Client ID: {clientId}");
            Console.WriteLine($"Display Name: {displayName}");
            Console.WriteLine($"Permissions: {string.Join(", ", permissions)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting application: {ex.Message}");
        }
    }

    static async Task AddApplication(string clientId, string clientSecret, string displayName, string[] permissions)
    {
        try
        {
            var host = CreateHostBuilder().Build();
            var applicationManager = host.Services.GetRequiredService<IOpenIddictApplicationManager>();

            // Check if application already exists
            var existingApp = await applicationManager.FindByClientIdAsync(clientId);
            if (existingApp != null)
            {
                Console.WriteLine($"Application with Client ID '{clientId}' already exists.");
                return;
            }

            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                DisplayName = displayName
            };

            foreach (var permission in permissions)
            {
                descriptor.Permissions.Add(permission);
            }

            await applicationManager.CreateAsync(descriptor);
            Console.WriteLine($"Application '{clientId}' created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating application: {ex.Message}");
        }
    }

    static async Task UpdateApplication(string clientId, string? clientSecret, string? displayName, string[]? permissions)
    {
        try
        {
            var host = CreateHostBuilder().Build();
            var applicationManager = host.Services.GetRequiredService<IOpenIddictApplicationManager>();

            var application = await applicationManager.FindByClientIdAsync(clientId);
            if (application == null)
            {
                Console.WriteLine($"Application with Client ID '{clientId}' not found.");
                return;
            }

            var descriptor = new OpenIddictApplicationDescriptor();
            await applicationManager.PopulateAsync(descriptor, application);

            if (!string.IsNullOrEmpty(clientSecret))
                descriptor.ClientSecret = clientSecret;
            if (!string.IsNullOrEmpty(displayName))
                descriptor.DisplayName = displayName;
            if (permissions != null && permissions.Length > 0)
            {
                descriptor.Permissions.Clear();
                foreach (var permission in permissions)
                {
                    descriptor.Permissions.Add(permission);
                }
            }

            await applicationManager.UpdateAsync(application, descriptor);
            Console.WriteLine($"Application '{clientId}' updated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating application: {ex.Message}");
        }
    }

    static async Task DeleteApplication(string clientId)
    {
        try
        {
            var host = CreateHostBuilder().Build();
            var applicationManager = host.Services.GetRequiredService<IOpenIddictApplicationManager>();

            var application = await applicationManager.FindByClientIdAsync(clientId);
            if (application == null)
            {
                Console.WriteLine($"Application with Client ID '{clientId}' not found.");
                return;
            }

            await applicationManager.DeleteAsync(application);
            Console.WriteLine($"Application '{clientId}' deleted successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting application: {ex.Message}");
        }
    }

    static async Task ListScopes()
    {
        try
        {
            var host = CreateHostBuilder().Build();
            var scopeManager = host.Services.GetRequiredService<IOpenIddictScopeManager>();

            Console.WriteLine("OpenIddict Scopes:");
            Console.WriteLine("==================");

            await foreach (var scope in scopeManager.ListAsync())
            {
                var name = await scopeManager.GetNameAsync(scope);
                var displayName = await scopeManager.GetDisplayNameAsync(scope);
                var resources = await scopeManager.GetResourcesAsync(scope);

                Console.WriteLine($"Name: {name}");
                Console.WriteLine($"Display Name: {displayName}");
                Console.WriteLine($"Resources: {string.Join(", ", resources)}");
                Console.WriteLine("---");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing scopes: {ex.Message}");
        }
    }

    static async Task GetScope(string name)
    {
        try
        {
            var host = CreateHostBuilder().Build();
            var scopeManager = host.Services.GetRequiredService<IOpenIddictScopeManager>();

            var scope = await scopeManager.FindByNameAsync(name);
            if (scope == null)
            {
                Console.WriteLine($"Scope '{name}' not found.");
                return;
            }

            var displayName = await scopeManager.GetDisplayNameAsync(scope);
            var resources = await scopeManager.GetResourcesAsync(scope);

            Console.WriteLine($"Scope Details for '{name}':");
            Console.WriteLine("=========================");
            Console.WriteLine($"Name: {name}");
            Console.WriteLine($"Display Name: {displayName}");
            Console.WriteLine($"Resources: {string.Join(", ", resources)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting scope: {ex.Message}");
        }
    }

    static async Task AddScope(string name, string displayName, string[] resources)
    {
        try
        {
            var host = CreateHostBuilder().Build();
            var scopeManager = host.Services.GetRequiredService<IOpenIddictScopeManager>();

            // Check if scope already exists
            var existingScope = await scopeManager.FindByNameAsync(name);
            if (existingScope != null)
            {
                Console.WriteLine($"Scope '{name}' already exists.");
                return;
            }

            var descriptor = new OpenIddictScopeDescriptor
            {
                Name = name,
                DisplayName = displayName
            };

            foreach (var resource in resources)
            {
                descriptor.Resources.Add(resource);
            }

            await scopeManager.CreateAsync(descriptor);
            Console.WriteLine($"Scope '{name}' created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating scope: {ex.Message}");
        }
    }

    static async Task UpdateScope(string name, string? displayName, string[]? resources)
    {
        try
        {
            var host = CreateHostBuilder().Build();
            var scopeManager = host.Services.GetRequiredService<IOpenIddictScopeManager>();

            var scope = await scopeManager.FindByNameAsync(name);
            if (scope == null)
            {
                Console.WriteLine($"Scope '{name}' not found.");
                return;
            }

            var descriptor = new OpenIddictScopeDescriptor();
            await scopeManager.PopulateAsync(descriptor, scope);

            if (!string.IsNullOrEmpty(displayName))
                descriptor.DisplayName = displayName;
            if (resources != null && resources.Length > 0)
            {
                descriptor.Resources.Clear();
                foreach (var resource in resources)
                {
                    descriptor.Resources.Add(resource);
                }
            }

            await scopeManager.UpdateAsync(scope, descriptor);
            Console.WriteLine($"Scope '{name}' updated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating scope: {ex.Message}");
        }
    }

    static async Task DeleteScope(string name)
    {
        try
        {
            var host = CreateHostBuilder().Build();
            var scopeManager = host.Services.GetRequiredService<IOpenIddictScopeManager>();

            var scope = await scopeManager.FindByNameAsync(name);
            if (scope == null)
            {
                Console.WriteLine($"Scope '{name}' not found.");
                return;
            }

            await scopeManager.DeleteAsync(scope);
            Console.WriteLine($"Scope '{name}' deleted successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting scope: {ex.Message}");
        }
    }

    static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Add Entity Framework
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection"));
                });

                // Add OpenIddict
                services.AddOpenIddict()
                    .AddCore(options =>
                    {
                        options.UseEntityFrameworkCore()
                            .UseDbContext<ApplicationDbContext>();
                    });
            })
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .AddEnvironmentVariables();
            });
    }
}
