# SimpleIdentityServer CLI

A command-line interface for managing OpenIddict applications and scopes in the Simple Identity Server.

## Features

- **Application Management**: Create, read, update, and delete OpenIddict applications
- **Scope Management**: Create, read, update, and delete OpenIddict scopes
- **Database Integration**: Direct integration with the OpenIddict database
- **Easy to Use**: Simple command-line interface with clear commands and options

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Access to the Simple Identity Server database

## Configuration

Update the `appsettings.json` file with your database connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=your-database;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

## Usage

### Application Commands

#### List all applications
```bash
dotnet run -- app list
```

#### Get application details
```bash
dotnet run -- app get --client-id "your-client-id"
```

#### Add new application
```bash
dotnet run -- app add --client-id "new-client" --client-secret "secret123" --display-name "New Client" --permissions "ept:token" "ept:introspection" "gt:client_credentials" "scp:openid" "scp:email" "scp:profile" "scp:api1.read"
```

#### Update application
```bash
dotnet run -- app update --client-id "existing-client" --display-name "Updated Name" --permissions "ept:token" "ept:introspection" "gt:client_credentials" "scp:email" "scp:profile" "scp:api1.read"
```

#### Delete application
```bash
dotnet run -- app delete --client-id "client-to-delete"
```

### Scope Commands

#### List all scopes
```bash
dotnet run -- scope list
```

#### Get scope details
```bash
dotnet run -- scope get --name "scope-name"
```

#### Add new scope
```bash
dotnet run -- scope add --name "new-scope" --display-name "New Scope" --resources "api1" "api2"
```

#### Update scope
```bash
dotnet run -- scope update --name "existing-scope" --display-name "Updated Scope" --resources "api1" "api3"
```

#### Delete scope
```bash
dotnet run -- scope delete --name "scope-to-delete"
```

## Common Permissions

**Note**: Use the full OpenIddict permission constants when creating applications. The examples below show the actual string values.

### Endpoint Permissions
- `ept:token` - Token endpoint (OpenIddictConstants.Permissions.Endpoints.Token)
- `ept:authorization` - Authorization endpoint (OpenIddictConstants.Permissions.Endpoints.Authorization)
- `ept:introspection` - Token introspection endpoint (OpenIddictConstants.Permissions.Endpoints.Introspection)
- `ept:revocation` - Token revocation endpoint (OpenIddictConstants.Permissions.Endpoints.Revocation)
- `ept:logout` - Logout endpoint (OpenIddictConstants.Permissions.Endpoints.Logout)

### Grant Type Permissions
- `gt:authorization_code` - Authorization code grant (OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode)
- `gt:client_credentials` - Client credentials grant (OpenIddictConstants.Permissions.GrantTypes.ClientCredentials)
- `gt:refresh_token` - Refresh token grant (OpenIddictConstants.Permissions.GrantTypes.RefreshToken)
- `gt:implicit` - Implicit grant (OpenIddictConstants.Permissions.GrantTypes.Implicit)
- `gt:password` - Password grant (OpenIddictConstants.Permissions.GrantTypes.Password)

### Response Type Permissions
- `rst:code` - Authorization code response type (OpenIddictConstants.Permissions.ResponseTypes.Code)
- `rst:token` - Token response type (OpenIddictConstants.Permissions.ResponseTypes.Token)
- `rst:id_token` - ID token response type (OpenIddictConstants.Permissions.ResponseTypes.IdToken)

### Standard Scope Permissions
- `scp:openid` - OpenID Connect scope (OpenIddictConstants.Permissions.Scopes.OpenId)
- `scp:email` - Email scope (OpenIddictConstants.Permissions.Scopes.Email)
- `scp:profile` - Profile scope (OpenIddictConstants.Permissions.Scopes.Profile)
- `scp:roles` - Roles scope (OpenIddictConstants.Permissions.Scopes.Roles)

### Custom Scope Permissions (defined in this project)
- `scp:api1.read` - Read access to API1
- `scp:api1.write` - Write access to API1
- `scp:api2.read` - Read access to API2
- `scp:api2.write` - Write access to API2
- `scp:admin` - Administrative access

## Existing Applications and Scopes

The Simple Identity Server comes pre-configured with the following applications and scopes:

### Pre-configured Applications
- **service-api**: Service API Client with full API access
  - Client Secret: `supersecret`
  - Permissions: Token, Introspection, Client Credentials, Email, Profile, Roles, api1.read, api1.write
- **web-app**: Web Application Client with read access
  - Client Secret: `webapp-secret` 
  - Permissions: Token, Introspection, Client Credentials, Email, Profile, Roles, api1.read
- **mobile-app**: Mobile Application Client with full API access
  - Client Secret: `mobile-secret`
  - Permissions: Token, Introspection, Client Credentials, Email, Profile, Roles, api1.read, api1.write

### Pre-configured Scopes
- **api1.read**: Read access to API 1 (Resource: api1)
- **api1.write**: Write access to API 1 (Resource: api1)  
- **api2.read**: Read access to API 2 (Resource: api2)
- **api2.write**: Write access to API 2 (Resource: api2)
- **admin**: Administrative access (Resource: admin-api)

## Quick Start

### View existing applications and scopes
```bash
# List all applications
dotnet run -- app list

# List all scopes  
dotnet run -- scope list

# Get details of specific application
dotnet run -- app get --client-id "service-api"

# Get details of specific scope
dotnet run -- scope get --name "api1.read"
```

## Examples

### Create a new web application client (similar to existing web-app)
```bash
dotnet run -- app add --client-id "web-app-v2" --client-secret "web-secret-v2" --display-name "Web Application v2" --permissions "ept:token" "ept:introspection" "gt:client_credentials" "scp:email" "scp:profile" "scp:roles" "scp:api1.read"
```

### Create a service API client (similar to existing service-api)
```bash
dotnet run -- app add --client-id "service-api-v2" --client-secret "service-secret-v2" --display-name "Service API Client v2" --permissions "ept:token" "ept:introspection" "gt:client_credentials" "scp:email" "scp:profile" "scp:roles" "scp:api1.read" "scp:api1.write"
```

### Create a new API scope (matching project pattern)
```bash
dotnet run -- scope add --name "api3.read" --display-name "Read access to API 3" --resources "api3"
```

### Create an admin scope
```bash
dotnet run -- scope add --name "admin.full" --display-name "Full administrative access" --resources "admin-api"
```

### Update an existing client's permissions (mobile-app example)
```bash
dotnet run -- app update --client-id "mobile-app" --permissions "ept:token" "ept:introspection" "gt:client_credentials" "scp:email" "scp:profile" "scp:roles" "scp:api1.read" "scp:api1.write"
```

## Error Handling

The CLI provides clear error messages for common issues:
- Database connection failures
- Missing applications or scopes
- Invalid permissions or scopes
- Duplicate client IDs or scope names

## Building and Running

### Build the project
```bash
dotnet build
```

### Run the CLI
```bash
dotnet run
```

### Run with specific command
```bash
dotnet run -- app list
```

## Troubleshooting

### Database Connection Issues
- Verify the connection string in `appsettings.json`
- Ensure the database server is running
- Check that the database exists and is accessible

### Permission Issues
- Verify that the permissions follow OpenIddict naming conventions
- Check that the database schema is properly initialized
- Ensure OpenIddict is properly configured in the database

### Build Issues
- Ensure .NET 8.0 SDK is installed
- Restore NuGet packages: `dotnet restore`
- Clean and rebuild: `dotnet clean && dotnet build`
