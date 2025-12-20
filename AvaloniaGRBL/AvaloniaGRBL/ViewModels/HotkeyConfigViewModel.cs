using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaGRBL.ViewModels;

public partial class HotkeyConfigViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<HotkeyItem> _hotkeys = new ObservableCollection<HotkeyItem>();
    
    [ObservableProperty]
    private HotkeyItem? _selectedHotkey;
    
    [ObservableProperty]
    private string _statusMessage = "Select a hotkey and press the desired key combination";
    
    public HotkeyConfigViewModel()
    {
        InitializeDefaultHotkeys();
    }
    
    private void InitializeDefaultHotkeys()
    {
        // Connection
        Hotkeys.Add(new HotkeyItem("Connect/Disconnect", "F12", "Toggle connection to GRBL"));
        
        // File operations
        Hotkeys.Add(new HotkeyItem("Open File", "Ctrl+O", "Open a G-Code file"));
        Hotkeys.Add(new HotkeyItem("Reopen Last File", "Ctrl+R", "Reopen the last file"));
        Hotkeys.Add(new HotkeyItem("Save Program", "Ctrl+S", "Save the current program"));
        Hotkeys.Add(new HotkeyItem("Execute File", "F5", "Start executing the file"));
        Hotkeys.Add(new HotkeyItem("Abort File", "Ctrl+F5", "Abort file execution"));
        
        // GRBL Commands
        Hotkeys.Add(new HotkeyItem("Reset GRBL", "Ctrl+X", "Reset the GRBL controller"));
        Hotkeys.Add(new HotkeyItem("Homing", "Ctrl+H", "Start homing cycle"));
        Hotkeys.Add(new HotkeyItem("Unlock", "Ctrl+U", "Unlock GRBL after alarm"));
        Hotkeys.Add(new HotkeyItem("Pause Job", "F6", "Pause the current job"));
        Hotkeys.Add(new HotkeyItem("Resume Job", "F7", "Resume the paused job"));
        Hotkeys.Add(new HotkeyItem("Set Zero", "Ctrl+Z", "Set current position as zero"));
        
        // Preview
        Hotkeys.Add(new HotkeyItem("Auto Size", "Ctrl+A", "Auto-size the preview"));
        Hotkeys.Add(new HotkeyItem("Zoom In", "Ctrl++", "Zoom in the preview"));
        Hotkeys.Add(new HotkeyItem("Zoom Out", "Ctrl+-", "Zoom out the preview"));
        
        // Jogging
        Hotkeys.Add(new HotkeyItem("Jog North", "NumPad8", "Jog machine north (Y+)"));
        Hotkeys.Add(new HotkeyItem("Jog South", "NumPad2", "Jog machine south (Y-)"));
        Hotkeys.Add(new HotkeyItem("Jog East", "NumPad6", "Jog machine east (X+)"));
        Hotkeys.Add(new HotkeyItem("Jog West", "NumPad4", "Jog machine west (X-)"));
        Hotkeys.Add(new HotkeyItem("Jog North-East", "NumPad9", "Jog machine north-east"));
        Hotkeys.Add(new HotkeyItem("Jog North-West", "NumPad7", "Jog machine north-west"));
        Hotkeys.Add(new HotkeyItem("Jog South-East", "NumPad3", "Jog machine south-east"));
        Hotkeys.Add(new HotkeyItem("Jog South-West", "NumPad1", "Jog machine south-west"));
        Hotkeys.Add(new HotkeyItem("Jog Home", "NumPad5", "Jog to home position"));
        
        // Help
        Hotkeys.Add(new HotkeyItem("Help Online", "Ctrl+F1", "Open online help"));
    }
    
    [RelayCommand]
    private void ClearHotkey()
    {
        if (SelectedHotkey != null)
        {
            SelectedHotkey.KeyCombination = "None";
            StatusMessage = $"Cleared hotkey for '{SelectedHotkey.Action}'";
        }
    }
    
    [RelayCommand]
    private void ResetToDefaults()
    {
        Hotkeys.Clear();
        InitializeDefaultHotkeys();
        StatusMessage = "All hotkeys reset to default values";
    }
    
    public void UpdateHotkey(HotkeyItem hotkey, string keyCombination)
    {
        if (hotkey != null)
        {
            // Check for duplicates
            var duplicate = Hotkeys.FirstOrDefault(h => 
                h != hotkey && 
                h.KeyCombination == keyCombination && 
                keyCombination != "None");
            
            if (duplicate != null)
            {
                StatusMessage = $"⚠ Warning: '{keyCombination}' is already assigned to '{duplicate.Action}'";
            }
            else
            {
                StatusMessage = $"Assigned '{keyCombination}' to '{hotkey.Action}'";
            }
            
            hotkey.KeyCombination = keyCombination;
        }
    }
}

public partial class HotkeyItem : ObservableObject
{
    [ObservableProperty]
    private string _action;
    
    [ObservableProperty]
    private string _keyCombination;
    
    [ObservableProperty]
    private string _description;
    
    public HotkeyItem(string action, string keyCombination, string description)
    {
        _action = action;
        _keyCombination = keyCombination;
        _description = description;
    }
}
