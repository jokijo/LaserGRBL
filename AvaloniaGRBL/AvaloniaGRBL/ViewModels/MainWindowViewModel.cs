using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvaloniaGRBL.Models;
using AvaloniaGRBL.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaGRBL.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly GrblConnection _grblConnection;
    private readonly Queue<string> _logQueue = new(1000);
    private bool _disposed;
    
    [ObservableProperty]
    private ObservableCollection<string> _availablePorts;
    
    [ObservableProperty]
    private string? _selectedPort;
    
    [ObservableProperty]
    private ObservableCollection<int> _availableBaudRates;
    
    [ObservableProperty]
    private int _selectedBaudRate = 115200;
    
    [ObservableProperty]
    private bool _isConnected;
    
    [ObservableProperty]
    private string _statusText = "Disconnected";
    
    [ObservableProperty]
    private string _connectionLog = "";
    
    [ObservableProperty]
    private GCodeFile? _loadedGCodeFile;
    
    [ObservableProperty]
    private bool _hasGCodeLoaded;
    
    [ObservableProperty]
    private string _gcodeFileName = "No file loaded";
    
    [ObservableProperty]
    private string _gcodeStats = "";
    
    public MainWindowViewModel()
    {
        // Initialize serial communication and GRBL connection
        var serialComm = new SerialPortCommunication();
        _grblConnection = new GrblConnection(serialComm);
        
        // Subscribe to connection events
        _grblConnection.StatusChanged += OnConnectionStatusChanged;
        _grblConnection.DataReceived += OnDataReceived;
        _grblConnection.ErrorOccurred += OnErrorOccurred;
        
        // Initialize port list
        _availablePorts = new ObservableCollection<string>();
        RefreshPorts();
        
        // Initialize baud rates
        _availableBaudRates = new ObservableCollection<int>(SerialPortService.GetCommonBaudRates());
    }
    
    [RelayCommand]
    private void RefreshPorts()
    {
        var ports = SerialPortService.GetAvailablePorts();
        AvailablePorts.Clear();
        
        foreach (var port in ports)
        {
            AvailablePorts.Add(port);
        }
        
        if (AvailablePorts.Any() && string.IsNullOrEmpty(SelectedPort))
        {
            SelectedPort = AvailablePorts.First();
        }
        
        AppendLog($"Found {AvailablePorts.Count} serial port(s)");
    }
    
    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (IsConnected)
        {
            Disconnect();
            return;
        }
        
        if (string.IsNullOrEmpty(SelectedPort))
        {
            AppendLog("Error: No port selected");
            return;
        }
        
        try
        {
            AppendLog($"Connecting to {SelectedPort} at {SelectedBaudRate} baud...");
            await _grblConnection.ConnectAsync(SelectedPort, SelectedBaudRate);
            IsConnected = true;
        }
        catch (Exception ex)
        {
            AppendLog($"Connection failed: {ex.Message}");
            IsConnected = false;
        }
    }
    
    [RelayCommand]
    private void Disconnect()
    {
        try
        {
            AppendLog("Disconnecting...");
            _grblConnection.Disconnect();
            IsConnected = false;
        }
        catch (Exception ex)
        {
            AppendLog($"Disconnect error: {ex.Message}");
        }
    }
    
    private void OnConnectionStatusChanged(object? sender, string status)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            StatusText = status;
            AppendLog($"Status: {status}");
        });
    }
    
    private void OnDataReceived(object? sender, string data)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            AppendLog($"RX: {data}");
        });
    }
    
    private void OnErrorOccurred(object? sender, Exception exception)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            AppendLog($"Error: {exception.Message}");
        });
    }
    
    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}";
        
        // Add to queue and maintain max size
        _logQueue.Enqueue(logEntry);
        if (_logQueue.Count > 1000)
        {
            _logQueue.Dequeue();
        }
        
        // Rebuild log text from queue using string.Join for efficiency
        ConnectionLog = string.Join(Environment.NewLine, _logQueue);
    }
    
    [RelayCommand]
    private async Task LoadGCodeFileAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
            
            if (topLevel == null)
                return;
            
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Open G-Code File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("G-Code Files")
                    {
                        Patterns = new[] { "*.gcode", "*.nc", "*.ngc", "*.txt" }
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });
            
            if (files.Count > 0)
            {
                var file = files[0];
                var path = file.Path.LocalPath;
                
                AppendLog($"Loading G-Code file: {file.Name}");
                
                // Load file on background thread to avoid blocking UI
                LoadedGCodeFile = await Task.Run(() => GCodeFile.Load(path));
                HasGCodeLoaded = true;
                GcodeFileName = file.Name;
                
                UpdateGCodeStats();
                
                AppendLog($"G-Code file loaded: {LoadedGCodeFile.CommandCount} commands");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Error loading G-Code file: {ex.Message}");
            HasGCodeLoaded = false;
            LoadedGCodeFile = null;
        }
    }
    
    private void UpdateGCodeStats()
    {
        if (LoadedGCodeFile == null)
        {
            GcodeStats = "";
            return;
        }
        
        var bounds = LoadedGCodeFile.Bounds;
        GcodeStats = $"Commands: {LoadedGCodeFile.CommandCount}\n" +
                     $"Size: {bounds.Width:F2} x {bounds.Height:F2} mm";
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            // Unsubscribe from events
            _grblConnection.StatusChanged -= OnConnectionStatusChanged;
            _grblConnection.DataReceived -= OnDataReceived;
            _grblConnection.ErrorOccurred -= OnErrorOccurred;
            
            // Dispose connection
            _grblConnection.Dispose();
            
            _disposed = true;
        }
    }
}
