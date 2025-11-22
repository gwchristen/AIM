using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace AIM.Services
{
    public interface IDialogService
    {
        Task ShowErrorDialogAsync(string title, string content);
        Task<bool> ShowConfirmationDialogAsync(string title, string content);
        Task<string?> ShowRenameDialogAsync(string currentName);
        Task ShowInfoDialog(string title, string message);
        Task ShowSuccessDialog(string title, string message);
        Task<(ContentDialogResult, string)> ShowTextInputDialog(string title, string message, string defaultText = "");
        Task<string?> ShowPinEntryDialogAsync(string title, string message);

        // Add these two missing methods:
        Task ShowMessageAsync(string title, string message);
        Task<string?> PickFolderAsync();
    }
}