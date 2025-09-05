namespace SimpleIdentityServer.API.Configuration;

/// <summary>
/// Centralized access to all appsettings configuration keys used in the Simple Identity Server project.
/// This class provides readonly string properties for all configuration keys to ensure
/// consistency and avoid typos throughout the codebase.
/// </summary>
public static class AppSettingsNames
{
    #region Connection Strings
    
    /// <summary>
    /// Main database connection string key
    /// </summary>
    public static readonly string DefaultConnection = "DefaultConnection";
    
    /// <summary>
    /// Security logs database connection string key
    /// </summary>
    public static readonly string SecurityLogsConnection = "SecurityLogsConnection";
    
    #endregion

    #region Logging Configuration
    
    /// <summary>
    /// Logging configuration section
    /// </summary>
    public static readonly string Logging = "Logging";
    
    /// <summary>
    /// Log level configuration section
    /// </summary>
    public static readonly string LoggingLogLevel = "Logging:LogLevel";
    
    /// <summary>
    /// Default log level key
    /// </summary>
    public static readonly string LoggingLogLevelDefault = "Logging:LogLevel:Default";
    
    /// <summary>
    /// Microsoft AspNetCore log level key
    /// </summary>
    public static readonly string LoggingLogLevelMicrosoftAspNetCore = "Logging:LogLevel:Microsoft.AspNetCore";
    
    /// <summary>
    /// Security monitoring middleware log level key
    /// </summary>
    public static readonly string LoggingLogLevelSecurityMonitoringMiddleware = "Logging:LogLevel:SimpleIdentityServer.API.Middleware.SecurityMonitoringMiddleware";
    
    /// <summary>
    /// Debug logging middleware log level key
    /// </summary>
    public static readonly string LoggingLogLevelDebugLoggingMiddleware = "Logging:LogLevel:SimpleIdentityServer.API.Middleware.DebugLoggingMiddleware";
    
    #endregion

    #region Application Configuration
    
    /// <summary>
    /// Allowed hosts configuration key
    /// </summary>
    public static readonly string AllowedHosts = "AllowedHosts";
    
    /// <summary>
    /// Application configuration section
    /// </summary>
    public static readonly string Application = "Application";
    
    #endregion

    #region OpenIddict Configuration
    
    /// <summary>
    /// OpenIddict configuration section
    /// </summary>
    public static readonly string ApplicationOpenIddict = "Application:OpenIddict";
    
    /// <summary>
    /// Token endpoint URI configuration key
    /// </summary>
    public static readonly string ApplicationOpenIddictTokenEndpointUri = "Application:OpenIddict:TokenEndpointUri";
    
    /// <summary>
    /// Introspection endpoint URI configuration key
    /// </summary>
    public static readonly string ApplicationOpenIddictIntrospectionEndpointUri = "Application:OpenIddict:IntrospectionEndpointUri";
    
    /// <summary>
    /// Configuration endpoint URI configuration key
    /// </summary>
    public static readonly string ApplicationOpenIddictConfigurationEndpointUri = "Application:OpenIddict:ConfigurationEndpointUri";
    
    /// <summary>
    /// Access token lifetime in minutes configuration key
    /// </summary>
    public static readonly string ApplicationOpenIddictAccessTokenLifetimeMinutes = "Application:OpenIddict:AccessTokenLifetimeMinutes";
    
    /// <summary>
    /// Refresh token lifetime in days configuration key
    /// </summary>
    public static readonly string ApplicationOpenIddictRefreshTokenLifetimeDays = "Application:OpenIddict:RefreshTokenLifetimeDays";
    
    #endregion

    #region Certificate Configuration
    
    /// <summary>
    /// Certificates configuration section
    /// </summary>
    public static readonly string ApplicationCertificates = "Application:Certificates";
    
    /// <summary>
    /// Certificate password configuration key
    /// </summary>
    public static readonly string ApplicationCertificatesPassword = "Application:Certificates:Password";
    
