# SimpleIdentityServer Load Balancer Setup

This Docker Compose configuration sets up the SimpleIdentityServer.API behind an NGINX load balancer for testing and development purposes.

## Architecture

```
Internet/Client
       ↓
   NGINX Load Balancer (172.25.0.10)
       ↓
┌─────────────────────────────────┐
│  Round-robin distribution to:   │
├─────────────────────────────────┤
│  API Instance 1 (172.25.0.11)  │
│  API Instance 2 (172.25.0.12)  │
│  API Instance 3 (172.25.0.13)  │
└─────────────────────────────────┘
       ↓
   SQL Server (172.25.0.20)
```

## Network Configuration

- **Custom Docker Network**: `172.25.0.0/16` with gateway `172.25.0.1`
- **Load Balancer**: `172.25.0.10:80`
- **API Instances**: `172.25.0.11-13:80`
- **SQL Server**: `172.25.0.20:1433`
- **Health Monitor**: `172.25.0.30`

## Services

### NGINX Load Balancer
- **Image**: `nginx:alpine`
- **Ports**: `80:80`, `443:443` (HTTPS commented out for development)
- **Features**:
  - Least connections load balancing
  - Health checks with automatic failover
  - Rate limiting (10 req/s general, 5 req/s for token endpoint)
  - Proper forwarded headers for client IP detection
  - Request/response logging with timing

### API Instances (3x)
- **Image**: Built from `SimpleIdentityServer.API/Dockerfile`
- **Environment**: Production mode with load balancer support
- **Features**:
  - Forwarded headers enabled
  - Trusted proxy configuration
  - Shared SQL Server database
  - Rate limiting and security monitoring

### SQL Server
- **Image**: `mcr.microsoft.com/mssql/server:2022-latest`
- **Database**: SimpleIdentityServer
- **Credentials**: `sa` / `StrongPassword123!`
- **Features**:
  - Health checks
  - Persistent volume
  - Express edition for development

### Health Monitor
- **Image**: `curlimages/curl:latest`
- **Purpose**: Continuous health monitoring of all services
- **Frequency**: Every 60 seconds

## Quick Start

### Prerequisites
- Docker Desktop installed and running
- PowerShell (for provided scripts)

### Start Environment
```powershell
.\start-environment.ps1
```

### Test Load Balancer
```powershell
.\test-load-balancer.ps1
```

### Stop Environment
```powershell
.\stop-environment.ps1
```

## Manual Docker Commands

### Start Services
```bash
docker-compose up --build -d
```

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f nginx-lb
docker-compose logs -f api-instance-1
```

### Stop Services
```bash
docker-compose down -v
```

## Access Points

### Through Load Balancer
- **Base URL**: `http://localhost`
- **OpenID Configuration**: `http://localhost/.well-known/openid-configuration`
- **Token Endpoint**: `http://localhost/connect/token`
- **Introspection**: `http://localhost/connect/introspect`
- **Health Check**: `http://localhost/health`

### Direct API Access (for testing)
- **API Instance 1**: `http://172.25.0.11`
- **API Instance 2**: `http://172.25.0.12`
- **API Instance 3**: `http://172.25.0.13`

### Database
- **Server**: `localhost:1433`
- **Username**: `sa`
- **Password**: `StrongPassword123!`

## Load Balancer Features

### Rate Limiting
- **General API**: 10 requests/second with burst of 20
- **Token Endpoint**: 5 requests/second with burst of 10
- **Response**: HTTP 429 with `Retry-After` header

### Health Checks
- Automatic detection of failed API instances
- 3 failures within 30 seconds triggers removal
- Automatic re-inclusion when health restored

### Forwarded Headers
- Preserves client IP addresses
- Supports `X-Forwarded-For`, `X-Forwarded-Proto`
- Configured trusted proxy networks

### Monitoring
- Request/response timing logs
- Upstream server status
- Connection pooling and keepalive

## Testing Scenarios

### Load Distribution
The test script makes 30 requests and verifies they're distributed across instances.

### Rate Limiting
Rapid requests trigger rate limiting with proper HTTP 429 responses.

### Failover
Stop an API instance to test automatic failover:
```bash
docker stop simple-identity-server-api-1
# Test continues with remaining instances
docker start simple-identity-server-api-1
# Instance automatically rejoins load balancer
```

### Client IP Detection
The API properly detects client IPs through the load balancer for rate limiting and security monitoring.

## Production Considerations

### HTTPS Configuration
Uncomment the HTTPS server block in `nginx/nginx.conf` and provide SSL certificates in `nginx/ssl/`.

### Security
- Change default passwords
- Configure proper trusted proxy IPs
- Enable security headers
- Use production-grade certificates

### Scaling
- Adjust API instance count in `docker-compose.yml`
- Update upstream configuration in `nginx/nginx.conf`
- Monitor resource usage and scale accordingly

### Monitoring
- Implement proper logging aggregation
- Set up health monitoring alerts
- Monitor database performance

## Troubleshooting

### Services Won't Start
```bash
# Check Docker status
docker-compose ps

# View logs
docker-compose logs

# Rebuild images
docker-compose build --no-cache
```

### Load Balancer Not Working
```bash
# Check NGINX configuration
docker exec simple-identity-server-lb nginx -t

# View NGINX logs
docker-compose logs nginx-lb
```

### Database Connection Issues
```bash
# Check SQL Server health
docker exec simple-identity-server-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P StrongPassword123! -Q "SELECT 1"

# View database logs
docker-compose logs sqlserver
```

### Network Issues
```bash
# Inspect network
docker network inspect simple-identity-server_simple-identity-net

# Check container IPs
docker inspect simple-identity-server-lb | grep IPAddress
```

## File Structure

```
.
├── docker-compose.yml              # Main orchestration file
├── nginx/
│   ├── nginx.conf                  # Load balancer configuration
│   ├── proxy_params.conf          # Proxy parameters
│   └── ssl/                       # SSL certificates (for HTTPS)
├── code/SimpleIdentityServer/SimpleIdentityServer.API/
│   ├── Dockerfile                 # API container build
│   └── appsettings.Production.json # Production configuration
├── start-environment.ps1          # Start script
├── stop-environment.ps1           # Stop script
├── test-load-balancer.ps1         # Test script
└── LOAD_BALANCER_SETUP.md         # This documentation
```
