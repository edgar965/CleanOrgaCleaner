using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views.Components;

public partial class AppHeader : ContentView
{
    public event EventHandler? MenuOpened;
    public event EventHandler? MenuClosed;

    public AppHeader()
    {
        InitializeComponent();
        ApplyTranslations();
        UpdateUserInfo();
    }

    public void ApplyTranslations()
    {
        MenuButton.Text = "\u2261  " + Translations.Get("menu") + " \u25BC";
        LogoutButton.Text = Translations.Get("logout");
    }

    public void UpdateUserInfo()
    {
        DateLabel.Text = DateTime.Now.ToString("dd.MM.yyyy");
        UserInfoLabel.Text = ApiService.Instance.CleanerName ?? Preferences.Get("username", "");
    }

    private void OnMenuButtonClicked(object sender, EventArgs e)
    {
        MenuOpened?.Invoke(this, EventArgs.Empty);
    }

    private async void OnLogoTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainTabs/TodayPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await ApiService.Instance.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
