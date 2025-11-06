using AIM.Models;
using AIM.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Linq;

namespace AIM.Views;

public partial class BrowsePage : UserControl
{
    public BrowseViewModel ViewModel { get; set; }

    public BrowsePage()
    {
        InitializeComponent();
        ViewModel = new BrowseViewModel();
        DataContext = ViewModel;
        ViewModel.RenameRequested += OnRenameRequested;
        ViewModel.DeleteRequested += OnDeleteRequested;
        ViewModel.ShipRequested += OnShipRequested;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnRenameRequested(string fullPath, string currentName)
    {
        var textBox = new TextBox { Text = currentName, Width = 300 };
        var dialog = new Window
        {
            Title = "Rename File",
            Width = 400,
            Height = 150,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Enter new name:" },
                    textBox,
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 10,
                        Children =
                        {
                            new Button { Content = "Cancel", Width = 80, Tag = "Cancel" },
                            new Button { Content = "Rename", Width = 80, Tag = "OK" }
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
                btn.Click += (s, e) =>
                {
                    result = btn.Tag?.ToString() == "OK";
                    dialog.Close();
                };
            }
        }

        await dialog.ShowDialog(MainWindow.Instance!);
        if (result)
        {
            ViewModel.CompleteRename(textBox.Text);
        }
    }

    private async void OnDeleteRequested(FileItem file)
    {
        var dialog = new Window
        {
            Title = "Delete File",
            Width = 300,
            Height = 150,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Move to archive?" },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 10,
                        Children =
                        {
                            new Button { Content = "No", Width = 80, Tag = "Cancel" },
                            new Button { Content = "Yes", Width = 80, Tag = "OK" }
                        }
                    }
                }
            }
        };

        bool result = false;
        foreach (var child in ((StackPanel)((StackPanel)dialog.Content).Children[1]).Children)
        {
            if (child is Button btn)
            {
                btn.Click += (s, e) =>
                {
                    result = btn.Tag?.ToString() == "OK";
                    dialog.Close();
                };
            }
        }

        await dialog.ShowDialog(MainWindow.Instance!);
        if (result)
        {
            ViewModel.CompleteDelete();
        }
    }

    private async void OnShipRequested(FileItem file)
    {
        var dialog = new Window
        {
            Title = "Ship File",
            Width = 300,
            Height = 150,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Move to shipped folder?" },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 10,
                        Children =
                        {
                            new Button { Content = "No", Width = 80, Tag = "Cancel" },
                            new Button { Content = "Yes", Width = 80, Tag = "OK" }
                        }
                    }
                }
            }
        };

        bool result = false;
        foreach (var child in ((StackPanel)((StackPanel)dialog.Content).Children[1]).Children)
        {
            if (child is Button btn)
            {
                btn.Click += (s, e) =>
                {
                    result = btn.Tag?.ToString() == "OK";
                    dialog.Close();
                };
            }
        }

        await dialog.ShowDialog(MainWindow.Instance!);
        if (result)
        {
            ViewModel.CompleteShip();
        }
    }

    private void ClearLeftLevel1_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedLeftLevel1 = null;
        ViewModel.LeftLevel2.Clear();
        ViewModel.LeftLevel3.Clear();
        ViewModel.UpdateLeftSelectedDirectory();
    }

    private void ClearLeftLevel2_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedLeftLevel2 = null;
        ViewModel.LeftLevel3.Clear();
        ViewModel.UpdateLeftSelectedDirectory();
    }

    private void ClearLeftLevel3_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedLeftLevel3 = null;
        ViewModel.UpdateLeftSelectedDirectory();
    }

    private void ClearRightLevel1_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedRightLevel1 = null;
        ViewModel.RightLevel2.Clear();
        ViewModel.RightLevel3.Clear();
        ViewModel.UpdateRightSelectedDirectory();
    }

    private void ClearRightLevel2_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedRightLevel2 = null;
        ViewModel.RightLevel3.Clear();
        ViewModel.UpdateRightSelectedDirectory();
    }

    private void ClearRightLevel3_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedRightLevel3 = null;
        ViewModel.UpdateRightSelectedDirectory();
    }
}
