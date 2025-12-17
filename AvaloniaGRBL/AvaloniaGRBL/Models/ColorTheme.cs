using System.Collections.Generic;

namespace AvaloniaGRBL.Models;

/// <summary>
/// Represents a color theme with all UI element colors
/// </summary>
public class ColorTheme
{
    public string Name { get; set; } = string.Empty;
    
    // Main background colors
    public string MainBackground { get; set; } = "#2A3647";
    public string PanelBackground { get; set; } = "#1E2936";
    public string BorderColor { get; set; } = "#0F1419";
    public string CanvasBackground { get; set; } = "#2D3E50";
    
    // Button colors
    public string ButtonBackground { get; set; } = "#3C4A5C";
    public string ButtonForeground { get; set; } = "#CCCCCC";
    public string ButtonHoverBackground { get; set; } = "#4A5A6F";
    public string ButtonPressedBackground { get; set; } = "#2A3647";
    public string ButtonBorder { get; set; } = "#2A3647";
    
    // Jog button colors (directional controls)
    public string JogButtonForeground { get; set; } = "#8AB4F8";
    public string JogButtonHoverForeground { get; set; } = "#AACCFF";
    
    // Action button colors (highlighted buttons)
    public string ActionButtonForeground { get; set; } = "#FFD700";
    public string ActionButtonBorder { get; set; } = "#FFD700";
    public string ActionButtonHoverForeground { get; set; } = "#FFE44D";
    
    // Text colors
    public string PrimaryText { get; set; } = "#CCCCCC";
    public string SecondaryText { get; set; } = "#999999";
    public string AccentText { get; set; } = "#8AB4F8";
    public string ConsoleText { get; set; } = "#00FF00";
    
    // Control colors
    public string SliderBackground { get; set; } = "#3C4A5C";
    public string SliderForeground { get; set; } = "#8AB4F8";
    public string ComboBoxBackground { get; set; } = "#3C4A5C";
    public string ComboBoxForeground { get; set; } = "#CCCCCC";
    public string SeparatorColor { get; set; } = "#3C4A5C";
    
    // Status colors
    public string InfoBoxBackground { get; set; } = "#1E2936";
    public string InfoBoxBorder { get; set; } = "#3C4A5C";
    
