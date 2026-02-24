using Brine2D.Core;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering;

/// <summary>
/// Manages render state: blend modes, scissor rectangles, layers, and camera transforms.
/// </summary>
internal sealed class SDL3StateManager
{
    private readonly ILogger<SDL3StateManager> _logger;
    private readonly Stack<Rectangle?> _scissorRectStack = new();
    
    private BlendMode _currentBlendMode = BlendMode.Alpha;
    private Rectangle? _currentScissorRect;
    private byte _currentRenderLayer = 128; // Default: middle layer
    
    public ICamera? Camera { get; set; }
    public BlendMode CurrentBlendMode => _currentBlendMode;
    public Rectangle? CurrentScissorRect => _currentScissorRect;
    public byte CurrentRenderLayer => _currentRenderLayer;
    
    public SDL3StateManager(ILogger<SDL3StateManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    // ============================================================
    // BLEND MODE
    // ============================================================
    
    public void SetBlendMode(BlendMode blendMode)
    {
        _currentBlendMode = blendMode;
        _logger.LogDebug("Blend mode set to: {BlendMode}", blendMode);
    }
    
    // ============================================================
    // SCISSOR RECTANGLE
    // ============================================================
    
    public void SetScissorRect(Rectangle? rect, int viewportWidth, int viewportHeight)
    {
        // Validate rectangle if provided
        if (rect.HasValue)
        {
            var r = rect.Value;
            if (r.Width < 0 || r.Height < 0)
            {
                throw new ArgumentException(
                    "Scissor rectangle dimensions cannot be negative", 
                    nameof(rect));
            }
            
            // Warn if extending beyond viewport
            if (r.X < 0 || r.Y < 0 || 
                r.X + r.Width > viewportWidth || 
                r.Y + r.Height > viewportHeight)
            {
                _logger.LogWarning(
                    "Scissor rect ({X}, {Y}, {Width}, {Height}) extends beyond viewport ({ViewportWidth}x{ViewportHeight})",
                    r.X, r.Y, r.Width, r.Height, viewportWidth, viewportHeight);
            }
        }
        
        _currentScissorRect = rect;
        
        _logger.LogDebug("Scissor rect set to: {Rect}", 
            rect.HasValue ? $"{rect.Value.X}, {rect.Value.Y}, {rect.Value.Width}x{rect.Value.Height}" : "None");
    }
    
    public void PushScissorRect(Rectangle? rect, int viewportWidth, int viewportHeight)
    {
        _scissorRectStack.Push(_currentScissorRect);
        SetScissorRect(rect, viewportWidth, viewportHeight);
        
        _logger.LogDebug("Scissor rect pushed (stack depth: {Depth})", _scissorRectStack.Count);
    }
    
    public void PopScissorRect()
    {
        if (_scissorRectStack.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot pop scissor rect: stack is empty");
        }
        
        var previousRect = _scissorRectStack.Pop();
        _currentScissorRect = previousRect;
        
        _logger.LogDebug("Scissor rect popped (stack depth: {Depth})", _scissorRectStack.Count);
    }
    
    // ============================================================
    // RENDER LAYER
    // ============================================================
    
    public void SetRenderLayer(byte layer)
    {
        _currentRenderLayer = layer;
    }
}