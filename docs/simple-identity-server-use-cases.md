# Simple Identity Server API - Use Cases and Examples

This document provides comprehensive use cases for the Simple Identity Server API, including various token types, authentication flows, and practical examples using both Postman and cURL.

## Table of Contents

1. [Overview](#overview)
2. [Server Information](#server-information)
3. [Pre-configured Clients](#pre-configured-clients)
4. [Available Scopes](#available-scopes)
5. [Authentication Flow (Client Credentials)](#authentication-flow-client-credentials)
6. [Use Cases](#use-cases)
7. [Token Introspection](#token-introspection)
8. [Refresh Tokens](#refresh-tokens)
9. [Error Handling](#error-handling)
10. [Security Considerations](#security-considerations)

## Overview

The Simple Identity Server is an OAuth2/OpenID Connect identity server built using OpenIddict and ASP.NET Core 8.0. It supports:

- **Grant Type**: Client Credentials Flow (service-to-service authentication)
- **Token Types**: JWT Access Tokens (encrypted and signed)
- **Scopes**: Granular API access control
- **Introspection**: Token validation and metadata retrieval
- **Field-level Authorization**: Role-based data filtering

## Server Information

### Default Endpoints

| Endpoint | URL | Description |
|----------|-----|-------------|
| Server Info | `GET /Home` | Server information and available endpoints |
| Health Check | `GET /Home/health` | Server health status |
| Token | `POST /connect/token` | OAuth2 token endpoint |
| Introspection | `POST /connect/introspect` | Token introspection endpoint |
| Configuration | `GET /.well-known/openid-configuration` | OpenID Connect discovery |
| JWKS | `GET /.well-known/jwks` | JSON Web Key Set |

### Server Configuration

- **Access Token Lifetime**: 60 minutes
- **Refresh Token Lifetime**: 14 days
- **Rate Limiting**: 20 requests/minute for token endpoint, 50 requests/minute for introspection
- **Encryption**: Certificates for token encryption and signing

## Pre-configured Clients

| Client ID | Client Secret | Permissions | Purpose | Available Scopes |
|-----------|---------------|-------------|---------|------------------|
| `service-api` | `supersecret` | Full API access | Service-to-service communication | `api1.read`, `api1.write`, `email`, `profile`, `roles` |
| `web-app` | `webapp-secret` | Read access | Web application client | `api1.read`, `email`, `profile`, `roles` |
| `mobile-app` | `mobile-secret` | Full API access | Mobile application client | `api1.read`, `api1.write`, `email`, `profile`, `roles` |

## Available Scopes

| Scope | Description | Resource | Access Level |
|-------|-------------|----------|--------------|
| `api1.read` | Read access to API 1 | `api1` | Basic read operations |
| `api1.write` | Write access to API 1 | `api1` | Full CRUD operations |
| `api2.read` | Read access to API 2 | `api2` | Basic read operations |
| `api2.write` | Write access to API 2 | `api2` | Full CRUD operations |
| `admin` | Administrative access | `admin-api` | Full system administration |
| `email` | Email scope | Standard OpenID | Email claims |
| `profile` | Profile scope | Standard OpenID | Profile claims |
| `roles` | Roles scope | Standard OpenID | Role claims |

## Authentication Flow (Client Credentials)

```mermaid
sequenceDiagram
    participant Client
    participant IdentityServer
    participant ResourceAPI
    
    Client->>IdentityServer: POST /connect/token (client credentials)
    IdentityServer->>IdentityServer: Validate client credentials
    IdentityServer->>IdentityServer: Generate encrypted JWT token
    IdentityServer-->>Client: Access token + metadata
    
    Client->>ResourceAPI: GET /api/resource (Bearer token)
    ResourceAPI->>IdentityServer: POST /connect/introspect (validate token)
    IdentityServer-->>ResourceAPI: Token validation response
    ResourceAPI-->>Client: Protected resource data
```

## Use Cases

### Use Case 1: Service-to-Service Communication (Full Access)

**Scenario**: A backend service needs full access to API resources for data processing and synchronization.

**Client**: `service-api` (has `service` role, full permissions)

#### Postman Configuration

**Step 1: Get Access Token**

```http
POST https://localhost:7443/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id=service-api
&client_secret=supersecret
&scope=api1.read api1.write email profile roles
```

**Step 2: Access Protected Resource**

```http
GET https://localhost:5001/WeatherForecast
Authorization: Bearer {{access_token}}
```

#### cURL Examples

**Get Access Token:**

```bash
curl -X POST "https://localhost:7443/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=service-api&client_secret=supersecret&scope=api1.read api1.write email profile roles"
```

**Expected Response:**

```json
{
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "api1.read api1.write email profile roles"
}
```

**Access Protected Resource:**

```bash
curl -X GET "https://localhost:5001/WeatherForecast" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9..."
```

**Expected Response (Full Data):**

```json
[
  {
    "date": "2025-09-21",
    "temperatureC": 15,
    "temperatureF": 59,
    "summary": "Mild"
  },
  {
    "date": "2025-09-22",
    "temperatureC": 22,
    "temperatureF": 71,
    "summary": "Warm"
  }
]
```

### Use Case 2: Web Application (Limited Access)

**Scenario**: A web application needs read-only access to display data to users with limited sensitive information.

**Client**: `web-app` (has `web_user` role, read-only permissions)

#### Postman Configuration

**Step 1: Get Access Token**

```http
POST https://localhost:7443/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id=web-app
&client_secret=webapp-secret
&scope=api1.read email profile roles
```

**Step 2: Access Protected Resource**

```http
GET https://localhost:5001/WeatherForecast/detailed
Authorization: Bearer {{access_token}}
```

#### cURL Examples

**Get Access Token:**

```bash
curl -X POST "https://localhost:7443/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=web-app&client_secret=webapp-secret&scope=api1.read email profile roles"
```

**Access Detailed Resource:**

```bash
curl -X GET "https://localhost:5001/WeatherForecast/detailed" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9..."
```

**Expected Response (Filtered Data):**

```json
[
  {
    "date": "2025-09-21",
    "summary": "Mild",
    "location": "Sample City"
  },
  {
    "date": "2025-09-22", 
    "summary": "Warm",
    "location": "Sample City"
  }
]
```

*Note: Temperature, humidity, pressure, and internal ID are filtered out for web users.*

### Use Case 3: Mobile Application (Full Access)

**Scenario**: A mobile application needs full access to API resources for offline synchronization and comprehensive data display.

**Client**: `mobile-app` (has `mobile_user` role, full API access)

#### Postman Configuration

**Step 1: Get Access Token**

```http
POST https://localhost:7443/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id=mobile-app
&client_secret=mobile-secret
&scope=api1.read api1.write email profile roles
```

#### cURL Examples

**Get Access Token:**

```bash
curl -X POST "https://localhost:7443/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=mobile-app&client_secret=mobile-secret&scope=api1.read api1.write email profile roles"
```

**Access Detailed Resource:**

```bash
curl -X GET "https://localhost:5001/WeatherForecast/detailed" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9..."
```

**Expected Response (Limited Data - Mobile Users):**

```json
[
  {
    "date": "2025-09-21",
    "summary": "Mild",
    "location": "Sample City"
  }
]
```

*Note: Mobile users get basic data only (date, summary, location) - temperature and sensitive fields are filtered.*

### Use Case 4: Token with Specific Scopes

**Scenario**: Request a token with only specific scopes needed for a particular operation.

#### Postman Configuration

```http
POST https://localhost:7443/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id=service-api
&client_secret=supersecret
&scope=api1.read
```

#### cURL Example

```bash
curl -X POST "https://localhost:7443/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=service-api&client_secret=supersecret&scope=api1.read"
```

**Expected Response:**

```json
{
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9...",
  "token_type": "Bearer", 
  "expires_in": 3600,
  "scope": "api1.read"
}
```

### Use Case 5: Default Scopes (No Scope Parameter)

**Scenario**: Request a token without specifying scopes to get all available scopes for the client.

#### Postman Configuration

```http
POST https://localhost:7443/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id=service-api
&client_secret=supersecret
```

#### cURL Example

```bash
curl -X POST "https://localhost:7443/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=service-api&client_secret=supersecret"
```

**Expected Response:**

```json
{
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "email profile roles api1.read api1.write"
}
```

## Token Introspection

Token introspection allows resource servers to validate tokens and retrieve metadata about them.

### Use Case 6: Basic Token Introspection

**Scenario**: Validate a token and check its status.

#### Postman Configuration

```http
POST https://localhost:7443/connect/introspect
Content-Type: application/x-www-form-urlencoded
Authorization: Basic c2VydmljZS1hcGk6c3VwZXJzZWNyZXQ=

token={{access_token}}
```

*Note: Authorization header is Base64 encoded `service-api:supersecret`*

#### cURL Example

```bash
curl -X POST "https://localhost:7443/connect/introspect" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic c2VydmljZS1hcGk6c3VwZXJzZWNyZXQ=" \
  -d "token=eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9..."
```

**Expected Response (Active Token):**

```json
{
  "active": true,
  "exp": 1726844400,
  "iat": 1726840800,
  "token_type": "Bearer",
  "sub": "service-api",
  "scope_access": "authorized"
}
```

**Expected Response (Inactive Token):**

```json
{
  "active": false
}
```

### Use Case 7: Introspection with Different Client

**Scenario**: A less trusted client performs introspection and receives limited information.

#### cURL Example (Web App Client)

```bash
# First encode web-app:webapp-secret in Base64: d2ViLWFwcDp3ZWJhcHAtc2VjcmV0
curl -X POST "https://localhost:7443/connect/introspect" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic d2ViLWFwcDp3ZWJhcHAtc2VjcmV0" \
  -d "token=eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9..."
```

**Expected Response (Limited Information):**

```json
{
  "active": true,
  "exp": 1726844400,
  "iat": 1726840800
}
```

*Note: Less trusted clients receive minimal token information for security.*

## Refresh Tokens

### Current Implementation Status

**⚠️ Important Note**: The current Simple Identity Server implementation **only supports the Client Credentials grant type**, which **does not use refresh tokens**. Refresh tokens are primarily used with:

- Authorization Code flow (for web applications)
- Resource Owner Password Credentials flow 
- Implicit flow (deprecated)

However, the system is **configured and ready** to support refresh tokens with a 14-day lifetime when other OAuth2 flows are implemented.

### Configuration

The server is pre-configured for refresh tokens:

```json
{
  "Application": {
    "OpenIddict": {
      "AccessTokenLifetimeMinutes": 60,
      "RefreshTokenLifetimeDays": 14
    }
  }
}
```

### Why Client Credentials Doesn't Use Refresh Tokens

The Client Credentials flow is designed for **service-to-service** authentication where:

- The client can securely store credentials
- No user interaction is required
- The client can request new tokens anytime using its credentials
- Long-lived tokens aren't necessary since the client can always authenticate

### Refresh Token Flow (When Implemented)

If the server were extended to support Authorization Code or other flows, here's how refresh tokens would work:

#### Flow Diagram

```mermaid
sequenceDiagram
    participant Client
    participant IdentityServer
    participant ResourceAPI
    
    Note over Client,IdentityServer: Initial Authorization
    Client->>IdentityServer: POST /connect/token (authorization_code)
    IdentityServer-->>Client: access_token + refresh_token
    
    Note over Client,ResourceAPI: Using Access Token
    Client->>ResourceAPI: GET /api/resource (Bearer access_token)
    ResourceAPI-->>Client: Protected resource data
    
    Note over Client,IdentityServer: Token Refresh (when access_token expires)
    Client->>IdentityServer: POST /connect/token (refresh_token grant)
    IdentityServer-->>Client: new access_token + new refresh_token
    
    Note over Client,ResourceAPI: Continue with new token
    Client->>ResourceAPI: GET /api/resource (Bearer new_access_token)
    ResourceAPI-->>Client: Protected resource data
```

### Hypothetical Refresh Token Examples

*Note: These examples show how refresh tokens would work if Authorization Code flow were implemented.*

#### Use Case: Refreshing an Expired Access Token

**Step 1: Initial Token Request (Authorization Code)**

```http
POST https://localhost:7443/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code
&client_id=web-app
&client_secret=webapp-secret
&code=ABC123
&redirect_uri=https://myapp.com/callback
```

**Expected Response:**

```json
{
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9...",
  "refresh_token": "def456...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "api1.read email profile"
}
```

**Step 2: Using Refresh Token (When Access Token Expires)**

```http
POST https://localhost:7443/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token
&client_id=web-app
&client_secret=webapp-secret
&refresh_token=def456...
```

**Expected Response:**

```json
{
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9...",
  "refresh_token": "ghi789...",
  "token_type": "Bearer", 
  "expires_in": 3600,
  "scope": "api1.read email profile"
}
```

#### cURL Examples (Hypothetical)

**Refresh Token Request:**

```bash
curl -X POST "https://localhost:7443/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&client_id=web-app&client_secret=webapp-secret&refresh_token=def456..."
```

### Current Behavior: Refresh Token Grant Not Supported

If you attempt to use refresh token grant with the current implementation:

#### Request

```bash
curl -X POST "https://localhost:7443/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&client_id=service-api&client_secret=supersecret&refresh_token=any-token"
```

#### Response

```json
{
  "error": "unsupported_grant_type",
  "error_description": "The specified grant type is not supported."
}
```

### Refresh Token Security Features (When Implemented)

When refresh tokens are implemented, they would include these security features:

#### 1. Rotation
- Each refresh token use generates a new refresh token
- Old refresh tokens are invalidated
- Prevents token replay attacks

#### 2. Binding
- Refresh tokens are bound to specific clients
- Cannot be used by different client applications
- Includes client authentication

#### 3. Scope Limitations
- Refresh tokens can only request same or narrower scopes
- Cannot escalate permissions beyond original grant
- Scope validation on each refresh

#### 4. Revocation
Refresh tokens can be revoked through the introspection endpoint:

```bash
curl -X POST "https://localhost:7443/connect/introspect" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic c2VydmljZS1hcGk6c3VwZXJzZWNyZXQ=" \
  -d "token=refresh_token_here&token_type_hint=refresh_token"
```

### Alternative: Client Credentials Token Refresh

Since the current implementation uses Client Credentials, the equivalent of "refreshing" is simply requesting a new token:

#### Postman Configuration

```http
POST https://localhost:7443/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id=service-api
&client_secret=supersecret
&scope=api1.read api1.write
```

#### Benefits of Client Credentials Approach

1. **Simpler**: No need to manage refresh token storage
2. **More Secure**: Credentials are validated on every request
3. **Stateless**: No server-side token state to manage
4. **Immediate Revocation**: Disable client credentials to revoke all access

### Future Implementation Considerations

To add refresh token support, the server would need:

1. **Enable Additional Grant Types**:
   ```csharp
   options.AllowAuthorizationCodeFlow()
          .AllowRefreshTokenFlow();
   ```

2. **Add Authorization Endpoint**:
   ```csharp
   options.SetAuthorizationEndpointUris("/connect/authorize");
   ```

3. **Implement Authorization Controller**:
   - Handle authorization requests
   - User consent management
   - Authorization code generation

4. **Update Token Controller**:
   - Handle `refresh_token` grant type
   - Implement token rotation
   - Validate refresh token binding

### Best Practices for Refresh Tokens

When implementing refresh tokens in the future:

#### Security
- Always rotate refresh tokens
- Use short-lived access tokens (1-24 hours)
- Implement refresh token binding
- Monitor for suspicious refresh patterns

#### Storage
- Store refresh tokens securely
- Encrypt refresh tokens at rest
- Implement proper cleanup of expired tokens
- Use database indexes for performance

#### Client Implementation
- Handle refresh token expiration gracefully
- Implement automatic token refresh before expiration
- Store refresh tokens securely (encrypted storage)
- Clear tokens on application logout

### Summary

- **Current State**: Only Client Credentials flow supported (no refresh tokens needed)
- **Configuration**: Refresh token settings are ready (14-day lifetime)
- **Alternative**: Request new tokens using client credentials
- **Future**: System can be extended to support refresh tokens with Authorization Code flow

The current Client Credentials approach is actually **more secure and simpler** for service-to-service authentication, which is the primary use case for this identity server.

## Error Handling

### Invalid Client Credentials

#### Request

```bash
curl -X POST "https://localhost:7443/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=invalid-client&client_secret=wrong-secret"
```

#### Response

```json
{
  "error": "invalid_client",
  "error_description": "The client credentials are invalid."
}
```

### Invalid Scope

#### Request

```bash
curl -X POST "https://localhost:7443/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=service-api&client_secret=supersecret&scope=invalid-scope"
```

#### Response

```json
{
  "error": "invalid_scope",
  "error_description": "The requested scope is invalid, unknown, or malformed."
}
```

### Unauthorized Access to Resource

#### Request

```bash
curl -X GET "https://localhost:5001/WeatherForecast" \
  -H "Authorization: Bearer invalid-or-expired-token"
```

#### Response

```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer error="invalid_token"
```

### Rate Limiting

When rate limits are exceeded:

#### Response

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 60

{
  "error": "rate_limit_exceeded",
  "error_description": "Too many requests. Please try again later."
}
```

## Security Considerations

### 1. Field-Level Authorization

The system implements field-level authorization based on client roles:

- **Service Clients** (`service` role): Full access to all data fields
- **Web Clients** (`web_user` role): Basic data only (date, summary, location)
- **Mobile Clients** (`mobile_user` role): Basic data only
- **Admin Clients**: Full access including sensitive internal fields

### 2. Token Security Features

- **Encryption**: All tokens are encrypted using certificates
- **Signing**: Tokens are digitally signed for integrity
- **Lifetime**: Access tokens expire after 60 minutes
- **Introspection**: Supports token validation and revocation

### 3. Rate Limiting

- **Token Endpoint**: 20 requests per minute
- **Introspection Endpoint**: 50 requests per minute
- **Global**: 100 requests per minute

### 4. Security Monitoring

The system logs security events including:
- All token requests with client tracking
- Suspicious high-frequency requests
- Slow request monitoring
- Failed authentication attempts

## Server Discovery

### Get Server Information

#### Request

```bash
curl -X GET "https://localhost:7443/Home"
```

#### Response

```json
{
  "message": "Simple Identity Server",
  "version": "1.0.0",
  "description": "OAuth2/OpenID Connect Identity Server using OpenIddict",
  "endpoints": {
    "token": "/connect/token",
    "introspection": "/connect/introspect", 
    "configuration": "/.well-known/openid-configuration",
    "jwks": "/.well-known/jwks"
  },
  "supportedGrantTypes": ["client_credentials"],
  "documentation": "See /swagger for API documentation"
}
```

### OpenID Connect Discovery

#### Request

```bash
curl -X GET "https://localhost:7443/.well-known/openid-configuration"
```

#### Response

```json
{
  "issuer": "https://localhost:7443",
  "token_endpoint": "https://localhost:7443/connect/token",
  "introspection_endpoint": "https://localhost:7443/connect/introspect",
  "jwks_uri": "https://localhost:7443/.well-known/jwks",
  "grant_types_supported": ["client_credentials"],
  "token_endpoint_auth_methods_supported": ["client_secret_post", "client_secret_basic"],
  "introspection_endpoint_auth_methods_supported": ["client_secret_post", "client_secret_basic"]
}
```

## Postman Collection Setup

### Environment Variables

Create a Postman environment with these variables:

```json
{
  "identity_server_url": "https://localhost:7443",
  "resource_api_url": "https://localhost:5001",
  "service_client_id": "service-api",
  "service_client_secret": "supersecret",
  "web_client_id": "web-app", 
  "web_client_secret": "webapp-secret",
  "mobile_client_id": "mobile-app",
  "mobile_client_secret": "mobile-secret"
}
```

### Pre-request Script for Token

Add this pre-request script to automatically get a token:

```javascript
pm.sendRequest({
    url: pm.environment.get("identity_server_url") + "/connect/token",
    method: 'POST',
    header: {
        'Content-Type': 'application/x-www-form-urlencoded'
    },
    body: {
        mode: 'urlencoded',
        urlencoded: [
            {key: 'grant_type', value: 'client_credentials'},
            {key: 'client_id', value: pm.environment.get("service_client_id")},
            {key: 'client_secret', value: pm.environment.get("service_client_secret")},
            {key: 'scope', value: 'api1.read api1.write email profile roles'}
        ]
    }
}, function (err, res) {
    if (err) {
        console.log(err);
    } else {
        var responseJson = res.json();
        pm.environment.set("access_token", responseJson.access_token);
    }
});
```

## Troubleshooting

### Common Issues

1. **Certificate Errors**: Ensure SSL certificates are properly configured
2. **Connection Refused**: Verify server is running on correct ports
3. **Invalid Token**: Check token expiration and format
4. **Scope Errors**: Verify client has permission for requested scopes
5. **Rate Limiting**: Wait for rate limit window to reset

### Debug Information

Enable detailed logging by setting log level to `Debug` in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "SimpleIdentityServer.API.Middleware.SecurityMonitoringMiddleware": "Information"
    }
  }
}
```

This comprehensive guide covers all major use cases for the Simple Identity Server API, providing practical examples for both development and production scenarios.
