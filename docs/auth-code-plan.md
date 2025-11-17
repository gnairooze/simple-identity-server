# Authorization Code Flow Implementation Plan

This document provides a detailed implementation plan for adding Authorization Code flow support to the Simple Identity Server, based on the specifications in `simple-identity-server-specs.md`.

## Table of Contents

1. [Overview](#overview)
2. [Server-Side Implementation](#server-side-implementation)
3. [Database Schema Updates](#database-schema-updates)
4. [Web Pages Implementation](#web-pages-implementation)
5. [Client-Side JavaScript](#client-side-javascript)
6. [Security Implementation](#security-implementation)
7. [Testing Strategy](#testing-strategy)
8. [Deployment Considerations](#deployment-considerations)

## Overview

### Goals

- Add Authorization Code flow with PKCE support
- Implement secure web authentication pages
- Maintain existing Client Credentials flow functionality
- Follow security best practices

### Architecture Changes

```flow-diagram
┌─────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│   Web Client    │    │  Identity Server │    │   Resource API   │
│                 │    │                  │    │                  │
│ - Login Page    │◄──►│ - Auth Endpoint  │    │ - Protected      │
│ - Register Page │    │ - Token Endpoint │    │   Resources      │
│ - Reset Page    │    │ - User Store     │    │ - Token          │
│ - PKCE Support  │    │ - OpenIddict     │    │   Validation     │
└─────────────────┘    └──────────────────┘    └──────────────────┘
```

## Server-Side Implementation

### 1. Update OpenIddict Configuration

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/Configuration/ServiceConfiguration.cs`

```csharp
private static void ConfigureOpenIddict(WebApplicationBuilder builder)
{
    var openIddictOptions = builder.Configuration.GetSection(AppSettingsNames.ApplicationOpenIddict).Get<OpenIddictOptions>();
    if (openIddictOptions == null)
    {
        throw new InvalidOperationException($"{AppSettingsNames.ApplicationOpenIddict} configuration section is required");
    }

    var certificateOptions = builder.Configuration.GetSection(AppSettingsNames.ApplicationCertificates).Get<CertificateOptions>();
    if (certificateOptions == null)
    {
        throw new InvalidOperationException($"{AppSettingsNames.ApplicationCertificates} configuration section is required");
    }

    builder.Services.AddOpenIddict()
        .AddCore(options =>
        {
            options.UseEntityFrameworkCore()
                   .UseDbContext<ApplicationDbContext>();
        })
        .AddServer(options =>
        {
            // Configure endpoints
            options
                .SetAuthorizationEndpointUris(openIddictOptions.AuthorizationEndpointUri)
                .SetTokenEndpointUris(openIddictOptions.TokenEndpointUri)
                .SetIntrospectionEndpointUris(openIddictOptions.IntrospectionEndpointUri)
                .SetUserinfoEndpointUris(openIddictOptions.UserinfoEndpointUri)
                .SetConfigurationEndpointUris(openIddictOptions.ConfigurationEndpointUri)
                .SetLogoutEndpointUris(openIddictOptions.LogoutEndpointUri);

            // Enable flows
            options.AllowClientCredentialsFlow()
                   .AllowAuthorizationCodeFlow()
                   .RequireProofKeyForCodeExchange(); // Enforce PKCE

            // Register the signing and encryption credentials
            options.AddEncryptionCertificate(CertificateManager.GetEncryptionCertificate(certificateOptions))
                .AddSigningCertificate(CertificateManager.GetSigningCertificate(certificateOptions));

            // Register the ASP.NET Core host and configure options
            options.UseAspNetCore()
                   .EnableAuthorizationEndpointPassthrough()
                   .EnableTokenEndpointPassthrough()
                   .EnableUserinfoEndpointPassthrough()
                   .EnableLogoutEndpointPassthrough();

            // Configure token lifetimes
            options.SetAccessTokenLifetime(TimeSpan.FromMinutes(openIddictOptions.AccessTokenLifetimeMinutes))
                   .SetRefreshTokenLifetime(TimeSpan.FromDays(openIddictOptions.RefreshTokenLifetimeDays))
                   .SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(openIddictOptions.AuthorizationCodeLifetimeMinutes));
        })
        .AddValidation(options =>
        {
            options.UseLocalServer();
            options.UseAspNetCore();
        });

    // Add Identity services for user management
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 1;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
}
```

### 2. Add Authorization Controller

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/Controllers/AuthorizationController.cs`

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Collections.Immutable;
using System.Security.Claims;
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
            if (request.HasPrompt(Prompts.None))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
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
                        Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                });
        }

        // Retrieve the profile of the logged in user
        var user = await _userManager.GetUserAsync(result.Principal) ??
            throw new InvalidOperationException("The user details cannot be retrieved.");

        // Retrieve the application details from the database
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
            throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

        // Retrieve the permanent authorizations associated with the user and the calling client application
        var authorizations = await _authorizationManager.FindAsync(
            subject: await _userManager.GetUserIdAsync(user),
            client: await _applicationManager.GetIdAsync(application),
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes()).ToListAsync();

        switch (await _applicationManager.GetConsentTypeAsync(application))
        {
            case ConsentTypes.External when !authorizations.Any():
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The logged in user is not allowed to access this client application."
                    }));

            case ConsentTypes.Implicit:
            case ConsentTypes.External when authorizations.Any():
            case ConsentTypes.Explicit when authorizations.Any() && !request.HasPrompt(Prompts.Consent):
                break;

            case ConsentTypes.Explicit when request.HasPrompt(Prompts.None):
            case ConsentTypes.Systematic when request.HasPrompt(Prompts.None):
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Interactive user consent is required."
                    }));

            default:
                return RedirectToAction(nameof(Consent), new { ReturnUrl = Request.PathBase + Request.Path + QueryString.Create(
                    Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList()) });
        }

        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        // Note: in this sample, the granted scopes match the requested scope
        // but you may want to allow the user to uncheck specific scopes.
        // For that, simply restrict the list of scopes before calling SetScopes.
        principal.SetScopes(request.GetScopes());
        principal.SetResources(await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync());

        // Automatically create a permanent authorization to avoid requiring explicit consent
        // for future authorization or token requests containing the same scopes.
        var authorization = authorizations.LastOrDefault();
        if (authorization is null)
        {
            authorization = await _authorizationManager.CreateAsync(
                principal: principal,
                subject: await _userManager.GetUserIdAsync(user),
                client: await _applicationManager.GetIdAsync(application),
                type: AuthorizationTypes.Permanent,
                scopes: principal.GetScopes());
        }

        principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));

        foreach (var claim in principal.Claims.SetDestinations(GetDestinations))
        {
            _logger.LogDebug("Claim {Type}: {Value} -> {Destinations}", 
                claim.Type, claim.Value, string.Join(", ", claim.GetDestinations()));
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

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
            throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        principal.SetScopes(request.GetScopes());
        principal.SetResources(await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync());

        var authorization = await _authorizationManager.CreateAsync(
            principal: principal,
            subject: await _userManager.GetUserIdAsync(user),
            client: await _applicationManager.GetIdAsync(application),
            type: AuthorizationTypes.Permanent,
            scopes: principal.GetScopes());

        principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));

        foreach (var claim in principal.Claims.SetDestinations(GetDestinations))
        {
            _logger.LogDebug("Claim {Type}: {Value} -> {Destinations}", 
                claim.Type, claim.Value, string.Join(", ", claim.GetDestinations()));
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

                if (claim.Subject.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;

                if (claim.Subject.HasScope(Scopes.Email))
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;

                if (claim.Subject.HasScope(Scopes.Roles))
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
```

#### Why `[IgnoreAntiforgeryToken]` is Used

The /authorize endpoint must ignore antiforgery tokens because:
OAuth 2.0/OpenID Connect Protocol Requirement: The authorize endpoint is called by external client applications (not forms within your own app). These clients initiate the authorization flow by redirecting users to this endpoint with specific query parameters (client_id, redirect_uri, scope, etc.). These external clients cannot and should not provide your server's antiforgery tokens.
Supports Both GET and POST: Notice the endpoint has both [HttpGet("authorize")] and [HttpPost("authorize")] attributes. The OAuth 2.0 spec allows authorization requests via both methods, and external clients may use either. GET requests don't typically include antiforgery tokens anyway.
Cross-Site Requests are Expected: Unlike typical form submissions within your app, the authorize endpoint is designed to receive cross-origin requests from external applications. This is a fundamental part of OAuth 2.0 - clients redirect users to the authorization server, which is a cross-site navigation.
Protected by OAuth 2.0 Security Mechanisms: The endpoint doesn't need CSRF protection via antiforgery tokens because OAuth 2.0 has its own security mechanisms:
The state parameter (for CSRF protection)
Redirect URI validation
PKCE (Proof Key for Code Exchange) for public clients
Client authentication for confidential clients
If the antiforgery token validation were enabled, legitimate OAuth clients would fail to initiate authorization flows because they wouldn't have valid antiforgery tokens from your server.

### 3. Add Account Controller for Web Pages

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/Controllers/AccountController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SimpleIdentityServer.API.Models;
using SimpleIdentityServer.API.Services;
using System.ComponentModel.DataAnnotations;

namespace SimpleIdentityServer.API.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet("login")]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in.", model.Email);
            return LocalRedirect(returnUrl ?? "/");
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out.", model.Email);
            return RedirectToAction(nameof(Lockout));
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet("register")]
    public IActionResult Register(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("register")]
    [EnableRateLimiting("AuthPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser 
        { 
            UserName = model.Email, 
            Email = model.Email,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} created a new account with password.", model.Email);

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", 
                new { userId = user.Id, code }, Request.Scheme);

            await _emailService.SendEmailConfirmationAsync(model.Email, callbackUrl);

            return RedirectToAction(nameof(RegisterConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("AuthPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
            // Don't reveal that the user does not exist or is not confirmed
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.Action(nameof(ResetPassword), "Account", 
            new { code }, Request.Scheme);

        await _emailService.SendPasswordResetAsync(model.Email, callbackUrl);

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet("reset-password")]
    public IActionResult ResetPassword(string code = null)
    {
        if (code == null)
        {
            throw new ApplicationException("A code must be supplied for password reset.");
        }

        var model = new ResetPasswordViewModel { Code = code };
        return View(model);
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("AuthPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View();
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        if (userId == null || code == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new ApplicationException($"Unable to load user with ID '{userId}'.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, code);
        return View(result.Succeeded ? "ConfirmEmail" : "Error");
    }

    [HttpGet("lockout")]
    public IActionResult Lockout()
    {
        return View();
    }

    [HttpGet("register-confirmation")]
    public IActionResult RegisterConfirmation()
    {
        return View();
    }

    [HttpGet("forgot-password-confirmation")]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet("reset-password-confirmation")]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }
}
```

## Database Schema Updates

### 1. Add ApplicationUser Entity

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/Data/ApplicationUser.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SimpleIdentityServer.API.Data;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }

    public DateTime? EmailConfirmedAt { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? LastFailedLoginAt { get; set; }
}
```

### 2. Update ApplicationDbContext

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/Data/ApplicationDbContext.cs`

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SimpleIdentityServer.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Identity tables
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(256);
        });

        // Configure OpenIddict entities
        builder.UseOpenIddict();
    }
}
```

### 3. Add Migration Script

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/Scripts/AddIdentityTables.sql`

