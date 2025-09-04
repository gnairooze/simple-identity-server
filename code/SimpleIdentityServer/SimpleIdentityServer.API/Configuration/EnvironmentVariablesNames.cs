namespace SimpleIdentityServer.API.Configuration;

/// <summary>
/// Centralized access to all environment variables used in the Simple Identity Server project.
/// This class provides readonly string properties for all environment variable keys to ensure
/// consistency and avoid typos throughout the codebase.
/// </summary>
public static class EnvironmentVariablesNames
{
    /// <summary>
    /// Main database connection string environment variable key.
    /// Used to specify the connection string for the primary SimpleIdentityServer database.
    /// </summary>
    public static readonly string DefaultConnectionString = "SIMPLE_IDENTITY_SERVER_DEFAULT_CONNECTION_STRING";

    /// <summary>
    /// Security logs database connection string environment variable key.
    /// Used to specify the connection string for the security logs database.
    /// </summary>
    public static readonly string SecurityLogsConnectionString = "SIMPLE_IDENTITY_SERVER_SECURITY_LOGS_CONNECTION_STRING";

    /// <summary>
    /// Certificate password environment variable key.
    /// Used to specify the password for certificate files used by OpenIddict.
    /// </summary>
    public static readonly string CertificatePassword = "SIMPLE_IDENTITY_SERVER_CERT_PASSWORD";

    /// <summary>
    /// CORS allowed origins environment variable key.
    /// Used to specify semicolon-separated list of allowed origins for CORS policy.
    /// </summary>
    public static readonly string CorsAllowedOrigins = "SIMPLE_IDENTITY_SERVER_CORS_ALLOWED_ORIGINS";

    /// <summary>
    /// Node name environment variable key.
    /// Used to identify this server instance in load-balanced scenarios.
    /// Defaults to machine name if not specified.
    /// </summary>
    public static readonly string NodeName = "SIMPLE_IDENTITY_SERVER_NODE_NAME";

    /// <summary>
    /// Enable detailed security logs environment variable key.
    /// Used to enable additional security logging features.
    /// </summary>
    public static readonly string EnableDetailedSecurityLogs = "ENABLE_DETAILED_SECURITY_LOGS";

    /// <summary>
    /// ASP.NET Core environment name environment variable key.
    /// Standard ASP.NET Core variable to specify the environment (Development, Staging, Production).
    /// </summary>
    public static readonly string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";

    /// <summary>
    /// Database password environment variable key used in containerized deployments.
    /// Used to specify the SQL Server SA account password.
    /// </summary>
    public static readonly string DatabasePassword = "SIMPLE_IDENTITY_SERVER_DB_PASSWORD";
}
