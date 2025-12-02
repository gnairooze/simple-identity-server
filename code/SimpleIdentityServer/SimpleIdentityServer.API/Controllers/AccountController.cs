using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SimpleIdentityServer.API.Data;
using SimpleIdentityServer.API.Models;
using SimpleIdentityServer.API.Services;

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
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
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
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("register")]
    [EnableRateLimiting("AuthPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
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

            await _emailService.SendEmailConfirmationAsync(model.Email, callbackUrl ?? string.Empty);

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

        await _emailService.SendPasswordResetAsync(model.Email, callbackUrl ?? string.Empty);

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet("reset-password")]
    public IActionResult ResetPassword(string? code = null)
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

        var user = await _userManager.FindByIdAsync(userId) ?? throw new ApplicationException($"Unable to load user with ID '{userId}'.");
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

    [HttpGet("login-with-2fa")]
    public IActionResult LoginWith2fa(bool rememberMe, string? returnUrl = null)
    {
        // Placeholder for 2FA implementation
        return View();
    }
}

