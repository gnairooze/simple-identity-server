namespace SimpleIdentityServer.API.Configuration;

/// <summary>
/// Configuration options for email service
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// SMTP server hostname
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (typically 587 for TLS, 465 for SSL, 25 for non-encrypted)
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Enable SSL/TLS encryption
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// SMTP username for authentication
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password for authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// From email address
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// From display name
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Enable development mode (logs emails instead of sending them)
    /// </summary>
    public bool DevelopmentMode { get; set; } = true;

    /// <summary>
    /// Timeout in seconds for SMTP operations
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Validates that all required settings are configured
    /// </summary>
    public void Validate()
    {
        if (DevelopmentMode)
        {
            // In development mode, we only need FromEmail for logging
            if (string.IsNullOrWhiteSpace(FromEmail))
            {
                throw new InvalidOperationException("Email:FromEmail is required even in development mode");
            }
            return;
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SmtpHost))
            errors.Add("Email:SmtpHost is required in production mode");

        if (SmtpPort <= 0 || SmtpPort > 65535)
            errors.Add("Email:SmtpPort must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(Username))
            errors.Add("Email:Username is required in production mode");

        if (string.IsNullOrWhiteSpace(Password))
            errors.Add("Email:Password is required in production mode");

        if (string.IsNullOrWhiteSpace(FromEmail))
            errors.Add("Email:FromEmail is required");

        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"Email configuration is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }
}

