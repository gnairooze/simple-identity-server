-- Direct Database Creation Script
-- This script creates the security logs database and ensures proper permissions

PRINT 'Starting database creation process...'

-- Switch to master database
USE master

-- Check if database exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SimpleIdentityServerSecurityLogs')
BEGIN
    PRINT 'Creating SimpleIdentityServerSecurityLogs database...'
    CREATE DATABASE SimpleIdentityServerSecurityLogs
    PRINT 'Database created successfully!'
END
ELSE
BEGIN
    PRINT 'SimpleIdentityServerSecurityLogs database already exists.'
END
