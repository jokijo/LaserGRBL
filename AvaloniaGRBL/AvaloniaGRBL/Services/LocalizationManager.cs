using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace AvaloniaGRBL.Services;

/// <summary>
/// Singleton localization manager that supports dynamic language switching without restart
/// </summary>
public class LocalizationManager : INotifyPropertyChanged
{
    private static LocalizationManager? _instance;
    private static readonly object _lock = new();
    
    private readonly ResourceManager _resourceManager;

    public static LocalizationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LocalizationManager();
                }
            }
            return _instance;
        }
    }

    private LocalizationManager()
    {
        _resourceManager = new ResourceManager("AvaloniaGRBL.Resources.Strings", typeof(LocalizationManager).Assembly);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Indexer to access localized strings by key
    /// </summary>
    public string this[string key]
    {
        get
        {
            try
            {
                return _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
            }
            catch
            {
                return key;
            }
        }
    }

    /// <summary>
    /// Changes the current UI culture and notifies all bindings to update
    /// </summary>
    public void SetCulture(string cultureCode)
    {
        try
        {
            var culture = new CultureInfo(cultureCode);
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            
            // Update thread culture
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            
            // Save to settings
            SettingsService.SetObject("UserLanguage", cultureCode);
            
            // Notify all bindings that all properties have changed
            OnPropertyChanged(string.Empty);
        }
        catch (CultureNotFoundException ex)
        {
            Console.WriteLine($"Culture not found: {cultureCode}. Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads saved language preference
    /// </summary>
    public void LoadSavedLanguage()
    {
        var savedLanguage = SettingsService.GetObject<string>("UserLanguage", "");
        if (!string.IsNullOrEmpty(savedLanguage))
        {
            try
            {
                var culture = new CultureInfo(savedLanguage);
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.CurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            }
            catch (CultureNotFoundException ex)
            {
                Console.WriteLine($"Saved culture not found: {savedLanguage}. Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Gets current language display name
    /// </summary>
    public string GetCurrentLanguageName()
    {
        var currentCode = CultureInfo.CurrentUICulture.Name;
        var langInfo = LocalizationService.GetLanguageInfo(currentCode);
        return langInfo?.DisplayName ?? "English";
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
