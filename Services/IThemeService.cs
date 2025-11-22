namespace AIM.Services
{
    public interface IThemeService
    {
        void InitializeTheme();
        void SetTheme(string themeName);  // Add this line
    }
}