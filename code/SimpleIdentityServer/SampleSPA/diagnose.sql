-- Diagnostic SQL Script for SampleSPA OAuth Client Issues
-- Run this script to check the current state of your OAuth clients
-- and identify any configuration issues

USE [SimpleIdentityServer]
GO

PRINT '========================================='
PRINT 'SampleSPA OAuth Client Diagnostics'
PRINT '========================================='
PRINT ''

-- Check if the database exists
IF DB_ID('SimpleIdentityServer') IS NULL
BEGIN
    PRINT '❌ ERROR: Database ''SimpleIdentityServer'' not found!'
    PRINT '   Please check your database connection.'
    RETURN
END

PRINT '✓ Database found: SimpleIdentityServer'
PRINT ''

-- Check if OpenIddict tables exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OpenIddictApplications')
BEGIN
    PRINT '❌ ERROR: OpenIddictApplications table not found!'
    PRINT '   OpenIddict tables may not be initialized.'
    RETURN
END

PRINT '✓ OpenIddict tables found'
PRINT ''

-- Check for web-app client
IF NOT EXISTS (SELECT 1 FROM [OpenIddictApplications] WHERE [ClientId] = 'web-app')
BEGIN
    PRINT '❌ ERROR: Client ''web-app'' not found in database!'
    PRINT ''
    PRINT 'SOLUTION: Run the setup-client.sql script to create the client.'
    PRINT ''
    PRINT 'Available clients:'
    SELECT [ClientId], [DisplayName], [Type]
    FROM [OpenIddictApplications]
    ORDER BY [ClientId]
    RETURN
END

PRINT '✓ Client ''web-app'' exists'
PRINT ''

-- Display current client configuration
PRINT 'Current Configuration for ''web-app'':'
PRINT '-------------------------------------'

DECLARE @ClientId NVARCHAR(100)
DECLARE @DisplayName NVARCHAR(MAX)
DECLARE @Type NVARCHAR(50)
DECLARE @ConsentType NVARCHAR(50)
DECLARE @RedirectUris NVARCHAR(MAX)
DECLARE @PostLogoutUris NVARCHAR(MAX)
DECLARE @Permissions NVARCHAR(MAX)
DECLARE @Requirements NVARCHAR(MAX)

SELECT 
    @ClientId = [ClientId],
    @DisplayName = [DisplayName],
    @Type = [Type],
    @ConsentType = [ConsentType],
    @RedirectUris = [RedirectUris],
    @PostLogoutUris = [PostLogoutRedirectUris],
    @Permissions = [Permissions],
    @Requirements = [Requirements]
FROM [OpenIddictApplications]
WHERE [ClientId] = 'web-app'

PRINT 'Client ID: ' + ISNULL(@ClientId, 'NULL')
PRINT 'Display Name: ' + ISNULL(@DisplayName, 'NULL')
PRINT 'Type: ' + ISNULL(@Type, 'NULL')
PRINT 'Consent Type: ' + ISNULL(@ConsentType, 'NULL')
PRINT ''
PRINT 'Redirect URIs:'
PRINT '  ' + ISNULL(@RedirectUris, 'NULL')
PRINT ''
PRINT 'Post-Logout Redirect URIs:'
PRINT '  ' + ISNULL(@PostLogoutUris, 'NULL')
PRINT ''
PRINT 'Permissions:'
PRINT '  ' + ISNULL(@Permissions, 'NULL')
PRINT ''
PRINT 'Requirements:'
PRINT '  ' + ISNULL(@Requirements, 'NULL')
PRINT ''

-- Check for JSON format issues
PRINT 'Checking for common issues...'
PRINT '-------------------------------------'

DECLARE @HasIssues BIT = 0

-- Check if RedirectUris is valid JSON array
IF @RedirectUris IS NOT NULL AND LEFT(@RedirectUris, 1) != '['
BEGIN
    PRINT '❌ ERROR: RedirectUris is not a valid JSON array!'
    PRINT '   Current value starts with: ' + LEFT(@RedirectUris, 20)
    PRINT '   Expected format: ["http://..."]'
    SET @HasIssues = 1
