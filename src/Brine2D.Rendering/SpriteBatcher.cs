using System.Numerics;
using Brine2D.Core.Animation;

namespace Brine2D.Rendering;

/// <summary>
/// Batches sprite draw calls for efficient rendering.
/// Works with both Legacy and GPU renderers by sorting and grouping sprites
/// before submission to minimize state changes and draw calls.
/// </summary>
public class SpriteBatcher
{
    private readonly List<SpriteBatchItem> _items = new();
    private readonly Dictionary<ITexture, List<SpriteBatchItem>> _batchesByTexture = new();
    
    /// <summary>
    /// Gets the current number of queued sprites.
    /// </summary>
    public int Count => _items.Count;
    
    /// <summary>
    /// Gets the number of draw calls that will be issued when flushed.
    /// Useful for performance monitoring.
    /// </summary>
    public int EstimatedDrawCalls => _batchesByTexture.Count;
    
    /// <summary>
    /// Adds a sprite to the batch queue.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="position">World position of the sprite.</param>
    /// <param name="sourceRect">Optional source rectangle in the texture (for sprite sheets).</param>
    /// <param name="scale">Scale to apply to the sprite.</param>
    /// <param name="rotation">Rotation in radians (currently not supported by IRenderer).</param>
    /// <param name="origin">Origin point for rotation/scaling (0-1 range, default center).</param>
    /// <param name="tint">Color tint to apply.</param>
    /// <param name="layer">Rendering layer (lower = background, higher = foreground).</param>
    public void Draw(
        ITexture texture,
        Vector2 position,
        Rectangle? sourceRect,
        Vector2 scale,
        float rotation,
        Vector2 origin,
        Color tint,
        int layer)
    {
        _items.Add(new SpriteBatchItem
        {
            Texture = texture,
            Position = position,
            SourceRect = sourceRect,
            Scale = scale,
            Rotation = rotation,
            Origin = origin,
            Tint = tint,
            Layer = layer
        });
    }
    
    /// <summary>
    /// Flushes all batched sprites to the renderer.
    /// Automatically sorts by layer and groups by texture for optimal performance.
    /// </summary>
    /// <param name="renderer">The renderer to submit draw calls to.</param>
    /// <param name="camera">Optional camera for world-to-screen transformation.</param>
    public void Flush(IRenderer renderer, ICamera? camera = null)
    {
        if (_items.Count == 0)
            return;
        
        // Sort by layer (front-to-back), then by texture (minimize texture swaps)
        var sorted = _items
            .OrderBy(item => item.Layer)
            .ThenBy(item => item.Texture.GetHashCode())
            .ToList();
        
        // Group by texture for batch rendering
        _batchesByTexture.Clear();
        foreach (var item in sorted)
        {
            if (!_batchesByTexture.ContainsKey(item.Texture))
                _batchesByTexture[item.Texture] = new List<SpriteBatchItem>();
            
            _batchesByTexture[item.Texture].Add(item);
        }
        
        // Render each texture batch
        // Future GPU renderer can create a single vertex buffer per texture here
        foreach (var (texture, batch) in _batchesByTexture)
        {
            foreach (var item in batch)
            {
                DrawSprite(renderer, item);
            }
        }
        
        // Clear for next frame
        _items.Clear();
    }
    
    /// <summary>
    /// Clears all queued sprites without rendering.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        _batchesByTexture.Clear();
    }
    
    private void DrawSprite(IRenderer renderer, SpriteBatchItem item)
    {
        // Calculate final destination rect
        var width = item.SourceRect?.Width ?? item.Texture.Width;
        var height = item.SourceRect?.Height ?? item.Texture.Height;
        
        var destWidth = width * item.Scale.X;
        var destHeight = height * item.Scale.Y;
        
        // Apply origin offset (origin is 0-1 range)
        var destX = item.Position.X - (item.Origin.X * destWidth);
        var destY = item.Position.Y - (item.Origin.Y * destHeight);
        
        // TODO: Add rotation support when IRenderer supports it
        // Current IRenderer API doesn't support rotation
        // Future: Use transform matrix with GPU renderer
        
        if (item.SourceRect.HasValue)
        {
            var src = item.SourceRect.Value;
            renderer.DrawTexture(
                item.Texture,
                src.X, src.Y, src.Width, src.Height,
                destX, destY, destWidth, destHeight);
        }
        else
        {
            renderer.DrawTexture(item.Texture, destX, destY, destWidth, destHeight);
        }
    }
}

/// <summary>
/// Represents a single sprite draw call in the batch.
/// </summary>
internal struct SpriteBatchItem
{
    public ITexture Texture;
    public Vector2 Position;
    public Rectangle? SourceRect;
    public Vector2 Scale;
    public float Rotation;
    public Vector2 Origin;
    public Color Tint;
    public int Layer;
}