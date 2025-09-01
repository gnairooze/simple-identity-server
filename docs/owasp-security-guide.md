# OWASP Security Guide for SimpleIdentityServer.API

## Overview

This document provides a comprehensive security checklist for SimpleIdentityServer.API based on OWASP API Security Top 10 and general security best practices. The recommendations are tailored specifically to the current implementation using OpenIddict and ASP.NET Core.

## Current Security Assessment

### ✅ Already Implemented
- OAuth2/OpenID Connect with OpenIddict
- Client credentials flow
- HTTPS redirection
- Authentication and authorization middleware
- Client secret validation
- Token introspection endpoint
- **Production certificates** - Proper certificate handling in production environment
- **Comprehensive rate limiting** - Global and endpoint-specific rate limiting with configurable limits
- **Security logging** - Serilog-based structured logging with SQL Server storage and 30-day retention
- **Security monitoring** - Real-time monitoring of token requests and suspicious activities
- **Request size limits** - 1MB body size limits and connection throttling (100 concurrent connections)
- **Database security** - Command timeouts (30s) and connection retry logic
- **Load balancer support** - Proper forwarded headers handling for real client IP detection
- **Security headers** - Implemented at load balancer level (Caddy)
- **Authorization policies** - Scope-based authorization implemented in Resource.API

### ⚠️ Needs Immediate Attention
- **Hardcoded database credentials** - Still present in appsettings.json files
- **Global exception handling** - Not implemented in application pipeline
- **Input validation** - No validation attributes on models/DTOs
- **Security headers in API** - Headers only set at load balancer, not in application
- **Transport security requirements** - Still disabled in Resource.API (`RequireHttpsMetadata = false`)

## OWASP API Security Top 10 - Action Items

### 1. API1:2023 - Broken Object Level Authorization (BOLA)

**Current Risk**: Medium - Limited endpoints but no explicit object-level checks

**Actions Required**:
- [ ] Implement resource-based authorization for all endpoints
- [ ] Add user context validation in TokenController
- [ ] Create authorization policies for different client types
- [ ] Implement proper scope validation for resource access

```csharp
// Example implementation needed in controllers
[Authorize(Policy = "RequireClientScope")]
public class TokenController : ControllerBase
{
    // Add proper authorization checks
}
```

### 2. API2:2023 - Broken Authentication

**Current Risk**: Medium - Production certificates implemented, but some transport security issues remain

**Actions Required**:
- [x] ~~**CRITICAL**: Replace development certificates with production certificates~~ ✅ **COMPLETED** - Production certificates now properly configured
- [ ] Remove `DisableTransportSecurityRequirement()` in production - **Still present in Resource.API**
- [ ] Implement certificate rotation strategy
- [ ] Add multi-factor authentication for administrative functions
- [ ] Strengthen client secret requirements (minimum length, complexity)

**Code Changes Needed**:
```csharp
// In Program.cs - Replace this block for production:
options.AddDevelopmentEncryptionCertificate()
       .AddDevelopmentSigningCertificate();

// With:
options.AddEncryptionCertificate(GetProductionEncryptionCertificate())
       .AddSigningCertificate(GetProductionSigningCertificate());
```

### 3. API3:2023 - Broken Object Property Level Authorization

**Current Risk**: Medium - Limited data exposure but no field-level controls

**Actions Required**:
- [ ] Implement field-level authorization in API responses
- [ ] Add data filtering based on client permissions
- [ ] Review token introspection response for sensitive data exposure
- [ ] Implement response filtering middleware

### 4. API4:2023 - Unrestricted Resource Consumption

**Current Risk**: Low - Comprehensive resource controls implemented

**Actions Required**:
- [x] ~~**CRITICAL**: Implement rate limiting middleware~~ ✅ **COMPLETED** - Global and endpoint-specific rate limiting implemented
- [x] ~~Add request size limits~~ ✅ **COMPLETED** - 1MB request body limits configured
- [x] ~~Implement connection throttling~~ ✅ **COMPLETED** - 100 concurrent connection limit
- [x] ~~Add timeout configurations for database operations~~ ✅ **COMPLETED** - 30-second command timeout with retry logic
- [x] ~~Monitor and limit token generation frequency~~ ✅ **COMPLETED** - Token request monitoring with suspicious activity detection

**Implementation Example**:
```csharp
// Add to Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Add to pipeline
app.UseRateLimiter();
```

### 5. API5:2023 - Broken Function Level Authorization

**Current Risk**: Medium - Basic authorization but missing granular controls