    /// <summary>
    /// Encryption certificate path configuration key
    /// </summary>
    public static readonly string ApplicationCertificatesEncryptionCertificatePath = "Application:Certificates:EncryptionCertificatePath";
    
    /// <summary>
    /// Signing certificate path configuration key
    /// </summary>
    public static readonly string ApplicationCertificatesSigningCertificatePath = "Application:Certificates:SigningCertificatePath";
    
    #endregion

    #region Database Configuration
    
    /// <summary>
    /// Database configuration section
    /// </summary>
    public static readonly string ApplicationDatabase = "Application:Database";
    
    /// <summary>
    /// Database command timeout in seconds configuration key
    /// </summary>
    public static readonly string ApplicationDatabaseCommandTimeoutSeconds = "Application:Database:CommandTimeoutSeconds";
    
    /// <summary>
    /// Database maximum retry count configuration key
    /// </summary>
    public static readonly string ApplicationDatabaseMaxRetryCount = "Application:Database:MaxRetryCount";
    
    /// <summary>
    /// Database maximum retry delay in seconds configuration key
    /// </summary>
    public static readonly string ApplicationDatabaseMaxRetryDelaySeconds = "Application:Database:MaxRetryDelaySeconds";
    
    #endregion

    #region Security Logging Configuration
    
    /// <summary>
    /// Security logging configuration section
    /// </summary>
    public static readonly string ApplicationSecurityLogging = "Application:SecurityLogging";
    
    /// <summary>
    /// Security logging retention days configuration key
    /// </summary>
    public static readonly string ApplicationSecurityLoggingRetentionDays = "Application:SecurityLogging:RetentionDays";
    
    /// <summary>
    /// Security logging cleanup interval hours configuration key
    /// </summary>
    public static readonly string ApplicationSecurityLoggingCleanupIntervalHours = "Application:SecurityLogging:CleanupIntervalHours";
    
    /// <summary>
    /// Security logging batch posting limit configuration key
    /// </summary>
    public static readonly string ApplicationSecurityLoggingBatchPostingLimit = "Application:SecurityLogging:BatchPostingLimit";
    
    /// <summary>
    /// Security logging batch period seconds configuration key
    /// </summary>
    public static readonly string ApplicationSecurityLoggingBatchPeriodSeconds = "Application:SecurityLogging:BatchPeriodSeconds";
    
    #endregion

    #region Development Configuration
    
    /// <summary>
    /// Development configuration section
    /// </summary>
    public static readonly string ApplicationDevelopment = "Application:Development";
    
    /// <summary>
    /// Development CORS origins configuration key
    /// </summary>
    public static readonly string ApplicationDevelopmentCorsOrigins = "Application:Development:CorsOrigins";
    
    /// <summary>
    /// Development default connection string configuration key
    /// </summary>
    public static readonly string ApplicationDevelopmentDefaultConnectionString = "Application:Development:DefaultConnectionString";
    
    /// <summary>
    /// Development security logs connection string configuration key
    /// </summary>
    public static readonly string ApplicationDevelopmentSecurityLogsConnectionString = "Application:Development:SecurityLogsConnectionString";
    
    #endregion

    #region Kestrel Configuration
    
    /// <summary>
    /// Kestrel configuration section
    /// </summary>
    public static readonly string Kestrel = "Kestrel";
    
    /// <summary>
    /// Kestrel request headers timeout seconds configuration key
    /// </summary>
    public static readonly string KestrelRequestHeadersTimeoutSeconds = "Kestrel:RequestHeadersTimeoutSeconds";
    
    /// <summary>
    /// Kestrel keep alive timeout minutes configuration key
    /// </summary>
    public static readonly string KestrelKeepAliveTimeoutMinutes = "Kestrel:KeepAliveTimeoutMinutes";
    
    /// <summary>
    /// Kestrel maximum request body size configuration key
    /// </summary>
    public static readonly string KestrelMaxRequestBodySize = "Kestrel:MaxRequestBodySize";
    
