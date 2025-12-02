# Authorization Code Flow Implementation

This document describes the implementation of the OAuth 2.0 Authorization Code flow with PKCE support in the Simple Identity Server.

## Overview

The Authorization Code flow has been successfully implemented with the following features:

- ✅ OAuth 2.0 Authorization Code flow with PKCE
- ✅ ASP.NET Core Identity integration for user management
- ✅ Secure authentication pages (Login, Register, Password Reset)
- ✅ Rate limiting for authentication endpoints
- ✅ Client-side JavaScript with security features
- ✅ PKCE helper for client applications
- ✅ Comprehensive security headers and middleware

## Architecture

### New Components

1. **Identity System**
   - `ApplicationUser` - Extended Identity user entity
   - `ApplicationDbContext` - Updated to support IdentityDbContext
   - Identity services configured with password policies

2. **Controllers**
   - `AuthorizationController` - OAuth 2.0 authorization endpoints
   - `AccountController` - User authentication and management

3. **Views**
   - Login, Register, Forgot Password, Reset Password
   - Confirmation pages
   - Auth layout with modern styling

4. **Client-Side Modules**
   - `auth-module.js` - Core authentication with rate limiting
   - `login.js` - Login page functionality
   - `register.js` - Registration with password strength checker
   - `pkce-helper.js` - PKCE implementation for clients

5. **Services**
   - `IEmailService` - Email service interface
   - `EmailService` - Basic email service implementation

## Database Schema

### Identity Tables

The following ASP.NET Core Identity tables are created automatically:

- `Users` (AspNetUsers) - User accounts with custom fields
- `AspNetRoles` - User roles
- `AspNetUserRoles` - User-role relationships
- `AspNetUserClaims` - User claims
- `AspNetUserLogins` - External login providers
- `AspNetUserTokens` - User tokens

### Custom Fields on Users Table

- `FirstName` (nvarchar(100))
- `LastName` (nvarchar(100))
- `CreatedAt` (datetime2)
- `LastLoginAt` (datetime2)
- `IsActive` (bit)
- `ProfilePictureUrl` (nvarchar(500))
- `EmailConfirmedAt` (datetime2)
- `FailedLoginAttempts` (int)
- `LastFailedLoginAt` (datetime2)

### OpenIddict Scopes

The following scopes are created for Authorization Code flow:

- `openid` - OpenID Connect
- `profile` - User profile information
- `email` - Email address
- `roles` - User roles

## Configuration

### appsettings.json Updates

```json
{
  "Application": {
    "OpenIddict": {
      "AuthorizationEndpointUri": "/connect/authorize",
      "UserinfoEndpointUri": "/connect/userinfo",
      "LogoutEndpointUri": "/connect/logout",
      "AuthorizationCodeLifetimeMinutes": 10
    }
  },
  "RateLimiting": {
    "AuthenticationEndpoints": {
      "PermitLimit": 5,
      "WindowMinutes": 5
    }
  }
}
```

## Endpoints

### OAuth 2.0 Endpoints

- `GET/POST /connect/authorize` - Authorization endpoint
- `POST /connect/token` - Token endpoint (existing)
- `POST /connect/introspect` - Introspection endpoint (existing)
- `GET /connect/userinfo` - User information endpoint
- `POST /connect/logout` - Logout endpoint

### Account Management Endpoints

- `GET/POST /account/login` - User login
- `GET/POST /account/register` - User registration
- `GET/POST /account/forgot-password` - Password reset request
- `GET/POST /account/reset-password` - Password reset
- `POST /account/logout` - Account logout
- `GET /account/confirm-email` - Email confirmation

## Security Features

### PKCE (Proof Key for Code Exchange)

- Required for all authorization code flows
- SHA-256 code challenge method
- Implemented in `pkce-helper.js` for client applications

### Rate Limiting

- Global: 100 requests per minute
- Token endpoint: 20 requests per minute
- Introspection: 50 requests per minute
- **Authentication endpoints: 5 requests per 5 minutes**

### Password Policy

- Minimum 8 characters
- Requires uppercase letter
- Requires lowercase letter
- Requires digit
- Requires special character
- Minimum 1 unique character

### Account Lockout

- 5 failed attempts trigger lockout
- 5-minute lockout duration
- Applies to all new users

### Security Headers

All pages include comprehensive security headers:
- X-Frame-Options
- X-Content-Type-Options
- X-XSS-Protection
- Content-Security-Policy
- Strict-Transport-Security
- Referrer-Policy

### CSRF Protection

- Antiforgery tokens on all POST forms
- Cookie with Strict SameSite policy
- HTTPS-only cookies

## Usage Examples

### For Client Applications (SPA/Mobile)

#### 1. Initiate Authorization Flow with PKCE

```javascript
import { PKCEHelper } from './pkce-helper.js';

const config = {
    authorizationEndpoint: 'https://identity.example.com/connect/authorize',
    tokenEndpoint: 'https://identity.example.com/connect/token',
    clientId: 'web-app',
    redirectUri: 'https://app.example.com/callback',
    scope: 'openid profile email'
};

// Generate authorization URL
const authUrl = await PKCEHelper.generateAuthorizationUrl(config);

// Redirect user to authorization URL
window.location.href = authUrl;
```

