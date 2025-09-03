# Environment Variables Configuration

This document describes the environment variables required for secure operation of the SimpleIdentityServer API.

## Required Environment Variables (Production)

### Database Connections
- `DEFAULT_CONNECTION_STRING`: Main database connection string
  - Example: `Server=your-db-server;Database=SimpleIdentityServer;User Id=your-user;Password=your-secure-password;TrustServerCertificate=true;Encrypt=true;MultipleActiveResultSets=true`
  
- `SECURITY_LOGS_CONNECTION_STRING`: Security logs database connection string
  - Example: `Server=your-db-server;Database=SimpleIdentityServerSecurityLogs;User Id=your-user;Password=your-secure-password;TrustServerCertificate=true;Encrypt=true;MultipleActiveResultSets=true`

### CORS Configuration
- `CORS_ALLOWED_ORIGINS`: Semicolon-separated list of allowed origins
  - Example: `https://yourdomain.com;https://app.yourdomain.com`

## Optional Environment Variables

### Certificate Management
- `CERT_PASSWORD`: Password for certificate files (default: "DefaultPassword123!")

### Node Identification
- `NODE_NAME`: Identifier for this server instance in load-balanced scenarios (default: machine name)

### ASP.NET Core Configuration
- `ASPNETCORE_ENVIRONMENT`: Environment name (Development, Staging, Production)

### Additional Security
- `ENABLE_DETAILED_SECURITY_LOGS`: Enable additional security logging (true/false)

## Development Fallbacks

In development environment, the application will use:
- Integrated Security for database connections to localhost
- Localhost origins for CORS (ports 3000, 5000, 5001)
- Development certificates for OpenIddict

## Security Notes

1. **Never** commit actual environment variable values to version control
2. Use a secure secret management system in production (Azure Key Vault, AWS Secrets Manager, etc.)
3. Rotate database passwords regularly
4. Ensure connection strings use encrypted connections (`Encrypt=true`)
5. Use strong, unique passwords for all database accounts
6. Limit database user permissions to only what's required

## Docker Configuration

When running in Docker, pass environment variables using:
- Docker Compose environment files
- Kubernetes secrets
- Docker swarm secrets
- Container orchestration platform secret management

Example Docker Compose:
```yaml
services:
  identity-server:
    environment:
      - DEFAULT_CONNECTION_STRING=Server=sqlserver;Database=SimpleIdentityServer;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=true;Encrypt=true
      - SECURITY_LOGS_CONNECTION_STRING=Server=sqlserver;Database=SimpleIdentityServerSecurityLogs;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=true;Encrypt=true
      - CORS_ALLOWED_ORIGINS=https://myapp.com
      - ASPNETCORE_ENVIRONMENT=Production
```
