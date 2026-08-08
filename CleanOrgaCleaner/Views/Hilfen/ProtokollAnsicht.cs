using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Darstellung einer Protokollzeile (Zeitpunkt, Person, Vorgang).
/// Aufgaben- und Auftragsseite zeigen dasselbe Protokoll - deshalb hier einmal.
/// </summary>
public static class ProtokollAnsicht
{
    public static View Zeile(string zeitpunkt, string person, string text)
    {
        var inhalt = new VerticalStackLayout { Spacing = 4 };
        inhalt.Children.Add(new Label
        {
            Text = zeitpunkt,
            FontSize = 12,
            TextColor = Color.FromArgb("#999999")
        });
        inhalt.Children.Add(new Label
        {
            Text = person,
            FontSize = 12,
            TextColor = Color.FromArgb("#1a3a5c"),
            FontAttributes = FontAttributes.Bold
        });
        inhalt.Children.Add(new Label
        {
            Text = text,
            FontSize = 14,
            TextColor = Color.FromArgb("#333333")
        });

        return new Border
        {
            BackgroundColor = Color.FromArgb("#f8f9fa"),
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Stroke = Colors.Transparent,
            Padding = 12,
            Margin = new Thickness(0, 0, 0, 8),
            Content = inhalt
        };
    }
}
