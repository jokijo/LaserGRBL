namespace AvaloniaGRBL.Models;

/// <summary>
/// Represents the direction for jog operations
/// </summary>
public enum JogDirection
{
    /// <summary>Abort current jog</summary>
    Abort,
    
    /// <summary>Home position</summary>
    Home,
    
    /// <summary>North (Y+)</summary>
    N,
    
    /// <summary>South (Y-)</summary>
    S,
    
    /// <summary>West (X-)</summary>
    W,
    
    /// <summary>East (X+)</summary>
    E,
    
    /// <summary>Northwest (X- Y+)</summary>
    NW,
    
    /// <summary>Northeast (X+ Y+)</summary>
    NE,
    
    /// <summary>Southwest (X- Y-)</summary>
    SW,
    
    /// <summary>Southeast (X+ Y-)</summary>
    SE,
    
    /// <summary>Z axis up (Z+)</summary>
    Zup,
    
    /// <summary>Z axis down (Z-)</summary>
    Zdown
}