    /// <summary>
    /// Kestrel maximum concurrent connections configuration key
    /// </summary>
    public static readonly string KestrelMaxConcurrentConnections = "Kestrel:MaxConcurrentConnections";
    
    #endregion

    #region Rate Limiting Configuration
    
    /// <summary>
    /// Rate limiting configuration section
    /// </summary>
    public static readonly string RateLimiting = "RateLimiting";
    
    /// <summary>
    /// Global rate limiting configuration section
    /// </summary>
    public static readonly string RateLimitingGlobal = "RateLimiting:Global";
    
    /// <summary>
    /// Global rate limiting permit limit configuration key
    /// </summary>
    public static readonly string RateLimitingGlobalPermitLimit = "RateLimiting:Global:PermitLimit";
    
    /// <summary>
    /// Global rate limiting window minutes configuration key
    /// </summary>
    public static readonly string RateLimitingGlobalWindowMinutes = "RateLimiting:Global:WindowMinutes";
    
    /// <summary>
    /// Token endpoint rate limiting configuration section
    /// </summary>
    public static readonly string RateLimitingTokenEndpoint = "RateLimiting:TokenEndpoint";
    
    /// <summary>
    /// Token endpoint rate limiting permit limit configuration key
    /// </summary>
    public static readonly string RateLimitingTokenEndpointPermitLimit = "RateLimiting:TokenEndpoint:PermitLimit";
    
    /// <summary>
    /// Token endpoint rate limiting window minutes configuration key
    /// </summary>
    public static readonly string RateLimitingTokenEndpointWindowMinutes = "RateLimiting:TokenEndpoint:WindowMinutes";
    
    /// <summary>
    /// Introspection endpoint rate limiting configuration section
    /// </summary>
    public static readonly string RateLimitingIntrospectionEndpoint = "RateLimiting:IntrospectionEndpoint";
    
    /// <summary>
    /// Introspection endpoint rate limiting permit limit configuration key
    /// </summary>
    public static readonly string RateLimitingIntrospectionEndpointPermitLimit = "RateLimiting:IntrospectionEndpoint:PermitLimit";
    
    /// <summary>
    /// Introspection endpoint rate limiting window minutes configuration key
    /// </summary>
    public static readonly string RateLimitingIntrospectionEndpointWindowMinutes = "RateLimiting:IntrospectionEndpoint:WindowMinutes";
    
    /// <summary>
    /// Security monitoring rate limiting configuration section
    /// </summary>
    public static readonly string RateLimitingSecurityMonitoring = "RateLimiting:SecurityMonitoring";
    
    /// <summary>
    /// Security monitoring suspicious request threshold 5 minutes configuration key
    /// </summary>
    public static readonly string RateLimitingSecurityMonitoringSuspiciousRequestThreshold5Min = "RateLimiting:SecurityMonitoring:SuspiciousRequestThreshold5Min";
    
    /// <summary>
    /// Security monitoring high frequency request threshold 1 hour configuration key
    /// </summary>
    public static readonly string RateLimitingSecurityMonitoringHighFrequencyRequestThreshold1Hour = "RateLimiting:SecurityMonitoring:HighFrequencyRequestThreshold1Hour";
    
    /// <summary>
    /// Security monitoring slow request threshold seconds configuration key
    /// </summary>
    public static readonly string RateLimitingSecurityMonitoringSlowRequestThresholdSeconds = "RateLimiting:SecurityMonitoring:SlowRequestThresholdSeconds";
    
    /// <summary>
    /// Security monitoring request tracking retention hours configuration key
    /// </summary>
    public static readonly string RateLimitingSecurityMonitoringRequestTrackingRetentionHours = "RateLimiting:SecurityMonitoring:RequestTrackingRetentionHours";
    
    #endregion

    #region Load Balancer Configuration
    
    /// <summary>
    /// Load balancer configuration section
    /// </summary>
    public static readonly string LoadBalancer = "LoadBalancer";
    
