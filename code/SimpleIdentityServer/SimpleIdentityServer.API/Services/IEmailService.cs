namespace SimpleIdentityServer.API.Services;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string email, string callbackUrl);
    Task SendPasswordResetAsync(string email, string callbackUrl);
}

