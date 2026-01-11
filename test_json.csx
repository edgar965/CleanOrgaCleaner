// C# Script zum Testen der JSON-Deserialisierung
#r "nuget: System.Text.Json, 9.0.0"

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

// Models - exakt wie im Android Client
public class BildStatus
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("notiz")]
    public string Notiz { get; set; } = "";

    [JsonPropertyName("erstellt_am")]
    public string ErstelltAm { get; set; } = "";

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("full_url")]
    public string? FullUrl { get; set; }
}

public class Problem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("beschreibung")]
    public string? Beschreibung { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("erstellt_am")]
    public string? ErstelltAm { get; set; }
}

public class CleaningTask
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("apartment_name")]
    public string ApartmentName { get; set; } = "";

    [JsonPropertyName("bilder")]
    public List<BildStatus>? Bilder { get; set; }

    [JsonPropertyName("probleme")]
    public List<Problem>? Probleme { get; set; }
}

public class TodayDataResponse
{
    [JsonPropertyName("tasks")]
    public List<CleaningTask> Tasks { get; set; } = new();
}

// Test JSON - exakt wie vom Server
var json = @"{""tasks"": [{""id"": 1546, ""apartment_name"": ""02"", ""bilder"": [{""id"": 43, ""notiz"": """", ""erstellt_am"": ""11.01.2026 10:08"", ""thumbnail_url"": ""/media/bildstatus/2026/01/gallery_20260111_110809.jpg"", ""full_url"": ""/media/bildstatus/2026/01/gallery_20260111_110809.jpg""}, {""id"": 42, ""notiz"": """", ""erstellt_am"": ""11.01.2026 09:07"", ""thumbnail_url"": ""/media/bildstatus/2026/01/gallery_20260111_100748.jpg"", ""full_url"": ""/media/bildstatus/2026/01/gallery_20260111_100748.jpg""}], ""probleme"": [{""id"": 1, ""name"": ""Testproblem"", ""beschreibung"": ""Test"", ""status"": ""open"", ""erstellt_am"": ""11.01.2026 10:00""}]}]}";

Console.WriteLine("=== Test 1: OHNE JsonSerializerOptions ===");
var data1 = JsonSerializer.Deserialize<TodayDataResponse>(json);
Console.WriteLine($"Tasks: {data1?.Tasks?.Count ?? 0}");
if (data1?.Tasks?.Count > 0)
{
    var task = data1.Tasks[0];
    Console.WriteLine($"Task {task.Id}: Bilder={task.Bilder?.Count ?? -1} (null={task.Bilder == null}), Probleme={task.Probleme?.Count ?? -1} (null={task.Probleme == null})");
}

Console.WriteLine();
Console.WriteLine("=== Test 2: MIT PropertyNameCaseInsensitive ===");
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
var data2 = JsonSerializer.Deserialize<TodayDataResponse>(json, options);
Console.WriteLine($"Tasks: {data2?.Tasks?.Count ?? 0}");
if (data2?.Tasks?.Count > 0)
{
    var task = data2.Tasks[0];
    Console.WriteLine($"Task {task.Id}: Bilder={task.Bilder?.Count ?? -1} (null={task.Bilder == null}), Probleme={task.Probleme?.Count ?? -1} (null={task.Probleme == null})");

    if (task.Bilder != null)
    {
        foreach (var b in task.Bilder)
        {
            Console.WriteLine($"  Bild {b.Id}: {b.FullUrl}");
        }
    }
    if (task.Probleme != null)
    {
        foreach (var p in task.Probleme)
        {
            Console.WriteLine($"  Problem {p.Id}: {p.Name}");
        }
    }
}

Console.WriteLine();
Console.WriteLine("=== FERTIG ===");
