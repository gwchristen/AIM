using AIM.Views;
using AIM.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AIM;

public partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    public MainViewModel ViewModel { get; }

    private ContentControl? _mainContent;
    private Button? _browseButton;
    private Button? _previewButton;
    private Button? _searchButton;
    private Button? _scansButton;
    private Button? _invArchivesButton;
    private Button? _statsButton;
    private Button? _settingsButton;

    private bool isBrowseSelected = true;
    private bool isPreviewSelected;
    private bool isSearchSelected;
    private bool isScansSelected;
    private bool isInvArchivesSelected;
    private bool isStatsSelected;
    private bool isSettingsSelected;

    public bool IsBrowseSelected
    {
        get => isBrowseSelected;
        set => isBrowseSelected = value;
    }

    public bool IsPreviewSelected
    {
        get => isPreviewSelected;
        set => isPreviewSelected = value;
    }

    public bool IsSearchSelected
    {
        get => isSearchSelected;
        set => isSearchSelected = value;
    }

    public bool IsScansSelected
    {
        get => isScansSelected;
        set => isScansSelected = value;
    }

    public bool IsInvArchivesSelected
    {
        get => isInvArchivesSelected;
        set => isInvArchivesSelected = value;
    }

    public bool IsStatsSelected
    {
        get => isStatsSelected;
        set => isStatsSelected = value;
    }

    public bool IsSettingsSelected
    {
        get => isSettingsSelected;
        set => isSettingsSelected = value;
    }

    public MainWindow()
    {
        Instance = this;
        ViewModel = new MainViewModel();
        DataContext = ViewModel;
        
        InitializeComponent();
        
        // Get references to controls
        _mainContent = this.FindControl<ContentControl>("MainContent");
        _browseButton = this.FindControl<Button>("BrowseButton");
        _previewButton = this.FindControl<Button>("PreviewButton");
        _searchButton = this.FindControl<Button>("SearchButton");
        _scansButton = this.FindControl<Button>("ScansButton");
        _invArchivesButton = this.FindControl<Button>("InvArchivesButton");
        _statsButton = this.FindControl<Button>("StatsButton");
        _settingsButton = this.FindControl<Button>("SettingsButton");
        
        NavigateTo(new BrowsePage());
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void NavigateTo(UserControl page)
    {
        if (_mainContent != null)
        {
            _mainContent.Content = page;
        }
    }

    private void UpdateNavigationSelection(Button selectedButton)
    {
        // Remove selected class from all buttons
        _browseButton?.Classes.Remove("selected");
        _previewButton?.Classes.Remove("selected");
        _searchButton?.Classes.Remove("selected");
        _scansButton?.Classes.Remove("selected");
        _invArchivesButton?.Classes.Remove("selected");
        _statsButton?.Classes.Remove("selected");
        _settingsButton?.Classes.Remove("selected");
        
        // Add selected class to the clicked button
        selectedButton?.Classes.Add("selected");
    }

    private async void SelectCustomRootButton_Click(object? sender, RoutedEventArgs e)
    {
        var storageProvider = StorageProvider;
        if (storageProvider == null) return;

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Root Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var folder = folders[0];
            ViewModel.SelectedRoot = folder.Path.LocalPath;
        }
    }

    private void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        NavigateTo(new BrowsePage());
        UpdateNavigationSelection(sender as Button);
        IsBrowseSelected = true;
        IsPreviewSelected = false;
        IsSearchSelected = false;
        IsScansSelected = false;
        IsInvArchivesSelected = false;
        IsStatsSelected = false;
        IsSettingsSelected = false;
    }

    private void PreviewButton_Click(object? sender, RoutedEventArgs e)
    {
        NavigateTo(new PreviewPage());
        UpdateNavigationSelection(sender as Button);
        IsBrowseSelected = false;
        IsPreviewSelected = true;
        IsSearchSelected = false;
        IsScansSelected = false;
        IsInvArchivesSelected = false;
        IsStatsSelected = false;
        IsSettingsSelected = false;
    }

    private void SearchButton_Click(object? sender, RoutedEventArgs e)
    {
        NavigateTo(new SearchPage());
        UpdateNavigationSelection(sender as Button);
        IsBrowseSelected = false;
        IsPreviewSelected = false;
        IsSearchSelected = true;
        IsScansSelected = false;
        IsInvArchivesSelected = false;
        IsStatsSelected = false;
        IsSettingsSelected = false;
    }

    private void ScansButton_Click(object? sender, RoutedEventArgs e)
    {
        NavigateTo(new ScansPage());
        UpdateNavigationSelection(sender as Button);
        IsBrowseSelected = false;
        IsPreviewSelected = false;
        IsSearchSelected = false;
        IsScansSelected = true;
        IsInvArchivesSelected = false;
        IsStatsSelected = false;
        IsSettingsSelected = false;
    }

    private void InvArchivesButton_Click(object? sender, RoutedEventArgs e)
    {
        NavigateTo(new InvArchivesPage());
        UpdateNavigationSelection(sender as Button);
        IsBrowseSelected = false;
        IsPreviewSelected = false;
        IsSearchSelected = false;
        IsScansSelected = false;
        IsInvArchivesSelected = true;
        IsStatsSelected = false;
        IsSettingsSelected = false;
    }

    private void StatsButton_Click(object? sender, RoutedEventArgs e)
    {
        NavigateTo(new StatsPage());
        UpdateNavigationSelection(sender as Button);
        IsBrowseSelected = false;
        IsPreviewSelected = false;
        IsSearchSelected = false;
        IsScansSelected = false;
        IsInvArchivesSelected = false;
        IsStatsSelected = true;
        IsSettingsSelected = false;
    }

    private void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        NavigateTo(new SettingsPage());
        UpdateNavigationSelection(sender as Button);
        IsBrowseSelected = false;
        IsPreviewSelected = false;
        IsSearchSelected = false;
        IsScansSelected = false;
        IsInvArchivesSelected = false;
        IsStatsSelected = false;
        IsSettingsSelected = true;
    }
}
