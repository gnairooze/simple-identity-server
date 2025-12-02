# Authorization Code Flow - Quick Start Guide

## Setup (5 minutes)

### 1. Install Dependencies

Dependencies are already added to the project file. Just restore:

```bash
cd code/SimpleIdentityServer/SimpleIdentityServer.API
dotnet restore
```

### 2. Run Database Migration

Execute the SQL script in your database:

```bash
# Use SQL Server Management Studio or sqlcmd
sqlcmd -S localhost -d IdentityServer -i Scripts/AddIdentityTables.sql
```

Or use Entity Framework:

```bash
dotnet ef migrations add AddIdentitySupport
dotnet ef database update
```

### 3. Update Configuration

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=IdentityServer;Trusted_Connection=True;"
  }
}
```

### 4. Run the Application

```bash
dotnet run
```

## Test the Implementation

### Test 1: User Registration

1. Open browser to: `https://localhost:5001/account/register`
2. Fill in:
   - Email: `test@example.com`
   - Password: `Test@1234`
   - Confirm Password: `Test@1234`
   - Check "I agree to terms"
3. Click "Create Account"
4. Check console logs for confirmation email link

### Test 2: User Login

1. Open: `https://localhost:5001/account/login`
2. Enter credentials
3. Should redirect to home or return URL

### Test 3: OAuth Authorization Flow

#### Using Browser

```
https://localhost:5001/connect/authorize?
  response_type=code&
  client_id=web-app&
  redirect_uri=https://localhost:5001/signin-oidc&
  scope=openid%20profile%20email&
  state=test123&
  code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&
  code_challenge_method=S256
```

#### Using JavaScript

```javascript
import { PKCEHelper } from '/js/modules/auth/pkce-helper.js';

const config = {
    authorizationEndpoint: 'https://localhost:5001/connect/authorize',
    tokenEndpoint: 'https://localhost:5001/connect/token',
    clientId: 'web-app',
    redirectUri: 'https://localhost:5001/signin-oidc',
    scope: 'openid profile email'
};

// Generate and navigate to auth URL
const authUrl = await PKCEHelper.generateAuthorizationUrl(config);
window.location.href = authUrl;
```

## Common Endpoints

### Account Management

- `GET /account/login` - Login page
- `POST /account/login` - Submit login
- `GET /account/register` - Registration page
- `POST /account/register` - Submit registration
- `GET /account/forgot-password` - Forgot password page
- `POST /account/forgot-password` - Request password reset
- `GET /account/reset-password?code=XXX` - Reset password page
- `POST /account/reset-password` - Submit new password
- `POST /account/logout` - Logout

### OAuth 2.0 / OpenID Connect

- `GET /connect/authorize` - Authorization endpoint
- `POST /connect/token` - Token endpoint
- `POST /connect/introspect` - Introspection endpoint
- `GET /connect/userinfo` - User info endpoint
- `POST /connect/logout` - Logout endpoint
- `GET /.well-known/openid-configuration` - Discovery document

## Client Application Integration

### Step 1: Install PKCE Helper

Copy `wwwroot/js/modules/auth/pkce-helper.js` to your client app.

### Step 2: Start Authorization

```javascript
import { PKCEHelper } from './pkce-helper.js';

const config = {
    authorizationEndpoint: 'https://identity.example.com/connect/authorize',
    tokenEndpoint: 'https://identity.example.com/connect/token',
    clientId: 'your-client-id',
    redirectUri: 'https://your-app.com/callback',
    scope: 'openid profile email'
};

// Generate authorization URL with PKCE
const authUrl = await PKCEHelper.generateAuthorizationUrl(config);

// Redirect user
window.location.href = authUrl;
```

### Step 3: Handle Callback

```javascript
// Parse URL parameters
const urlParams = new URLSearchParams(window.location.search);
const code = urlParams.get('code');
const state = urlParams.get('state');

if (code) {
    try {
        // Exchange code for tokens
        const tokens = await PKCEHelper.exchangeCodeForTokens(config, code, state);
        
        // Store tokens
        localStorage.setItem('access_token', tokens.access_token);
        localStorage.setItem('id_token', tokens.id_token);
        
        // Parse user info from ID token
        const userInfo = PKCEHelper.parseJWT(tokens.id_token);
        console.log('User:', userInfo);
        
        // Redirect to main app
        window.location.href = '/dashboard';
    } catch (error) {
        console.error('Login failed:', error);
    }
}
```

### Step 4: Use Access Token

