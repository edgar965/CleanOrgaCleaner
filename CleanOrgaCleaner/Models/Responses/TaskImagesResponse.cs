using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort mit allen Bildern einer Aufgabe.
/// </summary>
public class TaskImagesResponse : ServerAntwort
{
    [JsonPropertyName("images")]
    public List<TaskImageDto>? Images { get; set; }
}
