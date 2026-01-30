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
        
    public int Order => 100;

    public ECSLifecycleHook(UpdatePipeline updatePipeline)
    {
        _updatePipeline = updatePipeline ?? throw new ArgumentNullException(nameof(updatePipeline));
    }

    public void PreUpdate(GameTime gameTime) { }

    public void PostUpdate(GameTime gameTime)
    {
        _updatePipeline.Execute(gameTime);
    }

    public void PreRender(GameTime gameTime) { }
    public void PostRender(GameTime gameTime) { }
}