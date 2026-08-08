using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;
using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Einstellungen: Sprache, Avatar, angemeldete Person, App-Informationen.
///
/// Biometrie liegt in SettingsPage.Biometrie.cs, die Mitteilungen in
/// SettingsPage.Mitteilungen.cs, die Avatar- und Sprachlisten in
/// Views/Hilfen/.
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly BiometricService _biometricService;

    public SettingsPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _biometricService = BiometricService.Instance;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Kopfleiste kümmert sich um Übersetzungen, Person, Arbeitszeit und Offline-Hinweis
            _ = Header.InitializeAsync();
            Header.SetPageTitle("settings");

            ApplyTranslations();
            LadeAngemeldetePerson();
            LadeAvatarAnzeige();
            LadeSpracheAnzeige();
            _ = LadeBiometrieEinstellungAsync();
            _ = AktualisiereMitteilungsZustandAsync();
        }
        catch (Exception ex)
        {
            // Lifecycle-Handler: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] OnAppearing error: {ex.Message}");
        }
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        Title = t("settings");

        SettingsTitleLabel.Text = t("settings");

        // Angemeldete Person
        LoggedInAsLabel.Text = t("logged_in_as");

        // Avatar
        AvatarHintLabel.Text = t("tap_to_change");
        ChangeAvatarButton.Text = t("change");

        // Sprache
        LanguageTitleLabel.Text = t("language");
        LanguagePicker.Title = t("select_language");

        // Sicherheit / Biometrie
        BiometricTitleLabel.Text = t("security");
        BiometricHintLabel.Text = t("biometric_hint");

        // App-Informationen
        AppInfoLabel.Text = t("app_info");
        VersionLabel.Text = t("version");
        // Echte Build-Version statt hartcodierter Konstante (zeigte "1.52")
        VersionValueLabel.Text = $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";
        ServerLabel.Text = t("server");

        // Mitteilungen
        NotificationsTitleLabel.Text = t("notifications");
        NotificationsLabel.Text = t("push_notifications");
    }

    private void LadeAngemeldetePerson()
    {
        var name = Preferences.Get("username", "");
        var anzeige = string.IsNullOrEmpty(name) ? "Unbekannt" : name;
        UserNameLabel.Text = anzeige;
        AvatarUsernameLabel.Text = anzeige;
    }

    #region Avatar

    private void LadeAvatarAnzeige()
    {
        ZeigeAvatar(Preferences.Get("avatar", ""));
    }

    private void ZeigeAvatar(string avatar)
    {
        CurrentAvatarLabel.Text = string.IsNullOrEmpty(avatar) ? AvatarListe.LogoZeichen : avatar;
    }

    private async void OnChangeAvatarClicked(object? sender, EventArgs e)
    {
        var t = Translations.Get;

        try
        {
            // Auswahlliste: leerer Eintrag steht für das Logo
            var auswahl = AvatarListe.Eintraege
                .Select(a => string.IsNullOrEmpty(a) ? $"{AvatarListe.LogoZeichen} Logo" : a)
                .ToArray();

            var gewaehlt = await DisplayActionSheetAsync(t("select_avatar"), t("cancel"), null, auswahl);
            if (gewaehlt == null || gewaehlt == t("cancel"))
                return;

            var position = Array.IndexOf(auswahl, gewaehlt);
            if (position < 0 || position >= AvatarListe.Eintraege.Count)
                return;

            var neuerAvatar = AvatarListe.Eintraege[position];

            var antwort = await _apiService.SetAvatarAsync(neuerAvatar);
            if (antwort.Success)
            {
                Preferences.Set("avatar", neuerAvatar);
                ZeigeAvatar(neuerAvatar);
                await DisplayAlertAsync(t("saved"), t("avatar_changed"), t("ok"));
            }
            else
            {
                await DisplayAlertAsync(t("error"), antwort.Error ?? t("unknown_error"), t("ok"));
            }
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] SetAvatar error: {ex.Message}");
            await UiSicher.AlertAsync(t("error"), t("connection_error"), t("ok"));
        }
    }

    #endregion

    #region Sprache

    private void LadeSpracheAnzeige()
    {
        var gespeichert = Preferences.Get("language", Sprachliste.Standard);

        // Ohne Abmelden des Handlers würde das Setzen selbst eine Speicherung auslösen
        LanguagePicker.SelectedIndexChanged -= OnLanguageChanged;
        LanguagePicker.SelectedIndex = Sprachliste.Position(gespeichert);
        LanguagePicker.SelectedIndexChanged += OnLanguageChanged;
    }

    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (LanguagePicker.SelectedIndex < 0)
            return;

        var t = Translations.Get;
        var sprache = Sprachliste.Code(LanguagePicker.SelectedIndex);

        try
        {
            var antwort = await _apiService.SetLanguageAsync(sprache);
            if (antwort.Success)
            {
                Preferences.Set("language", sprache);
                Translations.CurrentLanguage = sprache;

                // Oberfläche sofort in der neuen Sprache zeigen
                ApplyTranslations();
                Header.ApplyTranslations();
            }
            else
            {
                await DisplayAlertAsync(t("error"), antwort.Error ?? t("unknown_error"), t("ok"));
            }
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] SetLanguage error: {ex.Message}");
            await UiSicher.AlertAsync(t("error"), t("connection_error"), t("ok"));
        }
    }

    #endregion
}
