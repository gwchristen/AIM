using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace AIM.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public MainViewModel MainViewModel => MainWindow.Instance?.ViewModel ?? throw new InvalidOperationException("MainViewModel not available");

    private bool isUnlocked = false;

    public bool IsUnlocked
    {
        get => isUnlocked;
        set
        {
            if (SetProperty(ref isUnlocked, value))
            {
                IsUnlockedChanged?.Invoke(value);
            }
        }
    }

    public event Action<bool> IsUnlockedChanged;
}