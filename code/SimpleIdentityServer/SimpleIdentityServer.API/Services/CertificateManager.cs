using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SimpleIdentityServer.API.Services;

public static class CertificateManager
{
    public static X509Certificate2 GetOrCreateEncryptionCertificate(SimpleIdentityServer.API.Configuration.CertificateOptions certificateOptions)
    {
        var certPath = certificateOptions.EncryptionCertificatePath;
        var certPassword = GetCertificatePassword(certificateOptions.Password);
        
        if (File.Exists(certPath))
        {
            return new X509Certificate2(certPath, certPassword);
        }
        
        // If certificate doesn't exist, create a self-signed one and save it
        // This is a simplified approach - in production, use proper certificate management
        var cert = CreateSelfSignedCertificate("CN=SimpleIdentityServer-Encryption");
        
        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(certPath)!);
        
        // Save certificate for other instances to use
        File.WriteAllBytes(certPath, cert.Export(X509ContentType.Pfx, certPassword));
        
        return cert;
    }

    public static X509Certificate2 GetOrCreateSigningCertificate(SimpleIdentityServer.API.Configuration.CertificateOptions certificateOptions)
    {
        var certPath = certificateOptions.SigningCertificatePath;
        var certPassword = GetCertificatePassword(certificateOptions.Password);
        
        if (File.Exists(certPath))
        {
            return new X509Certificate2(certPath, certPassword);
        }
        
        // If certificate doesn't exist, create a self-signed one and save it
        var cert = CreateSelfSignedCertificate("CN=SimpleIdentityServer-Signing");
        
        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(certPath)!);
        
        // Save certificate for other instances to use
        File.WriteAllBytes(certPath, cert.Export(X509ContentType.Pfx, certPassword));
        
        return cert;
    }

    private static string GetCertificatePassword(string? configPassword)
    {
        if (!string.IsNullOrEmpty(configPassword))
            return configPassword;
            
        var envPassword = Environment.GetEnvironmentVariable("CERT_PASSWORD");
        if (!string.IsNullOrEmpty(envPassword))
            return envPassword;
            
        throw new InvalidOperationException("Certificate password is required. Set CERT_PASSWORD environment variable or Application:Certificates:Password in configuration.");
    }

    private static X509Certificate2 CreateSelfSignedCertificate(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            subjectName, 
            rsa, 
            HashAlgorithmName.SHA256, 
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, 
                critical: false));

        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(2));
        
        // Return a new certificate with the private key
        return new X509Certificate2(
            certificate.Export(X509ContentType.Pfx), 
            (string?)null, 
            X509KeyStorageFlags.Exportable);
    }
}