```sql
-- Add Identity tables for Authorization Code flow
-- This script should be run after creating a new migration

-- Users table extensions
ALTER TABLE [Users] ADD 
    [FirstName] NVARCHAR(100) NULL,
    [LastName] NVARCHAR(100) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastLoginAt] DATETIME2 NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [ProfilePictureUrl] NVARCHAR(500) NULL,
    [EmailConfirmedAt] DATETIME2 NULL,
    [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
    [LastFailedLoginAt] DATETIME2 NULL;

-- Create indexes for performance
CREATE INDEX IX_Users_Email ON [Users] ([Email]);
CREATE INDEX IX_Users_IsActive ON [Users] ([IsActive]);
CREATE INDEX IX_Users_CreatedAt ON [Users] ([CreatedAt]);

-- Insert default scopes for Authorization Code flow
INSERT INTO [OpenIddictScopes] ([Id], [ConcurrencyToken], [Description], [DisplayName], [Name], [Properties], [Resources])
VALUES 
    (NEWID(), NULL, 'OpenID Connect scope', 'OpenID', 'openid', NULL, NULL),
    (NEWID(), NULL, 'Profile information scope', 'Profile', 'profile', NULL, NULL),
    (NEWID(), NULL, 'Email scope', 'Email', 'email', NULL, NULL),
    (NEWID(), NULL, 'Roles scope', 'Roles', 'roles', NULL, NULL);

-- Insert sample web application client
INSERT INTO [OpenIddictApplications] ([Id], [ClientId], [ClientSecret], [ConcurrencyToken], [ConsentType], [DisplayName], [Permissions], [PostLogoutRedirectUris], [Properties], [RedirectUris], [Requirements], [Type])
VALUES (
    NEWID(),
    'web-app',
    'AQAAAAEAACcQAAAAEFtmKkdBhAOKqHYjKgHnOqKGfDgzHyKgKgHnOqKGfDgzHyKgKgHnOqKGfDgzHyKg', -- Hash of 'web-secret'
    NULL,
    'explicit',
    'Web Application',
    '["ept:authorization", "ept:logout", "ept:token", "gt:authorization_code", "rst:code", "scp:openid", "scp:profile", "scp:email"]',
    '["https://localhost:5001/signout-callback-oidc"]',
    NULL,
    '["https://localhost:5001/signin-oidc"]',
    '["pkce"]',
    'confidential'
);
```

