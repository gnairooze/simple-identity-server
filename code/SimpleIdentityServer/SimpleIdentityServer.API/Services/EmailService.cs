using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SimpleIdentityServer.API.Configuration;

namespace SimpleIdentityServer.API.Services;

/// <summary>
/// Production-ready email service implementation using SMTP
/// Supports both development mode (logging only) and production mode (actual SMTP sending)
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailOptions _emailOptions;

    public EmailService(
        ILogger<EmailService> logger,
        IOptions<EmailOptions> emailOptions)
    {
        _logger = logger;
        _emailOptions = emailOptions.Value;
    }

    public async Task SendEmailConfirmationAsync(string email, string callbackUrl)
    {
        const string subject = "Confirm Your Email Address";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .button {{
            display: inline-block;
            padding: 12px 24px;
            background-color: #007bff;
            color: white !important;
            text-decoration: none;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Welcome to Simple Identity Server!</h2>
        <p>Thank you for registering. Please confirm your email address by clicking the button below:</p>
        <a href=""{callbackUrl}"" class=""button"">Confirm Email Address</a>
        <p>Or copy and paste this link into your browser:</p>
        <p><a href=""{callbackUrl}"">{callbackUrl}</a></p>
        <div class=""footer"">
            <p>If you did not create an account, please ignore this email.</p>
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetAsync(string email, string callbackUrl)
    {
        const string subject = "Reset Your Password";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .button {{
            display: inline-block;
            padding: 12px 24px;
            background-color: #dc3545;
            color: white !important;
            text-decoration: none;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffc107; padding: 15px; border-radius: 4px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Password Reset Request</h2>
        <p>We received a request to reset your password. Click the button below to reset it:</p>
        <a href=""{callbackUrl}"" class=""button"">Reset Password</a>
        <p>Or copy and paste this link into your browser:</p>
        <p><a href=""{callbackUrl}"">{callbackUrl}</a></p>
        <div class=""warning"">
            <strong>Security Notice:</strong> This password reset link will expire in a short time for security reasons.
        </div>
        <div class=""footer"">
            <p>If you did not request a password reset, please ignore this email and your password will remain unchanged.</p>
            <p>For security reasons, never share this link with anyone.</p>
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        // In development mode, just log the email
        if (_emailOptions.DevelopmentMode)
        {
            _logger.LogInformation(
                $"Development Mode - Email would be sent:{Environment.NewLine}" +
                $"To: {toEmail}{Environment.NewLine}" +
                $"From: {_emailOptions.FromEmail}{Environment.NewLine}" +
                $"Subject: {subject}{Environment.NewLine}" +
                $"Body: {htmlBody}");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailOptions.FromName, _emailOptions.FromEmail));
            message.To.Add(new MailboxAddress(toEmail, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                // Also include a plain text version for email clients that don't support HTML
                TextBody = ConvertHtmlToPlainText(htmlBody)
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Set timeout
            client.Timeout = _emailOptions.TimeoutSeconds * 1000;

            // Connect to SMTP server
            await client.ConnectAsync(
                _emailOptions.SmtpHost,
                _emailOptions.SmtpPort,
                _emailOptions.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            // Authenticate if credentials are provided
            if (!string.IsNullOrWhiteSpace(_emailOptions.Username) &&
                !string.IsNullOrWhiteSpace(_emailOptions.Password))
            {
                await client.AuthenticateAsync(_emailOptions.Username, _emailOptions.Password);
            }

            // Send the email
            await client.SendAsync(message);

            // Disconnect
            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "Email sent successfully to {ToEmail} with subject: {Subject}",
                toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {ToEmail} with subject: {Subject}",
                toEmail, subject);

            // Re-throw the exception so the caller knows the email failed
            // This allows the application to handle the error appropriately
            throw;
        }
    }

    /// <summary>
    /// Converts HTML content to plain text by removing tags
    /// This is a simple implementation for email fallback
    /// </summary>
    private static string ConvertHtmlToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Remove HTML tags
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", string.Empty);

        // Decode HTML entities
        text = System.Net.WebUtility.HtmlDecode(text);

        // Normalize whitespace
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }
}

