namespace Brine2D.Core.Input;

/// <summary>
///     Abstraction for querying keyboard input in Brine2D.
/// </summary>
/// <remarks>
///     <para>Edge semantics:</para>
///     <list type="bullet">
///         <item><description><see cref="WasKeyPressed(Key)" />: transition from up to down (true for one frame).</description></item>
///         <item><description><see cref="WasKeyReleased(Key)" />: transition from down to up (true for one frame).</description></item>
///     </list>
///     <para>Usage:</para>
///     <list type="bullet">
///         <item><description>Update the keyboard implementation once per frame (implementation-specific).</description></item>
///         <item><description>Use <see cref="IsKeyDown(Key)" /> for continuous actions (e.g., movement).</description></item>
///         <item><description>Use <see cref="WasKeyPressed(Key)" /> / <see cref="WasKeyReleased(Key)" /> for discrete actions (e.g., toggles).</description></item>
///     </list>
///     <para>Unknown or unsupported keys should return <see langword="false" /> for all queries.</para>
///     <para>Key repeat: whether OS key repeats generate additional “pressed” edges is implementation-defined and may be configurable.</para>
///     <para>Threading: unless stated otherwise, use from the main thread.</para>
/// </remarks>
/// <example>
///     <code>
///     // Continuous input (held)
///     if (keyboard.IsKeyDown(Key.Left))
///     {
///         MoveLeft();
///     }
///
///     // Edge-triggered input (pressed this frame)
///     if (keyboard.WasKeyPressed(Key.Space))
///     {
///         Jump();
///     }
///
///     // Edge-triggered input (released this frame)
///     if (keyboard.WasKeyReleased(Key.LeftShift))
///     {
///         StopSprinting();
///     }
///     </code>
/// </example>
public interface IKeyboard
{
    /// <summary>
    ///     Returns whether the specified key is currently held down.
    /// </summary>
    /// <param name="key">The key to query.</param>
    /// <returns><c>true</c> while the key is physically held down; otherwise, <c>false</c>.</returns>
    bool IsKeyDown(Key key);

    /// <summary>
    ///     Returns <c>true</c> only on the frame the key transitioned from up to down.
    /// </summary>
    /// <param name="key">The key to query.</param>
    /// <returns><c>true</c> on the first frame the key becomes pressed; otherwise, <c>false</c>.</returns>
    /// <remarks>This is an edge-triggered (up to down) test and should be true for a single frame per press.</remarks>
    bool WasKeyPressed(Key key);

    /// <summary>
    ///     Returns <c>true</c> only on the frame the key transitioned from down to up.
    /// </summary>
    /// <param name="key">The key to query.</param>
    /// <returns><c>true</c> on the first frame the key becomes released; otherwise, <c>false</c>.</returns>
    /// <remarks>This is an edge-triggered (down to up) test and should be true for a single frame per release.</remarks>
    bool WasKeyReleased(Key key);
}