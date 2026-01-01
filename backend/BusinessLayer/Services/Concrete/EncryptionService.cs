using System.Security.Cryptography;
using System.Text;
using BusinessLayer.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BusinessLayer.Services.Concrete;

/// <summary>
/// AES-256 encryption service for protecting sensitive backup credentials.
/// Uses CBC mode with random IV for each encryption.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    /// <summary>
    /// Initializes the encryption service with a master key from configuration.
    /// </summary>
    /// <param name="configuration">Application configuration containing the encryption key</param>
    public EncryptionService(IConfiguration configuration)
    {
        var encryptionKey = configuration["BackupEncryption:MasterKey"];
        
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            throw new InvalidOperationException(
                "Backup encryption master key not configured. " +
                "Please set BackupEncryption:MasterKey in appsettings.json (32-byte base64 string).");
        }

        try
        {
            _key = Convert.FromBase64String(encryptionKey);
            
            if (_key.Length != 32)
            {
                throw new InvalidOperationException(
                    $"Encryption key must be 32 bytes (256 bits). Current key is {_key.Length} bytes. " +
                    "Generate one with: openssl rand -base64 32");
            }
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Invalid encryption key format. Must be base64-encoded. " +
                "Generate one with: openssl rand -base64 32");
        }
    }

    /// <summary>
    /// Encrypts plain text using AES-256-CBC.
    /// Returns: [IV(16 bytes)][Encrypted Data] as Base64
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV(); // Generate random IV for this encryption

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to encrypted data
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts AES-256-CBC encrypted text.
    /// Expects format: [IV(16 bytes)][Encrypted Data] as Base64
    /// </summary>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));

        try
        {
            var fullCipher = Convert.FromBase64String(encryptedText);

            if (fullCipher.Length < 16)
                throw new CryptographicException("Invalid encrypted data: too short to contain IV");

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Extract IV from first 16 bytes
            var iv = new byte[16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
            aes.IV = iv;

            // Extract encrypted data
            var cipherBytes = new byte[fullCipher.Length - 16];
            Buffer.BlockCopy(fullCipher, 16, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Invalid encrypted data format", ex);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("Decryption failed. Data may be corrupted or key is incorrect.", ex);
        }
    }
}
