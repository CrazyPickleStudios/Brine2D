using Brine2D.Core;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering;

/// <summary>
/// Manages render state: blend modes, scissor rectangles, layers, and camera transforms.
/// </summary>
internal sealed class SDL3StateManager
{
    private readonly ILogger<SDL3StateManager> _logger;
    private readonly Stack<Rectangle?> _scissorRectStack = new();
    
    private BlendMode _currentBlendMode = IRenderer.DefaultBlendMode;
    private Rectangle? _currentScissorRect;
    private byte _currentRenderLayer = IRenderer.DefaultRenderLayer;
    
    public ICamera? Camera { get; set; }
    public BlendMode CurrentBlendMode => _currentBlendMode;
    public byte CurrentRenderLayer => _currentRenderLayer;
    public Rectangle? CurrentScissorRect => _currentScissorRect;
    public int ScissorRectStackDepth => _scissorRectStack.Count;
    
    public SDL3StateManager(ILogger<SDL3StateManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void ResetFrameState()
    {
        _currentBlendMode = IRenderer.DefaultBlendMode;
        _currentRenderLayer = IRenderer.DefaultRenderLayer;
        _currentScissorRect = null;

        if (_scissorRectStack.Count > 0)
        {
            _logger.LogWarning("Scissor rect stack had {Count} unpopped entries at frame boundary", _scissorRectStack.Count);
            _scissorRectStack.Clear();
        }
    }
    
    public void SetBlendMode(BlendMode blendMode)
    {
        _currentBlendMode = blendMode;
    }
    
    public void SetScissorRect(Rectangle? rect, int viewportWidth, int viewportHeight)
    {
        if (rect.HasValue)
        {
            var r = rect.Value;
            if (r.Width < 0 || r.Height < 0)
            {
                throw new ArgumentException(
                    "Scissor rectangle dimensions cannot be negative", 
                    nameof(rect));
            }

            float clampedX = Math.Max(r.X, 0);
            float clampedY = Math.Max(r.Y, 0);
            float clampedRight = Math.Min(r.X + r.Width, viewportWidth);
            float clampedBottom = Math.Min(r.Y + r.Height, viewportHeight);
            float clampedW = Math.Max(clampedRight - clampedX, 0);
            float clampedH = Math.Max(clampedBottom - clampedY, 0);

            if (clampedX != r.X || clampedY != r.Y || clampedW != r.Width || clampedH != r.Height)
            {
                _logger.LogDebug(
                    "Scissor rect ({X}, {Y}, {Width}, {Height}) clamped to viewport ({ViewportWidth}x{ViewportHeight}) → ({CX}, {CY}, {CW}, {CH})",
                    r.X, r.Y, r.Width, r.Height, viewportWidth, viewportHeight,
                    clampedX, clampedY, clampedW, clampedH);
            }

            _currentScissorRect = new Rectangle(clampedX, clampedY, clampedW, clampedH);
            return;
        }
        
        _currentScissorRect = null;
    }
    
    public void PushScissorRect(Rectangle? rect, int viewportWidth, int viewportHeight)
    {
        _scissorRectStack.Push(_currentScissorRect);
        SetScissorRect(ScissorRectHelper.Intersect(_currentScissorRect, rect), viewportWidth, viewportHeight);
    }
    
    public void PopScissorRect(int viewportWidth, int viewportHeight)
    {
        if (_scissorRectStack.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot pop scissor rect: stack is empty");
        }

        SetScissorRect(_scissorRectStack.Pop(), viewportWidth, viewportHeight);
    }
    
    public void SetRenderLayer(byte layer)
    {
        _currentRenderLayer = layer;
    }
}