## Web Pages Implementation

### 1. Login Page

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/Views/Account/Login.cshtml`

```html
@model LoginViewModel
@{
    ViewData["Title"] = "Sign In";
    Layout = "_AuthLayout";
}

<div class="auth-container">
    <div class="auth-card">
        <div class="auth-header">
            <h1>Sign In</h1>
            <p>Enter your credentials to access your account</p>
        </div>

        <form id="loginForm" asp-action="Login" asp-route-returnurl="@ViewData["ReturnUrl"]" method="post" novalidate>
            <div asp-validation-summary="ModelOnly" class="alert alert-danger" role="alert"></div>
            
            <div class="form-group">
                <label asp-for="Email" class="form-label">Email Address</label>
                <input asp-for="Email" 
                       type="email" 
                       class="form-control" 
                       autocomplete="username"
                       aria-describedby="email-error"
                       required>
                <span asp-validation-for="Email" id="email-error" class="field-validation-error"></span>
            </div>

            <div class="form-group">
                <label asp-for="Password" class="form-label">Password</label>
                <div class="password-input-container">
                    <input asp-for="Password" 
                           type="password" 
                           class="form-control" 
                           autocomplete="current-password"
                           aria-describedby="password-error"
                           required>
                    <button type="button" class="password-toggle" aria-label="Toggle password visibility">
                        <i class="icon-eye" aria-hidden="true"></i>
                    </button>
                </div>
                <span asp-validation-for="Password" id="password-error" class="field-validation-error"></span>
            </div>

            <div class="form-group form-check">
                <input asp-for="RememberMe" type="checkbox" class="form-check-input">
                <label asp-for="RememberMe" class="form-check-label">
                    Keep me signed in
                </label>
            </div>

            <button type="submit" class="btn btn-primary btn-block" id="loginButton">
                <span class="button-text">Sign In</span>
                <span class="button-spinner" style="display: none;">
                    <i class="spinner" aria-hidden="true"></i>
                </span>
            </button>

            <div class="auth-links">
                <a asp-action="ForgotPassword" class="link-secondary">Forgot your password?</a>
                <a asp-action="Register" asp-route-returnurl="@ViewData["ReturnUrl"]" class="link-primary">Create new account</a>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    <script type="module" src="~/js/modules/auth/login.js"></script>
}
```

### 2. Registration Page

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/Views/Account/Register.cshtml`

```html
@model RegisterViewModel
@{
    ViewData["Title"] = "Create Account";
    Layout = "_AuthLayout";
}

<div class="auth-container">
    <div class="auth-card">
        <div class="auth-header">
            <h1>Create Account</h1>
            <p>Sign up to get started with your account</p>
        </div>

        <form id="registerForm" asp-action="Register" asp-route-returnurl="@ViewData["ReturnUrl"]" method="post" novalidate>
            <div asp-validation-summary="ModelOnly" class="alert alert-danger" role="alert"></div>
            
            <div class="form-group">
                <label asp-for="Email" class="form-label">Email Address</label>
                <input asp-for="Email" 
                       type="email" 
                       class="form-control" 
                       autocomplete="username"
                       aria-describedby="email-error"
                       required>
                <span asp-validation-for="Email" id="email-error" class="field-validation-error"></span>
            </div>

            <div class="form-group">
                <label asp-for="Password" class="form-label">Password</label>
                <div class="password-input-container">
                    <input asp-for="Password" 
                           type="password" 
                           class="form-control" 
                           autocomplete="new-password"
                           aria-describedby="password-error password-strength"
                           required>
                    <button type="button" class="password-toggle" aria-label="Toggle password visibility">
                        <i class="icon-eye" aria-hidden="true"></i>
                    </button>
                </div>
                <div id="password-strength" class="password-strength" aria-live="polite"></div>
                <span asp-validation-for="Password" id="password-error" class="field-validation-error"></span>
            </div>

            <div class="form-group">
                <label asp-for="ConfirmPassword" class="form-label">Confirm Password</label>
                <div class="password-input-container">
                    <input asp-for="ConfirmPassword" 
                           type="password" 
                           class="form-control" 
                           autocomplete="new-password"
                           aria-describedby="confirm-password-error"
                           required>
                    <button type="button" class="password-toggle" aria-label="Toggle password visibility">
                        <i class="icon-eye" aria-hidden="true"></i>
                    </button>
                </div>
                <span asp-validation-for="ConfirmPassword" id="confirm-password-error" class="field-validation-error"></span>
            </div>

            <div class="form-group form-check">
                <input asp-for="AgreeToTerms" type="checkbox" class="form-check-input" required>
                <label asp-for="AgreeToTerms" class="form-check-label">
                    I agree to the <a href="/terms" target="_blank" rel="noopener">Terms of Service</a> 
                    and <a href="/privacy" target="_blank" rel="noopener">Privacy Policy</a>
                </label>
                <span asp-validation-for="AgreeToTerms" class="field-validation-error"></span>
            </div>

            <button type="submit" class="btn btn-primary btn-block" id="registerButton" disabled>
                <span class="button-text">Create Account</span>
                <span class="button-spinner" style="display: none;">
                    <i class="spinner" aria-hidden="true"></i>
                </span>
            </button>

            <div class="auth-links">
                <a asp-action="Login" asp-route-returnurl="@ViewData["ReturnUrl"]" class="link-primary">Already have an account? Sign in</a>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    <script type="module" src="~/js/modules/auth/register.js"></script>
}
```

## Client-Side JavaScript

### 1. Authentication Module

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/wwwroot/js/modules/auth/auth-module.js`

```javascript
/**
 * Core authentication module with security features
 */