**Actions Required**:
- [ ] Implement role-based access control (RBAC) for all endpoints
- [ ] Add function-level authorization policies
- [ ] Restrict administrative endpoints to authorized users only
- [ ] Implement proper scope validation for different operations

### 6. API6:2023 - Unrestricted Access to Sensitive Business Flows

**Current Risk**: Medium - Token generation flow needs protection

**Actions Required**:
- [ ] Implement CAPTCHA for sensitive operations
- [ ] Add business logic validation
- [ ] Implement anomaly detection for token requests
- [ ] Add workflow-based authorization

### 7. API7:2023 - Server Side Request Forgery (SSRF)

**Current Risk**: Low - Limited external requests but needs validation

**Actions Required**:
- [ ] Validate and sanitize all URLs if external requests are added
- [ ] Implement allowlist for external services
- [ ] Add network segmentation controls

### 8. API8:2023 - Security Misconfiguration

**Current Risk**: Medium - Some misconfigurations resolved, critical ones remain

**Actions Required**:
- [ ] **CRITICAL**: Remove hardcoded database credentials from appsettings.json - **Still present**
- [ ] Implement secure configuration management
- [ ] Remove development configurations from production
- [x] ~~Enable security headers~~ ✅ **COMPLETED** - Security headers implemented at load balancer level
- [ ] Configure proper CORS policies - **Not yet configured**
- [ ] Disable unnecessary features and endpoints
- [ ] **NEW**: Add security headers directly in API application (not just load balancer)

**Immediate Configuration Changes**:

1. **Move sensitive configuration to environment variables or Azure Key Vault**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "#{ConnectionString}#"
  }
}
```

2. **Add security headers middleware**:
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    await next();
});
```

### 9. API9:2023 - Improper Inventory Management

**Current Risk**: Medium - Limited documentation and monitoring

**Actions Required**:
- [ ] Create comprehensive API documentation
- [ ] Implement API versioning strategy
- [ ] Add endpoint inventory and monitoring
- [ ] Document all client applications and their permissions
- [ ] Implement API lifecycle management

### 10. API10:2023 - Unsafe Consumption of APIs

**Current Risk**: Low - Currently not consuming external APIs

**Actions Required**:
- [ ] If consuming external APIs, implement proper validation
- [ ] Add timeout and circuit breaker patterns
- [ ] Validate all external API responses

## Additional Security Measures

### Logging and Monitoring

**Current Status**: ✅ **LARGELY COMPLETED**

**Actions Required**:
- [x] ~~**CRITICAL**: Implement comprehensive security logging~~ ✅ **COMPLETED** - Serilog with SQL Server storage implemented
- [x] ~~Add structured logging with correlation IDs~~ ✅ **COMPLETED** - Request IDs and structured logging in place
- [x] ~~Monitor failed authentication attempts~~ ✅ **COMPLETED** - SecurityMonitoringMiddleware tracks all requests
- [x] ~~Implement alerting for suspicious activities~~ ✅ **COMPLETED** - Suspicious token request detection implemented
- [x] ~~Add performance monitoring~~ ✅ **COMPLETED** - Request duration tracking and slow request detection

**Implementation**:
```csharp
// Add to Program.cs
builder.Logging.AddConsole();
builder.Logging.AddApplicationInsights();

// Custom security logging middleware
app.UseMiddleware<SecurityLoggingMiddleware>();
```

### Input Validation

**Actions Required**:
- [ ] Add input validation attributes to all models
- [ ] Implement request validation middleware
- [ ] Add SQL injection protection (parameterized queries)
- [ ] Validate all query parameters and headers

### Database Security

**Actions Required**:
- [ ] **CRITICAL**: Use Azure Key Vault or similar for connection strings
- [ ] Implement database connection encryption
- [ ] Add database access logging
- [ ] Use least privilege database accounts
- [ ] Enable database auditing

### Error Handling

**Current Status**: ⚠️ **PARTIALLY IMPLEMENTED**

**Actions Required**:
- [ ] **CRITICAL**: Implement global exception handling - **Not implemented in pipeline**
- [x] ~~Remove sensitive information from error responses~~ ✅ **COMPLETED** - DebugLoggingMiddleware masks sensitive headers
- [ ] Add custom error pages
- [x] ~~Log all exceptions securely~~ ✅ **COMPLETED** - SecurityMonitoringMiddleware logs exceptions with context

**Implementation**:
```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
        {
            // Log the error securely (don't expose sensitive data)
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(error.Error, "Unhandled exception occurred");
            
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "Internal server error",
                message = "An error occurred processing your request"
            }));
        }
    });
});
```

