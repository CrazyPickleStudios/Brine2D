using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Engine.Systems;
using Brine2D.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Base class for game scenes.
/// </summary>
/// <remarks>
/// <para><strong>Lifecycle Order:</strong></para>
/// <para>Update: PreUpdate hooks → Scene.Update() → World.Update() → PostUpdate hooks</para>
/// <para>Render: PreRender hooks → World.Render() → Scene.Render() → PostRender hooks</para>
/// <para>
/// <strong>When to use Scene.OnRender():</strong>
/// Use for UI, HUD, overlays, and scene-specific effects that should render on top of entities.
/// Entity/Component rendering happens automatically before Scene.OnRender().
/// </para>
/// </remarks>
public abstract class Scene : IScene
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

    public virtual string Name => GetType().Name;
    public virtual bool EnableLifecycleHooks { get; set; } = true;
    public virtual bool EnableAutomaticFrameManagement { get; set; } = true;
    
    /// <summary>
    /// Constructs a scene.
    /// Framework properties (Logger, World, Renderer) are set automatically by SceneManager.
    /// Override and add your own constructor parameters for dependencies you need.
    /// </summary>
    protected Scene() { }

    #region Resource Lifecycle

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Initializing scene: {SceneName}", Name);
        await OnInitializeAsync(cancellationToken);
    }

    public virtual async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Loading scene: {SceneName}", Name);
        await OnLoadAsync(cancellationToken);
    }

    public virtual async Task UnloadAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Unloading scene: {SceneName}", Name);
        await OnUnloadAsync(cancellationToken);
    }

    /// <summary>
    /// Called during initialization. Override to provide custom initialization logic.
    /// This is for setup and configuration tasks (NOT asset loading - use OnLoadAsync for that).
    /// </summary>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Called during loading. Override to load resources asynchronously.
    /// This is where you load textures, sounds, build atlases, create GPU resources, and initialize scene state.
    /// </summary>
    protected virtual Task OnLoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Called during unloading. Override to clean up resources.
    /// </summary>
    protected virtual Task OnUnloadAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    #endregion

    #region Frame Lifecycle

    public void Update(GameTime gameTime)
    {
        OnUpdate(gameTime);
    }

    public void Render(GameTime gameTime)
    {
        OnRender(gameTime);
    }

    protected virtual void OnUpdate(GameTime gameTime) { }
    protected virtual void OnRender(GameTime gameTime) { }

    #endregion

    #region Scene-Specific Systems

    private SceneSystemConfigurator? _systemConfigurator;

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
