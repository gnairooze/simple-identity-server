# Fixing the OAuth Client Database Issue

## The Problem

You're encountering this error:
```
System.Text.Json.JsonReaderException: 'h' is an invalid start of a value
```

This occurs because OpenIddict expects redirect URIs and post-logout redirect URIs to be stored as **JSON arrays** in the database, but they may be stored as plain strings instead.

For example:
- ❌ **Wrong**: `http://localhost:5001/signin-oidc` (plain string)
- ✅ **Correct**: `["http://localhost:5001/signin-oidc"]` (JSON array)

## Solution

### Option 1: Run the SQL Fix Script (Recommended)

1. **Open SQL Server Management Studio** or your preferred SQL client

2. **Connect to your database** (default: `SimpleIdentityServer`)

3. **Run the setup script**:
   ```
   SampleSPA/setup-client.sql
   ```

   This script will:
   - Fix any malformed redirect URIs in the database
   - Delete and recreate the `web-app` client with correct settings
   - Configure redirect URIs for your SampleSPA (`http://localhost:8080/callback.html`)

4. **Restart your Identity Server**

5. **Try logging in again** from the SampleSPA

### Option 2: Manual Database Fix

If you prefer to manually fix the issue:

```sql
USE [SimpleIdentityServer]
GO

-- Fix malformed RedirectUris
UPDATE [OpenIddictApplications]
SET [RedirectUris] = '["' + [RedirectUris] + '"]'
WHERE [RedirectUris] IS NOT NULL 
  AND [RedirectUris] NOT LIKE '[%';

-- Fix malformed PostLogoutRedirectUris
UPDATE [OpenIddictApplications]
SET [PostLogoutRedirectUris] = '["' + [PostLogoutRedirectUris] + '"]'
WHERE [PostLogoutRedirectUris] IS NOT NULL 
  AND [PostLogoutRedirectUris] NOT LIKE '[%';

-- Update web-app client redirect URI
UPDATE [OpenIddictApplications]
SET [RedirectUris] = '["http://localhost:8080/callback.html", "https://localhost:8080/callback.html"]',
    [PostLogoutRedirectUris] = '["http://localhost:8080/index.html", "https://localhost:8080/index.html"]'
WHERE [ClientId] = 'web-app';
GO
```

### Option 3: Check Current Data

To see what's currently in your database:

```sql
SELECT 
    [ClientId],
    [DisplayName],
    [RedirectUris],
    [PostLogoutRedirectUris],
    [Permissions],
    [Requirements],
    [Type]
FROM [OpenIddictApplications]
WHERE [ClientId] = 'web-app';
```

Look at the `RedirectUris` column:
- If it starts with `http` (not `[`), it's malformed
- It should start with `[` and end with `]`

## After Fixing

1. **Clear OpenIddict cache** by restarting the Identity Server
2. **Clear browser cache** or use incognito mode
3. **Try the login flow again**

## Verification

After running the fix, verify the client is correctly configured:

```sql
-- This should return data with properly formatted JSON arrays
SELECT 
    [ClientId],
    [RedirectUris],
    [PostLogoutRedirectUris]
FROM [OpenIddictApplications]
WHERE [ClientId] = 'web-app';
```

Expected output:
- **RedirectUris**: `["http://localhost:8080/callback.html", "https://localhost:8080/callback.html"]`
- **PostLogoutRedirectUris**: `["http://localhost:8080/index.html", "https://localhost:8080/index.html"]`

## Why This Happens

This issue can occur when:
1. Clients are registered using direct SQL INSERT with incorrect formatting
2. Migration scripts have syntax errors
3. Manual database updates don't follow OpenIddict's JSON requirements

## Prevention

Always use one of these methods to register clients:

### Method 1: Using OpenIddict Application Manager (Recommended)

```csharp
await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
{
    ClientId = "web-app",
    DisplayName = "Sample SPA",
    Type = "public",
    ConsentType = "implicit",
    RedirectUris = 
    {
        new Uri("http://localhost:8080/callback.html"),
        new Uri("https://localhost:8080/callback.html")
    },
    PostLogoutRedirectUris =
    {
        new Uri("http://localhost:8080/index.html")
    },
    Permissions =
    {
        OpenIddictConstants.Permissions.Endpoints.Authorization,
        OpenIddictConstants.Permissions.Endpoints.Token,
        OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
        OpenIddictConstants.Permissions.ResponseTypes.Code,
        OpenIddictConstants.Permissions.Scopes.OpenId,
        OpenIddictConstants.Permissions.Scopes.Profile,
        OpenIddictConstants.Permissions.Scopes.Email
    },
    Requirements =
    {
        OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
    }
});
```

### Method 2: Using SQL with Proper JSON Format

```sql
INSERT INTO [OpenIddictApplications] (...)
VALUES (
    ...,
    '["http://localhost:8080/callback.html"]', -- Proper JSON array
    ...
);
```

## Still Having Issues?

If the problem persists:

1. **Check Identity Server logs** for detailed error messages
2. **Verify the client ID** in `js/config.js` matches the database
3. **Check CORS settings** - the Identity Server must allow requests from `http://localhost:8080`
4. **Restart the Identity Server** to clear all caches
5. **Try with a different browser** or incognito mode

## Need More Help?

Check these files for additional information:
- `README.md` - Full documentation
- `QUICKSTART.md` - Quick setup guide
- `js/config.js` - Configuration settings

