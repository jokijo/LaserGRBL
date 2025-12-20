using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaGRBL.Services;
using AvaloniaGRBL.Models;

namespace AvaloniaGRBL.ViewModels;

public partial class ShakeTestViewModel : ViewModelBase
{
    public event EventHandler? CloseRequested;
    
    private readonly Action<string, int, int, int, int>? _generateTestCallback;
    
    [ObservableProperty]
    private ObservableCollection<string> _axisOptions = new() { "X", "Y" };
    
    [ObservableProperty]
    private string _selectedAxis = "X";
    
    [ObservableProperty]
    private int _axisLength = 450;
    
    [ObservableProperty]
    private int _maxAxisLength = 450;
    
    [ObservableProperty]
    private int _crossSpeed = 1000;
    
    [ObservableProperty]
    private int _maxCrossSpeed = 10000;
    
    [ObservableProperty]
    private int _crossPower = 100;
    
    [ObservableProperty]
    private int _maxPower = 1000;
    
    [ObservableProperty]
    private ObservableCollection<int> _limitSpeedOptions = new();
    
    [ObservableProperty]
    private int _selectedLimitSpeed = 10200;
    
    public ShakeTestViewModel()
    {
        InitializeLimitSpeedOptions();
    }
    
    public ShakeTestViewModel(Action<string, int, int, int, int> generateTestCallback) : this()
    {
        _generateTestCallback = generateTestCallback;
        InitializeFromConfiguration();
    }
    
    private void InitializeFromConfiguration()
    {
        // Try to get configuration values
        // For now, we'll use defaults. In a full implementation,
        // this would read from GrblCore.Configuration
        
        int maxRateX = 20000;
        int maxRateY = 20000;
        int maxPwm = 1000;
        
        // Set max values
        MaxCrossSpeed = Math.Min(maxRateX, maxRateY);
        MaxPower = maxPwm;
        
        // Update axis-specific values based on selected axis
        UpdateAxisLimits();
    }
    
    partial void OnSelectedAxisChanged(string value)
    {
        UpdateAxisLimits();
    }
    
    private void UpdateAxisLimits()
    {
        int maxRateA = 20000;
        int maxLenA = 400;
        
        // In a full implementation, read from configuration:
        // maxRateA = SelectedAxis == "X" ? (int)GrblCore.Configuration.MaxRateX : (int)GrblCore.Configuration.MaxRateY;
        // maxLenA = SelectedAxis == "X" ? (int)GrblCore.Configuration.TableWidth : (int)GrblCore.Configuration.TableHeight;
        
        MaxAxisLength = maxLenA;
        AxisLength = maxLenA;
        
        // Update limit speed options
        LimitSpeedOptions.Clear();
        LimitSpeedOptions.Add(maxRateA);
        
        for (int i = maxRateA / 1000 * 1000; i > 0; i -= 1000)
        {
            if (!LimitSpeedOptions.Contains(i))
                LimitSpeedOptions.Add(i);
        }
        
        // Select first option if current selection is not in list
        if (!LimitSpeedOptions.Contains(SelectedLimitSpeed) && LimitSpeedOptions.Count > 0)
        {
            SelectedLimitSpeed = LimitSpeedOptions[0];
        }
    }
    
    private void InitializeLimitSpeedOptions()
    {
        LimitSpeedOptions.Clear();
        LimitSpeedOptions.Add(10200);
        LimitSpeedOptions.Add(10000);
        for (int i = 9000; i > 0; i -= 1000)
        {
            LimitSpeedOptions.Add(i);
        }
        SelectedLimitSpeed = 10200;
    }
    
    [RelayCommand]
    private void Create()
    {
        // Call the callback to generate the test
        _generateTestCallback?.Invoke(SelectedAxis, SelectedLimitSpeed, AxisLength, CrossPower, CrossSpeed);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
    
    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
