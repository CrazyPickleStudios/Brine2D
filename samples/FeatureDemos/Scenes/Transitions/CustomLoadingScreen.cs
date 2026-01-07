using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes.Transitions;

/// <summary>
/// Custom loading screen with animated spinner.
/// </summary>
public class CustomLoadingScreen : LoadingScene
{
    private readonly IRenderer? _renderer;
    private float _spinnerRotation;
    
    public CustomLoadingScreen(IRenderer? renderer, ILogger logger) 
        : base(renderer, logger)
    {
        _renderer = renderer;
    }
    
    protected override void OnUpdate(GameTime gameTime)
    {
        base.OnUpdate(gameTime);
        
        // Animate spinner
        _spinnerRotation += (float)gameTime.DeltaTime * 360f; // 1 rotation per second
        if (_spinnerRotation >= 360f)
        {
            _spinnerRotation -= 360f;
        }
    }
    
    protected override void OnRenderLoading(GameTime gameTime)
    {
        if (_renderer == null) return;
        
        var centerX = (_renderer.Camera?.ViewportWidth ?? 1280) / 2f;
        var centerY = (_renderer.Camera?.ViewportHeight ?? 720) / 2f;
        
        // Draw background gradient effect (simulated with rectangles)
        DrawGradientBackground();
        
        // Draw title
        _renderer.DrawText("LOADING", centerX - 50, centerY - 100, Color.White);
        
        // Draw spinning indicator
        DrawSpinner(centerX, centerY - 30, 30f, _spinnerRotation);
        
        // Draw progress bar
        var barWidth = 400f;
        var barHeight = 30f;
        var barX = centerX - barWidth / 2f;
        var barY = centerY + 50;
        
        // Background (dark blue)
        _renderer.DrawRectangleFilled(barX, barY, barWidth, barHeight, new Color(20, 30, 60));
        
        // Filled portion (bright blue)
        _renderer.DrawRectangleFilled(
            barX, barY, 
            barWidth * LoadingProgress, 
            barHeight, 
            new Color(50, 150, 255)
        );
        
        // Border (white)
        _renderer.DrawRectangleOutline(barX, barY, barWidth, barHeight, Color.White, 3f);
        
        // Status text
        _renderer.DrawText(LoadingMessage, centerX - 60, barY + 50, new Color(200, 200, 200));
        
        // Percentage
        var percentText = $"{(int)(LoadingProgress * 100)}%";
        _renderer.DrawText(percentText, centerX - 20, centerY + 10, Color.White);
    }
    
    private void DrawGradientBackground()
    {
        if (_renderer == null) return;
        
        // Draw a simple gradient effect with rectangles
        var height = _renderer.Camera?.ViewportHeight ?? 720;
        var width = _renderer.Camera?.ViewportWidth ?? 1280;
        
        for (int i = 0; i < 10; i++)
        {
            var alpha = (byte)(50 - i * 5);
            var color = new Color(10, 20, 40, alpha);
            _renderer.DrawRectangleFilled(0, i * (height / 10f), width, height / 10f, color);
        }
    }
    
    private void DrawSpinner(float centerX, float centerY, float radius, float rotation)
    {
        if (_renderer == null) return;
        
        // Draw spinning circle segments (simulated spinner)
        for (int i = 0; i < 8; i++)
        {
            var angle = (rotation + i * 45f) * MathF.PI / 180f;
            var x = centerX + MathF.Cos(angle) * radius;
            var y = centerY + MathF.Sin(angle) * radius;
            
            // Fade alpha based on position
            var alpha = (byte)(255 - i * 30);
            var color = new Color(100, 200, 255, alpha);
            
            _renderer.DrawCircleFilled(x, y, 5f, color);
        }
    }
}