using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIdentityServer.CLI.Business
{
    public class ApplicationManagement
    {
        private IOpenIddictApplicationManager _applicationManager;

        public ApplicationManagement(IOpenIddictApplicationManager applicationManager)
        {
            _applicationManager = applicationManager;
        }

        public async Task ListApplications()
        {
            try
            {
                Console.WriteLine("OpenIddict Applications:");
                Console.WriteLine("=======================");

                await foreach (var application in _applicationManager.ListAsync())
                {
                    var clientId = await _applicationManager.GetClientIdAsync(application);
                    var displayName = await _applicationManager.GetDisplayNameAsync(application);
                    var permissions = await _applicationManager.GetPermissionsAsync(application);

                    Console.WriteLine($"Client ID: {clientId}");
                    Console.WriteLine($"Display Name: {displayName}");
                    Console.WriteLine($"Permissions: {string.Join(", ", permissions)}");
                    Console.WriteLine("---");
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("ConnectionString"))
            {
                Console.WriteLine("Database Connection Error:");
                Console.WriteLine("=========================");
                Console.WriteLine("The application cannot connect to the database. This usually means:");
                Console.WriteLine("1. The appsettings.json file is missing or not found");
                Console.WriteLine("2. The connection string is not properly configured");
                Console.WriteLine("3. The database server is not running");
                Console.WriteLine();
                Console.WriteLine($"Error details: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Please ensure:");
                Console.WriteLine("- appsettings.json exists in the application directory");
                Console.WriteLine("- The DefaultConnection string is properly configured");
                Console.WriteLine("- The database server is accessible");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing applications: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task GetApplication(string clientId)
        {
            try
            {
                var application = await _applicationManager.FindByClientIdAsync(clientId);
                if (application == null)
                {
                    Console.WriteLine($"Application with Client ID '{clientId}' not found.");
                    return;
                }

                var displayName = await _applicationManager.GetDisplayNameAsync(application);
                var permissions = await _applicationManager.GetPermissionsAsync(application);

                Console.WriteLine($"Application Details for '{clientId}':");
                Console.WriteLine("=================================");
                Console.WriteLine($"Client ID: {clientId}");
                Console.WriteLine($"Display Name: {displayName}");
                Console.WriteLine($"Permissions: {string.Join(", ", permissions)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting application: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task AddApplication(string clientId, string clientSecret, string displayName, string[] permissions)
        {
            try
            {
                // Check if application already exists
                var existingApp = await _applicationManager.FindByClientIdAsync(clientId);
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

                await _applicationManager.CreateAsync(descriptor);
                Console.WriteLine($"Application '{clientId}' created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating application: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task UpdateApplication(string clientId, string? clientSecret, string? displayName, string[]? permissions)
        {
            try
            {
                var application = await _applicationManager.FindByClientIdAsync(clientId);
                if (application == null)
                {
                    Console.WriteLine($"Application with Client ID '{clientId}' not found.");
                    return;
                }

                var descriptor = new OpenIddictApplicationDescriptor();
                await _applicationManager.PopulateAsync(descriptor, application);

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

                await _applicationManager.UpdateAsync(application, descriptor);
                Console.WriteLine($"Application '{clientId}' updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating application: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task DeleteApplication(string clientId)
        {
            try
            {
                var application = await _applicationManager.FindByClientIdAsync(clientId);
                if (application == null)
                {
                    Console.WriteLine($"Application with Client ID '{clientId}' not found.");
                    return;
                }

                await _applicationManager.DeleteAsync(application);
                Console.WriteLine($"Application '{clientId}' deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting application: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
