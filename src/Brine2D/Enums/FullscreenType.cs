namespace Brine2D
{
    /// <summary>
    /// Types of fullscreen modes.
    /// </summary>
    // TODO: Requires Review
    public enum FullscreenType
    {
        /// <summary>
        /// Sometimes known as borderless fullscreen windowed mode. A borderless screen-sized window is created which sits on top of all desktop UI elements. The window is automatically resized to match the dimensions of the desktop, and its size cannot be changed.
        /// </summary>
        Desktop,
        /// <summary>
        /// Standard exclusive-fullscreen mode. Changes the display mode (actual resolution) of the monitor.
        /// </summary>
        Exclusive,
    }
}
