using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace AIM.Views;

public sealed partial class PreviewPage : Page
{
    public PreviewViewModel ViewModel { get; }

    private int _currentFindIndex = -1;

    public PreviewPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<PreviewViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnNavigatedTo(e.Parameter);
    }

    private void GoBackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    #region Keyboard Accelerators
    private async void SaveAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (ViewModel.SaveContentCommand.CanExecute(null))
            await ViewModel.SaveContentCommand.ExecuteAsync(null);
    }

    private async void OpenAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        await ViewModel.OpenFileCommand.ExecuteAsync(null);
    }

    private void FindAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        FindTextBox.Focus(FocusState.Programmatic);
        FindTextBox.SelectAll();
    }

    private void GoToLineAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        GoToLineNumberBox.Focus(FocusState.Programmatic);
    }

    private void EscapeAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
            args.Handled = true;
        }
    }
    #endregion

    #region Find
    private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentFindIndex = -1;
        var searchText = FindTextBox.Text;

        if (string.IsNullOrEmpty(searchText))
        {
            FindResultsText.Text = "";
            return;
        }

        var content = ViewModel.TextContent ?? "";
        int count = 0, index = 0;
        while ((index = content.IndexOf(searchText, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += searchText.Length;
        }

        if (count > 0)
        {
            FindResultsText.Text = $"{count} match(es)";
            FindResultsText.Opacity = 0.7;
        }
        else
        {
            FindResultsText.Text = "No matches";
            FindResultsText.Opacity = 0.5;
        }
    }

    private void FindTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            FindNext();
            e.Handled = true;
        }
    }

    private void FindNext_Click(object sender, RoutedEventArgs e) => FindNext();
    private void FindPrevious_Click(object sender, RoutedEventArgs e) => FindPrevious();

    private void FindNext()
    {
        var searchText = FindTextBox.Text;
        if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(ViewModel.TextContent))
            return;

        var content = ViewModel.TextContent;
        var startIndex = _currentFindIndex + 1;
        if (startIndex >= content.Length) startIndex = 0;

        var index = content.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
        if (index == -1 && startIndex > 0)
            index = content.IndexOf(searchText, 0, StringComparison.OrdinalIgnoreCase);

        if (index >= 0)
        {
            _currentFindIndex = index;
            ContentTextBox.Select(index, searchText.Length);
            ContentTextBox.Focus(FocusState.Programmatic);
        }
    }

    private void FindPrevious()
    {
        var searchText = FindTextBox.Text;
        if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(ViewModel.TextContent))
            return;

        var content = ViewModel.TextContent;
        var startIndex = _currentFindIndex - 1;
        if (startIndex < 0) startIndex = content.Length - 1;

        var index = content.LastIndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
        if (index == -1)
            index = content.LastIndexOf(searchText, content.Length - 1, StringComparison.OrdinalIgnoreCase);

        if (index >= 0)
        {
            _currentFindIndex = index;
            ContentTextBox.Select(index, searchText.Length);
            ContentTextBox.Focus(FocusState.Programmatic);
        }
    }
    #endregion

    #region Go to Line
    private void GoToLineNumberBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            GoToLine((int)GoToLineNumberBox.Value);
            e.Handled = true;
        }
    }

    private void GoToLineButton_Go_Click(object sender, RoutedEventArgs e)
    {
        GoToLine((int)GoToLineNumberBox.Value);
    }

    private void GoToLine(int lineNumber)
    {
        if (lineNumber < 1 || string.IsNullOrEmpty(ViewModel.TextContent))
            return;

        // Clamp to valid range
        if (lineNumber > ViewModel.LineCount)
            lineNumber = ViewModel.LineCount;

        // Get the line's start position and length
        var (startIndex, length) = ViewModel.GetLineRange(lineNumber);

        // Focus the text box and select the entire line
        ContentTextBox.Focus(FocusState.Programmatic);
        ContentTextBox.Select(startIndex, length);
    }
    #endregion

    #region Editor Events
    private void ContentTextBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateCursorPosition(ContentTextBox.SelectionStart, ContentTextBox.SelectionLength);
    }
    #endregion
}