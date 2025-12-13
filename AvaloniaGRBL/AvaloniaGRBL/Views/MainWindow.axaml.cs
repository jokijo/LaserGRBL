using System;
using Avalonia.Controls;
using AvaloniaGRBL.Services;
using AvaloniaGRBL.ViewModels;

namespace AvaloniaGRBL.Views;

public partial class MainWindow : Window
{
    private GCodeRenderer? _renderer;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Wire up the renderer when the window is loaded
        this.Loaded += MainWindow_Loaded;
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
            }
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
    }
}