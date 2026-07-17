using System.Globalization;

namespace Pokekool.Mobile.Converters;

/// <summary>true si la valeur est non nulle / non vide (string).</summary>
public sealed class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s ? !string.IsNullOrEmpty(s) : value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>MultiBinding : retourne la première valeur non vide (ex. CardName sinon ProductName).</summary>
public sealed class FirstNotEmptyConverter : IMultiValueConverter
{
    public object? Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
        => values?.FirstOrDefault(v => v is string s && !string.IsNullOrWhiteSpace(s)) ?? "";

    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Inverse un booléen.</summary>
public sealed class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}
