# Identity Provider Integration Guide

This guide explains how to integrate the Simple Identity Server with a .NET 8.0 Resource API and client applications.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Step 1: Register Client in Identity Provider](#step-1-register-client-in-identity-provider)
4. [Step 2: Create Resource API](#step-2-create-resource-api)
5. [Step 3: Implement Token Validation](#step-3-implement-token-validation)
6. [Step 4: Create Client Application](#step-4-create-client-application)
7. [Testing the Integration](#testing-the-integration)
8. [Security Best Practices](#security-best-practices)
9. [Troubleshooting](#troubleshooting)

## Overview

This guide demonstrates how to:
- Register a client application in the identity provider
- Create a secure resource API that validates tokens
- Implement a client application that obtains and uses access tokens
- Validate tokens using both JWT validation and introspection

## Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Client App    │    │ Identity Server  │    │  Resource API   │
│                 │    │                  │    │                 │
│ 1. Request      │───▶│ 2. Validate      │    │ 4. Validate     │
│    Token        │    │    Credentials   │    │    Token        │
│                 │    │                  │    │                 │
│ 3. Use Token    │    │ 5. Return Token  │    │ 6. Return Data  │
│    in Requests  │◀───│                  │    │◀────────────────│
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## Step 1: Register Client in Identity Provider

### 1.1 Add New Client Using CLI

Use the Simple Identity Server CLI to add your new client:

```bash
cd code/SimpleIdentityServer/SimpleIdentityServer.CLI
dotnet run -- app add --client-id "my-resource-api-client" --client-secret "your-secure-secret-here" --display-name "My Resource API Client" --permissions "ept:token" "ept:introspection" "gt:client_credentials" "scp:email" "scp:profile" "scp:roles" "scp:api1.read" "scp:api1.write"
```

You can also verify the client was created successfully:

```bash
dotnet run -- app get --client-id "my-resource-api-client"
```

Or list all applications:

```bash
dotnet run -- app list
```

**Note**: The CLI uses OpenIddict's permission format with prefixes:
- `ept:` for endpoints (e.g., `ept:token`, `ept:introspection`)
- `gt:` for grant types (e.g., `gt:client_credentials`)
- `scp:` for scopes (e.g., `scp:email`, `scp:api1.read`)

### 1.2 Verify Client Registration

The CLI command directly adds the client to the database, so no Identity Server restart is required. The client is immediately available for use. You can verify this by checking the application list or testing token requests directly.

## Step 2: Create Resource API

sample existing project is [Resource.API](code\SimpleIdentityServer\Resource.API).

### 2.1 Create New .NET 8.0 Web API Project

```bash
dotnet new webapi -n MyResourceApi
cd MyResourceApi
```

### 2.2 Install Required NuGet Packages

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.IdentityModel.Tokens
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package OpenIddict.Validation.AspNetCore
dotnet add package OpenIddict.Validation.ServerIntegration
```

### 2.3 Configure appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "IdentityServer": {
    "Authority": "https://localhost:7443",
    "Audience": "api1",
    "IntrospectionEndpoint": "https://localhost:7443/connect/introspect",
    "ClientId": "my-resource-api-client",
    "ClientSecret": "your-secure-secret-here"
  }
}
```

### 2.4 Configure Program.cs

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Validation.ServerIntegration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["IdentityServer:Authority"];
        options.Audience = builder.Configuration["IdentityServer:Audience"];
        options.RequireHttpsMetadata = false; // Set to true in production
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["IdentityServer:Authority"],
            ValidAudience = builder.Configuration["IdentityServer:Audience"]
        };
    });

// Configure OpenIddict Validation (for introspection)
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer(builder.Configuration["IdentityServer:Authority"]);
        options.AddAudiences(builder.Configuration["IdentityServer:Audience"]);
        
        // Configure the validation handler to use introspection
        options.UseIntrospection()
               .SetClientId(builder.Configuration["IdentityServer:ClientId"])
               .SetClientSecret(builder.Configuration["IdentityServer:ClientSecret"]);
        
        // Configure the validation handler to use the local server.
        options.UseLocalServer();
        
        // Configure the validation handler to use ASP.NET Core.
        options.UseAspNetCore();
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireApi1Read", policy =>
        policy.RequireClaim("scope", "api1.read"));
    
    options.AddPolicy("RequireApi1Write", policy =>
        policy.RequireClaim("scope", "api1.write"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Step 3: Implement Token Validation

### 3.1 Create Protected API Controller

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyResourceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "RequireApi1Read")]
    public IActionResult Get()
    {
        var clientId = User.FindFirst("client_id")?.Value;
        var role = User.FindFirst("role")?.Value;
        
        return Ok(new
        {
            message = "Data retrieved successfully",
            clientId = clientId,
            role = role,
            timestamp = DateTime.UtcNow,
            data = new[] { "item1", "item2", "item3" }
        });
    }

    [HttpPost]
    [Authorize(Policy = "RequireApi1Write")]
    public IActionResult Create([FromBody] object data)
    {
        var clientId = User.FindFirst("client_id")?.Value;
        
        return Ok(new
        {
            message = "Data created successfully",
            clientId = clientId,
            timestamp = DateTime.UtcNow,
            createdData = data
        });
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult GetPublic()
    {
        return Ok(new
        {
            message = "Public data - no authentication required",
            timestamp = DateTime.UtcNow
        });
    }
}
```

### 3.2 Create Custom Token Validation Service

```csharp
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace MyResourceApi.Services;

public interface ITokenValidationService
{
    Task<bool> ValidateTokenAsync(string token);
    Task<Dictionary<string, object>> GetTokenInfoAsync(string token);
}

public class TokenValidationService : ITokenValidationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public TokenValidationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var request = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _configuration["IdentityServer:ClientId"]),
                new KeyValuePair<string, string>("client_secret", _configuration["IdentityServer:ClientSecret"]),
                new KeyValuePair<string, string>("token", token)
            });

            var response = await _httpClient.PostAsync(
                _configuration["IdentityServer:IntrospectionEndpoint"], 
                request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                
                return result?.ContainsKey("active") == true && 
                       result["active"].ToString()?.ToLower() == "true";
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Token validation error: {ex.Message}");
        }

        return false;
    }

    public async Task<Dictionary<string, object>> GetTokenInfoAsync(string token)
    {
        try
        {
            var request = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _configuration["IdentityServer:ClientId"]),
                new KeyValuePair<string, string>("client_secret", _configuration["IdentityServer:ClientSecret"]),
                new KeyValuePair<string, string>("token", token)
            });

            var response = await _httpClient.PostAsync(
                _configuration["IdentityServer:IntrospectionEndpoint"], 
                request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Dictionary<string, object>>(content) ?? new Dictionary<string, object>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token info retrieval error: {ex.Message}");
        }

        return new Dictionary<string, object>();
    }
}
```

Register the service in `Program.cs`:

```csharp
builder.Services.AddHttpClient<ITokenValidationService, TokenValidationService>();
```

## Step 4: Create Client Application

sample existing project is [Client.App](code\SimpleIdentityServer\Client.App).

or you can create a new console application.

### 4.1 Create Console Client Application

```csharp
using System.Net.Http.Headers;
using System.Text;

