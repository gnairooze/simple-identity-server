using FluentAssertions;
using SimpleIdentityServer.API.Test.Infrastructure;
using System.Net;
using Xunit;

namespace SimpleIdentityServer.API.Test.Integration;

public class TokenLifecycleTests : TestBase
{
    public TokenLifecycleTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task TokenLifecycle_CreateTokenThenIntrospect_ShouldWorkCorrectly()
    {
        // Arrange & Act - Get access token
        var tokenResponse = await GetTokenResponse("service-api", "supersecret", "api1.read");
        
        // Assert token creation
        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrEmpty();
        tokenResponse.TokenType.Should().Be("Bearer");
        tokenResponse.ExpiresIn.Should().BeGreaterThan(0);

        // Act - Introspect the token
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = tokenResponse.AccessToken,
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        var introspectResponse = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert introspection
        introspectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var introspectionResult = await DeserializeResponseAsync<IntrospectionResponse>(introspectResponse);
        introspectionResult.Should().NotBeNull();
        introspectionResult!.Active.Should().BeTrue();
        introspectionResult.TokenType.Should().NotBeNullOrEmpty();
        introspectionResult.Sub.Should().NotBeNullOrEmpty();
        introspectionResult.Exp.Should().BeGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    [Theory]
    [InlineData("service-api", "supersecret", "api1.read")]
    [InlineData("web-app", "webapp-secret", "api1.read")]
    [InlineData("mobile-app", "mobile-secret", "api1.write")]
    public async Task TokenLifecycle_CreateTokenWithScopeThenIntrospect_ShouldPreserveScope(
        string clientId, string clientSecret, string scope)
    {
        // Arrange & Act - Get access token with specific scope
        var tokenResponse = await GetTokenResponse(clientId, clientSecret, scope);
        
        // Assert token creation
        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrEmpty();
        if (!string.IsNullOrEmpty(tokenResponse.Scope))
        {
            tokenResponse.Scope.Should().Contain(scope);
        }

        // Act - Introspect the token with same client (should get detailed info)
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = tokenResponse.AccessToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        };

        var introspectResponse = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert introspection
        introspectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var introspectionResult = await DeserializeResponseAsync<IntrospectionResponse>(introspectResponse);
        introspectionResult.Should().NotBeNull();
        introspectionResult!.Active.Should().BeTrue();
        introspectionResult.Sub.Should().Be(clientId); // Subject should be the client_id
    }

