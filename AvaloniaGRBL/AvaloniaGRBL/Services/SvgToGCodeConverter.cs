using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text;
using Svg;
using Svg.Pathing;

namespace AvaloniaGRBL.Services;

/// <summary>
/// Converts SVG files to G-Code for laser engraving/cutting
/// </summary>
public class SvgToGCodeConverter
{
    private readonly StringBuilder _gcode;
    private double _currentX;
    private double _currentY;
    private double _svgHeight; // Track SVG height for Y-axis inversion
    
    // G-Code generation settings
    public double FeedRate { get; set; } = 1000; // mm/min
    public double TravelSpeed { get; set; } = 3000; // mm/min
    public int LaserPowerOn { get; set; } = 1000; // S value for laser on
    public int LaserPowerOff { get; set; } = 0; // S value for laser off
    public double Scale { get; set; } = 1.0; // Scaling factor
    
    public SvgToGCodeConverter()
    {
        _gcode = new StringBuilder();
        _currentX = 0;
        _currentY = 0;
    }
    
    /// <summary>
    /// Converts an SVG file to G-Code
    /// </summary>
    public string ConvertFile(string svgFilePath)
    {
        var svgContent = File.ReadAllText(svgFilePath);
        var svgDocument = SvgDocument.FromSvg<SvgDocument>(svgContent);
        
        return ConvertDocument(svgDocument);
    }
    
    /// <summary>
    /// Converts an SVG document to G-Code
    /// </summary>
    public string ConvertDocument(SvgDocument document)
    {
        _gcode.Clear();
        _currentX = 0;
        _currentY = 0;
        
        // Get SVG viewbox/bounds for Y-axis inversion
        var bounds = document.Bounds;
        _svgHeight = bounds.Height;
        
        // Add header
        AddHeader();
        
        // Process SVG elements
        ProcessElement(document);
        
        // Add footer
        AddFooter();
        
        return _gcode.ToString();
    }
    
    private void AddHeader()
    {
        _gcode.AppendLine("(SVG to G-Code conversion)");
        _gcode.AppendLine("G21 ; Set units to millimeters");
        _gcode.AppendLine("G90 ; Absolute positioning");
        _gcode.AppendLine($"M3 S{LaserPowerOff} ; Laser off");
        _gcode.AppendLine("G0 X0 Y0 ; Move to origin");
        _gcode.AppendLine();
    }
    
    private void AddFooter()
    {
        _gcode.AppendLine();
        _gcode.AppendLine($"M5 ; Laser off");
        _gcode.AppendLine("G0 X0 Y0 ; Return to origin");
        _gcode.AppendLine("M2 ; Program end");
    }
    
    /// <summary>
    /// Transform coordinates from SVG space to G-Code space
    /// Handles Y-axis inversion (SVG Y goes down, G-Code Y goes up)
    /// </summary>
    private (double x, double y) TransformPoint(double x, double y)
    {
        return (x * Scale, (_svgHeight - y) * Scale);
    }
    
    private void ProcessElement(SvgElement element)
    {
        if (element == null)
            return;
            
        // Process paths
        if (element is SvgPath path)
        {
            ProcessPath(path);
        }
        // Process lines
        else if (element is SvgLine line)
        {
            ProcessLine(line);
        }
        // Process rectangles
        else if (element is SvgRectangle rect)
        {
            ProcessRectangle(rect);
        }
        // Process circles
        else if (element is SvgCircle circle)
        {
            ProcessCircle(circle);
        }
        // Process ellipses
        else if (element is SvgEllipse ellipse)
        {
            ProcessEllipse(ellipse);
        }
        // Process polylines
        else if (element is SvgPolyline polyline)
        {
            ProcessPolyline(polyline);
        }
        // Process polygons
        else if (element is SvgPolygon polygon)
        {
            ProcessPolygon(polygon);
        }
        
        // Recursively process child elements
        if (element.Children != null)
        {
            foreach (var child in element.Children)
            {
                ProcessElement(child);
            }
        }
    }
    
