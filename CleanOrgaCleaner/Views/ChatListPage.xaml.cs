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

        // Initialize header (handles translations, user info, work status, offline banner)
        await Header.InitializeAsync();
        Header.SetPageTitle("chat");

        ApplyTranslations();
        System.Diagnostics.Debug.WriteLine("[ChatListPage] OnAppearing called");
        await LoadCleanersAsync();
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        MessagesLabel.Text = t("messages");
        SelectContactLabel.Text = t("select_contact");
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
        await Shell.Current.GoToAsync($"ChatCurrentPage?partner=admin");
    }

    private async void OnCleanerChatTapped(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CleanerInfo cleaner)
        {
            await Shell.Current.GoToAsync($"ChatCurrentPage?partner={cleaner.Id}");
        }
    }
}
