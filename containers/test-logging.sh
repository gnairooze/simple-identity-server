#!/bin/bash

# Test Security Logging Script
# This script tests if the SecurityLogs table is created and logs are being written

echo "=== Security Logging Test ==="
echo "Timestamp: $(date)"
echo ""

# Check if SQL Server is running
if ! docker-compose ps sqlserver | grep -q "healthy"; then
    echo "❌ SQL Server is not healthy. Please start the services first."
    echo "Run: docker-compose up -d"
    exit 1
fi

echo "✅ SQL Server is healthy"

# Load environment variables
export $(grep -v '^#' production.env | xargs)

echo ""
echo "🔍 Checking SecurityLogs database and table..."

# Check if SecurityLogs database exists
DB_CHECK=$(docker-compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${SA_PASSWORD}" -Q "SELECT name FROM sys.databases WHERE name = 'SimpleIdentityServerSecurityLogs'" -C -h -1 2>/dev/null | tr -d '\r\n' | xargs)

if [ "$DB_CHECK" = "SimpleIdentityServerSecurityLogs" ]; then
    echo "✅ SecurityLogs database exists"
else
    echo "❌ SecurityLogs database not found"
    echo "Running database initialization..."
    docker-compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${SA_PASSWORD}" -i /tmp/init-security-logs-db.sql -C
fi

echo ""
echo "🔍 Checking SecurityLogs table structure..."

# Check table structure
docker-compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${SA_PASSWORD}" -d SimpleIdentityServerSecurityLogs -Q "
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'SecurityLogs'
ORDER BY ORDINAL_POSITION
" -C

echo ""
echo "📊 Checking recent log entries..."

# Check recent logs
docker-compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${SA_PASSWORD}" -d SimpleIdentityServerSecurityLogs -Q "
SELECT TOP 10 
    TimeStamp,
    Level,
    Message,
    NodeName,
    EventType
FROM SecurityLogs 
ORDER BY TimeStamp DESC
" -C

echo ""
echo "📈 Log entry count by level..."

# Count logs by level
docker-compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${SA_PASSWORD}" -d SimpleIdentityServerSecurityLogs -Q "
SELECT 
    Level,
    COUNT(*) as Count
FROM SecurityLogs 
GROUP BY Level
ORDER BY Count DESC
" -C

echo ""
echo "=== Test Complete ==="
echo ""
echo "💡 Tips:"
echo "1. If no logs appear, check API container logs: docker-compose logs api-instance-1"
echo "2. Restart API containers to trigger startup logs: docker-compose restart api-instance-1"
echo "3. Make API requests to generate more logs: curl -k https://localhost:8081/home/health"

