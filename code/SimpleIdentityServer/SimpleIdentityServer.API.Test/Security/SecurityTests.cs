using FluentAssertions;
using SimpleIdentityServer.API.Test.Infrastructure;
using System.Net;
using System.Text;
using Xunit;

namespace SimpleIdentityServer.API.Test.Security;

public class SecurityTests : TestBase
{
    public SecurityTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Token_ShouldHaveSecurityHeaders()
    {
        // Arrange
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/token", CreateFormContent(tokenRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Security headers should be present
        response.Headers.Should().ContainKey("Cache-Control");
        var cacheControl = response.Headers.GetValues("Cache-Control").First();
        cacheControl.Should().Contain("no-store");
        cacheControl.Should().Contain("no-cache");
    }

    [Fact]
    public async Task Introspect_ShouldHaveSecurityHeaders()
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
        
        // Security headers should be present
        response.Headers.Should().ContainKey("Cache-Control");
        var cacheControl = response.Headers.GetValues("Cache-Control").First();
        cacheControl.Should().Contain("no-store");
        cacheControl.Should().Contain("no-cache");
    }

    [Theory]
    [InlineData("/connect/token")]
    [InlineData("/connect/introspect")]
    public async Task Endpoints_ShouldRejectGetRequests(string endpoint)
    {
        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Theory]
    [InlineData("/connect/token")]
    [InlineData("/connect/introspect")]
    public async Task Endpoints_ShouldRejectPutRequests(string endpoint)
    {
        // Act
        var response = await Client.PutAsync(endpoint, new StringContent(""));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Theory]
    [InlineData("/connect/token")]
    [InlineData("/connect/introspect")]
    public async Task Endpoints_ShouldRejectDeleteRequests(string endpoint)
    {
        // Act
        var response = await Client.DeleteAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Token_ShouldRejectSqlInjectionAttempts()
    {
        // Arrange - SQL injection attempts in various fields
        var sqlInjectionAttempts = new[]
        {
            "'; DROP TABLE Applications; --",
            "' OR '1'='1",
            "'; SELECT * FROM Applications; --",
            "admin'--",
            "' UNION SELECT * FROM Applications--"
        };

        foreach (var injection in sqlInjectionAttempts)
        {
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = injection,
                ["client_secret"] = "supersecret"
            };

            // Act
            var response = await Client.PostAsync("/connect/token", CreateFormContent(tokenRequest));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden, 
                $"SQL injection attempt '{injection}' should be rejected");
        }
    }

    [Fact]
    public async Task Introspect_ShouldRejectSqlInjectionAttempts()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var sqlInjectionAttempts = new[]
        {
            "'; DROP TABLE Tokens; --",
            "' OR '1'='1",
            "'; SELECT * FROM Tokens; --",
            "admin'--"
        };

        foreach (var injection in sqlInjectionAttempts)
        {
            var introspectRequest = new Dictionary<string, string>
            {
                ["token"] = accessToken,
                ["client_id"] = injection,
                ["client_secret"] = "supersecret"
            };

            // Act
            var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"SQL injection attempt '{injection}' should be rejected");
        }
    }

    [Fact]
    public async Task Token_ShouldRejectXssAttempts()
    {
        // Arrange - XSS attempts
        var xssAttempts = new[]
        {
            "<script>alert('xss')</script>",
            "javascript:alert('xss')",
            "<img src=x onerror=alert('xss')>",
            "';alert('xss');//"
        };

        foreach (var xss in xssAttempts)
        {
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = xss,
                ["client_secret"] = "supersecret"
            };

            // Act
            var response = await Client.PostAsync("/connect/token", CreateFormContent(tokenRequest));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"XSS attempt '{xss}' should be rejected");
        }
    }

    [Fact]
    public async Task Token_ShouldHandleLargePayloads()
    {
        // Arrange - Create very large client_id
        var largeClientId = new string('a', 10000);
        
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = largeClientId,
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/token", CreateFormContent(tokenRequest));

        // Assert
        // Should either reject due to size limits or handle gracefully
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, 
            HttpStatusCode.Forbidden,
            HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Introspect_ShouldHandleLargeTokens()
    {
        // Arrange - Create very large token
        var largeToken = new string('a', 10000);
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = largeToken,
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        // Should handle gracefully and return active: false
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.RequestEntityTooLarge);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await DeserializeResponseAsync<IntrospectionResponse>(response);
            result.Should().NotBeNull();
            result!.Active.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Token_ShouldRejectNullBytes()
    {
        // Arrange
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "service-api\0malicious",
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/token", CreateFormContent(tokenRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Introspect_ShouldRejectNullBytes()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = "service-api\0malicious",
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Token_ShouldRejectInvalidCharacters()
    {
        // Arrange - Various invalid characters
        var invalidCharacters = new[]
        {
            "client\r\nid",
            "client\tid",
            "client\vid",
            "client\fid",
            "client\0id"
        };

        foreach (var invalidClientId in invalidCharacters)
        {
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = invalidClientId,
                ["client_secret"] = "supersecret"
            };

            // Act
            var response = await Client.PostAsync("/connect/token", CreateFormContent(tokenRequest));

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Forbidden);
            // Additional assertion with message
            response.IsSuccessStatusCode.Should().BeFalse(
                $"Invalid character in client_id '{invalidClientId}' should be rejected");
        }
    }

    [Fact]
    public async Task Token_ShouldRejectMalformedUrlencodedData()
    {
        // Arrange - Malformed URL-encoded data
        var malformedData = new[]
        {
            "grant_type=client_credentials&client_id=service-api&client_secret=supersecret&%",
            "grant_type=client_credentials&client_id=service-api&client_secret=supersecret&%ZZ",
            "grant_type=client_credentials&client_id=service-api&client_secret=supersecret&invalid%encoding"
        };

        foreach (var data in malformedData)
        {
            var content = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");

            // Act
            var response = await Client.PostAsync("/connect/token", content);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Forbidden);
            // Additional assertion with message
            response.IsSuccessStatusCode.Should().BeFalse(
                $"Malformed data '{data}' should be rejected");
        }
    }

    [Fact]
    public async Task Token_ShouldRejectEmptyContentType()
    {
        // Arrange
        var tokenRequest = CreateFormContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        });

        tokenRequest.Headers.ContentType = null;

        // Act
        var response = await Client.PostAsync("/connect/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Introspect_ShouldRejectEmptyContentType()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");
        
        var introspectRequest = CreateFormContent(new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        });

        introspectRequest.Headers.ContentType = null;

        // Act
        var response = await Client.PostAsync("/connect/introspect", introspectRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public async Task Token_ShouldRejectWhitespaceOnlyValues(string whitespaceValue)
    {
        // Arrange
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = whitespaceValue,
            ["client_secret"] = "supersecret"
        };

        // Act
        var response = await Client.PostAsync("/connect/token", CreateFormContent(tokenRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
