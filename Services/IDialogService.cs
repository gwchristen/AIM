using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace AIM.Services;

public interface IDialogService
{
    // Your existing methods
    Task ShowErrorDialogAsync(string title, string content);
    Task<bool> ShowConfirmationDialogAsync(string title, string content);
    Task<string?> ShowRenameDialogAsync(string currentName);

    // New methods required by InventoryArchiveViewModel
    Task ShowInfoDialog(string title, string message);
    Task ShowSuccessDialog(string title, string message);
    Task<(ContentDialogResult, string)> ShowTextInputDialog(string title, string message, string defaultText = "");
}