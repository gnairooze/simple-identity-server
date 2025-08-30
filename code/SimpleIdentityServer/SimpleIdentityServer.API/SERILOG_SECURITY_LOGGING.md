# Serilog Security Logging Implementation

This document describes the implementation of Serilog-based security logging with SQL Server storage and 30-day retention for the Simple Identity Server.

## Overview

The security logging system captures all security events mentioned in `SECURITY_FEATURES.md` and stores them in a separate SQL Server database with structured logging using Serilog.

## Features

- **Separate Security Database**: Dedicated SQL Server instance for security logs
- **Structured Logging**: All security events stored with structured data for easy querying
- **30-Day Retention**: Automatic cleanup of logs older than 30 days
- **Load Balancer Support**: Node identification for distributed deployments
- **High Performance**: Batched writes and optimized database schema
- **Dual Logging**: Both console output and SQL Server storage

## Security Events Logged

All events from `SECURITY_FEATURES.md` are captured:

1. **TOKEN_REQUEST_MONITORED** - Normal token requests with client tracking
2. **SUSPICIOUS_TOKEN_FREQUENCY** - High frequency token requests (>10 in 5 min)
3. **HIGH_TOKEN_FREQUENCY** - Very high frequency requests (>100 in 1 hour)
4. **INTROSPECTION_REQUEST** - Token introspection requests
5. **REQUEST_COMPLETED** - All completed requests with duration
6. **REQUEST_EXCEPTION** - Unhandled exceptions with context

## Database Schema

### SecurityLogs Table

The table is automatically created by Serilog with the following structure:

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

### Indexes for Performance

The following indexes are recommended for optimal query performance:

```sql
-- Time-based queries (cleanup, date ranges)
CREATE INDEX IX_SecurityLogs_TimeStamp ON SecurityLogs (TimeStamp DESC);

-- Event type filtering
CREATE INDEX IX_SecurityLogs_EventType ON SecurityLogs (EventType);

-- IP-based analysis
CREATE INDEX IX_SecurityLogs_IpAddress ON SecurityLogs (IpAddress);

-- Client-based analysis  
CREATE INDEX IX_SecurityLogs_ClientId ON SecurityLogs (ClientId);

-- Composite indexes for common queries
CREATE INDEX IX_SecurityLogs_EventType_TimeStamp ON SecurityLogs (EventType, TimeStamp DESC);
CREATE INDEX IX_SecurityLogs_IpAddress_TimeStamp ON SecurityLogs (IpAddress, TimeStamp DESC);
```

## Configuration

### Connection Strings

**Development (appsettings.json):**
```json
{
  "ConnectionStrings": {
    "SecurityLogsConnection": "Server=.,14333;Database=SimpleIdentityServerSecurityLogs;MultipleActiveResultSets=true;uid=SimpleIdentityServerAdmin;pwd=P@ssw0rdP@ssw0rdP@ssw0rd;TrustServerCertificate=true;Encrypt=false"
  }
}
```

**Production (appsettings.Production.json):**
```json
{
  "ConnectionStrings": {
    "SecurityLogsConnection": "Server=security-db,1433;Database=SimpleIdentityServerSecurityLogs;MultipleActiveResultSets=true;uid=sa;pwd=StrongPassword123!;TrustServerCertificate=true;Encrypt=false"
  }
}
```

### Serilog Configuration

The Serilog configuration includes:

- **Console Sink**: For real-time monitoring during development
- **SQL Server Sink**: For persistent structured storage
- **Enrichers**: Machine name, process ID, thread ID, environment name
- **Batch Processing**: Optimized batch sizes (50 for dev, 100 for production)
- **Custom Columns**: All security-specific fields mapped to database columns

## Docker Deployment

### Separate Security Database Container

The `docker-compose.yml` includes a dedicated SQL Server container for security logs:

```yaml
security-db:
  image: mcr.microsoft.com/mssql/server:2022-latest
  container_name: simple-identity-server-security-db
  environment:
    - ACCEPT_EULA=Y
    - SA_PASSWORD=StrongPassword123!
    - MSSQL_PID=Express
  ports:
    - "1434:1433"  # Different port to avoid conflicts
  volumes:
    - security_logs_data:/var/opt/mssql
  networks:
    simple-identity-net:
      ipv4_address: 172.25.0.21
```

### Volume Persistence

Security logs are persisted in a dedicated Docker volume:
```yaml
volumes:
  security_logs_data:
    driver: local
```

## 30-Day Retention

### Automatic Cleanup

A background service runs every 24 hours to clean up logs older than 30 days:

```csharp
// Cleanup runs in Program.cs
static void StartLogCleanupService(WebApplication app)
{
    // Background task that runs every 24 hours
    // Deletes records where TimeStamp < DATEADD(day, -30, GETUTCDATE())
}
```

### Manual Cleanup

A stored procedure is available for manual cleanup:

```sql
EXEC CleanupSecurityLogs @RetentionDays = 30;
```

## Performance Characteristics

### Write Performance
- **Batch Size**: 50 (dev) / 100 (production) records per batch
- **Batch Interval**: 10s (dev) / 5s (production)
- **Asynchronous**: Non-blocking writes to database