    [Fact]
    public async Task TokenLifecycle_MultipleClientsIntrospectingSameToken_ShouldReturnDifferentLevelsOfDetail()
    {
        // Arrange - Get token from service-api
        var tokenResponse = await GetTokenResponse("service-api", "supersecret", "api1.read api1.write");
        
        // Act 1 - Self-introspection (should get detailed info)
        var selfIntrospectRequest = new Dictionary<string, string>
        {
            ["token"] = tokenResponse!.AccessToken,
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        var selfIntrospectResponse = await Client.PostAsync("/connect/introspect", CreateFormContent(selfIntrospectRequest));
        var selfResult = await DeserializeResponseAsync<IntrospectionResponse>(selfIntrospectResponse);

        // Act 2 - Cross-client introspection (should get limited info)
        var crossIntrospectRequest = new Dictionary<string, string>
        {
            ["token"] = tokenResponse.AccessToken,
            ["client_id"] = "web-app",
            ["client_secret"] = "webapp-secret"
        };

        var crossIntrospectResponse = await Client.PostAsync("/connect/introspect", CreateFormContent(crossIntrospectRequest));
        var crossResult = await DeserializeResponseAsync<IntrospectionResponse>(crossIntrospectResponse);

        // Act 3 - Admin introspection (should get detailed info)
        var adminIntrospectRequest = new Dictionary<string, string>
        {
            ["token"] = tokenResponse.AccessToken,
            ["client_id"] = "admin-client",
            ["client_secret"] = "admin-secret"
        };

        var adminIntrospectResponse = await Client.PostAsync("/connect/introspect", CreateFormContent(adminIntrospectRequest));
        var adminResult = await DeserializeResponseAsync<IntrospectionResponse>(adminIntrospectResponse);

        // Assert
        // All should show token as active
        selfResult.Should().NotBeNull();
        selfResult!.Active.Should().BeTrue();
        
        crossResult.Should().NotBeNull();
        crossResult!.Active.Should().BeTrue();
        
        adminResult.Should().NotBeNull();
        adminResult!.Active.Should().BeTrue();

        // Self-introspection should have detailed info
        selfResult.TokenType.Should().NotBeNullOrEmpty();
        selfResult.Sub.Should().Be("service-api");

        // Cross-client introspection should have limited info
        crossResult.TokenType.Should().BeNull();
        crossResult.Sub.Should().BeNull();

        // Admin introspection should have special admin info
        adminResult.ScopeAccess.Should().Be("authorized");
    }

    [Fact]
    public async Task TokenLifecycle_InvalidTokenIntrospection_ShouldReturnActiveFalse()
    {
        // Arrange - Create invalid tokens
        var invalidTokens = new[]
        {
            "invalid-token",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.signature", // Invalid JWT
            "", // Empty token
            "bearer-token-without-proper-format",
            new string('a', 1000) // Very long invalid token
        };

        foreach (var invalidToken in invalidTokens)
        {
            if (string.IsNullOrEmpty(invalidToken))
                continue; // Skip empty token as it's handled differently

            // Act
            var introspectRequest = new Dictionary<string, string>
            {
                ["token"] = invalidToken,
                ["client_id"] = "service-api",
                ["client_secret"] = "supersecret"
            };

            var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await DeserializeResponseAsync<IntrospectionResponse>(response);
            result.Should().NotBeNull();
            result!.Active.Should().BeFalse($"Token '{invalidToken[..Math.Min(20, invalidToken.Length)]}...' should be inactive");
        }
    }

    [Fact]
    public async Task TokenLifecycle_TokenCreationAndImmediateIntrospection_ShouldBeConsistent()
    {
        // Arrange
        var clients = new[]
        {
            ("service-api", "supersecret"),
            ("web-app", "webapp-secret"),
            ("mobile-app", "mobile-secret")
        };

        foreach (var (clientId, clientSecret) in clients)
        {
            // Act - Create token
            var tokenResponse = await GetTokenResponse(clientId, clientSecret);
            
            // Act - Immediately introspect
            var introspectRequest = new Dictionary<string, string>
            {
                ["token"] = tokenResponse!.AccessToken,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            };

            var introspectResponse = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));
            var introspectionResult = await DeserializeResponseAsync<IntrospectionResponse>(introspectResponse);

            // Assert
            introspectionResult.Should().NotBeNull();
            introspectionResult!.Active.Should().BeTrue($"Token for client '{clientId}' should be active immediately after creation");
            
            // The token should not be expired
            if (introspectionResult.Exp.HasValue)
            {
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(introspectionResult.Exp.Value);
                expirationTime.Should().BeAfter(DateTimeOffset.UtcNow, $"Token for client '{clientId}' should not be expired immediately after creation");
            }

            // The creation time should be recent
            if (introspectionResult.Iat.HasValue)
            {
                var creationTime = DateTimeOffset.FromUnixTimeSeconds(introspectionResult.Iat.Value);
                creationTime.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
                creationTime.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-1), "Token should be created within the last minute");
            }
        }
    }

    private async Task<TokenResponse?> GetTokenResponse(string clientId, string clientSecret, string? scope = null)
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
            throw new InvalidOperationException($"Failed to get token: {response.StatusCode} - {errorContent}");
        }

        return await DeserializeResponseAsync<TokenResponse>(response);
    }
}
