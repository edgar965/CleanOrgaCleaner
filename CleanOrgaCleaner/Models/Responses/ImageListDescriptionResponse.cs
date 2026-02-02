using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Response from ImageListDescription create/update API
/// </summary>
public class ImageListDescriptionResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("item_id")]
    public int? ItemId { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("item")]
    public ImageListDescription? Item { get; set; }
}

/// <summary>
/// Response from ImageListDescription delete API
/// </summary>
public class ImageListDescriptionDeleteResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Response from get task items/anmerkung API
/// </summary>
public class ImageListItemsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("items")]
    public List<ImageListDescription>? Items { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
