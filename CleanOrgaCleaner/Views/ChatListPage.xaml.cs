using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using System.Collections.ObjectModel;

namespace CleanOrgaCleaner.Views;

public partial class ChatListPage : ContentPage
{
    private readonly ApiService _apiService;
    private ObservableCollection<CleanerInfo> _cleaners;
    private string _adminAvatar = "A";

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

        try
        {
            // Initialize header (handles translations, user info, work status, offline banner)
            _ = Header.InitializeAsync();
            Header.SetPageTitle("chat");

            ApplyTranslations();
            System.Diagnostics.Debug.WriteLine("[ChatListPage] OnAppearing called");
            _ = LoadCleanersAsync();
        }
        catch (Exception ex)
        {
            // async void Lifecycle-Handler: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] OnAppearing error: {ex.Message}");
        }
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        MessagesLabel.Text = t("messages");
        AdminSectionLabel.Text = t("administration").ToUpper();
        ColleaguesSectionLabel.Text = t("colleagues").ToUpper();
    }

    private async Task LoadCleanersAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[ChatListPage] Loading cleaners...");
            var response = await _apiService.GetCleanersListAsync();

            if (response != null)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatListPage] Got {response.Cleaners.Count} cleaners from API");
                System.Diagnostics.Debug.WriteLine($"[ChatListPage] Admin avatar: '{response.AdminAvatar}'");

                _cleaners.Clear();
                foreach (var c in response.Cleaners)
                {
                    System.Diagnostics.Debug.WriteLine($"[ChatListPage] Adding cleaner: {c.Name} (ID: {c.Id})");
                    _cleaners.Add(c);
                }
                System.Diagnostics.Debug.WriteLine($"[ChatListPage] Collection now has {_cleaners.Count} items");

                // Set admin avatar
                if (!string.IsNullOrEmpty(response.AdminAvatar))
                {
                    _adminAvatar = response.AdminAvatar;
                    AdminAvatarLabel.Text = _adminAvatar;
                    AdminAvatarLabel.FontSize = 32;
                }
                else
                {
                    AdminAvatarLabel.Text = "A";
                    AdminAvatarLabel.FontSize = 20;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] LoadCleaners error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] Stack trace: {ex.StackTrace}");
        }
    }

    private async void OnAdminChatTapped(object sender, EventArgs e)
    {
        try
        {
            if (Shell.Current == null) return;
            var avatarEncoded = Uri.EscapeDataString(_adminAvatar);
            await Shell.Current.GoToAsync($"ChatCurrentPage?partner=admin&partnerName=Admin&partnerAvatar={avatarEncoded}");
        }
        catch (Exception ex)
        {
            // async void + Navigation: Shell.Current kann null sein, GoToAsync werfen
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] Admin chat nav error: {ex.Message}");
        }
    }

    private async void OnCleanerChatTapped(object sender, EventArgs e)
    {
        try
        {
            if (Shell.Current == null) return;
            if (sender is Button btn && btn.CommandParameter is CleanerInfo cleaner)
            {
                var partnerName = Uri.EscapeDataString(cleaner.Name ?? "Kollege");
                var partnerAvatar = Uri.EscapeDataString(cleaner.Avatar ?? "");
                await Shell.Current.GoToAsync($"ChatCurrentPage?partner={cleaner.Id}&partnerName={partnerName}&partnerAvatar={partnerAvatar}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] Cleaner chat nav error: {ex.Message}");
        }
    }
}
