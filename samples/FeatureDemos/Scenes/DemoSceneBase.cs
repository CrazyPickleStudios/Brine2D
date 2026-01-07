using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Engine.Transitions;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes;

/// <summary>
/// Base class for demo scenes with common functionality.
/// Automatically cleans up entities on scene unload to prevent leaks.
/// </summary>
public abstract class DemoSceneBase : Scene
{
    protected readonly IInputService Input;
    protected readonly ISceneManager SceneManager;
    protected readonly IGameContext GameContext;
    protected readonly IEntityWorld? World;
    
    protected DemoSceneBase(
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        ILogger logger,
        IEntityWorld? world = null) : base(logger)
    {
        Input = input;
        SceneManager = sceneManager;
        GameContext = gameContext;
        World = world;
    }
    
    /// <summary>
    /// Returns to the main menu with a fade transition.
    /// </summary>
    protected void ReturnToMenu()
    {
        Logger.LogInformation("Returning to main menu...");
        _ = SceneManager.LoadSceneAsync<MainMenuScene>(
            new FadeTransition(duration: 0.3f, color: Color.Black)
        );
    }
    
    /// <summary>
    /// Checks if ESC was pressed and returns to menu if so.
    /// Call this in your OnUpdate() method.
    /// </summary>
    /// <returns>True if returning to menu, false otherwise.</returns>
    protected bool CheckReturnToMenu()
    {
        if (Input.IsKeyPressed(Keys.Escape))
        {
            ReturnToMenu();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Automatically cleans up all entities when scene unloads.
    /// This prevents entity leaks when transitioning between scenes.
    /// Override this if you need custom cleanup logic, but remember to call base.OnUnloadAsync()!
    /// </summary>
    protected override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        if (World != null)
        {
            var entityCount = World.Entities.Count();
            if (entityCount > 0)
            {
                Logger.LogDebug("Cleaning up {Count} entities for scene: {SceneName}", entityCount, Name);
                World.Clear();
            }
        }
        
        return base.OnUnloadAsync(cancellationToken);
    }
}