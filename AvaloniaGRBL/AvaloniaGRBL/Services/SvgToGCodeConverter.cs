using System;
using System.Collections.Generic;
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
            
        var pathData = path.PathData;
        bool penDown = false;
        
        foreach (var segment in pathData)
        {
            if (segment is SvgMoveToSegment moveTo)
            {
                if (penDown)
                {
                    LaserOff();
                    penDown = false;
                }
                MoveTo(moveTo.End.X * Scale, moveTo.End.Y * Scale);
            }
            else if (segment is SvgLineSegment lineTo)
            {
                if (!penDown)
                {
                    LaserOn();
                    penDown = true;
                }
                LineTo(lineTo.End.X * Scale, lineTo.End.Y * Scale);
            }
            else if (segment is SvgCubicCurveSegment cubic)
            {
                if (!penDown)
                {
                    LaserOn();
                    penDown = true;
                }
                // Approximate cubic bezier with line segments
                var points = ApproximateCubicBezier(
                    _currentX, _currentY,
                    cubic.FirstControlPoint.X * Scale, cubic.FirstControlPoint.Y * Scale,
                    cubic.SecondControlPoint.X * Scale, cubic.SecondControlPoint.Y * Scale,
                    cubic.End.X * Scale, cubic.End.Y * Scale,
                    20);
                    
                foreach (var point in points)
                {
                    LineTo(point.Item1, point.Item2);
                }
            }
            else if (segment is SvgQuadraticCurveSegment quad)
            {
                if (!penDown)
                {
                    LaserOn();
                    penDown = true;
                }
                // Approximate quadratic bezier with line segments
                var points = ApproximateQuadraticBezier(
                    _currentX, _currentY,
                    quad.ControlPoint.X * Scale, quad.ControlPoint.Y * Scale,
                    quad.End.X * Scale, quad.End.Y * Scale,
                    20);
                    
                foreach (var point in points)
                {
                    LineTo(point.Item1, point.Item2);
                }
            }
            else if (segment is SvgClosePathSegment)
            {
                // Path will be closed automatically by returning to start
            }
        }
        
        if (penDown)
        {
            LaserOff();
        }
    }
    
    private void ProcessLine(SvgLine line)
    {
        MoveTo(line.StartX.Value * Scale, line.StartY.Value * Scale);
        LaserOn();
        LineTo(line.EndX.Value * Scale, line.EndY.Value * Scale);
        LaserOff();
    }
    
    private void ProcessRectangle(SvgRectangle rect)
    {
        double x = rect.X.Value * Scale;
        double y = rect.Y.Value * Scale;
        double w = rect.Width.Value * Scale;
        double h = rect.Height.Value * Scale;
        
        MoveTo(x, y);
        LaserOn();
        LineTo(x + w, y);
        LineTo(x + w, y + h);
        LineTo(x, y + h);
        LineTo(x, y);
        LaserOff();
    }
    
    private void ProcessCircle(SvgCircle circle)
    {
        double cx = circle.CenterX.Value * Scale;
        double cy = circle.CenterY.Value * Scale;
        double r = circle.Radius.Value * Scale;
        
        // Approximate circle with line segments
        int segments = 64;
        for (int i = 0; i <= segments; i++)
        {
            double angle = 2 * Math.PI * i / segments;
            double x = cx + r * Math.Cos(angle);
            double y = cy + r * Math.Sin(angle);
            
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
        double cx = ellipse.CenterX.Value * Scale;
        double cy = ellipse.CenterY.Value * Scale;
        double rx = ellipse.RadiusX.Value * Scale;
        double ry = ellipse.RadiusY.Value * Scale;
        
        // Approximate ellipse with line segments
        int segments = 64;
        for (int i = 0; i <= segments; i++)
        {
            double angle = 2 * Math.PI * i / segments;
            double x = cx + rx * Math.Cos(angle);
            double y = cy + ry * Math.Sin(angle);
            
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
                
            double x = polyline.Points[i] * Scale;
            double y = polyline.Points[i + 1] * Scale;
            
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
                
            double x = polygon.Points[i] * Scale;
            double y = polygon.Points[i + 1] * Scale;
            
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
