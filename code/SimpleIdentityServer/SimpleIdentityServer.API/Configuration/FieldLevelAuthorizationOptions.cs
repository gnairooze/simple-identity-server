namespace SimpleIdentityServer.API.Configuration;

/// <summary>
/// Configuration options for field-level authorization
/// </summary>
public class FieldLevelAuthorizationOptions
{
    public const string SectionName = "FieldLevelAuthorization";

    /// <summary>
    /// Mapping of field names to required roles/clients
    /// </summary>
    public Dictionary<string, string[]> FieldPermissions { get; set; } = new();

    /// <summary>
    /// Default roles that can access all fields if not specified
    /// </summary>
    public string[] DefaultAllowedRoles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether to log field filtering actions
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Whether to enable strict mode (deny by default)
    /// </summary>
    public bool StrictMode { get; set; } = false;

    /// <summary>
    /// Trusted clients that bypass field-level restrictions
    /// </summary>
    public string[] TrustedClients { get; set; } = Array.Empty<string>();
}
