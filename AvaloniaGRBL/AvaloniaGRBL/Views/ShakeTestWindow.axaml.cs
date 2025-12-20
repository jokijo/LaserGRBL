using System;
using Avalonia.Controls;
using AvaloniaGRBL.ViewModels;

namespace AvaloniaGRBL.Views;

public partial class ShakeTestWindow : Window
{
    public ShakeTestWindow()
    {
        InitializeComponent();
        
        // Subscribe to close event from ViewModel
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ShakeTestViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
            viewModel.CloseRequested += OnCloseRequested;
        }
    }
    
    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is ShakeTestViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
        }
        base.OnClosing(e);
    }
}