```javascript
// Make authenticated API call
const accessToken = localStorage.getItem('access_token');

fetch('https://api.example.com/data', {
    headers: {
        'Authorization': `Bearer ${accessToken}`
    }
})
.then(response => response.json())
.then(data => console.log(data));
```

## Configuration Options

### Rate Limiting

```json
{
  "RateLimiting": {
    "AuthenticationEndpoints": {
      "PermitLimit": 5,        // Max attempts
      "WindowMinutes": 5       // Time window
    }
  }
}
```

### Token Lifetimes

```json
{
  "Application": {
    "OpenIddict": {
      "AccessTokenLifetimeMinutes": 60,
      "RefreshTokenLifetimeDays": 14,
      "AuthorizationCodeLifetimeMinutes": 10
    }
  }
}
```

### Password Policy

Configured in `ServiceConfiguration.cs`:

```csharp
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequireUppercase = true;
options.Password.RequiredLength = 8;
```

## Troubleshooting

### Issue: "Invalid redirect_uri"

**Solution**: Register your redirect URI in the database:

```sql
UPDATE OpenIddictApplications
SET RedirectUris = '["https://your-app.com/callback"]'
WHERE ClientId = 'your-client-id'
```

### Issue: "PKCE validation failed"

**Solution**: Ensure you're using the same code_verifier that generated the code_challenge.

### Issue: "Rate limit exceeded"

**Solution**: 
- Wait 5 minutes
- Clear sessionStorage: `sessionStorage.clear()`
- Adjust rate limits in configuration

### Issue: "Email confirmation required"

**Solution**: Check application logs for confirmation link or implement production email service.

### Issue: "Authorization code expired"

**Solution**: Complete token exchange within 10 minutes or adjust `AuthorizationCodeLifetimeMinutes`.

## Development Tips

### 1. Disable Email Confirmation (Development Only)

In `AccountController.cs`:

```csharp
var user = new ApplicationUser 
{ 
    UserName = model.Email, 
    Email = model.Email,
    EmailConfirmed = true  // Set to true for development
};
```

### 2. View Email Links in Logs

Check console output for email confirmation and password reset links during development.

### 3. Test with Postman

Import this request:

```
POST https://localhost:5001/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code
&client_id=web-app
&code=YOUR_AUTHORIZATION_CODE
&redirect_uri=https://localhost:5001/signin-oidc
&code_verifier=YOUR_CODE_VERIFIER
```

### 4. Bypass Rate Limiting (Testing Only)

Temporarily increase limits in `appsettings.json`:

```json
{
  "RateLimiting": {
    "AuthenticationEndpoints": {
      "PermitLimit": 100,
      "WindowMinutes": 1
    }
  }
}
```

## Production Checklist

Before deploying to production:

- [ ] Implement production email service
- [ ] Configure HTTPS with valid certificates
- [ ] Update redirect URIs to production URLs
- [ ] Review and adjust rate limits
- [ ] Enable email confirmation requirement
- [ ] Set strong client secrets (for confidential clients)
- [ ] Configure backup and monitoring
- [ ] Test all flows end-to-end
- [ ] Review security headers
- [ ] Update CORS policies

## Sample Client Configuration

Register a new client in the database:

```sql
INSERT INTO [OpenIddictApplications] 
([Id], [ClientId], [DisplayName], [Type], [ConsentType], [Permissions], [RedirectUris], [Requirements])
VALUES (
    NEWID(),
    'my-spa-app',
    'My SPA Application',
    'public',
    'implicit',
    '["ept:authorization", "ept:token", "gt:authorization_code", "rst:code", "scp:openid", "scp:profile", "scp:email"]',
    '["https://my-app.com/callback"]',
    '["pkce"]'
);
```

## Resources

- Full Documentation: `docs/AUTHORIZATION_CODE_IMPLEMENTATION.md`
- Implementation Plan: `docs/auth-code-plan.md`
- OAuth 2.0 Spec: https://tools.ietf.org/html/rfc6749
- PKCE Spec: https://tools.ietf.org/html/rfc7636
- OpenIddict Docs: https://documentation.openiddict.com/

## Support

For issues:
1. Check application logs
2. Review configuration settings
3. Verify database migration completed
4. Test with sample client first

## Quick Commands

```bash
# Build
dotnet build

# Run
dotnet run

# Test
dotnet test

# Create migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Drop database (caution!)
dotnet ef database drop
```

That's it! You now have a fully functional OAuth 2.0 Authorization Code flow with PKCE support. 🎉

