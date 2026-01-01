using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Presentation.Helpers;

public static class CertificateLoader
{
    public static X509Certificate2 LoadFromPemFiles(string certPath, string keyPath, string? password = null)
    {
        var certPem = File.ReadAllText(certPath);
        var keyPem = File.ReadAllText(keyPath);

        using var cert = X509Certificate2.CreateFromPem(certPem, keyPem);
        
        return password != null 
            ? new X509Certificate2(cert.Export(X509ContentType.Pfx, password), password)
            : new X509Certificate2(cert.Export(X509ContentType.Pfx));
    }
}
