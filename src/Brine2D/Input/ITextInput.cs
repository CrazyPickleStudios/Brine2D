namespace Brine2D.Input;

/// <summary>
///     Represents a text input handler with IME composition support.
/// </summary>
/// <remarks>
///     Implementations should manage text composition state and provide composed text and selection details.
///     Use <see cref="Start" /> to begin capturing text input and <see cref="Stop" /> to end it.
/// </remarks>
public interface ITextInput
{
    /// <summary>
    ///     Gets the current composition string produced by the IME during text input.
    /// </summary>
    string Composition { get; }

    /// <summary>
    ///     Gets the cursor position within the current <see cref="Composition" />.
    /// </summary>
    int CompositionCursor { get; }

    /// <summary>
    ///     Gets the length of the selection within the current <see cref="Composition" />.
    /// </summary>
    int CompositionSelectionLength { get; }

    /// <summary>
    ///     Gets a value indicating whether the input is currently in composition mode.
    /// </summary>
    bool IsComposing { get; }

    /// <summary>
    ///     Gets the committed text captured by the input system.
    /// </summary>
    string Text { get; }

    /// <summary>
    ///     Starts capturing text input and enables composition handling.
    /// </summary>
    void Start();

    /// <summary>
    ///     Stops capturing text input and disables composition handling.
    /// </summary>
    void Stop();
}