namespace MyClientApp;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private const string IdentityServerUrl = "https://localhost:7443";
    private const string ResourceApiUrl = "https://localhost:7002"; // Your resource API URL
    private const string ClientId = "my-resource-api-client";
    private const string ClientSecret = "your-secure-secret-here";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Resource API Client");
        Console.WriteLine("===================");

        try
        {
            // Step 1: Get access token
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Failed to obtain access token.");
                return;
            }

            Console.WriteLine($"Access Token: {token}");
            Console.WriteLine();

            // Step 2: Use token to access protected resources
            await AccessProtectedResourceAsync(token, "GET", "/api/data");
            await AccessProtectedResourceAsync(token, "POST", "/api/data", new { name = "test", value = 123 });

            // Step 3: Access public endpoint (no token needed)
            await AccessPublicResourceAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static async Task<string> GetAccessTokenAsync()
    {
        Console.WriteLine("1. Requesting access token...");

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("client_secret", ClientSecret),
            new KeyValuePair<string, string>("scope", "api1.read api1.write")
        });

        var response = await httpClient.PostAsync($"{IdentityServerUrl}/connect/token", tokenRequest);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Token Response: {content}");
            
            // Parse the JSON response to extract the access_token
            var tokenStart = content.IndexOf("\"access_token\":\"") + 16;
            var tokenEnd = content.IndexOf("\"", tokenStart);
            return content.Substring(tokenStart, tokenEnd - tokenStart);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {response.StatusCode} - {errorContent}");
            return null;
        }
    }

    static async Task AccessProtectedResourceAsync(string token, string method, string endpoint, object data = null)
    {
        Console.WriteLine($"\n2. Accessing protected resource: {method} {endpoint}");

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        HttpResponseMessage response;
        
        if (method.ToUpper() == "GET")
        {
            response = await httpClient.GetAsync($"{ResourceApiUrl}{endpoint}");
        }
        else if (method.ToUpper() == "POST")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            response = await httpClient.PostAsync($"{ResourceApiUrl}{endpoint}", content);
        }
        else
        {
            Console.WriteLine($"Unsupported method: {method}");
            return;
        }

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Success: {responseContent}");
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }
    }

    static async Task AccessPublicResourceAsync()
    {
        Console.WriteLine("\n3. Accessing public resource");

        // Clear authorization header for public endpoint
        httpClient.DefaultRequestHeaders.Authorization = null;
        
        var response = await httpClient.GetAsync($"{ResourceApiUrl}/api/data/public");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Public Resource: {content}");
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode}");
        }
    }
}
```

## Testing the Integration

### 5.1 Start Identity Server

```bash
cd simple-identity/code
dotnet run
```

### 5.2 Start Resource API

```bash
cd MyResourceApi
dotnet run
```

### 5.3 Run Client Application

```bash
cd MyClientApp
dotnet run
```

### 5.4 Expected Output

```
Resource API Client
===================
1. Requesting access token...
Token Response: {"access_token":"eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...","expires_in":3600,"token_type":"Bearer","scope":"api1.read api1.write"}

