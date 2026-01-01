namespace BusinessLayer.Services.Interfaces;

/// <summary>
/// Provides AES encryption and decryption for sensitive data.
/// Used to encrypt backup credentials before storing in database.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plain text using AES-256-CBC encryption
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <returns>Base64-encoded encrypted string</returns>
    string Encrypt(string plainText);
    
    /// <summary>
    /// Decrypts encrypted text back to plain text
    /// </summary>
    /// <param name="encryptedText">Base64-encoded encrypted string</param>
    /// <returns>The decrypted plain text</returns>
    string Decrypt(string encryptedText);
}
