# Simple Identity Server - Deployment Guide

This guide explains how to deploy the Simple Identity Server using the secure environment variable configuration.

## 🔐 Security-First Configuration

All sensitive configuration is now centralized in the `production.env` file, following security best practices:

- **No hardcoded credentials** in docker-compose.yml
- **Single source of truth** for environment variables
- **Easy environment management** for different deployments
- **Secure file permissions** to protect sensitive data

## 📋 Pre-Deployment Checklist

### 1. **Review production.env File**
```bash
# Navigate to containers directory
cd containers

# Review the production.env file
cat production.env
```

### 2. **Update Critical Security Settings**

**REQUIRED CHANGES:**
```bash
# 1. Change the database password (CRITICAL)
SIMPLE_IDENTITY_SERVER_DB_PASSWORD=YourSecureProductionPassword123!

# 2. Configure CORS for your domains (CRITICAL)
SIMPLE_IDENTITY_SERVER_CORS_ALLOWED_ORIGINS=https://yourdomain.com;https://app.yourdomain.com

# 3. Update certificate password (RECOMMENDED)
SIMPLE_IDENTITY_SERVER_CERT_PASSWORD=YourSecureCertPassword123!
```

### 3. **Set Secure File Permissions**
```bash
# Restrict access to production.env file
chmod 600 production.env

# Verify permissions (should show -rw-------)
ls -la production.env
```

## 🚀 Deployment Instructions

### Method 1: Standard Deployment
```bash
# 1. Navigate to containers directory
cd containers

# 2. Ensure production.env is properly configured
# 3. Start the environment
docker-compose up -d

# 4. Verify all services are running
docker-compose ps

# 5. Check logs for any issues
docker-compose logs -f
```

### Method 2: Production Deployment with External Secrets
```bash
# For production with external secret management
# Override specific variables as needed

# Example: Using external database
export SIMPLE_IDENTITY_SERVER_DEFAULT_CONNECTION_STRING="Server=prod-db.company.com;Database=SimpleIdentityServer;..."
export SIMPLE_IDENTITY_SERVER_SECURITY_LOGS_CONNECTION_STRING="Server=prod-db.company.com;Database=SecurityLogs;..."

# Start with overrides
docker-compose up -d
```

## 🔍 Verification Steps

### 1. **Health Checks**
```bash
# Check all containers are healthy
docker-compose ps

# Check specific service logs
docker-compose logs api-instance-1
docker-compose logs sqlserver
```

### 2. **API Connectivity**
```bash
# Test direct API access
curl -k https://localhost:8081/home/health
curl -k https://localhost:8082/home/health  
curl -k https://localhost:8083/home/health

# Test load balancer (if configured)
curl -k https://identity.dev.test/home/health
```

### 3. **Database Connectivity**
```bash
# Test database connection
docker exec -it simple-identity-server-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourPassword" -Q "SELECT @@VERSION" -C
```

## 🔧 Configuration Management

### Environment Variables in production.env

| Category | Variable | Required | Description |
|----------|----------|----------|-------------|
| **Database** | `SIMPLE_IDENTITY_SERVER_DB_PASSWORD` | ✅ Yes | SQL Server SA password |
| **Database** | `SIMPLE_IDENTITY_SERVER_DEFAULT_CONNECTION_STRING` | No | Custom main DB connection |
| **Database** | `SIMPLE_IDENTITY_SERVER_SECURITY_LOGS_CONNECTION_STRING` | No | Custom logs DB connection |
| **CORS** | `SIMPLE_IDENTITY_SERVER_CORS_ALLOWED_ORIGINS` | ✅ Yes | Allowed origins (semicolon-separated) |
| **Certificates** | `SIMPLE_IDENTITY_SERVER_CERT_PASSWORD` | No | Certificate password |
| **ASP.NET** | `ASPNETCORE_ENVIRONMENT` | No | Environment name |
| **Load Balancer** | `LOADBALANCER_*` | No | Load balancer settings |

### Environment-Specific Deployments

**Development:**
```bash
# Use development values in production.env
SIMPLE_IDENTITY_SERVER_CORS_ALLOWED_ORIGINS=https://localhost:3000;https://identity.dev.test
SIMPLE_IDENTITY_SERVER_DB_PASSWORD=DevPassword123!
```

