using System;
using AvaloniaGRBL.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaGRBL.Services;

/// <summary>
/// Singleton service that manages the application theme and provides color properties.
/// This is the single source of truth for all theme colors in the application.
/// </summary>
public partial class ThemeService : ObservableObject
{
    private static ThemeService? _instance;
    private static readonly object _lock = new object();

    /// <summary>
    /// Gets the singleton instance of the ThemeService
    /// </summary>
    public static ThemeService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ThemeService();
                }
            }
            return _instance;
        }
    }

    private ThemeService()
    {
        // Initialize with default "Dark" theme
        SetTheme("Dark");
    }

    [ObservableProperty]
    private string _currentThemeName = "Dark";

    // Main background colors
    [ObservableProperty] private string _mainBackground = "#2A3647";
    [ObservableProperty] private string _panelBackground = "#1E2936";
    [ObservableProperty] private string _borderColor = "#0F1419";
    [ObservableProperty] private string _canvasBackground = "#2D3E50";

    // Button colors
    [ObservableProperty] private string _buttonBackground = "#3C4A5C";
    [ObservableProperty] private string _buttonForeground = "#CCCCCC";
    [ObservableProperty] private string _buttonHoverBackground = "#4A5A6F";
    [ObservableProperty] private string _buttonPressedBackground = "#2A3647";
    [ObservableProperty] private string _buttonBorder = "#2A3647";

    // Jog button colors (directional controls)
    [ObservableProperty] private string _jogButtonForeground = "#8AB4F8";
    [ObservableProperty] private string _jogButtonHoverForeground = "#AACCFF";

    // Action button colors (highlighted buttons)
    [ObservableProperty] private string _actionButtonForeground = "#FFD700";
    [ObservableProperty] private string _actionButtonBorder = "#FFD700";
    [ObservableProperty] private string _actionButtonHoverForeground = "#FFE44D";

    // Text colors
    [ObservableProperty] private string _primaryText = "#CCCCCC";
    [ObservableProperty] private string _secondaryText = "#999999";
    [ObservableProperty] private string _accentText = "#8AB4F8";
    [ObservableProperty] private string _consoleText = "#00FF00";

    // Control colors
    [ObservableProperty] private string _sliderBackground = "#3C4A5C";
    [ObservableProperty] private string _sliderForeground = "#8AB4F8";
    [ObservableProperty] private string _comboBoxBackground = "#3C4A5C";
    [ObservableProperty] private string _comboBoxForeground = "#CCCCCC";
    [ObservableProperty] private string _separatorColor = "#3C4A5C";

    // Status colors
    [ObservableProperty] private string _infoBoxBackground = "#1E2936";
    [ObservableProperty] private string _infoBoxBorder = "#3C4A5C";

    /// <summary>
    /// Changes the current theme by applying all colors from the specified theme
    /// </summary>
    /// <param name="themeName">Name of the theme to apply</param>
    /// <returns>True if theme was successfully applied, false if theme not found</returns>
    public bool SetTheme(string themeName)
    {
        var themes = ColorTheme.GetAllThemes();

        if (!themes.TryGetValue(themeName, out var theme))
        {
            return false;
        }

        CurrentThemeName = themeName;

        // Apply all theme colors
        MainBackground = theme.MainBackground;
        PanelBackground = theme.PanelBackground;
        BorderColor = theme.BorderColor;
        CanvasBackground = theme.CanvasBackground;
        ButtonBackground = theme.ButtonBackground;
        ButtonForeground = theme.ButtonForeground;
        ButtonHoverBackground = theme.ButtonHoverBackground;
        ButtonPressedBackground = theme.ButtonPressedBackground;
        ButtonBorder = theme.ButtonBorder;
        JogButtonForeground = theme.JogButtonForeground;
        JogButtonHoverForeground = theme.JogButtonHoverForeground;
        ActionButtonForeground = theme.ActionButtonForeground;
        ActionButtonBorder = theme.ActionButtonBorder;
        ActionButtonHoverForeground = theme.ActionButtonHoverForeground;
        PrimaryText = theme.PrimaryText;
        SecondaryText = theme.SecondaryText;
        AccentText = theme.AccentText;
        ConsoleText = theme.ConsoleText;
        SliderBackground = theme.SliderBackground;
        SliderForeground = theme.SliderForeground;
        ComboBoxBackground = theme.ComboBoxBackground;
        ComboBoxForeground = theme.ComboBoxForeground;
        SeparatorColor = theme.SeparatorColor;
        InfoBoxBackground = theme.InfoBoxBackground;
        InfoBoxBorder = theme.InfoBoxBorder;

        return true;
    }

    /// <summary>
    /// Gets the list of available theme names
    /// </summary>
    public string[] GetAvailableThemes()
    {
        var themes = ColorTheme.GetAllThemes();
        var names = new string[themes.Count];
        themes.Keys.CopyTo(names, 0);
        return names;
    }
}
