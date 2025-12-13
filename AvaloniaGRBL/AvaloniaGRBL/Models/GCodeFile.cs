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
