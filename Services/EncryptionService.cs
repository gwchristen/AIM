using System;
using System.Security.Cryptography;
using System.Text;

namespace AIM.Services;

/// <summary>
/// Provides AES encryption/decryption for sensitive data like authorized users list.
/// Uses a hardcoded key derived from the machine's network adapter ID for consistency.
/// </summary>
public class EncryptionService
{
    private readonly byte[] _encryptionKey;
    private readonly byte[] _encryptionIV;

    public EncryptionService()
    {
        // Derive encryption key from machine-specific data
        // This ensures encrypted data is tied to the machine it was created on
        _encryptionKey = DeriveKeyFromMachine();
        _encryptionIV = DeriveIVFromMachine();
    }

    /// <summary>
    /// Encrypt a string using AES encryption.
    /// </summary>
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
    /// Decrypt a string using AES encryption.
    /// </summary>
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
    /// Derive a 256-bit encryption key from machine-specific data.
    /// </summary>
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
    /// Derive a 128-bit IV from machine-specific data.
    /// </summary>
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