### Storage Estimates
- **Average Record Size**: ~2KB per security event
- **Daily Volume**: Varies by traffic (estimated 10K-100K events/day)
- **30-Day Storage**: ~600MB - 6GB for typical workloads

## Querying Security Logs

### Common Queries

**Recent security events by type:**
```sql
SELECT EventType, COUNT(*) as Count, MAX(TimeStamp) as LastOccurrence
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(hour, -24, GETUTCDATE())
GROUP BY EventType
ORDER BY Count DESC;
```

**Suspicious IP activity:**
```sql
SELECT IpAddress, COUNT(*) as RequestCount, 
       COUNT(DISTINCT EventType) as EventTypes,
       MIN(TimeStamp) as FirstSeen,
       MAX(TimeStamp) as LastSeen
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(hour, -1, GETUTCDATE())
  AND EventType IN ('SUSPICIOUS_TOKEN_FREQUENCY', 'HIGH_TOKEN_FREQUENCY')
GROUP BY IpAddress
ORDER BY RequestCount DESC;
```

**Client authentication patterns:**
```sql
SELECT ClientId, COUNT(*) as TokenRequests,
       AVG(DurationMs) as AvgDuration,
       COUNT(CASE WHEN EventType = 'SUSPICIOUS_TOKEN_FREQUENCY' THEN 1 END) as SuspiciousRequests
FROM SecurityLogs 
WHERE EventType IN ('TOKEN_REQUEST_MONITORED', 'SUSPICIOUS_TOKEN_FREQUENCY')
  AND TimeStamp >= DATEADD(day, -1, GETUTCDATE())
  AND ClientId IS NOT NULL
GROUP BY ClientId
ORDER BY TokenRequests DESC;
```

**Performance analysis:**
```sql
SELECT 
    EventType,
    AVG(DurationMs) as AvgDuration,
    MAX(DurationMs) as MaxDuration,
    COUNT(CASE WHEN DurationMs > 5000 THEN 1 END) as SlowRequests,
    COUNT(*) as TotalRequests
FROM SecurityLogs 
WHERE DurationMs IS NOT NULL
  AND TimeStamp >= DATEADD(day, -7, GETUTCDATE())
GROUP BY EventType
ORDER BY AvgDuration DESC;
```

## Monitoring and Alerting

### Log Levels
- **Information**: Normal security events
- **Warning**: Slow requests (>5 seconds), suspicious activity
- **Error**: Exceptions and security incidents

### Recommended Alerts
1. **High error rate**: >10 errors per minute
2. **Suspicious activity spikes**: >50 SUSPICIOUS_TOKEN_FREQUENCY events per hour
3. **Database connectivity**: Security log write failures
4. **Storage growth**: Unexpected database size increases

## Security Considerations

### Database Security
- **Separate Database**: Isolated from application data
- **Connection Security**: Encrypted connections in production
- **Access Control**: Dedicated service account with minimal permissions
- **Network Isolation**: Database only accessible from application containers

### Data Protection
- **No Sensitive Data**: Passwords, tokens, or PII are never logged
- **IP Address Handling**: Proper handling of forwarded headers
- **Retention Compliance**: Automatic 30-day cleanup for compliance

### Audit Trail
- **Immutable Logs**: No update/delete operations on individual records
- **Correlation IDs**: Every request has a unique RequestId for tracing
- **Node Identification**: Multi-instance deployments tracked by NodeName

## Troubleshooting

### Common Issues

**Serilog not writing to SQL Server:**
1. Check connection string configuration
2. Verify security database is running
3. Check application logs for Serilog errors
4. Ensure database has sufficient disk space

**High database growth:**
1. Verify cleanup service is running
2. Check for unusual traffic patterns
3. Consider adjusting retention period
4. Monitor batch processing efficiency

**Missing security events:**
1. Verify SecurityMonitoringMiddleware is registered
2. Check log level configuration
3. Ensure Serilog is properly initialized
4. Verify database connectivity

### Diagnostic Queries

**Check recent log activity:**
```sql
SELECT TOP 10 TimeStamp, EventType, Message, NodeName
FROM SecurityLogs 
ORDER BY TimeStamp DESC;
```

**Verify cleanup is working:**
```sql
SELECT 
    MIN(TimeStamp) as OldestLog,
    MAX(TimeStamp) as NewestLog,
    COUNT(*) as TotalRecords,
    DATEDIFF(day, MIN(TimeStamp), GETUTCDATE()) as RetentionDays
FROM SecurityLogs;
```

## Maintenance

### Regular Tasks
1. **Monitor database size** and growth patterns
2. **Review security event patterns** for anomalies  
3. **Verify cleanup service** is running properly
4. **Update indexes** as query patterns evolve
5. **Archive old data** if longer retention is needed

### Scaling Considerations
For high-traffic scenarios:
- Consider **partitioned tables** by date
- Implement **distributed logging** with centralized aggregation
- Use **read replicas** for analytics queries
- Consider **data archival** to cheaper storage tiers

## Integration with SIEM

The structured SQL Server logs can be easily integrated with Security Information and Event Management (SIEM) systems:

- **Direct SQL integration** for real-time monitoring
- **Export capabilities** for data feeds
- **Standardized schema** for consistent parsing
- **Rich metadata** for correlation analysis
