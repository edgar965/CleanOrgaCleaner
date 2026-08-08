using CleanOrgaCleaner.Views.Zeichnen;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Foto markieren: Freihand, Kreis oder Pfeil.
///
/// Die Markierungen selbst liegen als eigene Klassen in Views/Zeichnen/;
/// diese Seite kümmert sich nur um Umrechnung, Berührung und Speichern.
/// </summary>
public partial class ImageAnnotationPage : ContentPage
{
    /// <summary>Strichstärke auf dem Bildschirm.</summary>
    private const float Strichstaerke = 6f;

    /// <summary>Grenzen der Strichstärke im gespeicherten Bild.</summary>
    private const float MinStrichImBild = 3f;
    private const float MaxStrichImBild = 30f;

    private static readonly SKColor Zeichenfarbe = SKColors.Red;
    private static readonly Color WerkzeugAktiv = Color.FromArgb("#E91E63");
    private static readonly Color WerkzeugPassiv = Color.FromArgb("#555");

    private readonly List<ZeichenElement> _elemente = new();
    private SKBitmap? _original;
    private ZeichenElement? _inArbeit;
    private Zeichenwerkzeug _werkzeug = Zeichenwerkzeug.Freihand;

    // Umrechnung Bildschirm <-> Bild
    private float _massstab = 1f;
    private float _versatzX;
    private float _versatzY;

    /// <summary>Markiertes Bild - erst nach dem Speichern gefüllt.</summary>
    public byte[]? AnnotatedImageBytes { get; private set; }

    /// <summary>Hat die Person gespeichert (statt abgebrochen)?</summary>
    public bool WasSaved { get; private set; }

    public ImageAnnotationPage(byte[] imageBytes)
    {
        InitializeComponent();
        LadeBild(imageBytes);
    }

