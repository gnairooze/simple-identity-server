-- SQL Script to Setup OAuth Client for SampleSPA
-- Run this script against your SimpleIdentityServer database

USE [SimpleIdentityServer]
GO

-- =============================================
-- STEP 1: Fix malformed redirect URIs
-- =============================================

PRINT 'Checking for malformed redirect URIs...'
GO

-- Fix any RedirectUris that are not valid JSON arrays
UPDATE [OpenIddictApplications]
SET [RedirectUris] = '["' + [RedirectUris] + '"]'
WHERE [RedirectUris] IS NOT NULL
  AND [RedirectUris] NOT LIKE '[%'
  AND [RedirectUris] NOT LIKE 'null';

-- Fix any PostLogoutRedirectUris that are not valid JSON arrays
UPDATE [OpenIddictApplications]
SET [PostLogoutRedirectUris] = '["' + [PostLogoutRedirectUris] + '"]'
WHERE [PostLogoutRedirectUris] IS NOT NULL
  AND [PostLogoutRedirectUris] NOT LIKE '[%'
  AND [PostLogoutRedirectUris] NOT LIKE 'null';

PRINT 'Malformed URIs fixed (if any).'
GO

-- =============================================
-- STEP 2: Ensure required scopes exist
-- =============================================

PRINT 'Setting up required scopes...'
GO

-- Insert openid scope if not exists
IF NOT EXISTS (SELECT 1 FROM [OpenIddictScopes] WHERE [Name] = 'openid')
BEGIN
    INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
    VALUES (NEWID(), NEWID(), 'OpenID Connect scope', 'OpenID', 'openid', NULL, NULL);
    PRINT '  ✓ Created scope: openid'
END
ELSE
    PRINT '  ✓ Scope exists: openid'
GO

-- Insert profile scope if not exists
IF NOT EXISTS (SELECT 1 FROM [OpenIddictScopes] WHERE [Name] = 'profile')
BEGIN
    INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
    VALUES (NEWID(), NEWID(), 'Profile information scope', 'Profile', 'profile', NULL, NULL);
    PRINT '  ✓ Created scope: profile'
END
ELSE
    PRINT '  ✓ Scope exists: profile'
GO

-- Insert email scope if not exists
IF NOT EXISTS (SELECT 1 FROM [OpenIddictScopes] WHERE [Name] = 'email')
BEGIN
    INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
    VALUES (NEWID(), NEWID(), 'Email address scope', 'Email', 'email', NULL, NULL);
    PRINT '  ✓ Created scope: email'
END
ELSE
    PRINT '  ✓ Scope exists: email'
GO

-- Insert roles scope if not exists
IF NOT EXISTS (SELECT 1 FROM [OpenIddictScopes] WHERE [Name] = 'roles')
BEGIN
    INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
    VALUES (NEWID(), NEWID(), 'User roles scope', 'Roles', 'roles', NULL, NULL);
    PRINT '  ✓ Created scope: roles'
END
ELSE
    PRINT '  ✓ Scope exists: roles'
GO

PRINT 'Scopes configured.'
PRINT ''
GO

-- =============================================
-- STEP 3: Setup the web-app client
-- =============================================

PRINT 'Setting up web-app client for SampleSPA...'
GO

-- Delete existing web-app client if it exists (to start fresh)
DELETE FROM [OpenIddictTokens] WHERE [ApplicationId] IN (SELECT [Id] FROM [OpenIddictApplications] WHERE [ClientId] = 'web-app');
DELETE FROM [OpenIddictAuthorizations] WHERE [ApplicationId] IN (SELECT [Id] FROM [OpenIddictApplications] WHERE [ClientId] = 'web-app');
DELETE FROM [OpenIddictApplications] WHERE [ClientId] = 'web-app';
GO

-- Insert the web-app client with correct configuration for SampleSPA
INSERT INTO [OpenIddictApplications] (
    [Id],
    [ClientId],
    [ClientSecret],
    [ConcurrencyToken],
    [ConsentType],
    [DisplayName],
    [Permissions],
    [PostLogoutRedirectUris],
    [Properties],
    [RedirectUris],
    [Requirements],
    [ApplicationType]
)
VALUES (
    NEWID(),
    'web-app',
    NULL, -- No client secret for public clients (PKCE)
    NEWID(), -- Generate a new concurrency token
    'implicit', -- Implicit consent for testing (use 'explicit' for production)
    'Sample SPA Web Application',
    -- Permissions: endpoints, grant types, response types, and scopes
    '[
        "ept:authorization",
        "ept:logout",
        "ept:token",
        "gt:authorization_code",
        "rst:code",
        "scp:openid",
        "scp:profile",
        "scp:email",
        "scp:roles"
    ]',
    -- Post-logout redirect URIs (JSON array)
    '[
        "http://localhost:8080/index.html",
        "https://localhost:8080/index.html"
    ]',
    NULL, -- No custom properties
    -- Redirect URIs (JSON array) - supports both HTTP and HTTPS for local development
    '[
        "http://localhost:8080/callback.html",
        "https://localhost:8080/callback.html"
    ]',
    '["pkce"]', -- Require PKCE
    'public' -- Public client (SPA)
);
GO

PRINT 'Client setup complete!'
GO

-- Display the created client for verification
SELECT
    [ClientId],
    [DisplayName],
    [ApplicationType],
    [ConsentType],
    [RedirectUris],
    [PostLogoutRedirectUris],
    [Permissions],
    [Requirements]
FROM [OpenIddictApplications]
WHERE [ClientId] = 'web-app';
GO

PRINT ''
PRINT '========================================='
PRINT 'Setup Complete!'
PRINT '========================================='
PRINT ''
PRINT 'Client ID: web-app'
PRINT 'Client Type: public (PKCE required)'
PRINT 'Redirect URIs:'
PRINT '  - http://localhost:8080/callback.html'
PRINT '  - https://localhost:8080/callback.html'
PRINT ''
PRINT 'You can now run your SampleSPA application!'
PRINT '========================================='
GO

