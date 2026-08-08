using CleanOrgaCleaner.Localization;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Anmeldung von Hand über den Anmeldeknopf.
/// </summary>
public partial class LoginPage
{
    /// <summary>
    /// Direkter await, kein WaitAsync, kein CancellationToken (Modell aus
    /// v1.06, das auf iOS stabil läuft).
    /// </summary>
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        Log("ManualLogin START");

        if (string.IsNullOrWhiteSpace(PropertyIdEntry.Text) ||
            string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            ShowError(Translations.Get("error"));
            return;
        }

        if (!int.TryParse(PropertyIdEntry.Text, out int firmaId))
        {
            ShowError(Translations.Get("error"));
            return;
        }

        LoginButton.IsEnabled = false;
        LoginButton.Text = Translations.Get("loading");
        ErrorLabel.IsVisible = false;

        // Ursprünglichen Haken merken
        var merkenVorher = RememberMeCheckbox.IsChecked;
        Log($"RememberMe original={merkenVorher}");

        // Behelf: Haken kurz setzen (stabilisiert den async-Ablauf auf iOS)
        if (!RememberMeCheckbox.IsChecked)
        {
            RememberMeCheckbox.IsChecked = true;
            await Task.Delay(10);
        }

        Log("LoginAsync START");
        _navigiert = false;

        try
        {
            var ergebnis = await _apiService.LoginAsync(firmaId, UsernameEntry.Text, PasswordEntry.Text);
            Log($"LoginAsync DONE: success={ergebnis?.Success}");

            // iOS: Anzeige-Thread braucht Luft nach dem Netzaufruf
            await Task.Yield();

            if (ergebnis == null)
            {
                Log("Login result is null");
                ShowError(Translations.Get("connection_error"));
                return;
            }

            if (!ergebnis.Success)
            {
                Log($"Login FAILED: {ergebnis.ErrorMessage}");
                ShowError(ergebnis.ErrorMessage ?? Translations.Get("error"));
                return;
            }

            Log("Login SUCCESS - saving credentials");
            Preferences.Set("property_id", PropertyIdEntry.Text);
            Preferences.Set("username", UsernameEntry.Text);
            Preferences.Set("is_logged_in", true);

            await ZugangsdatenMerkenAsync();

            SpracheUebernehmen(ergebnis.CleanerLanguage);

            await SitzungStartenAsync(biometrieAnbieten: true);
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            ShowError($"{Translations.Get("error")}: {ex.Message}");
        }
        finally
        {
            if (!_navigiert)
            {
                // Haken wieder auf den ursprünglichen Stand
                RememberMeCheckbox.IsChecked = merkenVorher;
                KnopfFreigeben();
            }
            Log("ManualLogin END");
        }
    }

    /// <summary>Kennwort je nach Haken sicher ablegen oder verwerfen.</summary>
    private async Task ZugangsdatenMerkenAsync()
    {
        if (!RememberMeCheckbox.IsChecked)
        {
            Preferences.Set("remember_me", false);
            SecureStorage.Remove("password");
            return;
        }

        Preferences.Set("remember_me", true);
        try
        {
            await SecureStorage.SetAsync("password", PasswordEntry.Text);
            Log("SecureStorage.SetAsync DONE");
        }
        catch (Exception ex)
        {
            Log($"SecureStorage save error: {ex.Message}");
        }
    }

    /// <summary>
    /// Nach der ersten erfolgreichen Anmeldung anbieten, künftig per
    /// Fingerabdruck/Gesicht anzumelden.
    /// </summary>
    private async Task BiometrieAnbietenAsync()
    {
        try
        {
            if (!RememberMeCheckbox.IsChecked)
            {
                Log("PromptBiometric: RememberMe=false, skip");
                return;
            }

            if (_biometricService.IsBiometricLoginEnabled())
            {
                Log("PromptBiometric: already enabled, skip");
                return;
            }

            if (!await _biometricService.IsBiometricAvailableAsync())
            {
                Log("PromptBiometric: not available");
                return;
            }

            var art = await _biometricService.GetBiometricTypeAsync();
            Log($"PromptBiometric: type={art}");

            var einschalten = await DisplayAlertAsync(
                art,
                $"Möchten Sie {art} für zukünftige Anmeldungen aktivieren?",
                "Ja",
                "Nein");
            Log($"PromptBiometric: user chose={einschalten}");

            if (!einschalten) return;

            if (await _biometricService.AuthenticateAsync($"{art} einrichten"))
            {
                _biometricService.SetBiometricLoginEnabled(true);
                Log($"{art} enabled");
            }
        }
        catch (Exception ex)
        {
            Log($"PromptBiometric EXCEPTION: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
