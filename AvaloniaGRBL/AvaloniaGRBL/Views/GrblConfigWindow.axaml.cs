using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaGRBL.ViewModels;

namespace AvaloniaGRBL.Views;

public partial class GrblConfigWindow : Window
{
    public GrblConfigWindow()
    {
        InitializeComponent();
    }
    
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
    
    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        
        // Initialize the view model when the window opens
        if (DataContext is GrblConfigViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