    /// <summary>
    /// Get all available predefined themes
    /// </summary>
    public static Dictionary<string, ColorTheme> GetAllThemes()
    {
        return new Dictionary<string, ColorTheme>
        {
            { "CAD Style", new ColorTheme
                {
                    Name = "CAD Style",
                    MainBackground = "#000000",
                    PanelBackground = "#1A1A1A",
                    BorderColor = "#333333",
                    CanvasBackground = "#0A0A0A",
                    ButtonBackground = "#2A2A2A",
                    ButtonForeground = "#FFFFFF",
                    ButtonHoverBackground = "#3A3A3A",
                    ButtonPressedBackground = "#1A1A1A",
                    ButtonBorder = "#404040",
                    JogButtonForeground = "#00FFFF",
                    JogButtonHoverForeground = "#66FFFF",
                    ActionButtonForeground = "#FFFF00",
                    ActionButtonBorder = "#FFFF00",
                    ActionButtonHoverForeground = "#FFFF66",
                    PrimaryText = "#FFFFFF",
                    SecondaryText = "#AAAAAA",
                    AccentText = "#00FFFF",
                    ConsoleText = "#00FF00",
                    SliderBackground = "#2A2A2A",
                    SliderForeground = "#00FFFF",
                    ComboBoxBackground = "#2A2A2A",
                    ComboBoxForeground = "#FFFFFF",
                    SeparatorColor = "#404040",
                    InfoBoxBackground = "#1A1A1A",
                    InfoBoxBorder = "#404040"
                }
            },
            { "CAD Dark", new ColorTheme
                {
                    Name = "CAD Dark",
                    MainBackground = "#0D1117",
                    PanelBackground = "#161B22",
                    BorderColor = "#21262D",
                    CanvasBackground = "#010409",
                    ButtonBackground = "#21262D",
                    ButtonForeground = "#C9D1D9",
                    ButtonHoverBackground = "#30363D",
                    ButtonPressedBackground = "#161B22",
                    ButtonBorder = "#30363D",
                    JogButtonForeground = "#58A6FF",
                    JogButtonHoverForeground = "#79C0FF",
                    ActionButtonForeground = "#F0883E",
                    ActionButtonBorder = "#F0883E",
                    ActionButtonHoverForeground = "#FFA657",
                    PrimaryText = "#C9D1D9",
                    SecondaryText = "#8B949E",
                    AccentText = "#58A6FF",
                    ConsoleText = "#7EE787",
                    SliderBackground = "#21262D",
                    SliderForeground = "#58A6FF",
                    ComboBoxBackground = "#21262D",
                    ComboBoxForeground = "#C9D1D9",
                    SeparatorColor = "#30363D",
                    InfoBoxBackground = "#161B22",
                    InfoBoxBorder = "#30363D"
                }
            },
            { "Blue Laser", new ColorTheme
                {
                    Name = "Blue Laser",
                    MainBackground = "#1A1F35",
                    PanelBackground = "#0F1419",
                    BorderColor = "#0A0D15",
                    CanvasBackground = "#0D1220",
                    ButtonBackground = "#2A3550",
                    ButtonForeground = "#A8C5E6",
                    ButtonHoverBackground = "#3A4560",
                    ButtonPressedBackground = "#1A2540",
                    ButtonBorder = "#1A2540",
                    JogButtonForeground = "#4A9EFF",
                    JogButtonHoverForeground = "#6AB5FF",
                    ActionButtonForeground = "#FFD700",
                    ActionButtonBorder = "#FFD700",
                    ActionButtonHoverForeground = "#FFE44D",
                    PrimaryText = "#A8C5E6",
                    SecondaryText = "#6B7B95",
                    AccentText = "#4A9EFF",
                    ConsoleText = "#00FFFF",
                    SliderBackground = "#2A3550",
                    SliderForeground = "#4A9EFF",
                    ComboBoxBackground = "#2A3550",
                    ComboBoxForeground = "#A8C5E6",
                    SeparatorColor = "#1A2540",
                    InfoBoxBackground = "#0F1419",
                    InfoBoxBorder = "#2A3550"
                }
            },
            { "Red Laser", new ColorTheme
                {
                    Name = "Red Laser",
                    MainBackground = "#2A1515",
                    PanelBackground = "#1F0D0D",
                    BorderColor = "#150808",
                    CanvasBackground = "#1A0A0A",
                    ButtonBackground = "#4A2020",
                    ButtonForeground = "#FFB3B3",
                    ButtonHoverBackground = "#5A3030",
                    ButtonPressedBackground = "#3A1515",
                    ButtonBorder = "#3A1515",
                    JogButtonForeground = "#FF4444",
                    JogButtonHoverForeground = "#FF6666",
                    ActionButtonForeground = "#FFD700",
                    ActionButtonBorder = "#FFD700",
                    ActionButtonHoverForeground = "#FFE44D",
                    PrimaryText = "#FFB3B3",
                    SecondaryText = "#996666",
                    AccentText = "#FF4444",
                    ConsoleText = "#FF6666",
                    SliderBackground = "#4A2020",
                    SliderForeground = "#FF4444",
                    ComboBoxBackground = "#4A2020",
                    ComboBoxForeground = "#FFB3B3",
                    SeparatorColor = "#3A1515",
                    InfoBoxBackground = "#1F0D0D",
                    InfoBoxBorder = "#4A2020"
                }
            },
            { "Dark", new ColorTheme
                {
                    Name = "Dark",
                    MainBackground = "#2A3647",
                    PanelBackground = "#1E2936",
                    BorderColor = "#0F1419",
                    CanvasBackground = "#2D3E50",
                    ButtonBackground = "#3C4A5C",
                    ButtonForeground = "#CCCCCC",
                    ButtonHoverBackground = "#4A5A6F",
                    ButtonPressedBackground = "#2A3647",
                    ButtonBorder = "#2A3647",
                    JogButtonForeground = "#8AB4F8",
                    JogButtonHoverForeground = "#AACCFF",
                    ActionButtonForeground = "#FFD700",
                    ActionButtonBorder = "#FFD700",
                    ActionButtonHoverForeground = "#FFE44D",
                    PrimaryText = "#CCCCCC",
                    SecondaryText = "#999999",
                    AccentText = "#8AB4F8",
                    ConsoleText = "#00FF00",
                    SliderBackground = "#3C4A5C",
                    SliderForeground = "#8AB4F8",
                    ComboBoxBackground = "#3C4A5C",
                    ComboBoxForeground = "#CCCCCC",
                    SeparatorColor = "#3C4A5C",
                    InfoBoxBackground = "#1E2936",
                    InfoBoxBorder = "#3C4A5C"
                }
            },
            { "Hacker", new ColorTheme
                {
                    Name = "Hacker",
                    MainBackground = "#000000",
                    PanelBackground = "#001100",
                    BorderColor = "#002200",
                    CanvasBackground = "#000A00",
                    ButtonBackground = "#003300",
                    ButtonForeground = "#00FF00",
                    ButtonHoverBackground = "#004400",
                    ButtonPressedBackground = "#002200",
                    ButtonBorder = "#00FF00",
                    JogButtonForeground = "#00FF00",
                    JogButtonHoverForeground = "#33FF33",
                    ActionButtonForeground = "#00FFFF",
                    ActionButtonBorder = "#00FFFF",
                    ActionButtonHoverForeground = "#33FFFF",
                    PrimaryText = "#00FF00",
                    SecondaryText = "#009900",
                    AccentText = "#00FFFF",
                    ConsoleText = "#00FF00",
                    SliderBackground = "#003300",
                    SliderForeground = "#00FF00",
                    ComboBoxBackground = "#003300",
                    ComboBoxForeground = "#00FF00",
                    SeparatorColor = "#002200",
                    InfoBoxBackground = "#001100",
                    InfoBoxBorder = "#00FF00"
                }
            },
            { "Nighty", new ColorTheme
                {
                    Name = "Nighty",
                    MainBackground = "#0F0F23",
                    PanelBackground = "#1E1E3F",
                    BorderColor = "#2D2D5E",
                    CanvasBackground = "#0A0A1E",
                    ButtonBackground = "#2D2D5E",
                    ButtonForeground = "#B8B8E8",
                    ButtonHoverBackground = "#3C3C7D",
                    ButtonPressedBackground = "#1E1E3F",
                    ButtonBorder = "#3C3C7D",
                    JogButtonForeground = "#8A8AFF",
                    JogButtonHoverForeground = "#A5A5FF",
                    ActionButtonForeground = "#FFBB00",
                    ActionButtonBorder = "#FFBB00",
                    ActionButtonHoverForeground = "#FFD333",
                    PrimaryText = "#B8B8E8",
                    SecondaryText = "#7878A8",
                    AccentText = "#8A8AFF",
                    ConsoleText = "#66FF66",
                    SliderBackground = "#2D2D5E",
                    SliderForeground = "#8A8AFF",
                    ComboBoxBackground = "#2D2D5E",
                    ComboBoxForeground = "#B8B8E8",
                    SeparatorColor = "#3C3C7D",
                    InfoBoxBackground = "#1E1E3F",
                    InfoBoxBorder = "#2D2D5E"
                }
            }
        };
    }
}
