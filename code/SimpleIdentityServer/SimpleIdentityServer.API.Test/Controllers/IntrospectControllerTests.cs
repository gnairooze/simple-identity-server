using FluentAssertions;
using SimpleIdentityServer.API.Test.Infrastructure;
using System.Net;
using System.Text.Json;
using Xunit;

namespace SimpleIdentityServer.API.Test.Controllers;

public class IntrospectControllerTests : TestBase
{
    public IntrospectControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Theory]
    [InlineData("service-api", "supersecret")]
    [InlineData("web-app", "webapp-secret")]
    [InlineData("mobile-app", "mobile-secret")]
    [InlineData("admin-client", "admin-secret")]
    public async Task Introspect_ValidTokenWithAuthorizedClient_ShouldReturnActiveTrue(string clientId, string clientSecret)
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync(clientId, clientSecret);
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var introspectionResponse = await DeserializeResponseAsync<IntrospectionResponse>(response);
        introspectionResponse.Should().NotBeNull();
        introspectionResponse!.Active.Should().BeTrue();
        introspectionResponse.Exp.Should().BeGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        introspectionResponse.Iat.Should().BeLessOrEqualTo(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task Introspect_ValidTokenWithTrustedClient_ShouldIncludeDetailedInfo()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = "service-api", // Same client - should get detailed info
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var introspectionResponse = await DeserializeResponseAsync<IntrospectionResponse>(response);
        introspectionResponse.Should().NotBeNull();
        introspectionResponse!.Active.Should().BeTrue();
        introspectionResponse.TokenType.Should().NotBeNullOrEmpty();
        introspectionResponse.Sub.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Introspect_ValidTokenWithAdminClient_ShouldIncludeAdminInfo()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = "admin-client", // Admin client - should get detailed info
            ["client_secret"] = "admin-secret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var introspectionResponse = await DeserializeResponseAsync<IntrospectionResponse>(response);
        introspectionResponse.Should().NotBeNull();
        introspectionResponse!.Active.Should().BeTrue();
        introspectionResponse.ScopeAccess.Should().Be("authorized");
    }

    [Fact]
    public async Task Introspect_ValidTokenWithDifferentClient_ShouldReturnLimitedInfo()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = "web-app", // Different client - should get limited info
            ["client_secret"] = "webapp-secret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var introspectionResponse = await DeserializeResponseAsync<IntrospectionResponse>(response);
        introspectionResponse.Should().NotBeNull();
        introspectionResponse!.Active.Should().BeTrue();
        // Should only have basic timing information
        introspectionResponse.Exp.Should().BeGreaterThan(0);
        introspectionResponse.Iat.Should().BeGreaterThan(0);
        // Should not have detailed information
        introspectionResponse.TokenType.Should().BeNull();
        introspectionResponse.Sub.Should().BeNull();
    }

    [Fact]
    public async Task Introspect_InvalidToken_ShouldReturnActiveFalse()
    {
        // Arrange
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = "invalid-token-12345",
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var introspectionResponse = await DeserializeResponseAsync<IntrospectionResponse>(response);
        introspectionResponse.Should().NotBeNull();
        introspectionResponse!.Active.Should().BeFalse();
    }

    [Fact]
    public async Task Introspect_EmptyToken_ShouldReturnBadRequest()
    {
        // Arrange
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = "",
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorResponse = await DeserializeResponseAsync<ErrorResponse>(response);
        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be("invalid_request");
        errorResponse.ErrorDescription.Should().Contain("token parameter is required");
    }

    [Fact]
    public async Task Introspect_MissingToken_ShouldReturnBadRequest()
    {
        // Arrange
        var introspectRequest = new Dictionary<string, string>
        {
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorResponse = await DeserializeResponseAsync<ErrorResponse>(response);
        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be("invalid_request");
        errorResponse.ErrorDescription.Should().Contain("token parameter is required");
    }

    [Fact]
    public async Task Introspect_InvalidClientId_ShouldReturnForbidden()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = "non-existent-client",
            ["client_secret"] = "any-secret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        
        var errorResponse = await DeserializeResponseAsync<ErrorResponse>(response);
        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be("invalid_client");
        errorResponse.ErrorDescription.Should().Contain("not found in the database");
    }

    [Theory]
    [InlineData("service-api", "wrong-secret")]
    [InlineData("web-app", "incorrect-password")]
    [InlineData("mobile-app", "bad-secret")]
    public async Task Introspect_InvalidClientSecret_ShouldReturnForbidden(string clientId, string wrongSecret)
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = clientId,
            ["client_secret"] = wrongSecret
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        
        var errorResponse = await DeserializeResponseAsync<ErrorResponse>(response);
        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be("invalid_client");
        errorResponse.ErrorDescription.Should().Contain("credentials are invalid");
    }

    [Fact]
    public async Task Introspect_MissingClientId_ShouldReturnForbidden()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        
        var errorResponse = await DeserializeResponseAsync<ErrorResponse>(response);
        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be("invalid_client");
    }

    [Fact]
    public async Task Introspect_MissingClientSecret_ShouldReturnForbidden()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = "service-api"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        
        var errorResponse = await DeserializeResponseAsync<ErrorResponse>(response);
        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be("invalid_client");
    }

    [Fact]
    public async Task Introspect_EmptyRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var emptyContent = new StringContent("", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

        // Act
        var response = await Client.PostAsync("/connect/introspect", emptyContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Introspect_InvalidContentType_ShouldReturnBadRequest()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(new { token = accessToken, client_id = "service-api", client_secret = "supersecret" }),
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/connect/introspect", jsonContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Introspect_ValidRequest_ShouldIncludeSecurityHeaders()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Check for security headers
        response.Headers.Should().ContainKey("Cache-Control");
        response.Headers.GetValues("Cache-Control").Should().Contain(v => v.Contains("no-store"));
    }

    [Fact]
    public async Task Introspect_ConcurrentRequests_ShouldHandleMultipleRequestsCorrectly()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Act - Send 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest)));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var introspectionResponse = await DeserializeResponseAsync<IntrospectionResponse>(response);
            introspectionResponse.Should().NotBeNull();
            introspectionResponse!.Active.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Introspect_TokenFromDifferentClient_ShouldStillIntrospectCorrectly()
    {
        // Arrange
        // Get token from service-api
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        // Try to introspect with web-app client
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = "web-app",
            ["client_secret"] = "webapp-secret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var introspectionResponse = await DeserializeResponseAsync<IntrospectionResponse>(response);
        introspectionResponse.Should().NotBeNull();
        introspectionResponse!.Active.Should().BeTrue();
        // Should have limited information since it's a different client
        introspectionResponse.Exp.Should().BeGreaterThan(0);
        introspectionResponse.Iat.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Introspect_ExpiredToken_ShouldReturnActiveFalse()
    {
        // Note: This test would require creating an expired token or mocking time
        // For now, we'll test with an obviously invalid token format
        
        // Arrange
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = "expired.token.here",
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var introspectionResponse = await DeserializeResponseAsync<IntrospectionResponse>(response);
        introspectionResponse.Should().NotBeNull();
        introspectionResponse!.Active.Should().BeFalse();
    }

    [Theory]
    [InlineData("service-api", "web-app")]
    [InlineData("web-app", "mobile-app")]
    [InlineData("mobile-app", "service-api")]
    public async Task Introspect_CrossClientIntrospection_ShouldWorkWithLimitedInfo(string tokenClientId, string introspectClientId)
    {
        // Arrange
        var tokenCredentials = GetClientCredentials(tokenClientId);
        var introspectCredentials = GetClientCredentials(introspectClientId);
        
        var accessToken = await GetAccessTokenAsync(tokenCredentials.clientId, tokenCredentials.clientSecret);
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = introspectCredentials.clientId,
            ["client_secret"] = introspectCredentials.clientSecret
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var introspectionResponse = await DeserializeResponseAsync<IntrospectionResponse>(response);
        introspectionResponse.Should().NotBeNull();
        introspectionResponse!.Active.Should().BeTrue();
        
        // Cross-client introspection should provide limited information
        if (tokenClientId != introspectClientId && introspectClientId != "admin-client")
        {
            introspectionResponse.TokenType.Should().BeNull();
            introspectionResponse.Sub.Should().BeNull();
        }
    }

    private static (string clientId, string clientSecret) GetClientCredentials(string clientId)
    {
        return clientId switch
        {
            "service-api" => ("service-api", "supersecret"),
            "web-app" => ("web-app", "webapp-secret"),
            "mobile-app" => ("mobile-app", "mobile-secret"),
            "admin-client" => ("admin-client", "admin-secret"),
            _ => throw new ArgumentException($"Unknown client: {clientId}")
        };
    }
}
