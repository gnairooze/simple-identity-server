using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using Microsoft.EntityFrameworkCore;
using System.CommandLine;
using System.CommandLine.Invocation;
using SimpleIdentityServer.CLI.Business;
using SimpleIdentityServer.CLI.Data;

namespace SimpleIdentityServer.CLI;

public class Program
{
    static async Task<int> Main(string[] args)
    {
        CreateManagers(out var appMgr, out var scpMgr);

        var rootCommand = CommandsManager.PrepareCommands(appMgr, scpMgr);

        return await rootCommand.InvokeAsync(args);
    }
    
    public static void CreateManagers(out ApplicationManagement appMgr, out ScopeManagement scpMgr)
    {
        var host = CreateHostBuilder().Build();
        var applicationManager = host.Services.GetRequiredService<IOpenIddictApplicationManager>();
        appMgr = new(applicationManager);

        var scopeManager = host.Services.GetRequiredService<IOpenIddictScopeManager>();
        scpMgr = new(scopeManager);
    }

    public static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Get connection string with validation
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    // Try to provide helpful error information
                    var configRoot = context.Configuration as IConfigurationRoot;
                    var debugView = configRoot?.GetDebugView();
                    
                    throw new InvalidOperationException(
                        $"Connection string 'DefaultConnection' not found. " +
                        $"Current working directory: {Directory.GetCurrentDirectory()}\n" +
                        $"Configuration sources: {string.Join(", ", configRoot?.Providers?.Select(p => p.GetType().Name) ?? new[] { "Unknown" })}\n" +
                        $"Configuration debug view:\n{debugView}");
                }

                // Add Entity Framework
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlServer(connectionString);
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
                // Get the directory where the executable is located
                var baseDirectory = AppContext.BaseDirectory;
                var currentDirectory = Directory.GetCurrentDirectory();
                
                config.SetBasePath(baseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                
                // Also try current directory if different from base directory
                if (!string.Equals(baseDirectory, currentDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    config.AddJsonFile(Path.Combine(currentDirectory, "appsettings.json"), optional: true, reloadOnChange: true)
                        .AddJsonFile(Path.Combine(currentDirectory, $"appsettings.{context.HostingEnvironment.EnvironmentName}.json"), optional: true, reloadOnChange: true);
                }
                
                config.AddEnvironmentVariables();
            });
    }
}
