using AIM.Models;

namespace AIM.Services;

public interface ISettingsService
{
    // This now correctly returns the settings object
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
}