## Current Implementation Summary

### ✅ **COMPLETED Security Features** (Major Improvements Since Initial Assessment)

1. **Production Certificate Management** - Proper certificate handling for production environments with shared certificate support for load balancing
2. **Comprehensive Rate Limiting** - Multi-tier rate limiting (global: 100/min, token: 20/min, introspection: 50/min) with proper client identification
3. **Advanced Security Logging** - Serilog-based structured logging with SQL Server storage, 30-day retention, and correlation IDs
4. **Real-time Security Monitoring** - SecurityMonitoringMiddleware with suspicious activity detection and alerting
5. **Resource Consumption Controls** - Request size limits (1MB), connection throttling (100 concurrent), database timeouts (30s)
6. **Load Balancer Security** - Proper forwarded headers handling, trusted proxy configuration, and client IP detection
7. **Security Headers** - Implemented at load balancer level with comprehensive security headers
8. **Database Resilience** - Connection retry logic, command timeouts, and error handling

### ⚠️ **REMAINING CRITICAL ITEMS** (Immediate Attention Required)

1. **Database Credentials Security** - Hardcoded passwords still present in `appsettings.json` and `appsettings.Production.json`
2. **Global Exception Handling** - No centralized exception handling middleware in the application pipeline
3. **Input Validation** - No validation attributes on models or DTOs
4. **Transport Security** - Resource.API still has `RequireHttpsMetadata = false`
5. **Application-Level Security Headers** - Headers only set at load balancer, not in the API itself

### 📊 **Security Risk Assessment Update**

- **Overall Risk Level**: Reduced from **HIGH** to **MEDIUM**
- **Critical Issues Resolved**: 5 of 8 critical items completed
- **Major Security Improvements**: 8 significant security features implemented
- **Remaining High-Priority Items**: 5 items requiring immediate attention

## Implementation Priority

### Phase 1 - Critical Security Issues (Immediate - Week 1)
1. **Remove hardcoded database credentials** - ⚠️ **STILL PENDING**
2. ~~Replace development certificates with production certificates~~ - ✅ **COMPLETED**
3. **Enable transport security requirements** - ⚠️ **PARTIALLY DONE** (Resource.API still has `RequireHttpsMetadata = false`)
4. ~~Implement basic rate limiting~~ - ✅ **COMPLETED**
5. ~~Add security headers~~ - ✅ **COMPLETED** (at load balancer level)

### Phase 2 - Core Security Features (Week 2-3)
1. ~~Implement comprehensive logging and monitoring~~ - ✅ **COMPLETED**
2. **Add input validation** - ⚠️ **STILL PENDING**
3. **Implement proper error handling** - ⚠️ **PARTIALLY DONE** (global exception handler missing)
4. **Add authentication/authorization enhancements** - ⚠️ **IN PROGRESS** (scope-based auth implemented in Resource.API)
5. **Configure secure CORS policies** - ⚠️ **STILL PENDING**

### Phase 3 - Advanced Security Features (Week 4-6)
1. ~~Implement advanced rate limiting and throttling~~ - ✅ **COMPLETED**
2. **Add business logic protection** - ⚠️ **PARTIALLY DONE** (token request monitoring implemented)
3. ~~Implement comprehensive monitoring and alerting~~ - ✅ **COMPLETED**
4. **Add security testing automation** - ⚠️ **STILL PENDING**
5. **Create security documentation and procedures** - ✅ **IN PROGRESS** (SECURITY_FEATURES.md and SERILOG_SECURITY_LOGGING.md created)

## Security Testing Checklist

- [ ] Penetration testing of all endpoints
- [ ] Automated security scanning (SAST/DAST)
- [ ] Dependency vulnerability scanning
- [ ] Configuration security review
- [ ] Load testing with security focus
- [ ] Authentication and authorization testing

## Compliance and Documentation

- [ ] Document security architecture
- [ ] Create incident response procedures
- [ ] Implement security training for development team
- [ ] Regular security reviews and updates
- [ ] Compliance documentation (if required)

## Monitoring and Maintenance

- [ ] Regular security updates for dependencies
- [ ] Certificate rotation procedures
- [ ] Security metrics and KPIs
- [ ] Regular security assessments
- [ ] Threat modeling updates

## Resources and References

- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [OpenIddict Documentation](https://documentation.openiddict.com/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Azure Key Vault Integration](https://docs.microsoft.com/en-us/azure/key-vault/)

---

**Note**: This security guide should be reviewed and updated regularly as the application evolves and new security threats emerge. All critical items marked as **CRITICAL** should be addressed before deploying to production.
