using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AvaloniaGRBL.Converters;

/// <summary>
/// Converter that maps slider index (0-10) to jog step values with logarithmic spacing
/// </summary>
public class JogStepIndexConverter : IValueConverter
{
    public static readonly JogStepIndexConverter Instance = new();
    
    private static readonly double[] StepValues = { 0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50, 100, 200 };
    
    // Convert from actual step value (JogStep) to slider index (Slider.Value)
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double stepValue)
        {
            // Find the closest matching step value
            int closestIndex = 0;
            double minDifference = Math.Abs(StepValues[0] - stepValue);
            
            for (int i = 1; i < StepValues.Length; i++)
            {
                double difference = Math.Abs(StepValues[i] - stepValue);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    closestIndex = i;
                }
            }
            
            return (double)closestIndex;
        }
        return 3.0; // Default to index 3 (1.0mm)
    }
    
    // Convert from slider index (Slider.Value) to actual step value (JogStep)
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue)
        {
            int index = (int)Math.Round(doubleValue);
            if (index >= 0 && index < StepValues.Length)
            {
                return StepValues[index];
            }
        }
        return 1.0; // Default value (1.0mm)
    }
}
