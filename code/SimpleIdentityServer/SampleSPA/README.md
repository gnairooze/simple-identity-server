# Sample SPA - OAuth 2.0 Authorization Code Flow with PKCE

This is a sample Single Page Application (SPA) that demonstrates the OAuth 2.0 Authorization Code flow with PKCE (Proof Key for Code Exchange) using vanilla JavaScript, HTML, and CSS.

## Features

- ✅ OAuth 2.0 Authorization Code flow with PKCE
- ✅ Modern, responsive UI built with vanilla CSS
- ✅ Secure token handling
- ✅ User information display
- ✅ Token introspection and claims visualization
- ✅ No external dependencies
- ✅ Ready to use with Simple Identity Server

## Prerequisites

- Simple Identity Server running and configured
- A registered OAuth client in the Identity Server
- A web server to serve static files (or use the provided script)

## Quick Start

### 1. Configure the Application

Edit `js/config.js` and update the configuration to match your Identity Server:

```javascript
export const config = {
    clientId: 'web-app',  // Your client ID
    authorizationEndpoint: 'https://localhost:7001/connect/authorize',
    tokenEndpoint: 'https://localhost:7001/connect/token',
    userinfoEndpoint: 'https://localhost:7001/connect/userinfo',
    redirectUri: window.location.origin + '/callback.html',
    scope: 'openid profile email roles'
};
```

### 2. Register the Client in Identity Server

Make sure your OAuth client is registered in the Identity Server with the following configuration:

- **Client ID**: `web-app` (or your chosen ID)
- **Client Type**: `public` (for SPAs)
- **Redirect URI**: `http://localhost:8080/callback.html` (or your URL)
- **Allowed Scopes**: `openid`, `profile`, `email`, `roles`
- **Require PKCE**: `true`
- **Allowed Grant Types**: `authorization_code`

### 3. Serve the Application

You can use any static file server. Here are some options:

#### Option A: Using Python
```bash
# Python 3
python -m http.server 8080

# Python 2
python -m SimpleHTTPServer 8080
```

#### Option B: Using Node.js
```bash
# Install http-server globally
npm install -g http-server

# Run server
http-server -p 8080
```

#### Option C: Using PHP
```bash
php -S localhost:8080
```

#### Option D: Using .NET
```bash
dotnet tool install -g dotnet-serve
dotnet serve -p 8080
```

### 4. Access the Application

Open your browser and navigate to:
```
http://localhost:8080/index.html
```

## Project Structure

```
SampleSPA/
├── index.html              # Main application page
├── callback.html           # OAuth callback handler page
├── README.md              # This file
├── css/
│   └── styles.css         # Application styles
└── js/
    ├── config.js          # Configuration file
    ├── pkce-helper.js     # PKCE implementation
    ├── app.js             # Main application logic
    └── callback.js        # Callback handler logic
```

## How It Works

### 1. Login Flow

1. User clicks "Login with OAuth 2.0" button
2. Application generates PKCE code verifier and challenge
3. User is redirected to Identity Server's authorization endpoint
4. User logs in (if not already authenticated)
5. Identity Server redirects back to `callback.html` with authorization code
6. Application exchanges code for tokens using PKCE code verifier
7. Tokens are stored in sessionStorage
8. User is redirected to main page showing their profile

### 2. Token Management

- **Access Token**: Used to access protected APIs
- **ID Token**: Contains user claims and information
- **Storage**: Tokens are stored in sessionStorage (cleared when browser closes)
- **Expiration**: Application checks token expiration and logs out if expired

### 3. Security Features

- **PKCE**: Protects against authorization code interception
- **State Parameter**: Prevents CSRF attacks
- **Secure Storage**: Uses sessionStorage (not localStorage for better security)
- **Token Validation**: Validates JWT tokens before use
- **HTTPS Ready**: Designed to work over HTTPS in production

## Configuration Options

### js/config.js

```javascript
{
    // OAuth 2.0 Client ID
    clientId: 'web-app',

    // Optional: Client secret (leave empty for public clients)
    clientSecret: '',

    // Identity Server endpoints
    authorizationEndpoint: 'https://localhost:7001/connect/authorize',
    tokenEndpoint: 'https://localhost:7001/connect/token',
    userinfoEndpoint: 'https://localhost:7001/connect/userinfo',
    logoutEndpoint: 'https://localhost:7001/connect/logout',

    // Redirect URIs
    redirectUri: window.location.origin + '/callback.html',
    postLogoutRedirectUri: window.location.origin + '/index.html',

    // Scopes
    scope: 'openid profile email roles',

    // Response type
    responseType: 'code'
}
```