#### 2. Handle Callback and Exchange Code for Tokens

```javascript
// Parse callback URL
const urlParams = new URLSearchParams(window.location.search);
const code = urlParams.get('code');
const state = urlParams.get('state');

// Exchange code for tokens
try {
    const tokens = await PKCEHelper.exchangeCodeForTokens(config, code, state);
    console.log('Access Token:', tokens.access_token);
    console.log('ID Token:', tokens.id_token);
    
    // Parse ID token
    const userInfo = PKCEHelper.parseJWT(tokens.id_token);
    console.log('User Info:', userInfo);
} catch (error) {
    console.error('Token exchange failed:', error);
}
```

### For Server-Side Applications

#### 1. Authorization Request

```
GET /connect/authorize?
    response_type=code&
    client_id=server-app&
    redirect_uri=https://app.example.com/callback&
    scope=openid profile email&
    state=random_state_value&
    code_challenge=BASE64URL(SHA256(code_verifier))&
    code_challenge_method=S256
```

#### 2. Token Exchange

```
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&
client_id=server-app&
client_secret=secret&
code=authorization_code&
redirect_uri=https://app.example.com/callback&
code_verifier=original_code_verifier
```

## Migration Steps

### 1. Update Database

Run the migration script to add Identity tables:

```bash
# Using SQL Server Management Studio
# Execute: SimpleIdentityServer.API/Scripts/AddIdentityTables.sql
```

Or use Entity Framework migrations:

```bash
cd SimpleIdentityServer.API
dotnet ef migrations add AddIdentitySupport
dotnet ef database update
```

### 2. Update Configuration

Ensure your `appsettings.json` includes the new configuration values shown above.

### 3. Restart Application

Restart the application to apply the changes:

```bash
dotnet run
```

## Testing

### Manual Testing

1. **Register a new user**:
   - Navigate to `/account/register`
   - Fill in email and password
   - Check logs for email confirmation link

2. **Login**:
   - Navigate to `/account/login`
   - Enter credentials
   - Should redirect to return URL or home

3. **Authorization Flow**:
   - Start authorization request to `/connect/authorize`
   - Should redirect to login if not authenticated
   - After login, should return authorization code

### Integration Testing

See `SimpleIdentityServer.API.Test/Integration/AuthorizationCodeFlowTests.cs` for integration tests.

## Client Configuration

### Sample Client Registration

A sample web application client is created during database initialization:

```json
{
  "ClientId": "web-app",
  "DisplayName": "Web Application",
  "Type": "public",
  "ConsentType": "implicit",
  "RedirectUris": ["https://localhost:5001/signin-oidc"],
  "PostLogoutRedirectUris": ["https://localhost:5001/signout-callback-oidc"],
  "Requirements": ["pkce"],
  "Permissions": [
    "ept:authorization",
    "ept:logout",
    "ept:token",
    "gt:authorization_code",
    "rst:code",
    "scp:openid",
    "scp:profile",
    "scp:email"
  ]
}
```

## Troubleshooting

### Common Issues

1. **"CSRF token not found" error**
   - Ensure `@Html.AntiForgeryToken()` is in forms
   - Check that antiforgery middleware is configured

2. **Rate limit exceeded**
   - Check `RateLimiting:AuthenticationEndpoints` configuration
   - Clear `sessionStorage` if testing repeatedly

3. **Authorization code expired**
   - Default lifetime is 10 minutes
   - Adjust `AuthorizationCodeLifetimeMinutes` if needed

4. **PKCE validation failed**
   - Ensure code_verifier is stored in sessionStorage
   - Verify code_challenge_method is S256

## Security Considerations

### Production Deployment

1. **Configure HTTPS**:
   - Enforce HTTPS in production
   - Configure HSTS headers

2. **Email Service**:
   - Replace `EmailService` with production email provider
   - Implement email templates

3. **Client Secrets**:
   - Store client secrets securely
   - Use Key Vault or environment variables

4. **Certificate Management**:
   - Use proper certificates for signing/encryption
   - Rotate certificates regularly

5. **Rate Limiting**:
   - Adjust limits based on traffic patterns
   - Monitor for abuse

6. **Session Management**:
   - Configure cookie options appropriately
   - Implement session timeout

## Next Steps

Potential enhancements:

1. **Two-Factor Authentication (2FA)**
   - Implement authenticator app support
   - SMS verification

2. **External Login Providers**
   - Google, Microsoft, GitHub OAuth
   - Social login integration

3. **Consent Screen**
   - Implement proper consent UI
   - Allow users to manage consents

4. **User Profile Management**
   - Profile edit page
   - Avatar upload

5. **Admin Panel**
   - User management interface
   - Client application management

6. **Refresh Token Support**
   - Implement refresh token rotation
   - Long-lived sessions

## References

- [OAuth 2.0 Authorization Code Flow](https://tools.ietf.org/html/rfc6749#section-4.1)
- [PKCE (RFC 7636)](https://tools.ietf.org/html/rfc7636)
- [OpenIddict Documentation](https://documentation.openiddict.com/)
- [ASP.NET Core Identity](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)

