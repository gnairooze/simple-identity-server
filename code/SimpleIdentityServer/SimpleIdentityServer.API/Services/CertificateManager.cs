using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using SimpleIdentityServer.API.Configuration;

namespace SimpleIdentityServer.API.Services;

public static class CertificateManager
{
    public static X509Certificate2 GetEncryptionCertificate(SimpleIdentityServer.API.Configuration.CertificateOptions certificateOptions)
    {
        var certPath = certificateOptions.EncryptionCertificatePath;
        var certPassword = GetCertificatePassword(certificateOptions.Password);
        
        if (!File.Exists(certPath))
        {
            throw new InvalidOperationException ($"Encryption certificate not found at path: {Path.GetFullPath(certPath)}");
        }
        
        return new X509Certificate2(certPath, certPassword);
    }

    public static X509Certificate2 GetSigningCertificate(SimpleIdentityServer.API.Configuration.CertificateOptions certificateOptions)
    {
        var certPath = certificateOptions.SigningCertificatePath;
        var certPassword = GetCertificatePassword(certificateOptions.Password);
        
        if (!File.Exists(certPath))
        {
            throw new InvalidOperationException ($"Signing certificate not found at path: {Path.GetFullPath(certPath)}");
        }
        
        return new X509Certificate2(certPath, certPassword);
    }

    private static string GetCertificatePassword(string? configPassword)
    {
        if (!string.IsNullOrEmpty(configPassword))
            return configPassword;
            
        var envPassword = Environment.GetEnvironmentVariable(EnvironmentVariablesNames.CertificatePassword);
        if (!string.IsNullOrEmpty(envPassword))
            return envPassword;
            
        throw new InvalidOperationException($"Certificate password is required. Set {EnvironmentVariablesNames.CertificatePassword} environment variable or Application:Certificates:Password in configuration.");
    }
}
