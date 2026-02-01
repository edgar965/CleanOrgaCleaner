using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

// Models - new unified ImageListDescription model
public class ImageListDescriptionPhoto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }
}

public class ImageListDescription
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("photos")]
    public List<ImageListDescriptionPhoto>? Photos { get; set; }

    [JsonPropertyName("erstellt_am")]
    public string? ErstelltAm { get; set; }

    [JsonPropertyName("erledigt")]
    public bool Erledigt { get; set; }
}

public class CleaningTask
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("apartment_name")]
    public string ApartmentName { get; set; } = "";

    [JsonPropertyName("problems")]
    public List<ImageListDescription>? Problems { get; set; }

    [JsonPropertyName("anmerkungen")]
    public List<ImageListDescription>? Anmerkungen { get; set; }
}

public class TodayDataResponse
{
    [JsonPropertyName("tasks")]
    public List<CleaningTask> Tasks { get; set; } = new();
}

class Program
{
    static void Main()
    {
        // Test JSON - new unified format from server
        var json = @"{""tasks"": [{""id"": 1546, ""apartment_name"": ""02"", ""problems"": [{""id"": 1, ""type"": ""problem"", ""name"": ""Testproblem"", ""description"": ""Test"", ""erledigt"": false, ""erstellt_am"": ""11.01.2026 10:00"", ""photos"": [{""id"": 1, ""url"": ""/media/image_list/test.jpg"", ""thumbnail_url"": ""/media/image_list/test_thumb.jpg""}]}], ""anmerkungen"": [{""id"": 2, ""type"": ""anmerkung"", ""name"": ""Notiz"", ""description"": """", ""erledigt"": false, ""erstellt_am"": ""11.01.2026 10:08"", ""photos"": []}]}]}";

        Console.WriteLine("=== Test: ImageListDescription JSON Parsing ===");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var data = JsonSerializer.Deserialize<TodayDataResponse>(json, options);
        Console.WriteLine($"Tasks: {data?.Tasks?.Count ?? 0}");
        if (data?.Tasks?.Count > 0)
        {
            var task = data.Tasks[0];
            Console.WriteLine($"Task {task.Id}: Problems={task.Problems?.Count ?? 0}, Anmerkungen={task.Anmerkungen?.Count ?? 0}");

            if (task.Problems != null)
            {
                foreach (var p in task.Problems)
                {
                    Console.WriteLine($"  Problem {p.Id}: {p.Name} (photos: {p.Photos?.Count ?? 0})");
                }
            }
            if (task.Anmerkungen != null)
            {
                foreach (var a in task.Anmerkungen)
                {
                    Console.WriteLine($"  Anmerkung {a.Id}: {a.Name} (photos: {a.Photos?.Count ?? 0})");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("=== FERTIG ===");
    }
}