    private void ProcessPath(SvgPath path)
    {
        if (path.PathData == null)
            return;
        
        try
        {
            // Get the GraphicsPath with transforms applied
            var graphicsPath = path.Path(null);
            if (graphicsPath == null || graphicsPath.PointCount == 0)
                return;
            
            // Apply element's transforms
            if (path.Transforms != null && path.Transforms.Count > 0)
            {
                var matrix = path.Transforms.GetMatrix();
                graphicsPath.Transform(matrix);
            }
            
            // Flatten the path (converts all curves to line segments)
            graphicsPath.Flatten();
            
            // Extract flattened points and types
            var points = graphicsPath.PathPoints;
            var types = graphicsPath.PathTypes;
            
            bool penDown = false;
            
            for (int i = 0; i < points.Length; i++)
            {
                var pointType = (System.Drawing.Drawing2D.PathPointType)(types[i] & 0x07);
                
                if (pointType == System.Drawing.Drawing2D.PathPointType.Start)
                {
                    if (penDown)
                    {
                        LaserOff();
                        penDown = false;
                    }
                    var (x, y) = TransformPoint(points[i].X, points[i].Y);
                    MoveTo(x, y);
                }
                else // Line or Bezier point (already flattened to lines)
                {
                    if (!penDown)
                    {
                        LaserOn();
                        penDown = true;
                    }
                    var (x, y) = TransformPoint(points[i].X, points[i].Y);
                    LineTo(x, y);
                }
                
                // Check if this closes a subpath
                if ((types[i] & (byte)System.Drawing.Drawing2D.PathPointType.CloseSubpath) != 0)
                {
                    // Subpath closed, but continue with pen state
                }
            }
            
            if (penDown)
            {
                LaserOff();
            }
        }
        catch (Exception ex)
        {
            // Log error but continue processing other elements
            Console.WriteLine($"Error processing path: {ex.Message}");
        }
    }
    
    private void ProcessLine(SvgLine line)
    {
        var (x1, y1) = TransformPoint(line.StartX.Value, line.StartY.Value);
        var (x2, y2) = TransformPoint(line.EndX.Value, line.EndY.Value);
        MoveTo(x1, y1);
        LaserOn();
        LineTo(x2, y2);
        LaserOff();
    }
    
    private void ProcessRectangle(SvgRectangle rect)
    {
        double x = rect.X.Value;
        double y = rect.Y.Value;
        double w = rect.Width.Value;
        double h = rect.Height.Value;
        
        var (x1, y1) = TransformPoint(x, y);
        var (x2, y2) = TransformPoint(x + w, y);
        var (x3, y3) = TransformPoint(x + w, y + h);
        var (x4, y4) = TransformPoint(x, y + h);
        
        MoveTo(x1, y1);
        LaserOn();
        LineTo(x2, y2);
        LineTo(x3, y3);
        LineTo(x4, y4);
        LineTo(x1, y1);
        LaserOff();
    }
    
    private void ProcessCircle(SvgCircle circle)
    {
        double cx = circle.CenterX.Value;
        double cy = circle.CenterY.Value;
        double r = circle.Radius.Value;
        
        // Approximate circle with line segments
        int segments = 64;
        for (int i = 0; i <= segments; i++)
        {
            double angle = 2 * Math.PI * i / segments;
            double svgX = cx + r * Math.Cos(angle);
            double svgY = cy + r * Math.Sin(angle);
            var (x, y) = TransformPoint(svgX, svgY);
            
            if (i == 0)
            {
                MoveTo(x, y);
                LaserOn();
            }
            else
            {
                LineTo(x, y);
            }
        }
        LaserOff();
    }
    
