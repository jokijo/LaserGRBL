using Avalonia;
using Avalonia.Controls;

namespace AvaloniaGRBL.Views;

public partial class PowerVsSpeedWindow : Window
{
    public PowerVsSpeedWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void OnCreate(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
