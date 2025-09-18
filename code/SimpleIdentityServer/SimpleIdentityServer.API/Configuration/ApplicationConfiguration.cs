using Microsoft.AspNetCore.HttpOverrides;
using SimpleIdentityServer.API.Configuration;
using System.Net;

namespace SimpleIdentityServer.API.Configuration;

public static class ApplicationConfiguration
{
    public static void ConfigureSecureEnvironmentSettings(WebApplicationBuilder builder)
    {
        // Get connection strings from environment variables
        var defaultConnection = Environment.GetEnvironmentVariable(EnvironmentVariablesNames.DefaultConnectionString);
        var securityLogsConnection = Environment.GetEnvironmentVariable(EnvironmentVariablesNames.SecurityLogsConnectionString);
        
        if (!string.IsNullOrEmpty(defaultConnection))
        {
            builder.Configuration[AppSettingsNames.ConnectionStringsDefaultConnection] = defaultConnection;
        }
        else
        {
            throw new InvalidOperationException($"{EnvironmentVariablesNames.DefaultConnectionString} environment variable is required");
        }
        
        if (!string.IsNullOrEmpty(securityLogsConnection))
        {
            builder.Configuration[AppSettingsNames.ConnectionStringsSecurityLogsConnection] = securityLogsConnection;
        }
        else
        {
            throw new InvalidOperationException($"{EnvironmentVariablesNames.SecurityLogsConnectionString} environment variable is required");
        }

        // Configure certificate password from environment variable
        var certPassword = Environment.GetEnvironmentVariable(EnvironmentVariablesNames.CertificatePassword);
        if (!string.IsNullOrEmpty(certPassword))
        {
            builder.Configuration[AppSettingsNames.ApplicationCertificatesPassword] = certPassword;
        }
        else
        {
            throw new InvalidOperationException($"{EnvironmentVariablesNames.CertificatePassword} environment variable is required");
        }
    }

    public static void ValidateConfiguration(WebApplicationBuilder builder)
    {
        var validationService = new SimpleIdentityServer.Services.ConfigurationValidationService(
            builder.Configuration, 
            builder.Environment);
        
        try
        {
            validationService.ValidateConfiguration();
        }
        catch (InvalidOperationException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    public static void ConfigureCors(WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("ProductionCorsPolicy", policy =>
            {
                // only allow specific origins from environment variables
                var allowedOrigins = Environment.GetEnvironmentVariable(EnvironmentVariablesNames.CorsAllowedOrigins)?.Split(';') ?? Array.Empty<string>();
                if (allowedOrigins.Length > 0 && !string.IsNullOrWhiteSpace(allowedOrigins[0]))
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    throw new InvalidOperationException($"{EnvironmentVariablesNames.CorsAllowedOrigins} environment variable is required in production");
                }

                policy.WithMethods("GET", "POST", "OPTIONS")
                      .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });
    }

    public static void ConfigureForwardedHeaders(WebApplicationBuilder builder, LoadBalancerOptions loadBalancerConfig)
    {
        if (!loadBalancerConfig.EnableForwardedHeaders) return;

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            // Configure which headers to forward
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            
            // Clear the default networks and proxies
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            
            // Add trusted proxy IPs from configuration
            foreach (var proxyIp in loadBalancerConfig.TrustedProxies)
            {
                if (IPAddress.TryParse(proxyIp, out var parsedProxy))
                {
                    options.KnownProxies.Add(parsedProxy);
                }
            }
            
            // Add trusted networks from configuration
            foreach (var networkCidr in loadBalancerConfig.TrustedNetworks)
            {
                try
                {
                    var parts = networkCidr.Split('/');
                    if (parts.Length == 2 && 
                        IPAddress.TryParse(parts[0], out var networkAddress) && 
                        int.TryParse(parts[1], out var prefixLength))
                    {
                        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(networkAddress, prefixLength));
                    }
                }
                catch (Exception ex)
                {
                    // Log invalid network configuration but don't crash
                    Console.WriteLine($"Warning: Invalid network configuration '{networkCidr}': {ex.Message}");
                }
            }
            
            // Configure security settings from configuration
            options.ForwardLimit = loadBalancerConfig.ForwardLimit;
            options.RequireHeaderSymmetry = loadBalancerConfig.RequireHeaderSymmetry;
        });
    }
}
