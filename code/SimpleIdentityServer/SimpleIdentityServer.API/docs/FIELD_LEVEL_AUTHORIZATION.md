# Field-Level Authorization Implementation (API3:2023)

This document describes the implementation of field-level authorization controls to address **API3:2023 - Broken Object Property Level Authorization** from the OWASP API Security Top 10.

## Overview

The implementation provides comprehensive field-level authorization that filters API responses based on client permissions, roles, and trust levels. This prevents unauthorized access to sensitive data fields within API responses.

## Components

### 1. FieldLevelAuthorizationAttribute

**Location**: `Authorization/FieldLevelAuthorizationAttribute.cs`

An action filter attribute that can be applied to controllers or actions to enable field-level filtering.

```csharp
[FieldLevelAuthorization(requiredScopes: new[] { "api1.read" })]
public IActionResult GetSensitiveData()
{
    // Implementation
}
```

**Features**:
- Filters response objects based on user claims
- Supports both single objects and collections
- Configurable role and scope requirements
- Reflection-based property filtering

### 2. ResponseFilteringMiddleware

**Location**: `Middleware/ResponseFilteringMiddleware.cs` (both APIs)

Global middleware that intercepts JSON responses and applies field-level filtering automatically.

**Features**:
- Automatic JSON response filtering
- Claims-based field authorization
- Configurable sensitivity rules
- Performance optimized with streaming
- Comprehensive logging

**Filtering Rules**:
```csharp
var sensitiveFields = new Dictionary<string, string[]>
{
    { "temperatureC", new[] { "service", "admin" } },
    { "temperatureF", new[] { "service", "admin" } },
    { "humidity", new[] { "service", "admin" } },
    { "pressure", new[] { "service", "admin" } },
    { "internalId", new[] { "admin" } }, // Admin only
    // Basic fields for all authenticated users
    { "summary", new[] { "web_user", "mobile_user", "service", "admin" } },
    { "date", new[] { "web_user", "mobile_user", "service", "admin" } }
};
```

### 3. Enhanced Token Introspection

**Location**: `Controllers/TokenController.cs`

Modified introspection endpoint to limit sensitive token information based on the requesting client's trust level.

**Security Improvements**:
- Basic information (active, exp, iat) always included
- Detailed information (sub, scope, token_type) only for trusted clients
- Self-introspection allowed (client can introspect its own tokens)
- Admin and service clients get full details

**Trusted Clients**:
```csharp
var trustedClients = new[] { "service-api", "admin-client", "monitoring-service" };
```

### 4. Configuration Options

**Location**: `Configuration/FieldLevelAuthorizationOptions.cs`

Configurable options for field-level authorization behavior.

```json
{
  "FieldLevelAuthorization": {
    "EnableLogging": true,
    "StrictMode": false,
    "TrustedClients": ["service-api", "admin-client"],
    "DefaultAllowedRoles": ["admin"],
    "FieldPermissions": {
      "sensitiveField": ["admin", "service"]
    }
  }
}
```

## Client Access Levels

### Service Clients (`service` role)
- **Access**: Full data including temperature, humidity, pressure
- **Use Case**: Backend services, data processing, monitoring
- **Example**: `service-api` client

### Web Clients (`web_user` role)
- **Access**: Basic data (date, summary, location)
- **Restricted**: Temperature data, internal identifiers
- **Use Case**: Web applications, user dashboards

### Mobile Clients (`mobile_user` role)
- **Access**: Basic data (date, summary, location)
- **Restricted**: Temperature data, internal identifiers
- **Use Case**: Mobile applications, limited bandwidth scenarios

### Admin Clients (`admin` role)
- **Access**: All data including internal identifiers
- **Use Case**: Administrative interfaces, debugging, monitoring

## Implementation Details

### Middleware Registration

```csharp
// In Program.cs - after authentication/authorization
app.UseMiddleware<ResponseFilteringMiddleware>();
```

### Controller Implementation

```csharp
[HttpGet("detailed")]
[Authorize(Policy = "RequireApi1Read")]
public IEnumerable<object> GetDetailed()
{
    // Data generation
    var data = GenerateData();
    
    // Filtering is handled automatically by middleware
    return data;
}
```

### Manual Filtering (Alternative)

```csharp
private IEnumerable<object> FilterBasedOnClaims(object[] data)
{
    var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    var clientId = User.FindFirst("client_id")?.Value;
    
    return data.Select(item => FilterSingleItem(item, userRoles, clientId));
}
```

## Testing

### Automated Testing Script

**Location**: `test-field-level-authorization.ps1`

Comprehensive PowerShell script that tests:
- Different client types (service, web, mobile)
- Field-level filtering effectiveness
- Token introspection protection
- Identity server endpoint exclusions

### Running Tests

```powershell
# Basic test
.\test-field-level-authorization.ps1

# With custom URLs
.\test-field-level-authorization.ps1 -IdentityServerUrl "https://localhost:7443" -ResourceApiUrl "https://localhost:7444"

# Skip certificate validation (development)
.\test-field-level-authorization.ps1 -SkipCertCheck
```

### Expected Results

**Service Client Response**:
```json
[
  {
    "date": "2024-01-01",
    "temperatureC": 25,
    "temperatureF": 77,
    "summary": "Warm",
    "humidity": 65,
    "pressure": 1013,
    "location": "Sample City"
  }
]
```

**Web/Mobile Client Response**:
```json
[
  {
    "date": "2024-01-01",
    "summary": "Warm",
    "location": "Sample City"
  }
]
```

## Security Benefits

1. **Data Minimization**: Clients only receive data they're authorized to access
2. **Principle of Least Privilege**: Different access levels based on client trust
3. **Transparent Filtering**: Automatic filtering without code changes
4. **Audit Trail**: Comprehensive logging of filtering actions
5. **Introspection Protection**: Prevents token information leakage
6. **Performance Optimized**: Streaming-based filtering for large responses

## Best Practices

1. **Define Clear Roles**: Establish distinct roles with specific data access needs
2. **Regular Audits**: Review field permissions and client access levels
3. **Monitor Logs**: Track filtering actions for security analysis
4. **Test Thoroughly**: Validate filtering with all client types
5. **Document Permissions**: Maintain clear documentation of field access rules
6. **Gradual Rollout**: Implement filtering incrementally to avoid breaking changes

## Compliance

This implementation addresses:
- **OWASP API3:2023**: Broken Object Property Level Authorization
- **GDPR**: Data minimization and purpose limitation
- **SOC 2**: Access controls and data protection
- **ISO 27001**: Information security controls

## Future Enhancements

1. **Dynamic Rules**: Database-driven field permission configuration
2. **Context-Aware Filtering**: Time-based or location-based access controls
3. **Performance Metrics**: Response time impact analysis
4. **Advanced Logging**: Integration with SIEM systems
5. **Client Feedback**: API responses indicating filtered fields
