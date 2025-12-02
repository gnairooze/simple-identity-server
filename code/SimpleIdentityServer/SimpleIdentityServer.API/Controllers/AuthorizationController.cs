using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Client;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using SimpleIdentityServer.API.Data;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SimpleIdentityServer.API.Controllers;

[ApiController]
[Route("connect")]
public class AuthorizationController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AuthorizationController> logger)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Retrieve the user principal stored in the authentication cookie
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        // If the user principal can't be extracted or the cookie is too old, redirect to login
        if (!result.Succeeded || 
            (request.MaxAge != null && result.Properties?.IssuedUtc != null &&
             DateTimeOffset.UtcNow - result.Properties.IssuedUtc > TimeSpan.FromSeconds(request.MaxAge.Value)))
        {
            // Store the original request in TempData for after login
            if (request.HasPromptValue("None"))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not logged in."
                    }));
            }

            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? [.. Request.Form] : Request.Query.ToList())
                });
        }

        // Retrieve the profile of the logged in user
        var user = await _userManager.GetUserAsync(result.Principal) ??
            throw new InvalidOperationException("The user details cannot be retrieved.");

        // Retrieve the application details from the database
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId ?? string.Empty) ??
            throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

        // Retrieve the permanent authorizations associated with the user and the calling client application
        var authorizations = new List<object>();
        await foreach (var auth in _authorizationManager.FindAsync(
            subject: await _userManager.GetUserIdAsync(user),
            client: await _applicationManager.GetIdAsync(application) ?? string.Empty,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes()))
        {
            authorizations.Add(auth);
        }

        switch (await _applicationManager.GetConsentTypeAsync(application))
        {
            case ConsentTypes.External when authorizations.Count == 0:
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The logged in user is not allowed to access this client application."
                    }));

            case ConsentTypes.Implicit:
            case ConsentTypes.External when authorizations.Count != 0:
            case ConsentTypes.Explicit when authorizations.Count != 0 && !request.HasPromptValue("Consent"):
                break;

            case ConsentTypes.Explicit when request.HasPromptValue("None"):
            case ConsentTypes.Systematic when request.HasPromptValue("None"):
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Interactive user consent is required."
                    }));

            default:
                return RedirectToAction(nameof(Consent), new { ReturnUrl = Request.PathBase + Request.Path + QueryString.Create(
                    Request.HasFormContentType ? [.. Request.Form] : Request.Query.ToList()) });
        }

        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        // Note: in this sample, the granted scopes match the requested scope
        // but you may want to allow the user to uncheck specific scopes.
        // For that, simply restrict the list of scopes before calling SetScopes.
        principal.SetScopes(request.GetScopes());

        var resources = new List<string>();
        await foreach (var resource in _scopeManager.ListResourcesAsync(principal.GetScopes()))
        {
            resources.Add(resource);
        }
        principal.SetResources(resources);

        // Automatically create a permanent authorization to avoid requiring explicit consent
        // for future authorization or token requests containing the same scopes.
        var authorization = authorizations.LastOrDefault() ?? await _authorizationManager.CreateAsync(
                principal: principal,
                subject: await _userManager.GetUserIdAsync(user),
                client: await _applicationManager.GetIdAsync(application) ?? string.Empty,
                type: AuthorizationTypes.Permanent,
                scopes: principal.GetScopes());
        principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim));
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("consent")]
    [HttpPost("consent")]
    [Authorize]
    public async Task<IActionResult> Consent()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Handle consent logic here
        // For now, we'll auto-approve all requests
        var user = await _userManager.GetUserAsync(User) ??
            throw new InvalidOperationException("The user details cannot be retrieved.");

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId ?? string.Empty) ??
            throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        principal.SetScopes(request.GetScopes());

        var resources = new List<string>();
        await foreach (var resource in _scopeManager.ListResourcesAsync(principal.GetScopes()))
        {
            resources.Add(resource);
        }
        principal.SetResources(resources);

        var authorization = await _authorizationManager.CreateAsync(
            principal: principal,
            subject: await _userManager.GetUserIdAsync(user),
            client: await _applicationManager.GetIdAsync(application) ?? string.Empty,
            type: AuthorizationTypes.Permanent,
            scopes: principal.GetScopes());

        principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim));
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        await _signInManager.SignOutAsync();

        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = request.PostLogoutRedirectUri
            });
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        switch (claim.Type)
        {
            case Claims.Name:
                yield return Destinations.AccessToken;

                if (claim.Subject?.HasScope(Scopes.Profile) == true)
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;

                if (claim.Subject?.HasScope(Scopes.Email) == true)
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;

                if (claim.Subject?.HasScope(Scopes.Roles) == true)
                    yield return Destinations.IdentityToken;

                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}

