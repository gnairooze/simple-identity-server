# Load Balancer Token Validation Solutions

## Problem
When using multiple API instances behind a load balancer, token validation fails inconsistently because each instance uses different signing keys (ephemeral keys), causing tokens signed by one instance to be rejected by others.

## Root Cause
The issue was in `Program.cs` lines 285-286:
```csharp
options.AddEphemeralEncryptionKey()
       .AddEphemeralSigningKey()
```

Each API instance generates its own ephemeral signing keys on startup, so tokens signed by Instance A cannot be validated by Instance B.

## Solution 1: Shared Certificates (Implemented)

### Changes Made:
1. **Program.cs**: Modified OpenIddict configuration to use shared certificates in production
2. **docker-compose.yml**: Added shared volume for certificates and environment variable for certificate password
3. **Certificate Management**: Added helper methods to create/load shared certificates

### How it Works:
- All API instances use the same signing and encryption certificates stored in a shared volume
- First instance to start creates the certificates, others reuse them
- Ensures consistent token signing across all instances

### Production Considerations:
- Use proper certificate management (Azure Key Vault, HashiCorp Vault, etc.)
- Implement certificate rotation
- Use strong certificate passwords from secure storage

## Solution 2: Stateless JWT Tokens (Alternative)

For a simpler approach, you could modify the introspection logic to validate JWT tokens directly without database lookups:

```csharp
[HttpPost("introspect")]
public async Task<IActionResult> Introspect()
{
    var request = HttpContext.GetOpenIddictServerRequest();
    if (request == null) return BadRequest();

    // Validate client credentials first
    var application = await _applicationManager.FindByClientIdAsync(request.ClientId ?? string.Empty);
    if (application == null || !await _applicationManager.ValidateClientSecretAsync(application, request.ClientSecret ?? string.Empty))
    {
        return Forbid(/* ... */);
    }

    var token = request.Token;
    if (string.IsNullOrEmpty(token)) return BadRequest();

    // Instead of database lookup, validate JWT directly
    try
    {
        var tokenHandler = new JsonWebTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetSharedSigningKey(), // Use same key as token creation
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);
        
        if (result.IsValid)
        {
            var jwtToken = result.SecurityToken as JsonWebToken;
            return Ok(new 
            { 
                active = true,
                exp = jwtToken?.ValidTo.ToUnixTimeSeconds(),
                iat = jwtToken?.ValidFrom.ToUnixTimeSeconds()
            });
        }
        else
        {
            return Ok(new { active = false });
        }
    }
    catch
    {
        return Ok(new { active = false });
    }
}
```

## Testing the Solution

After implementing Solution 1, test with:

1. Start the environment: `docker-compose up -d`
2. Get a token from one instance: `curl -X POST http://localhost:8081/connect/token -d "grant_type=client_credentials&client_id=service-api&client_secret=your-secret"`
3. Validate the token against different instances:
   - `curl -X POST http://localhost:8082/connect/introspect -d "token=YOUR_TOKEN&client_id=service-api&client_secret=your-secret"`
   - `curl -X POST http://localhost:8083/connect/introspect -d "token=YOUR_TOKEN&client_id=service-api&client_secret=your-secret"`

All instances should now return consistent results.

## Monitoring

Check the health check service logs to see if all instances are responding correctly:
```bash
docker logs simple-identity-server-health
```