Access Token: eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...

2. Accessing protected resource: GET /api/data
Success: {"message":"Data retrieved successfully","clientId":"my-resource-api-client","role":"service","timestamp":"2024-01-15T10:30:00Z","data":["item1","item2","item3"]}

2. Accessing protected resource: POST /api/data
Success: {"message":"Data created successfully","clientId":"my-resource-api-client","timestamp":"2024-01-15T10:30:00Z","createdData":{"name":"test","value":123}}

3. Accessing public resource
Public Resource: {"message":"Public data - no authentication required","timestamp":"2024-01-15T10:30:00Z"}
```

## Security Best Practices

### 6.1 Token Management

- **Store tokens securely**: Never store tokens in plain text or client-side storage
- **Use HTTPS**: Always use HTTPS in production
- **Validate tokens**: Always validate tokens on the server side
- **Check scopes**: Verify that the client has the required scopes
- **Monitor token expiration**: Implement proper token refresh logic

### 6.2 Client Registration

- **Use strong secrets**: Generate cryptographically strong client secrets
- **Limit scopes**: Only grant the minimum required scopes
- **Regular rotation**: Rotate client secrets regularly
- **Audit access**: Log and monitor client access patterns

### 6.3 API Security

- **Input validation**: Validate all input parameters
- **Rate limiting**: Implement rate limiting to prevent abuse
- **CORS configuration**: Configure CORS properly
- **Error handling**: Don't expose sensitive information in error messages

## Troubleshooting

### Common Issues

1. **"unauthorized_client" error**
   - Check that the client is registered in the identity provider
   - Verify client ID and secret are correct
   - Ensure the client has the required permissions

2. **"invalid_token" error**
   - Check that the token is not expired
   - Verify the token format is correct
   - Ensure the audience matches the expected value

3. **"insufficient_scope" error**
   - Check that the client has the required scopes
   - Verify the scope is included in the token request

4. **CORS errors**
   - Configure CORS in the resource API
   - Ensure the identity provider allows the client origin

### Debug Tips

1. **Enable detailed logging**:
```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});
```

2. **Use Swagger UI** to test endpoints directly

3. **Check token contents** using JWT debugger tools

4. **Monitor network traffic** using browser developer tools

### Testing Checklist

- [ ] Identity server starts without errors
- [ ] Client can obtain access token
- [ ] Resource API validates tokens correctly
- [ ] Protected endpoints require authentication
- [ ] Public endpoints work without authentication
- [ ] Scope-based authorization works
- [ ] Token expiration is handled properly
- [ ] Error responses are appropriate

## Additional Resources

- [OpenIddict Documentation](https://documentation.openiddict.com/)
- [OAuth 2.0 Specification](https://tools.ietf.org/html/rfc6749)
- [JWT Specification](https://tools.ietf.org/html/rfc7519)
- [Token Introspection RFC](https://tools.ietf.org/html/rfc7662) 