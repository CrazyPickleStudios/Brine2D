using Brine2D.Core;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Base class for loading screens.
/// Override OnRenderLoading() to customize the loading screen appearance.
/// </summary>
public abstract class LoadingScene : Scene
{
    private readonly IRenderer? _renderer;
    protected float LoadingProgress { get; private set; }
    protected string LoadingMessage { get; private set; } = "Loading...";
    
    protected LoadingScene(IRenderer? renderer, ILogger logger) : base(logger)
    {
        _renderer = renderer;  // Can be null
    }
    
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
        // Default loading screen
        OnRenderLoading(gameTime);
    }
    
    /// <summary>
    /// Override this to customize the loading screen appearance.
    /// </summary>
    protected virtual void OnRenderLoading(GameTime gameTime)
    {
        if (_renderer == null) return;
        
        // Default: Black background with white text and progress bar
        var centerX = (_renderer.Camera?.ViewportWidth ?? 1280) / 2f;
        var centerY = (_renderer.Camera?.ViewportHeight ?? 720) / 2f;
        
        // Draw "Loading..." text
        _renderer.DrawText(LoadingMessage, centerX - 50, centerY - 50, Color.White);
        
        // Draw progress bar
        var barWidth = 300f;
        var barHeight = 20f;
        var barX = centerX - barWidth / 2f;
        var barY = centerY;
        
        // Background (dark gray)
        _renderer.DrawRectangleFilled(barX, barY, barWidth, barHeight, new Color(50, 50, 50));
        
        // Filled portion (white)
        _renderer.DrawRectangleFilled(barX, barY, barWidth * LoadingProgress, barHeight, Color.White);
        
        // Border (light gray)
        _renderer.DrawRectangleOutline(barX, barY, barWidth, barHeight, new Color(150, 150, 150), 2f);
        
        // Percentage text
        var percentText = $"{(int)(LoadingProgress * 100)}%";
        _renderer.DrawText(percentText, centerX - 15, centerY + 30, Color.White);
    }
}