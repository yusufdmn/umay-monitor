namespace Presentation.Configuration;

public class CertificateOptions
{
    public const string SectionName = "Certificate";
    
    public string CertPath { get; set; } = string.Empty;
    public string KeyPath { get; set; } = string.Empty;
    public string? Password { get; set; }
}
