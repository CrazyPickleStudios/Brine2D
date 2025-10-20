namespace Brine2D;

/// <summary>
///     Types of device display orientation.
/// </summary>
public enum DisplayOrientation
{
    /// <summary>
    ///     Orientation cannot be determined.
    /// </summary>
    Unknown,

    /// <summary>
    ///     Landscape orientation.
    /// </summary>
    Landscape,

    /// <summary>
    ///     Landscape orientation (flipped).
    /// </summary>
    LandscapeFlipped,

    /// <summary>
    ///     Portrait orientation.
    /// </summary>
    Portrait,

    /// <summary>
    ///     Portrait orientation (flipped).
    /// </summary>
    PortraitFlipped
}