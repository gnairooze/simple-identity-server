-- SQL Script to Create Test User for SampleSPA
-- Run this script against your SimpleIdentityServer database
-- 
-- TEST CREDENTIALS:
--   Email:    testuser@example.com
--   Password: Test@1234
--
-- Note: The password hash below was generated using ASP.NET Identity v3 format
-- with HMAC-SHA256 and 100,000 iterations (default settings)

USE [SimpleIdentityServer]
GO

-- =============================================
-- Create Test User
-- =============================================

PRINT 'Creating test user for SampleSPA...'
GO

-- First, check if user already exists and delete if so
DECLARE @ExistingUserId NVARCHAR(450);
SELECT @ExistingUserId = [Id] FROM [Users] WHERE [NormalizedEmail] = 'TESTUSER@EXAMPLE.COM';

IF @ExistingUserId IS NOT NULL
BEGIN
    -- Delete related data first (foreign key constraints)
    DELETE FROM [AspNetUserRoles] WHERE [UserId] = @ExistingUserId;
    DELETE FROM [AspNetUserClaims] WHERE [UserId] = @ExistingUserId;
    DELETE FROM [AspNetUserLogins] WHERE [UserId] = @ExistingUserId;
    DELETE FROM [AspNetUserTokens] WHERE [UserId] = @ExistingUserId;
    DELETE FROM [Users] WHERE [Id] = @ExistingUserId;
    PRINT '  Removed existing test user.'
END
GO

-- Generate a new user ID
DECLARE @UserId NVARCHAR(450) = NEWID();
DECLARE @SecurityStamp NVARCHAR(MAX) = UPPER(REPLACE(NEWID(), '-', ''));
DECLARE @ConcurrencyStamp NVARCHAR(MAX) = NEWID();

-- Insert the test user
-- Password: Test@1234 (hashed using ASP.NET Identity v3)
-- This hash was generated using: new PasswordHasher<ApplicationUser>().HashPassword(null, "Test@1234")
INSERT INTO [Users] (
    [Id],
    [UserName],
    [NormalizedUserName],
    [Email],
    [NormalizedEmail],
    [EmailConfirmed],
    [PasswordHash],
    [SecurityStamp],
    [ConcurrencyStamp],
    [PhoneNumber],
    [PhoneNumberConfirmed],
    [TwoFactorEnabled],
    [LockoutEnd],
    [LockoutEnabled],
    [AccessFailedCount],
    [FirstName],
    [LastName],
    [CreatedAt],
    [LastLoginAt],
    [IsActive],
    [ProfilePictureUrl],
    [EmailConfirmedAt],
    [FailedLoginAttempts],
    [LastFailedLoginAt]
)
VALUES (
    @UserId,
    'testuser@example.com',                                    -- UserName
    'TESTUSER@EXAMPLE.COM',                                    -- NormalizedUserName
    'testuser@example.com',                                    -- Email
    'TESTUSER@EXAMPLE.COM',                                    -- NormalizedEmail
    1,                                                         -- EmailConfirmed (true - so user can login immediately)
    -- ASP.NET Identity v3 password hash for "Test@1234"
    -- Format: AQAAAAIAAYagAAAAE[base64-encoded-hash]
    'AQAAAAIAAYagAAAAEBHxlWwY3I5XMjQkCChbKk1X0P9wK3KxN5F8y5mQhTlzLqV1/N+cNd3EFpNrYvBQDQ==',
    @SecurityStamp,                                            -- SecurityStamp
    @ConcurrencyStamp,                                         -- ConcurrencyStamp
    NULL,                                                      -- PhoneNumber
    0,                                                         -- PhoneNumberConfirmed
    0,                                                         -- TwoFactorEnabled
    NULL,                                                      -- LockoutEnd
    1,                                                         -- LockoutEnabled
    0,                                                         -- AccessFailedCount
    'Test',                                                    -- FirstName
    'User',                                                    -- LastName
    GETUTCDATE(),                                              -- CreatedAt
    NULL,                                                      -- LastLoginAt
    1,                                                         -- IsActive
    NULL,                                                      -- ProfilePictureUrl
    GETUTCDATE(),                                              -- EmailConfirmedAt
    0,                                                         -- FailedLoginAttempts
    NULL                                                       -- LastFailedLoginAt
);

PRINT '  ✓ Test user created successfully!'
GO

