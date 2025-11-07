using Brine2D.Core.Content;
using Brine2D.Core.Graphics;
using Brine2D.Core.Input;

namespace Brine2D.Core.Hosting;

/// <summary>
///     Aggregates core engine services (content, input, rendering, window, etc.) into a single context.
///     Typically passed to game states/systems and used during the main loop to access per-frame functionality.
/// </summary>
/// <remarks>
///     Unless otherwise documented by a specific service, members are expected to be accessed from the engine's main
///     thread.
///     The context lifetime generally matches the engine lifetime.
/// </remarks>
public interface IEngineContext
{
    /// <summary>
    ///     Central content manager for locating, loading, caching, and unloading assets.
    ///     Provides registration for file providers and asset loaders.
    /// </summary>
    IContentManager Content { get; }

    /// <summary>
    ///     Access to connected gamepads, including a convenience <c>Primary</c> gamepad and indexed retrieval.
    ///     Input values are sampled and updated once per frame.
    /// </summary>
    IGamepads Gamepads { get; }

    /// <summary>
    ///     Keyboard input state for polling keys and transitions.
    ///     Values reflect the state for the current frame.
    /// </summary>
    IKeyboard Input { get; }

    /// <summary>
    ///     Mouse input state and control (position, deltas, wheel, buttons, capture, cursor, etc.).
    ///     Methods that change OS/window cursor state should be called on the main thread.
    /// </summary>
    IMouse Mouse { get; }

    /// <summary>
    ///     Low-level renderer entry point for frame orchestration and draw submission.
    ///     Use <see cref="Sprites" /> for batched 2D sprite rendering when appropriate.
    /// </summary>
    IRenderer Renderer { get; }

    /// <summary>
    ///     2D sprite batch renderer (Begin/Draw/End) for efficient textured quad rendering in screen- or world-space.
    ///     Honors camera transforms when begun with a camera.
    /// </summary>
    ISpriteRenderer Sprites { get; }

    /// <summary>
    ///     The main application window (size, title, close state).
    ///     Useful for querying backbuffer dimensions and reacting to close requests.
    /// </summary>
    IWindow Window { get; }
}