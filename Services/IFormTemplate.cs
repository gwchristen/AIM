using AIM.Models;
using System.Threading.Tasks;

namespace AIM.Services;

/// <summary>
/// Interface for form template implementations.
/// Allows different form structures to be generated based on directory layouts.
/// </summary>
public interface IFormTemplate
{
    /// <summary>
    /// Gets the display name of this template.
    /// </summary>
    string TemplateName { get; }

    /// <summary>
    /// Generates a printable form from a directory structure.
    /// </summary>
    /// <param name="directoryPath">Root directory path to generate form from</param>
    /// <returns>A PrintableForm ready for display and printing</returns>
    Task<PrintableForm> GenerateAsync(string directoryPath);
}