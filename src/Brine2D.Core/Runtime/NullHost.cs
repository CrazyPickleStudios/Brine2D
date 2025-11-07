using Brine2D.Core.Content;
using Brine2D.Core.Content.Loaders;
using Brine2D.Core.Graphics;
using Brine2D.Core.Hosting;
using Brine2D.Core.Input;
using Brine2D.Core.Math;

namespace Brine2D.Core.Runtime;

/// <summary>
///     Headless, no-op game host/engine context suitable for CI, server-side logic tests, or content pipeline tasks.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>No window is created (<see cref="IWindow" /> width/height are 0; <see cref="IRenderer" /> does nothing).</description></item>
///         <item><description>Input devices return neutral state (keyboard/mouse/gamepads are inert).</description></item>
///         <item><description>Content system is available and functional with file-based providers/loaders as configured.</description></item>
///         <item><description>An optional auto-quit timeout can terminate the loop automatically (useful for tests).</description></item>
///     </list>
///     <para>Threading: use from the engine's main thread.</para>
/// </remarks>
/// <example>
///     <code>
///     // Run a headless game for up to 5 seconds
///     IGameHost host = new NullHost(autoQuitAfterSeconds: 5);
///     host.Run(new MyGame());
///     </code>
/// </example>
public sealed class NullHost : IGameHost, IEngineContext, IWindow, IRenderer, IKeyboard, IMouse
{
    /// <summary>
    ///     Optional limit in seconds to stop the loop automatically; when <c>null</c>, runs until the game stops itself.
    /// </summary>
    private readonly double? _autoQuitAfterSeconds;

    /// <summary>
    ///     Content manager for loading text/bytes (and any other loaders the caller registers).
    /// </summary>
    private readonly ContentManager _content = new();

    /// <summary>
    ///     Gamepad collection stub that always reports 0 devices and never raises events.
    /// </summary>
    private readonly NullGamepads _gamepads = new();

    /// <summary>
    ///     Sanitized game loop options used by the fixed-timestep loop.
    /// </summary>
    private readonly GameLoopOptions _options;

    /// <summary>
    ///     Sprite renderer stub that ignores all draw calls.
    /// </summary>
    private readonly NullSpriteRenderer _sprites = new();

    /// <summary>
    ///     Creates a new headless null host.
    /// </summary>
    /// <param name="options">
    ///     Fixed-timestep loop configuration. If null, defaults to 60 FPS, max 6 updates per frame,
    ///     delta clamp 0.25s, and sleeping enabled.
    /// </param>
    /// <param name="autoQuitAfterSeconds">
    ///     Optional time in seconds after which the loop terminates automatically. Defaults to 5 seconds.
    ///     Use <c>null</c> to disable and let the game control exit.
    /// </param>
    public NullHost(GameLoopOptions? options = null, double? autoQuitAfterSeconds = 5.0)
    {
        _options = options ?? new GameLoopOptions(60, 6);
        _autoQuitAfterSeconds = autoQuitAfterSeconds;

        // Basic on-disk content roots and simple text/bytes loaders for convenience in tests/tools.
        _content.AddFileProvider(new PhysicalFileProvider("Content", "Assets"));
        _content.AddLoader(new StringLoader());
        _content.AddLoader(new BytesLoader());
    }

    /// <inheritdoc />
    public IContentManager Content => _content;

    /// <summary>
    ///     Gamepad manager. Always empty and inert.
    /// </summary>
    public IGamepads Gamepads => _gamepads;

    /// <inheritdoc />
    public int Height => 0;

    /// <summary>
    ///     Keyboard input abstraction (always neutral).
    /// </summary>
    public IKeyboard Input => this;

    /// <summary>
    ///     Always false; the null host does not receive close requests.
    /// </summary>
    public bool IsClosing => false;

    /// <summary>
    ///     Mouse input source for this host. Always returns neutral values and ignores state changes.
    /// </summary>
    public IMouse Mouse => this;

    /// <summary>
    ///     Renderer abstraction (no-op).
    /// </summary>
    public IRenderer Renderer => this;

    /// <summary>
    ///     Sprite renderer interface. All calls are no-ops.
    /// </summary>
    public ISpriteRenderer Sprites => _sprites;

    /// <summary>
    ///     Logical window title. Read-only fixed string for this host.
    /// </summary>
    public string Title
    {
        get => "Brine2D Server";
        set { }
    }

