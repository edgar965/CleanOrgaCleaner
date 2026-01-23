using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using System.Collections.ObjectModel;

namespace CleanOrgaCleaner.Views;

public partial class ChatListPage : ContentPage
{
    private readonly ApiService _apiService;
    private ObservableCollection<CleanerInfo> _cleaners;

    public ChatListPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _cleaners = new ObservableCollection<CleanerInfo>();
        CleanersCollection.ItemsSource = _cleaners;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplyTranslations();
        System.Diagnostics.Debug.WriteLine("[ChatListPage] OnAppearing called");
        await LoadCleanersAsync();
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        // Header
        MenuButton.Text = "\u2261  " + t("menu") + " \u25BC";
        LogoutButton.Text = t("logout");
        DateLabel.Text = DateTime.Now.ToString("dd.MM.yyyy");
        UserInfoLabel.Text = _apiService.CleanerName ?? Preferences.Get("username", "");
        MessagesLabel.Text = t("messages");
        SelectContactLabel.Text = t("select_contact");
        AdminSectionLabel.Text = t("administration").ToUpper();
        ColleaguesSectionLabel.Text = t("colleagues").ToUpper();
        MenuTodayLabel.Text = t("today");
        MenuChatLabel.Text = t("chat");
        MenuAuftragLabel.Text = t("task");
        MenuSettingsLabel.Text = t("settings");
    }

    private async Task LoadCleanersAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[ChatListPage] Loading cleaners...");
            var cleaners = await _apiService.GetAllCleanersAsync();
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] Got {cleaners.Count} cleaners from API");
            
            _cleaners.Clear();
            foreach (var c in cleaners)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatListPage] Adding cleaner: {c.Name} (ID: {c.Id})");
                _cleaners.Add(c);
            }
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] Collection now has {_cleaners.Count} items");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] LoadCleaners error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] Stack trace: {ex.StackTrace}");
        }
    }

    private async void OnAdminChatTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"ChatCurrentPage?partner=admin");
    }

    private async void OnCleanerChatTapped(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CleanerInfo cleaner)
        {
            await Shell.Current.GoToAsync($"ChatCurrentPage?partner={cleaner.Id}");
        }
    }

    private async void OnMenuTodayClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//TodayPage");
    }

    private void OnMenuChatsClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
    }

    private async void OnMenuSettingsClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//SettingsPage");
    }

    private void OnMenuButtonClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = !MenuOverlayGrid.IsVisible;
    }

    private async void OnLogoTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainTabs/TodayPage");
    }

    private void OnOverlayTapped(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;

        var confirm = await DisplayAlert(
            Translations.Get("logout"),
            Translations.Get("really_logout"),
            Translations.Get("yes"),
            Translations.Get("no"));

        if (!confirm)
            return;

        try
        {
            await _apiService.LogoutAsync();
        }
        catch
        {
            // Ignore errors - we're logging out anyway
        }

        // Clear stored credentials
        Preferences.Remove("property_id");
        Preferences.Remove("username");
        Preferences.Remove("language");
        Preferences.Remove("is_logged_in");
        Preferences.Remove("remember_me");
        Preferences.Remove("biometric_login_enabled");

        // Clear secure storage
        SecureStorage.Remove("password");

        // Disconnect WebSocket
        WebSocketService.Instance.Dispose();

        // Navigate to login page
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void OnMenuAuftragClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//MainTabs/AuftragPage");
    }
}
