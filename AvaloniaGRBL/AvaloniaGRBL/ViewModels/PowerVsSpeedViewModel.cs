using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaGRBL.ViewModels;

public partial class PowerVsSpeedViewModel : ViewModelBase
{
    private readonly Action<int, int, int, int, int, int, int, int, double, int, int, string, string> _generateCallback;
    
    [ObservableProperty]
    private int _powerMin = 100;
    
    [ObservableProperty]
    private int _powerMax = 1000;
    
    [ObservableProperty]
    private int _powerSteps = 10;
    
    [ObservableProperty]
    private int _powerStepSize = 100;
    
    [ObservableProperty]
    private int _speedMin = 1000;
    
    [ObservableProperty]
    private int _speedMax = 4000;
    
    [ObservableProperty]
    private int _speedSteps = 7;
    
    [ObservableProperty]
    private int _speedStepSize = 500;
    
    [ObservableProperty]
    private string _laserMode = "M4";
    
    [ObservableProperty]
    private double _quality = 8.0;
    
    [ObservableProperty]
    private int _textSpeed = 1000;
    
    [ObservableProperty]
    private int _textPower = 500;
    
    [ObservableProperty]
    private string _customLabel = "";
    
    [ObservableProperty]
    private double _width = 100.0;
    
    [ObservableProperty]
    private double _height = 70.0;
    
    [ObservableProperty]
    private bool _powerMinEnabled = true;
    
    [ObservableProperty]
    private bool _speedMinEnabled = true;

    public PowerVsSpeedViewModel(Action<int, int, int, int, int, int, int, int, double, int, int, string, string> generateCallback)
    {
        _generateCallback = generateCallback;
        UpdateStepSizes();
    }

    partial void OnPowerStepsChanged(int value)
    {
        UpdatePowerStepSize();
        Width = PowerSteps * 10;
    }

    partial void OnPowerMinChanged(int value)
    {
        UpdatePowerStepSize();
    }

    partial void OnPowerMaxChanged(int value)
    {
        UpdatePowerStepSize();
    }

    partial void OnSpeedStepsChanged(int value)
    {
        UpdateSpeedStepSize();
        Height = SpeedSteps * 10;
    }

    partial void OnSpeedMinChanged(int value)
    {
        UpdateSpeedStepSize();
    }

    partial void OnSpeedMaxChanged(int value)
    {
        UpdateSpeedStepSize();
    }

    private void UpdatePowerStepSize()
    {
        if (PowerSteps == 1)
        {
            PowerMinEnabled = false;
            PowerMin = PowerMax;
            PowerStepSize = 0;
        }
        else
        {
            if (!PowerMinEnabled)
            {
                PowerMin = 0;
                PowerMinEnabled = true;
            }
            PowerStepSize = (int)((PowerMax - PowerMin) / (double)(PowerSteps - 1));
        }
    }

    private void UpdateSpeedStepSize()
    {
        if (SpeedSteps == 1)
        {
            SpeedMinEnabled = false;
            SpeedMin = SpeedMax;
            SpeedStepSize = 0;
        }
        else
        {
            if (!SpeedMinEnabled)
            {
                SpeedMin = 0;
                SpeedMinEnabled = true;
            }
            SpeedStepSize = (int)((SpeedMax - SpeedMin) / (double)(SpeedSteps - 1));
        }
    }

    private void UpdateStepSizes()
    {
        UpdatePowerStepSize();
        UpdateSpeedStepSize();
    }

    [RelayCommand]
    private void Create()
    {
        _generateCallback?.Invoke(
            SpeedSteps,     // f_row
            PowerSteps,     // s_col
            SpeedMin,       // f_start
            SpeedMax,       // f_end
            PowerMin,       // s_start
            PowerMax,       // s_end
            (int)Width,     // x_size
            (int)Height,    // y_size
            Quality,        // resolution
            TextSpeed,      // f_text
            TextPower,      // s_text
            CustomLabel,    // title
            LaserMode       // ton (M3 or M4)
        );
    }
}
