# OpenIddict Database Schema Documentation

This document provides a comprehensive overview of the OpenIddict database tables and their columns used in the Simple Identity Server project.

## Table of Contents

1. [Overview](#overview)
2. [Database Configuration](#database-configuration)
3. [Core Tables](#core-tables)
4. [Table Relationships](#table-relationships)
5. [Column Descriptions](#column-descriptions)
6. [Data Types and Constraints](#data-types-and-constraints)
7. [Indexes and Performance](#indexes-and-performance)
8. [Sample Data Examples](#sample-data-examples)
9. [Database Maintenance](#database-maintenance)

## Overview

OpenIddict uses Entity Framework Core to manage its database schema. The framework creates several tables to store OAuth 2.0 and OpenID Connect related data including applications (clients), authorizations, scopes, and tokens.

### Database Provider
- **Provider**: SQL Server
- **Connection**: `Server=.,14333;Database=SimpleIdentityServer;...`
- **Framework**: Entity Framework Core 8.0
- **OpenIddict Version**: 5.0.0

## Database Configuration

The database is configured in the `ApplicationDbContext` class:

```csharp
public class ApplicationDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.UseOpenIddict(); // Configures OpenIddict entities
    }
}
```

## Core Tables

OpenIddict creates the following main tables in the database:

### 1. OpenIddictApplications
**Purpose**: Stores OAuth 2.0/OpenID Connect client applications

| Column Name | Data Type | Nullable | Description |
|-------------|-----------|----------|-------------|
| `Id` | nvarchar(450) | No | Primary key, unique identifier for the application |
| `ApplicationType` | nvarchar(50) | Yes | Type of application (web, native, etc.) |
| `ClientId` | nvarchar(100) | Yes | Public client identifier |
| `ClientSecret` | nvarchar(max) | Yes | Encrypted client secret |
| `ClientType` | nvarchar(50) | Yes | Client type (confidential, public) |
| `ConcurrencyToken` | nvarchar(50) | Yes | Concurrency token for optimistic locking |
| `ConsentType` | nvarchar(50) | Yes | Consent type (explicit, implicit, external, systematic) |
| `DisplayName` | nvarchar(max) | Yes | Human-readable application name |
| `DisplayNames` | nvarchar(max) | Yes | Localized display names (JSON) |
| `JsonWebKeySet` | nvarchar(max) | Yes | JSON Web Key Set for the application |
| `Permissions` | nvarchar(max) | Yes | JSON array of granted permissions |
| `PostLogoutRedirectUris` | nvarchar(max) | Yes | JSON array of post-logout redirect URIs |
| `Properties` | nvarchar(max) | Yes | Custom properties (JSON) |
| `RedirectUris` | nvarchar(max) | Yes | JSON array of redirect URIs |
| `Requirements` | nvarchar(max) | Yes | JSON array of requirements |
| `Settings` | nvarchar(max) | Yes | Application settings (JSON) |

### 2. OpenIddictAuthorizations
**Purpose**: Stores authorization grants issued to applications

| Column Name | Data Type | Nullable | Description |
|-------------|-----------|----------|-------------|
| `Id` | nvarchar(450) | No | Primary key, unique identifier |
| `ApplicationId` | nvarchar(450) | Yes | Foreign key to OpenIddictApplications |
| `ConcurrencyToken` | nvarchar(50) | Yes | Concurrency token |
| `CreationDate` | datetime2(7) | Yes | When the authorization was created |
| `Properties` | nvarchar(max) | Yes | Custom properties (JSON) |
| `Scopes` | nvarchar(max) | Yes | JSON array of authorized scopes |
| `Status` | nvarchar(50) | Yes | Authorization status (valid, invalid, revoked) |
| `Subject` | nvarchar(400) | Yes | Subject identifier (user ID) |
| `Type` | nvarchar(50) | Yes | Authorization type (ad-hoc, permanent) |

### 3. OpenIddictScopes
**Purpose**: Stores OAuth 2.0 scopes and OpenID Connect claims

| Column Name | Data Type | Nullable | Description |
|-------------|-----------|----------|-------------|
| `Id` | nvarchar(450) | No | Primary key, unique identifier |
| `ConcurrencyToken` | nvarchar(50) | Yes | Concurrency token |
| `Description` | nvarchar(max) | Yes | Human-readable scope description |
| `Descriptions` | nvarchar(max) | Yes | Localized descriptions (JSON) |
| `DisplayName` | nvarchar(max) | Yes | Human-readable scope name |
| `DisplayNames` | nvarchar(max) | Yes | Localized display names (JSON) |
| `Name` | nvarchar(200) | Yes | Scope name (unique) |
| `Properties` | nvarchar(max) | Yes | Custom properties (JSON) |
| `Resources` | nvarchar(max) | Yes | JSON array of associated resources |

### 4. OpenIddictTokens
**Purpose**: Stores access tokens, refresh tokens, and authorization codes

| Column Name | Data Type | Nullable | Description |
|-------------|-----------|----------|-------------|
| `Id` | nvarchar(450) | No | Primary key, unique identifier |
| `ApplicationId` | nvarchar(450) | Yes | Foreign key to OpenIddictApplications |
| `AuthorizationId` | nvarchar(450) | Yes | Foreign key to OpenIddictAuthorizations |
| `ConcurrencyToken` | nvarchar(50) | Yes | Concurrency token |
| `CreationDate` | datetime2(7) | Yes | Token creation timestamp |
| `ExpirationDate` | datetime2(7) | Yes | Token expiration timestamp |
| `Payload` | nvarchar(max) | Yes | Encrypted token payload |
| `Properties` | nvarchar(max) | Yes | Custom properties (JSON) |
| `RedemptionDate` | datetime2(7) | Yes | When token was redeemed/used |
| `ReferenceId` | nvarchar(100) | Yes | Reference identifier for the token |
| `Status` | nvarchar(50) | Yes | Token status (valid, invalid, revoked, redeemed) |
| `Subject` | nvarchar(400) | Yes | Subject identifier (user ID) |
| `Type` | nvarchar(50) | Yes | Token type (access_token, refresh_token, authorization_code) |

## Table Relationships

```
OpenIddictApplications (1) ←→ (M) OpenIddictAuthorizations
OpenIddictApplications (1) ←→ (M) OpenIddictTokens
OpenIddictAuthorizations (1) ←→ (M) OpenIddictTokens
```

### Foreign Key Relationships

1. **OpenIddictAuthorizations.ApplicationId** → **OpenIddictApplications.Id**
2. **OpenIddictTokens.ApplicationId** → **OpenIddictApplications.Id**
3. **OpenIddictTokens.AuthorizationId** → **OpenIddictAuthorizations.Id**

## Column Descriptions

### Key Concepts

#### Application Types
- **`web`**: Server-side web applications
- **`native`**: Mobile/desktop applications
- **`spa`**: Single-page applications

#### Client Types
- **`confidential`**: Can securely store credentials (server-side apps)
- **`public`**: Cannot securely store credentials (mobile/SPA apps)

#### Consent Types
- **`explicit`**: User must explicitly consent
- **`implicit`**: Consent is implied
- **`external`**: Consent handled externally
- **`systematic`**: No consent required

#### Token Types
- **`access_token`**: Bearer token for API access
- **`refresh_token`**: Token to refresh access tokens
- **`authorization_code`**: Temporary code in authorization code flow
- **`id_token`**: Identity token in OpenID Connect

#### Token Status
- **`valid`**: Token is active and usable
- **`invalid`**: Token is malformed or corrupted
- **`revoked`**: Token has been explicitly revoked
- **`redeemed`**: Token has been used (for one-time tokens)

#### Permission Prefixes
OpenIddict uses prefixes to categorize different types of permissions:

- **`oidc:`** - OpenID Connect endpoints (e.g., `oidc:token`, `oidc:introspection`)
- **`gt:`** - Grant types (e.g., `gt:client_credentials`, `gt:authorization_code`)
- **`scp:`** - Standard OpenID Connect scopes (e.g., `scp:openid`, `scp:profile`, `scp:email`)
- **No prefix** - Custom API scopes (e.g., `api1.read`, `api1.write`, `admin`)

**Important Note**: Custom API scopes (like `api1.read`, `api1.write`) are stored **without** the `scp:` prefix. Only standard OpenID Connect scopes use the `scp:` prefix. This is why you see `"api1.read"` instead of `"scp:api1.read"` in your implementation.

## Data Types and Constraints

### Primary Keys
All primary keys use `nvarchar(450)` to support:
- GUID-based identifiers
- Custom string identifiers
- Efficient indexing in SQL Server

### JSON Columns
Several columns store JSON data for flexibility:
- **Permissions**: `["oidc:token", "oidc:introspection", "gt:client_credentials", "scp:openid", "scp:profile", "api1.read", "api1.write"]`
- **Scopes**: `["api1.read", "api1.write", "openid", "profile"]`
- **Properties**: `{"custom_claim": "value", "tenant_id": "123"}`

### Timestamps
All datetime columns use `datetime2(7)` for:
- High precision (100 nanoseconds)
- UTC timezone handling
- Better performance than `datetime`

## Indexes and Performance

### Automatic Indexes
OpenIddict creates indexes for:
- Primary keys (clustered)
- Foreign key relationships
- Frequently queried columns

### Recommended Additional Indexes
For better performance in production:

```sql
-- Index on ClientId for application lookups
CREATE NONCLUSTERED INDEX IX_OpenIddictApplications_ClientId 
ON OpenIddictApplications (ClientId);

-- Index on token expiration for cleanup
CREATE NONCLUSTERED INDEX IX_OpenIddictTokens_ExpirationDate 
ON OpenIddictTokens (ExpirationDate);

-- Index on token status for active token queries
CREATE NONCLUSTERED INDEX IX_OpenIddictTokens_Status 
ON OpenIddictTokens (Status);

-- Composite index for token lookups
CREATE NONCLUSTERED INDEX IX_OpenIddictTokens_App_Subject_Type 
ON OpenIddictTokens (ApplicationId, Subject, Type);
```

## Sample Data Examples

### Application Record
```json
{
  "Id": "12345678-1234-1234-1234-123456789012",
  "ClientId": "service-api",
  "DisplayName": "Service API Client",
  "ClientType": "confidential",
  "ConsentType": "implicit",
  "Permissions": [
    "oidc:token",
    "oidc:introspection", 
    "gt:client_credentials",
    "scp:email",
    "scp:profile",
    "scp:roles",
    "api1.read",
    "api1.write"
  ]
}
```

### Token Record
```json
{
  "Id": "87654321-4321-4321-4321-210987654321",
  "ApplicationId": "12345678-1234-1234-1234-123456789012",
  "Type": "access_token",
  "Status": "valid",
  "Subject": "service-api",
  "CreationDate": "2024-01-15T10:30:00.0000000Z",
  "ExpirationDate": "2024-01-15T11:30:00.0000000Z",
  "Payload": "eyJhbGciOiJSUzI1NiIs..."
}
```

### Scope Record
```json
{
  "Id": "11111111-1111-1111-1111-111111111111",
  "Name": "api1.read",
  "DisplayName": "Read access to API 1",
  "Description": "Allows read-only access to API 1 resources",
  "Resources": ["api1"]
}
```

## Database Maintenance

### Token Cleanup
OpenIddict automatically handles token cleanup, but you can manually clean expired tokens:

```sql
-- Remove expired tokens (be careful with this in production)
DELETE FROM OpenIddictTokens 
WHERE ExpirationDate < GETUTCDATE() 
  AND Status IN ('invalid', 'revoked', 'redeemed');
```

### Authorization Cleanup
Remove orphaned authorizations:

```sql
-- Remove authorizations without valid tokens
DELETE FROM OpenIddictAuthorizations 
WHERE Id NOT IN (
    SELECT DISTINCT AuthorizationId 
    FROM OpenIddictTokens 
    WHERE AuthorizationId IS NOT NULL 
      AND Status = 'valid'
);
```

### Database Size Monitoring
Monitor table sizes for capacity planning:

```sql
SELECT 
    t.name AS TableName,
    s.name AS SchemaName,
    p.rows AS RowCount,
    CAST(ROUND(((SUM(a.total_pages) * 8) / 1024.00), 2) AS NUMERIC(36, 2)) AS TotalSpaceMB
FROM sys.tables t
INNER JOIN sys.indexes i ON t.object_id = i.object_id
INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.name LIKE 'OpenIddict%'
GROUP BY t.name, s.name, p.rows
ORDER BY TotalSpaceMB DESC;
```

## Configuration Examples

### Entity Framework Configuration
```csharp
// In ApplicationDbContext
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);
    
    // Configure OpenIddict entities
    builder.UseOpenIddict();
    
    // Custom configurations
    builder.Entity<OpenIddictApplication>()
        .HasIndex(x => x.ClientId)
        .IsUnique();
}
```

### Connection String Examples
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=IdentityServer;Trusted_Connection=true;TrustServerCertificate=true;",
    "SqlServerExpress": "Server=.\\SQLEXPRESS;Database=IdentityServer;Integrated Security=true;TrustServerCertificate=true;",
    "AzureSql": "Server=tcp:myserver.database.windows.net,1433;Database=IdentityServer;User ID=myuser;Password=mypassword;Encrypt=True;"
  }
}
```

## Security Considerations

### Sensitive Data
The following columns contain sensitive information and should be protected:
- **`ClientSecret`**: Encrypted client credentials
- **`Payload`**: Encrypted token data
- **`Properties`**: May contain sensitive custom data

### Access Control
Implement proper database access controls:
- Use dedicated service accounts
- Limit permissions to necessary operations
- Enable auditing for sensitive operations
- Encrypt database connections

### Backup Strategy
- Regular automated backups
- Test restore procedures
- Consider encryption for backups
- Retain backups according to compliance requirements

## Troubleshooting

### Common Issues

#### 1. Token Not Found
```sql
-- Check if token exists and its status
SELECT Id, Type, Status, ExpirationDate, Subject
FROM OpenIddictTokens 
WHERE ReferenceId = 'your-token-reference';
```

#### 2. Client Authentication Failed
```sql
-- Verify client exists and check permissions
SELECT ClientId, DisplayName, Permissions
FROM OpenIddictApplications 
WHERE ClientId = 'your-client-id';
```

#### 3. Scope Not Available
```sql
-- Check available scopes
SELECT Name, DisplayName, Description
FROM OpenIddictScopes 
WHERE Name = 'your-scope-name';
```

### Performance Issues
- Check for missing indexes on frequently queried columns
- Monitor token table growth and implement cleanup
- Consider partitioning large tables
- Use connection pooling appropriately

## Migration and Versioning

### Schema Updates
OpenIddict handles schema migrations automatically, but for production:
1. Review migration scripts before applying
2. Backup database before migrations
3. Test migrations in staging environment
4. Plan for rollback scenarios

### Version Compatibility
- OpenIddict 5.x uses the schema described in this document
- Upgrading between major versions may require schema changes
- Always check migration guides for breaking changes

---

**Note**: This documentation is based on OpenIddict 5.0.0 with Entity Framework Core 8.0. Schema details may vary with different versions.

For the most up-to-date information, refer to the [official OpenIddict documentation](https://documentation.openiddict.com/).