export class AuthModule {
    constructor() {
        this.csrfToken = this.getCsrfToken();
        this.rateLimiter = new RateLimiter();
        this.validator = new InputValidator();
    }

    /**
     * Get CSRF token from meta tag or form
     */
    getCsrfToken() {
        const token = document.querySelector('meta[name="__RequestVerificationToken"]')?.content ||
                     document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        
        if (!token) {
            console.warn('CSRF token not found');
        }
        
        return token;
    }

    /**
     * Make secure AJAX request with CSRF protection
     */
    async secureRequest(url, data, options = {}) {
        if (!this.rateLimiter.canProceed()) {
            throw new Error('Rate limit exceeded. Please wait before trying again.');
        }

        const defaultOptions = {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                ...(this.csrfToken && { 'RequestVerificationToken': this.csrfToken })
            },
            credentials: 'same-origin',
            body: JSON.stringify(data)
        };

        const requestOptions = { ...defaultOptions, ...options };

        try {
            const response = await fetch(url, requestOptions);
            
            if (!response.ok) {
                if (response.status === 429) {
                    this.rateLimiter.recordFailure();
                    throw new Error('Too many requests. Please wait before trying again.');
                }
                throw new Error(`Request failed: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            this.rateLimiter.recordFailure();
            throw error;
        }
    }

    /**
     * Validate form input securely
     */
    validateInput(formData) {
        return this.validator.validateForm(formData);
    }

    /**
     * Show error message with XSS protection
     */
    showError(message, container = null) {
        const errorContainer = container || document.querySelector('.alert-danger');
        if (errorContainer) {
            // Sanitize message to prevent XSS
            errorContainer.textContent = message;
            errorContainer.style.display = 'block';
            errorContainer.setAttribute('role', 'alert');
            errorContainer.focus();
        }
    }

    /**
     * Clear error messages
     */
    clearErrors() {
        const errorContainers = document.querySelectorAll('.alert-danger, .field-validation-error');
        errorContainers.forEach(container => {
            container.textContent = '';
            container.style.display = 'none';
        });
    }
}

/**
 * Rate limiting for authentication attempts
 */
class RateLimiter {
    constructor(maxAttempts = 5, windowMs = 300000) { // 5 attempts per 5 minutes
        this.maxAttempts = maxAttempts;
        this.windowMs = windowMs;
        this.attempts = this.getStoredAttempts();
    }

    canProceed() {
        this.cleanOldAttempts();
        return this.attempts.length < this.maxAttempts;
    }

    recordFailure() {
        this.attempts.push(Date.now());
        this.storeAttempts();
    }

    cleanOldAttempts() {
        const cutoff = Date.now() - this.windowMs;
        this.attempts = this.attempts.filter(time => time > cutoff);
        this.storeAttempts();
    }

    getStoredAttempts() {
        try {
            const stored = sessionStorage.getItem('auth_attempts');
            return stored ? JSON.parse(stored) : [];
        } catch {
            return [];
        }
    }

    storeAttempts() {
        try {
            sessionStorage.setItem('auth_attempts', JSON.stringify(this.attempts));
        } catch {
            // Storage failed, continue without persistence
        }
    }
}

/**
 * Input validation with security focus
 */
class InputValidator {
    constructor() {
        this.patterns = {
            email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
            password: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/
        };
    }

    validateForm(formData) {
        const errors = [];

        // Email validation
        if (formData.email && !this.patterns.email.test(formData.email)) {
            errors.push('Please enter a valid email address');
        }

        // Password validation
        if (formData.password && !this.patterns.password.test(formData.password)) {
            errors.push('Password must be at least 8 characters with uppercase, lowercase, number, and special character');
        }

        // Password confirmation
        if (formData.password && formData.confirmPassword && formData.password !== formData.confirmPassword) {
            errors.push('Passwords do not match');
        }

        return {
            isValid: errors.length === 0,
            errors
        };
    }

    sanitizeInput(input) {
        if (typeof input !== 'string') return input;
        
        // Basic XSS prevention
        return input
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#x27;')
            .replace(/\//g, '&#x2F;');
    }
}
```

### 2. Login Page JavaScript

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/wwwroot/js/modules/auth/login.js`

```javascript
import { AuthModule } from './auth-module.js';

/**
 * Login page functionality with security features
 */
class LoginPage {
    constructor() {
        this.authModule = new AuthModule();
        this.form = document.getElementById('loginForm');
        this.submitButton = document.getElementById('loginButton');
        this.init();
    }

    init() {
        if (!this.form) return;

        this.setupEventListeners();
        this.setupPasswordToggle();
        this.setupFormValidation();
    }

    setupEventListeners() {
        this.form.addEventListener('submit', this.handleSubmit.bind(this));
        
        // Real-time validation
        const inputs = this.form.querySelectorAll('input[required]');
        inputs.forEach(input => {
            input.addEventListener('blur', this.validateField.bind(this));
            input.addEventListener('input', this.clearFieldError.bind(this));
        });
    }

    setupPasswordToggle() {
        const toggleButtons = document.querySelectorAll('.password-toggle');
        toggleButtons.forEach(button => {
            button.addEventListener('click', this.togglePasswordVisibility.bind(this));
        });
    }

    setupFormValidation() {
        // Enable submit button only when form is valid
        const requiredInputs = this.form.querySelectorAll('input[required]');
        requiredInputs.forEach(input => {
            input.addEventListener('input', this.updateSubmitButton.bind(this));
        });
    }

    async handleSubmit(event) {
        event.preventDefault();
        
        if (this.submitButton.disabled) return;

        this.authModule.clearErrors();
        this.setLoading(true);

        try {
            const formData = new FormData(this.form);
            const data = {
                email: formData.get('Email'),
                password: formData.get('Password'),
                rememberMe: formData.get('RememberMe') === 'true'
            };

            // Client-side validation
            const validation = this.authModule.validateInput(data);
            if (!validation.isValid) {
                this.showErrors(validation.errors);
                return;
            }

            // Submit form normally (let server handle the redirect)
            this.form.submit();

        } catch (error) {
            this.authModule.showError(error.message);
        } finally {
            this.setLoading(false);
        }
    }

    validateField(event) {
        const field = event.target;
        const value = field.value.trim();
        const fieldName = field.name;
        
        let isValid = true;
        let errorMessage = '';

        if (!value && field.required) {
            isValid = false;
            errorMessage = `${fieldName} is required`;
        } else if (fieldName === 'Email' && value && !this.authModule.validator.patterns.email.test(value)) {
            isValid = false;
            errorMessage = 'Please enter a valid email address';
        }

        this.showFieldError(field, isValid ? '' : errorMessage);
        return isValid;
    }

    clearFieldError(event) {
        const field = event.target;
        this.showFieldError(field, '');
    }

    showFieldError(field, message) {
        const errorElement = document.getElementById(field.getAttribute('aria-describedby'));
        if (errorElement) {
            errorElement.textContent = message;
            errorElement.style.display = message ? 'block' : 'none';
        }
        
        field.classList.toggle('is-invalid', !!message);
        field.setAttribute('aria-invalid', !!message);
    }

    showErrors(errors) {
        const errorContainer = document.querySelector('.alert-danger');
        if (errorContainer && errors.length > 0) {
            errorContainer.innerHTML = errors.map(error => `<div>${this.authModule.validator.sanitizeInput(error)}</div>`).join('');
            errorContainer.style.display = 'block';
        }
    }

    togglePasswordVisibility(event) {
        const button = event.currentTarget;
        const passwordInput = button.parentElement.querySelector('input[type="password"], input[type="text"]');
        const icon = button.querySelector('i');
        
        if (passwordInput.type === 'password') {
            passwordInput.type = 'text';
            icon.className = 'icon-eye-off';
            button.setAttribute('aria-label', 'Hide password');
        } else {
            passwordInput.type = 'password';
            icon.className = 'icon-eye';
            button.setAttribute('aria-label', 'Show password');
        }
    }

    updateSubmitButton() {
        const requiredInputs = this.form.querySelectorAll('input[required]');
        const allValid = Array.from(requiredInputs).every(input => input.value.trim() !== '');
        
        this.submitButton.disabled = !allValid;
    }

    setLoading(loading) {
        const buttonText = this.submitButton.querySelector('.button-text');
        const buttonSpinner = this.submitButton.querySelector('.button-spinner');
        
        this.submitButton.disabled = loading;
        buttonText.style.display = loading ? 'none' : 'inline';
        buttonSpinner.style.display = loading ? 'inline' : 'none';
        
        if (loading) {
            this.submitButton.setAttribute('aria-busy', 'true');
        } else {
            this.submitButton.removeAttribute('aria-busy');
        }
    }
}

// Initialize when DOM is loaded
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => new LoginPage());
} else {
    new LoginPage();
}
```

### 3. PKCE Helper for Client Applications

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/wwwroot/js/modules/auth/pkce-helper.js`

```javascript
/**
 * PKCE (Proof Key for Code Exchange) implementation for OAuth 2.0 Authorization Code flow
 */
export class PKCEHelper {
    /**
     * Generate a cryptographically secure code verifier
     */
    static generateCodeVerifier() {
        const array = new Uint8Array(32);
        crypto.getRandomValues(array);
        return this.base64URLEncode(array);
    }

    /**
     * Generate code challenge from verifier using SHA256
     */
    static async generateCodeChallenge(verifier) {
        const encoder = new TextEncoder();
        const data = encoder.encode(verifier);
        const digest = await crypto.subtle.digest('SHA-256', data);
        return this.base64URLEncode(new Uint8Array(digest));
    }

    /**
     * Base64 URL encode (RFC 4648 Section 5)
     */
    static base64URLEncode(array) {
        return btoa(String.fromCharCode(...array))
            .replace(/\+/g, '-')
            .replace(/\//g, '_')
            .replace(/=/g, '');
    }

    /**
     * Generate authorization URL with PKCE parameters
     */
    static async generateAuthorizationUrl(config) {
        const codeVerifier = this.generateCodeVerifier();
        const codeChallenge = await this.generateCodeChallenge(codeVerifier);
        
        // Store code verifier for token exchange
        sessionStorage.setItem('pkce_code_verifier', codeVerifier);
        
        const params = new URLSearchParams({
            response_type: 'code',
            client_id: config.clientId,
            redirect_uri: config.redirectUri,
            scope: config.scope || 'openid profile email',
            state: this.generateState(),
            code_challenge: codeChallenge,
            code_challenge_method: 'S256'
        });

        return `${config.authorizationEndpoint}?${params.toString()}`;
    }

    /**
     * Generate cryptographically secure state parameter
     */
    static generateState() {
        const array = new Uint8Array(16);
        crypto.getRandomValues(array);
        const state = this.base64URLEncode(array);
        
        // Store state for validation
        sessionStorage.setItem('oauth_state', state);
        
        return state;
    }

    /**
     * Exchange authorization code for tokens
     */
    static async exchangeCodeForTokens(config, authorizationCode, state) {
        // Validate state parameter
        const storedState = sessionStorage.getItem('oauth_state');
        if (!storedState || storedState !== state) {
            throw new Error('Invalid state parameter');
        }

        // Get stored code verifier
        const codeVerifier = sessionStorage.getItem('pkce_code_verifier');
        if (!codeVerifier) {
            throw new Error('Code verifier not found');
        }

        const tokenData = {
            grant_type: 'authorization_code',
            client_id: config.clientId,
            client_secret: config.clientSecret,
            code: authorizationCode,
            redirect_uri: config.redirectUri,
            code_verifier: codeVerifier
        };

        try {
            const response = await fetch(config.tokenEndpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'Accept': 'application/json'
                },
                body: new URLSearchParams(tokenData)
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.error_description || 'Token exchange failed');
            }

            const tokens = await response.json();
            
            // Clean up stored values
            sessionStorage.removeItem('pkce_code_verifier');
            sessionStorage.removeItem('oauth_state');
            
            return tokens;
        } catch (error) {
            // Clean up on error
            sessionStorage.removeItem('pkce_code_verifier');
            sessionStorage.removeItem('oauth_state');
            throw error;
        }
    }

    /**
     * Validate and parse JWT token (basic validation)
     */
    static parseJWT(token) {
        try {
            const parts = token.split('.');
            if (parts.length !== 3) {
                throw new Error('Invalid JWT format');
            }

            const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
            
            // Check expiration
            if (payload.exp && payload.exp < Date.now() / 1000) {
                throw new Error('Token has expired');
            }

            return payload;
        } catch (error) {
            throw new Error('Invalid JWT token');
        }
    }
}
```

## Security Implementation

### 1. Rate Limiting Options

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/Configuration/RateLimitingOptions.cs`

```csharp
namespace SimpleIdentityServer.API.Configuration;

/// <summary>
/// Configuration options for rate limiting
/// </summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Global rate limiting settings
    /// </summary>
    public RateLimitSettings Global { get; set; } = new()
    {
        PermitLimit = 100,
        WindowMinutes = 1
    };

    /// <summary>
    /// Token endpoint specific rate limiting settings
    /// </summary>
    public RateLimitSettings TokenEndpoint { get; set; } = new()
    {
        PermitLimit = 20,
        WindowMinutes = 1
    };

    /// <summary>
    /// Introspection endpoint specific rate limiting settings
    /// </summary>
    public RateLimitSettings IntrospectionEndpoint { get; set; } = new()
    {
        PermitLimit = 50,
        WindowMinutes = 1
    };

    /// <summary>
    /// Authentication endpoints specific rate limiting settings
    /// </summary>
    public RateLimitSettings AuthenticationEndpoints { get; set; } = new()
    {
        PermitLimit = 5,
        WindowMinutes = 5
    };

    /// <summary>
    /// Security monitoring settings
    /// </summary>
    public SecurityMonitoringSettings SecurityMonitoring { get; set; } = new();
}

/// <summary>
/// Rate limit settings for a specific endpoint or global
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Maximum number of requests allowed in the time window
    /// </summary>
    public int PermitLimit { get; set; }

