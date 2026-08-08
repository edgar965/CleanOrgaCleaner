using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using System.Collections.ObjectModel;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Übersicht der Gesprächspartner: Verwaltung und aktive Kolleginnen/Kollegen.
/// </summary>
public partial class ChatListPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly ObservableCollection<CleanerInfo> _partners = new();

    public ChatListPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        BindableLayout.SetItemsSource(ChatPartnersContainer, _partners);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Kopfleiste kümmert sich um Übersetzungen, Person, Arbeitszeit und Offline-Hinweis
            _ = Header.InitializeAsync();
            Header.SetPageTitle("chat");

            ApplyTranslations();
            _ = LadePartnerAsync();
        }
        catch (Exception ex)
        {
            // Lifecycle-Handler: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] OnAppearing error: {ex.Message}");
        }
    }

    private void ApplyTranslations()
    {
        MessagesLabel.Text = Translations.Get("messages");
    }

    private async Task LadePartnerAsync()
    {
        try
        {
            var antwort = await _apiService.GetCleanersListAsync();

            _partners.Clear();

            // Verwaltung IMMER als erster Eintrag - auch wenn die Kollegenliste
            // offline nicht geladen werden kann, muss dieses Gespräch erreichbar
            // bleiben.
            _partners.Add(new CleanerInfo
            {
                Id = 0,
                Name = "Admin",
                Avatar = antwort?.AdminAvatar ?? "",
                IsAdmin = true
            });

            if (antwort == null) return;

            foreach (var partner in antwort.Cleaners)
                _partners.Add(partner);

            System.Diagnostics.Debug.WriteLine($"[ChatListPage] {_partners.Count} Gesprächspartner");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] LadePartner error: {ex.Message}");
        }
    }

    private async void OnPartnerChatTapped(object sender, EventArgs e)
    {
        try
        {
            if (Shell.Current == null) return;
            if (sender is not Button knopf || knopf.CommandParameter is not CleanerInfo partner) return;

            // Verwaltung => partner=admin, sonst die Id der Arbeitskraft
            var kennung = partner.IsAdmin ? "admin" : partner.Id.ToString();
            var name = Uri.EscapeDataString(partner.Name ?? (partner.IsAdmin ? "Admin" : "Kollege"));
            var avatar = Uri.EscapeDataString(partner.Avatar ?? "");

            await Shell.Current.GoToAsync($"ChatCurrentPage?partner={kennung}&partnerName={name}&partnerAvatar={avatar}");
        }
        catch (Exception ex)
        {
            // async void + Navigation: Shell.Current kann null sein, GoToAsync werfen
            System.Diagnostics.Debug.WriteLine($"[ChatListPage] Chat nav error: {ex.Message}");
        }
    }
}
