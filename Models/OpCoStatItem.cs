using CommunityToolkit.Mvvm.ComponentModel;

namespace AIM.Models;

public partial class OpCoStatItem : ObservableObject
{
    [ObservableProperty]
    private string? _opCoName;

    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private long _deviceCount;
}