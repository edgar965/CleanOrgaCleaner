using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort auf das Abhaken eines Checklisten-Eintrags.
/// </summary>
public class ChecklistToggleResponse : ServerAntwort
{
    [JsonPropertyName("checked")]
    public bool Checked { get; set; }

    [JsonPropertyName("item_index")]
    public int ItemIndex { get; set; }
}
