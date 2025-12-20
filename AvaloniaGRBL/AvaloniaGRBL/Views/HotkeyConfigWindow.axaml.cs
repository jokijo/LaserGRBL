using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaGRBL.ViewModels;

namespace AvaloniaGRBL.Views;

public partial class HotkeyConfigWindow : Window
{
    private HotkeyConfigViewModel? ViewModel => DataContext as HotkeyConfigViewModel;
    
    public HotkeyConfigWindow()
    {
        InitializeComponent();
        DataContext = new HotkeyConfigViewModel();
        
        // Subscribe to key events
        this.KeyDown += OnWindowKeyDown;
    }
    
    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel?.SelectedHotkey == null)
            return;
        
        // Ignore certain keys that shouldn't be captured
        if (e.Key == Key.Tab || e.Key == Key.Escape || e.Key == Key.Enter)
            return;
        
        // Delete key clears the hotkey
        if (e.Key == Key.Delete)
        {
            ViewModel.UpdateHotkey(ViewModel.SelectedHotkey, "None");
            e.Handled = true;
            return;
        }
        
        // Build the key combination string
        var keyCombination = BuildKeyCombinationString(e);
        
        if (!string.IsNullOrEmpty(keyCombination))
        {
            ViewModel.UpdateHotkey(ViewModel.SelectedHotkey, keyCombination);
            e.Handled = true;
        }
    }
    
    private string BuildKeyCombinationString(KeyEventArgs e)
    {
        var parts = new System.Collections.Generic.List<string>();
        
        // Add modifiers
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            parts.Add("Ctrl");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            parts.Add("Alt");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            parts.Add("Shift");
        
        // Add the main key
        var keyString = GetKeyString(e.Key);
        if (!string.IsNullOrEmpty(keyString))
        {
            parts.Add(keyString);
        }
        
        // Must have at least a non-modifier key
        if (parts.Count == 0 || (e.KeyModifiers != KeyModifiers.None && parts.Count == CountModifiers(e.KeyModifiers)))
            return string.Empty;
        
        return string.Join("+", parts);
    }
    
    private int CountModifiers(KeyModifiers modifiers)
    {
        int count = 0;
        if (modifiers.HasFlag(KeyModifiers.Control)) count++;
        if (modifiers.HasFlag(KeyModifiers.Alt)) count++;
        if (modifiers.HasFlag(KeyModifiers.Shift)) count++;
        return count;
    }
    
    private string GetKeyString(Key key)
    {
        // Function keys
        if (key >= Key.F1 && key <= Key.F24)
            return key.ToString();
        
        // Number keys
        if (key >= Key.D0 && key <= Key.D9)
            return key.ToString().Replace("D", "");
        
        // NumPad keys
        if (key >= Key.NumPad0 && key <= Key.NumPad9)
            return key.ToString();
        
        // Letters
        if (key >= Key.A && key <= Key.Z)
            return key.ToString();
        
        // Special keys
        switch (key)
        {
            case Key.Add: return "+";
            case Key.Subtract: return "-";
            case Key.Multiply: return "*";
            case Key.Divide: return "/";
            case Key.OemPlus: return "+";
            case Key.OemMinus: return "-";
            case Key.Space: return "Space";
            case Key.Home: return "Home";
            case Key.End: return "End";
            case Key.PageUp: return "PageUp";
            case Key.PageDown: return "PageDown";
            case Key.Up: return "Up";
            case Key.Down: return "Down";
            case Key.Left: return "Left";
            case Key.Right: return "Right";
            case Key.Insert: return "Insert";
            default: return string.Empty;
        }
    }
    
    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        // In a real implementation, save the hotkeys to settings
        Close(true);
    }
    
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
