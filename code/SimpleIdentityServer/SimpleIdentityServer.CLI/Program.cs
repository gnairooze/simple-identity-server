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

class Program
{
    static async Task<int> Main(string[] args)
    {
        CreateManagers(out var appMgr, out var scpMgr);

        var rootCommand = CommandsManager.PrepareCommands(appMgr, scpMgr);

        return await rootCommand.InvokeAsync(args);
    }
    
    static void CreateManagers(out ApplicationManagement appMgr, out ScopeManagement scpMgr)
    {
        var host = CreateHostBuilder().Build();
        var applicationManager = host.Services.GetRequiredService<IOpenIddictApplicationManager>();
        appMgr = new(applicationManager);

        var scopeManager = host.Services.GetRequiredService<IOpenIddictScopeManager>();
        scpMgr = new(scopeManager);
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
