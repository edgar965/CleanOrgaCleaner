using SkiaSharp;

namespace CleanOrgaCleaner.Views.Zeichnen;

/// <summary>
/// Gemeinsame Grundlage aller Markierungen auf einem Foto.
/// Die Koordinaten liegen immer im Bild, nicht auf dem Bildschirm - sonst
/// wandert die Markierung beim Speichern.
/// </summary>
public abstract class ZeichenElement
{
    public SKColor Farbe { get; set; }

    /// <summary>
    /// Ist die Markierung groß genug, um sie zu behalten? Ein versehentliches
    /// Antippen soll keinen Punkt hinterlassen.
    /// </summary>
    public abstract bool IstGrossGenug { get; }

    /// <summary>Markierung auf die Zeichenfläche bringen.</summary>
    public abstract void Zeichne(SKCanvas flaeche, SKPaint stift);
}
