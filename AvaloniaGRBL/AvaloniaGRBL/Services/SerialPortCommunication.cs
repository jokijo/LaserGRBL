using System;
using System.IO.Ports;

namespace AvaloniaGRBL.Services;

/// <summary>
/// Cross-platform serial port communication implementation using System.IO.Ports
/// </summary>
public class SerialPortCommunication : ISerialCommunication, IDisposable
{
    private SerialPort? _serialPort;
    private string? _portName;
    private int _baudRate;
    private bool _disposed;
    
    public bool IsOpen => _serialPort?.IsOpen ?? false;
    
    public bool HasData => _serialPort?.BytesToRead > 0;
    
    public void Configure(string portName, int baudRate)
    {
        _portName = portName;
        _baudRate = baudRate;
    }
    
    public void Open()
    {
        if (IsOpen)
            return;
            
        if (string.IsNullOrEmpty(_portName))
            throw new InvalidOperationException("Port name not configured");
            
        try
        {
            Close();
            
            _serialPort = new SerialPort
            {
                PortName = _portName,
                BaudRate = _baudRate,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                NewLine = "\n",
                WriteTimeout = 1000,
                ReadTimeout = 1000,
                DtrEnable = false,
                RtsEnable = false
            };
            
            _serialPort.Open();
            
            // Clear any existing data in buffers
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
        }
        catch (Exception ex)
        {
            _serialPort?.Dispose();
            _serialPort = null;
            throw new InvalidOperationException($"Failed to open port {_portName}: {ex.Message}", ex);
        }
    }
    
    public void Close()
    {
        if (_serialPort != null)
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();
                    _serialPort.Close();
                }
            }
            catch
            {
                // Ignore errors during close
            }
            finally
            {
                _serialPort.Dispose();
                _serialPort = null;
            }
        }
    }
    
    public void Write(byte b)
    {
        if (!IsOpen)
            throw new InvalidOperationException("Serial port is not open");
            
        _serialPort!.Write(new byte[] { b }, 0, 1);
    }
    
    public void Write(byte[] data)
    {
        if (!IsOpen)
            throw new InvalidOperationException("Serial port is not open");
            
        _serialPort!.Write(data, 0, data.Length);
    }
    
    public void Write(string text)
    {
        if (!IsOpen)
            throw new InvalidOperationException("Serial port is not open");
            
        _serialPort!.Write(text);
    }
    
    public string ReadLine()
    {
        if (!IsOpen)
            throw new InvalidOperationException("Serial port is not open");
            
        return _serialPort!.ReadLine();
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            Close();
            _disposed = true;
        }
    }
}
