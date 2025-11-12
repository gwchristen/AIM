using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace AIM.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationDialogAsync(string title, string content)
    {
        // A confirmation dialog just needs a title, content, and standard buttons.
        if (App.MainWindow?.Content is not FrameworkElement rootElement)
        {
            return false; // Cannot show a dialog if the main window isn't ready.
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            XamlRoot = rootElement.XamlRoot // This is crucial for WinUI 3 dialogs.
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public async Task<string> ShowRenameDialogAsync(string currentName)
    {
        // A rename dialog is more complex; it needs a TextBox inside.
        if (App.MainWindow?.Content is not FrameworkElement rootElement)
        {
            return null; // Cannot show a dialog.
        }

        var inputTextBox = new TextBox
        {
            AcceptsReturn = false,
            Height = 32,
            Text = currentName,
            SelectionStart = currentName.Length
        };

        var dialog = new ContentDialog
        {
            Title = "Rename File",
            Content = inputTextBox,
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            XamlRoot = rootElement.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // If the user clicked "Rename", return the text from the TextBox.
            return inputTextBox.Text;
        }

        // Otherwise, return null.
        return null;
    }
}