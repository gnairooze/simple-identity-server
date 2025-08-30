# Security Logs SQL Queries

This document provides comprehensive SQL queries for reading and analyzing HTTP request and response data from the SecurityLogs database in the Simple Identity Server.

## Table of Contents

- [Database Schema](#database-schema)
- [Basic Request/Response Queries](#basic-requestresponse-queries)
- [Advanced Analytical Queries](#advanced-analytical-queries)
- [Security-Focused Queries](#security-focused-queries)
- [Performance Monitoring Queries](#performance-monitoring-queries)
- [Client Analysis Queries](#client-analysis-queries)
- [Real-time Monitoring](#real-time-monitoring)
- [Stored Procedures](#stored-procedures)
- [Usage Examples](#usage-examples)

## Database Schema

The SecurityLogs table structure:

```sql
CREATE TABLE [dbo].[SecurityLogs] (
    [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Message] nvarchar(max) NULL,
    [MessageTemplate] nvarchar(max) NULL,
    [Level] nvarchar(128) NULL,
    [TimeStamp] datetime2(7) NOT NULL,
    [Exception] nvarchar(max) NULL,
    [LogEvent] nvarchar(max) NULL,
    
    -- Custom Security Fields
    [RequestId] nvarchar(100) NULL,
    [EventType] nvarchar(50) NULL,
    [IpAddress] nvarchar(45) NULL,
    [UserAgent] nvarchar(500) NULL,
    [Path] nvarchar(200) NULL,
    [Method] nvarchar(10) NULL,
    [StatusCode] int NULL,
    [DurationMs] float NULL,
    [ClientId] nvarchar(100) NULL,
    [NodeName] nvarchar(50) NULL
);
```

## Basic Request/Response Queries

### 1. Get All Recent Requests and Responses

```sql
-- Get all requests from the last 24 hours
SELECT 
    Id,
    TimeStamp,
    RequestId,
    EventType,
    Method,
    Path,
    StatusCode,
    DurationMs,
    IpAddress,
    UserAgent,
    ClientId,
    NodeName,
    Message
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(hour, -24, GETUTCDATE())
ORDER BY TimeStamp DESC;
```

### 2. Get Requests by Specific Path/Endpoint

```sql
-- Get all token endpoint requests
SELECT 
    TimeStamp,
    RequestId,
    Method,
    Path,
    StatusCode,
    DurationMs,
    IpAddress,
    ClientId,
    Message
FROM SecurityLogs 
WHERE Path LIKE '%/connect/token%'
  AND TimeStamp >= DATEADD(day, -7, GETUTCDATE())
ORDER BY TimeStamp DESC;
```

### 3. Get Request/Response Pairs by RequestId

```sql
-- Get all events for a specific request (request and response correlation)
SELECT 
    TimeStamp,
    EventType,
    Method,
    Path,
    StatusCode,
    DurationMs,
    Level,
    Message,
    Exception
FROM SecurityLogs 
WHERE RequestId = 'your-request-id-here'
ORDER BY TimeStamp ASC;
```

### 4. Get Requests by HTTP Method

```sql
-- Get all POST requests from the last week
SELECT 
    TimeStamp,
    RequestId,
    Path,
    StatusCode,
    DurationMs,
    IpAddress,
    ClientId
FROM SecurityLogs 
WHERE Method = 'POST'
  AND TimeStamp >= DATEADD(day, -7, GETUTCDATE())
ORDER BY TimeStamp DESC;
```

### 5. Get Requests by Status Code Range

```sql
-- Get all successful requests (2xx status codes)
SELECT 
    TimeStamp,
    RequestId,
    Method,
    Path,
    StatusCode,
    DurationMs,
    IpAddress,
    ClientId
FROM SecurityLogs 
WHERE StatusCode BETWEEN 200 AND 299
  AND TimeStamp >= DATEADD(day, -1, GETUTCDATE())
ORDER BY TimeStamp DESC;
```

## Advanced Analytical Queries

### 6. Request Volume Analysis

```sql
-- Request volume by hour for the last 24 hours
SELECT 
    DATEPART(hour, TimeStamp) as Hour,
    COUNT(*) as RequestCount,
    AVG(DurationMs) as AvgDuration,
    COUNT(DISTINCT IpAddress) as UniqueIPs,
    COUNT(DISTINCT ClientId) as UniqueClients
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(hour, -24, GETUTCDATE())
  AND EventType = 'REQUEST_COMPLETED'
GROUP BY DATEPART(hour, TimeStamp)
ORDER BY Hour;
```

### 7. Daily Request Trends

```sql
-- Daily request trends for the last 30 days
SELECT 
    CAST(TimeStamp AS DATE) as RequestDate,
    COUNT(*) as TotalRequests,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorRequests,
    AVG(DurationMs) as AvgDuration,
    COUNT(DISTINCT IpAddress) as UniqueIPs
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(day, -30, GETUTCDATE())
  AND EventType = 'REQUEST_COMPLETED'
GROUP BY CAST(TimeStamp AS DATE)
ORDER BY RequestDate DESC;
```

### 8. Error Response Analysis

```sql
-- Get all error responses (4xx and 5xx status codes)
SELECT 
    TimeStamp,
    RequestId,
    Method,
    Path,
    StatusCode,
    DurationMs,
    IpAddress,
    ClientId,
    Message,
    Exception
FROM SecurityLogs 
WHERE StatusCode >= 400
  AND TimeStamp >= DATEADD(day, -7, GETUTCDATE())
ORDER BY StatusCode DESC, TimeStamp DESC;
```

### 9. Error Rate by Endpoint

```sql
-- Error rate analysis by endpoint
SELECT 
    Path,
    Method,
    COUNT(*) as TotalRequests,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorRequests,
    ROUND(
        (COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) * 100.0) / COUNT(*), 
        2
    ) as ErrorRate,
    COUNT(CASE WHEN StatusCode = 500 THEN 1 END) as ServerErrors,
    COUNT(CASE WHEN StatusCode = 404 THEN 1 END) as NotFoundErrors,
    COUNT(CASE WHEN StatusCode = 401 THEN 1 END) as UnauthorizedErrors
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(day, -7, GETUTCDATE())
  AND StatusCode IS NOT NULL
GROUP BY Path, Method
HAVING COUNT(*) > 10  -- Only include endpoints with significant traffic
ORDER BY ErrorRate DESC;
```

### 10. Slow Request Analysis

```sql
-- Find slow requests (> 5 seconds)
SELECT 
    TimeStamp,
    RequestId,
    Method,
    Path,
    StatusCode,
    DurationMs,
    IpAddress,
    ClientId,
    NodeName
FROM SecurityLogs 
WHERE DurationMs > 5000
  AND TimeStamp >= DATEADD(day, -7, GETUTCDATE())
ORDER BY DurationMs DESC;
```

## Security-Focused Queries

### 11. Suspicious Activity Detection

```sql
-- Get suspicious token frequency events
SELECT 
    TimeStamp,
    IpAddress,
    ClientId,
    Path,
    Message,
    UserAgent
FROM SecurityLogs 
WHERE EventType IN ('SUSPICIOUS_TOKEN_FREQUENCY', 'HIGH_TOKEN_FREQUENCY')
  AND TimeStamp >= DATEADD(day, -1, GETUTCDATE())
ORDER BY TimeStamp DESC;
```

### 12. IP Address Activity Summary

```sql
-- Analyze activity by IP address
SELECT 
    IpAddress,
    COUNT(*) as TotalRequests,
    COUNT(DISTINCT Path) as UniquePaths,
    COUNT(DISTINCT ClientId) as UniqueClients,
    MIN(TimeStamp) as FirstSeen,
    MAX(TimeStamp) as LastSeen,
    AVG(DurationMs) as AvgDuration,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorCount,
    COUNT(CASE WHEN EventType LIKE '%SUSPICIOUS%' THEN 1 END) as SuspiciousEvents
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(day, -7, GETUTCDATE())
  AND IpAddress IS NOT NULL
GROUP BY IpAddress
ORDER BY TotalRequests DESC;
```

### 13. Failed Authentication Attempts

```sql
-- Track failed authentication attempts
SELECT 
    IpAddress,
    UserAgent,
    COUNT(*) as FailedAttempts,
    MIN(TimeStamp) as FirstAttempt,
    MAX(TimeStamp) as LastAttempt,
    COUNT(DISTINCT ClientId) as DifferentClients
FROM SecurityLogs 
WHERE StatusCode = 401  -- Unauthorized
  AND Path LIKE '%/connect/token%'
  AND TimeStamp >= DATEADD(day, -1, GETUTCDATE())
GROUP BY IpAddress, UserAgent
HAVING COUNT(*) > 5  -- More than 5 failed attempts
ORDER BY FailedAttempts DESC;
```

### 14. Anomalous User Agent Detection

```sql
-- Detect unusual or suspicious user agents
SELECT 
    UserAgent,
    COUNT(*) as RequestCount,
    COUNT(DISTINCT IpAddress) as UniqueIPs,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorCount,
    MIN(TimeStamp) as FirstSeen,
    MAX(TimeStamp) as LastSeen
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(day, -7, GETUTCDATE())
  AND UserAgent IS NOT NULL
  AND (
    UserAgent LIKE '%bot%' 
    OR UserAgent LIKE '%crawler%'
    OR UserAgent LIKE '%scanner%'
    OR LEN(UserAgent) < 10  -- Suspiciously short user agents
  )
GROUP BY UserAgent
ORDER BY RequestCount DESC;
```

## Performance Monitoring Queries

### 15. Endpoint Performance Analysis

```sql
-- Performance analysis by endpoint
SELECT 
    Path,
    Method,
    COUNT(*) as RequestCount,
    AVG(DurationMs) as AvgDuration,
    MIN(DurationMs) as MinDuration,
    MAX(DurationMs) as MaxDuration,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY DurationMs) as MedianDuration,
    PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY DurationMs) as P95Duration,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorCount,
    COUNT(CASE WHEN DurationMs > 5000 THEN 1 END) as SlowRequests
FROM SecurityLogs 
WHERE DurationMs IS NOT NULL
  AND TimeStamp >= DATEADD(day, -7, GETUTCDATE())
GROUP BY Path, Method
HAVING COUNT(*) > 10  -- Only include endpoints with significant traffic
ORDER BY RequestCount DESC;
```

### 16. Node Performance Comparison

```sql
-- Compare performance across different API instances
SELECT 
    NodeName,
    COUNT(*) as RequestCount,
    AVG(DurationMs) as AvgDuration,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorCount,
    COUNT(CASE WHEN DurationMs > 5000 THEN 1 END) as SlowRequests,
    ROUND(
        (COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) * 100.0) / COUNT(*), 
        2
    ) as ErrorRate,
    MAX(TimeStamp) as LastActivity
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(day, -1, GETUTCDATE())
  AND NodeName IS NOT NULL
GROUP BY NodeName
ORDER BY RequestCount DESC;
```

### 17. Performance Over Time

```sql
-- Track performance trends over time (hourly)
SELECT 
    DATEPART(year, TimeStamp) as Year,
    DATEPART(month, TimeStamp) as Month,
    DATEPART(day, TimeStamp) as Day,
    DATEPART(hour, TimeStamp) as Hour,
    COUNT(*) as RequestCount,
    AVG(DurationMs) as AvgDuration,
    MAX(DurationMs) as MaxDuration,
    COUNT(CASE WHEN DurationMs > 5000 THEN 1 END) as SlowRequests,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorCount
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(day, -7, GETUTCDATE())
  AND DurationMs IS NOT NULL
GROUP BY 
    DATEPART(year, TimeStamp),
    DATEPART(month, TimeStamp),
    DATEPART(day, TimeStamp),
    DATEPART(hour, TimeStamp)
ORDER BY Year DESC, Month DESC, Day DESC, Hour DESC;
```

## Client Analysis Queries

### 18. Client Authentication Patterns

```sql
-- Analyze authentication patterns by client
SELECT 
    ClientId,
    COUNT(*) as TokenRequests,
    COUNT(DISTINCT IpAddress) as UniqueIPs,
    AVG(DurationMs) as AvgDuration,
    COUNT(CASE WHEN EventType = 'SUSPICIOUS_TOKEN_FREQUENCY' THEN 1 END) as SuspiciousRequests,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as FailedRequests,
    MIN(TimeStamp) as FirstRequest,
    MAX(TimeStamp) as LastRequest
FROM SecurityLogs 
WHERE EventType IN ('TOKEN_REQUEST_MONITORED', 'SUSPICIOUS_TOKEN_FREQUENCY')
  AND TimeStamp >= DATEADD(day, -7, GETUTCDATE())
  AND ClientId IS NOT NULL
GROUP BY ClientId
ORDER BY TokenRequests DESC;
```

### 19. Client Usage Frequency

```sql
-- Track how frequently each client is making requests
SELECT 
    ClientId,
    COUNT(*) as TotalRequests,
    COUNT(DISTINCT CAST(TimeStamp AS DATE)) as ActiveDays,
    ROUND(COUNT(*) * 1.0 / COUNT(DISTINCT CAST(TimeStamp AS DATE)), 2) as AvgRequestsPerDay,
    MIN(TimeStamp) as FirstSeen,
    MAX(TimeStamp) as LastSeen,
    DATEDIFF(day, MIN(TimeStamp), MAX(TimeStamp)) + 1 as DaysActive
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(day, -30, GETUTCDATE())
  AND ClientId IS NOT NULL
GROUP BY ClientId
HAVING COUNT(*) > 10  -- Only clients with significant activity
ORDER BY TotalRequests DESC;
```

### 20. Client Error Analysis

```sql
-- Analyze error patterns by client
SELECT 
    ClientId,
    COUNT(*) as TotalRequests,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorRequests,
    ROUND(
        (COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) * 100.0) / COUNT(*), 
        2
    ) as ErrorRate,
    COUNT(CASE WHEN StatusCode = 401 THEN 1 END) as UnauthorizedErrors,
    COUNT(CASE WHEN StatusCode = 403 THEN 1 END) as ForbiddenErrors,
    COUNT(CASE WHEN StatusCode >= 500 THEN 1 END) as ServerErrors
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(day, -7, GETUTCDATE())
  AND ClientId IS NOT NULL
  AND StatusCode IS NOT NULL
GROUP BY ClientId
HAVING COUNT(*) > 10  -- Only clients with significant activity
ORDER BY ErrorRate DESC;
```

## Real-time Monitoring

### 21. Current Activity Dashboard

```sql
-- Real-time view of current activity (last 5 minutes)
SELECT 
    TimeStamp,
    RequestId,
    EventType,
    Method,
    Path,
    StatusCode,
    DurationMs,
    IpAddress,
    ClientId,
    NodeName
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(minute, -5, GETUTCDATE())
ORDER BY TimeStamp DESC;
```

### 22. Live Error Monitoring

```sql
-- Monitor errors in real-time (last 10 minutes)
SELECT 
    TimeStamp,
    RequestId,
    Method,
    Path,
    StatusCode,
    IpAddress,
    ClientId,
    NodeName,
    Message,
    Exception
FROM SecurityLogs 
WHERE StatusCode >= 400
  AND TimeStamp >= DATEADD(minute, -10, GETUTCDATE())
ORDER BY TimeStamp DESC;
```

### 23. Active Sessions Summary

```sql
-- Summary of current activity (last hour)
SELECT 
    COUNT(*) as TotalRequests,
    COUNT(DISTINCT IpAddress) as UniqueIPs,
    COUNT(DISTINCT ClientId) as UniqueClients,
    COUNT(DISTINCT NodeName) as ActiveNodes,
    AVG(DurationMs) as AvgDuration,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorCount,
    COUNT(CASE WHEN DurationMs > 5000 THEN 1 END) as SlowRequests
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(hour, -1, GETUTCDATE());
```

## Stored Procedures

### 24. Request Analysis Stored Procedure

```sql
-- Create a stored procedure for flexible request analysis
CREATE PROCEDURE GetRequestAnalysis
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @ClientId NVARCHAR(100) = NULL,
    @IpAddress NVARCHAR(45) = NULL,
    @MinDuration FLOAT = NULL,
    @StatusCode INT = NULL
AS
BEGIN
    SET @StartDate = ISNULL(@StartDate, DATEADD(day, -1, GETUTCDATE()))
    SET @EndDate = ISNULL(@EndDate, GETUTCDATE())
    
    SELECT 
        TimeStamp,
        RequestId,
        EventType,
        Method,
        Path,
        StatusCode,
        DurationMs,
        IpAddress,
        ClientId,
        NodeName,
        Message
    FROM SecurityLogs 
    WHERE TimeStamp BETWEEN @StartDate AND @EndDate
        AND (@ClientId IS NULL OR ClientId = @ClientId)
        AND (@IpAddress IS NULL OR IpAddress = @IpAddress)
        AND (@MinDuration IS NULL OR DurationMs >= @MinDuration)
        AND (@StatusCode IS NULL OR StatusCode = @StatusCode)
    ORDER BY TimeStamp DESC;
END
```

### 25. Security Alert Stored Procedure

```sql
-- Create a stored procedure for security alerts
CREATE PROCEDURE GetSecurityAlerts
    @HoursBack INT = 1
AS
BEGIN
    DECLARE @StartTime DATETIME2 = DATEADD(hour, -@HoursBack, GETUTCDATE())
    
    -- High error rate IPs
    SELECT 
        'HIGH_ERROR_RATE' as AlertType,
        IpAddress,
        COUNT(*) as TotalRequests,
        COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorRequests,
        ROUND(
            (COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) * 100.0) / COUNT(*), 
            2
        ) as ErrorRate
    FROM SecurityLogs 
    WHERE TimeStamp >= @StartTime
      AND IpAddress IS NOT NULL
    GROUP BY IpAddress
    HAVING COUNT(*) > 20 
       AND (COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) * 100.0) / COUNT(*) > 50
    
    UNION ALL
    
    -- Suspicious token frequency events
    SELECT 
        'SUSPICIOUS_TOKEN_ACTIVITY' as AlertType,
        IpAddress,
        COUNT(*) as SuspiciousEvents,
        NULL as ErrorRequests,
        NULL as ErrorRate
    FROM SecurityLogs 
    WHERE TimeStamp >= @StartTime
      AND EventType IN ('SUSPICIOUS_TOKEN_FREQUENCY', 'HIGH_TOKEN_FREQUENCY')
    GROUP BY IpAddress
    HAVING COUNT(*) > 5
    
    ORDER BY AlertType, TotalRequests DESC;
END
```

## Usage Examples

### Connection Setup

```sql
-- Connect to the SecurityLogs database
USE SimpleIdentityServerSecurityLogs;
```

### Example Query Execution

```sql
-- Example: Find all slow requests from a specific client in the last hour
SELECT 
    TimeStamp,
    RequestId,
    Path,
    DurationMs,
    StatusCode,
    NodeName
FROM SecurityLogs 
WHERE ClientId = 'your-client-id'
  AND DurationMs > 3000
  AND TimeStamp >= DATEADD(hour, -1, GETUTCDATE())
ORDER BY DurationMs DESC;
```

### Using Stored Procedures

```sql
-- Execute the request analysis stored procedure
EXEC GetRequestAnalysis 
    @StartDate = '2024-01-01 00:00:00',
    @EndDate = '2024-01-01 23:59:59',
    @ClientId = 'specific-client-id';

-- Execute security alerts procedure
EXEC GetSecurityAlerts @HoursBack = 24;
```

## Performance Tips

1. **Use Indexes**: Ensure proper indexes are created for TimeStamp, EventType, IpAddress, and ClientId columns
2. **Limit Date Ranges**: Always include TimeStamp filters to avoid scanning the entire table
3. **Use Specific Filters**: Be as specific as possible with WHERE clauses
4. **Consider Pagination**: For large result sets, use TOP or OFFSET/FETCH for pagination
5. **Monitor Query Performance**: Use execution plans to optimize slow queries

## Common Use Cases

- **Debugging**: Use RequestId correlation to trace specific requests
- **Performance Monitoring**: Track slow requests and error rates
- **Security Analysis**: Identify suspicious patterns and potential attacks
- **Capacity Planning**: Analyze traffic patterns and peak usage times
- **Client Support**: Track specific client behavior and issues
- **Compliance**: Generate audit reports and access logs

---

*For more information about the security logging system, see [SERILOG_SECURITY_LOGGING.md](../code/SimpleIdentityServer/SimpleIdentityServer.API/SERILOG_SECURITY_LOGGING.md)*
