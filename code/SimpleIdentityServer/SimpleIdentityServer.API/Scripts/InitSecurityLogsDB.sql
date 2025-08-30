-- Simple Security Logs Database Creation Script
-- This script creates the security logs database if it doesn't exist

USE master;

-- Create the security logs database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SimpleIdentityServerSecurityLogs')
BEGIN
    CREATE DATABASE SimpleIdentityServerSecurityLogs;
    PRINT 'SimpleIdentityServerSecurityLogs database created successfully.';
END
ELSE
BEGIN
    PRINT 'SimpleIdentityServerSecurityLogs database already exists.';
END

-- Switch to the new database
USE SimpleIdentityServerSecurityLogs;

-- Create a simple stored procedure for cleanup (basic version)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CleanupSecurityLogs]') AND type in (N'P', N'PC'))
BEGIN
    CREATE PROCEDURE CleanupSecurityLogs
        @RetentionDays INT = 30
    AS
    BEGIN
        SET NOCOUNT ON;
        
        -- This procedure will be used after the SecurityLogs table is created by Serilog
        DECLARE @CutoffDate DATETIME2 = DATEADD(day, -@RetentionDays, GETUTCDATE());
        
        -- Check if SecurityLogs table exists before trying to delete
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SecurityLogs]') AND type in (N'U'))
        BEGIN
            DELETE FROM SecurityLogs WHERE TimeStamp < @CutoffDate;
            PRINT 'Security logs cleanup completed.';
        END
        ELSE
        BEGIN
            PRINT 'SecurityLogs table does not exist yet. Run this procedure after Serilog creates the table.';
        END
    END
END

PRINT 'Security logs database initialization completed successfully!';