END
ELSE IF @RedirectUris IS NOT NULL
BEGIN
    PRINT '✓ RedirectUris format looks correct'
    
    -- Check if it contains the expected SampleSPA URI
    IF @RedirectUris NOT LIKE '%localhost:8080/callback.html%'
    BEGIN
        PRINT '⚠ WARNING: RedirectUris does not include http://localhost:8080/callback.html'
        PRINT '   SampleSPA requires this redirect URI.'
        SET @HasIssues = 1
    END
    ELSE
    BEGIN
        PRINT '✓ SampleSPA redirect URI found'
    END
END
ELSE
BEGIN
    PRINT '❌ ERROR: RedirectUris is NULL!'
    SET @HasIssues = 1
END

-- Check if PostLogoutRedirectUris is valid JSON array
IF @PostLogoutUris IS NOT NULL AND LEFT(@PostLogoutUris, 1) != '['
BEGIN
    PRINT '❌ ERROR: PostLogoutRedirectUris is not a valid JSON array!'
    PRINT '   Current value starts with: ' + LEFT(@PostLogoutUris, 20)
    PRINT '   Expected format: ["http://..."]'
    SET @HasIssues = 1
END
ELSE IF @PostLogoutUris IS NOT NULL
BEGIN
    PRINT '✓ PostLogoutRedirectUris format looks correct'
END

-- Check client type
IF @Type != 'public'
BEGIN
    PRINT '⚠ WARNING: Client type is ''' + @Type + ''' (expected: ''public'' for SPA)'
    PRINT '   SPAs should use public client type with PKCE.'
END
ELSE
BEGIN
    PRINT '✓ Client type is correct (public)'
END

-- Check for PKCE requirement
IF @Requirements IS NULL OR @Requirements NOT LIKE '%pkce%'
BEGIN
    PRINT '⚠ WARNING: PKCE requirement not found!'
    PRINT '   SampleSPA requires PKCE to be enabled.'
    SET @HasIssues = 1
END
ELSE
BEGIN
    PRINT '✓ PKCE requirement is set'
END

-- Check for required permissions
DECLARE @MissingPermissions TABLE (Permission NVARCHAR(100))

IF @Permissions NOT LIKE '%ept:authorization%'
    INSERT INTO @MissingPermissions VALUES ('ept:authorization')
IF @Permissions NOT LIKE '%ept:token%'
    INSERT INTO @MissingPermissions VALUES ('ept:token')
IF @Permissions NOT LIKE '%gt:authorization_code%'
    INSERT INTO @MissingPermissions VALUES ('gt:authorization_code')
IF @Permissions NOT LIKE '%rst:code%'
    INSERT INTO @MissingPermissions VALUES ('rst:code')
IF @Permissions NOT LIKE '%scp:openid%'
    INSERT INTO @MissingPermissions VALUES ('scp:openid')

IF EXISTS (SELECT 1 FROM @MissingPermissions)
BEGIN
    PRINT '⚠ WARNING: Missing required permissions:'
    SELECT '  - ' + Permission FROM @MissingPermissions
    SET @HasIssues = 1
END
ELSE
BEGIN
    PRINT '✓ All required permissions found'
END

PRINT ''
PRINT '========================================='
PRINT 'Diagnosis Summary'
PRINT '========================================='

IF @HasIssues = 1
BEGIN
    PRINT '❌ ISSUES FOUND - Client configuration needs correction'
    PRINT ''
    PRINT 'RECOMMENDED ACTION:'
    PRINT '1. Run the setup-client.sql script to fix the configuration'
    PRINT '2. Restart your Identity Server'
    PRINT '3. Try the login flow again'
END
ELSE
BEGIN
    PRINT '✓ No issues found - Configuration looks good!'
    PRINT ''
    PRINT 'If you are still experiencing errors:'
    PRINT '1. Restart your Identity Server to clear caches'
    PRINT '2. Check your SampleSPA configuration in js/config.js'
    PRINT '3. Verify CORS settings on Identity Server'
    PRINT '4. Check Identity Server logs for detailed errors'
END

PRINT ''
PRINT '========================================='

-- Show all clients for reference
PRINT ''
PRINT 'All registered clients:'
PRINT '-------------------------------------'
SELECT 
    [ClientId],
    [DisplayName],
    [Type],
    CASE 
        WHEN LEN([RedirectUris]) > 50 
        THEN LEFT([RedirectUris], 50) + '...'
        ELSE [RedirectUris]
    END AS [RedirectUris]
FROM [OpenIddictApplications]
ORDER BY [ClientId]

GO

