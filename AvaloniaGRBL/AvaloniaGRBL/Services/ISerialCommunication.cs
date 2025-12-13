namespace AvaloniaGRBL.Services;

/// <summary>
/// Interface for serial port communication wrapper
/// </summary>
public interface ISerialCommunication
{
    /// <summary>
    /// Configure the serial port with the specified parameters
    /// </summary>
    void Configure(string portName, int baudRate);
    
    /// <summary>
    /// Open the serial port connection
    /// </summary>
    void Open();
    
    /// <summary>
    /// Close the serial port connection
    /// </summary>
    void Close();
    
    /// <summary>
    /// Gets whether the serial port is open
    /// </summary>
    bool IsOpen { get; }
    
    /// <summary>
    /// Write a byte to the serial port
    /// </summary>
    void Write(byte b);
    
    /// <summary>
    /// Write a byte array to the serial port
    /// </summary>
    void Write(byte[] data);
    
    /// <summary>
    /// Write a string to the serial port
    /// </summary>
    void Write(string text);
    
    /// <summary>
    /// Read a line from the serial port (blocking)
    /// </summary>
    string ReadLine();
    
    /// <summary>
    /// Check if there is data available to read
    /// </summary>
    bool HasData { get; }
}
