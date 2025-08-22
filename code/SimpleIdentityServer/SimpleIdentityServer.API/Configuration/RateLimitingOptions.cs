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
