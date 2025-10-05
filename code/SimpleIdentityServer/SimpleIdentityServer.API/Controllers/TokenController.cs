using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using System.Collections.Immutable;

namespace SimpleIdentityServer.API.Controllers;

[ApiController]
[Route("connect")]
public class TokenController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IOpenIddictTokenManager _tokenManager;

    public TokenController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        IOpenIddictTokenManager tokenManager)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _tokenManager = tokenManager;
    }

    [HttpPost("token")]
    [EnableRateLimiting("TokenPolicy")]
    public async Task<IActionResult> Token()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
        {
            return BadRequest();
        }

        if (request.IsClientCredentialsGrantType())
        {
            // Validate the client credentials
            var application = await _applicationManager.FindByClientIdAsync(request.ClientId ?? string.Empty);
            if (application == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The client application was not found in the database."
                    }));
            }

            // Validate the client secret
            if (!await _applicationManager.ValidateClientSecretAsync(application, request.ClientSecret ?? string.Empty))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified client credentials are invalid."
                    }));
            }

            // Note: OpenIddict automatically validates scopes against client permissions
            // No custom validation needed here

            // Create a new ClaimsIdentity containing the claims that will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            // Add the claims that will be persisted in the tokens.
            var clientId = await _applicationManager.GetClientIdAsync(application) ?? string.Empty;
            var displayName = await _applicationManager.GetDisplayNameAsync(application) ?? string.Empty;
            
            identity.AddClaim(OpenIddictConstants.Claims.Subject, clientId);
            identity.AddClaim(OpenIddictConstants.Claims.Name, displayName);

            // Add custom claims based on the client
            identity.AddClaim("client_id", clientId);

            // Set the list of scopes granted to the client application in the access token.
            var scopesToGrant = request.GetScopes();
            if (!scopesToGrant.Any())
            {
                // If no scopes requested, grant default scopes based on client permissions
                var clientPermissions = await _applicationManager.GetPermissionsAsync(application);
                var defaultScopes = clientPermissions
                    .Where(p => p.StartsWith("scp:"))
                    .Select(p => p.Substring(4)) // Remove "scp:" prefix
                    .ToList();
                
                scopesToGrant = defaultScopes.ToImmutableArray();
            }

            // Add scope claims to the identity so they appear in the token
            foreach (var scope in scopesToGrant)
            {
                identity.AddClaim(OpenIddictConstants.Claims.Scope, scope);
            }

            // Add role-based claims
            if (clientId == "service-api")
            {
                identity.AddClaim(OpenIddictConstants.Claims.Role, "service");
                identity.AddClaim("custom_claim", "service_access");
            }
            else if (clientId == "web-app")
            {
                identity.AddClaim(OpenIddictConstants.Claims.Role, "web_user");
                identity.AddClaim("custom_claim", "web_access");
            }
            else if (clientId == "mobile-app")
            {
                identity.AddClaim(OpenIddictConstants.Claims.Role, "mobile_user");
                identity.AddClaim("custom_claim", "mobile_access");
            }

            // Create the ClaimsPrincipal
            var principal = new ClaimsPrincipal(identity);
            
            principal.SetScopes(scopesToGrant);

            // Set the resources server identifier for each scope in the access token.
            var resources = new List<string>();
            foreach (var scope in scopesToGrant)
            {
                var resource = await _scopeManager.FindByNameAsync(scope);
                if (resource != null)
                {
                    var resourceList = await _scopeManager.GetResourcesAsync(resource);
                    resources.AddRange(resourceList);
                }
            }
            principal.SetResources(resources.Distinct());

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest();
    }

    [HttpPost("introspect")]
    [EnableRateLimiting("IntrospectionPolicy")]
    public async Task<IActionResult> Introspect()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
        {
            return BadRequest();
        }

        // Validate the client credentials for the introspection request
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId ?? string.Empty);
        if (application == null)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The client application was not found in the database."
                }));
        }

        // Validate the client secret
        if (!await _applicationManager.ValidateClientSecretAsync(application, request.ClientSecret ?? string.Empty))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified client credentials are invalid."
                }));
        }

        // Get the token to introspect
        var token = request.Token;
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { error = "invalid_request", error_description = "The token parameter is required." });
        }

        // Try to find the token in the database
        var tokenEntity = await _tokenManager.FindByReferenceIdAsync(token);
        if (tokenEntity == null)
        {
            // Token not found, return active: false
            return Ok(new { active = false });
        }

        // Check if the token is still valid
        var status = await _tokenManager.GetStatusAsync(tokenEntity);
        if (status != OpenIddictConstants.Statuses.Valid)
        {
            return Ok(new { active = false });
        }

        // Check if the token has expired
        var expirationDate = await _tokenManager.GetExpirationDateAsync(tokenEntity);
        if (expirationDate.HasValue && expirationDate.Value < DateTimeOffset.UtcNow)
        {
            return Ok(new { active = false });
        }

        // Build the minimal introspection response with field-level filtering
        var response = new Dictionary<string, object>
        {
            ["active"] = true
        };

        // Only include timing information for authorized clients
        var introspectingClientId = request.ClientId;
        var tokenSubject = await _tokenManager.GetSubjectAsync(tokenEntity);
        
        // Basic timing information (always included)
        if (expirationDate.HasValue)
        {
            response["exp"] = expirationDate.Value.ToUnixTimeSeconds();
        }

        var creationDate = await _tokenManager.GetCreationDateAsync(tokenEntity);
        if (creationDate.HasValue)
        {
            response["iat"] = creationDate.Value.ToUnixTimeSeconds();
        }

        // Only include detailed token information for authorized scenarios
        if (ShouldIncludeDetailedTokenInfo(introspectingClientId, tokenSubject))
        {
            // Include additional claims only for authorized introspection requests
            var tokenType = await _tokenManager.GetTypeAsync(tokenEntity);
            if (!string.IsNullOrEmpty(tokenType))
            {
                response["token_type"] = tokenType;
            }

            if (!string.IsNullOrEmpty(tokenSubject))
            {
                response["sub"] = tokenSubject;
            }

            // Include scopes if the introspecting client is authorized
            // Note: For security reasons, we only include scope information for highly trusted clients
            // This prevents scope enumeration attacks through introspection
            if (introspectingClientId == "admin-client" || introspectingClientId == "monitoring-service")
            {
                // For now, we don't expose scopes in introspection to maintain security
                // Scopes can be verified through the token validation process instead
                response["scope_access"] = "authorized";
            }
        }

        return Ok(response);
    }

    private static bool ShouldIncludeDetailedTokenInfo(string? introspectingClientId, string? tokenSubject)
    {
        // Only include detailed information if:
        // 1. The introspecting client is the same as the token subject (self-introspection)
        // 2. The introspecting client has admin privileges
        // 3. The introspecting client is a trusted service
        
        var trustedClients = new[] { "service-api", "admin-client", "monitoring-service" };
        
        return introspectingClientId == tokenSubject ||
               (introspectingClientId != null && trustedClients.Contains(introspectingClientId));
    }
} 