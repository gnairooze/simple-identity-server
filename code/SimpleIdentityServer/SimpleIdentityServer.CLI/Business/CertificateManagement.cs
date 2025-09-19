using SimpleIdentityServer.CLI.Services;

namespace SimpleIdentityServer.CLI.Business;

public class CertificateManagement
{
    public Task CreateEncryptionCertificate(string certificatePath, string? password)
    {
        try
        {
            Console.WriteLine($"Creating encryption certificate at: {certificatePath}");
            
            var certificateOptions = new CertificateOptions
            {
                EncryptionCertificatePath = certificatePath,
                Password = password
            };

            var certificate = CertificateManager.GetOrCreateEncryptionCertificate(certificateOptions);
            
            Console.WriteLine($"✅ Encryption certificate created successfully!");
            Console.WriteLine($"   Path: {certificatePath}");
            Console.WriteLine($"   Subject: {certificate.Subject}");
            Console.WriteLine($"   Thumbprint: {certificate.Thumbprint}");
            Console.WriteLine($"   Valid from: {certificate.NotBefore}");
            Console.WriteLine($"   Valid until: {certificate.NotAfter}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating encryption certificate: {ex.Message}");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task CreateSigningCertificate(string certificatePath, string? password)
    {
        try
        {
            Console.WriteLine($"Creating signing certificate at: {certificatePath}");
            
            var certificateOptions = new CertificateOptions
            {
                SigningCertificatePath = certificatePath,
                Password = password
            };

            var certificate = CertificateManager.GetOrCreateSigningCertificate(certificateOptions);
            
            Console.WriteLine($"✅ Signing certificate created successfully!");
            Console.WriteLine($"   Path: {certificatePath}");
            Console.WriteLine($"   Subject: {certificate.Subject}");
            Console.WriteLine($"   Thumbprint: {certificate.Thumbprint}");
            Console.WriteLine($"   Valid from: {certificate.NotBefore}");
            Console.WriteLine($"   Valid until: {certificate.NotAfter}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating signing certificate: {ex.Message}");
            throw;
        }

        return Task.CompletedTask;
    }
}
