using Brine2D.Filesystem;

namespace Brine2D.Mouse;

/// <summary>
///     Provides an interface to the user's mouse.
/// </summary>
public sealed class MouseModule
{
    /// <summary>
    ///     Gets the current Cursor.
    /// </summary>
    /// <returns>
    ///     The current cursor, or nil if no cursor is set.
    /// </returns>
    public Cursor GetCursor()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Returns the current position of the mouse.
    /// </summary>
    /// <returns>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>x</term><description>The position of the mouse along the x-axis.</description>
    ///         </item>
    ///         <item>
    ///             <term>y</term><description>The position of the mouse along the y-axis.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    public (double x, double y) GetPosition()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Gets whether relative mode is enabled for the mouse.</para>
    ///     <para>
    ///         If relative mode is enabled, the cursor is hidden and doesn't move when the mouse does, but relative mouse
    ///         motion events are still generated via love.mousemoved. This lets the mouse move in any direction indefinitely
    ///         without the cursor getting stuck at the edges of the screen.
    ///     </para>
    ///     <para>
    ///         The reported position of the mouse is not updated while relative mode is enabled, even when relative mouse
    ///         motion events are generated.
    ///     </para>
    /// </summary>
    /// <returns>
    ///     True if relative mode is enabled, false if it's disabled.
    /// </returns>
    public bool GetRelativeMode()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Gets a Cursor object representing a system-native hardware cursor.</para>
    ///     <para>
    ///         Hardware cursors are framerate-independent and work the same way as normal operating system cursors. Unlike
    ///         drawing an image at the mouse's current coordinates, hardware cursors never have visible lag between when the
    ///         mouse is moved and when the cursor position updates, even at low framerates.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     The "image" CursorType is not a valid argument. Use love.mouse.newCursor to create a hardware cursor using a
    ///     custom image.
    /// </remarks>
    /// <param name="ctype">The type of system cursor to get.</param>
    /// <returns>
    ///     The Cursor object representing the system cursor type.
    /// </returns>
    public Cursor GetSystemCursor(CursorType ctype)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Returns the current x-position of the mouse.
    /// </summary>
    /// <returns>
    ///     The position of the mouse along the x-axis.
    /// </returns>
    public double GetX()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Returns the current y-position of the mouse.
    /// </summary>
    /// <returns>
    ///     The position of the mouse along the y-axis.
    /// </returns>
    public double GetY()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Gets whether cursor functionality is supported.</para>
    ///     <para>
    ///         If it isn't supported, calling love.mouse.newCursor and love.mouse.getSystemCursor will cause an error. Some
    ///         mobile devices do not support cursors.
    ///     </para>
    /// </summary>
    /// <returns>
    ///     Whether the system has cursor functionality.
    /// </returns>
    public bool IsCursorSupported()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Checks whether a certain mouse button is down.</para>
    ///     <para>
    ///         This function does not detect mouse wheel scrolling; you must use the love.wheelmoved (or love.mousepressed
    ///         in version 0.9.2 and older) callback for that.
    ///     </para>
    /// </summary>
    /// <param name="button">
    ///     The index of a button to check. 1 is the primary mouse button, 2 is the secondary mouse button and
    ///     3 is the middle button. Further buttons are mouse dependant.
    /// </param>
    /// <param name="buttons">Additional button indices to check.</param>
    /// <returns>True if any specified button is down.</returns>
    public bool IsDown(double button, params double[] buttons)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Checks whether a certain mouse button is down.</para>
    ///     <para>
    ///         This function does not detect mouse wheel scrolling; you must use the love.wheelmoved (or love.mousepressed
    ///         in version 0.9.2 and older) callback for that.
    ///     </para>
    /// </summary>
    /// <param name="buttons">Enumerable containing indices of mouse buttons to check.</param>
    /// <returns>True if any specified button is down.</returns>
    public bool IsDown(IEnumerable<double> buttons)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Checks if the mouse is grabbed.</para>
    /// </summary>
    /// <returns>
    ///     True if the cursor is grabbed, false if it is not.
    /// </returns>
    public bool IsGrabbed()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Checks if the cursor is visible.</para>
    /// </summary>
    /// <returns>
    ///     True if the cursor to visible, false if the cursor is hidden.
    /// </returns>
    public bool IsVisible()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Creates a new hardware Cursor object from an image file or ImageData.</para>
    ///     <para>
    ///         Hardware cursors are framerate-independent and work the same way as normal operating system cursors. Unlike
    ///         drawing an image at the mouse's current coordinates, hardware cursors never have visible lag between when the
    ///         mouse is moved and when the cursor position updates, even at low framerates.
    ///     </para>
    ///     <para>
    ///         The hot spot is the point the operating system uses to determine what was clicked and at what position the
    ///         mouse cursor is. For example, the normal arrow pointer normally has its hot spot at the top left of the image,
    ///         but a crosshair cursor might have it in the middle.
    ///     </para>
    /// </summary>
    /// <param name="imageData">The ImageData to use for the new Cursor.</param>
    /// <param name="hotx">The x-coordinate in the ImageData of the cursor's hot spot.</param>
    /// <param name="hoty">The y-coordinate in the ImageData of the cursor's hot spot.</param>
    /// <returns>
    ///     The new Cursor object.
    /// </returns>
    public Cursor NewCursor(ImageData imageData, double hotx = 0, double hoty = 0)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Creates a new hardware Cursor object from an image file or ImageData.</para>
    ///     <para>
    ///         Hardware cursors are framerate-independent and work the same way as normal operating system cursors. Unlike
    ///         drawing an image at the mouse's current coordinates, hardware cursors never have visible lag between when the
    ///         mouse is moved and when the cursor position updates, even at low framerates.
    ///     </para>
    ///     <para>
    ///         The hot spot is the point the operating system uses to determine what was clicked and at what position the
    ///         mouse cursor is. For example, the normal arrow pointer normally has its hot spot at the top left of the image,
    ///         but a crosshair cursor might have it in the middle.
    ///     </para>
    /// </summary>
    /// <param name="filename">Path to the image to use for the new Cursor.</param>
    /// <param name="hotx">The x-coordinate in the image of the cursor's hot spot.</param>
    /// <param name="hoty">The y-coordinate in the image of the cursor's hot spot.</param>
    /// <returns>
    ///     The new Cursor object.
    /// </returns>
    public Cursor NewCursor(string filename, double hotx = 0, double hoty = 0)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Creates a new hardware Cursor object from an image file or ImageData.</para>
    ///     <para>
    ///         Hardware cursors are framerate-independent and work the same way as normal operating system cursors. Unlike
    ///         drawing an image at the mouse's current coordinates, hardware cursors never have visible lag between when the
    ///         mouse is moved and when the cursor position updates, even at low framerates.
    ///     </para>
    ///     <para>
    ///         The hot spot is the point the operating system uses to determine what was clicked and at what position the
    ///         mouse cursor is. For example, the normal arrow pointer normally has its hot spot at the top left of the image,
    ///         but a crosshair cursor might have it in the middle.
    ///     </para>
    /// </summary>
    /// <param name="fileData">Data representing the image to use for the new Cursor.</param>
    /// <param name="hotx">The x-coordinate in the image of the cursor's hot spot.</param>
    /// <param name="hoty">The y-coordinate in the image of the cursor's hot spot.</param>
    /// <returns>
    ///     The new Cursor object.
    /// </returns>
    public Cursor NewCursor(FileData fileData, double hotx = 0, double hoty = 0)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Sets the current mouse cursor.
    /// </summary>
    /// <param name="cursor">The Cursor object to use as the current mouse cursor.</param>
    public void SetCursor(Cursor cursor)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Sets the current mouse cursor.
    /// </summary>
    /// <remarks>Resets the current mouse cursor to the default.</remarks>
    public void SetCursor()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Grabs the mouse and confines it to the window.
    /// </summary>
    /// <param name="grab">True to confine the mouse, false to let it leave the window.</param>
    public void SetGrabbed(bool grab)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Sets the current position of the mouse. Non-integer values are floored.
    /// </summary>
    /// <param name="x">The new position of the mouse along the x-axis.</param>
    /// <param name="y">The new position of the mouse along the y-axis.</param>
    public void SetPosition(double x, double y)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Sets whether relative mode is enabled for the mouse.</para>
    ///     <para>
    ///         When relative mode is enabled, the cursor is hidden and doesn't move when the mouse does, but relative mouse
    ///         motion events are still generated via love.mousemoved. This lets the mouse move in any direction indefinitely
    ///         without the cursor getting stuck at the edges of the screen.
    ///     </para>
    ///     <para>
    ///         The reported position of the mouse may not be updated while relative mode is enabled, even when relative
    ///         mouse motion events are generated.
    ///     </para>
    /// </summary>
    /// <param name="enable">True to enable relative mode, false to disable it.</param>
    public void SetRelativeMode(bool enable)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Sets the current visibility of the cursor.
    /// </summary>
    /// <param name="visible">True to set the cursor to visible, false to hide the cursor.</param>
    public void SetVisible(bool visible)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Sets the current X position of the mouse.</para>
    ///     <para>Non-integer values are floored.</para>
    /// </summary>
    /// <param name="x">The new position of the mouse along the x-axis.</param>
    public void SetX(double x)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Sets the current Y position of the mouse.</para>
    ///     <para>Non-integer values are floored.</para>
    /// </summary>
    /// <param name="y">The new position of the mouse along the y-axis.</param>
    public void SetY(double y)
    {
        throw new NotImplementedException();
    }
}