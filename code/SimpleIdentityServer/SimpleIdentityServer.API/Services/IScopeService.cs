using OpenIddict.Abstractions;

namespace SimpleIdentityServer.Services;

public interface IScopeService
{
    Task SeedScopesAsync();
    Task<object> GetScopeAsync(string scopeName);
    Task<IEnumerable<object>> GetAllScopesAsync();
} 