using System.Globalization;

namespace CleanOrgaCleaner.Converters;

/// <summary>
/// Binding-Konverter: Text vorhanden -> true, leer/nur Leerzeichen -> false.
/// Wird in App.xaml als Ressource bereitgestellt, um Beschriftungen nur dann
/// einzublenden, wenn sie Inhalt haben.
///
/// Eine Richtung: <see cref="ConvertBack"/> ergibt keinen Sinn, weil aus "true"
/// kein Ausgangstext rekonstruierbar ist.
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrWhiteSpace(value as string ?? value?.ToString());
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException(
            $"{nameof(StringToBoolConverter)} ist ein Ein-Weg-Konverter (nur Anzeige).");
    }
}
