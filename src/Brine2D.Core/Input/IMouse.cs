using Brine2D.Core.Math;

namespace Brine2D.Core.Input;

/// <summary>
///     Abstraction for mouse input state and control for the current window.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>Coordinates are in window/client space unless otherwise noted.</description>
///         </item>
///         <item>
///             <description>Delta properties and button edge methods reflect changes since the last frame/poll.</description>
///         </item>
///         <item>
///             <description>
///                 Operations that change OS cursor state (visibility, capture, confine, relative mode) typically
///                 require main/UI thread execution and affect the active window associated with this input source.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///     // Toggle relative mode and read deltas
///     if (keyboard.WasKeyPressed(Key.R))
///     {
///         mouse.SetRelativeMouseMode(!mouse.IsRelativeMouseModeEnabled);
///     }
/// 
///     if (mouse.IsRelativeMouseModeEnabled)
///     {
///         var dx = mouse.DeltaX;
///         var dy = mouse.DeltaY;
///         // use dx, dy for camera look, etc.
///     }
/// 
///     // Edge-triggered click
///     if (mouse.WasButtonPressed(MouseButton.Left))
///     {
///         HandleClick(mouse.X, mouse.Y);
///     }
///     </code>
/// </example>
public interface IMouse
{
    /// <summary>
    ///     Cursor X delta (pixels) accumulated since the last frame/poll.
    ///     In relative mode this is the primary position signal.
    /// </summary>
    float DeltaX { get; }

    /// <summary>
    ///     Cursor Y delta (pixels) accumulated since the last frame/poll.
    ///     In relative mode this is the primary position signal.
    /// </summary>
    float DeltaY { get; }

    /// <summary>
    ///     Indicates whether the mouse is currently captured by the window.
    /// </summary>
    bool IsMouseCaptured { get; }

    // Queries

    /// <summary>
    ///     Indicates whether relative mouse mode is currently enabled.
    /// </summary>
    bool IsRelativeMouseModeEnabled { get; }

    /// <summary>
    ///     Horizontal scroll delta accumulated since the last frame/poll.
    ///     Units and sign follow the underlying platform (commonly "ticks" or lines).
    /// </summary>
    float WheelX { get; }

    /// <summary>
    ///     Vertical scroll delta accumulated since the last frame/poll.
    ///     Units and sign follow the underlying platform (commonly "ticks" or lines).
    /// </summary>
    float WheelY { get; }

    /// <summary>
    ///     Current cursor X position in window/client coordinates (pixels).
    /// </summary>
    float X { get; }

    /// <summary>
    ///     Current cursor Y position in window/client coordinates (pixels).
    /// </summary>
    float Y { get; }

    /// <summary>
    ///     Captures or releases the mouse to/from the active window.
    /// </summary>
    /// <param name="enabled">
    ///     True to capture (the window continues receiving mouse input when the cursor leaves while buttons are down, subject
    ///     to platform behavior);
    ///     false to release.
    /// </param>
    void CaptureMouse(bool enabled);

    /// <summary>
    ///     Returns whether the specified <see cref="MouseButton" /> is currently held down (level query).
    /// </summary>
    /// <param name="button">The button to check.</param>
    bool IsButtonDown(MouseButton button);

    /// <summary>
    ///     Confines the mouse cursor to the window's client area.
    /// </summary>
    /// <param name="enabled">True to confine to the window bounds, false to remove confinement.</param>
    void SetConfinedToWindow(bool enabled);

    /// <summary>
    ///     Confines the mouse cursor to a specific rectangle within the window.
    /// </summary>
    /// <param name="rect">
    ///     The confine rectangle in window/client coordinates, or <c>null</c> to remove the confine rectangle.
    /// </param>
    /// <remarks>Overrides window-wide confinement while set.</remarks>
    void SetConfineRect(Rectangle? rect);

    /// <summary>
    ///     Sets the active OS cursor shape.
    /// </summary>
    /// <param name="cursor">The cursor to use.</param>
    void SetCursor(MouseCursor cursor);

    /// <summary>
    ///     Programmatically repositions the cursor within the window's client area.
    /// </summary>
    /// <param name="x">Target X coordinate (pixels).</param>
    /// <param name="y">Target Y coordinate (pixels).</param>
    /// <remarks>May be ignored or clamped by OS policies or confinement settings.</remarks>
    void SetCursorPosition(int x, int y);

    // Cursor/relative mouse control

    /// <summary>
    ///     Shows or hides the OS cursor for the active window.
    /// </summary>
    /// <param name="visible">True to show, false to hide.</param>
    void SetCursorVisible(bool visible);

    /// <summary>
    ///     Sets the system's double-click radius threshold in pixels.
    /// </summary>
    /// <param name="pixels">The maximum cursor movement between clicks to be considered a double-click.</param>
    void SetDoubleClickRadius(float pixels);

    // SDL double-click settings (delegated to SDL/system)

    /// <summary>
    ///     Sets the system's double-click time threshold in milliseconds.
    /// </summary>
    /// <param name="milliseconds">The maximum time between clicks to be considered a double-click.</param>
    void SetDoubleClickTime(uint milliseconds);

    /// <summary>
    ///     Enables or disables relative mouse mode.
    /// </summary>
    /// <param name="enabled">
    ///     True to enable relative mode (cursor is typically hidden and movement is reported as deltas without moving the OS
    ///     cursor);
    ///     false to restore absolute positioning.
    /// </param>
    void SetRelativeMouseMode(bool enabled);

    /// <summary>
    ///     Returns true only on the frame the specified <see cref="MouseButton" /> transitions from up to down (edge-triggered
    ///     press).
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <remarks>Use this for single-shot actions triggered by a press.</remarks>
    bool WasButtonPressed(MouseButton button);

    /// <summary>
    ///     Returns true only on the frame the specified <see cref="MouseButton" /> transitions from down to up (edge-triggered
    ///     release).
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <remarks>Use this for actions triggered on release.</remarks>
    bool WasButtonReleased(MouseButton button);
}