using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AvaloniaGRBL.Services;

/// <summary>
/// Service for managing application localization and language changes
/// </summary>
public static class LocalizationService
{
    /// <summary>
    /// List of available languages with their culture codes and display names
    /// </summary>
    public static readonly List<LanguageInfo> AvailableLanguages = new()
    {
        new LanguageInfo("en", "English"),
        new LanguageInfo("it", "Italian"),
        new LanguageInfo("es", "Spanish"),
        new LanguageInfo("fr", "French"),
        new LanguageInfo("de", "German"),
        new LanguageInfo("fi", "Finnish"),
        new LanguageInfo("da", "Danish"),
        new LanguageInfo("pt-BR", "Brazilian Portuguese"),
        new LanguageInfo("ru", "Russian"),
        new LanguageInfo("zh-CN", "Chinese (Simplified)"),
        new LanguageInfo("zh-TW", "Chinese (Traditional)"),
        new LanguageInfo("sk-SK", "Slovak"),
        new LanguageInfo("hu-HU", "Hungarian"),
        new LanguageInfo("cs-CZ", "Czech"),
        new LanguageInfo("pl-PL", "Polish"),
        new LanguageInfo("el-GR", "Greek"),
        new LanguageInfo("tr-TR", "Turkish"),
        new LanguageInfo("ro-RO", "Romanian"),
        new LanguageInfo("nl-NL", "Dutch"),
        new LanguageInfo("uk", "Ukrainian"),
        new LanguageInfo("ja-JP", "Japanese")
    };

    /// <summary>
    /// Gets the current UI culture
    /// </summary>
    public static CultureInfo CurrentCulture => 
        System.Threading.Thread.CurrentThread.CurrentUICulture;

    /// <summary>
    /// Sets the application language (delegates to LocalizationManager for dynamic switching)
    /// </summary>
    /// <param name="cultureCode">Culture code (e.g., "en", "it", "es")</param>
    public static void SetLanguage(string cultureCode)
    {
        LocalizationManager.Instance.SetCulture(cultureCode);
    }

    /// <summary>
    /// Loads the saved language preference
    /// </summary>
    public static void LoadSavedLanguage()
    {
        LocalizationManager.Instance.LoadSavedLanguage();
    }

    /// <summary>
    /// Gets language info by culture code
    /// </summary>
    public static LanguageInfo? GetLanguageInfo(string cultureCode)
    {
        return AvailableLanguages.FirstOrDefault(l => 
            l.CultureCode.Equals(cultureCode, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the display name of current language
    /// </summary>
    public static string GetCurrentLanguageName()
    {
        var currentCode = CurrentCulture.Name;
        var langInfo = GetLanguageInfo(currentCode);
        return langInfo?.DisplayName ?? "English";
    }
}

/// <summary>
/// Language information
/// </summary>
public class LanguageInfo
{
    public string CultureCode { get; }
    public string DisplayName { get; }

    public LanguageInfo(string cultureCode, string displayName)
    {
        CultureCode = cultureCode;
        DisplayName = displayName;
    }
}
