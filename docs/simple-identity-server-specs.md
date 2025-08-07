# identity server specs

## 0. Technology Stack

- OpenIdDict
- ASP.NET Core 8.0
- Entity Framework Core
- SQL Server
- Docker

## 1. Authentication Flow

- Use the Client Credentials Flow

## 2. Client Registration

For each client (API/service) that needs to authenticate:

- **Client ID**: A unique identifier for the client.
    
- **Client Secret or Credential**: Used to authenticate the client. a private key JWT.
    
- **Allowed Grant Types**: Set to `client_credentials` to restrict the flow to service-to-service scenarios.
    
- **Allowed Scopes**: Define which APIs/resources the client can access.
    

**Example Client Configuration (C#):**

```c#
new Client {
  ClientId = "service-api",
  ClientSecrets = { new Secret("supersecret".Sha256()) },
  AllowedGrantTypes = GrantTypes.ClientCredentials,
  AllowedScopes = { "api1.read", "api1.write" } 
}
```

## 3. Defining Scopes and Claims

- **Scopes**: Each scope can be linked to specific claims that will be included in the access token.
    
- **Claims**: Assertions about the client (such as client ID, roles, or custom attributes) that are embedded in the access token and used by APIs for authorization decisions.

**How to Include Claims:**

- Define which claims should be issued for each scope.
- Claims will be fetched from a database..

**Example Scope with Claims:**

```c#
new ApiScope("api1.read", "Read access to API 1") {
  UserClaims = { "role", "client_id", "custom_claim" }
}
```

## 4. Client Authentication Methods

- **Private Key JWT**: use asymmetric keys.
- **Mutual TLS (mTLS)**: For even stronger client identity assurance.

## 5. Token Configuration

- **Access Token Content**: Ensure the token includes the necessary claims for the API to make authorization decisions.
- **Lifetime**: Set appropriate token lifetimes for your use case.
- **Signing**: Use strong signing algorithms to protect token integrity.

## 6. API Configuration

- **Validate Access Tokens**: APIs must validate incoming tokens, check their scopes, and extract claims for authorization.
- **Authorization Logic**: Use the claims in the token (such as roles or custom attributes) to enforce access control within your APIs.

## 7. Best Practices

- **Audit and Monitor**: Keep logs of issued tokens and access for security reviews.

