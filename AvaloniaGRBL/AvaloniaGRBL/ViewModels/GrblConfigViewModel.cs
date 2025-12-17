using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using AvaloniaGRBL.Models;
using AvaloniaGRBL.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaGRBL.ViewModels;

public partial class GrblConfigViewModel : ViewModelBase
{
    private readonly GrblConnection _grblConnection;
    private readonly IStorageProvider? _storageProvider;
    
    [ObservableProperty]
    private ObservableCollection<GrblConfigParameter> _parameters = new();
    
    [ObservableProperty]
    private string _statusMessage = "";
    
    [ObservableProperty]
    private bool _isConnected;
    
    [ObservableProperty]
    private bool _isLoading;
    
    // Theme colors to match the main application
    [ObservableProperty] private string _mainBackground = "#2A3647";
    [ObservableProperty] private string _panelBackground = "#1E2936";
    [ObservableProperty] private string _borderColor = "#0F1419";
    [ObservableProperty] private string _buttonBackground = "#3C4A5C";
    [ObservableProperty] private string _buttonForeground = "#CCCCCC";
    [ObservableProperty] private string _buttonHoverBackground = "#4A5A6F";
    [ObservableProperty] private string _primaryText = "#CCCCCC";
    [ObservableProperty] private string _secondaryText = "#999999";
    [ObservableProperty] private string _accentText = "#8AB4F8";
    [ObservableProperty] private string _dataGridBackground = "#2D3E50";
    [ObservableProperty] private string _dataGridHeaderBackground = "#1E2936";
    [ObservableProperty] private string _dataGridRowBackground = "#2A3647";
    [ObservableProperty] private string _dataGridAlternateRowBackground = "#2D3E50";
    [ObservableProperty] private string _dataGridSelectedBackground = "#3C4A5C";
    [ObservableProperty] private string _dataGridEditableBackground = "#3A4A3A";
    
    public GrblConfigViewModel(GrblConnection grblConnection, IStorageProvider? storageProvider = null)
    {
        _grblConnection = grblConnection;
        _storageProvider = storageProvider;
        IsConnected = _grblConnection.IsConnected;
    }
    
    public async Task InitializeAsync()
    {
        if (IsConnected)
        {
            await ReadConfigurationAsync();
        }
        else
        {
            StatusMessage = "Not connected. Please connect to read configuration.";
        }
    }
    
    [RelayCommand(CanExecute = nameof(CanReadConfiguration))]
    private async Task ReadConfigurationAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Reading configuration from GRBL...";
            
            var config = await _grblConnection.ReadConfigurationAsync();
            
            // Update on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Parameters.Clear();
                foreach (var kvp in config.OrderBy(x => x.Key))
                {
                    var param = new GrblConfigParameter(kvp.Key, kvp.Value);
                    Parameters.Add(param);
                    System.Diagnostics.Debug.WriteLine($"Added parameter: ${param.Number} = {param.Value}");
                }
                System.Diagnostics.Debug.WriteLine($"Total parameters in collection: {Parameters.Count}");
            });
            
            StatusMessage = $"Successfully read {Parameters.Count} configuration parameters";
            IsLoading = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error reading configuration: {ex.Message}";
            IsLoading = false;
        }
    }
    
    private bool CanReadConfiguration() => IsConnected && !IsLoading;
    
    [RelayCommand(CanExecute = nameof(CanWriteConfiguration))]
    private async Task WriteConfigurationAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Writing configuration to GRBL...";
            
            var config = Parameters.ToDictionary(p => p.Number, p => p.Value);
            await _grblConnection.WriteConfigurationAsync(config);
            
            StatusMessage = "Configuration written successfully";
            IsLoading = false;
            
            // Re-read to confirm
            await Task.Delay(500);
            await ReadConfigurationAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error writing configuration: {ex.Message}";
            IsLoading = false;
        }
    }
    
    private bool CanWriteConfiguration() => IsConnected && !IsLoading && Parameters.Count > 0;
    
    [RelayCommand(CanExecute = nameof(CanExportConfiguration))]
    private async Task ExportConfigurationAsync()
    {
        if (_storageProvider == null)
        {
            StatusMessage = "Storage provider not available";
            return;
        }
        
        try
        {
            var file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export GRBL Configuration",
                SuggestedFileName = "grbl_config.nc",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("G-Code Files") { Patterns = new[] { "*.nc", "*.gcode" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });
            
            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new System.IO.StreamWriter(stream);
                
                foreach (var param in Parameters.OrderBy(p => p.Number))
                {
                    await writer.WriteLineAsync($"${param.Number}={param.Value}");
                }
                
                StatusMessage = $"Configuration exported to {file.Name}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting configuration: {ex.Message}";
        }
    }
    
    private bool CanExportConfiguration() => Parameters.Count > 0;
    
    [RelayCommand]
    private async Task ImportConfigurationAsync()
    {
        if (_storageProvider == null)
        {
            StatusMessage = "Storage provider not available";
            return;
        }
        
        try
        {
            var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import GRBL Configuration",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("G-Code Files") { Patterns = new[] { "*.nc", "*.gcode" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });
            
            if (files.Count > 0)
            {
                var file = files[0];
                await using var stream = await file.OpenReadAsync();
                using var reader = new System.IO.StreamReader(stream);
                
                var importedParams = new List<GrblConfigParameter>();
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    // Parse lines like "$0=10" or "$110=5000.000"
                    if (line.StartsWith("$"))
                    {
                        var parts = line.Substring(1).Split('=');
                        if (parts.Length == 2 && int.TryParse(parts[0], out int number))
                        {
                            var value = parts[1].Trim();
                            importedParams.Add(new GrblConfigParameter(number, value));
                        }
                    }
                }
                
                if (importedParams.Count > 0)
                {
                    Parameters.Clear();
                    foreach (var param in importedParams.OrderBy(p => p.Number))
                    {
                        Parameters.Add(param);
                    }
                    
                    StatusMessage = $"Imported {importedParams.Count} parameters from {file.Name}";
                }
                else
                {
                    StatusMessage = "No valid configuration parameters found in file";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing configuration: {ex.Message}";
        }
    }
}
