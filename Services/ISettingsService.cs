using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIM.Services;

public interface ISettingsService
{
    Task<Dictionary<string, string>> LoadSettingsAsync();
    Task SaveSettingsAsync(Dictionary<string, string> settings);
}