    private void LadeBild(byte[] bytes)
    {
        _original = SKBitmap.Decode(bytes);
        BackgroundImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));

        // Nach dem Laden stimmt die Größe erst - dann neu umrechnen
        BackgroundImage.SizeChanged += (s, e) => BerechneUmrechnung();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        BerechneUmrechnung();
    }

    /// <summary>
    /// Maßstab und Versatz zwischen Zeichenfläche und Bild bestimmen. Das Bild
    /// wird mittig eingepasst (AspectFit).
    /// </summary>
    private void BerechneUmrechnung()
    {
        if (_original == null || CanvasView.CanvasSize.Width <= 0 || CanvasView.CanvasSize.Height <= 0) return;

        var breite = CanvasView.CanvasSize.Width;
        var hoehe = CanvasView.CanvasSize.Height;

        _massstab = Math.Min(breite / _original.Width, hoehe / _original.Height);
        _versatzX = (breite - _original.Width * _massstab) / 2f;
        _versatzY = (hoehe - _original.Height * _massstab) / 2f;
    }

    private SKPoint AufBild(SKPoint bildschirmpunkt) => new(
        (bildschirmpunkt.X - _versatzX) / _massstab,
        (bildschirmpunkt.Y - _versatzY) / _massstab);

    private void OnToolSelected(object sender, EventArgs e)
    {
        if (sender is not Button knopf) return;

        _werkzeug = knopf.ClassId switch
        {
            "circle" => Zeichenwerkzeug.Kreis,
            "arrow" => Zeichenwerkzeug.Pfeil,
            _ => Zeichenwerkzeug.Freihand
        };

        BtnFreehand.BackgroundColor = Werkzeugfarbe(Zeichenwerkzeug.Freihand);
        BtnCircle.BackgroundColor = Werkzeugfarbe(Zeichenwerkzeug.Kreis);
        BtnArrow.BackgroundColor = Werkzeugfarbe(Zeichenwerkzeug.Pfeil);
    }

    private Color Werkzeugfarbe(Zeichenwerkzeug werkzeug)
        => _werkzeug == werkzeug ? WerkzeugAktiv : WerkzeugPassiv;

    private void OnUndoClicked(object sender, EventArgs e)
    {
        if (_elemente.Count == 0) return;
        _elemente.RemoveAt(_elemente.Count - 1);
        CanvasView.InvalidateSurface();
    }

    private void OnTouch(object sender, SKTouchEventArgs e)
    {
        var punkt = AufBild(e.Location);

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _inArbeit = NeuesElement(punkt);
                e.Handled = true;
                break;

            case SKTouchAction.Moved:
                if (_inArbeit == null) break;
                Erweitere(_inArbeit, punkt);
                CanvasView.InvalidateSurface();
                e.Handled = true;
                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                if (_inArbeit == null) break;
                if (_inArbeit.IstGrossGenug)
                    _elemente.Add(_inArbeit);
                _inArbeit = null;
                CanvasView.InvalidateSurface();
                e.Handled = true;
                break;
        }
    }

    private ZeichenElement NeuesElement(SKPoint punkt)
    {
        switch (_werkzeug)
        {
            case Zeichenwerkzeug.Kreis:
                return new KreisElement { Mitte = punkt, Farbe = Zeichenfarbe };
            case Zeichenwerkzeug.Pfeil:
                return new PfeilElement { Start = punkt, Ende = punkt, Farbe = Zeichenfarbe };
            default:
                var freihand = new FreihandElement { Farbe = Zeichenfarbe };
                freihand.Punkte.Add(punkt);
                return freihand;
        }
    }

    private static void Erweitere(ZeichenElement element, SKPoint punkt)
    {
        switch (element)
        {
            case FreihandElement freihand:
                freihand.Punkte.Add(punkt);
                break;
            case KreisElement kreis:
                kreis.Radius = SKPoint.Distance(kreis.Mitte, punkt);
                break;
            case PfeilElement pfeil:
                pfeil.Ende = punkt;
                break;
        }
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        // Bei jedem Zeichnen neu umrechnen: die Fläche kann sich geändert haben
        BerechneUmrechnung();

        var flaeche = e.Surface.Canvas;
        flaeche.Clear(SKColors.Transparent);

        flaeche.Save();
        flaeche.Translate(_versatzX, _versatzY);
        flaeche.Scale(_massstab);

        using var stift = ErzeugeStift(Strichstaerke);
        ZeichneAlles(flaeche, stift);

        flaeche.Restore();
    }

    private void ZeichneAlles(SKCanvas flaeche, SKPaint stift)
    {
        foreach (var element in _elemente)
        {
            stift.Color = element.Farbe;
            element.Zeichne(flaeche, stift);
        }

        if (_inArbeit != null)
        {
            stift.Color = _inArbeit.Farbe;
            _inArbeit.Zeichne(flaeche, stift);
        }
    }

    private static SKPaint ErzeugeStift(float staerke) => new()
    {
        Color = Zeichenfarbe,
        StrokeWidth = staerke,
        Style = SKPaintStyle.Stroke,
        IsAntialias = true,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round
    };

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        WasSaved = false;
        await SchliesseAsync();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_original == null)
        {
            await SchliesseAsync();
            return;
        }

        try
        {
            using var ergebnis = _original.Copy();
            using var flaeche = new SKCanvas(ergebnis);

            // Strichstärke auf die Bildgröße umrechnen: 6 Punkte auf dem
            // Bildschirm sollen im gespeicherten Bild gleich dick wirken.
            float staerke = _massstab > 0 ? Strichstaerke / _massstab : Strichstaerke;
            staerke = Math.Clamp(staerke, MinStrichImBild, MaxStrichImBild);

            using var stift = ErzeugeStift(staerke);
            foreach (var element in _elemente)
            {
                stift.Color = element.Farbe;
                element.Zeichne(flaeche, stift);
            }

            using var bild = SKImage.FromBitmap(ergebnis);
            using var daten = bild.Encode(SKEncodedImageFormat.Jpeg, 90);
            AnnotatedImageBytes = daten.ToArray();

            WasSaved = true;
            await SchliesseAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ImageAnnotationPage] Speichern: {ex}");
            await DisplayAlertAsync("Fehler", "Bild konnte nicht gespeichert werden", "OK");
        }
    }

    /// <summary>
    /// Seite schließen. async void Handler: das Schließen darf nie werfen,
    /// sonst beendet sich die App. Danach das entschlüsselte Bild freigeben -
    /// SKBitmap belegt je Foto mehrere MB außerhalb der Speicherverwaltung und
    /// blieb bisher bis zur nächsten Bereinigung liegen.
    /// </summary>
    private async Task SchliesseAsync()
    {
        try { await Navigation.PopModalAsync(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ImageAnnotationPage] Schließen: {ex.Message}");
        }
        finally
        {
            _original?.Dispose();
            _original = null;
        }
    }
}
