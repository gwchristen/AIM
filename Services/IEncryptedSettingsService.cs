using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIM.Services;

/// <summary>
/// Interface for handling encryption and decryption of sensitive settings using Windows Data Protection.
/// Provides secure storage for security configuration including master password and authorized users.
/// </summary>
public interface IEncryptedSettingsService
{
    /// <summary>
    /// Gets the security config file path based on user's chosen storage location.
    /// Creates the security directory if it doesn't exist.
    /// </summary>
    /// <param name="baseStoragePath">The base storage path for the application. 
    /// If null or empty, defaults to LocalApplicationData/AIM.</param>
    /// <returns>The full path to the security configuration file.</returns>
    string GetSecurityConfigPath(string baseStoragePath);

    /// <summary>
    /// Saves encrypted security configuration to the specified path.
    /// Uses Windows Data Protection API (DPAPI) to encrypt the data with LOCAL=user scope.
    /// </summary>
    /// <param name="configPath">The file path where the encrypted configuration will be saved.</param>
    /// <param name="masterPassword">The master password to encrypt and store.</param>
    /// <param name="authorizedUsers">The list of authorized user IDs to store.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <exception cref="System.Exception">Thrown when the configuration cannot be saved or encrypted.</exception>
    Task SaveSecurityConfigAsync(string configPath, string masterPassword, List<string> authorizedUsers);

    /// <summary>
    /// Loads and decrypts security configuration from the specified path.
    /// Uses Windows Data Protection API (DPAPI) to decrypt the data.
    /// </summary>
    /// <param name="configPath">The file path to the encrypted security configuration.</param>
    /// <returns>A <see cref="EncryptedSettingsService.SecurityData"/> object containing the decrypted data,
    /// or null if the file doesn't exist or cannot be decrypted.</returns>
    /// <exception cref="System.Exception">Thrown when the configuration cannot be loaded or decrypted.</exception>
    Task<EncryptedSettingsService.SecurityData> LoadSecurityConfigAsync(string configPath);

    /// <summary>
    /// Verifies a password against a stored hash.
    /// Uses SHA256 hashing for password verification.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="hash">The hash to verify against.</param>
    /// <returns><c>true</c> if the password matches the hash; otherwise, <c>false</c>.</returns>
    bool VerifyPasswordHash(string password, string hash);
}
