using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AvaloniaGRBL.Converters;

/// <summary>
/// Converter for boolean to text (Connect/Disconnect)
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public static readonly BoolToTextConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? "Disconnect" : "Connect";
        }
        return "Connect";
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
