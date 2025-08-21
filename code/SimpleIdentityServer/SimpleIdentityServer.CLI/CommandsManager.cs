using SimpleIdentityServer.CLI.Business;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIdentityServer.CLI
{
    internal class CommandsManager
    {
        public static RootCommand PrepareCommands(ApplicationManagement appMgr, ScopeManagement scpMgr)
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
            listAppsCommand.SetHandler(appMgr.ListApplications);
            getAppCommand.SetHandler(appMgr.GetApplication, getAppCommand.Options.OfType<Option<string>>().First());
            addAppCommand.SetHandler(appMgr.AddApplication,
                addAppCommand.Options.OfType<Option<string>>().ElementAt(0),
                addAppCommand.Options.OfType<Option<string>>().ElementAt(1),
                addAppCommand.Options.OfType<Option<string>>().ElementAt(2),
                addAppCommand.Options.OfType<Option<string[]>>().First());
            updateAppCommand.SetHandler(appMgr.UpdateApplication,
                updateAppCommand.Options.OfType<Option<string>>().ElementAt(0),
                updateAppCommand.Options.OfType<Option<string>>().ElementAt(1),
                updateAppCommand.Options.OfType<Option<string>>().ElementAt(2),
                updateAppCommand.Options.OfType<Option<string[]>>().First());
            deleteAppCommand.SetHandler(appMgr.DeleteApplication, deleteAppCommand.Options.OfType<Option<string>>().First());

            listScopesCommand.SetHandler(scpMgr.ListScopes);
            getScopeCommand.SetHandler(scpMgr.GetScope, getScopeCommand.Options.OfType<Option<string>>().First());
            addScopeCommand.SetHandler(scpMgr.AddScope,
                addScopeCommand.Options.OfType<Option<string>>().ElementAt(0),
                addScopeCommand.Options.OfType<Option<string>>().ElementAt(1),
                addScopeCommand.Options.OfType<Option<string[]>>().First());
            updateScopeCommand.SetHandler(scpMgr.UpdateScope,
                updateScopeCommand.Options.OfType<Option<string>>().ElementAt(0),
                updateScopeCommand.Options.OfType<Option<string>>().ElementAt(1),
                updateScopeCommand.Options.OfType<Option<string[]>>().First());
            deleteScopeCommand.SetHandler(scpMgr.DeleteScope, deleteScopeCommand.Options.OfType<Option<string>>().First());

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
            return rootCommand;
        }
    }
}
