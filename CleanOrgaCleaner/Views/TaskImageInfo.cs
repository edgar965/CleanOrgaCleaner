namespace CleanOrgaCleaner.Views;

/// <summary>
/// Ein Foto einer Aufgabe, wie es der Server liefert.
///
/// Lag vorher am Ende von AuftragPage.xaml.cs - eine Klasse je Datei.
/// </summary>
public class TaskImageInfo
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string? ThumbnailUrl { get; set; }
    public string? Note { get; set; }
    public string? CreatedAt { get; set; }
}
