using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Engine.Transitions;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes.Transitions;

/// <summary>
/// Demo scene showcasing scene transitions and loading screens.
/// Press 1-3 to transition between different scenes with fade effects.
/// Press ESC to return to main menu.
/// </summary>
public class TransitionDemoScene : DemoSceneBase
{
    private readonly IRenderer _renderer;
    
    private readonly Color _sceneColor;
    private readonly string _sceneName;

    public TransitionDemoScene(
        IRenderer renderer,
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        ILogger<TransitionDemoScene> logger) 
        : base(input, sceneManager, gameContext, logger, renderer, world: null)
    {
        _renderer = renderer;
        
        // Each instance gets a random color for visual distinction
        _sceneColor = new Color(
            (byte)Random.Shared.Next(100, 255),
            (byte)Random.Shared.Next(100, 255),
            (byte)Random.Shared.Next(100, 255)
        );
        
        _sceneName = $"Scene {Random.Shared.Next(1, 100)}";
    }

    protected override void OnInitialize()
    {
        Logger.LogInformation("=== Transition Demo Scene ===");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  1 - Load Scene A (fast fade)");
        Logger.LogInformation("  2 - Load Scene B (slow fade)");
        Logger.LogInformation("  3 - Load Scene C (with loading screen)");
        Logger.LogInformation("  ESC - Return to menu");
        
        // Set clear color
        _renderer.ClearColor = _sceneColor;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Check for return to menu (ESC)
        if (CheckReturnToMenu()) return;

        // Fast fade transition
        if (Input.IsKeyPressed(Keys.D1))
        {
            Logger.LogInformation("Loading Scene A with fast fade...");
            _ = SceneManager.LoadSceneAsync<SceneA>(
                new FadeTransition(duration: 0.5f, color: Color.Black)
            );
        }

        // Slow fade transition
        if (Input.IsKeyPressed(Keys.D2))
        {
            Logger.LogInformation("Loading Scene B with slow fade...");
            _ = SceneManager.LoadSceneAsync<SceneB>(
                new FadeTransition(duration: 2f, color: new Color(50, 0, 100))
            );
        }

        // With loading screen
        if (Input.IsKeyPressed(Keys.D3))
        {
            Logger.LogInformation("Loading Scene C with loading screen...");
            _ = SceneManager.LoadSceneAsync<SceneC>(
                loadingScreen: new CustomLoadingScreen(_renderer, Logger),
                transition: new FadeTransition(duration: 1f, color: Color.Black)
            );
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        // Draw scene info
        _renderer.DrawText("Scene Transition Demo", 10, 10, Color.White);
        _renderer.DrawText($"Current: {_sceneName}", 10, 40, Color.White);
        _renderer.DrawText($"Color: RGB({_sceneColor.R}, {_sceneColor.G}, {_sceneColor.B})", 10, 70, Color.White);
        
        _renderer.DrawText("Press 1 - Fast Fade (0.5s)", 10, 120, Color.Yellow);
        _renderer.DrawText("Press 2 - Slow Fade (2s)", 10, 150, Color.Yellow);
        _renderer.DrawText("Press 3 - With Loading Screen", 10, 180, Color.Yellow);
        _renderer.DrawText("Press ESC - Return to Menu", 10, 210, new Color(150, 150, 150));
        
        // Draw visual indicator
        DrawColorBox();
    }

    private void DrawColorBox()
    {
        var centerX = (_renderer.Camera?.ViewportWidth ?? 1280) / 2f;
        var centerY = (_renderer.Camera?.ViewportHeight ?? 720) / 2f;
        
        // Draw a large colored box in the center
        _renderer.DrawRectangleFilled(
            centerX - 150, 
            centerY - 150, 
            300, 
            300, 
            _sceneColor
        );
        
        // Draw border
        _renderer.DrawRectangleOutline(
            centerX - 150, 
            centerY - 150, 
            300, 
            300, 
            Color.White,
            3f
        );
        
        // Draw scene name in center
        _renderer.DrawText(_sceneName, centerX - 40, centerY - 10, Color.White);
    }
}