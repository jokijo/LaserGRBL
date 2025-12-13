using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaGRBL.Models;

namespace AvaloniaGRBL.Services;

/// <summary>
/// Renders G-Code commands onto an Avalonia Canvas
/// </summary>
public class GCodeRenderer
{
    private readonly Canvas _canvas;
    private GCodeFile? _currentFile;
    private double _scale = 1.0;
    private double _offsetX = 0;
    private double _offsetY = 0;
    
    // Colors for rendering
    private readonly IBrush _rapidMoveBrush = new SolidColorBrush(Colors.LightBlue);
    private readonly IBrush _laserOnBrush = new SolidColorBrush(Colors.Red);
    private readonly IBrush _laserOffBrush = new SolidColorBrush(Colors.LightGray);
    private readonly IBrush _gridBrush = new SolidColorBrush(Color.FromArgb(50, 200, 200, 200));
    
    private const double LineThickness = 1.0;
    private const double RapidMoveThickness = 0.5;
    
    public GCodeRenderer(Canvas canvas)
    {
        _canvas = canvas;
    }
    
    public void RenderFile(GCodeFile file)
    {
        _currentFile = file;
        _canvas.Children.Clear();
        
        if (file == null || file.IsEmpty || !file.Bounds.IsValid)
        {
            RenderEmptyState();
            return;
        }
        
        CalculateScaleAndOffset();
        RenderGrid();
        RenderCommands();
    }
    
    private void RenderEmptyState()
    {
        var text = new TextBlock
        {
            Text = "No G-Code loaded",
            FontSize = 24,
            Foreground = new SolidColorBrush(Color.FromArgb(128, 150, 150, 150)),
            [Canvas.LeftProperty] = 20.0,
            [Canvas.TopProperty] = 20.0
        };
        _canvas.Children.Add(text);
    }
    
    private void CalculateScaleAndOffset()
    {
        if (_currentFile == null || !_currentFile.Bounds.IsValid)
            return;
        
        var bounds = _currentFile.Bounds;
        var canvasWidth = _canvas.Bounds.Width > 0 ? _canvas.Bounds.Width : 800;
        var canvasHeight = _canvas.Bounds.Height > 0 ? _canvas.Bounds.Height : 600;
        
        // Add margins
        var margin = 40.0;
        var availableWidth = canvasWidth - 2 * margin;
        var availableHeight = canvasHeight - 2 * margin;
        
        // Calculate scale to fit the drawing
        var scaleX = bounds.Width > 0 ? availableWidth / bounds.Width : 1.0;
        var scaleY = bounds.Height > 0 ? availableHeight / bounds.Height : 1.0;
        
        // Use the smaller scale to maintain aspect ratio
        _scale = Math.Min(scaleX, scaleY) * 0.95;
        
        // Calculate offset to center the drawing
        var scaledWidth = bounds.Width * _scale;
        var scaledHeight = bounds.Height * _scale;
        
        _offsetX = margin + (availableWidth - scaledWidth) / 2 - bounds.MinX * _scale;
        _offsetY = margin + (availableHeight - scaledHeight) / 2 - bounds.MinY * _scale;
    }
    
    private Point TransformPoint(double x, double y)
    {
        // Transform from G-Code coordinates to canvas coordinates
        // Note: Y-axis is inverted in screen coordinates
        var canvasHeight = _canvas.Bounds.Height > 0 ? _canvas.Bounds.Height : 600;
        return new Point(
            x * _scale + _offsetX,
            canvasHeight - (y * _scale + _offsetY)
        );
    }
    
    private void RenderGrid()
    {
        if (_currentFile == null || !_currentFile.Bounds.IsValid)
            return;
        
        var bounds = _currentFile.Bounds;
        var gridSize = CalculateGridSize();
        
        // Draw vertical grid lines
        for (double x = Math.Floor(bounds.MinX / gridSize) * gridSize; x <= bounds.MaxX; x += gridSize)
        {
            var p1 = TransformPoint(x, bounds.MinY);
            var p2 = TransformPoint(x, bounds.MaxY);
            
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = p1,
                EndPoint = p2,
                Stroke = _gridBrush,
                StrokeThickness = 1
            };
            _canvas.Children.Add(line);
        }
        