**Staging:**
```bash
# Use staging values
SIMPLE_IDENTITY_SERVER_CORS_ALLOWED_ORIGINS=https://staging.yourdomain.com
SIMPLE_IDENTITY_SERVER_DB_PASSWORD=StagingSecurePassword123!
```

**Production:**
```bash
# Use production values
SIMPLE_IDENTITY_SERVER_CORS_ALLOWED_ORIGINS=https://yourdomain.com;https://app.yourdomain.com
SIMPLE_IDENTITY_SERVER_DB_PASSWORD=ProductionSecurePassword123!
```

## 🛡️ Security Best Practices

### 1. **File Security**
```bash
# Set restrictive permissions
chmod 600 production.env
chown root:root production.env  # If running as root

# Never commit production.env to version control
echo "production.env" >> .gitignore
```

### 2. **Password Security**
- Use passwords with **minimum 12 characters**
- Include **uppercase, lowercase, numbers, and symbols**
- **Rotate passwords** regularly
- Use **different passwords** for each environment

### 3. **CORS Security**
- Only include **necessary domains** in SIMPLE_IDENTITY_SERVER_CORS_ALLOWED_ORIGINS
- **Never use wildcards** (*) in production
- Use **HTTPS only** for allowed origins

### 4. **Database Security**
- Use **encrypted connections** (Encrypt=true in connection strings)
- Implement **network isolation** between services
- Enable **SQL Server audit logging**
- Regular **security updates** for SQL Server

## 🚨 Troubleshooting

### Common Issues

**1. Permission Denied on production.env**
```bash
# Fix file permissions
chmod 600 production.env
```

**2. Database Connection Failed / Login Failed for 'sa'**
```bash
# Check if SIMPLE_IDENTITY_SERVER_DB_PASSWORD is set correctly in both SQL Server and API containers
docker-compose exec sqlserver printenv | grep SA_PASSWORD
docker-compose exec api-instance-1 printenv | grep SIMPLE_IDENTITY_SERVER_DB_PASSWORD

# Verify connection strings are properly formatted
docker-compose exec api-instance-1 printenv | grep CONNECTION_STRING

# Test database connectivity directly
docker-compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT 1" -C

# Check if SQL Server is ready (wait for health check)
docker-compose ps sqlserver

# Restart services if password mismatch
docker-compose restart sqlserver
docker-compose restart api-instance-1 api-instance-2 api-instance-3
```

**3. CORS Issues**
```bash
# Verify CORS configuration
docker-compose exec api-instance-1 printenv | grep SIMPLE_IDENTITY_SERVER_CORS_ALLOWED_ORIGINS

# Check API logs for CORS errors
docker-compose logs api-instance-1 | grep -i cors
```

**4. Certificate Issues**
```bash
# Check certificate files exist
docker-compose exec api-instance-1 ls -la /app/certs/

# Verify certificate password
docker-compose exec api-instance-1 printenv | grep SIMPLE_IDENTITY_SERVER_CERT_PASSWORD
```

## 📊 Monitoring and Maintenance

### Log Monitoring
```bash
# Monitor all services
docker-compose logs -f

# Monitor specific service
docker-compose logs -f api-instance-1

# Monitor database
docker-compose logs -f sqlserver
```

### Health Monitoring
```bash
# Check service health
docker-compose ps

# Run health check service
docker-compose logs health-check
```

### Maintenance Tasks
- **Weekly**: Review security logs
- **Monthly**: Rotate passwords
- **Quarterly**: Update certificates
- **As needed**: Scale services, update configurations

## 🔄 Updates and Rollbacks

### Updating Configuration
```bash
# 1. Update production.env
nano production.env

# 2. Restart services to pick up changes
docker-compose restart

# 3. Verify changes
docker-compose logs -f
```

### Rollback Procedure
```bash
# 1. Restore previous production.env
cp production.env.backup production.env

# 2. Restart services
docker-compose restart

# 3. Verify rollback
docker-compose ps
```

## 📞 Support

For issues or questions:
1. Check the logs: `docker-compose logs`
2. Review this deployment guide
3. Check the OWASP security guide: `../docs/owasp-security-guide.md`
4. Review environment variables documentation: `../code/SimpleIdentityServer/SimpleIdentityServer.API/ENVIRONMENT_VARIABLES.md`