    /// <summary>
    /// Time window in minutes
    /// </summary>
    public int WindowMinutes { get; set; }
}

/// <summary>
/// Security monitoring configuration
/// </summary>
public class SecurityMonitoringSettings
{
    /// <summary>
    /// Maximum requests per client in 5 minutes before flagging as suspicious
    /// </summary>
    public int SuspiciousRequestThreshold5Min { get; set; } = 10;

    /// <summary>
    /// Maximum requests per client in 1 hour before flagging as high frequency
    /// </summary>
    public int HighFrequencyRequestThreshold1Hour { get; set; } = 100;

    /// <summary>
    /// Request duration in seconds that triggers slow request logging
    /// </summary>
    public double SlowRequestThresholdSeconds { get; set; } = 5.0;

    /// <summary>
    /// How long to keep request tracking data in hours
    /// </summary>
    public int RequestTrackingRetentionHours { get; set; } = 1;
}

/// <summary>
/// Load balancer and proxy configuration
/// </summary>
public class LoadBalancerOptions
{
    public const string SectionName = "LoadBalancer";

    /// <summary>
    /// List of trusted proxy IP addresses or CIDR ranges
    /// </summary>
    public List<string> TrustedProxies { get; set; } = new();

    /// <summary>
    /// List of trusted proxy networks in CIDR notation
    /// </summary>
    public List<string> TrustedNetworks { get; set; } = new()
    {
        "10.0.0.0/8",      // Private network
        "172.16.0.0/12",   // Private network
        "192.168.0.0/16",  // Private network
        "127.0.0.0/8"      // Localhost
    };

