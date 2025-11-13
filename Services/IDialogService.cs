using System.Threading.Tasks;

namespace AIM.Services;

public interface IDialogService
{
    Task<string> ShowRenameDialogAsync(string currentName);
    Task<bool> ShowConfirmationDialogAsync(string title, string content);
    Task ShowErrorDialogAsync(string title, string content); // This line is new
}