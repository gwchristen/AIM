using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace AIM.Services;

public class DialogService : IDialogService
{
    // This is the new method implementation
    public async Task ShowErrorDialogAsync(string title, string content)
    {
        if (App.MainWindow?.Content is not FrameworkElement rootElement)
        {
            return; // Cannot show a dialog if the main window isn't ready.
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK", // An error dialog only needs one button.
            XamlRoot = rootElement.XamlRoot
        };

        await dialog.ShowAsync();
    }

    public async Task<bool> ShowConfirmationDialogAsync(string title, string content)
    {
        if (App.MainWindow?.Content is not FrameworkElement rootElement)
        {
            return false;
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            XamlRoot = rootElement.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public async Task<string> ShowRenameDialogAsync(string currentName)
    {
        if (App.MainWindow?.Content is not FrameworkElement rootElement)
        {
            return null;
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
            return inputTextBox.Text;
        }

        return null;
    }
}