using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AvaloniaGRBL.Services;

/// <summary>
/// Manages application settings persistence and retrieval
/// </summary>
public static class SettingsService
{
    private static readonly Dictionary<string, object> _settings = new();
    private static readonly string _settingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AvaloniaGRBL",
        "settings.json"
    );
    
    static SettingsService()
    {
        LoadSettings();
    }
    
    /// <summary>
    /// Gets a setting value or returns the default if not found
    /// </summary>
    public static T GetObject<T>(string key, T defaultValue)
    {
        try
        {
            if (_settings.ContainsKey(key))
            {
                var value = _settings[key];
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
                }
                if (value is T typedValue)
                {
                    return typedValue;
                }
                // Try to convert
                return (T)Convert.ChangeType(value, typeof(T));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting setting {key}: {ex.Message}");
        }
        
        return defaultValue;
    }
    
    /// <summary>
    /// Sets a setting value
    /// </summary>
    public static void SetObject(string key, object value)
    {
        _settings[key] = value;
        SaveSettings();
    }
    
    /// <summary>
    /// Checks if a setting exists
    /// </summary>
    public static bool ExistObject(string key)
    {
        return _settings.ContainsKey(key);
    }
    
    /// <summary>
    /// Loads settings from disk
    /// </summary>
    private static void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (loaded != null)
                {
                    foreach (var kvp in loaded)
                    {
                        _settings[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Saves settings to disk
    /// </summary>
    private static void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            var json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Event raised when settings change
    /// </summary>
    public static event EventHandler? SettingsChanged;
    
    /// <summary>
    /// Notify that settings have changed
    /// </summary>
    public static void NotifySettingsChanged()
    {
        SettingsChanged?.Invoke(null, EventArgs.Empty);
    }
}
