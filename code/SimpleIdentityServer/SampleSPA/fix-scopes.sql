-- Quick Fix for Invalid Scope Error (ID2052)
-- Run this script if you're getting: "The specified 'scope' is invalid"

USE [SimpleIdentityServer]
GO

PRINT '========================================='
PRINT 'Fixing OpenIddict Scopes'
PRINT '========================================='
PRINT ''

-- Create all required scopes
PRINT 'Creating/verifying scopes...'

-- openid scope (required for OIDC)
IF NOT EXISTS (SELECT 1 FROM [OpenIddictScopes] WHERE [Name] = 'openid')
BEGIN
    INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
    VALUES (NEWID(), NEWID(), 'OpenID Connect scope', 'OpenID', 'openid', NULL, NULL);
    PRINT '  + Created: openid'
END
ELSE
    PRINT '  ✓ Exists: openid'

-- profile scope
IF NOT EXISTS (SELECT 1 FROM [OpenIddictScopes] WHERE [Name] = 'profile')
BEGIN
    INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
    VALUES (NEWID(), NEWID(), 'User profile information', 'Profile', 'profile', NULL, NULL);
    PRINT '  + Created: profile'
END
ELSE
    PRINT '  ✓ Exists: profile'

-- email scope
IF NOT EXISTS (SELECT 1 FROM [OpenIddictScopes] WHERE [Name] = 'email')
BEGIN
    INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
    VALUES (NEWID(), NEWID(), 'Email address', 'Email', 'email', NULL, NULL);
    PRINT '  + Created: email'
END
ELSE
    PRINT '  ✓ Exists: email'

-- roles scope
IF NOT EXISTS (SELECT 1 FROM [OpenIddictScopes] WHERE [Name] = 'roles')
BEGIN
    INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
    VALUES (NEWID(), NEWID(), 'User roles', 'Roles', 'roles', NULL, NULL);
    PRINT '  + Created: roles'
END
ELSE
    PRINT '  ✓ Exists: roles'

PRINT ''
PRINT 'Updating web-app client permissions...'

-- Update the web-app client to ensure it has all scope permissions
UPDATE [OpenIddictApplications]
SET [Permissions] = '[
    "ept:authorization",
    "ept:logout",
    "ept:token",
    "gt:authorization_code",
    "rst:code",
    "scp:openid",
    "scp:profile",
    "scp:email",
    "scp:roles"
]'
WHERE [ClientId] = 'web-app';

IF @@ROWCOUNT > 0
    PRINT '  ✓ Updated web-app permissions'
ELSE
    PRINT '  ! web-app client not found - run setup-client.sql first'

PRINT ''
PRINT '========================================='
PRINT 'Scope fix complete!'
PRINT '========================================='
PRINT ''
PRINT 'Next steps:'
PRINT '1. Restart your Identity Server'
PRINT '2. Try logging in again'
PRINT ''

-- Show current scopes
PRINT 'Current scopes in database:'
SELECT [Name], [DisplayName], [Description] FROM [OpenIddictScopes] ORDER BY [Name]

-- Show web-app permissions
PRINT ''
PRINT 'web-app client permissions:'
SELECT [ClientId], [Permissions] FROM [OpenIddictApplications] WHERE [ClientId] = 'web-app'

GO

