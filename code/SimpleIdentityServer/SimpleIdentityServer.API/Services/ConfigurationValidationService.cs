using System.ComponentModel.DataAnnotations;
using System.Text;
using SimpleIdentityServer.API.Configuration;

namespace SimpleIdentityServer.Services;

/// <summary>
/// Service for validating application configuration
/// Ensures all required settings are provided and valid
/// </summary>
public class ConfigurationValidationService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly List<string> _validationErrors = new();

    public ConfigurationValidationService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Validates all application configuration
    /// Throws InvalidOperationException with detailed error messages if validation fails
    /// </summary>
    public void ValidateConfiguration()
    {
        _validationErrors.Clear();

        // Validate connection strings
        ValidateConnectionStrings();

        // Validate application settings
        ValidateApplicationSettings();

        // Validate rate limiting settings
        ValidateRateLimitingSettings();

        // Validate load balancer settings
        ValidateLoadBalancerSettings();

        // Validate Kestrel settings
        ValidateKestrelSettings();

        // Validate CORS settings
        ValidateCorsSettings();

        // If there are validation errors, throw exception with all errors
        if (_validationErrors.Any())
        {
            var errorMessage = BuildErrorMessage();
            throw new InvalidOperationException(errorMessage);
        }
    }

    private void ValidateConnectionStrings()
    {
        var defaultConnection = _configuration.GetConnectionString(AppSettingsNames.DefaultConnection);
        var securityLogsConnection = _configuration.GetConnectionString(AppSettingsNames.SecurityLogsConnection);

        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            _validationErrors.Add($"{EnvironmentVariablesNames.DefaultConnectionString} environment variable is required");
        }

        if (string.IsNullOrWhiteSpace(securityLogsConnection))
        {
            _validationErrors.Add($"{EnvironmentVariablesNames.SecurityLogsConnectionString} environment variable is required");
        }
    }

    private void ValidateApplicationSettings()
    {
        // Validate Application section
        var applicationSection = _configuration.GetSection(ApplicationOptions.SectionName);
        if (!applicationSection.Exists())
        {
            _validationErrors.Add($"Configuration section '{ApplicationOptions.SectionName}' is required in appsettings.json");
            return;
        }

        var applicationOptions = new ApplicationOptions();
        applicationSection.Bind(applicationOptions);

        // Validate using data annotations
        var validationContext = new ValidationContext(applicationOptions);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(applicationOptions, validationContext, validationResults, true))
        {
            foreach (var result in validationResults)
            {
                _validationErrors.Add($"Application configuration: {result.ErrorMessage}");
            }
        }

        // Validate nested objects
        ValidateOpenIddictSettings(applicationOptions.OpenIddict);
        ValidateCertificateSettings(applicationOptions.Certificates);
        ValidateDatabaseSettings(applicationOptions.Database);
        ValidateSecurityLoggingSettings(applicationOptions.SecurityLogging);
    }

    private void ValidateOpenIddictSettings(OpenIddictOptions options)
    {
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(options, validationContext, validationResults, true))
        {
            foreach (var result in validationResults)
            {
                _validationErrors.Add($"OpenIddict configuration: {result.ErrorMessage}");
            }
        }

        // Validate URI formats
        if (!string.IsNullOrEmpty(options.TokenEndpointUri) && !options.TokenEndpointUri.StartsWith("/"))
        {
            _validationErrors.Add("OpenIddict TokenEndpointUri must start with '/'");
        }

        if (!string.IsNullOrEmpty(options.IntrospectionEndpointUri) && !options.IntrospectionEndpointUri.StartsWith("/"))
        {
            _validationErrors.Add("OpenIddict IntrospectionEndpointUri must start with '/'");
        }

        if (!string.IsNullOrEmpty(options.ConfigurationEndpointUri) && !options.ConfigurationEndpointUri.StartsWith("/"))
        {
            _validationErrors.Add("OpenIddict ConfigurationEndpointUri must start with '/'");
        }
    }

    private void ValidateCertificateSettings(CertificateOptions options)
    {
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(options, validationContext, validationResults, true))
        {
            foreach (var result in validationResults)
            {
                _validationErrors.Add($"Certificate configuration: {result.ErrorMessage}");
            }
        }

        // validate certificate paths exist
        if (!string.IsNullOrEmpty(options.EncryptionCertificatePath) && !File.Exists(options.EncryptionCertificatePath))
        {
            _validationErrors.Add($"Encryption certificate not found at path: {options.EncryptionCertificatePath}");
        }

        if (!string.IsNullOrEmpty(options.SigningCertificatePath) && !File.Exists(options.SigningCertificatePath))
        {
            _validationErrors.Add($"Signing certificate not found at path: {options.SigningCertificatePath}");
        }
    }

    private void ValidateDatabaseSettings(DatabaseOptions options)
    {
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(options, validationContext, validationResults, true))
        {
            foreach (var result in validationResults)
            {
                _validationErrors.Add($"Database configuration: {result.ErrorMessage}");
            }
        }
    }

    private void ValidateSecurityLoggingSettings(SecurityLoggingOptions options)
    {
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(options, validationContext, validationResults, true))
        {
            foreach (var result in validationResults)
            {
                _validationErrors.Add($"Security logging configuration: {result.ErrorMessage}");
            }
        }
    }

    private void ValidateRateLimitingSettings()
    {
        var rateLimitingSection = _configuration.GetSection(AppSettingsNames.RateLimiting);
        if (!rateLimitingSection.Exists())
        {
            _validationErrors.Add($"Configuration section '{AppSettingsNames.RateLimiting}' is required in appsettings.json");
            return;
        }

        // Validate required subsections
        var requiredSections = new[] { "Global", "TokenEndpoint", "IntrospectionEndpoint", "SecurityMonitoring" };
        foreach (var section in requiredSections)
        {
            var subsection = rateLimitingSection.GetSection(section);
            if (!subsection.Exists())
            {
                _validationErrors.Add($"Configuration section 'RateLimiting:{section}' is required in appsettings.json");
            }
        }
    }

    private void ValidateLoadBalancerSettings()
    {
        var loadBalancerSection = _configuration.GetSection(AppSettingsNames.LoadBalancer);
        if (!loadBalancerSection.Exists())
        {
            _validationErrors.Add($"Configuration section '{AppSettingsNames.LoadBalancer}' is required in appsettings.json");
            return;
        }

        // Validate required settings
        var enableForwardedHeaders = loadBalancerSection.GetValue<bool?>("EnableForwardedHeaders");
        if (!enableForwardedHeaders.HasValue)
        {
            _validationErrors.Add($"{AppSettingsNames.LoadBalancerEnableForwardedHeaders} setting is required in appsettings.json");
        }
    }

    private void ValidateKestrelSettings()
    {
        var kestrelSection = _configuration.GetSection(KestrelOptions.SectionName);
        if (!kestrelSection.Exists())
        {
            _validationErrors.Add($"Configuration section '{KestrelOptions.SectionName}' is required in appsettings.json");
            return;
        }

        var kestrelOptions = new KestrelOptions();
        kestrelSection.Bind(kestrelOptions);

        var validationContext = new ValidationContext(kestrelOptions);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(kestrelOptions, validationContext, validationResults, true))
        {
            foreach (var result in validationResults)
            {
                _validationErrors.Add($"Kestrel configuration: {result.ErrorMessage}");
            }
        }
    }

    private void ValidateCorsSettings()
    {
        // CORS origins must be specified
        var corsOrigins = Environment.GetEnvironmentVariable(EnvironmentVariablesNames.CorsAllowedOrigins);
        if (string.IsNullOrWhiteSpace(corsOrigins))
        {
            _validationErrors.Add($"{EnvironmentVariablesNames.CorsAllowedOrigins} environment variable is required");
        }
    }

    private string BuildErrorMessage()
    {
        var sb = new StringBuilder();
        sb.AppendLine("❌ Configuration validation failed. The following errors were found:");
        sb.AppendLine();

        for (int i = 0; i < _validationErrors.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {_validationErrors[i]}");
        }

        sb.AppendLine();
        sb.AppendLine("🔧 To fix these issues:");
        sb.AppendLine("1. Update appsettings.json with the required configuration sections");
        sb.AppendLine("2. Set the required environment variables");
        sb.AppendLine("3. Ensure all values are within the specified ranges");
        sb.AppendLine();
        sb.AppendLine("📖 See ENVIRONMENT_VARIABLES.md for detailed configuration guidance");

        return sb.ToString();
    }
}

