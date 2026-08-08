using SkiaSharp;

namespace CleanOrgaCleaner.Views.Zeichnen;

/// <summary>Kreis um eine Stelle im Bild.</summary>
public sealed class KreisElement : ZeichenElement
{
    public SKPoint Mitte { get; set; }
    public float Radius { get; set; }

    public override bool IstGrossGenug => Radius > 5;

    public override void Zeichne(SKCanvas flaeche, SKPaint stift)
    {
        if (Radius <= 0) return;
        flaeche.DrawCircle(Mitte, Radius, stift);
    }
}
