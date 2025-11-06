using AIM.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System;
using System.Threading.Tasks;

namespace AIM.Views;

public partial class SettingsPage : UserControl
{
    public MainViewModel ViewModel { get; }

    private bool _isLocked = false;

    public bool IsLocked
    {
        get => _isLocked;
        set => _isLocked = value;
    }

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = MainWindow.Instance!.ViewModel;
        DataContext = this;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void BrowseRootDirectory_Click(object? sender, RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.DefaultRootDirectory = path);
    }

    private async void BrowseArchivePath_Click(object? sender, RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.ArchivePath = path);
    }

    private async void BrowseShippedDirectory_Click(object? sender, RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.ShippedDirectory = path);
    }

    private async void BrowseFileScansDirectory_Click(object? sender, RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.FileScansDirectory = path);
    }

    private async void BrowseInventoryArchiveDirectory_Click(object? sender, RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.InventoryArchiveDirectory = path);
    }

    private async Task BrowseFolderAsync(Action<string> setPath)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            setPath(folders[0].Path.LocalPath);
        }
    }

    private void LockButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!IsLocked)
        {
            IsLocked = true;
        }
    }

    private async void UnlockButton_Click(object? sender, RoutedEventArgs e)
    {
        if (IsLocked)
        {
            var passwordBox = new TextBox { PasswordChar = '*', Width = 200 };
            var dialog = new Window
            {
                Title = "Enter Password",
                Width = 300,
                Height = 150,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock { Text = "Password:" },
                        passwordBox,
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Spacing = 10,
                            Children =
                            {
                                new Button { Content = "Cancel", Width = 80, Tag = "Cancel" },
                                new Button { Content = "Unlock", Width = 80, Tag = "OK" }
                            }
                        }
                    }
                }
            };

            bool result = false;
            foreach (var child in ((StackPanel)((StackPanel)dialog.Content).Children[2]).Children)
            {
                if (child is Button btn)
                {
                    btn.Click += (s, args) =>
                    {
                        result = btn.Tag?.ToString() == "OK";
                        dialog.Close();
                    };
                }
            }

            await dialog.ShowDialog(MainWindow.Instance!);
            if (result && passwordBox.Text == ViewModel.Password)
            {
                IsLocked = false;
            }
        }
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        // Settings are saved automatically via bindings
    }
}
