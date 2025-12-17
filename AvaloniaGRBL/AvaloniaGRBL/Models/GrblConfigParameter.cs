using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaGRBL.Models;

/// <summary>
/// Represents a single GRBL configuration parameter
/// </summary>
public partial class GrblConfigParameter : ObservableObject
{
    [ObservableProperty]
    private int _number;
    
    [ObservableProperty]
    private string _value = string.Empty;
    
    public string DollarNumber => $"${Number}";
    
    public string Parameter => GetParameterName(Number);
    
    public string Unit => GetParameterUnit(Number);
    
    public string Description => GetParameterDescription(Number);
    
    public GrblConfigParameter()
    {
    }
    
    public GrblConfigParameter(int number, string value)
    {
        Number = number;
        Value = value;
    }
    
    partial void OnNumberChanged(int value)
    {
        OnPropertyChanged(nameof(DollarNumber));
        OnPropertyChanged(nameof(Parameter));
        OnPropertyChanged(nameof(Unit));
        OnPropertyChanged(nameof(Description));
    }
    
    private static string GetParameterName(int number)
    {
        return number switch
        {
            0 => "Step pulse time",
            1 => "Step idle delay",
            2 => "Step pulse invert",
            3 => "Step direction invert",
            4 => "Invert step enable pin",
            5 => "Invert limit pins",
            6 => "Invert probe pin",
            10 => "Status report options",
            11 => "Junction deviation",
            12 => "Arc tolerance",
            13 => "Report in inches",
            20 => "Soft limits enable",
            21 => "Hard limits enable",
            22 => "Homing cycle enable",
            23 => "Homing direction invert",
            24 => "Homing locate feed rate",
            25 => "Homing search seek rate",
            26 => "Homing switch debounce delay",
            27 => "Homing switch pull-off distance",
            30 => "Maximum spindle speed",
            31 => "Minimum spindle speed",
            32 => "Laser-mode enable",
            100 => "X-axis steps per mm",
            101 => "Y-axis steps per mm",
            102 => "Z-axis steps per mm",
            110 => "X-axis maximum rate",
            111 => "Y-axis maximum rate",
            112 => "Z-axis maximum rate",
            120 => "X-axis acceleration",
            121 => "Y-axis acceleration",
            122 => "Z-axis acceleration",
            130 => "X-axis maximum travel",
            131 => "Y-axis maximum travel",
            132 => "Z-axis maximum travel",
            _ => $"Parameter {number}"
        };
    }
    
    private static string GetParameterUnit(int number)
    {
        return number switch
        {
            0 => "microseconds",
            1 => "milliseconds",
            2 => "mask",
            3 => "mask",
            4 => "boolean",
            5 => "boolean",
            6 => "boolean",
            10 => "mask",
            11 => "mm",
            12 => "mm",
            13 => "boolean",
            20 => "boolean",
            21 => "boolean",
            22 => "boolean",
            23 => "mask",
            24 => "mm/min",
            25 => "mm/min",
            26 => "milliseconds",
            27 => "mm",
            30 => "RPM",
            31 => "RPM",
            32 => "boolean",
            100 => "steps/mm",
            101 => "steps/mm",
            102 => "steps/mm",
            110 => "mm/min",
            111 => "mm/min",
            112 => "mm/min",
            120 => "mm/sec²",
            121 => "mm/sec²",
            122 => "mm/sec²",
            130 => "mm",
            131 => "mm",
            132 => "mm",
            _ => ""
        };
    }
    
    private static string GetParameterDescription(int number)
    {
        return number switch
        {
            0 => "Sets the duration of each step pulse in microseconds",
            1 => "Sets a short hold delay when stopping to let dynamics settle",
            2 => "Inverts the step pulse signal",
            3 => "Inverts the direction signal",
            4 => "Inverts the stepper driver enable pin",
            5 => "Inverts the limit input pins",
            6 => "Inverts the probe input pin",
            10 => "Alters data included in status reports",
            11 => "Sets how fast Grbl travels through consecutive motions",
            12 => "Sets the G2 and G3 arc tracing accuracy",
            13 => "Enables inch units when returning any position and rate value",
            20 => "Enables soft limits checks within machine travel",
            21 => "Enables hard limits",
            22 => "Enables homing cycle",
            23 => "Inverts the homing cycle search direction",
            24 => "Homing locate feed rate to precisely locate the limit switch",
            25 => "Homing search seek rate to quickly locate the limit switches",
            26 => "Homing switch debounce delay",
            27 => "Homing switch pull-off distance",
            30 => "Maximum spindle speed (RPM)",
            31 => "Minimum spindle speed (RPM)",
            32 => "Enables laser mode",
            100 => "X-axis travel resolution in steps per millimeter",
            101 => "Y-axis travel resolution in steps per millimeter",
            102 => "Z-axis travel resolution in steps per millimeter",
            110 => "X-axis maximum rate",
            111 => "Y-axis maximum rate",
            112 => "Z-axis maximum rate",
            120 => "X-axis acceleration",
            121 => "Y-axis acceleration",
            122 => "Z-axis acceleration",
            130 => "Maximum X-axis travel distance from homing position",
            131 => "Maximum Y-axis travel distance from homing position",
            132 => "Maximum Z-axis travel distance from homing position",
            _ => "GRBL configuration parameter"
        };
    }
}
