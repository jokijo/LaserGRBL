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
    public List<double> GCodes { get; set; } // Track all G-codes on this line
    
    public GCodeCommand(string command)
    {
        RawCommand = command?.Trim() ?? string.Empty;
        Parameters = new Dictionary<char, double>();
        GCodes = new List<double>();
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
        
        // Parse command type using word boundary matching for accuracy
        if (Regex.IsMatch(cleanCommand, @"\bG0\b", RegexOptions.IgnoreCase))
        {
            CommandType = GCodeCommandType.RapidMove;
        }
        else if (Regex.IsMatch(cleanCommand, @"\bG1\b", RegexOptions.IgnoreCase))
        {
            CommandType = GCodeCommandType.LinearMove;
        }
        else if (Regex.IsMatch(cleanCommand, @"\bG2\b", RegexOptions.IgnoreCase))
        {
            CommandType = GCodeCommandType.ArcCW;
        }
        else if (Regex.IsMatch(cleanCommand, @"\bG3\b", RegexOptions.IgnoreCase))
        {
            CommandType = GCodeCommandType.ArcCCW;
        }
        else if (Regex.IsMatch(cleanCommand, @"\bM3\b", RegexOptions.IgnoreCase))
        {
            CommandType = GCodeCommandType.SpindleOn;
        }
        else if (Regex.IsMatch(cleanCommand, @"\bM4\b", RegexOptions.IgnoreCase))
        {
            CommandType = GCodeCommandType.SpindleOn; // M4 is laser mode spindle on
        }
        else if (Regex.IsMatch(cleanCommand, @"\bM5\b", RegexOptions.IgnoreCase))
        {
            CommandType = GCodeCommandType.SpindleOff;
        }
        else
        {
            CommandType = GCodeCommandType.Other;
        }
        
        // Parse parameters using regex to extract letter-number pairs
        // Updated regex to require digits after decimal point when present
        var matches = Regex.Matches(cleanCommand, @"([A-Z])(-?\d+(?:\.\d+)?)");
        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                char letter = match.Groups[1].Value[0];
                if (double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    // Track all G-codes separately since multiple can appear on one line
                    if (letter == 'G')
                    {
                        GCodes.Add(value);
                    }
                    // Store the last value for each parameter (for X, Y, Z, F, S, etc.)
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
