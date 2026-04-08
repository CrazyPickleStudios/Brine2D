using Brine2D.Core;
using Brine2D.Rendering.SDL.PostProcessing;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace Brine2D.Rendering;

internal sealed partial class SDL3Renderer
{
    public void SetRenderLayer(byte layer)
    {
        ThrowIfNotInitialized();
        _stateManager.SetRenderLayer(layer);
    }

    public byte GetRenderLayer()
    {
        ThrowIfNotInitialized();
        return _stateManager.CurrentRenderLayer;
    }

    public BlendMode GetBlendMode()
    {
        ThrowIfNotInitialized();
        return _stateManager.CurrentBlendMode;
    }

    public void SetBlendMode(BlendMode blendMode)
    {
        ThrowIfNotInitialized();

        if (!Enum.IsDefined(blendMode))
            throw new ArgumentOutOfRangeException(nameof(blendMode), blendMode, "Undefined blend mode");

        if (_stateManager.CurrentBlendMode == blendMode)
            return;

        int index = (int)blendMode;
        if ((uint)index >= (uint)_blendModePipelines.Length || _blendModePipelines[index] == nint.Zero)
        {
            throw new ArgumentException(
                $"No pipeline for blend mode {blendMode}; pipeline creation may be incomplete.",
                nameof(blendMode));
        }

        FlushBatch();
        _stateManager.SetBlendMode(blendMode);
    }

    /// <summary>
    /// Draw a texture with full control over transform, origin, scale, and flip.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="position">Position in world/screen space.</param>
    /// <param name="sourceRect">Source rectangle (null = entire texture).</param>
    /// <param name="origin">Rotation/scale origin (0-1 normalized, 0.5,0.5 = center, 0,0 = top-left). Defaults to center.</param>
    /// <param name="rotation">Rotation angle in radians.</param>
    /// <param name="scale">Scale multiplier (null = no scaling).</param>
    /// <param name="color">Tint color (null = white).</param>
    /// <param name="flip">Sprite flip flags.</param>
    public void DrawTexture(
        ITexture texture,
        Vector2 position,
        Rectangle? sourceRect = null,
        Vector2? origin = null,
        float rotation = 0f,
        Vector2? scale = null,
        Color? color = null,
        SpriteFlip flip = SpriteFlip.None)
    {
        ThrowIfNotInitialized();
        if (!_frameManager.HasActiveFrame) return;
        ArgumentNullException.ThrowIfNull(texture);

        if (!texture.IsLoaded)
            return;

        if (texture.Width <= 0 || texture.Height <= 0)
            return;

        var textureHandle = GetTextureHandle(texture);

        var actualOrigin = origin ?? new Vector2(0.5f, 0.5f);
        var actualScale = scale ?? Vector2.One;
        var actualColor = color ?? Color.White;
        var srcRect = sourceRect ?? new Rectangle(0, 0, texture.Width, texture.Height);

        if (srcRect.Width <= 0 || srcRect.Height <= 0)
            return;

        var destWidth = srcRect.Width * actualScale.X;
        var destHeight = srcRect.Height * actualScale.Y;

        float u1 = srcRect.X / (float)texture.Width;
        float v1 = srcRect.Y / (float)texture.Height;
        float u2 = (srcRect.X + srcRect.Width) / (float)texture.Width;
        float v2 = (srcRect.Y + srcRect.Height) / (float)texture.Height;

        if ((flip & SpriteFlip.Horizontal) != 0) (u1, u2) = (u2, u1);
        if ((flip & SpriteFlip.Vertical) != 0) (v1, v2) = (v2, v1);

        var pivotX = destWidth * actualOrigin.X;
        var pivotY = destHeight * actualOrigin.Y;
        var adjustedX = position.X - pivotX;
        var adjustedY = position.Y - pivotY;

        _batchRenderer.DrawTexturedQuad(
            textureHandle,
            texture.ScaleMode,
            adjustedX,
            adjustedY,
            destWidth,
            destHeight,
            actualColor,
            u1, v1, u2, v2,
            rotation,
            new Vector2(pivotX, pivotY),
            _flushBatchAction,
            textureRef: texture);
    }

