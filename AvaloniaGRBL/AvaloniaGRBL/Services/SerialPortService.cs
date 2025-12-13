using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace AvaloniaGRBL.Services;

/// <summary>
/// Service for serial port discovery and management
/// </summary>
public static class SerialPortService
{
    /// <summary>
    /// Get a list of available serial port names
    /// </summary>
    public static string[] GetAvailablePorts()
    {
        try
        {
            return SerialPort.GetPortNames();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
    
    /// <summary>
    /// Get common GRBL baud rates
    /// </summary>
    public static int[] GetCommonBaudRates()
    {
        return new[] { 115200, 57600, 38400, 19200, 9600 };
    }
}
