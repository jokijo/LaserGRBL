using System;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaGRBL.Services;

/// <summary>
/// Service for managing GRBL connection and communication
/// </summary>
public class GrblConnection : IDisposable
{
    private readonly ISerialCommunication _serial;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receiveTask;
    
    public event EventHandler<string>? DataReceived;
    public event EventHandler<string>? StatusChanged;
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
            
            // Stop receive loop
            _cancellationTokenSource?.Cancel();
            _receiveTask?.Wait(TimeSpan.FromSeconds(2));
            
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
                        OnDataReceived(line.Trim());
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
    
    public void Dispose()
    {
        Disconnect();
    }
}
