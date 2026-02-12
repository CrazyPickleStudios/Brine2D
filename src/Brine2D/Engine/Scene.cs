using System.Reflection;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Engine.Systems;
using Brine2D.Rendering;
using Brine2D.Input;
using Brine2D.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Base class for game scenes.
/// </summary>
/// <remarks>
/// <para><strong>Lifecycle Order:</strong></para>
/// <list type="number">
/// <item><description>OnLoadAsync - Load assets (textures, audio, etc.)</description></item>
/// <item><description>OnEnter - Initialize scene logic (start music, spawn entities, etc.)</description></item>
/// <item><description>OnUpdate/OnRender - Game loop</description></item>
/// <item><description>OnExit - Cleanup logic (stop music, save state, etc.)</description></item>
/// <item><description>OnUnloadAsync - Unload assets</description></item>
/// </list>
/// 
/// <para><strong>Framework Properties (automatically set):</strong></para>
/// <list type="bullet">
/// <item><see cref="Logger"/> - Scoped logger instance</item>
/// <item><see cref="World"/> - Scene-scoped entity world</item>
/// <item><see cref="Renderer"/> - Renderer for drawing</item>
/// <item><see cref="Input"/> - Input context (convenience property)</item>
/// <item><see cref="Audio"/> - Audio service (convenience property)</item>
/// <item><see cref="Game"/> - Game context (convenience property)</item>
/// </list>
/// 
/// <para><strong>Constructor Injection (for custom services):</strong></para>
/// <code>
/// public class GameScene : Scene
/// {
///     private readonly IPlayerService _playerService;
///     
///     // Inject YOUR custom services
///     public GameScene(IPlayerService playerService)
///     {
///         _playerService = playerService;
///     }
/// }
/// </code>
/// </remarks>
public abstract class Scene
{
    /// <summary>
    /// Logger for this scene. Set automatically by the framework.
    /// </summary>
    protected internal ILogger Logger { get; internal set; } = null!;

    /// <summary>
    /// Entity world for this scene. Set automatically by the framework.
    /// Each scene gets its own isolated world.
    /// </summary>
    public IEntityWorld World { get; internal set; } = null!;

    /// <summary>
    /// Renderer for this scene. Set automatically by the framework.
    /// Use this for immediate-mode rendering in OnRender().
    /// </summary>
    protected internal IRenderer Renderer { get; internal set; } = null!;
    
    /// <summary>
    /// Internal access for SceneManager.
    /// </summary>
    internal IEntityWorld EntityWorld => World;

    /// <summary>
    /// Gets the name of the scene.
    /// </summary>
    public virtual string Name
    {
        get
        {
            // Check for [Scene] attribute
            var attribute = GetType().GetCustomAttribute<SceneAttribute>();
            if (attribute?.Name != null)
            {
                return attribute.Name;
            }
            
            // Fallback to class name
            return GetType().Name;
        }
    }
    
    /// <summary>
    /// Gets whether lifecycle hooks (ECS systems, pre/post update/render) execute automatically.
    /// Default is true. Set to false for manual control over system execution.
    /// </summary>
    public virtual bool EnableLifecycleHooks { get; set; } = true;
    
    /// <summary>
    /// Gets whether frame management (BeginFrame/EndFrame) happens automatically.
    /// Default is true. Set to false for manual control over rendering passes.
    /// </summary>
    public virtual bool EnableAutomaticFrameManagement { get; set; } = true;
    
    #region Convenience Properties
    
    private IInputContext? _input;
    private IAudioService? _audio;
    private IGameContext? _game;
    
    /// <summary>
    /// Convenience property for accessing input.
    /// Automatically resolved from the service provider.
    /// </summary>
    protected IInputContext Input => _input ??= World.GetRequiredService<IInputContext>();
    
    /// <summary>
    /// Convenience property for accessing audio.
    /// Automatically resolved from the service provider.
    /// </summary>
    protected IAudioService Audio => _audio ??= World.GetRequiredService<IAudioService>();
    
    /// <summary>
    /// Convenience property for accessing game context.
    /// Automatically resolved from the service provider.
    /// </summary>
    protected IGameContext Game => _game ??= World.GetRequiredService<IGameContext>();
    
