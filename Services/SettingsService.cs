using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIM.Services;

public class SettingsService : ISettingsService
{
    private const string SettingsFile = "settings.json";

    public async Task<Dictionary<string, string>> LoadSettingsAsync()
    {
        if (File.Exists(SettingsFile))
        {
            string json = await File.ReadAllTextAsync(SettingsFile);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        return new();
    }

    public async Task SaveSettingsAsync(Dictionary<string, string> settings)
    {
        string json = JsonSerializer.Serialize(settings);
        await File.WriteAllTextAsync(SettingsFile, json);
    }
}