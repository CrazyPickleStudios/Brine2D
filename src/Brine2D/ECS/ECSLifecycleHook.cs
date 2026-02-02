using Brine2D.Core;
using Brine2D.ECS.Systems;
using Brine2D.Engine;

namespace Brine2D.ECS;

/// <summary>
/// Lifecycle hook that executes ECS pipelines automatically.
/// SceneManager handles EntityWorld.Update() directly.
/// </summary>
internal class ECSLifecycleHook : ISceneLifecycleHook
{
    private readonly UpdatePipeline _updatePipeline;

    public int Order => 100; // Run after most other hooks

    public ECSLifecycleHook(UpdatePipeline updatePipeline)
    {
        _updatePipeline = updatePipeline ?? throw new ArgumentNullException(nameof(updatePipeline));
    }

    public void PreUpdate(GameTime gameTime, IEntityWorld world)
    {
        // Nothing needed - systems run in PostUpdate
    }

    public void PostUpdate(GameTime gameTime, IEntityWorld world)
    {
        // Execute all ECS update systems
        _updatePipeline.Execute(gameTime, world);
    }

    public void PreRender(GameTime gameTime, IEntityWorld world)
    {
        // Nothing needed - rendering happens in ECSRenderHook
    }

    public void PostRender(GameTime gameTime, IEntityWorld world)
    {
        // Nothing needed - rendering happens in ECSRenderHook
    }
}