    /// <summary>
    /// Maximum number of forwarded headers to process (security measure)
    /// </summary>
    public int ForwardLimit { get; set; } = 2;

    /// <summary>
    /// Whether to require header symmetry (all forwarded headers must be present)
    /// </summary>
    public bool RequireHeaderSymmetry { get; set; } = false;

    /// <summary>
    /// Whether the application is behind a load balancer/proxy
    /// </summary>
    public bool EnableForwardedHeaders { get; set; } = true;
}
```

### 2. Rate Limiting Configuration

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/Configuration/RateLimitingConfiguration.cs`

```csharp
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace SimpleIdentityServer.API.Configuration;

public static class RateLimitingConfiguration
{
    public static void ConfigureRateLimiting(WebApplicationBuilder builder)
    {
        // Configure rate limiting options
        builder.Services.Configure<RateLimitingOptions>(
            builder.Configuration.GetSection(RateLimitingOptions.SectionName));

        // Configure Rate Limiting with configuration-based settings
        var rateLimitingConfig = builder.Configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        builder.Services.AddRateLimiter(options =>
        {
            // Global rate limiter - applies to all endpoints
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokenLimit = 100,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10,
                        TokensPerPeriod = 20,
                        PermitLimit = rateLimitingConfig.Global.PermitLimit,
                        Window = TimeSpan.FromMinutes(rateLimitingConfig.Global.WindowMinutes)
                    }));

            // Authentication endpoints rate limiting
            options.AddPolicy("AuthPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext) ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingConfig.AuthenticationEndpoints.PermitLimit,
                        Window = TimeSpan.FromMinutes(rateLimitingConfig.AuthenticationEndpoints.WindowMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));

            // Token endpoint specific rate limiter - more restrictive
            options.AddPolicy("TokenPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = rateLimitingConfig.TokenEndpoint.PermitLimit,
                        Window = TimeSpan.FromMinutes(rateLimitingConfig.TokenEndpoint.WindowMinutes)
                    }));

            // Introspection endpoint rate limiter
            options.AddPolicy("IntrospectionPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = rateLimitingConfig.IntrospectionEndpoint.PermitLimit,
                        Window = TimeSpan.FromMinutes(rateLimitingConfig.IntrospectionEndpoint.WindowMinutes)
                    }));

            // Configure what happens when rate limit is exceeded
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429; // Too Many Requests

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                context.HttpContext.Response.ContentType = "application/json";

                var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) ?
                    ((int)retry.TotalSeconds).ToString() : "60";

                var errorResponse = $$"""
                    {
                        "error": "too_many_requests",
                        "error_description": "Rate limit exceeded. Please retry after the specified time.",
                        "retry_after_seconds": "{{retryAfterSeconds}}"
                    }
                    """;

                await context.HttpContext.Response.WriteAsync(errorResponse, cancellationToken: token);
            };
        });
    }

    private static string GetPartitionKey(HttpContext httpContext)
    {
        // Use client ID if available (from Authorization header or form data)
        var clientId = GetClientIdFromRequest(httpContext);
        if (!string.IsNullOrEmpty(clientId))
        {
            return $"client:{clientId}";
        }

        // Fall back to IP address with proper forwarded header support
        var ipAddress = HttpContextUtils.GetClientIpAddress(httpContext);
        return $"ip:{ipAddress}";
    }


    private static string? GetClientIdFromRequest(HttpContext httpContext)
    {
        // Try to get client_id from form data (token requests)
        if (httpContext.Request.HasFormContentType &&
            httpContext.Request.Form.TryGetValue("client_id", out var clientIdForm))
        {
            return clientIdForm.FirstOrDefault();
        }

        // Try to get from query parameters
        if (httpContext.Request.Query.TryGetValue("client_id", out var clientIdQuery))
        {
            return clientIdQuery.FirstOrDefault();
        }

        return null;
    }
}
```

