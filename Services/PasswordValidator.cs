using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace AIM.Services;

/// <summary>
/// Provides password strength validation for the security system.
/// Enforces strong password policies to protect against brute force attacks.
/// </summary>
public class PasswordValidator
{
    /// <summary>
    /// Minimum required password length.
    /// </summary>
    public const int MinimumLength = 8;

    /// <summary>
    /// Validates a password against security requirements.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="errorMessage">If validation fails, contains a description of the requirement that was not met.</param>
    /// <returns><c>true</c> if the password meets all requirements; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Password requirements:
    /// - Minimum 8 characters
    /// - At least one uppercase letter (A-Z)
    /// - At least one lowercase letter (a-z)
    /// - At least one digit (0-9)
    /// - At least one symbol/special character
    /// </remarks>
    public static bool ValidatePassword(string password, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(password))
        {
            errorMessage = "Password cannot be empty.";
            return false;
        }

        if (password.Length < MinimumLength)
        {
            errorMessage = $"Password must be at least {MinimumLength} characters long.";
            return false;
        }

        if (!password.Any(char.IsUpper))
        {
            errorMessage = "Password must contain at least one uppercase letter (A-Z).";
            return false;
        }

        if (!password.Any(char.IsLower))
        {
            errorMessage = "Password must contain at least one lowercase letter (a-z).";
            return false;
        }

        if (!password.Any(char.IsDigit))
        {
            errorMessage = "Password must contain at least one digit (0-9).";
            return false;
        }

        // Check for at least one special character
        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?`~]"))
        {
            errorMessage = "Password must contain at least one special character (e.g., !@#$%^&*).";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates a password and throws an exception if it doesn't meet requirements.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the password doesn't meet security requirements.</exception>
    public static void ValidatePasswordOrThrow(string password)
    {
        if (!ValidatePassword(password, out string errorMessage))
        {
            throw new ArgumentException(errorMessage, nameof(password));
        }
    }

    /// <summary>
    /// Gets a user-friendly message describing all password requirements.
    /// </summary>
    /// <returns>A formatted string listing all password requirements.</returns>
    public static string GetPasswordRequirementsMessage()
    {
        return $"Password must meet the following requirements:\n" +
               $"• At least {MinimumLength} characters long\n" +
               $"• At least one uppercase letter (A-Z)\n" +
               $"• At least one lowercase letter (a-z)\n" +
               $"• At least one digit (0-9)\n" +
               $"• At least one special character (e.g., !@#$%^&*)";
    }
}
