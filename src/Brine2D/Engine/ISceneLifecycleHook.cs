using Brine2D.Core;

namespace Brine2D.Engine;

/// <summary>
/// Hook that runs before/after scene update and render.
/// Used by ECS pipelines, UI systems, or other frameworks to inject behavior automatically.
/// Similar to ASP.NET middleware - registered once, runs automatically.
/// </summary>
public interface ISceneLifecycleHook
{
    /// <summary>
    /// Execution order (lower runs first).
    /// Recommended ranges:
    /// - 0-50: Pre-processing (input layers, camera setup)
    /// - 100-200: ECS systems
    /// - 500+: Post-processing (debug overlays, UI)
    /// </summary>
    int Order { get; }
    
    /// <summary>
    /// Called before scene.Update().
    /// Use for input processing, camera setup, etc.
    /// </summary>
    void PreUpdate(GameTime gameTime);
    
    /// <summary>
    /// Called after scene.Update().
    /// Use for ECS systems, physics, AI, etc.
    /// </summary>
    void PostUpdate(GameTime gameTime);
    
    /// <summary>
    /// Called before scene.Render().
    /// Use for ECS rendering, sprite batching, etc.
    /// </summary>
    void PreRender(GameTime gameTime);
    
    /// <summary>
    /// Called after scene.Render().
    /// Use for debug overlays, UI chrome, etc.
    /// </summary>
    void PostRender(GameTime gameTime);
}