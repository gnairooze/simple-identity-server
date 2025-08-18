using OpenIddict.Abstractions;
using System.Globalization;

namespace SimpleIdentityServer.Services;

public class ScopeService : IScopeService
{
    private readonly IOpenIddictScopeManager _scopeManager;

    public ScopeService(IOpenIddictScopeManager scopeManager)
    {
        _scopeManager = scopeManager;
    }

    public async Task SeedScopesAsync()
    {
        // Check if scopes already exist
        if (await _scopeManager.FindByNameAsync("api1.read") != null)
        {
            return; // Scopes already seeded
        }

        // Create api1.read scope with claims as specified in the specs
        await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "api1.read",
            DisplayName = "Read access to API 1",
            Resources = { "api1" },
            Descriptions = { [CultureInfo.InvariantCulture] = "Allows read access to API 1 resources" }
        });

        // Create api1.write scope with claims
        await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "api1.write",
            DisplayName = "Write access to API 1",
            Resources = { "api1" },
            Descriptions = { [CultureInfo.InvariantCulture] = "Allows write access to API 1 resources" }
        });

        // Create additional scopes for different APIs
        await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "api2.read",
            DisplayName = "Read access to API 2",
            Resources = { "api2" },
            Descriptions = { [CultureInfo.InvariantCulture] = "Allows read access to API 2 resources" }
        });

        await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "api2.write",
            DisplayName = "Write access to API 2",
            Resources = { "api2" },
            Descriptions = { [CultureInfo.InvariantCulture] = "Allows write access to API 2 resources" }
        });

        // Create admin scope for administrative access
        await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "admin",
            DisplayName = "Administrative access",
            Resources = { "admin-api" },
            Descriptions = { [CultureInfo.InvariantCulture] = "Allows administrative access to all APIs" }
        });
    }

    public async Task<object?> GetScopeAsync(string scopeName)
    {
        var scope = await _scopeManager.FindByNameAsync(scopeName);
        if (scope == null)
        {
            return null;
        }

        return new
        {
            Name = await _scopeManager.GetNameAsync(scope) ?? string.Empty,
            DisplayName = await _scopeManager.GetDisplayNameAsync(scope) ?? string.Empty,
            Descriptions = await _scopeManager.GetDescriptionsAsync(scope),
            Resources = await _scopeManager.GetResourcesAsync(scope)
        };
    }

    public async Task<IEnumerable<object>> GetAllScopesAsync()
    {
        var scopes = new List<object>();
        await foreach (var scope in _scopeManager.ListAsync())
        {
            scopes.Add(new
            {
                Name = await _scopeManager.GetNameAsync(scope) ?? string.Empty,
                DisplayName = await _scopeManager.GetDisplayNameAsync(scope) ?? string.Empty,
                Descriptions = await _scopeManager.GetDescriptionsAsync(scope),
                Resources = await _scopeManager.GetResourcesAsync(scope)
            });
        }
        return scopes;
    }
} 