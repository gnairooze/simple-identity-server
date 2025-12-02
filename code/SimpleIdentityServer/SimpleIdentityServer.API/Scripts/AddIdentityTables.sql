-- Add Identity tables for Authorization Code flow
-- This script should be run after creating a new migration

-- Note: Most Identity tables are created automatically by Entity Framework migrations
-- This script adds additional columns and indexes for ApplicationUser

-- Add custom columns to Users table if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'FirstName')
BEGIN
    ALTER TABLE [Users] ADD 
        [FirstName] NVARCHAR(100) NULL,
        [LastName] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastLoginAt] DATETIME2 NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [ProfilePictureUrl] NVARCHAR(500) NULL,
        [EmailConfirmedAt] DATETIME2 NULL,
        [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
        [LastFailedLoginAt] DATETIME2 NULL;
END
GO

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID(N'[dbo].[Users]'))
BEGIN
    CREATE INDEX IX_Users_Email ON [Users] ([Email]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_IsActive' AND object_id = OBJECT_ID(N'[dbo].[Users]'))
BEGIN
    CREATE INDEX IX_Users_IsActive ON [Users] ([IsActive]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_CreatedAt' AND object_id = OBJECT_ID(N'[dbo].[Users]'))
BEGIN
    CREATE INDEX IX_Users_CreatedAt ON [Users] ([CreatedAt]);
END
GO

-- Insert default scopes for Authorization Code flow if they don't exist
IF NOT EXISTS (SELECT 1 FROM [OpenIddictScopes] WHERE [Name] = 'openid')
BEGIN
    INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
    VALUES 
        (NEWID(), NULL, 'OpenID Connect scope', 'OpenID', 'openid', NULL, NULL);
END
GO

IF NOT EXISTS (SELECT 1 FROM [OpenIddictScopes] WHERE [Name] = 'profile')
BEGIN
    INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
    VALUES 
        (NEWID(), NULL, 'Profile information scope', 'Profile', 'profile', NULL, NULL);
END
GO

IF NOT EXISTS (SELECT 1 FROM [OpenIddictScopes] WHERE [Name] = 'email')
BEGIN
    INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
    VALUES 
        (NEWID(), NULL, 'Email scope', 'Email', 'email', NULL, NULL);
END
GO

IF NOT EXISTS (SELECT 1 FROM [OpenIddictScopes] WHERE [Name] = 'roles')
BEGIN
    INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
    VALUES 
        (NEWID(), NULL, 'Roles scope', 'Roles', 'roles', NULL, NULL);
END
GO

-- Insert sample web application client if it doesn't exist
-- Note: Replace this with your actual client configuration
-- The client secret below is hashed - you'll need to generate a proper hash for production
IF NOT EXISTS (SELECT 1 FROM [OpenIddictApplications] WHERE [ClientId] = 'web-app')
BEGIN
    INSERT INTO [OpenIddictApplications] ([Id], [ClientId], [ClientSecret], [ConcurrencyToken], [ConsentType], [DisplayName], [Permissions], [PostLogoutRedirectUris], [Properties], [RedirectUris], [Requirements], [Type])
    VALUES (
        NEWID(),
        'web-app',
        NULL, -- For PKCE flow, client secret can be null
        NULL,
        'implicit', -- implicit consent for testing, change to 'explicit' for production
        'Web Application',
        '["ept:authorization", "ept:logout", "ept:token", "gt:authorization_code", "rst:code", "scp:openid", "scp:profile", "scp:email"]',
        '["https://localhost:5001/signout-callback-oidc"]',
        NULL,
        '["https://localhost:5001/signin-oidc"]',
        '["pkce"]', -- Require PKCE
        'public' -- Public client (SPA, mobile app) - use 'confidential' for server-side apps with client secret
    );
END
GO

PRINT 'Identity tables and initial data setup complete.';

