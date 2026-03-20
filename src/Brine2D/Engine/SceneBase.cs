using Brine2D.Audio;
using Brine2D.Core;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Common base for all scene types. Holds framework-injected services and lifecycle hooks.
/// Not inheritable outside the engine assembly.
/// </summary>
public abstract class SceneBase
{
    private ILogger? _logger;
    private IRenderer? _renderer;
    private IInputContext? _input;
    private IAudioService? _audio;
    private IGameContext? _game;
    private bool _isUnloading;

    private protected static InvalidOperationException NotReady(string name) => new(
        $"'{name}' is not available in the scene constructor. " +
        $"Override {nameof(OnLoadAsync)}() or {nameof(OnEnter)}() to access framework properties.");

    private protected static InvalidOperationException NotAvailableDuringUnload(string name) => new(
        $"'{name}' is not available during {nameof(OnUnloadAsync)}. " +
        $"Use {nameof(OnExit)}() for any cleanup that requires main-thread services.");

    /// <summary>
    /// Logger for this scene. Available from OnLoadAsync onwards.
    /// Also available during <see cref="OnUnloadAsync"/>; logging is thread-safe and does not
    /// depend on SDL3-backed services.
    /// </summary>
    protected internal ILogger Logger
    {
        get => _logger ?? throw NotReady(nameof(Logger));
        internal set => _logger = value;
    }

    /// <summary>
    /// Renderer for this scene. Available from OnLoadAsync onwards.
    /// Not available during <see cref="OnUnloadAsync"/> — use <see cref="OnExit"/> for renderer cleanup.
    /// </summary>
    protected internal IRenderer Renderer
    {
        get
        {
            if (_isUnloading) throw NotAvailableDuringUnload(nameof(Renderer));
            return _renderer ?? throw NotReady(nameof(Renderer));
        }
        internal set => _renderer = value;
    }

    /// <summary>
    /// Input context for this scene. Available from OnLoadAsync onwards.
    /// Not available during <see cref="OnUnloadAsync"/> — use <see cref="OnExit"/> for input cleanup.
    /// </summary>
    protected internal IInputContext Input
    {
        get
        {
            if (_isUnloading) throw NotAvailableDuringUnload(nameof(Input));
            return _input ?? throw NotReady(nameof(Input));
        }
        internal set => _input = value;
    }

    /// <summary>
    /// Audio service for this scene. Available from OnLoadAsync onwards.
    /// Not available during <see cref="OnUnloadAsync"/> — use <see cref="OnExit"/> for audio cleanup.
    /// </summary>
    protected internal IAudioService Audio
    {
        get
        {
            if (_isUnloading) throw NotAvailableDuringUnload(nameof(Audio));
            return _audio ?? throw NotReady(nameof(Audio));
        }
        internal set => _audio = value;
    }

    /// <summary>
    /// Game context for this scene. Available from OnLoadAsync onwards.
    /// Also available during <see cref="OnUnloadAsync"/>; game context does not depend on
    /// SDL3-backed services.
    /// </summary>
    protected internal IGameContext Game
    {
        get => _game ?? throw NotReady(nameof(Game));
        internal set => _game = value;
    }

    /// <summary>
    /// Gets the display name of this scene. Defaults to the class name.
    /// Override to provide a custom name for logging, debug overlays, or save/load systems.
    /// </summary>
    public virtual string Name => GetType().Name;
    
    private protected SceneBase() { }

    /// <summary>
    /// Called by SceneManager after <see cref="OnExit"/> and before <see cref="OnUnloadAsync"/>
    /// to prevent accidental access to SDL3-backed services from background threads.
    /// <see cref="Logger"/> and <see cref="Game"/> remain accessible after this point.
    /// </summary>
    internal void BeginUnload() => _isUnloading = true;

    /// <summary>
    /// Called when the scene is being loaded. Override to load assets.
    /// Always pass <paramref name="cancellationToken"/> to async asset-loading calls.
    /// </summary>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <param name="progress">
    /// Reports scene loading progress (0.0–1.0) to the active loading screen.
    /// Call <c>progress?.Report(value)</c> at meaningful milestones during your load.
    /// The framework maps the reported value onto the loading screen's asset-loading range
    /// automatically. Is <see langword="null"/> when no loading screen is displayed.
    /// </param>
    protected internal virtual Task OnLoadAsync(CancellationToken cancellationToken, IProgress<float>? progress = null) => Task.CompletedTask;

    /// <summary>
    /// Called when entering the scene (after loading, before first update).
    /// Use this to initialize scene state, start music, or perform any one-time setup.
    /// Called on the main thread.
    /// </summary>
    protected internal virtual void OnEnter() { }

    /// <summary>Called every frame to update game logic.</summary>
    protected internal virtual void OnUpdate(GameTime gameTime) { }

    /// <summary>
    /// Called at a fixed timestep for deterministic simulation logic.
    /// Runs zero or more times per frame depending on accumulated time.
    /// </summary>
    protected internal virtual void OnFixedUpdate(GameTime fixedTime) { }

    /// <summary>
    /// Called every frame to render visuals, after all ECS systems have rendered via <c>World.Render</c>.
    /// Use this for overlays, HUD, or anything that must appear on top of ECS-rendered content.
    /// </summary>
    protected internal virtual void OnRender(GameTime gameTime) { }

    /// <summary>
    /// Called when exiting the scene (before unloading).
    /// Called on the main thread. SDL3-backed services (Renderer, Input, Audio) are still
    /// available here — use this for any cleanup that requires them.
    /// </summary>
    protected internal virtual void OnExit() { }

    /// <summary>
    /// Called during unloading. Override to clean up resources.
    /// Always pass <paramref name="cancellationToken"/> to async cleanup calls.
    /// Called on a background thread — do not call SDL3-backed services (Renderer, Audio, Input) here.
    /// Use <see cref="OnExit"/> for any main-thread cleanup that must precede unloading.
    /// </summary>
    protected internal virtual Task OnUnloadAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}