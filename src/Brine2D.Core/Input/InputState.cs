namespace Brine2D.Core.Input;

/// <summary>
///     Tracks per-frame keyboard state and exposes edge-triggered queries (pressed/released this frame).
/// </summary>
/// <remarks>
///     <para>Usage:</para>
///     <list type="bullet">
///         <item><description>Call <see cref="Update(IKeyboard)" /> exactly once per frame before reading queries.</description></item>
///         <item><description>Edges are derived from <see cref="IKeyboard.IsKeyDown(Key)" /> only; <see cref="IKeyboard.WasKeyPressed(Key)" /> / <see cref="IKeyboard.WasKeyReleased(Key)" /> are not required.</description></item>
///         <item><description>Complexity is O(K) per <see cref="Update(IKeyboard)" />, where K = number of values in <see cref="Key" />.</description></item>
///     </list>
///     <para>Assumptions: indexes arrays by <c>(int)Key</c>. <see cref="Key" /> must be a dense, zero-based, contiguous enum starting at 0. If that changes, switch to a map (e.g., Dictionary&lt;Key,bool&gt;).</para>
///     <para>Threading: not thread-safe; use from the main thread.</para>
/// </remarks>
/// <example>
///     <code><![CDATA[
/// var keyboard = /* engine keyboard */;
//// Create once
/// var input = new InputState();
///
/// while (running)
/// {
///     // 1) Update the engine keyboard first (implementation-specific)
///     // 2) Then update this tracker
///     input.Update(keyboard);
///
///     if (input.WasPressed(Key.Space))
///         Jump();
///
///     if (input.IsDown(Key.Left))
///         MoveLeft();
///
///     if (input.WasReleased(Key.LeftShift))
///         StopSprinting();
/// }
///     ]]></code>
/// </example>
public sealed class InputState
{
    // Current frame button down state (indexed by (int)Key).
    private readonly bool[] _curr;

    // Cached list of all Key enum values to avoid reallocations per frame.
    private readonly Key[] _keys;

    // Previous frame button down state (indexed by (int)Key).
    private readonly bool[] _prev;

    /// <summary>
    ///     Initializes internal key caches and state buffers sized to the number of keys.
    /// </summary>
    public InputState()
    {
        _keys = Enum.GetValues<Key>();
        _prev = new bool[_keys.Length];
        _curr = new bool[_keys.Length];
    }

    /// <summary>
    ///     Returns whether the key is currently held down (continuous).
    /// </summary>
    /// <param name="key">Key to query.</param>
    /// <returns>True while the key is physically held down.</returns>
    public bool IsDown(Key key)
    {
        return _curr[(int)key];
    }

    /// <summary>
    ///     Updates the internal state using the provided keyboard snapshot.
    /// </summary>
    /// <param name="input">Keyboard provider polled for current key down states.</param>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Copies the previous frame's state into <c>_prev</c>.</description></item>
    ///         <item><description>Fills <c>_curr</c> by querying <see cref="IKeyboard.IsKeyDown(Key)" /> for each known key.</description></item>
    ///         <item><description>Call exactly once per frame and before consuming edge queries.</description></item>
    ///     </list>
    /// </remarks>
    public void Update(IKeyboard input)
    {
        // Shift current state to previous state.
        Array.Copy(_curr, _prev, _curr.Length);

        // Populate current state from the keyboard provider.
        for (var i = 0; i < _keys.Length; i++)
        {
            _curr[i] = input.IsKeyDown(_keys[i]);
        }
    }

    /// <summary>
    ///     Returns true only on the frame the key transitioned from up to down.
    /// </summary>
    /// <param name="key">Key to query.</param>
    /// <returns>True on the first frame the key becomes pressed.</returns>
    public bool WasPressed(Key key)
    {
        return !_prev[(int)key] && _curr[(int)key];
    }

    /// <summary>
    ///     Returns true only on the frame the key transitioned from down to up.
    /// </summary>
    /// <param name="key">Key to query.</param>
    /// <returns>True on the first frame the key becomes released.</returns>
    public bool WasReleased(Key key)
    {
        return _prev[(int)key] && !_curr[(int)key];
    }
}