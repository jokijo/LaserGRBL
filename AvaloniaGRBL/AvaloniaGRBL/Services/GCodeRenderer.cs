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
    private readonly Canvas _contentCanvas; // Inner canvas for content that gets transformed
    private GCodeFile? _currentFile;
    private double _scale = 1.0;
    private double _offsetX = 0;
    private double _offsetY = 0;
    private double _userZoom = 1.0; // User-controlled zoom multiplier
    private double _baseScale = 1.0; // Auto-calculated base scale for fit
    private ScaleTransform? _zoomTransform;
    private TranslateTransform? _panTransform;
    private TransformGroup? _transformGroup;
    
    // Laser position indicator
    private double _laserX = 0;
    private double _laserY = 0;
    private Canvas? _laserCross; // Canvas containing the laser cross indicator
    
    // Cross cursor feature
    private bool _showCrossCursor = false;
    private Avalonia.Controls.Shapes.Line? _crossHorizontalLine;
    private Avalonia.Controls.Shapes.Line? _crossVerticalLine;
    private Avalonia.Controls.Shapes.Rectangle? _crossCursorSquare;
    
    // Display options
    private bool _showLaserOffMovements = false;
    private bool _showBoundingBox = false;
    
    // Panning state
    private bool _isPanning = false;
    private Point _panStartPoint;
    private Point _panStartOffset;
    
    // Colors for rendering
    private readonly IBrush _rapidMoveBrush = new SolidColorBrush(Colors.LightBlue);
    private readonly IBrush _laserOnBrush = new SolidColorBrush(Colors.Red);
    private readonly IBrush _laserOffBrush = new SolidColorBrush(Colors.LightGray);
    private readonly IBrush _gridBrush = new SolidColorBrush(Color.FromArgb(50, 200, 200, 200));
    private readonly IBrush _subGridBrush = new SolidColorBrush(Color.FromArgb(25, 150, 150, 150));
    private readonly IBrush _axisBrush = new SolidColorBrush(Color.FromArgb(128, 200, 200, 200));
    private readonly IBrush _axisTextBrush = new SolidColorBrush(Color.FromArgb(200, 200, 200, 200));
    private readonly IBrush _laserIndicatorBrush = new SolidColorBrush(Colors.Yellow);
    
    private const double LineThickness = 1.0;
    private const double RapidMoveThickness = 0.5;
    private const double ZoomIncrement = 1.1; // 10% zoom increment
    
    public bool ShowCrossCursor
    {
        get => _showCrossCursor;
        set
        {
            _showCrossCursor = value;
            if (_crossHorizontalLine != null) _crossHorizontalLine.IsVisible = value;
            if (_crossVerticalLine != null) _crossVerticalLine.IsVisible = value;
            if (_crossCursorSquare != null) _crossCursorSquare.IsVisible = value;
            
            // Hide or show the default mouse cursor
            _canvas.Cursor = value ? new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.None) 
                                   : new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }
    }
    
    public bool ShowLaserOffMovements
    {
        get => _showLaserOffMovements;
        set
        {
            if (_showLaserOffMovements != value)
            {
                _showLaserOffMovements = value;
                // Re-render to update visibility of laser-off movements
                if (_currentFile != null)
                {
                    RefreshRendering();
                }
            }
        }
    }
    
    public bool ShowBoundingBox
    {
        get => _showBoundingBox;
        set
        {
            if (_showBoundingBox != value)
            {
                _showBoundingBox = value;
                // Re-render to update bounding box visibility
                if (_currentFile != null)
                {
                    RefreshRendering();
                }
            }
        }
    }
    
    public GCodeRenderer(Canvas canvas)
    {
        _canvas = canvas;
        
        // Create inner canvas for content that will be transformed
        _contentCanvas = new Canvas();
        _canvas.Children.Add(_contentCanvas);
        
        // Set up transform group for zoom and pan
        // Order matters: zoom first, then pan (so panning isn't affected by zoom level)
        _panTransform = new TranslateTransform(0, 0);
        _zoomTransform = new ScaleTransform(1.0, 1.0);
        _transformGroup = new TransformGroup();
        _transformGroup.Children.Add(_zoomTransform);
        _transformGroup.Children.Add(_panTransform);
        _contentCanvas.RenderTransform = _transformGroup;
        
        // Set up mouse event handlers for panning
        _canvas.PointerPressed += OnCanvasPointerPressed;
        _canvas.PointerMoved += OnCanvasPointerMoved;
        _canvas.PointerReleased += OnCanvasPointerReleased;
        _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
        
        // Initialize cross cursor lines (on top canvas, not content canvas)
        var crossCursorBrush = new SolidColorBrush(Color.FromArgb(180, 103, 107, 117));
        _crossHorizontalLine = new Avalonia.Controls.Shapes.Line
        {
            Stroke = crossCursorBrush,
            StrokeThickness = 1,
            IsVisible = false,
            ZIndex = 1000
        };
        _crossVerticalLine = new Avalonia.Controls.Shapes.Line
        {
            Stroke = crossCursorBrush,
            StrokeThickness = 1,
            IsVisible = false,
            ZIndex = 1000
        };
        _crossCursorSquare = new Avalonia.Controls.Shapes.Rectangle
        {
            Width = 10,
            Height = 10,
            Stroke = crossCursorBrush,
            StrokeThickness = 1,
            Fill = Brushes.Transparent,
            IsVisible = false,
            ZIndex = 1001
        };
        _canvas.Children.Add(_crossHorizontalLine);
        _canvas.Children.Add(_crossVerticalLine);
        _canvas.Children.Add(_crossCursorSquare);
    }
    
    public void RenderFile(GCodeFile file)
    {
        _currentFile = file;
        ClearAllCanvasElements();
        
        if (file == null || file.IsEmpty || !file.Bounds.IsValid)
        {
            RenderEmptyState();
            return;
        }
        
        // Reset user zoom when loading new file
        _userZoom = 1.0;
        UpdateZoomTransform();
        
        CalculateScaleAndOffset();
        RenderGrid();
        RenderCommands();
        
        // Render bounding box if enabled
        if (_showBoundingBox)
        {
            RenderBoundingBox();
        }
    }
    
    /// <summary>
    /// Re-render the current file without resetting zoom/pan (for toggling display options)
    /// </summary>
    private void RefreshRendering()
    {
        if (_currentFile == null)
            return;
        
        // Clear content canvas (preserve zoom/pan state)
        _contentCanvas.Children.Clear();
        
        // Clear main canvas overlay elements (but keep cross cursor and content canvas)
        var itemsToRemove = new List<Control>();
        foreach (var child in _canvas.Children)
        {
            if (child != _contentCanvas && child != _crossHorizontalLine && child != _crossVerticalLine && child != _crossCursorSquare)
            {
                itemsToRemove.Add(child);
            }
        }
        foreach (var item in itemsToRemove)
        {
            _canvas.Children.Remove(item);
        }
        
        // Re-render with current settings
        RenderGrid();
        RenderCommands();
        
        // Render bounding box if enabled
        if (_showBoundingBox)
        {
            RenderBoundingBox();
        }
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
        _contentCanvas.Children.Add(text);
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
        _baseScale = Math.Min(scaleX, scaleY) * 0.95;
        _scale = _baseScale; // Don't apply user zoom here - it's handled by transform
        
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
        var subGridSize = gridSize / 10.0; // Subgrid for millimeters
        
        // Extend grid beyond image bounds by 20%
        var extendX = bounds.Width * 0.2;
        var extendY = bounds.Height * 0.2;
        var minX = bounds.MinX - extendX;
        var maxX = bounds.MaxX + extendX;
        var minY = bounds.MinY - extendY;
        var maxY = bounds.MaxY + extendY;
        
        // Draw subgrid (finer lines)
        for (double x = Math.Floor(minX / subGridSize) * subGridSize; x <= maxX; x += subGridSize)
        {
            var p1 = TransformPoint(x, minY);
            var p2 = TransformPoint(x, maxY);
            
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = p1,
                EndPoint = p2,
                Stroke = _subGridBrush,
                StrokeThickness = 0.5
            };
            _contentCanvas.Children.Add(line);
        }
        
        for (double y = Math.Floor(minY / subGridSize) * subGridSize; y <= maxY; y += subGridSize)
        {
            var p1 = TransformPoint(minX, y);
            var p2 = TransformPoint(maxX, y);
            
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = p1,
                EndPoint = p2,
                Stroke = _subGridBrush,
                StrokeThickness = 0.5
            };
            _contentCanvas.Children.Add(line);
        }
        
        // Draw major grid lines
        for (double x = Math.Floor(minX / gridSize) * gridSize; x <= maxX; x += gridSize)
        {
            var p1 = TransformPoint(x, minY);
            var p2 = TransformPoint(x, maxY);
            
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = p1,
                EndPoint = p2,
                Stroke = _gridBrush,
                StrokeThickness = 1
            };
            _contentCanvas.Children.Add(line);
        }
        
        for (double y = Math.Floor(minY / gridSize) * gridSize; y <= maxY; y += gridSize)
        {
            var p1 = TransformPoint(minX, y);
            var p2 = TransformPoint(maxX, y);
            
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = p1,
                EndPoint = p2,
                Stroke = _gridBrush,
                StrokeThickness = 1
            };
            _contentCanvas.Children.Add(line);
        }
        
        // Draw axis lines (X=0 and Y=0)
        RenderAxisLines(minX, maxX, minY, maxY);
        
        // Draw axis labels and ticks
        RenderAxisLabels(minX, maxX, minY, maxY, gridSize);
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
    
    private void RenderAxisLines(double minX, double maxX, double minY, double maxY)
    {
        // Draw X=0 axis line
        if (minX <= 0 && maxX >= 0)
        {
            var p1 = TransformPoint(0, minY);
            var p2 = TransformPoint(0, maxY);
            
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = p1,
                EndPoint = p2,
                Stroke = _axisBrush,
                StrokeThickness = 1.5
            };
            _contentCanvas.Children.Add(line);
        }
        
        // Draw Y=0 axis line
        if (minY <= 0 && maxY >= 0)
        {
            var p1 = TransformPoint(minX, 0);
            var p2 = TransformPoint(maxX, 0);
            
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = p1,
                EndPoint = p2,
                Stroke = _axisBrush,
                StrokeThickness = 1.5
            };
            _contentCanvas.Children.Add(line);
        }
    }
    
    private void RenderAxisLabels(double minX, double maxX, double minY, double maxY, double gridSize)
    {
        const double tickSize = 5;
        
        // X-axis labels (along bottom)
        for (double x = Math.Floor(minX / gridSize) * gridSize; x <= maxX; x += gridSize)
        {
            var pos = TransformPoint(x, minY);
            
            // Add tick mark on main canvas (fixed size)
            var screenPos = new Point(
                pos.X * _userZoom + (_panTransform?.X ?? 0),
                pos.Y * _userZoom + (_panTransform?.Y ?? 0)
            );
            
            var tick = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Point(screenPos.X, screenPos.Y),
                EndPoint = new Point(screenPos.X, screenPos.Y + tickSize),
                Stroke = _axisTextBrush,
                StrokeThickness = 1
            };
            _canvas.Children.Add(tick);
            
            // Add label
            var label = new TextBlock
            {
                Text = x.ToString("0"),
                FontSize = 10,
                Foreground = _axisTextBrush,
                [Canvas.LeftProperty] = screenPos.X - 10,
                [Canvas.TopProperty] = screenPos.Y + tickSize + 2
            };
            _canvas.Children.Add(label);
        }
        
        // Y-axis labels (along left side)
        for (double y = Math.Floor(minY / gridSize) * gridSize; y <= maxY; y += gridSize)
        {
            var pos = TransformPoint(minX, y);
            
            // Add tick mark on main canvas (fixed size)
            var screenPos = new Point(
                pos.X * _userZoom + (_panTransform?.X ?? 0),
                pos.Y * _userZoom + (_panTransform?.Y ?? 0)
            );
            
            var tick = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Point(screenPos.X, screenPos.Y),
                EndPoint = new Point(screenPos.X - tickSize, screenPos.Y),
                Stroke = _axisTextBrush,
                StrokeThickness = 1
            };
            _canvas.Children.Add(tick);
            
            // Add label
            var label = new TextBlock
            {
                Text = y.ToString("0"),
                FontSize = 10,
                Foreground = _axisTextBrush,
                [Canvas.LeftProperty] = screenPos.X - 25,
                [Canvas.TopProperty] = screenPos.Y - 7
            };
            _canvas.Children.Add(label);
        }
        
        // Add unit label in top-right corner
        var unitLabel = new TextBlock
        {
            Text = "mm",
            FontSize = 12,
            Foreground = _axisTextBrush,
            [Canvas.RightProperty] = 10.0,
            [Canvas.TopProperty] = 10.0
        };
        _canvas.Children.Add(unitLabel);
    }
    
    private void RenderBoundingBox()
    {
        if (_currentFile == null || !_currentFile.Bounds.IsValid)
            return;
        
        var bounds = _currentFile.Bounds;
        
        // Transform bounding box corners
        var topLeft = TransformPoint(bounds.MinX, bounds.MaxY);
        var topRight = TransformPoint(bounds.MaxX, bounds.MaxY);
        var bottomLeft = TransformPoint(bounds.MinX, bounds.MinY);
        var bottomRight = TransformPoint(bounds.MaxX, bounds.MinY);
        
        // Create bounding box rectangle
        var boundingBoxBrush = new SolidColorBrush(Color.FromArgb(255, 255, 165, 0)); // Orange
        
        // Top line
        var topLine = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = topLeft,
            EndPoint = topRight,
            Stroke = boundingBoxBrush,
            StrokeThickness = 2,
            StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 5, 3 }
        };
        _contentCanvas.Children.Add(topLine);
        
        // Right line
        var rightLine = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = topRight,
            EndPoint = bottomRight,
            Stroke = boundingBoxBrush,
            StrokeThickness = 2,
            StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 5, 3 }
        };
        _contentCanvas.Children.Add(rightLine);
        
        // Bottom line
        var bottomLine = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = bottomRight,
            EndPoint = bottomLeft,
            Stroke = boundingBoxBrush,
            StrokeThickness = 2,
            StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 5, 3 }
        };
        _contentCanvas.Children.Add(bottomLine);
        
        // Left line
        var leftLine = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = bottomLeft,
            EndPoint = topLeft,
            Stroke = boundingBoxBrush,
            StrokeThickness = 2,
            StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 5, 3 }
        };
        _contentCanvas.Children.Add(leftLine);
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
                    bool shouldDraw = true;
                    
                    if (cmd.CommandType == GCodeCommandType.RapidMove)
                    {
                        brush = _rapidMoveBrush;
                        thickness = RapidMoveThickness;
                    }
                    else
                    {
                        brush = laserOn ? _laserOnBrush : _laserOffBrush;
                        thickness = LineThickness;
                        
                        // Skip laser-off movements if the option is disabled
                        if (!laserOn && !_showLaserOffMovements)
                        {
                            shouldDraw = false;
                        }
                    }
                    
                    if (shouldDraw)
                    {
                        var line = new Avalonia.Controls.Shapes.Line
                        {
                            StartPoint = p1,
                            EndPoint = p2,
                            Stroke = brush,
                            StrokeThickness = thickness
                        };
                        
                        _contentCanvas.Children.Add(line);
                    }
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
                    // Skip laser-off movements if the option is disabled
                    if (!laserOn && !_showLaserOffMovements)
                    {
                        currentX = newX;
                        currentY = newY;
                        continue;
                    }
                    
                    var p1 = TransformPoint(currentX, currentY);
                    var p2 = TransformPoint(newX, newY);
                    
                    var brush = laserOn ? _laserOnBrush : _laserOffBrush;
                    
                    var line = new Avalonia.Controls.Shapes.Line
                    {
                        StartPoint = p1,
                        EndPoint = p2,
                        Stroke = brush,
                        StrokeThickness = LineThickness,
                        StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 3, 2 } // Dashed to indicate approximation
                    };
                    
                    _contentCanvas.Children.Add(line);
                }
                
                currentX = newX;
                currentY = newY;
            }
        }
    }
    
    public void Clear()
    {
        ClearAllCanvasElements();
        _currentFile = null;
    }
    
    private void ClearAllCanvasElements()
    {
        // Clear content canvas (drawings, grid, etc.)
        _contentCanvas.Children.Clear();
        
        // Clear main canvas elements (axis labels, ticks, laser cross, unit label)
        // Keep the content canvas and cross cursor elements
        var itemsToRemove = new List<Control>();
        foreach (var child in _canvas.Children)
        {
            if (child != _contentCanvas && child != _crossHorizontalLine && child != _crossVerticalLine && child != _crossCursorSquare)
            {
                itemsToRemove.Add(child);
            }
        }
        foreach (var item in itemsToRemove)
        {
            _canvas.Children.Remove(item);
        }
    }
    
    /// <summary>
    /// Zoom in by 10% towards center
    /// </summary>
    public void ZoomIn()
    {
        var centerX = _canvas.Bounds.Width / 2;
        var centerY = _canvas.Bounds.Height / 2;
        ZoomToPoint(ZoomIncrement, new Point(centerX, centerY));
    }
    
    /// <summary>
    /// Zoom out by 10% from center
    /// </summary>
    public void ZoomOut()
    {
        var centerX = _canvas.Bounds.Width / 2;
        var centerY = _canvas.Bounds.Height / 2;
        ZoomToPoint(1.0 / ZoomIncrement, new Point(centerX, centerY));
    }
    
    /// <summary>
    /// Reset zoom to auto-fit the drawing
    /// </summary>
    public void ZoomAuto()
    {
        _userZoom = 1.0;
        if (_panTransform != null)
        {
            _panTransform.X = 0;
            _panTransform.Y = 0;
        }
        UpdateZoomTransform();
        RefreshOverlayElements();
    }
    
    /// <summary>
    /// Zoom by a specific factor towards center (for button support)
    /// </summary>
    public void ZoomBy(double factor)
    {
        var centerX = _canvas.Bounds.Width / 2;
        var centerY = _canvas.Bounds.Height / 2;
        ZoomToPoint(factor, new Point(centerX, centerY));
    }
    
    /// <summary>
    /// Zoom towards a specific point on the canvas
    /// </summary>
    private void ZoomToPoint(double factor, Point canvasPoint)
    {
        if (_zoomTransform == null || _panTransform == null)
            return;
        
        // Get the point in the content canvas coordinates before zoom
        // With transform order: zoom first, then pan
        // Screen point = contentPoint * zoom + pan
        // So: contentPoint = (screen point - pan) / zoom
        var pointBeforeZoom = new Point(
            (canvasPoint.X - _panTransform.X) / _userZoom,
            (canvasPoint.Y - _panTransform.Y) / _userZoom
        );
        
        // Apply zoom
        var oldZoom = _userZoom;
        _userZoom *= factor;
        
        // Clamp zoom to reasonable values
        _userZoom = Math.Max(0.1, Math.Min(_userZoom, 20.0));
        
        // Update zoom transform
        _zoomTransform.ScaleX = _userZoom;
        _zoomTransform.ScaleY = _userZoom;
        
        // Adjust pan to keep the point under the cursor
        // After zoom: pointBeforeZoom * newZoom + newPan = canvasPoint
        // So: newPan = canvasPoint - pointBeforeZoom * newZoom
        _panTransform.X = canvasPoint.X - pointBeforeZoom.X * _userZoom;
        _panTransform.Y = canvasPoint.Y - pointBeforeZoom.Y * _userZoom;
        
        RefreshOverlayElements();
    }
    
    /// <summary>
    /// Update the zoom transform without redrawing
    /// </summary>
    private void UpdateZoomTransform()
    {
        if (_zoomTransform != null)
        {
            _zoomTransform.ScaleX = _userZoom;
            _zoomTransform.ScaleY = _userZoom;
        }
    }
    
    /// <summary>
    /// Handle mouse button press for panning
    /// </summary>
    private void OnCanvasPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(_canvas).Properties;
        if (properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _panStartPoint = e.GetPosition(_canvas);
            if (_panTransform != null)
            {
                _panStartOffset = new Point(_panTransform.X, _panTransform.Y);
            }
            _canvas.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
            e.Handled = true;
        }
    }
    
    /// <summary>
    /// Handle mouse move for panning
    /// </summary>
    private void OnCanvasPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var currentPoint = e.GetPosition(_canvas);
        
        // Update cross cursor position
        if (_showCrossCursor && _crossHorizontalLine != null && _crossVerticalLine != null && _crossCursorSquare != null)
        {
            _crossHorizontalLine.StartPoint = new Point(0, currentPoint.Y);
            _crossHorizontalLine.EndPoint = new Point(_canvas.Bounds.Width, currentPoint.Y);
            _crossVerticalLine.StartPoint = new Point(currentPoint.X, 0);
            _crossVerticalLine.EndPoint = new Point(currentPoint.X, _canvas.Bounds.Height);
            
            // Position square centered on cursor
            Canvas.SetLeft(_crossCursorSquare, currentPoint.X - 5);
            Canvas.SetTop(_crossCursorSquare, currentPoint.Y - 5);
        }
        
        if (_isPanning && _panTransform != null)
        {
            var delta = currentPoint - _panStartPoint;
            
            _panTransform.X = _panStartOffset.X + delta.X;
            _panTransform.Y = _panStartOffset.Y + delta.Y;
            
            RefreshOverlayElements();
            
            e.Handled = true;
        }
    }
    
    /// <summary>
    /// Handle mouse button release
    /// </summary>
    private void OnCanvasPointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            _canvas.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
            e.Handled = true;
        }
    }
    
    /// <summary>
    /// Handle mouse wheel for zoom towards cursor
    /// </summary>
    private void OnCanvasPointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y;
        var factor = delta > 0 ? ZoomIncrement : 1.0 / ZoomIncrement;
        
        var mousePos = e.GetPosition(_canvas);
        ZoomToPoint(factor, mousePos);
        
        e.Handled = true;
    }
    
    /// <summary>
    /// Update the laser position indicator with new coordinates
    /// </summary>
    public void UpdateLaserPosition(double x, double y)
    {
        _laserX = x;
        _laserY = y;
        UpdateLaserCross();
    }
    
    /// <summary>
    /// Refresh all overlay elements (axis labels and laser cross) after zoom/pan
    /// </summary>
    private void RefreshOverlayElements()
    {
        if (_currentFile == null || !_currentFile.Bounds.IsValid)
            return;
        
        var bounds = _currentFile.Bounds;
        var gridSize = CalculateGridSize();
        
        // Extend grid beyond image bounds by 20%
        var extendX = bounds.Width * 0.2;
        var extendY = bounds.Height * 0.2;
        var minX = bounds.MinX - extendX;
        var maxX = bounds.MaxX + extendX;
        var minY = bounds.MinY - extendY;
        var maxY = bounds.MaxY + extendY;
        
        // Remove old overlay elements
        var itemsToRemove = new List<Control>();
        foreach (var child in _canvas.Children)
        {
            if (child != _contentCanvas && child != _laserCross && child != _crossHorizontalLine && child != _crossVerticalLine && child != _crossCursorSquare)
            {
                itemsToRemove.Add(child);
            }
        }
        foreach (var item in itemsToRemove)
        {
            _canvas.Children.Remove(item);
        }
        
        // Re-render axis labels
        RenderAxisLabels(minX, maxX, minY, maxY, gridSize);
        
        // Update laser cross position
        UpdateLaserCross();
    }
    
    /// <summary>
    /// Create or update the yellow cross indicator showing laser position
    /// </summary>
    private void UpdateLaserCross()
    {
        // Remove existing cross if present
        if (_laserCross != null && _canvas.Children.Contains(_laserCross))
        {
            _canvas.Children.Remove(_laserCross);
        }
        
        // Don't draw if no file is loaded or position is invalid
        if (_currentFile == null || !_currentFile.Bounds.IsValid)
            return;
        
        // Transform laser position to canvas coordinates (before zoom/pan)
        var position = TransformPoint(_laserX, _laserY);
        
        // Apply zoom and pan transforms to get screen position
        var screenX = position.X * _userZoom + (_panTransform?.X ?? 0);
        var screenY = position.Y * _userZoom + (_panTransform?.Y ?? 0);
        
        // Create a canvas to hold the cross lines
        _laserCross = new Canvas();
        
        const double crossSize = 20; // Size of the cross in pixels (fixed screen size)
        const double crossThickness = 2;
        
        // Create horizontal line of the cross
        var horizontalLine = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Point(-crossSize / 2, 0),
            EndPoint = new Point(crossSize / 2, 0),
            Stroke = _laserIndicatorBrush,
            StrokeThickness = crossThickness
        };
        
        // Create vertical line of the cross
        var verticalLine = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Point(0, -crossSize / 2),
            EndPoint = new Point(0, crossSize / 2),
            Stroke = _laserIndicatorBrush,
            StrokeThickness = crossThickness
        };
        
        // Add lines to the cross canvas
        _laserCross.Children.Add(horizontalLine);
        _laserCross.Children.Add(verticalLine);
        
        // Position the cross at the screen location (fixed size, not transformed)
        Canvas.SetLeft(_laserCross, screenX);
        Canvas.SetTop(_laserCross, screenY);
        
        // Add to main canvas (so it stays fixed size regardless of zoom/pan)
        _canvas.Children.Add(_laserCross);
    }
}
