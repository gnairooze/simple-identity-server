-- Initialize Security Logs Database and Table
-- This script ensures the SecurityLogs database and table exist with proper schema

USE master;
GO

-- Create SecurityLogs database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SimpleIdentityServerSecurityLogs')
BEGIN
    CREATE DATABASE SimpleIdentityServerSecurityLogs;
    PRINT 'Created SimpleIdentityServerSecurityLogs database';
END
ELSE
BEGIN
    PRINT 'SimpleIdentityServerSecurityLogs database already exists';
END
GO

-- Switch to SecurityLogs database
USE SimpleIdentityServerSecurityLogs;
GO

-- Create SecurityLogs table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SecurityLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SecurityLogs] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [TimeStamp] datetime2(7) NOT NULL,
        [Level] nvarchar(128) NULL,
        [Message] nvarchar(max) NULL,
        [Exception] nvarchar(max) NULL,
        [LogEvent] nvarchar(max) NULL,
        [RequestId] nvarchar(100) NULL,
        [EventType] nvarchar(50) NULL,
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Path] nvarchar(200) NULL,
        [Method] nvarchar(10) NULL,
        [StatusCode] int NULL,
        [DurationMs] float NULL,
        [ClientId] nvarchar(100) NULL,
        [NodeName] nvarchar(50) NULL,
        CONSTRAINT [PK_SecurityLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    PRINT 'Created SecurityLogs table with all required columns';
END
ELSE
BEGIN
    PRINT 'SecurityLogs table already exists';
END
GO

-- Create main application database if it doesn't exist
USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SimpleIdentityServer')
BEGIN
    CREATE DATABASE SimpleIdentityServer;
    PRINT 'Created SimpleIdentityServer database';
END
ELSE
BEGIN
    PRINT 'SimpleIdentityServer database already exists';
END
GO

PRINT 'Database initialization completed successfully';