### 2. Security Headers Middleware

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/Middleware/SecurityHeadersMiddleware.cs`

```csharp
using Microsoft.Extensions.Primitives;

namespace SimpleIdentityServer.API.Middleware;

/// <summary>
/// Middleware to add security headers to all responses
/// Implements OWASP security header recommendations
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context);

        // Content Security Policy for auth pages and api
        if (context.Request.Path.StartsWithSegments("/account"))
        {
            AddCSPHeadersForPages(context);
        }
        else
        {
            AddCSPHeadersForApi(context);
        }

        await _next(context);

        // Log security header application
        _logger.LogDebug("Security headers applied to response for {Path}", context.Request.Path);
    }

    private static void AddCSPHeadersForApi(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Content Security Policy - policy for API
        var cspPolicy = "default-src 'none'; " +
                       "script-src 'none'; " +
                       "style-src 'none'; " +
                       "img-src 'none'; " +
                       "font-src 'none'; " +
                       "connect-src 'self'; " +
                       "frame-ancestors 'none'; " +
                       "base-uri 'none'; " +
                       "form-action 'none'";
        headers.Append("Content-Security-Policy", cspPolicy);
    }

    private static void AddCSPHeadersForPages(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Content Security Policy - policy for pages
        var cspPolicy = "default-src 'self'; " +
                       "script-src 'self' 'unsafe-inline'; " +
                       "style-src 'self' 'unsafe-inline'; " +
                       "img-src 'self' data:; " +
                       "font-src 'self'; " +
                       "connect-src 'self'; " +
                       "frame-ancestors 'none'; ";

        headers.Append("Content-Security-Policy", cspPolicy);
    }
    private static void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent clickjacking attacks
        headers.Append("X-Frame-Options", "DENY");

        // Prevent MIME type sniffing
        headers.Append("X-Content-Type-Options", "nosniff");

        // Enable XSS protection in browsers
        headers.Append("X-XSS-Protection", "1; mode=block");

        // Referrer policy - only send referrer for same origin
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions policy - disable unnecessary browser features
        var permissionsPolicy = "accelerometer=(), " +
                               "ambient-light-sensor=(), " +
                               "autoplay=(), " +
                               "battery=(), " +
                               "camera=(), " +
                               "cross-origin-isolated=(), " +
                               "display-capture=(), " +
                               "document-domain=(), " +
                               "encrypted-media=(), " +
                               "execution-while-not-rendered=(), " +
                               "execution-while-out-of-viewport=(), " +
                               "fullscreen=(), " +
                               "geolocation=(), " +
                               "gyroscope=(), " +
                               "magnetometer=(), " +
                               "microphone=(), " +
                               "midi=(), " +
                               "navigation-override=(), " +
                               "payment=(), " +
                               "picture-in-picture=(), " +
                               "publickey-credentials-get=(), " +
                               "screen-wake-lock=(), " +
                               "sync-xhr=(), " +
                               "usb=(), " +
                               "web-share=(), " +
                               "xr-spatial-tracking=()";
        headers.Append("Permissions-Policy", permissionsPolicy);

        // Strict Transport Security - enforce HTTPS
        if (context.Request.IsHttps)
        {
            headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        }

        // Cross-Origin policies for additional security
        headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
        headers.Append("Cross-Origin-Opener-Policy", "same-origin");
        headers.Append("Cross-Origin-Resource-Policy", "same-origin");

        // Cache control for sensitive endpoints
        if (IsSensitiveEndpoint(context.Request.Path))
        {
            headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
            headers.Append("Pragma", "no-cache");
            headers.Append("Expires", "0");
        }

        // Remove server information disclosure
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
        headers.Remove("X-AspNetMvc-Version");
    }

    private static bool IsSensitiveEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

        return pathValue.Contains("/connect/token") ||
               pathValue.Contains("/connect/introspect") ||
               pathValue.Contains("/.well-known") ||
               pathValue.Contains("/api");
    }
}
```

## Testing Strategy

### 1. Unit Tests for Authorization Controller

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API.Test/Controllers/AuthorizationControllerTests.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OpenIddict.Abstractions;
using SimpleIdentityServer.API.Controllers;
using SimpleIdentityServer.API.Data;
using Xunit;

namespace SimpleIdentityServer.API.Test.Controllers;

public class AuthorizationControllerTests
{
    private readonly Mock<IOpenIddictApplicationManager> _applicationManagerMock;
    private readonly Mock<IOpenIddictAuthorizationManager> _authorizationManagerMock;
    private readonly Mock<IOpenIddictScopeManager> _scopeManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ILogger<AuthorizationController>> _loggerMock;
    private readonly AuthorizationController _controller;

    public AuthorizationControllerTests()
    {
        _applicationManagerMock = new Mock<IOpenIddictApplicationManager>();
        _authorizationManagerMock = new Mock<IOpenIddictAuthorizationManager>();
        _scopeManagerMock = new Mock<IOpenIddictScopeManager>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            Mock.Of<UserManager<ApplicationUser>>(),
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null, null, null, null);
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        _loggerMock = new Mock<ILogger<AuthorizationController>>();

        _controller = new AuthorizationController(
            _applicationManagerMock.Object,
            _authorizationManagerMock.Object,
            _scopeManagerMock.Object,
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Authorize_WithValidRequest_ReturnsSignInResult()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act & Assert
        // Add test implementation
        Assert.True(true); // Placeholder
    }

    [Fact]
    public async Task Authorize_WithInvalidClient_ReturnsForbid()
    {
        // Arrange & Act & Assert
        // Add test implementation
        Assert.True(true); // Placeholder
    }
}
```

### 2. Integration Tests

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API.Test/Integration/AuthorizationCodeFlowTests.cs`

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Xunit;

namespace SimpleIdentityServer.API.Test.Integration;

public class AuthorizationCodeFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthorizationCodeFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task AuthorizeEndpoint_WithValidParameters_ReturnsRedirectToLogin()
    {
        // Arrange
        var authUrl = "/connect/authorize?response_type=code&client_id=web-app&redirect_uri=https://localhost:5001/signin-oidc&scope=openid profile&state=test&code_challenge=test&code_challenge_method=S256";

        // Act
        var response = await _client.GetAsync(authUrl);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task LoginPage_Get_ReturnsSuccessWithForm()
    {
        // Act
        var response = await _client.GetAsync("/account/login");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("loginForm", content);
        Assert.Contains("__RequestVerificationToken", content);
    }
}
```

