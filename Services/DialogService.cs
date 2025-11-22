using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AIM.Services;

public class DialogService : IDialogService
{
    private XamlRoot GetXamlRoot()
    {
        if (App.MainWindow?.Content?.XamlRoot != null)
        {
            return App.MainWindow.Content.XamlRoot;
        }
        throw new InvalidOperationException("XamlRoot is not available. The main window has not been initialized correctly.");
    }

    // Implementing the methods from YOUR interface
    public async Task ShowErrorDialogAsync(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = GetXamlRoot()
        };
        await dialog.ShowAsync();
    }

    public async Task<bool> ShowConfirmationDialogAsync(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            XamlRoot = GetXamlRoot()
        };
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public async Task<string?> ShowRenameDialogAsync(string currentName)
    {
        var inputTextBox = new TextBox { Text = currentName, Height = 32 };
        var dialog = new ContentDialog
        {
            Title = "Rename",
            Content = inputTextBox,
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            return inputTextBox.Text;
        }
        return null;
    }

    public async Task ShowInfoDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = GetXamlRoot()
        };
        await dialog.ShowAsync();
    }

    public async Task ShowSuccessDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = GetXamlRoot()
        };
        await dialog.ShowAsync();
    }

    public async Task<(ContentDialogResult, string)> ShowTextInputDialog(string title, string message, string defaultText = "")
    {
        var inputTextBox = new TextBox { Text = defaultText, Height = 32 };
        var contentPanel = new StackPanel();
        if (!string.IsNullOrEmpty(message))
        {
            contentPanel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });
        }
        contentPanel.Children.Add(inputTextBox);

        var dialog = new ContentDialog
        {
            Title = title,
            Content = contentPanel,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();
        return (result, inputTextBox.Text);
    }

    public async Task<string?> ShowPinEntryDialogAsync(string title, string message)
    {
        var passwordBox = new PasswordBox { MaxLength = 4, Width = 200 };
        var contentPanel = new StackPanel();
        if (!string.IsNullOrEmpty(message))
        {
            contentPanel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });
        }
        contentPanel.Children.Add(passwordBox);

        var dialog = new ContentDialog
        {
            Title = title,
            Content = contentPanel,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            return passwordBox.Password;
        }
        return null;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = App.MainWindow.Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    public async Task<string?> PickFolderAsync()
    {
        var folderPicker = new FolderPicker();
        folderPicker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }
}