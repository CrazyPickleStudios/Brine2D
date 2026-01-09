using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Engine.Transitions;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.Performance;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes;

/// <summary>
/// Base class for demo scenes with common functionality.
/// Automatically cleans up entities on scene unload to prevent leaks.
/// Includes optional performance monitoring (F3 to toggle details, F1 to toggle visibility).
/// </summary>
public abstract class DemoSceneBase : Scene
{
    protected readonly IInputService Input;
    protected readonly ISceneManager SceneManager;
    protected readonly IGameContext GameContext;
    protected readonly IEntityWorld? World;
    protected readonly IRenderer Renderer;
    protected readonly PerformanceOverlay? PerfOverlay;
    
    protected DemoSceneBase(
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        ILogger logger,
        IRenderer renderer,
        IEntityWorld? world = null,
        PerformanceOverlay? perfOverlay = null) : base(logger)
    {
        Input = input;
        SceneManager = sceneManager;
        GameContext = gameContext;
        World = world;
        PerfOverlay = perfOverlay;
        Renderer = renderer;
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
    /// Handles performance overlay hotkeys.
    /// Call this in your OnUpdate() if you want performance monitoring.
    /// F3 toggles detailed stats, F1 toggles visibility.
    /// </summary>
    protected void HandlePerformanceHotkeys()
    {
        if (PerfOverlay == null) return;
        
        // F3: Toggle detailed stats
        if (Input.IsKeyPressed(Keys.F3))
        {
            PerfOverlay.ShowDetailedStats = !PerfOverlay.ShowDetailedStats;
            Logger.LogDebug("Performance stats: {State}", 
                PerfOverlay.ShowDetailedStats ? "Detailed" : "Simple");
        }
        
        // F1: Toggle visibility
        if (Input.IsKeyPressed(Keys.F1))
        {
            PerfOverlay.IsVisible = !PerfOverlay.IsVisible;
            Logger.LogDebug("Performance overlay: {State}", 
                PerfOverlay.IsVisible ? "Visible" : "Hidden");
        }
    }
    
    /// <summary>
    /// Renders the performance overlay if available.
    /// Call this in your OnRender() if you want performance monitoring.
    /// </summary>
    protected void RenderPerformanceOverlay()
    {
        if (PerfOverlay == null || !PerfOverlay.IsVisible) return;
        
        PerfOverlay.Render(Renderer);
        
        // Only render graph if detailed stats are shown
        if (PerfOverlay.ShowDetailedStats)
        {
            PerfOverlay.RenderFrameTimeGraph(Renderer);
        }
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