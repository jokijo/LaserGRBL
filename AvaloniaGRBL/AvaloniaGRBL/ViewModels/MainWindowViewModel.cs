using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
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
    [NotifyCanExecuteChangedFor(nameof(StartProgramCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetGrblCommand))]
    private bool _isConnected;
    
    [ObservableProperty]
    private string _statusText = "Disconnected";
    
    [ObservableProperty]
    private string _connectionLog = "";
    
    [ObservableProperty]
    private GCodeFile? _loadedGCodeFile;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartProgramCommand))]
    private bool _hasGCodeLoaded;
    
    [ObservableProperty]
    private string _gcodeFileName = "No file loaded";
    
    [ObservableProperty]
    private string _gcodeStats = "";
    
    [ObservableProperty]
    private double _jogSpeed = 1000;
    
    [ObservableProperty]
    private double _jogStep = 1;
    
    public List<double> JogStepOptions { get; } = new() { 0.1, 1, 10, 100 };
    
    [ObservableProperty]
    private string _machinePosition = "X: 0.000  Y: 0.000  Z: 0.000";
    
    [ObservableProperty]
    private string _workPosition = "X: 0.000  Y: 0.000  Z: 0.000";
    
    [ObservableProperty]
    private string _grblState = "Unknown";
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartProgramCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseProgramCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResumeProgramCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopProgramCommand))]
    private bool _isProgramRunning;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PauseProgramCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResumeProgramCommand))]
    private bool _isProgramPaused;
    
    [ObservableProperty]
    private int _currentLine;
    
    [ObservableProperty]
    private int _totalLines;
    
    private CancellationTokenSource? _executionCancellation;
    private readonly ManualResetEventSlim _pauseEvent = new(true);
    
    public MainWindowViewModel()
    {
        // Initialize serial communication and GRBL connection
        var serialComm = new SerialPortCommunication();
        _grblConnection = new GrblConnection(serialComm);
        
        // Subscribe to connection events
        _grblConnection.StatusChanged += OnConnectionStatusChanged;
        _grblConnection.DataReceived += OnDataReceived;
        _grblConnection.StatusReportReceived += OnStatusReportReceived;
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
    
    private void OnStatusReportReceived(object? sender, GrblStatus status)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            GrblState = status.State;
            MachinePosition = $"X: {status.MachineX:F3}  Y: {status.MachineY:F3}  Z: {status.MachineZ:F3}";
            WorkPosition = $"X: {status.WorkX:F3}  Y: {status.WorkY:F3}  Z: {status.WorkZ:F3}";
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
    
    [RelayCommand]
    private void JogNorthWest()
    {
        if (!IsConnected) return;
        try
        {
            _grblConnection.Jog(JogDirection.NW, JogStep, JogSpeed);
            AppendLog($"Jog NW: {JogStep}mm @ {JogSpeed}mm/min");
        }
        catch (Exception ex)
        {
            AppendLog($"Jog failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void JogNorth()
    {
        if (!IsConnected) return;
        try
        {
            _grblConnection.Jog(JogDirection.N, JogStep, JogSpeed);
            AppendLog($"Jog N: {JogStep}mm @ {JogSpeed}mm/min");
        }
        catch (Exception ex)
        {
            AppendLog($"Jog failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void JogNorthEast()
    {
        if (!IsConnected) return;
        try
        {
            _grblConnection.Jog(JogDirection.NE, JogStep, JogSpeed);
            AppendLog($"Jog NE: {JogStep}mm @ {JogSpeed}mm/min");
        }
        catch (Exception ex)
        {
            AppendLog($"Jog failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void JogWest()
    {
        if (!IsConnected) return;
        try
        {
            _grblConnection.Jog(JogDirection.W, JogStep, JogSpeed);
            AppendLog($"Jog W: {JogStep}mm @ {JogSpeed}mm/min");
        }
        catch (Exception ex)
        {
            AppendLog($"Jog failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void JogHome()
    {
        if (!IsConnected) return;
        try
        {
            _grblConnection.Jog(JogDirection.Home, 0, 0);
            AppendLog("Homing cycle started");
        }
        catch (Exception ex)
        {
            AppendLog($"Homing failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void JogEast()
    {
        if (!IsConnected) return;
        try
        {
            _grblConnection.Jog(JogDirection.E, JogStep, JogSpeed);
            AppendLog($"Jog E: {JogStep}mm @ {JogSpeed}mm/min");
        }
        catch (Exception ex)
        {
            AppendLog($"Jog failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void JogSouthWest()
    {
        if (!IsConnected) return;
        try
        {
            _grblConnection.Jog(JogDirection.SW, JogStep, JogSpeed);
            AppendLog($"Jog SW: {JogStep}mm @ {JogSpeed}mm/min");
        }
        catch (Exception ex)
        {
            AppendLog($"Jog failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void JogSouth()
    {
        if (!IsConnected) return;
        try
        {
            _grblConnection.Jog(JogDirection.S, JogStep, JogSpeed);
            AppendLog($"Jog S: {JogStep}mm @ {JogSpeed}mm/min");
        }
        catch (Exception ex)
        {
            AppendLog($"Jog failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void JogSouthEast()
    {
        if (!IsConnected) return;
        try
        {
            _grblConnection.Jog(JogDirection.SE, JogStep, JogSpeed);
            AppendLog($"Jog SE: {JogStep}mm @ {JogSpeed}mm/min");
        }
        catch (Exception ex)
        {
            AppendLog($"Jog failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Start executing the loaded G-code program
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartProgram))]
    private async Task StartProgramAsync()
    {
        if (!IsConnected || LoadedGCodeFile == null)
        {
            AppendLog("Error: Cannot start program - not connected or no file loaded");
            return;
        }
        
        if (IsProgramRunning)
        {
            AppendLog("Error: Program is already running");
            return;
        }
        
        try
        {
            AppendLog($"Starting program execution: {LoadedGCodeFile.CommandCount} commands");
            
            IsProgramRunning = true;
            IsProgramPaused = false;
            CurrentLine = 0;
            TotalLines = LoadedGCodeFile.CommandCount;
            
            // Ensure pause event is set (not paused) at start
            _pauseEvent.Set();
            
            _executionCancellation = new CancellationTokenSource();
            
            await Task.Run(() => ExecuteProgramLoop(_executionCancellation.Token));
            
            AppendLog("Program execution completed");
        }
        catch (OperationCanceledException)
        {
            AppendLog("Program execution stopped");
        }
        catch (Exception ex)
        {
            AppendLog($"Program execution error: {ex.Message}");
        }
        finally
        {
            IsProgramRunning = false;
            IsProgramPaused = false;
            CurrentLine = 0;
            
            // Ensure pause event is set for next execution
            _pauseEvent.Set();
            
            _executionCancellation?.Dispose();
            _executionCancellation = null;
        }
    }
    
    private bool CanStartProgram()
    {
        return IsConnected && HasGCodeLoaded && !IsProgramRunning;
    }
    
    /// <summary>
    /// Execute the G-code program loop
    /// </summary>
    private void ExecuteProgramLoop(CancellationToken cancellationToken)
    {
        if (LoadedGCodeFile == null) return;
        
        int lastReportedLine = 0;
        
        for (int i = 0; i < LoadedGCodeFile.Commands.Count && !cancellationToken.IsCancellationRequested; i++)
        {
            // Wait if paused using efficient event-based waiting
            if (!_pauseEvent.Wait(0))
            {
                _pauseEvent.Wait(cancellationToken);
            }
            
            if (cancellationToken.IsCancellationRequested)
                break;
            
            var command = LoadedGCodeFile.Commands[i];
            
            // Skip empty lines and comments
            if (command.CommandType == GCodeCommandType.Comment || 
                string.IsNullOrWhiteSpace(command.RawCommand))
            {
                continue;
            }
            
            try
            {
                _grblConnection.SendCommand(command.RawCommand);
                
                // Update UI periodically (every 10 lines) to reduce overhead
                if (i - lastReportedLine >= 10 || i == LoadedGCodeFile.Commands.Count - 1)
                {
                    int currentLineSnapshot = i + 1;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        CurrentLine = currentLineSnapshot;
                    });
                    lastReportedLine = i;
                }
                
                // NOTE: Simple delay-based implementation for initial version.
                // A production implementation should wait for 'ok' acknowledgments from GRBL
                // to handle buffering properly and avoid overwhelming the controller.
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    AppendLog($"Error sending command at line {i + 1}: {ex.Message}");
                });
                break;
            }
        }
        
        // Final update
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (!cancellationToken.IsCancellationRequested && LoadedGCodeFile != null)
            {
                CurrentLine = LoadedGCodeFile.Commands.Count;
            }
        });
    }
    
    /// <summary>
    /// Pause the currently running program
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPauseProgram))]
    private void PauseProgram()
    {
        if (!IsProgramRunning || IsProgramPaused)
        {
            AppendLog("Error: No running program to pause");
            return;
        }
        
        try
        {
            // Block the execution loop
            _pauseEvent.Reset();
            
            // Send Feed Hold command (0x21 = '!')
            _grblConnection.SendImmediate(0x21);
            IsProgramPaused = true;
            AppendLog("Program paused (Feed Hold)");
        }
        catch (Exception ex)
        {
            AppendLog($"Pause failed: {ex.Message}");
        }
    }
    
    private bool CanPauseProgram()
    {
        return IsProgramRunning && !IsProgramPaused;
    }
    
    /// <summary>
    /// Resume the paused program
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanResumeProgram))]
    private void ResumeProgram()
    {
        if (!IsProgramRunning || !IsProgramPaused)
        {
            AppendLog("Error: No paused program to resume");
            return;
        }
        
        try
        {
            // Send Cycle Start/Resume command (0x7E = '~')
            _grblConnection.SendImmediate(0x7E);
            
            // Unblock the execution loop
            _pauseEvent.Set();
            IsProgramPaused = false;
            AppendLog("Program resumed (Cycle Start)");
        }
        catch (Exception ex)
        {
            AppendLog($"Resume failed: {ex.Message}");
        }
    }
    
    private bool CanResumeProgram()
    {
        return IsProgramRunning && IsProgramPaused;
    }
    
    /// <summary>
    /// Stop/Abort the currently running program
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStopProgram))]
    private void StopProgram()
    {
        if (!IsProgramRunning)
        {
            AppendLog("Error: No running program to stop");
            return;
        }
        
        try
        {
            // Cancel the execution loop
            _executionCancellation?.Cancel();
            
            // Send Feed Hold to stop motion immediately
            _grblConnection.SendImmediate(0x21);
            
            IsProgramRunning = false;
            IsProgramPaused = false;
            CurrentLine = 0;
            
            AppendLog("Program stopped");
        }
        catch (Exception ex)
        {
            AppendLog($"Stop failed: {ex.Message}");
        }
    }
    
    private bool CanStopProgram()
    {
        return IsProgramRunning;
    }
    
    /// <summary>
    /// Send soft reset to GRBL (Ctrl-X)
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanResetGrbl))]
    private void ResetGrbl()
    {
        if (!IsConnected)
        {
            AppendLog("Error: Not connected");
            return;
        }
        
        try
        {
            // Stop any running program first
            if (IsProgramRunning)
            {
                _executionCancellation?.Cancel();
                IsProgramRunning = false;
                IsProgramPaused = false;
                CurrentLine = 0;
            }
            
            // Send soft reset command (0x18 = Ctrl-X)
            _grblConnection.SendImmediate(0x18);
            
            AppendLog("GRBL reset sent (Ctrl-X)");
        }
        catch (Exception ex)
        {
            AppendLog($"Reset failed: {ex.Message}");
        }
    }
    
    private bool CanResetGrbl()
    {
        return IsConnected;
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            // Stop any running program
            _executionCancellation?.Cancel();
            _executionCancellation?.Dispose();
            
            // Dispose pause event
            _pauseEvent.Dispose();
            
            // Unsubscribe from events
            _grblConnection.StatusChanged -= OnConnectionStatusChanged;
            _grblConnection.DataReceived -= OnDataReceived;
            _grblConnection.StatusReportReceived -= OnStatusReportReceived;
            _grblConnection.ErrorOccurred -= OnErrorOccurred;
            
            // Dispose connection
            _grblConnection.Dispose();
            
            _disposed = true;
        }
    }
}
