using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI;

namespace AIM.ViewModels;

public partial class FormGeneratorViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly FormTemplateFactory _templateFactory;
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;

    private static readonly SolidColorBrush ActiveStepColor = new(Color.FromArgb(255, 116, 77, 169));
    private static readonly SolidColorBrush InactiveStepColor = new(Color.FromArgb(255, 128, 128, 128));
    private static readonly SolidColorBrush OhioColor = new(Color.FromArgb(255, 16, 124, 16));
    private static readonly SolidColorBrush IMColor = new(Color.FromArgb(255, 0, 120, 212));
    private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);

    #region Observable Properties
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateFormCommand))]
    [NotifyPropertyChangedFor(nameof(CanGenerate))]
    [NotifyPropertyChangedFor(nameof(HasDirectoryPreview))]
    [NotifyPropertyChangedFor(nameof(FormDirectoryName))]
    [NotifyPropertyChangedFor(nameof(Step3Color))]
    private string _formDirectory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOhioSelected))]
    [NotifyPropertyChangedFor(nameof(IsIMSelected))]
    [NotifyPropertyChangedFor(nameof(OhioCardBorderBrush))]
    [NotifyPropertyChangedFor(nameof(IMCardBorderBrush))]
    private string _selectedTemplate = "Ohio";

    [ObservableProperty]
    private List<string> _availableTemplates;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private string _generatingText = "Generating form...";

    [ObservableProperty]
    private int _previewFolderCount;

    [ObservableProperty]
    private int _previewFileCount;

    [ObservableProperty]
    private string _previewSizeText;

    [ObservableProperty]
    private bool _hasDirectoryPreview;

    [ObservableProperty]
    private bool _hasRecentDirectories;
    #endregion

    public ObservableCollection<string> RecentDirectories { get; } = new();

    #region Computed Properties
    public bool CanGenerate => !string.IsNullOrEmpty(FormDirectory) && !IsGenerating;

    public string FormDirectoryName => !string.IsNullOrEmpty(FormDirectory)
        ? Path.GetFileName(FormDirectory)
        : string.Empty;

    public bool IsOhioSelected => SelectedTemplate == "Ohio";
    public bool IsIMSelected => SelectedTemplate == "I&M";

    public SolidColorBrush OhioCardBorderBrush => IsOhioSelected ? OhioColor : TransparentBrush;
    public SolidColorBrush IMCardBorderBrush => IsIMSelected ? IMColor : TransparentBrush;

    public SolidColorBrush Step2Color => ActiveStepColor;
    public SolidColorBrush Step3Color => CanGenerate ? ActiveStepColor : InactiveStepColor;
    #endregion

    public FormGeneratorViewModel(
        IDialogService dialogService,
        FormTemplateFactory templateFactory,
        INavigationService navigationService,
        ISettingsService settingsService)
    {
        _dialogService = dialogService;
        _templateFactory = templateFactory;
        _navigationService = navigationService;
        _settingsService = settingsService;

        AvailableTemplates = new List<string>(_templateFactory.GetAvailableTemplates());
        LoadRecentDirectories();
    }

    private void LoadRecentDirectories()
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            if (settings.RecentFormDirectories != null && settings.RecentFormDirectories.Count > 0)
            {
                RecentDirectories.Clear();
                foreach (var dir in settings.RecentFormDirectories.Take(5))
                {
                    if (Directory.Exists(dir))
                    {
                        RecentDirectories.Add(dir);
                    }
                }
                HasRecentDirectories = RecentDirectories.Count > 0;
            }
        }
        catch
        {
            HasRecentDirectories = false;
        }
    }

    private void SaveRecentDirectory(string path)
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            settings.RecentFormDirectories ??= new List<string>();

            // Remove if exists, then add to front
            settings.RecentFormDirectories.Remove(path);
            settings.RecentFormDirectories.Insert(0, path);

            // Keep only last 5
            if (settings.RecentFormDirectories.Count > 5)
            {
                settings.RecentFormDirectories = settings.RecentFormDirectories.Take(5).ToList();
            }

            _settingsService.SaveSettings(settings);
            LoadRecentDirectories();
        }
        catch
        {
            // Ignore save errors
        }
    }

    [RelayCommand]
    private void SelectTemplate(string template)
    {
        SelectedTemplate = template;
    }

    [RelayCommand]
    private async Task SelectFormDirectoryAsync()
    {
        var path = await PickFolderAsync();
        if (path != null)
        {
            FormDirectory = path;
            await LoadDirectoryPreviewAsync();
            SaveRecentDirectory(path);
        }
    }

    [RelayCommand]
    private async Task UseRecentDirectoryAsync(string path)
    {
        if (Directory.Exists(path))
        {
            FormDirectory = path;
            await LoadDirectoryPreviewAsync();
            SaveRecentDirectory(path);
        }
        else
        {
            await _dialogService.ShowInfoDialog("Directory Not Found",
                "The selected directory no longer exists and will be removed from recent directories.");

            RecentDirectories.Remove(path);
            HasRecentDirectories = RecentDirectories.Count > 0;
        }
    }

    private async Task LoadDirectoryPreviewAsync()
    {
        if (string.IsNullOrEmpty(FormDirectory) || !Directory.Exists(FormDirectory))
        {
            HasDirectoryPreview = false;
            return;
        }

        try
        {
            var (folders, files, size) = await Task.Run(() =>
            {
                var dirs = Directory.GetDirectories(FormDirectory, "*", SearchOption.AllDirectories);
                var allFiles = Directory.GetFiles(FormDirectory, "*", SearchOption.AllDirectories);
                var totalSize = allFiles.Sum(f => new FileInfo(f).Length);
                return (dirs.Length, allFiles.Length, totalSize);
            });

            PreviewFolderCount = folders;
            PreviewFileCount = files;
            PreviewSizeText = FormatFileSize(size);
            HasDirectoryPreview = true;
        }
        catch
        {
            HasDirectoryPreview = false;
        }

        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(Step3Color));
    }

    private bool CanGenerateForm() => !string.IsNullOrEmpty(FormDirectory) && !IsGenerating;

    [RelayCommand(CanExecute = nameof(CanGenerateForm))]
    private async Task GenerateFormAsync()
    {
        IsGenerating = true;
        GeneratingText = $"Generating {SelectedTemplate} form...";
        OnPropertyChanged(nameof(CanGenerate));

        try
        {
            GeneratingText = "Loading template...";
            var template = _templateFactory.GetTemplate(SelectedTemplate);

            GeneratingText = "Processing inventory data...";
            var formData = await template.GenerateAsync(FormDirectory!);

            GeneratingText = "Opening printable form...";
            _navigationService.NavigateTo(typeof(Views.PrintableFormPage), formData);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Form Generation Failed",
                $"Could not generate the form data.\nError: {ex.Message}");
        }
        finally
        {
            IsGenerating = false;
            OnPropertyChanged(nameof(CanGenerate));
        }
    }

    private async Task<string> PickFolderAsync()
    {
        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
            FileTypeFilter = { "*" }
        };

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}