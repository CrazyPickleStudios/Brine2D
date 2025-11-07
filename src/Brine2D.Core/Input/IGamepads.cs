namespace Brine2D.Core.Input;

/// <summary>
///     Provides access to connected gamepads and manages per-frame edge state
///     (e.g., WasButtonPressed/WasButtonReleased) for all pads.
/// </summary>
/// <remarks>
///     <para>Usage:</para>
///     <list type="bullet">
///         <item><description>Query <see cref="Count" />, <see cref="Primary" />, or <see cref="Get(int)" /> to read current devices.</description></item>
///         <item><description>Call <see cref="BeginFrame" /> exactly once per frame before polling input for that frame.</description></item>
///         <item><description>Subscribe to <see cref="OnConnected" /> and <see cref="OnDisconnected" /> to react to hot-plug events.</description></item>
///     </list>
///     <para>Indexing:</para>
///     <list type="bullet">
///         <item><description>Gamepads are addressed by a 0-based index.</description></item>
///         <item><description>Index meaning and ordering are implementation-defined; many implementations keep a stable order of the first N connected devices.</description></item>
///     </list>
///     <para>Threading:</para>
///     <list type="bullet">
///         <item><description>Unless specified otherwise, use from the main (game) thread.</description></item>
///         <item><description>Event callbacks are raised on the thread that detected the change.</description></item>
///     </list>
/// </remarks>
public interface IGamepads
{
    /// <summary>
    ///     Raised when a gamepad becomes connected.
    /// </summary>
    /// <remarks>The argument is the 0-based index of the connected gamepad.</remarks>
    event Action<int>? OnConnected;

    /// <summary>
    ///     Raised when a gamepad becomes disconnected.
    /// </summary>
    /// <remarks>The argument is the 0-based index of the disconnected gamepad.</remarks>
    event Action<int>? OnDisconnected;

    /// <summary>Gets the number of currently connected gamepads.</summary>
    int Count { get; }

    /// <summary>
    ///     Gets the first connected gamepad, or <c>null</c> if none are connected.
    ///     Convenience accessor for the default gamepad in single-player scenarios.
    /// </summary>
    IGamepad? Primary { get; }

    /// <summary>
    ///     Advances per-frame input state and resets edge detections for all gamepads.
    ///     Must be called exactly once per frame by the host before input is polled for that frame.
    /// </summary>
    void BeginFrame();

    /// <summary>
    ///     Gets the gamepad at the specified 0-based <paramref name="index" />.
    /// </summary>
    /// <param name="index">The 0-based gamepad index.</param>
    /// <returns>
    ///     The <see cref="IGamepad" /> at the specified index, or <c>null</c> if the index is out of range
    ///     or no gamepad is present at that index.
    /// </returns>
    IGamepad? Get(int index);
}