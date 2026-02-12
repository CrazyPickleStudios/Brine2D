using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes.Transitions;

/// <summary>
/// Custom loading screen with animated spinner.
/// </summary>
public class CustomLoadingScreen : LoadingScene
{
    private float _spinnerRotation;
    
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
        // Custom loading screen rendering
        var centerX = Renderer.Width / 2f;
        var centerY = Renderer.Height / 2f;
        
        Renderer.DrawText("Custom Loading...", centerX - 80, centerY - 50, Color.Cyan);
        
        // Animated dots
        var dots = new string('.', ((int)(LoadingProgress * 10)) % 4);
        Renderer.DrawText(dots, centerX + 100, centerY - 50, Color.Cyan);
        
        // Progress bar (call base implementation)
        base.OnRenderLoading(gameTime);
        
        // Draw spinning indicator
        DrawSpinner(centerX, centerY - 30, 30f, _spinnerRotation);
        
        // Draw progress bar
        var barWidth = 400f;
        var barHeight = 30f;
        var barX = centerX - barWidth / 2f;
        var barY = centerY + 50;
        
        // Background (dark blue)
        Renderer.DrawRectangleFilled(barX, barY, barWidth, barHeight, new Color(20, 30, 60));
        
        // Filled portion (bright blue)
        Renderer.DrawRectangleFilled(
            barX, barY, 
            barWidth * LoadingProgress, 
            barHeight,
            new Color(50, 150, 255)
        );
        
        // Border (white)
        Renderer.DrawRectangleOutline(barX, barY, barWidth, barHeight, Color.White, 3f);
        
        // Status text
        Renderer.DrawText(LoadingMessage, centerX - 60, barY + 50, new Color(200, 200, 200));
        
        // Percentage
        var percentText = $"{(int)(LoadingProgress * 100)}%";
        Renderer.DrawText(percentText, centerX - 20, centerY + 10, Color.White);
    }
    
    private void DrawSpinner(float centerX, float centerY, float radius, float rotation)
    {
        if (Renderer == null) return;
        
        // Draw spinning circle segments (simulated spinner)
        for (int i = 0; i < 8; i++)
        {
            var angle = (rotation + i * 45f) * MathF.PI / 180f;
            var x = centerX + MathF.Cos(angle) * radius;
            var y = centerY + MathF.Sin(angle) * radius;
            
            // Fade alpha based on position
            var alpha = (byte)(255 - i * 30);
            var color = new Color(100, 200, 255, alpha);
            
            Renderer.DrawCircleFilled(x, y, 5f, color);
        }
    }
}