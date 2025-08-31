-- Fix SQL Server Query Cost Threshold Issue
-- This script disables the query governor cost limit that's preventing expensive queries

-- Method 1: Disable query governor cost limit globally
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;

EXEC sp_configure 'query governor cost limit', 0;  -- 0 = disabled
RECONFIGURE;

-- Method 2: Check and disable Resource Governor if it's enabled
IF EXISTS (SELECT * FROM sys.resource_governor_configuration WHERE is_enabled = 1)
BEGIN
    PRINT 'Resource Governor is enabled. Disabling it...'
    ALTER RESOURCE GOVERNOR DISABLE;
END
ELSE
BEGIN
    PRINT 'Resource Governor is already disabled.'
END

-- Method 3: Check current configuration
SELECT name, value, value_in_use, description 
FROM sys.configurations 
WHERE name LIKE '%cost%' OR name LIKE '%governor%' OR name LIKE '%threshold%';

-- Method 4: Check Resource Governor settings
SELECT * FROM sys.resource_governor_configuration;

PRINT 'Query cost limit configuration updated. Please restart SQL Server or reconnect applications.';
