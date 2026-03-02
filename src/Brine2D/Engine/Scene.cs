using System.Reflection;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Rendering;
using Brine2D.Input;
using Brine2D.Audio;
using Brine2D.Systems.Rendering;
using Brine2D.Systems.Physics;
using Brine2D.Systems.Audio;
using Brine2D.Systems.Collision;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Base class for game scenes. Override OnLoadAsync, OnEnter, OnUpdate, OnRender,
/// OnExit, and OnUnloadAsync to implement scene logic.
/// Framework properties (Logger, World, Renderer, Input, Audio, Game)
/// are set automatically before OnLoadAsync is called.
/// </summary>
public abstract class Scene
{
    private ILogger? _logger;
    private IEntityWorld? _world;
    private IRenderer? _renderer;
    private IInputContext? _input;
    private IAudioService? _audio;
    private IGameContext? _game;

    // Cached at construction so Name never triggers reflection at runtime.
    private readonly string _name;

    private static InvalidOperationException NotReady(string name) => new(
        $"'{name}' is not available in the Scene constructor. " +
        $"Override {nameof(OnLoadAsync)}() or {nameof(OnEnter)}() to access framework properties. " +
        $"If you need a logger in the constructor, declare ILogger<YourScene> as a constructor parameter instead.");

    /// <summary>Logger for this scene. Available from OnLoadAsync onwards.</summary>
    protected internal ILogger Logger
    {
        get => _logger ?? throw NotReady(nameof(Logger));
        internal set => _logger = value;
    }

    /// <summary>Entity world for this scene. Available from OnLoadAsync onwards.</summary>
    public IEntityWorld World
    {
        get => _world ?? throw NotReady(nameof(World));
        internal set => _world = value;
    }

    /// <summary>Renderer for this scene. Available from OnLoadAsync onwards.</summary>
    protected internal IRenderer Renderer
    {
        get => _renderer ?? throw NotReady(nameof(Renderer));
        internal set => _renderer = value;
    }

    /// <summary>Input context for this scene. Available from OnLoadAsync onwards.</summary>
    protected internal IInputContext Input
    {
        get => _input ?? throw NotReady(nameof(Input));
        internal set => _input = value;
    }

    /// <summary>Audio service for this scene. Available from OnLoadAsync onwards.</summary>
    protected internal IAudioService Audio
    {
        get => _audio ?? throw NotReady(nameof(Audio));
        internal set => _audio = value;
    }

    /// <summary>Game context for this scene. Available from OnLoadAsync onwards.</summary>
    protected internal IGameContext Game
    {
        get => _game ?? throw NotReady(nameof(Game));
        internal set => _game = value;
    }

    /// <summary>
    /// Gets the display name of this scene.
    /// Defaults to the class name, or the value specified in <see cref="SceneAttribute.Name"/>
    /// if the class is annotated with <c>[Scene("MyName")]</c>.
    /// Override to provide a fully dynamic name at runtime.
    /// </summary>
    public virtual string Name => _name;

    /// <summary>
    /// Gets whether frame management (BeginFrame/EndFrame) happens automatically.
    /// Default is true. Set to false for manual control over rendering passes.
    /// </summary>
    public virtual bool EnableAutomaticFrameManagement { get; set; } = true;

    /// <summary>
    /// Constructs a scene.
    /// Framework properties (Logger, World, Renderer, Input, Audio, Game) are set automatically by SceneManager.
    /// Override and add your own constructor parameters for custom dependencies you need.
    /// </summary>
    protected Scene()
    {
        // Read the [Scene] attribute once at construction. Name and EnableAutomaticFrameManagement
        // are both cached here so neither property triggers reflection at runtime.
        var attribute = GetType().GetCustomAttribute<SceneAttribute>();
        _name = attribute?.Name ?? GetType().Name;
        if (attribute != null)
            EnableAutomaticFrameManagement = attribute.EnableAutomaticFrameManagement;
    }

    #region Lifecycle Methods

    /// <summary>
    /// Called when the scene is being loaded. Override to load assets.
    /// </summary>
    protected internal virtual Task OnLoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Called when entering the scene (after loading, before first update).
    /// Use this to initialize scene logic - spawn entities, start music, etc.
    /// Default systems are already added to World by the framework.
    /// </summary>
    /// <example>
    /// <code>
    /// protected override void OnEnter()
    /// {
    ///     // Spawn entities
    ///     var player = World.CreateEntity("Player")
    ///         .AddComponent&lt;TransformComponent&gt;()
    ///         .AddBehavior&lt;PlayerMovementBehavior&gt;();
    ///     
    ///     // Start music
    ///     Audio.PlayMusic("theme.ogg");
    ///     
    ///     // Disable systems you don't need
    ///     World.GetSystem&lt;ParticleSystem&gt;()!.IsEnabled = false;
    /// }
    /// </code>
    /// </example>
    protected internal virtual void OnEnter()
    {
        // Override to add scene initialization
    }

    /// <summary>
    /// Called every frame to update game logic.
    /// </summary>
    protected internal virtual void OnUpdate(GameTime gameTime) { }

    /// <summary>
    /// Called every frame to render visuals.
    /// </summary>
    protected internal virtual void OnRender(GameTime gameTime) { }

    /// <summary>
    /// Called when exiting the scene (before unloading).
    /// Use for cleanup logic (stopping audio, clearing state, etc.).
    /// </summary>
    protected internal virtual void OnExit() { }

    /// <summary>
    /// Called during unloading. Override to clean up resources.
    /// </summary>
    protected internal virtual Task OnUnloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    #endregion
}
