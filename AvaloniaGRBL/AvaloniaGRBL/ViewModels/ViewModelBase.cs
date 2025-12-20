using AvaloniaGRBL.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaGRBL.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Provides access to the centralized theme service.
    /// All ViewModels can use this to bind to theme colors.
    /// </summary>
    public ThemeService Theme => ThemeService.Instance;
}
