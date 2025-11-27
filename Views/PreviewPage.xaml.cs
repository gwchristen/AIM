using AIM.Services;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Text.RegularExpressions;

namespace AIM.Views;

public sealed partial class PreviewPage : Page
{
    public PreviewViewModel ViewModel { get; }
    private readonly INavigationService _navigationService;
    private int _currentFindIndex = -1;
    private MatchCollection _findMatches;

    public PreviewPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<PreviewViewModel>();
        _navigationService = Ioc.Default.GetRequiredService<INavigationService>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnNavigatedTo(e.Parameter);
    }

    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);

        // Warn about unsaved changes
        if (ViewModel.IsDirty)
        {
            e.Cancel = true;
            var dialogService = Ioc.Default.GetRequiredService<IDialogService>();
            var result = await dialogService.ShowConfirmationDialogAsync(
                "Unsaved Changes",
                "You have unsaved changes. Do you want to discard them and leave? ");

            if (result)
            {
                ViewModel.DiscardChanges();
                _navigationService.GoBack();
            }
        }
    }

    private void GoBackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_navigationService.CanGoBack)
        {
            _navigationService.GoBack();
        }
    }

    #region Keyboard Accelerators
    private void SaveAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.SaveContentCommand.CanExecute(null))
        {
            ViewModel.SaveContentCommand.Execute(null);
        }
        args.Handled = true;
    }

    private void FindAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.IsTextVisible)
        {
            ShowFindBar();
        }
        args.Handled = true;
    }

    private void EscapeAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (FindBar.Visibility == Visibility.Visible)
        {
            CloseFindBar();
        }
        else if (_navigationService.CanGoBack)
        {
            _navigationService.GoBack();
        }
        args.Handled = true;
    }
    #endregion

    #region Find Functionality
    private void FindButton_Click(object sender, RoutedEventArgs e)
    {
        ShowFindBar();
    }

    private void ShowFindBar()
    {
        FindBar.Visibility = Visibility.Visible;
        FindTextBox.Focus(FocusState.Programmatic);
        FindTextBox.SelectAll();
    }

    private void CloseFindBar()
    {
        FindBar.Visibility = Visibility.Collapsed;
        _findMatches = null;
        _currentFindIndex = -1;
        FindResultsText.Text = "";
        ContentTextBox.Focus(FocusState.Programmatic);
    }

    private void CloseFindBar_Click(object sender, RoutedEventArgs e)
    {
        CloseFindBar();
    }

    private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateFindMatches();
    }

    private void FindTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            FindNext();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            CloseFindBar();
            e.Handled = true;
        }
    }

    private void UpdateFindMatches()
    {
        var searchText = FindTextBox.Text;
        if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(ViewModel.TextContent))
        {
            _findMatches = null;
            _currentFindIndex = -1;
            FindResultsText.Text = "";
            return;
        }

        try
        {
            _findMatches = Regex.Matches(ViewModel.TextContent, Regex.Escape(searchText), RegexOptions.IgnoreCase);
            _currentFindIndex = -1;

            if (_findMatches.Count == 0)
            {
                FindResultsText.Text = "No matches";
            }
            else
            {
                FindResultsText.Text = $"{_findMatches.Count} match(es)";
                FindNext();
            }
        }
        catch
        {
            _findMatches = null;
            FindResultsText.Text = "Invalid search";
        }
    }

    private void FindNext_Click(object sender, RoutedEventArgs e)
    {
        FindNext();
    }

    private void FindPrevious_Click(object sender, RoutedEventArgs e)
    {
        FindPrevious();
    }

    private void FindNext()
    {
        if (_findMatches == null || _findMatches.Count == 0) return;

        _currentFindIndex = (_currentFindIndex + 1) % _findMatches.Count;
        SelectMatch(_findMatches[_currentFindIndex]);
        UpdateFindResultsText();
    }

    private void FindPrevious()
    {
        if (_findMatches == null || _findMatches.Count == 0) return;

        _currentFindIndex = _currentFindIndex <= 0 ? _findMatches.Count - 1 : _currentFindIndex - 1;
        SelectMatch(_findMatches[_currentFindIndex]);
        UpdateFindResultsText();
    }

    private void SelectMatch(Match match)
    {
        ContentTextBox.Focus(FocusState.Programmatic);
        ContentTextBox.Select(match.Index, match.Length);
    }

    private void UpdateFindResultsText()
    {
        if (_findMatches != null && _findMatches.Count > 0)
        {
            FindResultsText.Text = $"{_currentFindIndex + 1} of {_findMatches.Count}";
        }
    }
    #endregion

    #region Status Bar
    private void ContentTextBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        UpdateCursorPosition();
    }

    private void UpdateCursorPosition()
    {
        if (ViewModel.TextContent == null) return;

        var selectionStart = ContentTextBox.SelectionStart;
        var textBeforeCursor = ViewModel.TextContent.Substring(0, Math.Min(selectionStart, ViewModel.TextContent.Length));

        var lineNumber = textBeforeCursor.Split('\n').Length;
        var lastNewLine = textBeforeCursor.LastIndexOf('\n');
        var columnNumber = selectionStart - lastNewLine;

        CursorPositionText.Text = $"Ln {lineNumber}, Col {columnNumber}";
    }
    #endregion
}