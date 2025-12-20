using System.Linq;

namespace AvaloniaGRBL.Models;

/// <summary>
/// Represents the current status of the GRBL controller
/// </summary>
public class GrblStatus
{
    /// <summary>Machine state (Idle, Run, Hold, etc.)</summary>
    public string State { get; set; } = "Unknown";
    
    /// <summary>Machine position X coordinate</summary>
    public double MachineX { get; set; }
    
    /// <summary>Machine position Y coordinate</summary>
    public double MachineY { get; set; }
    
    /// <summary>Machine position Z coordinate</summary>
    public double MachineZ { get; set; }
    
    /// <summary>Work position X coordinate</summary>
    public double WorkX { get; set; }
    
    /// <summary>Work position Y coordinate</summary>
    public double WorkY { get; set; }
    
    /// <summary>Work position Z coordinate</summary>
    public double WorkZ { get; set; }
    
    /// <summary>Work coordinate offset X</summary>
    public double WcoX { get; set; }
    
    /// <summary>Work coordinate offset Y</summary>
    public double WcoY { get; set; }
    
    /// <summary>Work coordinate offset Z</summary>
    public double WcoZ { get; set; }
    
    /// <summary>Current feed rate</summary>
    public double FeedRate { get; set; }
    
    /// <summary>Current spindle speed</summary>
    public double SpindleSpeed { get; set; }
    
    /// <summary>
    /// Parse GRBL status report
    /// Format: <![CDATA[<Idle|MPos:0.000,0.000,0.000|WPos:0.000,0.000,0.000|FS:0,0>]]>
    /// </summary>
    public static GrblStatus? Parse(string statusReport)
    {
        if (string.IsNullOrWhiteSpace(statusReport) || !statusReport.StartsWith("<") || !statusReport.EndsWith(">"))
            return null;
            
        try
        {
            var status = new GrblStatus();
            
            // Remove < and > brackets
            var content = statusReport.Trim('<', '>');
            var parts = content.Split('|');
            
            if (parts.Length == 0)
                return null;
            
            // First part is the state
            status.State = parts[0];
            
            // Parse remaining parts
            foreach (var part in parts.Skip(1))
            {
                if (part.StartsWith("MPos:"))
                {
                    var coords = part.Substring(5).Split(',');
                    if (coords.Length >= 2)
                    {
                        double.TryParse(coords[0], System.Globalization.NumberStyles.Float, 
                            System.Globalization.CultureInfo.InvariantCulture, out double x);
                        double.TryParse(coords[1], System.Globalization.NumberStyles.Float, 
                            System.Globalization.CultureInfo.InvariantCulture, out double y);
                        status.MachineX = x;
                        status.MachineY = y;
                        
                        if (coords.Length >= 3)
                        {
                            double.TryParse(coords[2], System.Globalization.NumberStyles.Float, 
                                System.Globalization.CultureInfo.InvariantCulture, out double z);
                            status.MachineZ = z;
                        }
                    }
                }
                else if (part.StartsWith("WPos:"))
                {
                    var coords = part.Substring(5).Split(',');
                    if (coords.Length >= 2)
                    {
                        double.TryParse(coords[0], System.Globalization.NumberStyles.Float, 
                            System.Globalization.CultureInfo.InvariantCulture, out double x);
                        double.TryParse(coords[1], System.Globalization.NumberStyles.Float, 
                            System.Globalization.CultureInfo.InvariantCulture, out double y);
                        status.WorkX = x;
                        status.WorkY = y;
                        
                        if (coords.Length >= 3)
                        {
                            double.TryParse(coords[2], System.Globalization.NumberStyles.Float, 
                                System.Globalization.CultureInfo.InvariantCulture, out double z);
                            status.WorkZ = z;
                        }
                    }
                }
                else if (part.StartsWith("FS:"))
                {
                    var values = part.Substring(3).Split(',');
                    if (values.Length >= 2)
                    {
                        double.TryParse(values[0], System.Globalization.NumberStyles.Float, 
                            System.Globalization.CultureInfo.InvariantCulture, out double feed);
                        double.TryParse(values[1], System.Globalization.NumberStyles.Float, 
                            System.Globalization.CultureInfo.InvariantCulture, out double spindle);
                        status.FeedRate = feed;
                        status.SpindleSpeed = spindle;
                    }
                }
                else if (part.StartsWith("WCO:"))
                {
                    var coords = part.Substring(4).Split(',');
                    if (coords.Length >= 2)
                    {
                        double.TryParse(coords[0], System.Globalization.NumberStyles.Float, 
                            System.Globalization.CultureInfo.InvariantCulture, out double x);
                        double.TryParse(coords[1], System.Globalization.NumberStyles.Float, 
                            System.Globalization.CultureInfo.InvariantCulture, out double y);
                        status.WcoX = x;
                        status.WcoY = y;
                        
                        if (coords.Length >= 3)
                        {
                            double.TryParse(coords[2], System.Globalization.NumberStyles.Float, 
                                System.Globalization.CultureInfo.InvariantCulture, out double z);
                            status.WcoZ = z;
                        }
                    }
                }
            }
            
            // If WCO was provided, calculate WPos from MPos - WCO (GRBL sends either WPos OR WCO, not both)
            if (status.WcoX != 0 || status.WcoY != 0 || status.WcoZ != 0)
            {
                status.WorkX = status.MachineX - status.WcoX;
                status.WorkY = status.MachineY - status.WcoY;
                status.WorkZ = status.MachineZ - status.WcoZ;
            }
            
            return status;
        }
        catch
        {
            return null;
        }
    }
}