        // Draw horizontal grid lines
        for (double y = Math.Floor(bounds.MinY / gridSize) * gridSize; y <= bounds.MaxY; y += gridSize)
        {
            var p1 = TransformPoint(bounds.MinX, y);
            var p2 = TransformPoint(bounds.MaxX, y);
            
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = p1,
                EndPoint = p2,
                Stroke = _gridBrush,
                StrokeThickness = 1
            };
            _canvas.Children.Add(line);
        }
    }
    
    private double CalculateGridSize()
    {
        if (_currentFile == null)
            return 10.0;
        
        var maxDimension = Math.Max(_currentFile.Bounds.Width, _currentFile.Bounds.Height);
        
        if (maxDimension < 50) return 5.0;
        if (maxDimension < 100) return 10.0;
        if (maxDimension < 500) return 50.0;
        return 100.0;
    }
    
    private void RenderCommands()
    {
        if (_currentFile == null)
            return;
        
        // NOTE: This assumes absolute positioning mode (G90)
        // Relative positioning mode (G91) is not currently supported
        double currentX = 0, currentY = 0;
        bool laserOn = false;
        
        foreach (var cmd in _currentFile.Commands)
        {
            // Track laser state
            if (cmd.CommandType == GCodeCommandType.SpindleOn)
            {
                laserOn = true;
                continue;
            }
            else if (cmd.CommandType == GCodeCommandType.SpindleOff)
            {
                laserOn = false;
                continue;
            }
            
            // Handle movement commands
            if (cmd.CommandType == GCodeCommandType.RapidMove || 
                cmd.CommandType == GCodeCommandType.LinearMove)
            {
                var newX = cmd.HasParameter('X') ? cmd.GetParameter('X') : currentX;
                var newY = cmd.HasParameter('Y') ? cmd.GetParameter('Y') : currentY;
                
                // Only draw if there's actual movement
                if (newX != currentX || newY != currentY)
                {
                    var p1 = TransformPoint(currentX, currentY);
                    var p2 = TransformPoint(newX, newY);
                    
                    IBrush brush;
                    double thickness;
                    
                    if (cmd.CommandType == GCodeCommandType.RapidMove)
                    {
                        brush = _rapidMoveBrush;
                        thickness = RapidMoveThickness;
                    }
                    else
                    {
                        brush = laserOn ? _laserOnBrush : _laserOffBrush;
                        thickness = LineThickness;
                    }
                    
                    var line = new Avalonia.Controls.Shapes.Line
                    {
                        StartPoint = p1,
                        EndPoint = p2,
                        Stroke = brush,
                        StrokeThickness = thickness
                    };
                    
                    _canvas.Children.Add(line);
                }
                
                currentX = newX;
                currentY = newY;
            }
            else if (cmd.CommandType == GCodeCommandType.ArcCW || 
                     cmd.CommandType == GCodeCommandType.ArcCCW)
            {
                // NOTE: Simplified arc rendering - arcs are approximated with straight lines
                // This provides a basic visualization but reduces accuracy for curved toolpaths
                // TODO: Implement proper arc rendering by calculating arc center from I/J parameters
                // and drawing multiple line segments to approximate the curve
                var newX = cmd.HasParameter('X') ? cmd.GetParameter('X') : currentX;
                var newY = cmd.HasParameter('Y') ? cmd.GetParameter('Y') : currentY;
                
                if (newX != currentX || newY != currentY)
                {
                    var p1 = TransformPoint(currentX, currentY);
                    var p2 = TransformPoint(newX, newY);
                    
                    var brush = laserOn ? _laserOnBrush : _laserOffBrush;
                    
                    var line = new Avalonia.Controls.Shapes.Line
                    {
                        StartPoint = p1,
                        EndPoint = p2,
                        Stroke = brush,
                        StrokeThickness = LineThickness
                    };
                    
                    _canvas.Children.Add(line);
                }
                
                currentX = newX;
                currentY = newY;
            }
        }
    }
    
    public void Clear()
    {
        _canvas.Children.Clear();
        _currentFile = null;
    }
}
