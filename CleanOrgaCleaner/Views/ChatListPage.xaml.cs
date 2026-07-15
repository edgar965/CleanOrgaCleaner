using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using System.Collections.ObjectModel;

namespace CleanOrgaCleaner.Views;

public partial class ChatListPage : ContentPage
{
    private readonly ApiService _apiService;
    private ObservableCollection<CleanerInfo> _partners;

    public ChatListPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _partners = new ObservableCollection<CleanerInfo>();
        BindableLayout.SetItemsSource(ChatPartnersContainer, _partners);
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

                _partners.Clear();

                // Admin immer als erster Eintrag
                _partners.Add(new CleanerInfo
                {
                    Id = 0,
                    Name = "Admin",
                    Avatar = response.AdminAvatar,
                    IsAdmin = true
                });

                // danach die aktiven Kollegen
                foreach (var c in response.Cleaners)
                {
                    System.Diagnostics.Debug.WriteLine($"[ChatListPage] Adding cleaner: {c.Name} (ID: {c.Id})");
                    _partners.Add(c);
                }
                System.Diagnostics.Debug.WriteLine($"[ChatListPage] Collection now has {_partners.Count} items");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] LoadCleaners error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] Stack trace: {ex.StackTrace}");
        }
    }

    private async void OnPartnerChatTapped(object sender, EventArgs e)
    {
        try
        {
            if (Shell.Current == null) return;
            if (sender is Button btn && btn.CommandParameter is CleanerInfo partner)
            {
                // Admin => partner=admin, sonst die Cleaner-Id
                var partnerId = partner.IsAdmin ? "admin" : partner.Id.ToString();
                var partnerName = Uri.EscapeDataString(partner.Name ?? (partner.IsAdmin ? "Admin" : "Kollege"));
                var partnerAvatar = Uri.EscapeDataString(partner.Avatar ?? "");
                await Shell.Current.GoToAsync($"ChatCurrentPage?partner={partnerId}&partnerName={partnerName}&partnerAvatar={partnerAvatar}");
            }
        }
        catch (Exception ex)
        {
            // async void + Navigation: Shell.Current kann null sein, GoToAsync werfen
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] Chat nav error: {ex.Message}");
        }
    }
}
