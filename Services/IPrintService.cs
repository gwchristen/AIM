using Microsoft.UI.Xaml;
using System.Threading.Tasks;

namespace AIM.Services;

public interface IPrintService
{
    Task PrintAsync(UIElement elementToPrint, string jobTitle);
}