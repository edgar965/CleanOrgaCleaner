using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Aufbau der Aufgaben-Knöpfe der Tagesliste.
/// </summary>
public partial class TodayPage
{
    private static readonly Color FarbeOffen = Color.FromArgb("#2196F3");
    private static readonly Color FarbeErledigt = Color.FromArgb("#4CAF50");

    private void BuildTaskGrid()
    {
        TasksStackLayout.Children.Clear();

        if (_tasks.Count == 0)
        {
            ZeigeLeerzustand();
            return;
        }

        EmptyStateView.IsVisible = false;
        TaskRefreshView.IsVisible = true;

        foreach (var aufgabe in _tasks)
            TasksStackLayout.Children.Add(ErzeugeAufgabenknopf(aufgabe));

        Log($"BuildTaskGrid: {_tasks.Count} Aufgaben aufgebaut");
    }

    private void ZeigeLeerzustand()
    {
        EmptyStateView.IsVisible = true;
        TaskRefreshView.IsVisible = false;
    }

    private View ErzeugeAufgabenknopf(CleaningTask aufgabe)
    {
        var erledigt = aufgabe.IsCompleted;
        var farbe = erledigt ? FarbeErledigt : FarbeOffen;

        var beschriftung = string.IsNullOrEmpty(aufgabe.ApartmentName)
            ? aufgabe.Aufgabenart
            : $"{aufgabe.ApartmentName}  {aufgabe.Aufgabenart}";

        var knopf = new Button
        {
            Text = erledigt ? $"✓ {beschriftung}" : beschriftung,
            BackgroundColor = farbe,
            TextColor = Colors.White,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 15,
            Padding = new Thickness(25, 18),
            HorizontalOptions = LayoutOptions.Fill,
            Shadow = new Shadow
            {
                Brush = farbe,
                Offset = new Point(0, 3),
                Radius = 10,
                Opacity = erledigt ? 0.35f : 0.3f
            }
        };

        knopf.Clicked += async (s, e) => await OeffneAufgabeAsync(aufgabe);
        return knopf;
    }

    /// <summary>
    /// Aufgabe öffnen - aber nur, wenn die Arbeitszeit läuft. Sonst würde die
    /// Zeit nicht erfasst.
    /// </summary>
    private async Task OeffneAufgabeAsync(CleaningTask aufgabe)
    {
        try
        {
            Header.CloseMenu();

            if (!Header.IsWorking)
            {
                await DisplayAlertAsync(
                    Translations.Get("attention"),
                    Translations.Get("start_work_first"),
                    Translations.Get("ok"));
                return;
            }

            if (Shell.Current == null) return;
            await Shell.Current.GoToAsync($"AufgabePage?taskId={aufgabe.Id}");
        }
        catch (Exception ex)
        {
            // Aufrufer ist ein async void Clicked-Lambda - nie werfen lassen
            Log($"OeffneAufgabe error: {ex.Message}");
        }
    }
}
