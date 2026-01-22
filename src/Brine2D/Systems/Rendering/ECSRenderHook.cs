using Brine2D.Core;
using Brine2D.ECS.Systems;
using Brine2D.Engine;
using Brine2D.Rendering;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Lifecycle hook that executes ECS render pipeline automatically.
/// Registered when AddECSRendering() is called.
/// Renders sprites, particles, etc. before scene UI.
/// </summary>
internal class ECSRenderHook : ISceneLifecycleHook
{
    private readonly RenderPipeline _renderPipeline;
    private readonly IRenderer _renderer;
    
    public int Order => 50;

    public ECSRenderHook(RenderPipeline renderPipeline, IRenderer renderer)
    {
        _renderPipeline = renderPipeline ?? throw new ArgumentNullException(nameof(renderPipeline));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public void PreUpdate(GameTime gameTime)
    {
        // Nothing needed
    }

    public void PostUpdate(GameTime gameTime)
    {
        // Nothing needed
    }

    public void PreRender(GameTime gameTime)
    {
        // Execute all ECS render systems (sprites, particles, debug, etc.)
        _renderPipeline.Execute(_renderer);
    }

    public void PostRender(GameTime gameTime)
    {
        // Nothing needed (scene renders UI after)
    }
}