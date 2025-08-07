using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;

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

            // Set the list of scopes granted to the client application in the access token.
            principal.SetScopes(request.GetScopes());

            // Set the resources server identifier for each scope in the access token.
            var resources = new List<string>();
            foreach (var scope in request.GetScopes())
            {
                var resource = await _scopeManager.FindByNameAsync(scope);
                if (resource != null)
                {
                    var resourceList = await _scopeManager.GetResourcesAsync(resource);
                    resources.AddRange(resourceList);
                }
            }
            principal.SetResources(resources);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest();
    }

    [HttpPost("introspect")]
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

        // Build the minimal introspection response
        var response = new Dictionary<string, object>
        {
            ["active"] = true
        };

        if (expirationDate.HasValue)
        {
            response["exp"] = expirationDate.Value.ToUnixTimeSeconds();
        }

        var creationDate = await _tokenManager.GetCreationDateAsync(tokenEntity);
        if (creationDate.HasValue)
        {
            response["iat"] = creationDate.Value.ToUnixTimeSeconds();
        }

        return Ok(response);
    }
} 