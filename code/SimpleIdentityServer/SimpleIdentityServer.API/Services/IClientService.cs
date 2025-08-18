using OpenIddict.Abstractions;

namespace SimpleIdentityServer.Services;

public interface IClientService
{
    Task SeedClientsAsync();
    Task<object?> GetClientAsync(string clientId);
    Task<IEnumerable<object>> GetAllClientsAsync();
} 