using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
    // URL constants
    private const string HelpFaqUrl = "https://lasergrbl.com/faq/";
    private const string DonateUrl = "https://lasergrbl.com/donate";
    private const string FacebookCommunityUrl = "https://www.facebook.com/groups/486886768471991";
    private const string CH340DriversSearchUrl = "https://www.google.com/search?q=ch340+drivers";
    
    // Localization manager for dynamic language switching
    public LocalizationManager Localization => LocalizationManager.Instance;
    
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
    
    private string? _lastOpenedFilePath;
    
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
            
            // Update laser position indicator on canvas using machine coordinates
            // (machine coordinates match the G-code preview coordinate system)
            Renderer?.UpdateLaserPosition(status.MachineX, status.MachineY);
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
                    new Avalonia.Platform.Storage.FilePickerFileType("Vector Files")
                    {
                        Patterns = new[] { "*.svg" }
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
                _lastOpenedFilePath = path;
                
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
    
    /// <summary>
    /// Loads a G-Code file from the specified path. This method can be called externally (e.g., from drag-and-drop).
    /// </summary>
    public async Task LoadGCodeFileFromPathAsync(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // Check if it's an SVG file
            if (extension == ".svg")
            {
                AppendLog($"Loading SVG file: {fileName}");
                
                // Convert SVG to G-Code on background thread
                LoadedGCodeFile = await Task.Run(() =>
                {
                    var converter = new SvgToGCodeConverter
                    {
                        FeedRate = 1000,
                        TravelSpeed = 3000,
                        LaserPowerOn = 1000,
                        LaserPowerOff = 0,
                        Scale = 1.0
                    };
                    
                    var gcode = converter.ConvertFile(filePath);
                    
                    // Create a temporary G-Code file from the converted content
                    var tempFile = Path.GetTempFileName();
                    File.WriteAllText(tempFile, gcode);
                    
                    var gcodeFile = GCodeFile.Load(tempFile);
                    
                    // Clean up temp file
                    try { File.Delete(tempFile); } catch { }
                    
                    return gcodeFile;
                });
                
                AppendLog($"SVG file converted to G-Code: {LoadedGCodeFile.CommandCount} commands");
            }
            else
            {
                AppendLog($"Loading G-Code file: {fileName}");
                
                // Load file on background thread to avoid blocking UI
                LoadedGCodeFile = await Task.Run(() => GCodeFile.Load(filePath));
                
                AppendLog($"G-Code file loaded: {LoadedGCodeFile.CommandCount} commands");
            }
            
            HasGCodeLoaded = true;
            GcodeFileName = fileName;
            _lastOpenedFilePath = filePath;
            
            UpdateGCodeStats();
        }
        catch (Exception ex)
        {
            AppendLog($"Error loading file: {ex.Message}");
            HasGCodeLoaded = false;
            LoadedGCodeFile = null;
        }
    }
    
    [RelayCommand]
    private async Task AppendGCodeFileAsync()
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
                Title = "Append G-Code File",
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
                
                AppendLog($"Appending G-Code file: {file.Name}");
                
                // Load the new file
                var appendedFile = await Task.Run(() => GCodeFile.Load(path));
                
                if (LoadedGCodeFile != null)
                {
                    // Append commands to existing file
                    LoadedGCodeFile.AppendCommands(appendedFile.Commands);
                    GcodeFileName = $"{GcodeFileName} + {file.Name}";
                    // Clear the last opened file path since content has changed
                    _lastOpenedFilePath = null;
                }
                else
                {
                    // No existing file, just load it
                    LoadedGCodeFile = appendedFile;
                    GcodeFileName = file.Name;
                    _lastOpenedFilePath = path;
                }
                
                HasGCodeLoaded = true;
                UpdateGCodeStats();
                
                AppendLog($"File appended. Total commands: {LoadedGCodeFile.CommandCount}");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Error appending G-Code file: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task ReOpenGCodeFileAsync()
    {
        if (string.IsNullOrEmpty(_lastOpenedFilePath))
        {
            AppendLog("No file to re-open");
            return;
        }
        
        try
        {
            AppendLog($"Re-opening file: {_lastOpenedFilePath}");
            
            // Load file on background thread to avoid blocking UI
            LoadedGCodeFile = await Task.Run(() => GCodeFile.Load(_lastOpenedFilePath));
            HasGCodeLoaded = true;
            GcodeFileName = Path.GetFileName(_lastOpenedFilePath);
            
            UpdateGCodeStats();
            
            AppendLog($"G-Code file re-opened: {LoadedGCodeFile.CommandCount} commands");
        }
        catch (Exception ex)
        {
            AppendLog($"Error re-opening G-Code file: {ex.Message}");
            HasGCodeLoaded = false;
            LoadedGCodeFile = null;
        }
    }
    
    [RelayCommand]
    private async Task SaveProgramAsync()
    {
        if (LoadedGCodeFile == null)
        {
            AppendLog("No program to save");
            return;
        }
        
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
            
            if (topLevel == null)
                return;
            
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save G-Code Program",
                DefaultExtension = "gcode",
                SuggestedFileName = string.IsNullOrEmpty(_lastOpenedFilePath) 
                    ? "program.gcode" 
                    : Path.GetFileName(_lastOpenedFilePath),
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("G-Code Files")
                    {
                        Patterns = new[] { "*.gcode", "*.nc", "*.ngc" }
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("Text Files")
                    {
                        Patterns = new[] { "*.txt" }
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });
            
            if (file != null)
            {
                var path = file.Path.LocalPath;
                AppendLog($"Saving program to: {path}");
                
                await LoadedGCodeFile.SaveAsync(path);
                _lastOpenedFilePath = path;
                GcodeFileName = Path.GetFileName(path);
                
                AppendLog($"Program saved successfully");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Error saving program: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void AdvancedSave()
    {
        // Placeholder for advanced save functionality
        AppendLog("Advanced Save feature coming soon");
    }
    
    [RelayCommand]
    private void SaveProject()
    {
        // Placeholder for save project functionality
        AppendLog("Save Project feature coming soon");
    }
    
    [RelayCommand]
    private void StartFromPosition()
    {
        // Placeholder for start from position functionality
        AppendLog("Start From Position feature coming soon");
    }
    
    [RelayCommand]
    private void RunMultiple()
    {
        // Placeholder for run multiple functionality
        AppendLog("Run Multiple feature coming soon");
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
    /// Set current position as origin (0,0,0)
    /// </summary>
    [RelayCommand]
    private void SetOrigin()
    {
        if (!IsConnected)
        {
            AppendLog("Error: Not connected to GRBL");
            return;
        }
        
        try
        {
            // Send G92 X0 Y0 Z0 command to set current position as origin
            _grblConnection.SendCommand("G92 X0 Y0 Z0");
            AppendLog("Work origin set to current position (G92 X0 Y0 Z0)");
        }
        catch (Exception ex)
        {
            AppendLog($"Set origin failed: {ex.Message}");
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
            // This will return immediately if not paused, or wait until resumed/cancelled
            try
            {
                _pauseEvent.Wait(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            
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
            // Update state first before sending commands
            IsProgramPaused = false;
            
            // Send Cycle Start/Resume command (0x7E = '~')
            _grblConnection.SendImmediate(0x7E);
            
            // Unblock the execution loop
            _pauseEvent.Set();
            
            AppendLog("Program resumed (Cycle Start)");
        }
        catch (Exception ex)
        {
            AppendLog($"Resume failed: {ex.Message}");
            // Restore paused state on error
            IsProgramPaused = true;
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
            // Unblock pause event to prevent deadlock
            _pauseEvent.Set();
            
            // Cancel the execution loop
            _executionCancellation?.Cancel();
            
            // Send Feed Hold to stop motion immediately
            _grblConnection.SendImmediate(0x21);
            
            // Turn off laser/spindle (M5 command)
            _grblConnection.SendCommand("M5");
            
            IsProgramRunning = false;
            IsProgramPaused = false;
            CurrentLine = 0;
            
            AppendLog("Program stopped (M5 sent to turn off laser)");
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
                // Unblock pause event to prevent deadlock
                _pauseEvent.Set();
                
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
    
    // ===== Grbl Menu Commands =====
    
    [RelayCommand]
    private void WiFiDiscovery()
    {
        AppendLog("WiFi Discovery feature coming soon");
    }
    
    [RelayCommand]
    private void GoHome()
    {
        if (!IsConnected)
        {
            AppendLog("Error: Not connected");
            return;
        }
        
        try
        {
            _grblConnection.SendCommand("$H");
            AppendLog("Homing command sent ($H)");
        }
        catch (Exception ex)
        {
            AppendLog($"Go Home failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void Unlock()
    {
        if (!IsConnected)
        {
            AppendLog("Error: Not connected");
            return;
        }
        
        try
        {
            _grblConnection.SendCommand("$X");
            AppendLog("Unlock command sent ($X)");
        }
        catch (Exception ex)
        {
            AppendLog($"Unlock failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task GrblConfigurationAsync()
    {
        if (!IsConnected)
        {
            AppendLog("Error: Not connected. Please connect to GRBL device first.");
            return;
        }
        
        try
        {
            var window = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var mainWindow = window?.MainWindow;
            
            if (mainWindow != null)
            {
                var configViewModel = new GrblConfigViewModel(_grblConnection, mainWindow.StorageProvider);
                var configWindow = new Views.GrblConfigWindow
                {
                    DataContext = configViewModel
                };
                
                await configWindow.ShowDialog(mainWindow);
                AppendLog("GRBL Configuration window closed");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Error opening GRBL Configuration: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task SettingsAsync()
    {
        try
        {
            var settingsWindow = new Views.SettingsWindow
            {
                DataContext = new SettingsViewModel()
            };
            
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow 
                : null;
            
            await settingsWindow.ShowDialog(mainWindow!);
            AppendLog("Settings saved");
        }
        catch (Exception ex)
        {
            AppendLog($"Error opening settings: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task MaterialDatabaseAsync()
    {
        try
        {
            var window = new Views.MaterialDatabaseWindow();
            
            // Get the main window
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow 
                : null;
            
            if (mainWindow != null)
            {
                await window.ShowDialog(mainWindow);
                AppendLog("Material database window closed");
            }
            else
            {
                window.Show();
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Error opening material database: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task LaserUsageStatsAsync()
    {
        try
        {
            var window = new Views.LaserUsageStatsWindow
            {
                DataContext = new LaserUsageStatsViewModel()
            };
            
            // Set up close command
            if (window.DataContext is LaserUsageStatsViewModel viewModel)
            {
                viewModel.CloseCommand = new RelayCommand(() => window.Close());
            }
            
            // Get the main window
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow 
                : null;
            
            if (mainWindow != null)
            {
                await window.ShowDialog(mainWindow);
                AppendLog("Laser usage stats window closed");
            }
            else
            {
                window.Show();
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Error opening laser usage stats: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task HotkeysAsync()
    {
        try
        {
            var window = new Views.HotkeyConfigWindow();
            
            // Get the main window
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow 
                : null;
            
            if (mainWindow != null)
            {
                await window.ShowDialog(mainWindow);
                AppendLog("Hotkey configuration window closed");
            }
            else
            {
                window.Show();
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Error opening hotkey configuration: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void Exit()
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime 
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        lifetime?.Shutdown();
    }
    
    // ===== Generate Menu Commands =====
    
    [RelayCommand]
    private void PowerVsSpeed()
    {
        AppendLog("Power vs Speed generator feature coming soon");
    }
    
    [RelayCommand]
    private void CuttingTest()
    {
        AppendLog("Cutting Test generator feature coming soon");
    }
    
    [RelayCommand]
    private void AccuracyTest()
    {
        AppendLog("Accuracy Test generator feature coming soon");
    }
    
    [RelayCommand]
    private void ShakeTest()
    {
        AppendLog("Shake Test generator feature coming soon");
    }
    
    [RelayCommand]
    private void ESP8266()
    {
        AppendLog("ESP8266 configuration feature coming soon");
    }
    
    [RelayCommand]
    private void GrblEmulator()
    {
        AppendLog("Grbl Emulator feature coming soon");
    }
    
    // ===== Schema Menu Commands =====
    
    [ObservableProperty]
    private string _currentSchema = "Dark";
    
    // Theme color properties - delegated to ThemeService
    public string MainBackground => Theme.MainBackground;
    public string PanelBackground => Theme.PanelBackground;
    public string BorderColor => Theme.BorderColor;
    public string CanvasBackground => Theme.CanvasBackground;
    public string ButtonBackground => Theme.ButtonBackground;
    public string ButtonForeground => Theme.ButtonForeground;
    public string ButtonHoverBackground => Theme.ButtonHoverBackground;
    public string ButtonPressedBackground => Theme.ButtonPressedBackground;
    public string ButtonBorder => Theme.ButtonBorder;
    public string JogButtonForeground => Theme.JogButtonForeground;
    public string JogButtonHoverForeground => Theme.JogButtonHoverForeground;
    public string ActionButtonForeground => Theme.ActionButtonForeground;
    public string ActionButtonBorder => Theme.ActionButtonBorder;
    public string ActionButtonHoverForeground => Theme.ActionButtonHoverForeground;
    public string PrimaryText => Theme.PrimaryText;
    public string SecondaryText => Theme.SecondaryText;
    public string AccentText => Theme.AccentText;
    public string ConsoleText => Theme.ConsoleText;
    public string SliderBackground => Theme.SliderBackground;
    public string SliderForeground => Theme.SliderForeground;
    public string ComboBoxBackground => Theme.ComboBoxBackground;
    public string ComboBoxForeground => Theme.ComboBoxForeground;
    public string SeparatorColor => Theme.SeparatorColor;
    public string InfoBoxBackground => Theme.InfoBoxBackground;
    public string InfoBoxBorder => Theme.InfoBoxBorder;
    
    [RelayCommand]
    private void SetSchema(string schemaName)
    {
        if (Theme.SetTheme(schemaName))
        {
            CurrentSchema = schemaName;
            AppendLog($"Color schema changed to: {schemaName}");
        }
        else
        {
            AppendLog($"Error: Unknown schema '{schemaName}'");
        }
    }
    
    // ===== Preview Menu Commands =====
    
    public GCodeRenderer? Renderer { get; set; }
    
    [RelayCommand]
    private void AutoSize()
    {
        Renderer?.ZoomAuto();
        AppendLog("Zoom reset to auto-fit");
    }
    
    [RelayCommand]
    private void ZoomIn()
    {
        Renderer?.ZoomIn();
        AppendLog("Zoomed in (+10%)");
    }
    
    [RelayCommand]
    private void ZoomOut()
    {
        Renderer?.ZoomOut();
        AppendLog("Zoomed out (-10%)");
    }
    
    [ObservableProperty]
    private bool _showLaserOffMovements = false;
    
    [RelayCommand]
    private void ToggleLaserOffMovements()
    {
        ShowLaserOffMovements = !ShowLaserOffMovements;
        AppendLog($"Show Laser Off Movements: {ShowLaserOffMovements}");
    }
    
    [ObservableProperty]
    private bool _showExecutedCommands = false;
    
    [RelayCommand]
    private void ToggleExecutedCommands()
    {
        ShowExecutedCommands = !ShowExecutedCommands;
        AppendLog($"Show Executed Commands: {ShowExecutedCommands}");
    }
    
    [ObservableProperty]
    private bool _showBoundingBox = false;
    
    [RelayCommand]
    private void ToggleBoundingBox()
    {
        ShowBoundingBox = !ShowBoundingBox;
        AppendLog($"Show Bounding Box: {ShowBoundingBox}");
    }
    
    [ObservableProperty]
    private bool _crossCursor = false;
    
    [RelayCommand]
    private void ToggleCrossCursor()
    {
        CrossCursor = !CrossCursor;
        AppendLog($"Cross Cursor: {CrossCursor}");
    }
    
    // ===== Language Menu Commands =====
    
    [ObservableProperty]
    private string _currentLanguage = "English";
    
    [RelayCommand]
    private void SetLanguage(string languageName)
    {
        // Find the language by display name
        var language = LocalizationService.AvailableLanguages
            .FirstOrDefault(l => l.DisplayName.Equals(languageName, StringComparison.OrdinalIgnoreCase));
        
        if (language == null)
        {
            AppendLog($"Language not found: {languageName}");
            return;
        }
        
        CurrentLanguage = language.DisplayName;
        LocalizationService.SetLanguage(language.CultureCode);
        AppendLog($"Language changed to: {languageName} - Applied immediately!");
    }
    
    // ===== Tools Menu Commands =====
    
    [RelayCommand]
    private void InstallCH340Driver()
    {
        AppendLog("Install CH340 Driver feature coming soon");
        AppendLog($"Please download the driver from: {CH340DriversSearchUrl}");
    }
    
    [RelayCommand]
    private void FlashGrblFirmware()
    {
        if (IsConnected)
        {
            AppendLog("Error: Please disconnect before flashing firmware");
            return;
        }
        AppendLog("Flash Grbl Firmware feature coming soon");
    }
    
    [RelayCommand]
    private void ConfigurationWizard()
    {
        AppendLog("Configuration Wizard feature coming soon");
    }
    
    // ===== Help Menu Commands =====
    
    [ObservableProperty]
    private bool _autoUpdateEnabled = true;
    
    [RelayCommand]
    private void ToggleAutoUpdate()
    {
        AutoUpdateEnabled = !AutoUpdateEnabled;
        AppendLog($"Auto Update: {(AutoUpdateEnabled ? "Enabled" : "Disabled")}");
    }
    
    [RelayCommand]
    private void CheckForUpdates()
    {
        AppendLog("Checking for updates...");
        AppendLog("Check for updates feature coming soon");
    }
    
    [RelayCommand]
    private void OpenSessionLog()
    {
        AppendLog("Open Session Log feature coming soon");
    }
    
    [ObservableProperty]
    private bool _extendedLogEnabled = false;
    
    [RelayCommand]
    private void ToggleExtendedLog()
    {
        ExtendedLogEnabled = !ExtendedLogEnabled;
        AppendLog($"Extended Log: {(ExtendedLogEnabled ? "Enabled" : "Disabled")}");
    }
    
    [RelayCommand]
    private void HelpOnline()
    {
        AppendLog($"Opening help online at: {HelpFaqUrl}");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = HelpFaqUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            AppendLog($"Error opening help: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void About()
    {
        AppendLog("About: AvaloniaGRBL - A cross-platform GRBL controller");
        AppendLog($"Visit: {HelpFaqUrl}");
    }
    
    [RelayCommand]
    private void FacebookCommunity()
    {
        AppendLog("Opening Facebook Community...");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = FacebookCommunityUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            AppendLog($"Error opening Facebook: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void Donate()
    {
        AppendLog("Opening donation page...");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = DonateUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            AppendLog($"Error opening donation page: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void License()
    {
        AppendLog("License: GPL-3.0");
        AppendLog("For more information, visit the project repository");
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
