using SkiaSharp;

namespace CleanOrgaCleaner.Views.Zeichnen;

/// <summary>Pfeil von einem Punkt auf eine Stelle im Bild.</summary>
public sealed class PfeilElement : ZeichenElement
{
    /// <summary>Öffnungswinkel der Spitze (rund 30 Grad).</summary>
    private const float SpitzenWinkel = 0.5f;

    /// <summary>Länge der Spitze im Verhältnis zur Strichstärke.</summary>
    private const float SpitzenLaenge = 4f;

    public SKPoint Start { get; set; }
    public SKPoint Ende { get; set; }

    public override bool IstGrossGenug => SKPoint.Distance(Start, Ende) > 10;

    public override void Zeichne(SKCanvas flaeche, SKPaint stift)
    {
        flaeche.DrawLine(Start, Ende, stift);

        // Spitze mit der Strichstärke mitwachsen lassen, damit sie bei dicken
        // Linien nicht verschwindet
        float winkel = (float)Math.Atan2(Ende.Y - Start.Y, Ende.X - Start.X);
        float laenge = stift.StrokeWidth * SpitzenLaenge;

        var links = new SKPoint(
            Ende.X - laenge * (float)Math.Cos(winkel - SpitzenWinkel),
            Ende.Y - laenge * (float)Math.Sin(winkel - SpitzenWinkel));
        var rechts = new SKPoint(
            Ende.X - laenge * (float)Math.Cos(winkel + SpitzenWinkel),
            Ende.Y - laenge * (float)Math.Sin(winkel + SpitzenWinkel));

        flaeche.DrawLine(Ende, links, stift);
        flaeche.DrawLine(Ende, rechts, stift);
    }
}
