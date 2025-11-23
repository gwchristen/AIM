using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Controls;

public sealed partial class PasswordInputControl : UserControl
{
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.Register(
            "Password",
            typeof(string),
            typeof(PasswordInputControl),
            new PropertyMetadata(""));

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(
            "Placeholder",
            typeof(string),
            typeof(PasswordInputControl),
            new PropertyMetadata(""));

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public PasswordInputControl()
    {
        this.InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        Password = PasswordBoxControl.Password;
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Password = TextBoxControl.Text;
    }

    private void ShowPasswordToggle_Click(object sender, RoutedEventArgs e)
    {
        bool isPasswordBoxVisible = PasswordBoxControl.Visibility == Visibility.Visible;

        if (isPasswordBoxVisible)
        {
            // Switch to TextBox
            TextBoxControl.Text = PasswordBoxControl.Password;
            PasswordBoxControl.Visibility = Visibility.Collapsed;
            TextBoxControl.Visibility = Visibility.Visible;
            ShowPasswordToggle.Content = "👁️ Hide";
        }
        else
        {
            // Switch to PasswordBox
            PasswordBoxControl.Password = TextBoxControl.Text;
            TextBoxControl.Visibility = Visibility.Collapsed;
            PasswordBoxControl.Visibility = Visibility.Visible;
            ShowPasswordToggle.Content = "👁️ Show";
        }
    }
}