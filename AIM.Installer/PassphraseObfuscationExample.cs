using System;
using System.Text;

namespace AIM.Installer
{
    /// <summary>
    /// Example utility for generating obfuscated passphrases for the installer.
    /// This is NOT part of the installer build - it's a utility for developers.
    /// 
    /// USAGE:
    /// 1. Update the plainPassphrase constant below
    /// 2. Run this code to generate the obfuscated value
    /// 3. Copy the output to InstallerForm.ObfuscatedPassphrase constant
    /// 4. Delete this file or exclude from build
    /// 
    /// SECURITY WARNING:
    /// - This is obfuscation, NOT encryption
    /// - The passphrase can be extracted by examining the installer binary
    /// - For production, use Azure Key Vault, certificates, or HSM instead
    /// </summary>
    public class PassphraseObfuscationExample
    {
        // XOR key - must match the one in InstallerForm and SecurityService
        private static readonly byte[] XorKey = new byte[] { 0xA5, 0x3C, 0x7E, 0x91, 0x42, 0xF8, 0x6D, 0x2B };

        /// <summary>
        /// Example usage - update this passphrase and run to generate obfuscated value
        /// </summary>
        public static void Main()
        {
            // CHANGE THIS to your actual passphrase
            string plainPassphrase = "MySecureP@ssphrase2024!";

            // Generate obfuscated version
            string obfuscated = ObfuscatePassphrase(plainPassphrase);
            Console.WriteLine($"Plain passphrase: {plainPassphrase}");
            Console.WriteLine($"Obfuscated value: {obfuscated}");
            Console.WriteLine();
            Console.WriteLine("Copy the obfuscated value to:");
            Console.WriteLine("AIM.Installer/InstallerForm.cs -> ObfuscatedPassphrase constant");

            // Verify deobfuscation works
            string deobfuscated = DeobfuscatePassphrase(obfuscated);
            Console.WriteLine();
            Console.WriteLine($"Verification - Deobfuscated: {deobfuscated}");
            Console.WriteLine($"Match: {plainPassphrase == deobfuscated}");
        }

        private static string ObfuscatePassphrase(string passphrase)
        {
            byte[] data = Encoding.UTF8.GetBytes(passphrase);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= XorKey[i % XorKey.Length];
            }
            return Convert.ToBase64String(data);
        }

        private static string DeobfuscatePassphrase(string obfuscated)
        {
            byte[] data = Convert.FromBase64String(obfuscated);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= XorKey[i % XorKey.Length];
            }
            return Encoding.UTF8.GetString(data);
        }
    }
}
