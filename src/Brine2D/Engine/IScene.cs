using Brine2D.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Brine2D.Engine;

/// <summary>
/// Interface for game scenes.
/// </summary>
public interface IScene
{
    /// <summary>
    /// Gets the name of the scene.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether the scene is currently active.
    /// </summary>
    bool IsActive { get; }

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
    /// Called when the scene becomes active (can be called multiple times).
    /// Use this to reset state, start music, resume timers, etc.
    /// </summary>
    void Enter();

    /// <summary>
    /// Called when the scene becomes inactive (can be called multiple times).
    /// Use this to stop music, save state, pause timers, etc.
    /// </summary>
    void Exit();
    
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
