using System;
using System.Security.Cryptography;
using System.Text;

namespace AIM.Services;

/// <summary>
/// Provides AES-256 encryption and decryption services for sensitive data.
/// Uses machine-specific key derivation to ensure encrypted data is tied to the machine it was created on.
/// Keys are derived from the machine name and username using SHA-256 hashing.
/// </summary>
public class EncryptionService
{
    private readonly byte[] _encryptionKey;
    private readonly byte[] _encryptionIV;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionService"/> class.
    /// Automatically derives encryption keys from machine-specific data.
    /// </summary>
    public EncryptionService()
    {
        // Derive encryption key from machine-specific data
        // This ensures encrypted data is tied to the machine it was created on
        _encryptionKey = DeriveKeyFromMachine();
        _encryptionIV = DeriveIVFromMachine();
    }

    /// <summary>
    /// Encrypts a plain text string using AES-256 encryption.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt. If null or empty, returns the input unchanged.</param>
    /// <returns>A Base64-encoded string containing the encrypted data.</returns>
    /// <exception cref="Exception">Thrown when encryption fails.</exception>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.IV = _encryptionIV;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new System.IO.StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Encryption] ERROR encrypting data: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Decrypts an AES-256 encrypted string back to plain text.
    /// </summary>
    /// <param name="cipherText">The Base64-encoded encrypted string. If null or empty, returns the input unchanged.</param>
    /// <returns>The decrypted plain text string.</returns>
    /// <exception cref="Exception">Thrown when decryption fails, typically due to invalid cipherText or wrong key.</exception>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.IV = _encryptionIV;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var ms = new System.IO.MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (var sr = new System.IO.StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Encryption] ERROR decrypting data: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Derives a 256-bit (32-byte) AES encryption key from machine-specific data.
    /// Uses SHA-256 to hash the combination of machine name and username.
    /// This ensures the key is consistent for the same user on the same machine.
    /// </summary>
    /// <returns>A 32-byte encryption key.</returns>
    private byte[] DeriveKeyFromMachine()
    {
        // Use machine name and OS as key material
        var machineId = Environment.MachineName + Environment.UserName;

        using (var sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
        }
    }

    /// <summary>
    /// Derives a 128-bit (16-byte) initialization vector (IV) from machine-specific data.
    /// Uses SHA-256 to hash the machine name and takes the first 16 bytes.
    /// The IV ensures that identical plaintexts encrypt to different ciphertexts.
    /// </summary>
    /// <returns>A 16-byte initialization vector.</returns>
    private byte[] DeriveIVFromMachine()
    {
        var machineId = Environment.MachineName;

        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
            // Take first 16 bytes for AES IV
            var iv = new byte[16];
            Array.Copy(hash, iv, 16);
            return iv;
        }
    }
}