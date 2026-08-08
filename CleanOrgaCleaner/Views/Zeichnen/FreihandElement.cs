using SkiaSharp;

namespace CleanOrgaCleaner.Views.Zeichnen;

/// <summary>Freihandlinie aus den Punkten, über die der Finger gezogen wurde.</summary>
public sealed class FreihandElement : ZeichenElement
{
    public List<SKPoint> Punkte { get; } = new();

    public override bool IstGrossGenug => Punkte.Count > 1;

    public override void Zeichne(SKCanvas flaeche, SKPaint stift)
    {
        if (Punkte.Count < 2) return;

        using var pfad = new SKPath();
        pfad.MoveTo(Punkte[0]);
        for (int i = 1; i < Punkte.Count; i++)
            pfad.LineTo(Punkte[i]);

        flaeche.DrawPath(pfad, stift);
    }
}
