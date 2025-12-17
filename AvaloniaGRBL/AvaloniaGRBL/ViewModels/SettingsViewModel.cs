using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaGRBL.Services;

namespace AvaloniaGRBL.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    // Hardware Tab
    [ObservableProperty]
    private string _selectedFirmware = "Grbl";
    
    public ObservableCollection<string> FirmwareTypes { get; } = new()
    {
        "Grbl", "Smoothie", "Marlin", "VigoWork"
    };
    
    [ObservableProperty]
    private bool _supportHardwarePWM = true;
    
    [ObservableProperty]
    private string _selectedProtocol = "UsbSerial";
    
    public ObservableCollection<string> ProtocolTypes { get; } = new()
    {
        "UsbSerial", "UsbSerial2", "RJCPSerial", "Telnet", "LaserWebESP8266", "Emulator"
    };
    
    [ObservableProperty]
    private string _selectedStreamingMode = "Buffered";
    
    public ObservableCollection<string> StreamingModes { get; } = new()
    {
        "Buffered", "Synchronous", "RepeatOnError"
    };
    
    [ObservableProperty]
    private string _selectedThreadingMode = "UltraFast";
    
    public ObservableCollection<string> ThreadingModes { get; } = new()
    {
        "Insane", "UltraFast", "Fast", "Quiet", "Slow"
    };
    
    [ObservableProperty]
    private bool _showIssueDetector = true;
    
    [ObservableProperty]
    private bool _softResetOnConnect = true;
    
    [ObservableProperty]
    private bool _hardResetOnConnect = false;
    
    [ObservableProperty]
    private bool _queryMachineInfoOnConnect = true;
    
    // Raster Import Tab
    [ObservableProperty]
    private bool _unidirectionalEngraving = false;
    
    [ObservableProperty]
    private bool _disableG0FastSkip = false;
    
    [ObservableProperty]
    private bool _rasterHiRes = false;
    
    [ObservableProperty]
    private bool _disableBoundaryWarning = false;
    
    // Vector Import Tab
    [ObservableProperty]
    private bool _useSmartBezier = true;
    
    // Jog Control Tab
    [ObservableProperty]
    private bool _enableContinuousJog = false;
    
    [ObservableProperty]
    private bool _enableZJogControl = false;
    
    [ObservableProperty]
    private bool _clickAndJog = true;
    
    // Auto Cooling Tab
    [ObservableProperty]
    private bool _autoCoolingEnabled = false;
    
    [ObservableProperty]
    private int _coolingOnMinutes = 10;
    
    [ObservableProperty]
    private int _coolingOnSeconds = 0;
    
    [ObservableProperty]
    private int _coolingOffMinutes = 1;
    
    [ObservableProperty]
    private int _coolingOffSeconds = 0;
    
    public ObservableCollection<int> MinuteOptions { get; } = new();
    public ObservableCollection<int> SecondOptions { get; } = new();
    
    // GCode Settings Tab
    [ObservableProperty]
    private string _customHeader = "G90\n";
    
    [ObservableProperty]
    private string _customPasses = "";
    
    [ObservableProperty]
    private string _customFooter = "M5\nG0 X0 Y0\n";
    
    // Sound Settings Tab
    [ObservableProperty]
    private bool _playSuccessSound = true;
    
    [ObservableProperty]
    private bool _playWarningSound = true;
    
    [ObservableProperty]
    private bool _playFatalSound = true;
    
    [ObservableProperty]
    private bool _playConnectSound = true;
    
    [ObservableProperty]
    private bool _playDisconnectSound = true;
    
    [ObservableProperty]
    private string _successSoundPath = "";
    
    [ObservableProperty]
    private string _warningSoundPath = "";
    
    [ObservableProperty]
    private string _fatalSoundPath = "";
    
    [ObservableProperty]
    private string _connectSoundPath = "";
    
    [ObservableProperty]
    private string _disconnectSoundPath = "";
    
    // Telegram Notifications
    [ObservableProperty]
    private bool _telegramNotificationEnabled = false;
    
    [ObservableProperty]
    private string _telegramCode = "";
    
    [ObservableProperty]
    private int _telegramThreshold = 1;
    
    // Options Tab
    [ObservableProperty]
    private string _selectedGraphicMode = "Auto";
    
    public ObservableCollection<string> GraphicModes { get; } = new()
    {
        "Auto", "Hardware Acceleration", "Software Rendering", "Legacy"
    };
    
    [ObservableProperty]
    private bool _disableSafetyCountdown = false;
    
    [ObservableProperty]
    private bool _quietSafetyCountdown = false;
    
    [ObservableProperty]
    private bool _legacyIcons = false;
    
    public SettingsViewModel()
    {
        LoadSettings();
        InitializeDropdowns();
    }
    
    private void InitializeDropdowns()
    {
        // Initialize minutes (0-60)
        for (int i = 0; i <= 60; i++)
        {
            MinuteOptions.Add(i);
        }
        
        // Initialize seconds (0-59)
        for (int i = 0; i <= 59; i++)
        {
            SecondOptions.Add(i);
        }
    }
    
    private void LoadSettings()
    {
        // Hardware Tab
        SelectedFirmware = SettingsService.GetObject("Firmware Type", "Grbl");
        SupportHardwarePWM = SettingsService.GetObject("Support Hardware PWM", true);
        SelectedProtocol = SettingsService.GetObject("ComWrapper Protocol", "UsbSerial");
        SelectedStreamingMode = SettingsService.GetObject("Streaming Mode", "Buffered");
        SelectedThreadingMode = SettingsService.GetObject("Threading Mode", "UltraFast");
        ShowIssueDetector = !SettingsService.GetObject("Do not show Issue Detector", false);
        SoftResetOnConnect = SettingsService.GetObject("Reset Grbl On Connect", true);
        HardResetOnConnect = SettingsService.GetObject("HardReset Grbl On Connect", false);
        QueryMachineInfoOnConnect = SettingsService.GetObject("Query MachineInfo ($I) at connect", true);
        
        // Raster Import Tab
        UnidirectionalEngraving = SettingsService.GetObject("Unidirectional Engraving", false);
        DisableG0FastSkip = SettingsService.GetObject("Disable G0 fast skip", false);
        RasterHiRes = SettingsService.GetObject("Raster Hi-Res", false);
        DisableBoundaryWarning = SettingsService.GetObject("DisableBoundaryWarning", false);
        
        // Vector Import Tab
        UseSmartBezier = SettingsService.GetObject("Vector.UseSmartBezier", true);
        
        // Jog Control Tab
        EnableContinuousJog = SettingsService.GetObject("Enable Continuous Jog", false);
        EnableZJogControl = SettingsService.GetObject("Enale Z Jog Control", false);
        ClickAndJog = SettingsService.GetObject("Click N Jog", true);
        
        // Auto Cooling Tab
        AutoCoolingEnabled = SettingsService.GetObject("AutoCooling", false);
        var coolingOn = SettingsService.GetObject("AutoCooling TOn", TimeSpan.FromMinutes(10));
        var coolingOff = SettingsService.GetObject("AutoCooling TOff", TimeSpan.FromMinutes(1));
        CoolingOnMinutes = coolingOn.Minutes;
        CoolingOnSeconds = coolingOn.Seconds;
        CoolingOffMinutes = coolingOff.Minutes;
        CoolingOffSeconds = coolingOff.Seconds;
        
        // GCode Settings Tab
        CustomHeader = SettingsService.GetObject("GCode.CustomHeader", "G90\n");
        CustomPasses = SettingsService.GetObject("GCode.CustomPasses", "");
        CustomFooter = SettingsService.GetObject("GCode.CustomFooter", "M5\nG0 X0 Y0\n");
        
        // Sound Settings Tab
        PlaySuccessSound = SettingsService.GetObject("Sound.Success.Enabled", true);
        PlayWarningSound = SettingsService.GetObject("Sound.Warning.Enabled", true);
        PlayFatalSound = SettingsService.GetObject("Sound.Fatal.Enabled", true);
        PlayConnectSound = SettingsService.GetObject("Sound.Connect.Enabled", true);
        PlayDisconnectSound = SettingsService.GetObject("Sound.Disconnect.Enabled", true);
        
        SuccessSoundPath = SettingsService.GetObject("Sound.Success", "");
        WarningSoundPath = SettingsService.GetObject("Sound.Warning", "");
        FatalSoundPath = SettingsService.GetObject("Sound.Fatal", "");
        ConnectSoundPath = SettingsService.GetObject("Sound.Connect", "");
        DisconnectSoundPath = SettingsService.GetObject("Sound.Disconnect", "");
        
        // Telegram Notifications
        TelegramNotificationEnabled = SettingsService.GetObject("TelegramNotification.Enabled", false);
        TelegramCode = SettingsService.GetObject("TelegramNotification.Code", "");
        TelegramThreshold = SettingsService.GetObject("TelegramNotification.Threshold", 1);
        
        // Options Tab
        SelectedGraphicMode = SettingsService.GetObject("ConfiguredGraphicMode", "Auto");
        DisableSafetyCountdown = SettingsService.GetObject("DisableSafetyCountdown", false);
        QuietSafetyCountdown = SettingsService.GetObject("QuietSafetyCountdown", false);
        LegacyIcons = SettingsService.GetObject("LegacyIcons", false);
    }
    
    [RelayCommand]
    private void Save()
    {
        // Hardware Tab
        SettingsService.SetObject("Firmware Type", SelectedFirmware);
        SettingsService.SetObject("Support Hardware PWM", SupportHardwarePWM);
        SettingsService.SetObject("ComWrapper Protocol", SelectedProtocol);
        SettingsService.SetObject("Streaming Mode", SelectedStreamingMode);
        SettingsService.SetObject("Threading Mode", SelectedThreadingMode);
        SettingsService.SetObject("Do not show Issue Detector", !ShowIssueDetector);
        SettingsService.SetObject("Reset Grbl On Connect", SoftResetOnConnect);
        SettingsService.SetObject("HardReset Grbl On Connect", HardResetOnConnect);
        SettingsService.SetObject("Query MachineInfo ($I) at connect", QueryMachineInfoOnConnect);
        
        // Raster Import Tab
        SettingsService.SetObject("Unidirectional Engraving", UnidirectionalEngraving);
        SettingsService.SetObject("Disable G0 fast skip", DisableG0FastSkip);
        SettingsService.SetObject("Raster Hi-Res", RasterHiRes);
        SettingsService.SetObject("DisableBoundaryWarning", DisableBoundaryWarning);
        
        // Vector Import Tab
        SettingsService.SetObject("Vector.UseSmartBezier", UseSmartBezier);
        
        // Jog Control Tab
        SettingsService.SetObject("Enable Continuous Jog", EnableContinuousJog);
        SettingsService.SetObject("Enale Z Jog Control", EnableZJogControl);
        SettingsService.SetObject("Click N Jog", ClickAndJog);
        
        // Auto Cooling Tab
        SettingsService.SetObject("AutoCooling", AutoCoolingEnabled);
        var onTime = new TimeSpan(0, CoolingOnMinutes, CoolingOnSeconds);
        var offTime = new TimeSpan(0, CoolingOffMinutes, CoolingOffSeconds);
        SettingsService.SetObject("AutoCooling TOn", onTime.TotalSeconds >= 10 ? onTime : TimeSpan.FromSeconds(10));
        SettingsService.SetObject("AutoCooling TOff", offTime.TotalSeconds >= 10 ? offTime : TimeSpan.FromSeconds(10));
        
        // GCode Settings Tab
        SettingsService.SetObject("GCode.CustomHeader", CustomHeader.Trim());
        SettingsService.SetObject("GCode.CustomPasses", CustomPasses.Trim());
        SettingsService.SetObject("GCode.CustomFooter", CustomFooter.Trim());
        
        // Sound Settings Tab
        SettingsService.SetObject("Sound.Success.Enabled", PlaySuccessSound);
        SettingsService.SetObject("Sound.Warning.Enabled", PlayWarningSound);
        SettingsService.SetObject("Sound.Fatal.Enabled", PlayFatalSound);
        SettingsService.SetObject("Sound.Connect.Enabled", PlayConnectSound);
        SettingsService.SetObject("Sound.Disconnect.Enabled", PlayDisconnectSound);
        
        SettingsService.SetObject("Sound.Success", SuccessSoundPath.Trim());
        SettingsService.SetObject("Sound.Warning", WarningSoundPath.Trim());
        SettingsService.SetObject("Sound.Fatal", FatalSoundPath.Trim());
        SettingsService.SetObject("Sound.Connect", ConnectSoundPath.Trim());
        SettingsService.SetObject("Sound.Disconnect", DisconnectSoundPath.Trim());
        
        // Telegram Notifications
        SettingsService.SetObject("TelegramNotification.Enabled", TelegramNotificationEnabled);
        SettingsService.SetObject("TelegramNotification.Code", TelegramCode);
        SettingsService.SetObject("TelegramNotification.Threshold", TelegramThreshold);
        
        // Options Tab
        SettingsService.SetObject("ConfiguredGraphicMode", SelectedGraphicMode);
        SettingsService.SetObject("DisableSafetyCountdown", DisableSafetyCountdown);
        SettingsService.SetObject("QuietSafetyCountdown", QuietSafetyCountdown);
        SettingsService.SetObject("LegacyIcons", LegacyIcons);
        
        // Notify settings changed
        SettingsService.NotifySettingsChanged();
        
        // Close the window
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
    
    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
    
    public event EventHandler? CloseRequested;
}
