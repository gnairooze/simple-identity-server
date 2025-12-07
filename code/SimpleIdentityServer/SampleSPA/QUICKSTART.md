# Quick Start Guide

This guide will help you get the Sample SPA up and running in minutes.

## Prerequisites

1. **Simple Identity Server** must be running (default: `https://localhost:7001`)
2. A **registered OAuth client** in the Identity Server
3. A **web browser** (Chrome, Firefox, Edge, Safari)

## Step 1: Configure the Client

Open `js/config.js` and update the following values:

```javascript
export const config = {
    clientId: 'web-app',  // ← Your client ID from Identity Server
    authorizationEndpoint: 'https://localhost:7001/connect/authorize',
    tokenEndpoint: 'https://localhost:7001/connect/token',
    // ... rest of config
};
```

### Default Configuration

If you're using the default Simple Identity Server setup, the configuration is already set correctly. The default client `web-app` should already be registered.

## Step 2: Start the Server

Choose one of the following methods:

### Option A: Python (Recommended)

```bash
python serve.py
```

### Option B: Node.js

```bash
node serve.js
```

Or with npm:

```bash
npm start
```

### Option C: Python Built-in Server

```bash
# Python 3
python -m http.server 8080

# Python 2
python -m SimpleHTTPServer 8080
```

## Step 3: Open the Application

Open your browser and navigate to:

```
http://localhost:8080/index.html
```

## Step 4: Test the Flow

1. Click **"Login with OAuth 2.0"** button
2. You'll be redirected to the Identity Server login page
3. Enter your credentials (or register a new account)
4. After successful login, you'll be redirected back to the SPA
5. Your profile information and tokens will be displayed

## Troubleshooting

### "Invalid redirect URI" Error

**Problem**: The redirect URI doesn't match the one registered in Identity Server.

**Solution**:
1. Check your client configuration in Identity Server
2. Make sure it includes: `http://localhost:8080/callback.html`
3. Update `js/config.js` if using a different port

### CORS Errors

**Problem**: Browser blocks requests due to CORS policy.

**Solution**:
1. Make sure your Identity Server allows CORS from `http://localhost:8080`
2. The Identity Server should have this configured by default for development

### Can't Connect to Identity Server

**Problem**: `ERR_CONNECTION_REFUSED` or similar error.

**Solution**:
1. Make sure the Identity Server is running
2. Verify the URL in `js/config.js` matches your server
3. Check if you need to accept the SSL certificate (for localhost)

### "Client not found" Error

**Problem**: The client ID is not registered in Identity Server.

**Solution**:
1. Verify the `clientId` in `js/config.js`
2. Check that the client is registered in your Identity Server
3. If using the default setup, the `web-app` client should already exist

## Default Test Users

### Pre-configured Test Credentials

If you've run the `setup-test-user.sql` script, you can use these accounts immediately:

| User Type | Email | Password |
|-----------|-------|----------|
| **Standard User** | `testuser@example.com` | `Test@1234` |
| **Admin User** | `admin@example.com` | `Test@1234` |

### Setup Test Users

**Option A: Run the SQL Script**
```sql
-- In SQL Server Management Studio, run:
-- SampleSPA/setup-test-user.sql
```

**Option B: Use the Registration Page**
1. Click **"Register"** on the login page
2. Create a new account with:
   - Email: `test@example.com` (or any valid email format)
   - Password: Must meet complexity requirements (8+ chars, uppercase, lowercase, digit, special char)
3. The account is created immediately (email confirmation may be optional in dev mode)

### Password Requirements

The password `Test@1234` meets all requirements:
- ✅ At least 8 characters
- ✅ Contains uppercase letter (T)
- ✅ Contains lowercase letters (est)
- ✅ Contains digit (1234)
- ✅ Contains special character (@)

## Next Steps

Once you have the application running:

1. **Explore the code** - Check out the JavaScript files to understand the implementation
2. **Customize the UI** - Modify `css/styles.css` to match your brand
3. **Add API calls** - Use the access token to call protected APIs
4. **Implement logout** - Add proper logout functionality
5. **Add refresh tokens** - Implement token refresh for longer sessions

## Configuration Checklist

- [ ] Identity Server is running
- [ ] Client is registered in Identity Server
- [ ] Redirect URI matches: `http://localhost:8080/callback.html`
- [ ] Client ID in `js/config.js` is correct
- [ ] Endpoints in `js/config.js` point to your Identity Server
- [ ] CORS is enabled on Identity Server for `http://localhost:8080`
- [ ] Web server is running on port 8080

## Common Ports

If port 8080 is already in use, you can use a different port:

```bash
# Python
python -m http.server 3000

# Node.js (modify serve.js to change PORT constant)
# Then update js/config.js redirect URIs accordingly
```

Remember to update:
1. The redirect URI in `js/config.js`
2. The redirect URI in your Identity Server client configuration

## Production Deployment

For production use, see the main `README.md` file for:
- Security considerations
- HTTPS configuration
- Environment-specific settings
- Deployment best practices

## Getting Help

- Check the browser console for error messages
- Review the `README.md` for detailed documentation
- Verify your Identity Server is properly configured
- Test the Identity Server endpoints directly using Postman or curl

## Example Working Configuration

Here's a complete working example:

**Identity Server**: `https://localhost:7001`

**Client Registration**:
```json
{
  "ClientId": "web-app",
  "Type": "public",
  "RedirectUris": ["http://localhost:8080/callback.html"],
  "RequirePkce": true,
  "AllowedScopes": ["openid", "profile", "email", "roles"]
}
```

**SPA Configuration** (`js/config.js`):
```javascript
{
  clientId: 'web-app',
  authorizationEndpoint: 'https://localhost:7001/connect/authorize',
  tokenEndpoint: 'https://localhost:7001/connect/token',
  redirectUri: 'http://localhost:8080/callback.html',
  scope: 'openid profile email roles'
}
```

---

**Ready to go?** Run `python serve.py` or `node serve.js` and open `http://localhost:8080/index.html`!

