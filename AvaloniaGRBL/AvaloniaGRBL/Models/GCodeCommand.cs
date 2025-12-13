using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AvaloniaGRBL.Models;

/// <summary>
/// Represents a single G-Code command
/// </summary>
public class GCodeCommand
{
    public string RawCommand { get; set; }
    public GCodeCommandType CommandType { get; set; }
    public Dictionary<char, double> Parameters { get; set; }
    
    public GCodeCommand(string command)
    {
        RawCommand = command?.Trim() ?? string.Empty;
        Parameters = new Dictionary<char, double>();
        Parse();
    }
    
    private void Parse()
    {
        if (string.IsNullOrWhiteSpace(RawCommand) || RawCommand.StartsWith(";") || RawCommand.StartsWith("("))
        {
            CommandType = GCodeCommandType.Comment;
            return;
        }
        
        // Remove comments from the line
        string cleanCommand = RawCommand;
        int commentIndex = cleanCommand.IndexOf(';');
        if (commentIndex >= 0)
            cleanCommand = cleanCommand.Substring(0, commentIndex).Trim();
        
        commentIndex = cleanCommand.IndexOf('(');
        if (commentIndex >= 0)
            cleanCommand = cleanCommand.Substring(0, commentIndex).Trim();
        
        if (string.IsNullOrWhiteSpace(cleanCommand))
        {
            CommandType = GCodeCommandType.Comment;
            return;
        }
        
        // Parse command type
        if (cleanCommand.StartsWith("G0", StringComparison.OrdinalIgnoreCase) || 
            cleanCommand.Contains("G0 ") || cleanCommand.Contains("G0X") || cleanCommand.Contains("G0Y"))
        {
            CommandType = GCodeCommandType.RapidMove;
        }
        else if (cleanCommand.StartsWith("G1", StringComparison.OrdinalIgnoreCase) || 
                 cleanCommand.Contains("G1 ") || cleanCommand.Contains("G1X") || cleanCommand.Contains("G1Y"))
        {
            CommandType = GCodeCommandType.LinearMove;
        }
        else if (cleanCommand.StartsWith("G2", StringComparison.OrdinalIgnoreCase) || cleanCommand.Contains("G2 "))
        {
            CommandType = GCodeCommandType.ArcCW;
        }
        else if (cleanCommand.StartsWith("G3", StringComparison.OrdinalIgnoreCase) || cleanCommand.Contains("G3 "))
        {
            CommandType = GCodeCommandType.ArcCCW;
        }
        else if (cleanCommand.StartsWith("M3", StringComparison.OrdinalIgnoreCase) || cleanCommand.Contains("M3 ") || cleanCommand.Contains("M3S"))
        {
            CommandType = GCodeCommandType.SpindleOn;
        }
        else if (cleanCommand.StartsWith("M5", StringComparison.OrdinalIgnoreCase) || cleanCommand.Contains("M5"))
        {
            CommandType = GCodeCommandType.SpindleOff;
        }
        else
        {
            CommandType = GCodeCommandType.Other;
        }
        
        // Parse parameters using regex to extract letter-number pairs
        var matches = Regex.Matches(cleanCommand, @"([A-Z])(-?\d+\.?\d*)");
        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                char letter = match.Groups[1].Value[0];
                if (double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    Parameters[letter] = value;
                }
            }
        }
    }
    
    public bool HasParameter(char param) => Parameters.ContainsKey(param);
    
    public double GetParameter(char param, double defaultValue = 0) => 
        Parameters.TryGetValue(param, out double value) ? value : defaultValue;
}

public enum GCodeCommandType
{
    Comment,
    RapidMove,      // G0
    LinearMove,     // G1
    ArcCW,          // G2 - Clockwise arc
    ArcCCW,         // G3 - Counter-clockwise arc
    SpindleOn,      // M3
    SpindleOff,     // M5
    Other
}
