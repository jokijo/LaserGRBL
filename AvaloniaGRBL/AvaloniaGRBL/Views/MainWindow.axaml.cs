using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaGRBL.Services;
using AvaloniaGRBL.ViewModels;

namespace AvaloniaGRBL.Views;

public partial class MainWindow : Window
{
    private GCodeRenderer? _renderer;
    private bool _isConnectionLogHovered;
    private bool _isGCodeLogHovered;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Wire up the renderer when the window is loaded
        this.Loaded += MainWindow_Loaded;
        
        // Setup drag-and-drop handlers
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
        DragDrop.SetAllowDrop(this, true);
        
        // Setup hover tracking for auto-scroll control
        this.Loaded += (s, e) => SetupScrollViewerHoverTracking();
    }
    
    private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Find the preview canvas and set up the renderer
        var canvas = this.FindControl<Canvas>("PreviewCanvas");
        if (canvas != null)
        {
            _renderer = new GCodeRenderer(canvas);
            
            // Subscribe to ViewModel changes
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                viewModel.Renderer = _renderer;
                
                // Sync initial state from ViewModel to Renderer
                _renderer.ShowCrossCursor = viewModel.CrossCursor;
                _renderer.ShowLaserOffMovements = viewModel.ShowLaserOffMovements;
                _renderer.ShowBoundingBox = viewModel.ShowBoundingBox;
            }
            
            // Add mouse wheel event handler for zoom
            canvas.PointerWheelChanged += Canvas_PointerWheelChanged;
        }
    }
    
    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from ViewModel events to prevent memory leak
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
        
        base.OnClosed(e);
    }
    
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.LoadedGCodeFile))
        {
            if (sender is MainWindowViewModel viewModel && _renderer != null)
            {
                if (viewModel.LoadedGCodeFile != null)
                {
                    _renderer.RenderFile(viewModel.LoadedGCodeFile);
                }
                else
                {
                    _renderer.Clear();
                }
            }
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.CrossCursor))
        {
            if (sender is MainWindowViewModel viewModel && _renderer != null)
            {
                _renderer.ShowCrossCursor = viewModel.CrossCursor;
            }
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.ShowLaserOffMovements))
        {
            if (sender is MainWindowViewModel viewModel && _renderer != null)
            {
                _renderer.ShowLaserOffMovements = viewModel.ShowLaserOffMovements;
            }
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.ShowBoundingBox))
        {
            if (sender is MainWindowViewModel viewModel && _renderer != null)
            {
                _renderer.ShowBoundingBox = viewModel.ShowBoundingBox;
            }
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.ConnectionLog))
        {
            ScrollToBottom("ConnectionLogScrollViewer");
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.GcodeLog))
        {
            ScrollToBottom("GCodeLogScrollViewer");
        }
    }
    
    private void SetupScrollViewerHoverTracking()
    {
        // Setup hover tracking for Connection Log
        var connectionLogScrollViewer = this.FindControl<ScrollViewer>("ConnectionLogScrollViewer");
        if (connectionLogScrollViewer != null)
        {
            connectionLogScrollViewer.PointerEntered += (s, e) => _isConnectionLogHovered = true;
            connectionLogScrollViewer.PointerExited += (s, e) => _isConnectionLogHovered = false;
        }
        
        // Setup hover tracking for GCode Log
        var gcodeLogScrollViewer = this.FindControl<ScrollViewer>("GCodeLogScrollViewer");
        if (gcodeLogScrollViewer != null)
        {
            gcodeLogScrollViewer.PointerEntered += (s, e) => _isGCodeLogHovered = true;
            gcodeLogScrollViewer.PointerExited += (s, e) => _isGCodeLogHovered = false;
        }
    }
    
    private void ScrollToBottom(string scrollViewerName)
    {
        // Check if user is hovering over the log - if so, don't auto-scroll
        if (scrollViewerName == "ConnectionLogScrollViewer" && _isConnectionLogHovered)
            return;
        if (scrollViewerName == "GCodeLogScrollViewer" && _isGCodeLogHovered)
            return;
        
        var scrollViewer = this.FindControl<ScrollViewer>(scrollViewerName);
        if (scrollViewer != null)
        {
            // Use Dispatcher to ensure scroll happens after content is updated
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                scrollViewer.ScrollToEnd();
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }
    
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Only allow file drops
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        
        e.Handled = true;
    }
    
    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            var files = e.DataTransfer.TryGetFiles();
            if (files != null && files.Length > 0)
            {
                var firstFile = files[0];
                if (firstFile is Avalonia.Platform.Storage.IStorageFile storageFile)
                {
                    var filePath = storageFile.Path.LocalPath;
                    
                    // Check if the file has a valid G-Code or SVG extension
                    var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                    if (extension == ".gcode" || extension == ".nc" || extension == ".ngc" || extension == ".txt" || extension == ".svg")
                    {
                        if (DataContext is MainWindowViewModel viewModel)
                        {
                            _ = viewModel.LoadGCodeFileFromPathAsync(filePath);
                        }
                    }
                }
            }
        }
        
        e.Handled = true;
    }
    
    private void Canvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_renderer == null)
            return;
        
        // Get the wheel delta (positive = scroll up/zoom in, negative = scroll down/zoom out)
        var delta = e.Delta.Y;
        
        if (delta > 0)
        {
            // Zoom in
            _renderer.ZoomIn();
        }
        else if (delta < 0)
        {
            // Zoom out
            _renderer.ZoomOut();
        }
        
        e.Handled = true;
    }
}