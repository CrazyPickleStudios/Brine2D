namespace Brine2D.Input;

/// <summary>
/// Configuration options for the input system.
/// </summary>
public class InputOptions
{
    /// <summary>
    /// Configuration section name for binding from JSON.
    /// </summary>
    public const string SectionName = "Input";
    
    /// <summary>
    /// Gets or sets whether gamepad support is enabled.
    /// </summary>
    public bool EnableGamepad { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the maximum number of gamepads to support simultaneously.
    /// </summary>
    public int MaxGamepads { get; set; } = 4;
    
    /// <summary>
    /// Gets or sets whether to enable gamepad rumble/haptic feedback.
    /// </summary>
    public bool EnableRumble { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the gamepad dead zone threshold (0.0 to 1.0).
    /// Values below this threshold are treated as zero.
    /// </summary>
    public float GamepadDeadZone { get; set; } = 0.15f;
}