    // IWindow
    /// <inheritdoc />
    public int Width => 0;

    // IEngineContext
    /// <summary>
    ///     Window abstraction (no actual OS window; dimensions are 0).
    /// </summary>
    public IWindow Window => this;

    float IMouse.DeltaX => 0;
    float IMouse.DeltaY => 0;
    bool IMouse.IsMouseCaptured => false;
    bool IMouse.IsRelativeMouseModeEnabled => false;
    float IMouse.WheelX => 0;
    float IMouse.WheelY => 0;

    // IMouse implementation (no-op)
    float IMouse.X => 0;
    float IMouse.Y => 0;

    // IRenderer
    /// <summary>
    ///     Clears the backbuffer. No-op in null host.
    /// </summary>
    public void Clear(Color color)
    {
    }

    // IInput (keyboard)
    /// <inheritdoc />
    public bool IsKeyDown(Key key)
    {
        return false;
    }

    /// <summary>
    ///     Presents the frame. No-op in null host.
    /// </summary>
    public void Present()
    {
    }

    /// <summary>
    ///     Initializes the game and runs a fixed-timestep loop until requested to stop.
    ///     - Calls <see cref="IGame.Initialize(IEngineContext)" /> once.
    ///     - For each iteration: updates input edge state, calls <c>Update</c>; then calls <c>Draw</c> once per frame.
    ///     - Stops when <paramref name="game" /> requests it (by returning false from update via game logic) or
    ///     when <see cref="_autoQuitAfterSeconds" /> elapses (if configured).
    /// </summary>
    /// <param name="game">Game instance to run.</param>
    public void Run(IGame game)
    {
        // One-time game initialization with this headless engine context.
        game.Initialize(this);

        var loop = new GameLoop(_options);
        loop.Run(
            gt =>
            {
                // Advance input/gamepad edge states for the new frame (no-op here).
                _gamepads.BeginFrame();

                // Let the game simulate using a fixed delta (as configured in GameLoopOptions).
                game.Update(gt);

                // Optional auto-quit guard for CI/tests to avoid infinite loops.
                if (_autoQuitAfterSeconds is double limit && gt.TotalSeconds >= limit)
                {
                    return false;
                }

                return true;
            },
            gt =>
            {
                // Render phase: in the null host this does nothing visually,
                // but allows the game to exercise its draw path safely.
                game.Draw(gt);
            });
    }

    /// <inheritdoc />
    public bool WasKeyPressed(Key key)
    {
        return false;
    }

    /// <inheritdoc />
    public bool WasKeyReleased(Key key)
    {
        return false;
    }

    void IMouse.CaptureMouse(bool enabled)
    {
    }

    bool IMouse.IsButtonDown(MouseButton b)
    {
        return false;
    }

    void IMouse.SetConfinedToWindow(bool enabled)
    {
    }

    void IMouse.SetConfineRect(Rectangle? rect)
    {
    }

    void IMouse.SetCursor(MouseCursor cursor)
    {
    }

    void IMouse.SetCursorPosition(int x, int y)
    {
    }

    void IMouse.SetCursorVisible(bool visible)
    {
    }

    void IMouse.SetDoubleClickRadius(float pixels)
    {
    }

    void IMouse.SetDoubleClickTime(uint milliseconds)
    {
    }

    void IMouse.SetRelativeMouseMode(bool enabled)
    {
    }

    bool IMouse.WasButtonPressed(MouseButton b)
    {
        return false;
    }

    bool IMouse.WasButtonReleased(MouseButton b)
    {
        return false;
    }

    /// <summary>
    ///     Inert gamepad collection used by <see cref="NullHost" />.
    ///     Always reports zero pads and never raises connection events.
    /// </summary>
    private sealed class NullGamepads : IGamepads
    {
        /// <inheritdoc />
        public int Count => 0;

        /// <inheritdoc />
        public IGamepad? Primary => null;

        /// <summary>
        ///     Advances per-frame state. No-op in the null host.
        /// </summary>
        public void BeginFrame()
        {
        }

        /// <inheritdoc />
        public IGamepad? Get(int index)
        {
            return null;
        }

#pragma warning disable CS0067 // event is never used (no-op null host)
        /// <summary>
        ///     Never raised in the null host.
        /// </summary>
        public event Action<int>? OnConnected;

        /// <summary>
        ///     Never raised in the null host.
        /// </summary>
        public event Action<int>? OnDisconnected;
#pragma warning restore CS0067
    }
}