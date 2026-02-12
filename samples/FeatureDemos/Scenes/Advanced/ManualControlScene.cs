using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes.Advanced;

/// <summary>
/// Example scene demonstrating complete manual control for power users.
/// Disables all automatic behavior and controls everything explicitly.
/// </summary>
public class ManualControlScene : DemoSceneBase
{
    private readonly IInputContext _input;
    private readonly IGameContext _gameContext;
    private readonly UpdatePipeline _updatePipeline;
    private readonly RenderPipeline _renderPipeline;

    public override bool EnableLifecycleHooks => false;

    public override bool EnableAutomaticFrameManagement => false;

    public ManualControlScene(
        IInputContext input,
        IGameContext gameContext,
        UpdatePipeline updatePipeline,
        RenderPipeline renderPipeline,
        ISceneManager sceneManager)
        : base(input, sceneManager, gameContext)
    {
        _input = input;
        _gameContext = gameContext;
        _updatePipeline = updatePipeline;
        _renderPipeline = renderPipeline;
    }

    protected override Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("=== Manual Control Scene (Power User Mode) ===");
        Logger.LogInformation("Lifecycle hooks: DISABLED (manual control)");
        Logger.LogInformation("Frame management: DISABLED (manual control)");
        
        Renderer.ClearColor = new Color(20, 20, 40);

        return Task.CompletedTask;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (CheckReturnToMenu()) return;

        // Manually control system execution
        // You decide when and how systems run
        if (_input.IsKeyDown(Key.P))
        {
            // Pause systems - don't execute pipeline
            Logger.LogInformation("Systems paused");
        }
        else
        {
            // Execute pipeline manually - CHANGED: Pass world!
            _updatePipeline.Execute(gameTime, World);
            World.Update(gameTime);
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        // Manually control frame management
        Renderer.BeginFrame();
        
        // Manually control rendering order - CHANGED: Pass world!
        _renderPipeline.Execute(Renderer, World);
        
        // Draw custom UI on top
        Renderer.DrawText("Manual Control Mode", 10, 10, Color.Yellow);
        Renderer.DrawText("Press P to pause systems", 10, 35, Color.White);
        
        Renderer.EndFrame();
    }
}