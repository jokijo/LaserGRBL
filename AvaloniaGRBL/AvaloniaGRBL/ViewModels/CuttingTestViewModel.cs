using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaGRBL.ViewModels;

public partial class CuttingTestViewModel : ViewModelBase
{
    private readonly Action<int, int, int, int, int, int, int, int, string, string> _generateCallback;
    
    [ObservableProperty]
    private int _passMin = 1;
    
    [ObservableProperty]
    private int _passMax = 4;
    
    [ObservableProperty]
    private int _powerFixed = 1000;
    
    [ObservableProperty]
    private int _speedMin = 200;
    
    [ObservableProperty]
    private int _speedMax = 1000;
    
    [ObservableProperty]
    private int _speedSteps = 5;
    
    [ObservableProperty]
    private int _speedStepSize = 200;
    
    [ObservableProperty]
    private string _laserMode = "M3";
    
    [ObservableProperty]
    private int _textSpeed = 1000;
    
    [ObservableProperty]
    private int _textPower = 500;
    
    [ObservableProperty]
    private string _customLabel = "";
    
    [ObservableProperty]
    private double _width = 66.0;
    
    [ObservableProperty]
    private double _height = 59.0;
    
    [ObservableProperty]
    private bool _speedMinEnabled = true;

    public CuttingTestViewModel(Action<int, int, int, int, int, int, int, int, string, string> generateCallback)
    {
        _generateCallback = generateCallback;
        UpdateStepSize();
        ComputeSize();
    }

    partial void OnSpeedStepsChanged(int value)
    {
        UpdateStepSize();
        ComputeSize();
    }

    partial void OnSpeedMinChanged(int value)
    {
        UpdateStepSize();
    }

    partial void OnSpeedMaxChanged(int value)
    {
        UpdateStepSize();
    }

    partial void OnPassMinChanged(int value)
    {
        if (PassMin > PassMax)
            PassMax = PassMin;
        ComputeSize();
    }

    partial void OnPassMaxChanged(int value)
    {
        if (PassMax < PassMin)
            PassMin = PassMax;
        ComputeSize();
    }

    private void UpdateStepSize()
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

    private void ComputeSize()
    {
        Width = SpeedSteps * 14 - 4 + 3;
        Height = (PassMax - PassMin + 1) * 14 - 4 + 3 + 3 + 3;
    }

    [RelayCommand]
    private void Create()
    {
        _generateCallback?.Invoke(
            SpeedSteps,     // f_col
            SpeedMin,       // f_start
            SpeedMax,       // f_end
            PassMin,        // p_start
            PassMax,        // p_end
            PowerFixed,     // s_fixed
            TextSpeed,      // f_text
            TextPower,      // s_text
            CustomLabel,    // title
            LaserMode       // ton (M3 or M4)
        );
    }
}
