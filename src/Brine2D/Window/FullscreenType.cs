namespace Brine2D.Window;

/// <summary>
///     Types of fullscreen modes.
/// </summary>
public enum FullscreenType
{
    /// <summary>
    ///     <para>Sometimes known as borderless fullscreen windowed mode.</para>
    ///     <para>
    ///         A borderless screen-sized window is created which sits on top of all desktop UI elements. The window is
    ///         automatically resized to match the dimensions of the desktop, and its size cannot be changed.
    ///     </para>
    /// </summary>
    Desktop,

    /// <summary>
    ///     Standard exclusive-fullscreen mode. Changes the display mode (actual resolution) of the monitor.
    /// </summary>
    Exclusive
}