# SimpleIdentityServer.API

A secure, production-ready OpenID Connect identity provider built with ASP.NET Core 8.0 and OpenIddict. This server provides OAuth 2.0 and OpenID Connect authentication and authorization services with comprehensive security monitoring and logging.

## Table of Contents

- [Technology Stack](#technology-stack)
- [Building the Project](#building-the-project)
- [Docker Container Deployment](#docker-container-deployment)
- [Local Development Setup](#local-development-setup)
- [Securing Resource.API](#securing-resourceapi)
- [Managing Configuration with CLI](#managing-configuration-with-cli)
- [Security Logging and Investigation](#security-logging-and-investigation)
- [Project Structure](#project-structure)
- [Contributing](#contributing)

## Technology Stack

### Core Technologies
- **.NET 8.0** - Latest LTS version of .NET
- **ASP.NET Core 8.0** - Web framework for building APIs
- **OpenIddict 5.0** - OpenID Connect and OAuth 2.0 framework
- **Entity Framework Core 8.0** - ORM for database operations
- **SQL Server** - Primary database for identity data and security logs

### Security & Monitoring
- **Serilog** - Structured logging framework
- **Custom Security Middleware** - Rate limiting, suspicious activity detection
- **Field-Level Authorization** - Granular access control
- **Certificate-based Authentication** - X.509 certificate support

### Infrastructure
- **Docker** - Containerization
- **Caddy** - Load balancer and reverse proxy
- **SQL Server 2022** - Database server

### Development Tools
- **Swagger/OpenAPI** - API documentation
- **System.CommandLine** - CLI tooling
- **xUnit** - Testing framework

## Building the Project

### Prerequisites

- **.NET 8.0 SDK** or later
- **SQL Server** (LocalDB, Express, or full instance)
- **Visual Studio 2022** or **Visual Studio Code** (optional)
- **Git** for version control

### Build Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/gnairooze/simple-identity-server.git
   cd simple-identity-server
   ```

2. **Restore NuGet packages:**
   ```bash
   cd code/SimpleIdentityServer
   dotnet restore
   ```

3. **Build the solution:**
   ```bash
   dotnet build SimpleIdentityServer.sln
   ```

4. **Run tests:** still under development
   ```bash
   dotnet test
   ```

### Build Configuration

The project supports multiple build configurations:

- **Debug** - Development with detailed logging
- **Release** - Production optimized build

```bash
# Debug build
dotnet build --configuration Debug

# Release build  
dotnet build --configuration Release
```

## Docker Container Deployment

### Prerequisites

- **Docker Desktop** or **Docker Engine**
- **Docker Compose**

### Quick Start

1. **Navigate to containers directory:**
   ```bash
   cd containers
   ```

2. **Update environment variables:**
   the current production.env file is a working sample file that you can use to update the environment variables. it should work out of the box.

   ```bash
   cp environment-variables.example production.env
   # Edit production.env with your secure values
   ```

4. **Generate SSL certificates (for production):**
   the current nginx/ssl directory is a working sample directory that you can use to update the ssl certificates. it should work out of the box.

   ```bash
   # Create SSL directory
   mkdir -p nginx/ssl
   
   # Generate certificates (replace with your domain)
   openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
     -keyout nginx/ssl/identity-dev-test.key \
     -out nginx/ssl/identity-dev-test.crt \
     -subj "/C=US/ST=State/L=City/O=Organization/CN=identity.dev.test"
   ```

6. **build the containers:**
   ```bash
   docker-compose build
   ```

7. **Start the containers:**
   ```bash
   docker-compose up -d
   ```

8. **test the containers:**
   test the containers by using [this postman collection](docs/SimpleIdentityServer.postman_collection.json)

### Container Architecture

The Docker deployment includes:

- **Load Balancer (Caddy):** `172.25.0.10:80/443`
- **API Instance 1:** `172.25.0.11:443` (exposed on `8081`)
- **API Instance 2:** `172.25.0.12:443` (exposed on `8082`) 
- **API Instance 3:** `172.25.0.13:443` (exposed on `8083`)
- **SQL Server:** `172.25.0.20:1433` (exposed on `1433`)
- **Health Check Service:** `172.25.0.30`

### Environment Configuration

**Required Environment Variables in `production.env`:**

```bash
# Database
SIMPLE_IDENTITY_SERVER_DB_PASSWORD=StrongPassword123!
SA_PASSWORD=StrongPassword123!

# Connection Strings
SIMPLE_IDENTITY_SERVER_DEFAULT_CONNECTION_STRING=Server=sqlserver,1433;Database=SimpleIdentityServer;MultipleActiveResultSets=true;uid=sa;pwd=StrongPassword123!;TrustServerCertificate=true;Encrypt=true

SIMPLE_IDENTITY_SERVER_SECURITY_LOGS_CONNECTION_STRING=Server=sqlserver,1433;Database=SimpleIdentityServerSecurityLogs;MultipleActiveResultSets=true;uid=sa;pwd=StrongPassword123!;TrustServerCertificate=true;Encrypt=true

# CORS
SIMPLE_IDENTITY_SERVER_CORS_ALLOWED_ORIGINS=https://identity.dev.test;https://yourdomain.com

# Certificates
SIMPLE_IDENTITY_SERVER_CERT_PASSWORD=SharedCertPassword123!

# Load Balancer
LoadBalancer__EnableForwardedHeaders=true
LoadBalancer__TrustedProxies__0=172.25.0.10
```

### Health Monitoring

The deployment includes automated health checks:

```bash
# Check individual instances
curl -k https://localhost:8081/home/health  # Instance 1
curl -k https://localhost:8082/home/health  # Instance 2  
curl -k https://localhost:8083/home/health  # Instance 3

# Check load balancer
curl -k https://identity.dev.test/health
```

### Scaling

To add more API instances:

1. **Update docker-compose.yml:**
   ```yaml
   api-instance-4:
     build:
       context: ../code/SimpleIdentityServer/SimpleIdentityServer.API
       dockerfile: Dockerfile
     container_name: simple-identity-server-api-4
     ports:
       - "8084:443"
     env_file:
       - production.env
     environment:
       - SIMPLE_IDENTITY_SERVER_NODE_NAME=api-instance-4
     # ... rest of configuration
   ```

2. **Update Caddy configuration:**
   Add the new instance to the upstream pool in `caddy/Caddyfile`


## Local Development Setup

### 1. Database Setup

**Option A: SQL Server LocalDB (Recommended for development)**

```bash
# LocalDB is included with Visual Studio
# Connection string will be: Server=(localdb)\MSSQLLocalDB;Database=SimpleIdentityServer_Dev;...
```

**Option B: SQL Server Express**

1. Download and install SQL Server Express
2. Update connection strings in `appsettings.json`

### 2. Certificate Setup

For HTTPS development, you'll need certificates:

**Custom Certificates**

1. Create certificates directory:
   ```bash
   mkdir -p code/SimpleIdentityServer/SimpleIdentityServer.API/certs
   ```

2. Generate certificates (example using OpenSSL):
   ```bash
   # Generate private key
   openssl genrsa -out identity-dev-test.key 2048
   
   # Generate certificate
   openssl req -new -x509 -key identity-dev-test.key -out identity-dev-test.crt -days 365
   
   # Convert to PFX for .NET (if needed)
   openssl pkcs12 -export -out signing.pfx -inkey identity-dev-test.key -in identity-dev-test.crt
   ```

### 3. Environment Variables

Create a local environment configuration:

**For Development:**

Set these environment variables or update `appsettings.json`:

```bash
# Database
SIMPLE_IDENTITY_SERVER_DB_PASSWORD=YourLocalPassword123!

# Connection Strings  
SIMPLE_IDENTITY_SERVER_DEFAULT_CONNECTION_STRING=Server=(localdb)\MSSQLLocalDB;Database=SimpleIdentityServer_Dev;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true

SIMPLE_IDENTITY_SERVER_SECURITY_LOGS_CONNECTION_STRING=Server=(localdb)\MSSQLLocalDB;Database=SimpleIdentityServer_SecurityLogs_Dev;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true

# CORS (for local testing)
SIMPLE_IDENTITY_SERVER_CORS_ALLOWED_ORIGINS=https://localhost:3000;https://localhost:5001;https://localhost:7443

# Certificate (if using custom certificates)
SIMPLE_IDENTITY_SERVER_CERT_PASSWORD=YourCertPassword123!
```

**Update appsettings.json for local development:**

```json
{
  "Application": {
    "Development": {
      "DefaultConnectionString": "Server=(localdb)\\MSSQLLocalDB;Database=SimpleIdentityServer_Dev;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true",
      "SecurityLogsConnectionString": "Server=(localdb)\\MSSQLLocalDB;Database=SimpleIdentityServer_SecurityLogs_Dev;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
    }
  }
}
```

### 4. Run the Application

```bash
cd code/SimpleIdentityServer/SimpleIdentityServer.API
dotnet run
```

The application will be available at:
- HTTPS: `https://localhost:7443`
- HTTP: `http://localhost:5000` (redirects to HTTPS)

### 5. Verify Installation

1. **Check health endpoint:**
   ```bash
   curl -k https://localhost:7443/home/health
   ```

2. **Check OpenID configuration:**
   ```bash
   curl -k https://localhost:7443/.well-known/openid-configuration
   ```

3. **Access Swagger UI:**
   Open `https://localhost:7443/swagger` in your browser

## Securing Resource.API

The Resource.API project demonstrates how to secure APIs using the SimpleIdentityServer.API as the identity provider.

### 1. Install Required Packages

The Resource.API already includes these packages:

```xml
<PackageReference Include="OpenIddict.Validation.AspNetCore" Version="5.0.1" />
<PackageReference Include="OpenIddict.Validation.ServerIntegration" Version="5.0.1" />
<PackageReference Include="OpenIddict.Validation.SystemNetHttp" Version="5.0.1" />
```

### 2. Configure Authentication

**In Program.cs:**

```csharp
// Configure authentication to use OpenIddict validation
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

// Configure OpenIddict Validation with introspection (required for encrypted tokens)
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer(builder.Configuration["IdentityServer:Authority"]!);
        options.AddAudiences(builder.Configuration["IdentityServer:Audience"]!);
        
        // Configure the validation handler to use introspection for encrypted tokens
        options.UseIntrospection()
               .SetClientId(builder.Configuration["IdentityServer:ClientId"]!)
               .SetClientSecret(builder.Configuration["IdentityServer:ClientSecret"]!);
        
        // Configure the validation handler to use ASP.NET Core.
        options.UseAspNetCore();
        
        // Configure the validation handler to use System.Net.Http for introspection.
        options.UseSystemNetHttp();
    });

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireApi1Read", policy =>
        policy.RequireClaim("scope", "api1.read"));
    
    options.AddPolicy("RequireApi1Write", policy =>
        policy.RequireClaim("scope", "api1.write"));
});

// Add middleware
app.UseAuthentication();
app.UseAuthorization();
```

### 3. Configure appsettings.json

**In Resource.API/appsettings.json:**

```json
{
  "IdentityServer": {
    "Authority": "https://localhost:7443",
    "Audience": "api1", 
    "IntrospectionEndpoint": "https://localhost:7443/connect/introspect",
    "ClientId": "service-api",
    "ClientSecret": "supersecret"
  }
}
```

**For Docker deployment:**

```json
{
  "IdentityServer": {
    "Authority": "https://identity.dev.test",
    "Audience": "api1",
    "IntrospectionEndpoint": "https://identity.dev.test/connect/introspect", 
    "ClientId": "service-api",
    "ClientSecret": "supersecret"
  }
}
```

### 4. Protect Controllers

**Example protected controller:**

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication
public class WeatherForecastController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "RequireApi1Read")] // Requires api1.read scope
    public IEnumerable<WeatherForecast> Get()
    {
        // Implementation
    }
    
    [HttpPost]
    [Authorize(Policy = "RequireApi1Write")] // Requires api1.write scope  
    public IActionResult Post([FromBody] WeatherForecast forecast)
    {
        // Implementation
    }
}
```

### 5. Database Configuration

**Create OAuth Client in SimpleIdentityServer database:**

Using the CLI tool (see next section) or directly in database:

```bash
# Navigate to CLI project
cd code/SimpleIdentityServer/SimpleIdentityServer.CLI

# Add a client for Resource.API
dotnet run -- app add \
  --client-id "resource-api-client" \
  --client-secret "resource-api-secret" \
  --display-name "Resource API Client" \
  --permissions "ept:introspection" "gt:client_credentials" "scp:api1.read" "scp:api1.write"
```

### 6. Test the Integration

**1. Get an access token:**

```bash
curl -X POST "https://localhost:7443/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=service-api&client_secret=supersecret&scope=api1.read"
```

**2. Use the token to call Resource.API:**

```bash
curl -X GET "https://localhost:5001/api/weatherforecast" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

### 7. Field-Level Authorization (Advanced)

The Resource.API includes field-level authorization middleware:

```csharp
[HttpGet]
[Authorize(Policy = "RequireApi1Read")]
[FieldLevelAuthorization("api1.admin", "SensitiveData")] // Hide SensitiveData unless user has api1.admin scope
public WeatherForecast GetDetailed()
{
    return new WeatherForecast
    {
        Temperature = 25,
        Summary = "Warm",
        SensitiveData = "Only visible with admin scope"
    };
}
```

## Managing Configuration with CLI

The SimpleIdentityServer.CLI tool provides command-line management of OAuth clients and scopes.

### Setup CLI

1. **Navigate to CLI project:**
   ```bash
   cd code/SimpleIdentityServer/SimpleIdentityServer.CLI
   ```

2. **Configure database connection:**
   
   **Edit appsettings.json:**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SimpleIdentityServer_Dev;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
     }
   }
   ```

3. **Build the CLI:**
   ```bash
   dotnet build
   ```

### Application Management

**List all applications:**
```bash
dotnet run -- app list
```

**Get application details:**
```bash
dotnet run -- app get --client-id "service-api"
```

**Add new application:**
```bash
dotnet run -- app add \
  --client-id "new-client" \
  --client-secret "secret123" \
  --display-name "New Client Application" \
  --permissions "ept:token" "ept:introspection" "gt:client_credentials" "scp:openid" "scp:email" "scp:profile" "scp:api1.read"
```

**Update existing application:**
```bash
dotnet run -- app update \
  --client-id "existing-client" \
  --display-name "Updated Name" \
  --permissions "ept:token" "ept:introspection" "gt:client_credentials" "scp:api1.read" "scp:api1.write"
```

**Delete application:**
```bash
dotnet run -- app delete --client-id "client-to-delete"
```

### Scope Management

**List all scopes:**
```bash
dotnet run -- scope list
```

**Get scope details:**
```bash
dotnet run -- scope get --name "api1.read"
```

**Add new scope:**
```bash
dotnet run -- scope add \
  --name "api2.read" \
  --display-name "Read access to API 2" \
  --resources "api2"
```

**Update scope:**
```bash
dotnet run -- scope update \
  --name "api1.read" \
  --display-name "Updated scope description" \
  --resources "api1" "shared"
```

**Delete scope:**
```bash
dotnet run -- scope delete --name "scope-to-delete"
```

### Certificate Management

The CLI tool provides commands to create self-signed certificates for encryption and signing operations used by the identity server.

**Create encryption certificate:**
```bash
dotnet run -- cert create-encryption --path "./certs/encryption.pfx" --password "YourCertPassword123!"
```

**Create signing certificate:**
```bash
dotnet run -- cert create-signing --path "./certs/signing.pfx" --password "YourCertPassword123!"
```

**Using environment variable for password:**
```bash
# Set environment variable
export SIMPLE_IDENTITY_SERVER_CERT_PASSWORD="YourCertPassword123!"

# Create certificates without password parameter
dotnet run -- cert create-encryption --path "./certs/encryption.pfx"
dotnet run -- cert create-signing --path "./certs/signing.pfx"
```

**Certificate command options:**
- `--path` (required): Path where to save the certificate file
- `--password` (optional): Certificate password. If not provided, uses `SIMPLE_IDENTITY_SERVER_CERT_PASSWORD` environment variable

**Certificate features:**
- Creates self-signed X.509 certificates with 2048-bit RSA keys
- Certificates are valid for 2 years from creation date
- Automatically creates directory structure if it doesn't exist
- Supports both PFX format with password protection
- Displays certificate details after creation (subject, thumbprint, validity period)

**Example output:**
```
Creating encryption certificate at: ./certs/encryption.pfx
✅ Encryption certificate created successfully!
   Path: ./certs/encryption.pfx
   Subject: CN=SimpleIdentityServer-Encryption
   Thumbprint: F12F9886D9B99C59209DA17904EDC670593628FC
   Valid from: 9/17/2025 12:56:29 PM
   Valid until: 9/18/2027 12:56:29 PM
```

### Pre-configured Clients

The system comes with these pre-configured clients:

| Client ID | Client Secret | Permissions | Purpose |
|-----------|---------------|-------------|---------|
| `service-api` | `supersecret` | Full API access | Service-to-service communication |
| `web-app` | `webapp-secret` | Read access | Web application client |
| `mobile-app` | `mobile-secret` | Full API access | Mobile application client |

### Permission Reference

**Endpoint Permissions:**
- `ept:token` - Token endpoint access
- `ept:authorization` - Authorization endpoint access
- `ept:introspection` - Token introspection endpoint access
- `ept:revocation` - Token revocation endpoint access

**Grant Type Permissions:**
- `gt:authorization_code` - Authorization code grant
- `gt:client_credentials` - Client credentials grant
- `gt:refresh_token` - Refresh token grant
- `gt:implicit` - Implicit grant

**Scope Permissions:**
- `scp:openid` - OpenID Connect scope
- `scp:email` - Email scope
- `scp:profile` - Profile scope
- `scp:roles` - Roles scope
- `scp:api1.read` - Read access to API1
- `scp:api1.write` - Write access to API1
- `scp:api2.read` - Read access to API2
- `scp:api2.write` - Write access to API2
- `scp:admin` - Administrative access

## Security Logging and Investigation

The SimpleIdentityServer.API includes comprehensive security logging with structured data storage in SQL Server.

### Security Events Logged

The system automatically logs these security events:

1. **TOKEN_REQUEST_MONITORED** - All token requests with client tracking
2. **SUSPICIOUS_TOKEN_FREQUENCY** - High frequency token requests (>10 in 5 min)
3. **HIGH_TOKEN_FREQUENCY** - Very high frequency requests (>100 in 1 hour)
4. **INTROSPECTION_REQUEST** - Token introspection requests
5. **REQUEST_COMPLETED** - All completed requests with duration
6. **REQUEST_EXCEPTION** - Unhandled exceptions with context

### Database Schema

Security logs are stored in a separate database with this structure:

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

### Investigation Queries

**1. View Recent Activity:**
```sql
SELECT 
    TimeStamp,
    EventType,
    Method,
    Path,
    StatusCode,
    DurationMs,
    IpAddress,
    ClientId,
    NodeName
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(hour, -24, GETUTCDATE())
ORDER BY TimeStamp DESC;
```

**2. Find Suspicious Activity:**
```sql
SELECT 
    IpAddress,
    COUNT(*) as RequestCount,
    COUNT(CASE WHEN EventType LIKE '%SUSPICIOUS%' THEN 1 END) as SuspiciousEvents,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorCount
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(hour, -1, GETUTCDATE())
GROUP BY IpAddress
HAVING COUNT(CASE WHEN EventType LIKE '%SUSPICIOUS%' THEN 1 END) > 0
ORDER BY SuspiciousEvents DESC;
```

**3. Monitor Failed Authentication:**
```sql
SELECT 
    IpAddress,
    UserAgent,
    COUNT(*) as FailedAttempts,
    MIN(TimeStamp) as FirstAttempt,
    MAX(TimeStamp) as LastAttempt
FROM SecurityLogs 
WHERE StatusCode = 401  -- Unauthorized
  AND Path LIKE '%/connect/token%'
  AND TimeStamp >= DATEADD(day, -1, GETUTCDATE())
GROUP BY IpAddress, UserAgent
HAVING COUNT(*) > 5
ORDER BY FailedAttempts DESC;
```

**4. Performance Analysis:**
```sql
SELECT 
    Path,
    Method,
    COUNT(*) as RequestCount,
    AVG(DurationMs) as AvgDuration,
    MAX(DurationMs) as MaxDuration,
    COUNT(CASE WHEN DurationMs > 5000 THEN 1 END) as SlowRequests
FROM SecurityLogs 
WHERE DurationMs IS NOT NULL
  AND TimeStamp >= DATEADD(day, -7, GETUTCDATE())
GROUP BY Path, Method
ORDER BY AvgDuration DESC;
```

**5. Client Activity Analysis:**
```sql
SELECT 
    ClientId,
    COUNT(*) as TotalRequests,
    COUNT(DISTINCT IpAddress) as UniqueIPs,
    AVG(DurationMs) as AvgDuration,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorCount,
    MIN(TimeStamp) as FirstSeen,
    MAX(TimeStamp) as LastSeen
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(day, -7, GETUTCDATE())
  AND ClientId IS NOT NULL
GROUP BY ClientId
ORDER BY TotalRequests DESC;
```

**6. Error Rate by Endpoint:**
```sql
SELECT 
    Path,
    Method,
    COUNT(*) as TotalRequests,
    COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) as ErrorRequests,
    ROUND(
        (COUNT(CASE WHEN StatusCode >= 400 THEN 1 END) * 100.0) / COUNT(*), 
        2
    ) as ErrorRate
FROM SecurityLogs 
WHERE TimeStamp >= DATEADD(day, -7, GETUTCDATE())
  AND StatusCode IS NOT NULL
GROUP BY Path, Method
HAVING COUNT(*) > 10
ORDER BY ErrorRate DESC;
```

### Log Retention

- **Automatic Cleanup:** Logs older than 30 days are automatically deleted
- **Cleanup Schedule:** Runs every 24 hours
- **Manual Cleanup:** Can be triggered through configuration

### Connecting to Security Logs Database

**Local Development:**
```bash
Server=(localdb)\MSSQLLocalDB;Database=SimpleIdentityServer_SecurityLogs_Dev;Integrated Security=true;
```

**Docker Deployment:**
```bash
Server=localhost,1433;Database=SimpleIdentityServerSecurityLogs;uid=sa;pwd=StrongPassword123!;TrustServerCertificate=true;
```

### Security Monitoring Best Practices

1. **Regular Review:** Monitor logs daily for suspicious patterns
2. **Automated Alerts:** Set up alerts for high error rates or suspicious activity
3. **Baseline Metrics:** Establish normal patterns to identify anomalies
4. **Incident Response:** Have procedures for responding to security events
5. **Log Retention:** Ensure compliance with regulatory requirements

## Project Structure

```
simple-identity-server/
├── code/
│   └── SimpleIdentityServer/
│       ├── SimpleIdentityServer.API/          # Main identity server
│       │   ├── Authorization/                 # Authorization attributes
│       │   ├── Configuration/                 # Application configuration
│       │   ├── Controllers/                   # API controllers
│       │   ├── Data/                         # Entity Framework context
│       │   ├── Middleware/                   # Custom middleware
│       │   ├── Services/                     # Business logic services
│       │   ├── Utils/                        # Utility classes
│       │   └── Scripts/                      # Database scripts
│       ├── SimpleIdentityServer.CLI/         # Command-line management tool
│       ├── SimpleIdentityServer.API.Test/    # Unit and integration tests
│       ├── Resource.API/                     # Example protected resource
│       └── Client.App/                       # Example client application
├── containers/                               # Docker deployment
│   ├── docker-compose.yml                   # Container orchestration
│   ├── production.env                       # Environment variables
│   ├── caddy/                              # Load balancer configuration
│   └── nginx/ssl/                          # SSL certificates
└── docs/                                    # Documentation
    ├── identity-provider-integration-guide.md
    ├── security-logs-sql-queries.md
    └── postman-testing-guide.md
```

## Contributing

1. **Fork the repository**
2. **Create a feature branch:** `git checkout -b feature/new-feature`
3. **Make changes and test thoroughly**
4. **Commit changes:** `git commit -am 'Add new feature'`
5. **Push to branch:** `git push origin feature/new-feature`
6. **Create Pull Request**

### Development Guidelines

- Follow C# coding standards
- Include unit tests for new features
- Update documentation for API changes
- Ensure all tests pass before submitting PR
- Use meaningful commit messages

---

For more detailed information, see the documentation in the `docs/` directory.
