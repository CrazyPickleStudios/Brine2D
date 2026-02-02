using Brine2D.Core;
using Brine2D.ECS;

namespace Brine2D.Engine;

/// <summary>
/// Interface for game scenes.
/// All scenes have an entity world for managing game objects.
/// </summary>
public interface IScene
{
    /// <summary>
    /// Gets the name of the scene.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the entity world for this scene.
    /// Each scene has its own isolated world for managing entities and components.
    /// </summary>
    /// <remarks>
    /// The world is automatically created and managed by the framework for Scene-based implementations.
    /// Custom IScene implementations must provide their own world.
    /// </remarks>
    IEntityWorld World { get; }

    /// <summary>
    /// Gets whether lifecycle hooks should execute automatically.
    /// Set to false for complete manual control over ECS pipelines and other hooks.
    /// </summary>
    bool EnableLifecycleHooks { get; }

    /// <summary>
    /// Gets whether frame management (Clear/BeginFrame/EndFrame) should happen automatically.
    /// Set to false for manual control over frame rendering (e.g., render targets, post-processing).
    /// </summary>
    bool EnableAutomaticFrameManagement { get; }

    /// <summary>
    /// Initializes the scene asynchronously (called once during scene loading).
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the scene's resources asynchronously (called once during scene loading).
    /// </summary>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the scene's game logic.
    /// </summary>
    void Update(GameTime gameTime);

    /// <summary>
    /// Renders the scene.
    /// </summary>
    void Render(GameTime gameTime);

    /// <summary>
    /// Unloads the scene's resources asynchronously (called once when scene is destroyed).
    /// </summary>
    Task UnloadAsync(CancellationToken cancellationToken = default);
}
