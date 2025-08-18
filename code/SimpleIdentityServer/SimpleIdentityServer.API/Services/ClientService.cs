using OpenIddict.Abstractions;

namespace SimpleIdentityServer.Services;

public class ClientService : IClientService
{
    private readonly IOpenIddictApplicationManager _applicationManager;

    public ClientService(IOpenIddictApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    public async Task SeedClientsAsync()
    {
        // Check if clients already exist and update them if needed
        var serviceApiClient = await _applicationManager.FindByClientIdAsync("service-api");
        if (serviceApiClient != null)
        {
            // Update existing client to include introspection permission
            var permissions = await _applicationManager.GetPermissionsAsync(serviceApiClient);
            if (!permissions.Contains(OpenIddictConstants.Permissions.Endpoints.Introspection))
            {
                var descriptor = new OpenIddictApplicationDescriptor();
                await _applicationManager.PopulateAsync(descriptor, serviceApiClient);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
                await _applicationManager.UpdateAsync(serviceApiClient, descriptor);
            }
            
            // Update other clients as well
            await UpdateClientWithIntrospectionPermission("web-app");
            await UpdateClientWithIntrospectionPermission("mobile-app");
            return;
        }

        // Create the service-api client as specified in the specs
        await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "service-api",
            ClientSecret = "supersecret",
            DisplayName = "Service API Client",
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Introspection,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                "api1.read",
                "api1.write"
            }
        });

        // Create additional example clients
        await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "web-app",
            ClientSecret = "webapp-secret",
            DisplayName = "Web Application Client",
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Introspection,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                "api1.read"
            }
        });

        await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "mobile-app",
            ClientSecret = "mobile-secret",
            DisplayName = "Mobile Application Client",
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Introspection,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                "api1.read",
                "api1.write"
            }
        });
    }

    private async Task UpdateClientWithIntrospectionPermission(string clientId)
    {
        var client = await _applicationManager.FindByClientIdAsync(clientId);
        if (client != null)
        {
            var permissions = await _applicationManager.GetPermissionsAsync(client);
            if (!permissions.Contains(OpenIddictConstants.Permissions.Endpoints.Introspection))
            {
                var descriptor = new OpenIddictApplicationDescriptor();
                await _applicationManager.PopulateAsync(descriptor, client);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
                await _applicationManager.UpdateAsync(client, descriptor);
            }
        }
    }

    public async Task<object?> GetClientAsync(string clientId)
    {
        var client = await _applicationManager.FindByClientIdAsync(clientId);
        if (client == null)
        {
            return null;
        }

        return new
        {
            ClientId = await _applicationManager.GetIdAsync(client) ?? string.Empty,
            DisplayName = await _applicationManager.GetDisplayNameAsync(client) ?? string.Empty,
            Permissions = await _applicationManager.GetPermissionsAsync(client)
        };
    }

    public async Task<IEnumerable<object>> GetAllClientsAsync()
    {
        var clients = new List<object>();
        await foreach (var client in _applicationManager.ListAsync())
        {
            clients.Add(new
            {
                ClientId = await _applicationManager.GetIdAsync(client) ?? string.Empty,
                DisplayName = await _applicationManager.GetDisplayNameAsync(client) ?? string.Empty,
                Permissions = await _applicationManager.GetPermissionsAsync(client)
            });
        }
        return clients;
    }
} 