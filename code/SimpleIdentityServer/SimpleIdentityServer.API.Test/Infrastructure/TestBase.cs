using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SimpleIdentityServer.API.Test.Infrastructure;

[Collection("Test Collection")]
public abstract class TestBase : IAsyncLifetime
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected TestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await Factory.SeedTestDataAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected StringContent CreateFormContent(Dictionary<string, string> formData)
    {
        var encodedContent = formData
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}")
            .Aggregate((x, y) => $"{x}&{y}");
        
        return new StringContent(encodedContent, Encoding.UTF8, "application/x-www-form-urlencoded");
    }

    protected async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
            return default;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<T>(content, options);
    }

    protected async Task<string> GetAccessTokenAsync(string clientId, string clientSecret, string? scope = null)
    {
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        };

        if (!string.IsNullOrWhiteSpace(scope))
        {
            tokenRequest["scope"] = scope;
        }

        var response = await Client.PostAsync("/connect/token", CreateFormContent(tokenRequest));
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to get access token: {errorContent}");
        }

        var tokenResponse = await DeserializeResponseAsync<TokenResponse>(response);
        return tokenResponse?.AccessToken ?? throw new InvalidOperationException("No access token received");
    }
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string? Scope { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? ErrorDescription { get; set; }
}

public class IntrospectionResponse
{
    public bool Active { get; set; }
    public string? Scope { get; set; }
    public string? ClientId { get; set; }
    public string? Username { get; set; }
    public string? TokenType { get; set; }
    public long? Exp { get; set; }
    public long? Iat { get; set; }
    public string? Sub { get; set; }
    public Dictionary<string, object>? CustomClaims { get; set; }
    public string[]? Aud { get; set; }
    public string? ScopeAccess { get; set; }
}
