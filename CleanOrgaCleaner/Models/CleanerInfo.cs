namespace CleanOrgaCleaner.Models;

/// <summary>
/// Information about a cleaner for chat list
/// </summary>
public class CleanerInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper();
    public int UnreadCount { get; set; }
    public bool IsWorking { get; set; }
}

/// <summary>
/// Response from cleaners list API
/// </summary>
public class CleanersListResponse
{
    public bool Success { get; set; }
    public List<CleanerInfo> Cleaners { get; set; } = new();
}
