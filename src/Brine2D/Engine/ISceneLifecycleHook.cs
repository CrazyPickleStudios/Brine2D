using Brine2D.Core;
using Brine2D.ECS;

namespace Brine2D.Engine;

/// <summary>
/// Hook interface for systems that need to execute during scene lifecycle.
/// Hooks run before/after scene update/render methods.
/// Order determines execution sequence (lower values execute first).
/// </summary>
public interface ISceneLifecycleHook
{
    /// <summary>
    /// Execution order for this hook (lower values execute first).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Called before Scene.Update().
    /// </summary>
    void PreUpdate(GameTime gameTime, IEntityWorld world);

    /// <summary>
    /// Called after Scene.Update().
    /// </summary>
    void PostUpdate(GameTime gameTime, IEntityWorld world);

    /// <summary>
    /// Called before Scene.Render().
    /// </summary>
    void PreRender(GameTime gameTime, IEntityWorld world);

    /// <summary>
    /// Called after Scene.Render().
    /// </summary>
    void PostRender(GameTime gameTime, IEntityWorld world);
}