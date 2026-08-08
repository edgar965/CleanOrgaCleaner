using CleanOrgaCleaner.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Eine Arbeitskraft in der Zuweisungsliste des Auftrags-Dialogs: Anzeigename,
/// Symbol und ob sie der Aufgabe zugewiesen ist.
///
/// Lag vorher am Ende von AuftragPage.xaml.cs - eine Klasse je Datei.
/// </summary>
public class CleanerAssignmentInfo : INotifyPropertyChanged
{
    private static readonly Color Zugewiesen = Color.FromArgb("#2196F3");

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Avatar { get; set; }

    /// <summary>Anfangsbuchstabe als Ersatz, wenn kein Symbol hinterlegt ist.</summary>
    public string Initial => Name.Length > 0 ? Name.Substring(0, 1).ToUpper() : "?";

    public bool HasAvatar => !string.IsNullOrEmpty(Avatar);
    public bool HasNoAvatar => !HasAvatar;

    private bool _isAssigned;
    public bool IsAssigned
    {
        get => _isAssigned;
        set
        {
            if (_isAssigned == value) return;
            _isAssigned = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AssignBgColor));
            OnPropertyChanged(nameof(AssignTextColor));
        }
    }

    public Color AssignBgColor => IsAssigned ? Zugewiesen : Colors.White;
    public Color AssignTextColor => IsAssigned ? Colors.White : Zugewiesen;

    public CleanerAssignmentInfo(CleanerInfo cleaner)
    {
        Id = cleaner.Id;
        Name = cleaner.Name;
        Avatar = cleaner.Avatar;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
