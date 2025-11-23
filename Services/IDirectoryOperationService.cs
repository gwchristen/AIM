using AIM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIM.Services;

/// <summary>
/// Interface for directory structure operations and form data generation.
/// Provides methods for generating printable forms from directory structures,
/// copying directory hierarchies, batch file renaming, directory statistics, and file anomaly detection.
/// </summary>
public interface IDirectoryOperationService
{
    /// <summary>
    /// Generates form data from a directory structure asynchronously.
    /// Traverses the directory hierarchy and creates a printable form based on the structure.
    /// The form is populated with headers and items representing directories and files.
    /// </summary>
    /// <param name="opCoDirectoryPath">The path to the operating company directory to process.</param>
    /// <returns>A <see cref="PrintableForm"/> containing the generated form data with headers and items.</returns>
    /// <remarks>
    /// This method expects a specific directory structure:
    /// - Level 1: Operating company directory (root)
    /// - Level 2: Subdirectories representing major categories
    /// - Level 3: Subdirectories with specific naming conventions (3a, 3b, 3c)
    /// Level 3 directories with "3c" in their name will have file contents read and included in the form.
    /// </remarks>
    Task<PrintableForm> GenerateFormDataAsync(string opCoDirectoryPath);

    /// <summary>
    /// Copies the structure of a source directory to a destination directory with a new name.
    /// Only the directory hierarchy is copied; files are not copied.
    /// </summary>
    /// <param name="sourceDir">The source directory path to copy the structure from.</param>
    /// <param name="destinationDir">The destination directory path where the structure will be created.</param>
    /// <param name="newDirName">The name for the new directory at the destination.</param>
    /// <returns>A task representing the asynchronous copy operation.</returns>
    /// <exception cref="System.IO.IOException">Thrown when a directory with the same name already exists at the destination.</exception>
    Task CopyDirectoryStructureAsync(string sourceDir, string destinationDir, string newDirName);

    /// <summary>
    /// Renames files sequentially within subdirectories of a root directory.
    /// Files are renamed using the pattern: {DirectoryName}_{Counter:D4}{Extension}
    /// </summary>
    /// <param name="rootDirectoryPath">The root directory containing subdirectories with files to rename.</param>
    /// <returns>A dictionary mapping directory names to the count of files renamed in each directory.</returns>
    /// <exception cref="System.IO.DirectoryNotFoundException">Thrown when the root directory does not exist.</exception>
    /// <remarks>
    /// Each subdirectory of the root is processed independently.
    /// Files are sorted by full path before renaming to ensure consistent ordering.
    /// If a naming conflict occurs, a GUID suffix is added to ensure uniqueness.
    /// </remarks>
    Task<Dictionary<string, int>> RenameFilesSequentiallyAsync(string rootDirectoryPath);

    /// <summary>
    /// Analyzes directories and generates statistics for each subdirectory.
    /// Counts files and devices (non-empty lines in files) for each operating company directory.
    /// </summary>
    /// <param name="rootDirectoryPath">The root directory path to analyze.</param>
    /// <returns>A list of <see cref="OpCoStatItem"/> objects containing statistics for each subdirectory.</returns>
    /// <exception cref="System.IO.DirectoryNotFoundException">Thrown when the root directory does not exist.</exception>
    /// <remarks>
    /// Device count is calculated by reading text files and counting non-empty lines.
    /// File read errors are silently ignored and don't contribute to the device count.
    /// </remarks>
    Task<List<OpCoStatItem>> GetDirectoryStatsAsync(string rootDirectoryPath);

    /// <summary>
    /// Finds file anomalies in a directory structure.
    /// Identifies files that contain location identifiers (OH, I&amp;M) but are located in incorrect directories.
    /// Also identifies files with no recognizable location identifier.
    /// </summary>
    /// <param name="rootDirectoryPath">The root directory path to scan for anomalies.</param>
    /// <returns>A <see cref="FileAnomalyReport"/> containing lists of misplaced and unidentified files.</returns>
    /// <exception cref="System.IO.DirectoryNotFoundException">Thrown when the root directory does not exist.</exception>
    /// <remarks>
    /// Expected directory structure includes "Ohio" and "I&amp;M" subdirectories at the root level.
    /// Files are classified based on filename patterns:
    /// - OH pattern indicates Ohio files (should be in Ohio directory)
    /// - I&amp;M, I+M, IM patterns indicate I&amp;M files (should be in I&amp;M directory)
    /// - Files without these patterns are marked as unidentified
    /// </remarks>
    Task<FileAnomalyReport> FindFileAnomaliesAsync(string rootDirectoryPath);
}