## API Reference

### PKCEHelper Class

#### `generateCodeVerifier()`
Generates a cryptographically secure code verifier.

#### `generateCodeChallenge(verifier)`
Generates SHA-256 code challenge from verifier.

#### `generateAuthorizationUrl(config)`
Creates the authorization URL with PKCE parameters.

#### `exchangeCodeForTokens(config, code, state)`
Exchanges authorization code for access and ID tokens.

#### `parseJWT(token)`
Parses and validates a JWT token.

#### `getUserInfo(config, accessToken)`
Fetches user information from the userinfo endpoint.

## Troubleshooting

### "Invalid redirect URI" Error

Make sure the redirect URI in your configuration exactly matches the one registered in the Identity Server.

### "PKCE validation failed" Error

This usually means:
- Code verifier was not found in sessionStorage
- Browser cleared sessionStorage between authorization and callback
- Multiple tabs/windows caused storage conflicts

**Solution**: Use a single browser tab and don't refresh during the flow.

### "Token has expired" Error

The ID token has expired. Click "Logout" and log in again.

### CORS Errors

If you see CORS errors:
1. Make sure your Identity Server allows CORS from your SPA origin
2. Check that the redirect URI is properly configured
3. Ensure you're using the correct protocol (http/https)

### Network Errors

If token exchange fails:
1. Check that the Identity Server is running
2. Verify the token endpoint URL in config.js
3. Check browser console for detailed error messages
4. Ensure your client is properly registered

## Production Deployment

### Security Checklist

- [ ] Use HTTPS everywhere (both SPA and Identity Server)
- [ ] Configure proper CORS policies
- [ ] Use secure, httpOnly cookies for sensitive data
- [ ] Implement token refresh mechanism
- [ ] Add proper error handling and logging
- [ ] Configure Content Security Policy headers
- [ ] Use environment-specific configuration
- [ ] Implement proper session timeout
- [ ] Add rate limiting on authorization endpoints
- [ ] Regular security audits

### Deployment Steps

1. **Build for Production**
   - Minify JavaScript and CSS
   - Optimize images and assets
   - Remove console.log statements

2. **Configure Environment**
   - Update config.js with production URLs
   - Use environment variables for sensitive data
   - Configure HTTPS certificates

3. **Deploy to Hosting**
   - Use a CDN for static assets
   - Configure proper cache headers
   - Set up HTTPS/TLS

4. **Update Identity Server**
   - Register production redirect URIs
   - Configure CORS for production domain
   - Update client configuration

## Advanced Features

### Adding Refresh Tokens

To implement refresh tokens, you would need to:

1. Request the `offline_access` scope
2. Store the refresh token securely
3. Implement token refresh logic before access token expires
4. Handle refresh token rotation

### Calling Protected APIs

```javascript
// Example: Call a protected API
async function callProtectedAPI() {
    const tokens = JSON.parse(sessionStorage.getItem('oauth_tokens'));

    const response = await fetch('https://api.example.com/data', {
        headers: {
            'Authorization': `Bearer ${tokens.access_token}`,
            'Accept': 'application/json'
        }
    });

    if (response.ok) {
        return await response.json();
    } else {
        throw new Error('API call failed');
    }
}
```

### Implementing Logout

```javascript
// Example: Implement proper logout
async function logout() {
    const tokens = JSON.parse(sessionStorage.getItem('oauth_tokens'));

    // Call logout endpoint
    const logoutUrl = `${config.logoutEndpoint}?` +
        `id_token_hint=${tokens.id_token}&` +
        `post_logout_redirect_uri=${config.postLogoutRedirectUri}`;

    // Clear local storage
    sessionStorage.clear();

    // Redirect to logout
    window.location.href = logoutUrl;
}
```

## Resources

- [OAuth 2.0 Authorization Code Flow](https://tools.ietf.org/html/rfc6749#section-4.1)
- [PKCE (RFC 7636)](https://tools.ietf.org/html/rfc7636)
- [OpenID Connect Core Specification](https://openid.net/specs/openid-connect-core-1_0.html)
- [OAuth 2.0 for Browser-Based Apps](https://tools.ietf.org/html/draft-ietf-oauth-browser-based-apps)

## License

This sample application is provided as-is for educational and demonstration purposes.

## Support

For issues and questions:
- Check the Simple Identity Server documentation
- Review the browser console for error messages
- Verify your configuration matches the Identity Server setup

