using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace AIM.Models;

public enum FileType { Text, Csv, Log, Other }

public partial class FileItem : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string fullPath = string.Empty;

    [ObservableProperty]
    private FileType type;
}