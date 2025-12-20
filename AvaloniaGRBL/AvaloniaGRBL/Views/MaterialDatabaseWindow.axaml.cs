using Avalonia.Controls;
using AvaloniaGRBL.ViewModels;

namespace AvaloniaGRBL.Views;

public partial class MaterialDatabaseWindow : Window
{
    private MaterialDatabaseViewModel? ViewModel => DataContext as MaterialDatabaseViewModel;
    
    public MaterialDatabaseWindow()
    {
        InitializeComponent();
        DataContext = new MaterialDatabaseViewModel();
        
        if (ViewModel != null)
        {
            ViewModel.CloseRequested += OnCloseRequested;
        }
    }
    
    private void OnCloseRequested(object? sender, System.EventArgs e)
    {
        Close();
    }
    
    protected override void OnClosed(System.EventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.CloseRequested -= OnCloseRequested;
        }
        base.OnClosed(e);
    }
}
