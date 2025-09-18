using System.ComponentModel.DataAnnotations;

namespace SimpleIdentityServer.API.Configuration;

/// <summary>
/// Application configuration options with validation
/// All settings must be provided via appsettings.json or environment variables
/// No hardcoded defaults are allowed
/// </summary>
public class ApplicationOptions
{
    public const string SectionName = "Application";

    /// <summary>
    /// OpenIddict endpoint configuration
    /// </summary>
    [Required(ErrorMessage = "OpenIddict configuration is required")]
    public OpenIddictOptions OpenIddict { get; set; } = new();

    /// <summary>
    /// Certificate management configuration
    /// </summary>
    [Required(ErrorMessage = "Certificate configuration is required")]
    public CertificateOptions Certificates { get; set; } = new();

    /// <summary>
    /// Database configuration
    /// </summary>
    [Required(ErrorMessage = "Database configuration is required")]
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>
    /// Security logging configuration
    /// </summary>
    [Required(ErrorMessage = "Security logging configuration is required")]
    public SecurityLoggingOptions SecurityLogging { get; set; } = new();
}

public class OpenIddictOptions
{
    /// <summary>
    /// Token endpoint URI
    /// </summary>
    [Required(ErrorMessage = "Token endpoint URI is required")]
    public string TokenEndpointUri { get; set; } = string.Empty;

    /// <summary>
    /// Introspection endpoint URI
    /// </summary>
    [Required(ErrorMessage = "Introspection endpoint URI is required")]
    public string IntrospectionEndpointUri { get; set; } = string.Empty;

    /// <summary>
    /// Configuration endpoint URI
    /// </summary>
    [Required(ErrorMessage = "Configuration endpoint URI is required")]
    public string ConfigurationEndpointUri { get; set; } = string.Empty;

    /// <summary>
    /// Access token lifetime in minutes
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Access token lifetime must be between 1 and 1440 minutes")]
    public int AccessTokenLifetimeMinutes { get; set; }

    /// <summary>
    /// Refresh token lifetime in days
    /// </summary>
    [Range(1, 365, ErrorMessage = "Refresh token lifetime must be between 1 and 365 days")]
    public int RefreshTokenLifetimeDays { get; set; }
}

public class CertificateOptions
{
    /// <summary>
    /// Certificate password from environment variable
    /// </summary>
    [Required(ErrorMessage = "Certificate password is required")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Path to encryption certificate
    /// </summary>
    [Required(ErrorMessage = "Encryption certificate path is required")]
    public string EncryptionCertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Path to signing certificate
    /// </summary>
    [Required(ErrorMessage = "Signing certificate path is required")]
    public string SigningCertificatePath { get; set; } = string.Empty;
}

public class DatabaseOptions
{
    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    [Range(1, 300, ErrorMessage = "Command timeout must be between 1 and 300 seconds")]
    public int CommandTimeoutSeconds { get; set; }

    /// <summary>
    /// Maximum retry count for database operations
    /// </summary>
    [Range(0, 10, ErrorMessage = "Max retry count must be between 0 and 10")]
    public int MaxRetryCount { get; set; }

    /// <summary>
    /// Maximum retry delay in seconds
    /// </summary>
    [Range(1, 30, ErrorMessage = "Max retry delay must be between 1 and 30 seconds")]
    public int MaxRetryDelaySeconds { get; set; }
}

public class SecurityLoggingOptions
{
    /// <summary>
    /// Log retention period in days
    /// </summary>
    [Range(1, 365, ErrorMessage = "Log retention days must be between 1 and 365")]
    public int RetentionDays { get; set; }

    /// <summary>
    /// Cleanup interval in hours
    /// </summary>
    [Range(1, 168, ErrorMessage = "Cleanup interval must be between 1 and 168 hours")]
    public int CleanupIntervalHours { get; set; }

    /// <summary>
    /// Batch posting limit for Serilog
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Batch posting limit must be between 1 and 1000")]
    public int BatchPostingLimit { get; set; }

    /// <summary>
    /// Batch period in seconds for Serilog
    /// </summary>
    [Range(1, 60, ErrorMessage = "Batch period must be between 1 and 60 seconds")]
    public int BatchPeriodSeconds { get; set; }
}

/// <summary>
/// Kestrel server configuration
/// </summary>
public class KestrelOptions
{
    public const string SectionName = "Kestrel";

    /// <summary>
    /// Request headers timeout in seconds
    /// </summary>
    [Range(1, 300, ErrorMessage = "Request headers timeout must be between 1 and 300 seconds")]
    public int RequestHeadersTimeoutSeconds { get; set; }

    /// <summary>
    /// Keep alive timeout in minutes
    /// </summary>
    [Range(1, 30, ErrorMessage = "Keep alive timeout must be between 1 and 30 minutes")]
    public int KeepAliveTimeoutMinutes { get; set; }

    /// <summary>
    /// Maximum request body size in bytes
    /// </summary>
    [Range(1024, 10_485_760, ErrorMessage = "Max request body size must be between 1KB and 10MB")]
    public long MaxRequestBodySize { get; set; }

    /// <summary>
    /// Maximum concurrent connections
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Max concurrent connections must be between 1 and 1000")]
    public int MaxConcurrentConnections { get; set; }
}

