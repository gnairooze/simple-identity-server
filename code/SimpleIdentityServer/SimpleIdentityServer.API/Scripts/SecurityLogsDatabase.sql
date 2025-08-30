-- Security Logs Database Setup Script
-- This script creates the security logs database and configures it for optimal performance

USE master;
GO

-- Create the security logs database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SimpleIdentityServerSecurityLogs')
BEGIN
    CREATE DATABASE SimpleIdentityServerSecurityLogs;
END
GO

USE SimpleIdentityServerSecurityLogs;
GO

-- The SecurityLogs table will be auto-created by Serilog, but we can create indexes for better performance
-- This script can be run after the application starts and creates the table

-- Wait for the table to be created by Serilog first, then run this:
/*
-- Create indexes for better query performance on security logs
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SecurityLogs]') AND type in (N'U'))
BEGIN
    -- Index on TimeStamp for date-based queries and cleanup
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SecurityLogs]') AND name = N'IX_SecurityLogs_TimeStamp')
        CREATE INDEX IX_SecurityLogs_TimeStamp ON SecurityLogs (TimeStamp DESC);

    -- Index on EventType for filtering by event types
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SecurityLogs]') AND name = N'IX_SecurityLogs_EventType')
        CREATE INDEX IX_SecurityLogs_EventType ON SecurityLogs (EventType);

    -- Index on IpAddress for IP-based analysis
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SecurityLogs]') AND name = N'IX_SecurityLogs_IpAddress')
        CREATE INDEX IX_SecurityLogs_IpAddress ON SecurityLogs (IpAddress);

    -- Index on ClientId for client-based analysis
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SecurityLogs]') AND name = N'IX_SecurityLogs_ClientId')
        CREATE INDEX IX_SecurityLogs_ClientId ON SecurityLogs (ClientId);

    -- Composite index for common queries (EventType + TimeStamp)
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SecurityLogs]') AND name = N'IX_SecurityLogs_EventType_TimeStamp')
        CREATE INDEX IX_SecurityLogs_EventType_TimeStamp ON SecurityLogs (EventType, TimeStamp DESC);

    -- Composite index for IP and time-based analysis
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SecurityLogs]') AND name = N'IX_SecurityLogs_IpAddress_TimeStamp')
        CREATE INDEX IX_SecurityLogs_IpAddress_TimeStamp ON SecurityLogs (IpAddress, TimeStamp DESC);
END
*/

-- Create a stored procedure for manual cleanup (in addition to the automatic cleanup)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CleanupSecurityLogs]') AND type in (N'P', N'PC'))
BEGIN
    EXEC('
    CREATE PROCEDURE CleanupSecurityLogs
        @RetentionDays INT = 30
    AS
    BEGIN
        SET NOCOUNT ON;
        
        DECLARE @CutoffDate DATETIME2 = DATEADD(day, -@RetentionDays, GETUTCDATE());
        DECLARE @DeletedRows INT;
        
        -- Delete in batches to avoid blocking
        WHILE 1 = 1
        BEGIN
            DELETE TOP (10000) FROM SecurityLogs 
            WHERE TimeStamp < @CutoffDate;
            
            SET @DeletedRows = @@ROWCOUNT;
            
            IF @DeletedRows = 0
                BREAK;
                
            -- Log progress
            PRINT ''Deleted '' + CAST(@DeletedRows AS VARCHAR(10)) + '' security log records older than '' + CAST(@RetentionDays AS VARCHAR(3)) + '' days'';
            
            -- Small delay to prevent blocking
            WAITFOR DELAY ''00:00:01'';
        END
        
        PRINT ''Security logs cleanup completed'';
    END
    ');
END
GO

-- Create a view for common security analytics queries
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[SecurityLogsSummary]'))
BEGIN
    EXEC('
    CREATE VIEW SecurityLogsSummary AS
    SELECT 
        EventType,
        COUNT(*) as EventCount,
        COUNT(DISTINCT IpAddress) as UniqueIPs,
        COUNT(DISTINCT ClientId) as UniqueClients,
        MIN(TimeStamp) as FirstOccurrence,
        MAX(TimeStamp) as LastOccurrence,
        AVG(CASE WHEN DurationMs IS NOT NULL THEN DurationMs END) as AvgDurationMs
    FROM SecurityLogs
    WHERE TimeStamp >= DATEADD(day, -7, GETUTCDATE()) -- Last 7 days
    GROUP BY EventType
    ');
END
GO

PRINT 'Security logs database setup completed successfully!';
PRINT 'Note: Indexes will be created automatically after the SecurityLogs table is created by Serilog.';
PRINT 'Run the commented index creation statements after the application starts for the first time.';
