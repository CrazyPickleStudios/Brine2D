using Brine2D.Core;
using Brine2D.ECS.Systems;

namespace Brine2D.ECS;

/// <summary>
/// Lifecycle hook that executes ECS update pipeline automatically.
/// Registered when AddObjectECS() is called.
/// No manual pipeline.Execute() calls needed in scenes!
/// </summary>
internal class ECSLifecycleHook : ISceneLifecycleHook
{
    private readonly UpdatePipeline _updatePipeline;
    private readonly IEntityWorld _world;
    
    public int Order => 100;

    public ECSLifecycleHook(UpdatePipeline updatePipeline, IEntityWorld world)
    {
        _updatePipeline = updatePipeline ?? throw new ArgumentNullException(nameof(updatePipeline));
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public void PreUpdate(GameTime gameTime)
    {
        // Nothing needed here (scene handles input first)
    }

    public void PostUpdate(GameTime gameTime)
    {
        // Execute all ECS update systems in order (physics, AI, etc.)
        _updatePipeline.Execute(gameTime);
        
        // Update entity lifecycle (component OnUpdate, timers, etc.)
        _world.Update(gameTime);
    }

    public void PreRender(GameTime gameTime)
    {
        // Nothing needed (render pipeline handles this)
    }

    public void PostRender(GameTime gameTime)
    {
        // Nothing needed
    }
}