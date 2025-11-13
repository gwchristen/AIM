using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.ViewModels;

public partial class InventoryAdminViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly DirectoryOperationService _directoryOperationService;
    private readonly INavigationService _navigationService;

    // --- Properties for Copy Structure (CORRECTED) ---
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateStructureCommand))] // THE FIX
    private string? _sourceDirectory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateStructureCommand))] // THE FIX
    private string? _destinationDirectory;

    private bool CanCreateStructure() => !string.IsNullOrEmpty(SourceDirectory) && !string.IsNullOrEmpty(DestinationDirectory);

    // --- Properties for Form Generation ---
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateFormCommand))] // Proactive fix for the next button
    private string? _formDirectory;

    private bool CanGenerateForm() => !string.IsNullOrEmpty(FormDirectory);

    // --- Properties for Renaming ---
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RenameFilesCommand))]
    private string? _renameDirectory;

    private bool CanRenameFiles() => !string.IsNullOrEmpty(RenameDirectory);

    // --- Properties for Stats ---
    [ObservableProperty]
    private string? _statsDirectory;

    [ObservableProperty]
    private ObservableCollection<OpCoStatItem> _opCoStats;

    // --- Properties for Anomaly Detection ---
    [ObservableProperty]
    private string? _anomalyDirectory;

    [ObservableProperty]
    private ObservableCollection<string> _misplacedOhFiles;

    [ObservableProperty]
    private ObservableCollection<string> _misplacedImFiles;

    [ObservableProperty]
    private ObservableCollection<string> _unidentifiedFiles;

    // --- Constructor ---
    public InventoryAdminViewModel(IDialogService dialogService, DirectoryOperationService directoryOperationService, INavigationService navigationService)
    {
        _dialogService = dialogService;
        _directoryOperationService = directoryOperationService;
        _navigationService = navigationService;

        _opCoStats = new ObservableCollection<OpCoStatItem>();
        _misplacedOhFiles = new ObservableCollection<string>();
        _misplacedImFiles = new ObservableCollection<string>();
        _unidentifiedFiles = new ObservableCollection<string>();
    }

    // --- Change Handlers ---
    partial void OnStatsDirectoryChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            CalculateStatsCommand.Execute(null);
        }
        else
        {
            OpCoStats.Clear();
        }
    }

    partial void OnAnomalyDirectoryChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            ScanForAnomaliesCommand.Execute(null);
        }
        else
        {
            ClearAnomalyResults();
        }
    }

    // --- Commands ---
    [RelayCommand]
    private async Task SelectSourceAsync() => SourceDirectory = await PickFolderAsync();

    [RelayCommand]
    private async Task SelectDestinationAsync() => DestinationDirectory = await PickFolderAsync();

    [RelayCommand(CanExecute = nameof(CanCreateStructure))]
    private async Task CreateStructureAsync()
    {
        var (result, newName) = await _dialogService.ShowTextInputDialog("Enter New Directory Name", "Please provide a name for the new directory structure.");
        if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary || string.IsNullOrWhiteSpace(newName)) return;

        try
        {
            await _directoryOperationService.CopyDirectoryStructureAsync(SourceDirectory!, DestinationDirectory!, newName);
            await _dialogService.ShowSuccessDialog("Success", $"The directory structure was successfully created at '{System.IO.Path.Combine(DestinationDirectory!, newName)}'.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Operation Failed", $"Could not create the directory structure.\nError: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SelectFormDirectoryAsync() => FormDirectory = await PickFolderAsync();

    [RelayCommand(CanExecute = nameof(CanGenerateForm))]
    private async Task GenerateFormAsync()
    {
        try
        {
            var formData = await _directoryOperationService.GenerateFormDataAsync(FormDirectory!);
            _navigationService.NavigateTo(typeof(Views.PrintableFormPage), formData);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Form Generation Failed", $"Could not generate the form data.\nError: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SelectRenameDirectoryAsync() => RenameDirectory = await PickFolderAsync();

    [RelayCommand(CanExecute = nameof(CanRenameFiles))]
    private async Task RenameFilesAsync()
    {
        bool confirmed = await _dialogService.ShowConfirmationDialogAsync("Confirm Mass Rename", "This will rename all files within the selected directory's OpCo subfolders.\nThis action is permanent and cannot be easily undone.\n\nAre you sure you want to proceed?");
        if (!confirmed) return;

        try
        {
            var summary = await _directoryOperationService.RenameFilesSequentiallyAsync(RenameDirectory!);
            var summaryText = new StringBuilder("Renaming complete.\n\nSummary:\n");
            if (summary.Any())
            {
                foreach (var entry in summary) summaryText.AppendLine($"- {entry.Key}: {entry.Value} files renamed.");
            }
            else
            {
                summaryText.AppendLine("No files were found to rename.");
            }
            await _dialogService.ShowSuccessDialog("Success", summaryText.ToString());
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Renaming Failed", $"An error occurred during the renaming process.\nError: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SelectStatsDirectoryAsync() => StatsDirectory = await PickFolderAsync();

    [RelayCommand]
    private async Task CalculateStatsAsync()
    {
        if (string.IsNullOrEmpty(StatsDirectory)) return;
        try
        {
            var stats = await _directoryOperationService.GetDirectoryStatsAsync(StatsDirectory);
            OpCoStats.Clear();
            foreach (var stat in stats) OpCoStats.Add(stat);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Stats Calculation Failed", $"An error occurred.\nError: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SelectAnomalyDirectoryAsync() => AnomalyDirectory = await PickFolderAsync();

    [RelayCommand]
    private async Task ScanForAnomaliesAsync()
    {
        if (string.IsNullOrEmpty(AnomalyDirectory)) return;
        try
        {
            var report = await _directoryOperationService.FindFileAnomaliesAsync(AnomalyDirectory);
            ClearAnomalyResults();
            report.MisplacedOhFiles.ForEach(MisplacedOhFiles.Add);
            report.MisplacedImFiles.ForEach(MisplacedImFiles.Add);
            report.UnidentifiedFiles.ForEach(UnidentifiedFiles.Add);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Scan Failed", $"An error occurred.\nError: {ex.Message}");
        }
    }

    // --- Helper Methods ---
    private void ClearAnomalyResults()
    {
        MisplacedOhFiles.Clear();
        MisplacedImFiles.Clear();
        UnidentifiedFiles.Clear();
    }

    private async Task<string?> PickFolderAsync()
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
}