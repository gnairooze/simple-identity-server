using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIdentityServer.CLI.Business
{
    internal class ScopeManagement
    {
        private IOpenIddictScopeManager _scopeManager;

        public ScopeManagement(IOpenIddictScopeManager scopeManager)
        {
            _scopeManager = scopeManager;
        }

        public async Task ListScopes()
        {
            try
            {
                Console.WriteLine("OpenIddict Scopes:");
                Console.WriteLine("==================");

                await foreach (var scope in _scopeManager.ListAsync())
                {
                    var name = await _scopeManager.GetNameAsync(scope);
                    var displayName = await _scopeManager.GetDisplayNameAsync(scope);
                    var resources = await _scopeManager.GetResourcesAsync(scope);

                    Console.WriteLine($"Name: {name}");
                    Console.WriteLine($"Display Name: {displayName}");
                    Console.WriteLine($"Resources: {string.Join(", ", resources)}");
                    Console.WriteLine("---");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing scopes: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task GetScope(string name)
        {
            try
            {
                var scope = await _scopeManager.FindByNameAsync(name);
                if (scope == null)
                {
                    Console.WriteLine($"Scope '{name}' not found.");
                    return;
                }

                var displayName = await _scopeManager.GetDisplayNameAsync(scope);
                var resources = await _scopeManager.GetResourcesAsync(scope);

                Console.WriteLine($"Scope Details for '{name}':");
                Console.WriteLine("=========================");
                Console.WriteLine($"Name: {name}");
                Console.WriteLine($"Display Name: {displayName}");
                Console.WriteLine($"Resources: {string.Join(", ", resources)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting scope: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task AddScope(string name, string displayName, string[] resources)
        {
            try
            {
                // Check if scope already exists
                var existingScope = await _scopeManager.FindByNameAsync(name);
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

                await _scopeManager.CreateAsync(descriptor);
                Console.WriteLine($"Scope '{name}' created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating scope: {ex.Message}");
                Console.WriteLine(ex.ToString());   
            }
        }

        public async Task UpdateScope(string name, string? displayName, string[]? resources)
        {
            try
            {
                var scope = await _scopeManager.FindByNameAsync(name);
                if (scope == null)
                {
                    Console.WriteLine($"Scope '{name}' not found.");
                    return;
                }

                var descriptor = new OpenIddictScopeDescriptor();
                await _scopeManager.PopulateAsync(descriptor, scope);

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

                await _scopeManager.UpdateAsync(scope, descriptor);
                Console.WriteLine($"Scope '{name}' updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating scope: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task DeleteScope(string name)
        {
            try
            {
                var scope = await _scopeManager.FindByNameAsync(name);
                if (scope == null)
                {
                    Console.WriteLine($"Scope '{name}' not found.");
                    return;
                }

                await _scopeManager.DeleteAsync(scope);
                Console.WriteLine($"Scope '{name}' deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting scope: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

    }
}
