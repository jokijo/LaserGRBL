using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AvaloniaGRBL.Models;

/// <summary>
/// Represents a G-Code file with all its commands and bounding box information
/// </summary>
public class GCodeFile
{
    public List<GCodeCommand> Commands { get; private set; }
    public string FileName { get; set; }
    public GCodeBounds Bounds { get; private set; }
    
    public GCodeFile()
    {
        Commands = new List<GCodeCommand>();
        Bounds = new GCodeBounds();
        FileName = string.Empty;
    }
    
    public static GCodeFile Load(string filePath)
    {
        var file = new GCodeFile
        {
            FileName = Path.GetFileName(filePath)
        };
        
        var lines = File.ReadAllLines(filePath);
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                var command = new GCodeCommand(line);
                file.Commands.Add(command);
            }
        }
        
        file.CalculateBounds();
        return file;
    }
    
    public void CalculateBounds()
    {
        Bounds = new GCodeBounds();
        
        double currentX = 0, currentY = 0, currentZ = 0;
        
        foreach (var cmd in Commands)
        {
            // Update current position based on command
            if (cmd.CommandType == GCodeCommandType.RapidMove || 
                cmd.CommandType == GCodeCommandType.LinearMove ||
                cmd.CommandType == GCodeCommandType.ArcCW ||
                cmd.CommandType == GCodeCommandType.ArcCCW)
            {
                // NOTE: This assumes absolute positioning mode (G90)
                // TODO: Add support for relative positioning mode (G91) by tracking modal state
                // and adding relative coordinates to current position instead of replacing them
                if (cmd.HasParameter('X'))
                    currentX = cmd.GetParameter('X');
                if (cmd.HasParameter('Y'))
                    currentY = cmd.GetParameter('Y');
                if (cmd.HasParameter('Z'))
                    currentZ = cmd.GetParameter('Z');
                
                Bounds.UpdateBounds(currentX, currentY, currentZ);
            }
        }
    }
    
    public int CommandCount => Commands.Count;
    
    public bool IsEmpty => Commands.Count == 0;
    
    /// <summary>
    /// Appends commands from another file to this file
    /// </summary>
    public void AppendCommands(List<GCodeCommand> commands)
    {
        Commands.AddRange(commands);
        CalculateBounds();
    }
    
    /// <summary>
    /// Saves the G-Code file to the specified path
    /// </summary>
    public async Task SaveAsync(string filePath)
    {
        var lines = Commands.Select(cmd => cmd.RawCommand).ToArray();
        await File.WriteAllLinesAsync(filePath, lines);
        FileName = Path.GetFileName(filePath);
    }
    
    /// <summary>
    /// Generates a Shake Test G-Code program to test machine reliability
    /// </summary>
    public void GenerateShakeTest(string axis, int flimit, int axislen, int cpower, int cspeed)
    {
        Commands.Clear();
        Bounds = new GCodeBounds();
        
        // Initial setup - laser OFF and move to origin
        Commands.Add(new GCodeCommand("M5"));  // laser OFF
        Commands.Add(new GCodeCommand("G1 F1000 X0 Y0 S0"));  // move to origin (slowly)
        Commands.Add(new GCodeCommand($"G1 F{cspeed} X7 Y10"));  // positioning
        Commands.Add(new GCodeCommand("M4"));  // laser ON
        Commands.Add(new GCodeCommand($"G1 F{cspeed} S{cpower} X13 Y10"));  // draw cross
        Commands.Add(new GCodeCommand("M5"));  // laser OFF
        Commands.Add(new GCodeCommand($"G1 F{cspeed} X10 Y7"));  // positioning
        Commands.Add(new GCodeCommand("M4"));  // laser ON
        Commands.Add(new GCodeCommand($"G1 F{cspeed} S{cpower} X10 Y13"));  // draw cross
        Commands.Add(new GCodeCommand("M5"));  // laser OFF
        Commands.Add(new GCodeCommand("G1 F1000 X10 Y10 S0"));  // move to cross center (slowly)
        
        // Perform shake tests with different trip distances
        GenerateShakeTest2(axis, flimit, axislen, 10, 50, 0.5);
        GenerateShakeTest2(axis, flimit, axislen, 10, 100, 2);
        GenerateShakeTest2(axis, flimit, axislen, 10, 200, 4);
        GenerateShakeTest2(axis, flimit, axislen, 10, 400, 8);
        
        // Move back to cross center
        Commands.Add(new GCodeCommand($"G1 F{cspeed} X10 Y10 S0"));  // move to cross center (fast)
        
        // Draw the cross again to verify position accuracy
        Commands.Add(new GCodeCommand($"G1 F{cspeed} X7 Y10"));  // positioning
        Commands.Add(new GCodeCommand("M4"));  // laser ON
        Commands.Add(new GCodeCommand($"G1 F{cspeed} S{cpower} X13 Y10"));  // draw cross
        Commands.Add(new GCodeCommand("M5"));  // laser OFF
        Commands.Add(new GCodeCommand($"G1 F{cspeed} X10 Y7"));  // positioning
        Commands.Add(new GCodeCommand("M4"));  // laser ON
        Commands.Add(new GCodeCommand($"G1 F{cspeed} S{cpower} X10 Y13"));  // draw cross
        Commands.Add(new GCodeCommand("M5"));  // laser OFF
        Commands.Add(new GCodeCommand("G1 F1000 X0 Y0 S0"));  // move to origin (slowly)
        
        CalculateBounds();
    }
    
    private void GenerateShakeTest2(string axis, int flimit, int axislen, int o, int trip, double step)
    {
        for (int c = trip / 2; c < axislen - trip / 2; c += trip)  // center of oscillation points
        {
            for (double i = 0; i < trip / 3; i += step)
            {
                Commands.Add(new GCodeCommand($"G1 F{flimit} {axis}{FormatNumber(o + c + i)}"));
                Commands.Add(new GCodeCommand($"G1 F{flimit} {axis}{FormatNumber(o + c - i)}"));
            }
        }
    }
    
    private string FormatNumber(double value)
    {
        return value.ToString("F3").TrimEnd('0').TrimEnd('.');
    }
}

public class GCodeBounds
{
    public double MinX { get; private set; } = double.MaxValue;
    public double MaxX { get; private set; } = double.MinValue;
    public double MinY { get; private set; } = double.MaxValue;
    public double MaxY { get; private set; } = double.MinValue;
    public double MinZ { get; private set; } = double.MaxValue;
    public double MaxZ { get; private set; } = double.MinValue;
    
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
    public double Depth => MaxZ - MinZ;
    
    public bool IsValid => MinX != double.MaxValue && MaxX != double.MinValue &&
                           MinY != double.MaxValue && MaxY != double.MinValue;
    
    public void UpdateBounds(double x, double y, double z)
    {
        MinX = Math.Min(MinX, x);
        MaxX = Math.Max(MaxX, x);
        MinY = Math.Min(MinY, y);
        MaxY = Math.Max(MaxY, y);
        MinZ = Math.Min(MinZ, z);
        MaxZ = Math.Max(MaxZ, z);
    }
}