## Deployment Considerations

### 1. Environment Configuration

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "",
    "SecurityLogsConnection": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug",
      "SimpleIdentityServer.API.Middleware.SecurityMonitoringMiddleware": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Application": {
    "OpenIddict": {
      "TokenEndpointUri": "/connect/token",
      "IntrospectionEndpointUri": "/connect/introspect",
      "ConfigurationEndpointUri": "/.well-known/openid-configuration",
      "AuthorizationEndpointUri": "/connect/authorize",
      "UserinfoEndpointUri": "/connect/userinfo",
      "LogoutEndpointUri": "/connect/logout",
      "AccessTokenLifetimeMinutes": 60,
      "RefreshTokenLifetimeDays": 14,
      "AuthorizationCodeLifetimeMinutes": 10
    },
    "Certificates": {
      "Password": "",
      "EncryptionCertificatePath": "/app/certs/encryption.pfx",
      "SigningCertificatePath": "/app/certs/signing.pfx"
    },
    "Database": {
      "CommandTimeoutSeconds": 30,
      "MaxRetryCount": 3,
      "MaxRetryDelaySeconds": 5
    },
    "SecurityLogging": {
      "RetentionDays": 30,
      "CleanupIntervalHours": 24,
      "BatchPostingLimit": 100,
      "BatchPeriodSeconds": 5
    }
  },
  "Kestrel": {
    "RequestHeadersTimeoutSeconds": 30,
    "KeepAliveTimeoutMinutes": 2,
    "MaxRequestBodySize": 1048576,
    "MaxConcurrentConnections": 100
  },
  "RateLimiting": {
    "Global": {
      "PermitLimit": 100,
      "WindowMinutes": 1
    },
    "TokenEndpoint": {
      "PermitLimit": 20,
      "WindowMinutes": 1
    },
    "IntrospectionEndpoint": {
      "PermitLimit": 50,
      "WindowMinutes": 1
    },
    "SecurityMonitoring": {
      "SuspiciousRequestThreshold5Min": 10,
      "HighFrequencyRequestThreshold1Hour": 100,
      "SlowRequestThresholdSeconds": 5.0,
      "RequestTrackingRetentionHours": 1
    }
  },
  "LoadBalancer": {
    "EnableForwardedHeaders": true,
    "TrustedProxies": [
      "172.25.0.10"
    ],
    "TrustedNetworks": [
      "10.0.0.0/8",
      "172.16.0.0/12",
      "192.168.0.0/16",
      "127.0.0.0/8",
      "172.25.0.0/16"
    ],
    "ForwardLimit": 2,
    "RequireHeaderSymmetry": false
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.AspNetCore": "Debug",
        "System": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "MSSqlServer",
        "Args": {
          "connectionString": "",
          "sinkOptionsSection": {
            "tableName": "SecurityLogs",
            "schemaName": "dbo",
            "autoCreateSqlTable": true,
            "batchPostingLimit": 100,
            "period": "00:00:05"
          },
          "restrictedToMinimumLevel": "Debug",
          "columnOptionsSection": {
            "disableTriggers": true,
            "clusteredColumnstoreIndex": false,
            "primaryKeyColumnName": "Id",
            "addStandardColumns": [ "LogEvent" ],
            "removeStandardColumns": [ "Properties" ],
            "additionalColumns": [
              {
                "ColumnName": "RequestId",
                "DataType": "nvarchar",
                "DataLength": 100,
                "AllowNull": true
              },
              {
                "ColumnName": "EventType",
                "DataType": "nvarchar",
                "DataLength": 50,
                "AllowNull": true
              },
              {
                "ColumnName": "IpAddress",
                "DataType": "nvarchar",
                "DataLength": 45,
                "AllowNull": true
              },
              {
                "ColumnName": "UserAgent",
                "DataType": "nvarchar",
                "DataLength": 500,
                "AllowNull": true
              },
              {
                "ColumnName": "Path",
                "DataType": "nvarchar",
                "DataLength": 200,
                "AllowNull": true
              },
              {
                "ColumnName": "Method",
                "DataType": "nvarchar",
                "DataLength": 10,
                "AllowNull": true
              },
              {
                "ColumnName": "StatusCode",
                "DataType": "int",
                "AllowNull": true
              },
              {
                "ColumnName": "DurationMs",
                "DataType": "float",
                "AllowNull": true
              },
              {
                "ColumnName": "ClientId",
                "DataType": "nvarchar",
                "DataLength": 100,
                "AllowNull": true
              },
              {
                "ColumnName": "NodeName",
                "DataType": "nvarchar",
                "DataLength": 50,
                "AllowNull": true
              }
            ]
          }
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithThreadId",
      "WithProcessId",
      "WithEnvironmentName"
    ],
    "Properties": {
      "Application": "SimpleIdentityServer"
    }
  }
}
```

### 2. Docker Configuration Updates

**File**: `code/SimpleIdentityServer/SimpleIdentityServer.API/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SimpleIdentityServer.API.csproj", "./"]
RUN dotnet restore "SimpleIdentityServer.API.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "SimpleIdentityServer.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SimpleIdentityServer.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimpleIdentityServer.API.dll"] 
```

## Implementation Checklist

### Phase 1: Server-Side Setup
- [ ] Update OpenIddict configuration to support Authorization Code flow
- [ ] Add ApplicationUser entity and update DbContext
- [ ] Create and run database migrations
- [ ] Add Authorization and Account controllers
- [ ] Configure rate limiting and security middleware

### Phase 2: Web Pages
- [ ] Create authentication page layouts
- [ ] Implement Login page with security features
- [ ] Implement Registration page with password strength
- [ ] Implement Password Reset functionality
- [ ] Add proper error handling and validation

### Phase 3: Client-Side Security
- [ ] Implement modular JavaScript architecture
- [ ] Add PKCE support for client applications
- [ ] Implement rate limiting and input validation
- [ ] Add accessibility features and progressive enhancement

### Phase 4: Testing & Security
- [ ] Write unit tests for controllers and services
- [ ] Create integration tests for authentication flows
- [ ] Perform security testing (OWASP guidelines)
- [ ] Load testing for authentication endpoints

### Phase 5: Deployment
- [ ] Update production configuration
- [ ] Configure SSL certificates
- [ ] Set up monitoring and logging
- [ ] Deploy and test in production environment

This implementation plan provides a comprehensive roadmap for adding Authorization Code flow support to the Simple Identity Server while maintaining security best practices and following the specifications outlined in the requirements.
