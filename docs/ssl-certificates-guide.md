# SSL Certificates in SimpleIdentityServer.API

This document provides a comprehensive overview of the SSL certificates used in the SimpleIdentityServer.API project, their purposes, configuration, and management.

## Table of Contents

- [Certificate Types Overview](#certificate-types-overview)
- [OpenIddict Certificates](#openiddict-certificates)
- [TLS/SSL Certificates](#tlsssl-certificates)
- [Certificate Configuration](#certificate-configuration)
- [Certificate Generation](#certificate-generation)
- [Docker Deployment Certificates](#docker-deployment-certificates)
- [Security Considerations](#security-considerations)
- [Troubleshooting](#troubleshooting)

## Certificate Types Overview

The SimpleIdentityServer.API uses several types of certificates for different security purposes:

### 1. **OpenIddict Certificates** (Application-Level)
- **Encryption Certificate** (`encryption.pfx`)
- **Signing Certificate** (`signing.pfx`)

### 2. **TLS/SSL Certificates** (Transport-Level)
- **Server Certificate** (`identity-dev-test.crt` + `identity-dev-test.key`)
- **CA Certificate** (`identity-dev-test-ca.crt`)

## OpenIddict Certificates

OpenIddict uses certificates for cryptographic operations related to OAuth 2.0 and OpenID Connect tokens.

### Encryption Certificate (`encryption.pfx`)

**Purpose:**
- Encrypts access tokens and refresh tokens issued by the identity server
- Ensures token confidentiality during transmission and storage
- Used by OpenIddict for token encryption operations

**Technical Details:**
- **Format:** PKCS#12 (.pfx)
- **Algorithm:** RSA 2048-bit
- **Subject:** `CN=SimpleIdentityServer-Encryption`
- **Validity:** 2 years from creation
- **Usage:** Key Encipherment, Digital Signature

**Configuration Paths:**
- **Development:** `./certs/encryption.pfx`
- **Production (Docker):** `/app/certs/encryption.pfx`

**Code Reference:**
```csharp
// ServiceConfiguration.cs
options.AddEncryptionCertificate(CertificateManager.GetEncryptionCertificate(certificateOptions))
```

### Signing Certificate (`signing.pfx`)

**Purpose:**
- Signs JWT tokens (ID tokens, access tokens) issued by the identity server
- Provides token integrity and authenticity verification
- Used by resource servers to verify token signatures

**Technical Details:**
- **Format:** PKCS#12 (.pfx)
- **Algorithm:** RSA 2048-bit
- **Subject:** `CN=SimpleIdentityServer-Signing`
- **Validity:** 2 years from creation
- **Usage:** Digital Signature, Key Encipherment

**Configuration Paths:**
- **Development:** `./certs/signing.pfx`
- **Production (Docker):** `/app/certs/signing.pfx`

**Code Reference:**
```csharp
// ServiceConfiguration.cs
options.AddSigningCertificate(CertificateManager.GetSigningCertificate(certificateOptions))
```

## TLS/SSL Certificates

TLS certificates secure HTTP communication between clients and the identity server.

### Server Certificate (`identity-dev-test.crt` + `identity-dev-test.key`)

**Purpose:**
- Secures HTTPS connections to the identity server
- Provides transport-level encryption and server authentication
- Used by the load balancer (Caddy) and API instances

**Technical Details:**
- **Format:** X.509 PEM format
- **Certificate File:** `identity-dev-test.crt`
- **Private Key File:** `identity-dev-test.key`
- **Subject:** `CN=identity.dev.test` (or your domain)
- **Validity:** Typically 1 year (can be customized)

**Usage Locations:**
- **Caddy Load Balancer:** `/etc/caddy/ssl/identity-dev-test.crt`
- **API Instances:** `/app/certs/identity-dev-test.crt`

**Caddy Configuration:**
```caddy
identity.dev.test:443 {
    tls /etc/caddy/ssl/identity-dev-test.crt /etc/caddy/ssl/identity-dev-test.key
    # ... rest of configuration
}
```

### CA Certificate (`identity-dev-test-ca.crt`)

**Purpose:**
- Certificate Authority certificate for validating the server certificate
- Used by health check services and client applications
- Enables certificate chain validation

**Usage:**
- Health check container uses this for SSL verification
- Can be installed in client systems' certificate stores

## Certificate Configuration

### Environment Variables

The certificate password is managed through environment variables:

```bash
# Required for production
SIMPLE_IDENTITY_SERVER_CERT_PASSWORD=YourSecurePassword123!
```

### Configuration Files

**Development (`appsettings.json`):**
```json
{
  "Application": {
    "Certificates": {
      "Password": "",
      "EncryptionCertificatePath": "./certs/encryption.pfx",
      "SigningCertificatePath": "./certs/signing.pfx"
    }
  }
}
```

**Production (`appsettings.Production.json`):**
```json
{
  "Application": {
    "Certificates": {
      "Password": "",
      "EncryptionCertificatePath": "/app/certs/encryption.pfx",
      "SigningCertificatePath": "/app/certs/signing.pfx"
    }
  }
}
```

### Certificate Loading Logic

The application uses a centralized certificate manager:

```csharp
// CertificateManager.cs
public static X509Certificate2 GetEncryptionCertificate(CertificateOptions certificateOptions)
{
    var certPath = certificateOptions.EncryptionCertificatePath;
    var certPassword = GetCertificatePassword(certificateOptions.Password);
    
    if (!File.Exists(certPath))
    {
        throw new InvalidOperationException($"Encryption certificate not found at path: {Path.GetFullPath(certPath)}");
    }
    
    return new X509Certificate2(certPath, certPassword);
}
```

## Certificate Generation

### Using the CLI Tool

The SimpleIdentityServer.CLI provides commands to generate the required OpenIddict certificates:

#### Generate Encryption Certificate
```bash
cd code/SimpleIdentityServer/SimpleIdentityServer.CLI
dotnet run -- cert create-encryption --path "../SimpleIdentityServer.API/certs/encryption.pfx"
```

#### Generate Signing Certificate
```bash
dotnet run -- cert create-signing --path "../SimpleIdentityServer.API/certs/signing.pfx"
```

#### Using Environment Variable for Password
```bash
# Set password environment variable
export SIMPLE_IDENTITY_SERVER_CERT_PASSWORD="YourSecurePassword123!"

# Generate certificates without password parameter
dotnet run -- cert create-encryption --path "./certs/encryption.pfx"
dotnet run -- cert create-signing --path "./certs/signing.pfx"
```

### Manual Certificate Generation

#### OpenSSL Commands for TLS Certificates

**Generate Server Certificate:**
```bash
# Generate private key
openssl genrsa -out identity-dev-test.key 2048

# Generate certificate signing request
openssl req -new -key identity-dev-test.key -out identity-dev-test.csr \
  -subj "/C=US/ST=State/L=City/O=Organization/CN=identity.dev.test"

# Generate self-signed certificate
openssl x509 -req -days 365 -in identity-dev-test.csr \
  -signkey identity-dev-test.key -out identity-dev-test.crt

# Clean up
rm identity-dev-test.csr
```

**Generate CA Certificate:**
```bash
# Generate CA private key
openssl genrsa -out identity-dev-test-ca.key 2048

# Generate CA certificate
openssl req -new -x509 -days 3650 -key identity-dev-test-ca.key \
  -out identity-dev-test-ca.crt \
  -subj "/C=US/ST=State/L=City/O=Organization/CN=Identity Dev Test CA"
```

## Docker Deployment Certificates

### Certificate Volume Mapping

In the Docker Compose configuration, certificates are mounted as volumes:

```yaml
volumes:
  # TLS certificates
  - ./nginx/ssl/identity-dev-test.crt:/app/certs/identity-dev-test.crt:ro
  - ./nginx/ssl/identity-dev-test.key:/app/certs/identity-dev-test.key:ro
  
  # OpenIddict certificates
  - ./nginx/ssl/encryption.pfx:/app/certs/encryption.pfx:ro
  - ./nginx/ssl/signing.pfx:/app/certs/signing.pfx:ro
```

### Certificate Directory Structure

```
containers/nginx/ssl/
├── identity-dev-test.crt       # Server certificate
├── identity-dev-test.key       # Server private key
├── identity-dev-test-ca.crt    # CA certificate
├── identity-dev-test-ca.key    # CA private key
├── encryption.pfx              # OpenIddict encryption certificate
└── signing.pfx                 # OpenIddict signing certificate
```

### Shared Certificates Across Instances

All API instances share the same certificates through Docker volumes:

```yaml
volumes:
  - shared_certs:/app/certs
```

This ensures:
- **Consistent token validation** across all instances
- **Load balancer compatibility** with identical certificates
- **Simplified certificate management** with single update point

## Security Considerations

### Certificate Security Best Practices

1. **Strong Passwords:**
   - Use complex passwords for certificate files (minimum 12 characters)
   - Store passwords in secure environment variables
   - Never commit passwords to version control

2. **File Permissions:**
   ```bash
   # Restrict certificate file access
   chmod 600 *.pfx *.key
   chmod 644 *.crt
   ```

3. **Certificate Rotation:**
   - Plan regular certificate rotation (recommended: annually)
   - Monitor certificate expiration dates
   - Test certificate updates in staging environment first

4. **Storage Security:**
   - Use secure certificate stores in production (Azure Key Vault, AWS Secrets Manager)
   - Encrypt certificate files at rest
   - Implement certificate backup and recovery procedures

### Production Certificate Management

**Recommended Approach:**
1. **Use proper Certificate Authority** (not self-signed) for TLS certificates
2. **Implement certificate automation** (Let's Encrypt, internal CA)
3. **Monitor certificate expiration** with automated alerts
4. **Use Hardware Security Modules (HSM)** for signing certificates in high-security environments

### Certificate Validation

The application validates certificates during startup:

```csharp
// ConfigurationValidationService.cs
private static void ValidateCertificates(IConfiguration configuration)
{
    var certificateOptions = configuration.GetSection("Application:Certificates").Get<CertificateOptions>();
    
    // Validate encryption certificate
    if (!File.Exists(certificateOptions.EncryptionCertificatePath))
        throw new InvalidOperationException("Encryption certificate file not found");
        
    // Validate signing certificate
    if (!File.Exists(certificateOptions.SigningCertificatePath))
        throw new InvalidOperationException("Signing certificate file not found");
}
```

## Troubleshooting

### Common Certificate Issues

#### 1. Certificate Not Found
**Error:** `Encryption certificate not found at path: ./certs/encryption.pfx`

**Solution:**
```bash
# Generate missing certificates using CLI
cd code/SimpleIdentityServer/SimpleIdentityServer.CLI
dotnet run -- cert create-encryption --path "../SimpleIdentityServer.API/certs/encryption.pfx"
dotnet run -- cert create-signing --path "../SimpleIdentityServer.API/certs/signing.pfx"
```

#### 2. Invalid Certificate Password
**Error:** `Certificate password is required`

**Solution:**
```bash
# Set environment variable
export SIMPLE_IDENTITY_SERVER_CERT_PASSWORD="YourPassword123!"
```

#### 3. Certificate Expired
**Error:** `Certificate has expired`

**Solution:**
```bash
# Regenerate certificates
dotnet run -- cert create-encryption --path "./certs/encryption.pfx"
dotnet run -- cert create-signing --path "./certs/signing.pfx"
```

#### 4. TLS Certificate Issues
**Error:** `SSL connection could not be established`

**Solution:**
```bash
# Verify certificate files exist
ls -la containers/nginx/ssl/

# Check certificate validity
openssl x509 -in identity-dev-test.crt -text -noout
```

### Certificate Verification Commands

**Check Certificate Details:**
```bash
# View certificate information
openssl x509 -in identity-dev-test.crt -text -noout

# Check certificate expiration
openssl x509 -in identity-dev-test.crt -noout -dates

# Verify certificate and key match
openssl x509 -noout -modulus -in identity-dev-test.crt | openssl md5
openssl rsa -noout -modulus -in identity-dev-test.key | openssl md5
```

**Test SSL Connection:**
```bash
# Test TLS connection
openssl s_client -connect identity.dev.test:443 -servername identity.dev.test

# Test with specific certificate
curl -v --cacert identity-dev-test-ca.crt https://identity.dev.test/health
```

### Health Check Certificate Validation

The health check service validates certificates:

```bash
# Health check with certificate validation
curl -f --cacert /usr/local/share/ca-certificates/nginx-ca.crt https://identity.dev.test/health
```

## Certificate Lifecycle Management

### Development Workflow

1. **Initial Setup:**
   ```bash
   # Create certificates directory
   mkdir -p code/SimpleIdentityServer/SimpleIdentityServer.API/certs
   
   # Generate OpenIddict certificates
   cd code/SimpleIdentityServer/SimpleIdentityServer.CLI
   dotnet run -- cert create-encryption --path "../SimpleIdentityServer.API/certs/encryption.pfx"
   dotnet run -- cert create-signing --path "../SimpleIdentityServer.API/certs/signing.pfx"
   ```

2. **Docker Deployment:**
   ```bash
   # Create SSL directory
   mkdir -p containers/nginx/ssl
   
   # Copy certificates to Docker SSL directory
   cp code/SimpleIdentityServer/SimpleIdentityServer.API/certs/*.pfx containers/nginx/ssl/
   
   # Generate TLS certificates (if needed)
   openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
     -keyout containers/nginx/ssl/identity-dev-test.key \
     -out containers/nginx/ssl/identity-dev-test.crt
   ```

3. **Production Migration:**
   - Replace self-signed certificates with CA-issued certificates
   - Update certificate paths in configuration
   - Implement automated certificate renewal
   - Set up certificate monitoring and alerts

---

This documentation provides a complete overview of certificate usage in the SimpleIdentityServer.API project. For additional security guidance, refer to the [OWASP Security Guide](owasp-security-guide.md) and [Identity Provider Integration Guide](identity-provider-integration-guide.md).
