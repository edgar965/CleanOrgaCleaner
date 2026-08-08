using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Bilder nachladen und formatfüllend anzeigen.
///
/// Vorher lag dieselbe Logik in vier Fassungen in AufgabePage, AuftragPage und
/// den Foto-Teilen (Vollbild als Überlagerung bzw. als eigene Seite). Diese
/// Klasse ist die einzige Stelle dafür.
/// </summary>
public sealed class BildAnzeige
{
    private readonly ContentPage _seite;
    private readonly string _protokollName;

    public BildAnzeige(ContentPage seite, string protokollName)
    {
        _seite = seite;
        _protokollName = protokollName;
    }

    /// <summary>
    /// Bild mit Anmeldung nachladen und setzen. Reiner Anzeigepfad: Adressen
    /// können leer sein und das Laden kann fehlschlagen - beides wird nur
    /// protokolliert. Das Setzen läuft über <see cref="UiSicher"/>, weil das
    /// Delegate nach dem Abbau der Seite laufen kann.
    /// </summary>
    public void Laden(Image ziel, string? adresse)
    {
        if (string.IsNullOrEmpty(adresse)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                var quelle = await ApiService.Instance.GetImageAsync(adresse);
                if (quelle != null)
                    UiSicher.AufMainThread(() => ziel.Source = quelle, _protokollName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_protokollName}] Bild laden: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Vollbild als Überlagerung über der geöffneten Seite. Antippen oder das
    /// Kreuz schließt sie wieder.
    /// </summary>
    /// <param name="mitAnmeldung">
    /// true: Bild über die angemeldete Verbindung nachladen (geschützte Adressen).
    /// false: Adresse direkt als Quelle verwenden.
    /// </param>
    public void VollbildUeberlagern(string? adresse, bool mitAnmeldung = true)
    {
        if (string.IsNullOrEmpty(adresse) || _seite.Content is not Grid wurzel) return;

        var ueberlagerung = new Grid { BackgroundColor = Color.FromArgb("#E6000000"), ZIndex = 6000 };
        void Schliessen() => wurzel.Children.Remove(ueberlagerung);

        var bild = new Image { Aspect = Aspect.AspectFit, Margin = new Thickness(16, 60, 16, 60) };
        if (mitAnmeldung)
            Laden(bild, adresse);
        else
            bild.Source = adresse;

        var tippen = new TapGestureRecognizer();
        tippen.Tapped += (s, e) => Schliessen();
        ueberlagerung.GestureRecognizers.Add(tippen);

        var kreuz = SchliessKnopf();
        kreuz.Clicked += (s, e) => Schliessen();

        ueberlagerung.Children.Add(bild);
        ueberlagerung.Children.Add(kreuz);

        // Über alle Zeilen der Seite legen, sonst bleibt die Kopfleiste sichtbar
        Grid.SetRowSpan(ueberlagerung, Math.Max(1, wurzel.RowDefinitions.Count));
        wurzel.Children.Add(ueberlagerung);
    }

    /// <summary>
    /// Vollbild als eigene Seite. Antippen oder das Kreuz schließt sie wieder.
    /// </summary>
    public async Task VollbildModalAsync(string? adresse)
    {
        if (string.IsNullOrEmpty(adresse)) return;

        try
        {
            var bild = new Image
            {
                Source = VollstaendigeAdresse(adresse),
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Margin = new Thickness(16, 60, 16, 60)
            };

            var kreuz = SchliessKnopf();
            var seite = new ContentPage
            {
                BackgroundColor = Colors.Black,
                Content = new Grid { Children = { bild, kreuz } }
            };

            async void Schliessen()
            {
                try { await _seite.Navigation.PopModalAsync(); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[{_protokollName}] Vollbild schließen: {ex.Message}");
                }
            }

            var tippen = new TapGestureRecognizer();
            tippen.Tapped += (s, e) => Schliessen();
            bild.GestureRecognizers.Add(tippen);
            kreuz.Clicked += (s, e) => Schliessen();

            await _seite.Navigation.PushModalAsync(seite);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{_protokollName}] Vollbild öffnen: {ex.Message}");
        }
    }

    /// <summary>Relative Serveradressen auf die volle Adresse ergänzen.</summary>
    public static string VollstaendigeAdresse(string adresse)
        => adresse.StartsWith("http") ? adresse : $"{ApiService.BaseUrl}{adresse}";

    /// <summary>
    /// Ein Bild als Bytes holen (zum Weiterverarbeiten, z.B. Markieren).
    /// Bewusst EIN gemeinsamer HttpClient: je Aufruf einen neuen anzulegen
    /// lässt auf Dauer Verbindungen offen (Socket-Erschöpfung).
    /// </summary>
    public static Task<byte[]> HoleBytesAsync(string adresse)
        => Abruf.GetByteArrayAsync(VollstaendigeAdresse(adresse));

    private static readonly HttpClient Abruf = new();

    private static Button SchliessKnopf() => new()
    {
        Text = "✕",
        FontSize = 24,
        FontAttributes = FontAttributes.Bold,
        TextColor = Colors.White,
        BackgroundColor = Colors.Transparent,
        WidthRequest = 50,
        HeightRequest = 50,
        HorizontalOptions = LayoutOptions.End,
        VerticalOptions = LayoutOptions.Start,
        Margin = new Thickness(0, 40, 16, 0)
    };
}