    private void ProcessEllipse(SvgEllipse ellipse)
    {
        double cx = ellipse.CenterX.Value;
        double cy = ellipse.CenterY.Value;
        double rx = ellipse.RadiusX.Value;
        double ry = ellipse.RadiusY.Value;
        
        // Approximate ellipse with line segments
        int segments = 64;
        for (int i = 0; i <= segments; i++)
        {
            double angle = 2 * Math.PI * i / segments;
            double svgX = cx + rx * Math.Cos(angle);
            double svgY = cy + ry * Math.Sin(angle);
            var (x, y) = TransformPoint(svgX, svgY);
            
            if (i == 0)
            {
                MoveTo(x, y);
                LaserOn();
            }
            else
            {
                LineTo(x, y);
            }
        }
        LaserOff();
    }
    
    private void ProcessPolyline(SvgPolyline polyline)
    {
        if (polyline.Points == null || polyline.Points.Count < 2)
            return;
            
        for (int i = 0; i < polyline.Points.Count; i += 2)
        {
            if (i + 1 >= polyline.Points.Count)
                break;
                
            var (x, y) = TransformPoint(polyline.Points[i], polyline.Points[i + 1]);
            
            if (i == 0)
            {
                MoveTo(x, y);
                LaserOn();
            }
            else
            {
                LineTo(x, y);
            }
        }
        LaserOff();
    }
    
    private void ProcessPolygon(SvgPolygon polygon)
    {
        if (polygon.Points == null || polygon.Points.Count < 2)
            return;
            
        double firstX = 0, firstY = 0;
        for (int i = 0; i < polygon.Points.Count; i += 2)
        {
            if (i + 1 >= polygon.Points.Count)
                break;
                
            var (x, y) = TransformPoint(polygon.Points[i], polygon.Points[i + 1]);
            
            if (i == 0)
            {
                firstX = x;
                firstY = y;
                MoveTo(x, y);
                LaserOn();
            }
            else
            {
                LineTo(x, y);
            }
        }
        
        // Close the polygon
        LineTo(firstX, firstY);
        LaserOff();
    }
    
    private List<(double, double)> ApproximateCubicBezier(
        double x0, double y0, double x1, double y1,
        double x2, double y2, double x3, double y3, int segments)
    {
        var points = new List<(double, double)>();
        
        for (int i = 1; i <= segments; i++)
        {
            double t = (double)i / segments;
            double t2 = t * t;
            double t3 = t2 * t;
            double mt = 1 - t;
            double mt2 = mt * mt;
            double mt3 = mt2 * mt;
            
            double x = mt3 * x0 + 3 * mt2 * t * x1 + 3 * mt * t2 * x2 + t3 * x3;
            double y = mt3 * y0 + 3 * mt2 * t * y1 + 3 * mt * t2 * y2 + t3 * y3;
            
            points.Add((x, y));
        }
        
        return points;
    }
    
    private List<(double, double)> ApproximateQuadraticBezier(
        double x0, double y0, double x1, double y1,
        double x2, double y2, int segments)
    {
        var points = new List<(double, double)>();
        
        for (int i = 1; i <= segments; i++)
        {
            double t = (double)i / segments;
            double t2 = t * t;
            double mt = 1 - t;
            double mt2 = mt * mt;
            
            double x = mt2 * x0 + 2 * mt * t * x1 + t2 * x2;
            double y = mt2 * y0 + 2 * mt * t * y1 + t2 * y2;
            
            points.Add((x, y));
        }
        
        return points;
    }
    
    private void MoveTo(double x, double y)
    {
        _gcode.AppendLine($"G0 X{FormatNumber(x)} Y{FormatNumber(y)} F{TravelSpeed}");
        _currentX = x;
        _currentY = y;
    }
    
    private void LineTo(double x, double y)
    {
        _gcode.AppendLine($"G1 X{FormatNumber(x)} Y{FormatNumber(y)} F{FeedRate}");
        _currentX = x;
        _currentY = y;
    }
    
    private void LaserOn()
    {
        _gcode.AppendLine($"M3 S{LaserPowerOn}");
    }
    
    private void LaserOff()
    {
        _gcode.AppendLine($"M3 S{LaserPowerOff}");
    }
    
    private string FormatNumber(double value)
    {
        return value.ToString("F3", CultureInfo.InvariantCulture);
    }
}
