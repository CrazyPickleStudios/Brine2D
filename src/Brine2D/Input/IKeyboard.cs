namespace Brine2D.Input;

/// <summary>
///     Represents a keyboard input source, providing state queries for both hardware scan keys
///     and logical key codes. Intended to be polled each frame for current and transitional states.
/// </summary>
public interface IKeyboard
{
    /// <summary>
    ///     Determines whether the specified scan key is currently held down.
    /// </summary>
    /// <param name="key">The hardware <see cref="ScanKey" /> to query.</param>
    /// <returns><c>true</c> if the key is down; otherwise, <c>false</c>.</returns>
    bool IsDown(ScanKey key);

    /// <summary>
    ///     Determines whether the specified logical key is currently held down.
    /// </summary>
    /// <param name="key">The logical <see cref="KeyCode" /> to query.</param>
    /// <returns><c>true</c> if the key is down; otherwise, <c>false</c>.</returns>
    bool IsDown(KeyCode key);

    /// <summary>
    ///     Determines whether the specified scan key was pressed since the last update tick/frame.
    /// </summary>
    /// <param name="key">The hardware <see cref="ScanKey" /> to query.</param>
    /// <returns><c>true</c> if the key transitioned to down; otherwise, <c>false</c>.</returns>
    bool WasPressed(ScanKey key);

    /// <summary>
    ///     Determines whether the specified logical key was pressed since the last update tick/frame.
    /// </summary>
    /// <param name="key">The logical <see cref="KeyCode" /> to query.</param>
    /// <returns><c>true</c> if the key transitioned to down; otherwise, <c>false</c>.</returns>
    bool WasPressed(KeyCode key);

    /// <summary>
    ///     Determines whether the specified scan key was released since the last update tick/frame.
    /// </summary>
    /// <param name="key">The hardware <see cref="ScanKey" /> to query.</param>
    /// <returns><c>true</c> if the key transitioned to up; otherwise, <c>false</c>.</returns>
    bool WasReleased(ScanKey key);

    /// <summary>
    ///     Determines whether the specified logical key was released since the last update tick/frame.
    /// </summary>
    /// <param name="key">The logical <see cref="KeyCode" /> to query.</param>
    /// <returns><c>true</c> if the key transitioned to up; otherwise, <c>false</c>.</returns>
    bool WasReleased(KeyCode key);
}