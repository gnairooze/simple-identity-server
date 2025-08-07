# Simple Identity Server

A .NET 8.0 Web API identity server built with OpenIddict, supporting OAuth2 Client Credentials flow for service-to-service authentication.

## Features

- **OAuth2 Client Credentials Flow**: Secure service-to-service authentication
- **OpenIddict Integration**: Modern, flexible OAuth2/OpenID Connect server
- **Entity Framework Core**: SQL Server database storage
- **JWT Tokens**: Signed and encrypted access tokens
- **Scope-based Authorization**: Fine-grained access control
- **Swagger Documentation**: Interactive API documentation
- **Docker Support**: Containerized deployment

## Technology Stack

- ASP.NET Core 8.0
- OpenIddict 5.0
- Entity Framework Core 8.0
- SQL Server
- Docker

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB, SQL Server Express, or full SQL Server)
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd simple-identity/code
   ```

2. **Update the connection string**
   Edit `appsettings.json` and update the `DefaultConnection` string to point to your SQL Server instance.

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Access the application**
   - API: https://localhost:7443
   - Swagger UI: https://localhost:7443/swagger
   - Health Check: https://localhost:7443/home/health

## API Endpoints

### OAuth2 Endpoints

- **POST /connect/token** - OAuth2 token endpoint for client credentials flow
- **POST /connect/introspect** - Token introspection endpoint
- **GET /.well-known/openid-configuration** - OpenID Connect discovery document
- **GET /.well-known/jwks** - JSON Web Key Set

### Management Endpoints

- **GET /api/client** - List all registered clients
- **GET /api/client/{clientId}** - Get specific client details
- **GET /api/scope** - List all available scopes
- **GET /api/scope/{scopeName}** - Get specific scope details

## Client Credentials Flow

### 1. Request Access Token

```bash
curl -X POST https://localhost:7443/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=service-api&client_secret=supersecret&scope=api1.read api1.write"
```

### 2. Response

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires_in": 3600,
  "token_type": "Bearer",
  "scope": "api1.read api1.write"
}
```

### 3. Use Access Token

```bash
curl -X GET https://your-api.com/protected-resource \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### 4. Introspect Token (Optional)

Resource servers can validate tokens using the introspection endpoint:

```bash
curl -X POST https://localhost:7443/connect/introspect \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=service-api&client_secret=supersecret&token=eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response for valid token:**
```json
{
  "active": true,
  "scope": "api1.read api1.write",
  "client_id": "service-api",
  "username": "Service API",
  "token_type": "Bearer",
  "exp": 1703123456,
  "iat": 1703119856,
  "custom_claims": {
    "role": "service",
    "custom_claim": "service_access"
  },
  "aud": ["https://api.example.com"]
}
```

**Response for invalid/expired token:**
```json
{
  "active": false
}
```

## Pre-configured Clients

The server comes with three pre-configured clients:

### 1. Service API Client
- **Client ID**: `service-api`
- **Client Secret**: `supersecret`
- **Scopes**: `api1.read`, `api1.write`
- **Role**: `service`
- **Custom Claim**: `service_access`

### 2. Web Application Client
- **Client ID**: `web-app`
- **Client Secret**: `webapp-secret`
- **Scopes**: `api1.read`
- **Role**: `web_user`
- **Custom Claim**: `web_access`

### 3. Mobile Application Client
- **Client ID**: `mobile-app`
- **Client Secret**: `mobile-secret`
- **Scopes**: `api1.read`, `api1.write`
- **Role**: `mobile_user`
- **Custom Claim**: `mobile_access`

## Available Scopes

- `api1.read` - Read access to API 1
- `api1.write` - Write access to API 1
- `api2.read` - Read access to API 2
- `api2.write` - Write access to API 2
- `admin` - Administrative access

## Token Claims

Access tokens include the following claims:

- `sub` - Subject (client ID)
- `name` - Client display name
- `client_id` - Client identifier
- `role` - Client role
- `custom_claim` - Custom access claim
- `scope` - Granted scopes

## Security Features

- **JWT Signing**: Tokens are signed using RSA keys
- **Scope Validation**: Fine-grained access control
- **Client Authentication**: Secure client credential validation
- **Token Lifetime**: Configurable token expiration
- **Audit Logging**: Comprehensive security logging

## Configuration

### Database Connection

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=SimpleIdentityServer;Trusted_Connection=true;"
  }
}
```

### Token Configuration

Token settings can be modified in `Program.cs`:

```csharp
.SetAccessTokenLifetime(TimeSpan.FromHours(1))
.SetRefreshTokenLifetime(TimeSpan.FromDays(14))
```

## Docker Deployment

### Build the Image

```bash
docker build -t simple-identity-server .
```

### Run the Container

```bash
docker run -p 7001:7001 -e "ConnectionStrings__DefaultConnection=your-connection-string" simple-identity-server
```

## Development

### Adding New Clients

1. Modify `ClientService.SeedClientsAsync()` in `Services/ClientService.cs`
2. Add new client configuration
3. Restart the application

### Adding New Scopes

1. Modify `ScopeService.SeedScopesAsync()` in `Services/ScopeService.cs`
2. Add new scope configuration
3. Restart the application

### Custom Claims

To add custom claims, modify the `TokenController.Token()` method in `Controllers/TokenController.cs`.

## Troubleshooting

### Common Issues

1. **Database Connection**: Ensure SQL Server is running and the connection string is correct
2. **Port Conflicts**: Change the port in `launchSettings.json` if needed
3. **SSL Certificate**: For production, configure proper SSL certificates

### Logs

Check the application logs for detailed error information:

```bash
dotnet run --environment Development
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License. 