    #endregion
    
    /// <summary>
    /// Constructs a scene.
    /// Framework properties (Logger, World, Renderer) are set automatically by SceneManager.
    /// Override and add your own constructor parameters for dependencies you need.
    /// </summary>
    protected Scene()
    {
        // Apply [Scene] attribute settings
        var attribute = GetType().GetCustomAttribute<SceneAttribute>();
        if (attribute != null)
        {
            EnableLifecycleHooks = attribute.EnableLifecycleHooks;
            EnableAutomaticFrameManagement = attribute.EnableAutomaticFrameManagement;
        }
    }

    #region Lifecycle Methods

    /// <summary>
    /// Called when the scene is being loaded. Override to load assets.
    /// </summary>
    /// <remarks>
    /// <para><strong>Use this for:</strong></para>
    /// <list type="bullet">
    /// <item>Loading textures, audio, and other assets</item>
    /// <item>Async I/O operations</item>
    /// </list>
    /// 
    /// <para><strong>Don't use this for:</strong></para>
    /// <list type="bullet">
    /// <item>Starting music or sound effects (use <see cref="OnEnter"/> instead)</item>
    /// <item>Spawning entities (use <see cref="OnEnter"/> instead)</item>
    /// </list>
    /// 
    /// <example>
    /// <code>
    /// protected internal override async Task OnLoadAsync(CancellationToken ct)
    /// {
    ///     _playerTexture = await LoadTextureAsync("player.png", ct);
    ///     _backgroundMusic = await LoadMusicAsync("theme.ogg", ct);
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    protected internal virtual Task OnLoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Called when entering the scene (after loading, before first update).
    /// Use for scene initialization logic.
    /// </summary>
    /// <remarks>
    /// <para><strong>Use this for:</strong></para>
    /// <list type="bullet">
    /// <item>Starting background music</item>
    /// <item>Spawning initial entities</item>
    /// <item>Initializing game state</item>
    /// <item>Playing intro animations</item>
    /// </list>
    /// 
    /// <example>
    /// <code>
    /// protected internal override void OnEnter()
    /// {
    ///     Audio.PlayMusic("theme.ogg");
    ///     
    ///     var player = World.CreateEntity("Player");
    ///     player.AddComponent&lt;TransformComponent&gt;();
    ///     player.AddComponent&lt;PlayerController&gt;();
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    protected internal virtual void OnEnter() { }

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
    /// <remarks>
    /// <example>
    /// <code>
    /// protected internal override void OnExit()
    /// {
    ///     Audio.StopMusic();
    ///     Logger.LogInformation("Level completed");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    protected internal virtual void OnExit() { }

    /// <summary>
    /// Called during unloading. Override to clean up resources.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code>
    /// protected internal override async Task OnUnloadAsync(CancellationToken ct)
    /// {
    ///     await SaveGameAsync(ct);
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    protected internal virtual Task OnUnloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    #endregion

    #region Scene-Specific Systems

    private SceneSystemConfigurator? _systemConfigurator;

    /// <summary>
    /// Override to configure scene-specific systems.
    /// </summary>
    protected virtual void ConfigureSystems(ISystemConfigurator systems)
    {
        // Default: no scene-specific configuration
    }
    
    internal SceneSystemConfigurator? SystemConfigurator => _systemConfigurator;
    
    internal void InitializeSystems(IServiceProvider services, ILogger logger)
    {
        _systemConfigurator = new SceneSystemConfigurator(
            services, 
            services.GetService<ILoggerFactory>()?.CreateLogger<SceneSystemConfigurator>());
        
        try
        {
            ConfigureSystems(_systemConfigurator);
            
            if (_systemConfigurator.SceneSystems.Count > 0 || 
                _systemConfigurator.DisabledSystemNames.Count > 0)
            {
                logger.LogInformation(
                    "Scene '{SceneName}' configured: {SystemCount} scene-specific systems, {DisabledCount} disabled systems",
                    Name,
                    _systemConfigurator.SceneSystems.Count,
                    _systemConfigurator.DisabledSystemNames.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to configure systems for scene '{SceneName}'", Name);
            throw;
        }
    }

    #endregion
}
