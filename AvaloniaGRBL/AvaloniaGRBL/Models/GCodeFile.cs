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
        bool absoluteMode = true; // G90 is default (absolute positioning)
        
        // Start with origin point
        Bounds.UpdateBounds(0, 0, 0);
        
        foreach (var cmd in Commands)
        {
            // Update modal state FIRST (before processing movement)
            // Modal commands on the same line affect the current command
            // Check all G-codes on this line (there can be multiple)
            foreach (var gValue in cmd.GCodes)
            {
                if (gValue == 90)
                    absoluteMode = true;
                else if (gValue == 91)
                    absoluteMode = false;
            }
            
            // Now process movement commands using the updated modal state
            if (cmd.CommandType == GCodeCommandType.RapidMove || 
                cmd.CommandType == GCodeCommandType.LinearMove ||
                cmd.CommandType == GCodeCommandType.ArcCW ||
                cmd.CommandType == GCodeCommandType.ArcCCW)
            {
                // Update position based on absolute or relative mode
                if (absoluteMode)
                {
                    // Absolute mode: parameters are absolute positions
                    if (cmd.HasParameter('X'))
                        currentX = cmd.GetParameter('X');
                    if (cmd.HasParameter('Y'))
                        currentY = cmd.GetParameter('Y');
                    if (cmd.HasParameter('Z'))
                        currentZ = cmd.GetParameter('Z');
                }
                else
                {
                    // Relative mode: parameters are offsets from current position
                    if (cmd.HasParameter('X'))
                        currentX += cmd.GetParameter('X');
                    if (cmd.HasParameter('Y'))
                        currentY += cmd.GetParameter('Y');
                    if (cmd.HasParameter('Z'))
                        currentZ += cmd.GetParameter('Z');
                }
                
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
    
    /// <summary>
    /// Generate a power vs speed test grid with gradients
    /// </summary>
    public void GenerateGreyscaleTest(int fRow, int sCol, int fStart, int fEnd, int sStart, int sEnd, 
                                      int xSize, int ySize, double resolution, int fText, int sText, 
                                      string title, string laserMode)
    {
        Commands.Clear();
        Bounds = new GCodeBounds();
        
        double ox = 3;
        double oy = 3;
        
        bool forward = true;
        double fDelta = fRow > 1 ? (fEnd - fStart) / (double)(fRow - 1) : 0;
        double sDelta = sCol > 1 ? (sEnd - sStart) / (double)(sCol - 1) : 0;
        
        double xStep = xSize / (double)sCol;
        double yStep = ySize / (double)fRow;
        double fillingStep = 1 / resolution;
        
        // Back to origin
        Commands.Add(new GCodeCommand($"G0 X{FormatNumber(ox)} Y{FormatNumber(oy)} S0"));
        Commands.Add(new GCodeCommand($"G1 {laserMode} F{FormatNumber(fStart)}"));
        
        // Draw filling
        double prevF = double.NaN, curF = double.NaN;
        forward = true;
        for (double y = 0; y <= ySize; y += fillingStep)
        {
            curF = fStart + ((int)(y / yStep)) * fDelta;
            
            if (curF != prevF)
            {
                Commands.Add(new GCodeCommand($"F{FormatNumber(curF)}"));
                prevF = curF;
            }
            
            Commands.Add(new GCodeCommand($"Y{FormatNumber(oy + y)} S0"));
            
            for (int x = 0; x < sCol; x++)
            {
                double cx = forward ? ((x + 1) * xStep) : xSize - ((x + 1) * xStep);
                double cs = forward ? sStart + (x * sDelta) : sEnd - (x * sDelta);
                
                Commands.Add(new GCodeCommand($"X{FormatNumber(ox + cx)} S{FormatNumber(cs)}"));
            }
            
            forward = !forward;
        }
        
        // Back to origin and draw grid X
        int fGrid = fText;
        int sGrid = sText;
        
        Commands.Add(new GCodeCommand($"G0 X{FormatNumber(ox)} Y{FormatNumber(oy)} S0"));
        Commands.Add(new GCodeCommand($"G1 {laserMode} F{FormatNumber(fGrid)}"));
        
        forward = true;
        for (int y = 0; y < fRow + 1; y++)
        {
            double cy = y * yStep;
            Commands.Add(new GCodeCommand($"Y{FormatNumber(oy + cy)} S0"));
            
            for (int x = 0; x < sCol + 1; x++)
            {
                double cx = forward ? x * xStep : xSize - x * xStep;
                Commands.Add(new GCodeCommand($"X{FormatNumber(ox + cx)} S{FormatNumber(sGrid)}"));
            }
            
            forward = !forward;
        }
        
        // Back to origin and draw grid Y
        Commands.Add(new GCodeCommand($"G0 X{FormatNumber(ox)} Y{FormatNumber(oy)} S0"));
        Commands.Add(new GCodeCommand($"G1 {laserMode} F{FormatNumber(fGrid)}"));
        
        forward = true;
        for (int x = 0; x < sCol + 1; x++)
        {
            double cx = x * xStep;
            Commands.Add(new GCodeCommand($"X{FormatNumber(ox + cx)} S0"));
            
            for (int y = 0; y < fRow + 1; y++)
            {
                double cy = forward ? y * yStep : ySize - y * yStep;
                Commands.Add(new GCodeCommand($"Y{FormatNumber(oy + cy)} S{FormatNumber(sGrid)}"));
            }
            
            forward = !forward;
        }
        
        // Add title (simplified - in reality would need a text rendering system)
        string srange = (sStart != sEnd) ? $"S{sStart} - S{sEnd}" : $"S{sEnd}";
        string frange = (fStart != fEnd) ? $"F{fStart} - F{fEnd}" : $"F{fEnd}";
        
        if (string.IsNullOrEmpty(title))
            title = "LaserGRBL power/speed test";
        else
            title = $"LaserGRBL power/speed test [{title}]";
        
        // Laser off
        Commands.Add(new GCodeCommand("M5"));
        
        FileName = "PowerSpeed Test";
        CalculateBounds();
    }
    
    /// <summary>
    /// Generate a cutting test with multiple passes at different speeds
    /// </summary>
    public void GenerateCuttingTest(int fCol, int fStart, int fEnd, int pStart, int pEnd, 
                                    int sFixed, int fText, int sText, string title, string laserMode)
    {
        Commands.Clear();
        Bounds = new GCodeBounds();
        
        int pRow = pEnd - pStart + 1;
        double ox = 3;
        double oy = 3;
        
        int xSize = fCol * 14 - 4;
        int ySize = pRow * 14 - 4;
        
        double fDelta = fCol > 1 ? (fEnd - fStart) / (double)(fCol - 1) : 0;
        
        // Back to origin
        Commands.Add(new GCodeCommand($"G0 X{FormatNumber(ox)} Y{FormatNumber(oy)} S{FormatNumber(sFixed)}"));
        Commands.Add(new GCodeCommand($"G1 {laserMode} F{FormatNumber(fStart)}"));
        
        double cx = ox;
        double cy = oy;
        
        for (int p = 0; p < pRow; p++) // rows
        {
            cy = oy + 14 * p;
            for (int f = 0; f < fCol; f++) // cols
            {
                cx = ox + 14 * f;
                for (int pass = 0; pass < pStart + p; pass++)
                {
                    // Move to position
                    Commands.Add(new GCodeCommand($"G0 X{FormatNumber(cx)} Y{FormatNumber(cy)}"));
                    // Now draw rectangle
                    Commands.Add(new GCodeCommand($"G1 X{FormatNumber(cx + 10)} F{FormatNumber(fStart + fDelta * f)} {laserMode}")); // Laser ON
                    Commands.Add(new GCodeCommand($"G1 Y{FormatNumber(cy + 10)}"));
                    Commands.Add(new GCodeCommand($"G1 X{FormatNumber(cx)}"));
                    Commands.Add(new GCodeCommand($"G1 Y{FormatNumber(cy)}"));
                    Commands.Add(new GCodeCommand("M5")); // Laser OFF
                }
            }
        }
        
        // Back to origin
        Commands.Add(new GCodeCommand($"G0 X{FormatNumber(ox)} Y{FormatNumber(oy)} S0"));
        Commands.Add(new GCodeCommand($"G1 {laserMode} F{FormatNumber(fText)}"));
        
        // Add title (simplified - would need text rendering for full implementation)
        string srange = $"S{sFixed}";
        string frange = (fStart != fEnd) ? $"F{fStart} - F{fEnd}" : $"F{fEnd}";
        string prange = (pStart != pEnd) ? $"{pStart} - {pEnd} pass" : $"{pEnd} pass";
        
        if (string.IsNullOrEmpty(title))
            title = "LaserGRBL cutting test";
        else
            title = $"LaserGRBL cutting test [{title}]";
        
        // Laser off
        Commands.Add(new GCodeCommand("M5"));
        
        FileName = "Cutting Test";
        CalculateBounds();
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
