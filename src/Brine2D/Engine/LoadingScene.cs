using System.Drawing;
using Brine2D.Core;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Base class for loading screens.
/// Framework properties (Logger, Renderer) set automatically by SceneManager.
/// Loading screens don't get World (they're visual-only, between scene scopes).
/// </summary>
public abstract class LoadingScene : Scene
{
    protected LoadingScene() { }
    
    protected float LoadingProgress { get; private set; }
    protected string LoadingMessage { get; private set; } = "Loading...";
    
    /// <summary>
    /// Updates the loading progress (0.0 to 1.0).
    /// </summary>
    public void UpdateProgress(float progress, string? message = null)
    {
        LoadingProgress = Math.Clamp(progress, 0f, 1f);
        if (message != null)
        {
            LoadingMessage = message;
        }
    }
    
    protected override void OnRender(GameTime gameTime)
    {
        OnRenderLoading(gameTime);
    }
    
    /// <summary>
    /// Override this to customize the loading screen appearance.
    /// </summary>
    protected virtual void OnRenderLoading(GameTime gameTime)
    {
        var centerX = Renderer.Width / 2f;
        var centerY = Renderer.Height / 2f;
        
        // Draw "Loading..." text
        Renderer.DrawText(LoadingMessage, centerX - 50, centerY - 50, Color.White);
        
        // Draw progress bar
        var barWidth = 300f;
        var barHeight = 20f;
        var barX = centerX - barWidth / 2f;
        var barY = centerY;
        
        // Background (dark gray)
        Renderer.DrawRectangleFilled(barX, barY, barWidth, barHeight, Color.FromArgb(50, 50, 50));
        
        // Filled portion (white)
        Renderer.DrawRectangleFilled(barX, barY, barWidth * LoadingProgress, barHeight, Color.White);
        
        // Border (light gray)
        Renderer.DrawRectangleOutline(barX, barY, barWidth, barHeight, Color.FromArgb(150, 150, 150), 2f);
        
        // Percentage text
        var percentText = $"{(int)(LoadingProgress * 100)}%";
        Renderer.DrawText(percentText, centerX - 15, centerY + 30, Color.White);
    }
}