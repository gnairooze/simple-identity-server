#!/bin/bash

# Simple Identity Server - Connection Test Script
# This script helps diagnose database connection issues

echo "=== Simple Identity Server Connection Test ==="
echo "Timestamp: $(date)"
echo ""

# Check if production.env exists
if [ ! -f "production.env" ]; then
    echo "❌ ERROR: production.env file not found!"
    echo "Please create production.env file with your configuration."
    exit 1
fi

echo "✅ production.env file found"

# Load environment variables from production.env
export $(grep -v '^#' production.env | xargs)

echo "📋 Environment Variables:"
echo "DB_PASSWORD: ${DB_PASSWORD:0:3}*** (showing first 3 characters)"
echo "DEFAULT_CONNECTION_STRING: ${DEFAULT_CONNECTION_STRING:0:20}..."
echo "SECURITY_LOGS_CONNECTION_STRING: ${SECURITY_LOGS_CONNECTION_STRING:0:20}..."
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ ERROR: Docker is not running or not accessible"
    exit 1
fi

echo "✅ Docker is running"

# Check if containers are running
echo ""
echo "📦 Container Status:"
docker-compose ps

echo ""
echo "🔍 SQL Server Health Check:"
if docker-compose ps sqlserver | grep -q "healthy"; then
    echo "✅ SQL Server is healthy"
    
    # Test SQL Server connection
    echo ""
    echo "🔌 Testing SQL Server Connection:"
    if docker-compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${DB_PASSWORD}" -Q "SELECT 'Connection successful' as Status" -C -h -1; then
        echo "✅ SQL Server connection successful"
    else
        echo "❌ SQL Server connection failed"
        echo "Check if DB_PASSWORD matches the SQL Server SA password"
    fi
    
else
    echo "⚠️  SQL Server is not healthy or not running"
    echo "Check SQL Server logs:"
    echo "docker-compose logs sqlserver"
fi

# Test API instances
echo ""
echo "🌐 Testing API Instances:"

for instance in 1 2 3; do
    container_name="api-instance-${instance}"
    port="808${instance}"
    
    if docker-compose ps ${container_name} | grep -q "Up"; then
        echo "✅ ${container_name} is running"
        
        # Test health endpoint
        if curl -f -k -s "https://localhost:${port}/home/health" > /dev/null; then
            echo "✅ ${container_name} health endpoint responding"
        else
            echo "⚠️  ${container_name} health endpoint not responding"
            echo "   Check logs: docker-compose logs ${container_name}"
        fi
    else
        echo "❌ ${container_name} is not running"
    fi
done

echo ""
echo "🔧 Troubleshooting Commands:"
echo "1. View all logs: docker-compose logs -f"
echo "2. View SQL Server logs: docker-compose logs sqlserver"
echo "3. View API logs: docker-compose logs api-instance-1"
echo "4. Restart services: docker-compose restart"
echo "5. Check environment variables: docker-compose exec api-instance-1 printenv | grep -E '(DB_PASSWORD|CONNECTION_STRING)'"

echo ""
echo "=== Test Complete ==="
