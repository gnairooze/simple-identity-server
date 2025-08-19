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
            // Update existing clients to fix scope permissions
            await UpdateClientScopePermissions("service-api");
            await UpdateClientScopePermissions("web-app");
            await UpdateClientScopePermissions("mobile-app");
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
                "scp:api1.read",
                "scp:api1.write"
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
                "scp:api1.read"
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
                "scp:api1.read",
                "scp:api1.write"
            }
        });
    }

    private async Task UpdateClientScopePermissions(string clientId)
    {
        var client = await _applicationManager.FindByClientIdAsync(clientId);
        if (client == null) return;

        var permissions = await _applicationManager.GetPermissionsAsync(client);
        var needsUpdate = false;
        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, client);

        // Check if we need to fix scope permissions (convert api1.read to scp:api1.read)
        if (permissions.Contains("api1.read") && !permissions.Contains("scp:api1.read"))
        {
            descriptor.Permissions.Remove("api1.read");
            descriptor.Permissions.Add("scp:api1.read");
            needsUpdate = true;
        }

        if (permissions.Contains("api1.write") && !permissions.Contains("scp:api1.write"))
        {
            descriptor.Permissions.Remove("api1.write");
            descriptor.Permissions.Add("scp:api1.write");
            needsUpdate = true;
        }

        // Ensure introspection permission is present
        if (!permissions.Contains(OpenIddictConstants.Permissions.Endpoints.Introspection))
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            await _applicationManager.UpdateAsync(client, descriptor);
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