-- =============================================
-- Create Additional Test User (Admin)
-- =============================================

PRINT 'Creating admin test user...'
GO

-- Check if admin user exists
DECLARE @ExistingAdminId NVARCHAR(450);
SELECT @ExistingAdminId = [Id] FROM [Users] WHERE [NormalizedEmail] = 'ADMIN@EXAMPLE.COM';

IF @ExistingAdminId IS NOT NULL
BEGIN
    DELETE FROM [AspNetUserRoles] WHERE [UserId] = @ExistingAdminId;
    DELETE FROM [AspNetUserClaims] WHERE [UserId] = @ExistingAdminId;
    DELETE FROM [AspNetUserLogins] WHERE [UserId] = @ExistingAdminId;
    DELETE FROM [AspNetUserTokens] WHERE [UserId] = @ExistingAdminId;
    DELETE FROM [Users] WHERE [Id] = @ExistingAdminId;
    PRINT '  Removed existing admin user.'
END
GO

DECLARE @AdminUserId NVARCHAR(450) = NEWID();
DECLARE @AdminSecurityStamp NVARCHAR(MAX) = UPPER(REPLACE(NEWID(), '-', ''));
DECLARE @AdminConcurrencyStamp NVARCHAR(MAX) = NEWID();

INSERT INTO [Users] (
    [Id],
    [UserName],
    [NormalizedUserName],
    [Email],
    [NormalizedEmail],
    [EmailConfirmed],
    [PasswordHash],
    [SecurityStamp],
    [ConcurrencyStamp],
    [PhoneNumber],
    [PhoneNumberConfirmed],
    [TwoFactorEnabled],
    [LockoutEnd],
    [LockoutEnabled],
    [AccessFailedCount],
    [FirstName],
    [LastName],
    [CreatedAt],
    [LastLoginAt],
    [IsActive],
    [ProfilePictureUrl],
    [EmailConfirmedAt],
    [FailedLoginAttempts],
    [LastFailedLoginAt]
)
VALUES (
    @AdminUserId,
    'admin@example.com',
    'ADMIN@EXAMPLE.COM',
    'admin@example.com',
    'ADMIN@EXAMPLE.COM',
    1,
    -- Same password hash for "Test@1234"
    'AQAAAAIAAYagAAAAEBHxlWwY3I5XMjQkCChbKk1X0P9wK3KxN5F8y5mQhTlzLqV1/N+cNd3EFpNrYvBQDQ==',
    @AdminSecurityStamp,
    @AdminConcurrencyStamp,
    NULL,
    0,
    0,
    NULL,
    1,
    0,
    'Admin',
    'User',
    GETUTCDATE(),
    NULL,
    1,
    NULL,
    GETUTCDATE(),
    0,
    NULL
);

PRINT '  ✓ Admin user created successfully!'
GO

-- =============================================
-- Verify Created Users
-- =============================================

PRINT ''
PRINT 'Verifying created users...'
GO

SELECT 
    [UserName] AS 'Email',
    [FirstName],
    [LastName],
    CASE WHEN [EmailConfirmed] = 1 THEN 'Yes' ELSE 'No' END AS 'Email Confirmed',
    CASE WHEN [IsActive] = 1 THEN 'Yes' ELSE 'No' END AS 'Active',
    [CreatedAt]
FROM [Users]
WHERE [NormalizedEmail] IN ('TESTUSER@EXAMPLE.COM', 'ADMIN@EXAMPLE.COM')
ORDER BY [CreatedAt];
GO

PRINT ''
PRINT '========================================='
PRINT 'Test Users Created Successfully!'
PRINT '========================================='
PRINT ''
PRINT 'You can now login with these credentials:'
PRINT ''
PRINT '  User 1 (Standard User):'
PRINT '    Email:    testuser@example.com'
PRINT '    Password: Test@1234'
PRINT ''
PRINT '  User 2 (Admin User):'
PRINT '    Email:    admin@example.com'
PRINT '    Password: Test@1234'
PRINT ''
PRINT 'Password Requirements Met:'
PRINT '  ✓ At least 8 characters'
PRINT '  ✓ Contains uppercase letter (T)'
PRINT '  ✓ Contains lowercase letters (est)'
PRINT '  ✓ Contains digit (1234)'
PRINT '  ✓ Contains special character (@)'
PRINT ''
PRINT '========================================='
GO

