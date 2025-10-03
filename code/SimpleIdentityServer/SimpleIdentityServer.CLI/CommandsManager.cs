using SimpleIdentityServer.CLI.Business;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIdentityServer.CLI
{
    public class CommandsManager
    {
        public static RootCommand PrepareCommands(ApplicationManagement appMgr, ScopeManagement scpMgr, CertificateManagement certMgr)
        {
            var rootCommand = new RootCommand("OpenIddict Management CLI - Manage applications, scopes, and certificates");

            // Application commands
            var appCommand = new Command("app", "Manage OpenIddict applications");

            // List applications command
            var listAppsCommand = new Command("list", "List all applications");
            listAppsCommand.SetHandler(appMgr.ListApplications);

            // Get application command
            var getAppCommand = new Command("get", "Get application details");
            getAppCommand.AddOption(new Option<string>("--client-id", "Client ID of the application") { IsRequired = true });
            getAppCommand.SetHandler(appMgr.GetApplication, getAppCommand.Options.OfType<Option<string>>().First());

            // Add application command
            var addAppCommand = new Command("add", "Add a new application");
            addAppCommand.AddOption(new Option<string>("--client-id", "Client ID") { IsRequired = true });
            addAppCommand.AddOption(new Option<string>("--client-secret", "Client secret") { IsRequired = true });
            addAppCommand.AddOption(new Option<string>("--display-name", "Display name") { IsRequired = true });
            addAppCommand.AddOption(new Option<string[]>("--permissions", "Permissions can be repeated for multiple permissions") { IsRequired = true });
            addAppCommand.SetHandler(appMgr.AddApplication,
                addAppCommand.Options.OfType<Option<string>>().ElementAt(0),
                addAppCommand.Options.OfType<Option<string>>().ElementAt(1),
                addAppCommand.Options.OfType<Option<string>>().ElementAt(2),
                addAppCommand.Options.OfType<Option<string[]>>().First());

            // Update application command
            var updateAppCommand = new Command("update", "Update an existing application");
            updateAppCommand.AddOption(new Option<string>("--client-id", "Client ID") { IsRequired = true });
            updateAppCommand.AddOption(new Option<string>("--client-secret", "Client secret"));
            updateAppCommand.AddOption(new Option<string>("--display-name", "Display name"));
            updateAppCommand.AddOption(new Option<string[]>("--permissions", "Permissions can be repeated for multiple permissions"));
            updateAppCommand.SetHandler(appMgr.UpdateApplication,
                updateAppCommand.Options.OfType<Option<string>>().ElementAt(0),
                updateAppCommand.Options.OfType<Option<string>>().ElementAt(1),
                updateAppCommand.Options.OfType<Option<string>>().ElementAt(2),
                updateAppCommand.Options.OfType<Option<string[]>>().First());

            // Delete application command
            var deleteAppCommand = new Command("delete", "Delete an application");
            deleteAppCommand.AddOption(new Option<string>("--client-id", "Client ID") { IsRequired = true });
            deleteAppCommand.SetHandler(appMgr.DeleteApplication, deleteAppCommand.Options.OfType<Option<string>>().First());

            // Scope commands
            var scopeCommand = new Command("scope", "Manage OpenIddict scopes");

            // List scopes command
            var listScopesCommand = new Command("list", "List all scopes");
            listScopesCommand.SetHandler(scpMgr.ListScopes);

            // Get scope command
            var getScopeCommand = new Command("get", "Get scope details");
            getScopeCommand.AddOption(new Option<string>("--name", "Name of the scope") { IsRequired = true });
            getScopeCommand.SetHandler(scpMgr.GetScope, getScopeCommand.Options.OfType<Option<string>>().First());

            // Add scope command
            var addScopeCommand = new Command("add", "Add a new scope");
            addScopeCommand.AddOption(new Option<string>("--name", "Scope name") { IsRequired = true });
            addScopeCommand.AddOption(new Option<string>("--display-name", "Display name") { IsRequired = true });
            addScopeCommand.AddOption(new Option<string[]>("--resources", "Resources can be repeated for multiple resources") { IsRequired = true });
            addScopeCommand.SetHandler(scpMgr.AddScope,
                addScopeCommand.Options.OfType<Option<string>>().ElementAt(0),
                addScopeCommand.Options.OfType<Option<string>>().ElementAt(1),
                addScopeCommand.Options.OfType<Option<string[]>>().First());

            // Update scope command
            var updateScopeCommand = new Command("update", "Update an existing scope");
            updateScopeCommand.AddOption(new Option<string>("--name", "Scope name") { IsRequired = true });
            updateScopeCommand.AddOption(new Option<string>("--display-name", "Display name"));
            updateScopeCommand.AddOption(new Option<string[]>("--resources", "Resources can be repeated for multiple resources"));
            updateScopeCommand.SetHandler(scpMgr.UpdateScope,
                updateScopeCommand.Options.OfType<Option<string>>().ElementAt(0),
                updateScopeCommand.Options.OfType<Option<string>>().ElementAt(1),
                updateScopeCommand.Options.OfType<Option<string[]>>().First());

            // Delete scope command
            var deleteScopeCommand = new Command("delete", "Delete a scope");
            deleteScopeCommand.AddOption(new Option<string>("--name", "Scope name") { IsRequired = true });
            deleteScopeCommand.SetHandler(scpMgr.DeleteScope, deleteScopeCommand.Options.OfType<Option<string>>().First());

            // Add commands to parent commands
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

            // Certificate commands
            var certCommand = new Command("cert", "Manage certificates");

            // Create encryption certificate command
            var createEncryptionCertCommand = new Command("create-encryption", "Create encryption certificate");
            createEncryptionCertCommand.AddOption(new Option<string>("--path", "Path where to save the certificate") { IsRequired = true });
            createEncryptionCertCommand.AddOption(new Option<string>("--password", "Certificate password (if not provided, will use environment variable)"));
            createEncryptionCertCommand.SetHandler(certMgr.CreateEncryptionCertificate,
                createEncryptionCertCommand.Options.OfType<Option<string>>().ElementAt(0),
                createEncryptionCertCommand.Options.OfType<Option<string>>().ElementAt(1));

            // Create signing certificate command
            var createSigningCertCommand = new Command("create-signing", "Create signing certificate");
            createSigningCertCommand.AddOption(new Option<string>("--path", "Path where to save the certificate") { IsRequired = true });
            createSigningCertCommand.AddOption(new Option<string>("--password", "Certificate password (if not provided, will use environment variable)"));
            createSigningCertCommand.SetHandler(certMgr.CreateSigningCertificate,
                createSigningCertCommand.Options.OfType<Option<string>>().ElementAt(0),
                createSigningCertCommand.Options.OfType<Option<string>>().ElementAt(1));

            certCommand.AddCommand(createEncryptionCertCommand);
            certCommand.AddCommand(createSigningCertCommand);

            rootCommand.AddCommand(appCommand);
            rootCommand.AddCommand(scopeCommand);
            rootCommand.AddCommand(certCommand);
            return rootCommand;
        }
    }
}