    /// <summary>
    /// Draw texture at position (Vector2, top-left anchor).
    /// </summary>
    public void DrawTexture(ITexture texture, Vector2 position)
    {
        DrawTexture(texture, position, origin: Vector2.Zero, scale: Vector2.One);
    }

    /// <summary>
    /// Draw texture at position (float x, y, top-left anchor).
    /// </summary>
    public void DrawTexture(ITexture texture, float x, float y)
    {
        DrawTexture(texture, new Vector2(x, y));
    }

    /// <summary>
    /// Draw texture at position with explicit width/height (top-left anchor).
    /// </summary>
    public void DrawTexture(ITexture texture, float x, float y, float width, float height)
    {
        ThrowIfNotInitialized();
        if (!_frameManager.HasActiveFrame) return;
        ArgumentNullException.ThrowIfNull(texture);

        if (!texture.IsLoaded || texture.Width <= 0 || texture.Height <= 0)
            return;

        if (width <= 0f || height <= 0f)
            return;

        DrawTexture(texture, new Vector2(x, y),
            scale: new Vector2(width / texture.Width, height / texture.Height),
            origin: Vector2.Zero);
    }

    public void DrawRectangleFilled(float x, float y, float width, float height, Color color)
    {
        ThrowIfNotInitialized();
        if (!_frameManager.HasActiveFrame) return;
        _batchRenderer.DrawRectangleFilled(x, y, width, height, color, _flushBatchAction);
    }

    public void DrawRectangleOutline(float x, float y, float width, float height, Color color, float thickness = 1)
    {
        ThrowIfNotInitialized();
        if (!_frameManager.HasActiveFrame) return;
        _batchRenderer.DrawRectangleOutline(x, y, width, height, color, thickness, _flushBatchAction);
    }

    public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1)
    {
        ThrowIfNotInitialized();
        if (!_frameManager.HasActiveFrame) return;
        _batchRenderer.DrawLine(x1, y1, x2, y2, color, thickness, _flushBatchAction);
    }

    public void DrawCircleFilled(float centerX, float centerY, float radius, Color color)
    {
        ThrowIfNotInitialized();
        if (!_frameManager.HasActiveFrame) return;
        _batchRenderer.DrawCircleFilled(centerX, centerY, radius, color, _flushBatchAction);
    }

    public void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1)
    {
        ThrowIfNotInitialized();
        if (!_frameManager.HasActiveFrame) return;
        _batchRenderer.DrawCircleOutline(centerX, centerY, radius, color, thickness, _flushBatchAction);
    }

    public void DrawRectangleFilled(Rectangle rect, Color color)
    {
        DrawRectangleFilled(rect.X, rect.Y, rect.Width, rect.Height, color);
    }

    public void DrawRectangleOutline(Rectangle rect, Color color, float thickness = 1f)
    {
        DrawRectangleOutline(rect.X, rect.Y, rect.Width, rect.Height, color, thickness);
    }

    public void DrawCircleFilled(Vector2 center, float radius, Color color)
    {
        DrawCircleFilled(center.X, center.Y, radius, color);
    }

    public void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness = 1f)
    {
        DrawCircleOutline(center.X, center.Y, radius, color, thickness);
    }

    public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f)
    {
        DrawLine(start.X, start.Y, end.X, end.Y, color, thickness);
    }

    private nint GetTextureHandle(ITexture texture)
    {
        return texture switch
        {
            SDL3Texture gpuTexture => gpuTexture.Handle,
            RenderTargetTextureView rtView => rtView.Handle,
            _ => throw new ArgumentException(
                $"Unsupported texture type: {texture.GetType().Name}. " +
                $"Only SDL3GPUTexture and RenderTarget textures are supported.",
                nameof(texture))
        };
    }
}