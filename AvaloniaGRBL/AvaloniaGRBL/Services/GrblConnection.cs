using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaGRBL.Models;

namespace AvaloniaGRBL.Services;

/// <summary>
/// Service for managing GRBL connection and communication
/// </summary>
public class GrblConnection : IDisposable
{
    private readonly ISerialCommunication _serial;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receiveTask;
    private Task? _statusPollingTask;
    
    public event EventHandler<string>? DataReceived;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<GrblStatus>? StatusReportReceived;
    public event EventHandler<Exception>? ErrorOccurred;
    
    public bool IsConnected => _serial.IsOpen;
    
    public GrblConnection(ISerialCommunication serial)
    {
        _serial = serial;
    }
    
    /// <summary>
    /// Connect to GRBL device
    /// </summary>
    public async Task ConnectAsync(string portName, int baudRate)
    {
        if (IsConnected)
            throw new InvalidOperationException("Already connected");
            
        try
        {
            OnStatusChanged("Connecting...");
            
            _serial.Configure(portName, baudRate);
            _serial.Open();
            
            // Wait a moment for the connection to stabilize
            await Task.Delay(100);
            
            // Start receiving data
            _cancellationTokenSource = new CancellationTokenSource();
            _receiveTask = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token));
            
            // Start status polling
            _statusPollingTask = Task.Run(() => StatusPollingLoop(_cancellationTokenSource.Token));
            
            // Send soft reset to GRBL
            SendCommand("\x18"); // Ctrl-X soft reset
            
            OnStatusChanged("Connected");
        }
        catch (Exception ex)
        {
            OnStatusChanged("Connection failed");
            OnErrorOccurred(ex);
            Disconnect();
            throw;
        }
    }
    
    /// <summary>
    /// Disconnect from GRBL device
    /// </summary>
    public void Disconnect()
    {
        try
        {
            OnStatusChanged("Disconnecting...");
            
            // Stop receive loop and status polling
            _cancellationTokenSource?.Cancel();
            
            // Wait for tasks to complete (don't block too long)
            if (_receiveTask != null)
            {
                try
                {
                    _receiveTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch (AggregateException)
                {
                    // Task was cancelled, this is expected
                }
            }
            
            if (_statusPollingTask != null)
            {
                try
                {
                    _statusPollingTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch (AggregateException)
                {
                    // Task was cancelled, this is expected
                }
            }
            
            // Close serial port
            _serial.Close();
            
            OnStatusChanged("Disconnected");
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _receiveTask = null;
            _statusPollingTask = null;
        }
    }
    
    /// <summary>
    /// Send a command to GRBL
    /// </summary>
    public void SendCommand(string command)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected");
            
        try
        {
            if (!command.EndsWith("\n"))
                command += "\n";
                
            _serial.Write(command);
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            throw;
        }
    }
    
    /// <summary>
    /// Send immediate byte command (realtime commands)
    /// </summary>
    public void SendImmediate(byte command)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected");
            
        try
        {
            _serial.Write(command);
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            throw;
        }
    }
    
    private void ReceiveLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_serial.HasData)
                {
                    string line = _serial.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        line = line.Trim();
                        
                        // Check if this is a status report
                        if (line.StartsWith("<") && line.EndsWith(">"))
                        {
                            var status = GrblStatus.Parse(line);
                            if (status != null)
                            {
                                OnStatusReportReceived(status);
                            }
                        }
                        
                        // Always fire DataReceived for logging
                        OnDataReceived(line);
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
            catch (TimeoutException)
            {
                // Timeout is expected when no data is available
                Thread.Sleep(10);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                OnErrorOccurred(ex);
                break;
            }
        }
    }
    
    protected virtual void OnDataReceived(string data)
    {
        DataReceived?.Invoke(this, data);
    }
    
    protected virtual void OnStatusChanged(string status)
    {
        StatusChanged?.Invoke(this, status);
    }
    
    protected virtual void OnErrorOccurred(Exception exception)
    {
        ErrorOccurred?.Invoke(this, exception);
    }
    
    protected virtual void OnStatusReportReceived(GrblStatus status)
    {
        StatusReportReceived?.Invoke(this, status);
    }
    
    private void StatusPollingLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Query status every 250ms
                Thread.Sleep(250);
                
                if (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    // Send status query (? command) - ASCII 63
                    SendImmediate(63);
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                OnErrorOccurred(ex);
                Thread.Sleep(1000); // Wait a bit longer on error
            }
        }
    }
    
    /// <summary>
    /// Execute jog command in specified direction
    /// </summary>
    /// <param name="direction">Direction to jog</param>
    /// <param name="distance">Distance in mm</param>
    /// <param name="feedRate">Feed rate in mm/min</param>
    public void Jog(JogDirection direction, double distance, double feedRate)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected");
            
        try
        {
            string command = direction switch
            {
                JogDirection.Home => "$H",
                JogDirection.N => FormatJogCommand(0, distance, 0, feedRate),
                JogDirection.S => FormatJogCommand(0, -distance, 0, feedRate),
                JogDirection.E => FormatJogCommand(distance, 0, 0, feedRate),
                JogDirection.W => FormatJogCommand(-distance, 0, 0, feedRate),
                JogDirection.NE => FormatJogCommand(distance, distance, 0, feedRate),
                JogDirection.NW => FormatJogCommand(-distance, distance, 0, feedRate),
                JogDirection.SE => FormatJogCommand(distance, -distance, 0, feedRate),
                JogDirection.SW => FormatJogCommand(-distance, -distance, 0, feedRate),
                JogDirection.Zup => FormatJogCommand(0, 0, distance, feedRate),
                JogDirection.Zdown => FormatJogCommand(0, 0, -distance, feedRate),
                _ => throw new ArgumentException($"Invalid jog direction: {direction}")
            };
            
            SendCommand(command);
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            throw;
        }
    }
    
    /// <summary>
    /// Format a jog command using GRBL $J syntax
    /// </summary>
    private string FormatJogCommand(double x, double y, double z, double feedRate)
    {
        var parts = new List<string> { "$J=G91" }; // Relative positioning
        
        if (x != 0)
            parts.Add($"X{x.ToString("F3", CultureInfo.InvariantCulture)}");
        if (y != 0)
            parts.Add($"Y{y.ToString("F3", CultureInfo.InvariantCulture)}");
        if (z != 0)
            parts.Add($"Z{z.ToString("F3", CultureInfo.InvariantCulture)}");
            
        parts.Add($"F{feedRate.ToString("F0", CultureInfo.InvariantCulture)}");
        
        return string.Join("", parts);
    }
    
    public void Dispose()
    {
        Disconnect();
    }
}