    /// <summary>
    /// Load balancer enable forwarded headers configuration key
    /// </summary>
    public static readonly string LoadBalancerEnableForwardedHeaders = "LoadBalancer:EnableForwardedHeaders";
    
    /// <summary>
    /// Load balancer trusted proxies configuration key
    /// </summary>
    public static readonly string LoadBalancerTrustedProxies = "LoadBalancer:TrustedProxies";
    
    /// <summary>
    /// Load balancer trusted networks configuration key
    /// </summary>
    public static readonly string LoadBalancerTrustedNetworks = "LoadBalancer:TrustedNetworks";
    
    /// <summary>
    /// Load balancer forward limit configuration key
    /// </summary>
    public static readonly string LoadBalancerForwardLimit = "LoadBalancer:ForwardLimit";
    
    /// <summary>
    /// Load balancer require header symmetry configuration key
    /// </summary>
    public static readonly string LoadBalancerRequireHeaderSymmetry = "LoadBalancer:RequireHeaderSymmetry";
    
    #endregion

    #region Serilog Configuration
    
    /// <summary>
    /// Serilog configuration section
    /// </summary>
    public static readonly string Serilog = "Serilog";
    
    /// <summary>
    /// Serilog minimum level configuration section
    /// </summary>
    public static readonly string SerilogMinimumLevel = "Serilog:MinimumLevel";
    
    /// <summary>
    /// Serilog minimum level default configuration key
    /// </summary>
    public static readonly string SerilogMinimumLevelDefault = "Serilog:MinimumLevel:Default";
    
    /// <summary>
    /// Serilog minimum level override configuration section
    /// </summary>
    public static readonly string SerilogMinimumLevelOverride = "Serilog:MinimumLevel:Override";
    
    /// <summary>
    /// Serilog write to configuration section
    /// </summary>
    public static readonly string SerilogWriteTo = "Serilog:WriteTo";
    
    /// <summary>
    /// Serilog enrich configuration section
    /// </summary>
    public static readonly string SerilogEnrich = "Serilog:Enrich";
    
    /// <summary>
    /// Serilog properties configuration section
    /// </summary>
    public static readonly string SerilogProperties = "Serilog:Properties";
    
    /// <summary>
    /// Serilog application property configuration key
    /// </summary>
    public static readonly string SerilogPropertiesApplication = "Serilog:Properties:Application";
    
    #endregion

    #region Resource API Configuration (for Resource.API project)
    
    /// <summary>
    /// Identity server configuration section
    /// </summary>
    public static readonly string IdentityServer = "IdentityServer";
    
    /// <summary>
    /// Identity server authority configuration key
    /// </summary>
    public static readonly string IdentityServerAuthority = "IdentityServer:Authority";
    
    /// <summary>
    /// Identity server audience configuration key
    /// </summary>
    public static readonly string IdentityServerAudience = "IdentityServer:Audience";
    
    /// <summary>
    /// Identity server introspection endpoint configuration key
    /// </summary>
    public static readonly string IdentityServerIntrospectionEndpoint = "IdentityServer:IntrospectionEndpoint";
    
    /// <summary>
    /// Identity server client ID configuration key
    /// </summary>
    public static readonly string IdentityServerClientId = "IdentityServer:ClientId";
    
    /// <summary>
    /// Identity server client secret configuration key
    /// </summary>
    public static readonly string IdentityServerClientSecret = "IdentityServer:ClientSecret";
    
    #endregion

    #region Connection String Sections
    
    /// <summary>
    /// Connection strings configuration section
    /// </summary>
    public static readonly string ConnectionStrings = "ConnectionStrings";
    
    /// <summary>
    /// Full path to default connection string
    /// </summary>
    public static readonly string ConnectionStringsDefaultConnection = "ConnectionStrings:DefaultConnection";
    
    /// <summary>
    /// Full path to security logs connection string
    /// </summary>
    public static readonly string ConnectionStringsSecurityLogsConnection = "ConnectionStrings:SecurityLogsConnection";
    
    #endregion
}
