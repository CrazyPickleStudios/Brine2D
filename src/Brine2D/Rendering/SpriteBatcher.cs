using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using Brine2D.Core;

namespace Brine2D.Rendering;

/// <summary>
/// Batches sprite draw calls for efficient rendering.
/// Sorts and groups sprites before submission to minimize state changes and draw calls.
/// </summary>
public sealed class SpriteBatcher : IDisposable
{
    private readonly List<SpriteBatchItem> _items = new(256);
    private int[] _sortIndices = ArrayPool<int>.Shared.Rent(256);
    private int _lastEstimatedDrawCalls;
    private int _nextInsertionOrder;
    private int _lowUsageFrames;
    private int _disposed;

    private const int ShrinkAfterFrames = 300;
    private const int ShrinkRatio = 4;
    private const int MinSortIndexCapacity = 256;

    /// <summary>
    /// Gets the current number of queued sprites.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Gets the estimated number of draw calls issued in the last flush.
    /// This is an upper bound — layer, blend mode, or texture changes each count as a new draw call,
    /// even if the underlying renderer could coalesce them.
    /// </summary>
    public int EstimatedDrawCalls => _lastEstimatedDrawCalls;

    /// <summary>
    /// Adds a sprite to the batch queue.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="position">World position of the sprite.</param>
    /// <param name="sourceRect">Optional source rectangle in the texture (for sprite sheets).</param>
    /// <param name="scale">Scale to apply to the sprite.</param>
    /// <param name="rotation">Rotation in radians.</param>
    /// <param name="origin">Origin point for rotation/scaling (0-1 range, default center).</param>
    /// <param name="tint">Color tint to apply.</param>
    /// <param name="layer">Rendering layer (lower = background, higher = foreground).</param>
    /// <param name="flip">Sprite flip flags.</param>
    /// <param name="blendMode">Blend mode for this sprite (default: Alpha).</param>
    public void Draw(
        ITexture texture,
        Vector2 position,
        Rectangle? sourceRect,
        Vector2 scale,
        float rotation,
        Vector2 origin,
        Color tint,
        byte layer,
        SpriteFlip flip = SpriteFlip.None,
        BlendMode blendMode = BlendMode.Alpha)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
        ArgumentNullException.ThrowIfNull(texture);

        if (!Enum.IsDefined(blendMode))
            throw new ArgumentOutOfRangeException(nameof(blendMode), blendMode, null);

        _items.Add(new SpriteBatchItem(
            texture, position, sourceRect ?? texture.Bounds, scale,
            rotation, origin, tint, layer, flip, blendMode,
            _nextInsertionOrder++));
    }

    /// <summary>
    /// Flushes all batched sprites to the renderer.
    /// Automatically sorts and groups sprites before submission to minimize state changes and draw calls.
    /// Sorts a lightweight index array rather than the full batch items to reduce swap cost.
    /// </summary>
    /// <param name="drawContext">The draw context to submit draw calls to.</param>
    /// <remarks>
    /// <para>
    /// The caller must ensure that <paramref name="drawContext"/> is in a valid state to accept
    /// draw calls (e.g., between <c>BeginFrame</c>/<c>EndFrame</c> when backed by a GPU renderer).
    /// If the context silently discards commands, all flushed sprites will be lost without error.
    /// </para>
    /// <para>
    /// After this method returns, the draw context may still hold buffered geometry that has not
    /// been submitted to the GPU. The caller (or a subsequent frame-lifecycle method such as
    /// <c>EndFrame</c>) is responsible for triggering the final GPU flush.
    /// </para>
    /// </remarks>
    public void Flush(IDrawContext drawContext)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
        ArgumentNullException.ThrowIfNull(drawContext);

        int count = _items.Count;
        if (count == 0)
        {
            TryShrinkBuffers(0);
            return;
        }

        byte savedLayer = drawContext.GetRenderLayer();
        BlendMode savedBlendMode = drawContext.GetBlendMode();

        if (_sortIndices.Length < count)
        {
            int previousLength = _sortIndices.Length;
            ArrayPool<int>.Shared.Return(_sortIndices);
            _sortIndices = ArrayPool<int>.Shared.Rent(Math.Max(count, previousLength * 2));
        }

        for (int i = 0; i < count; i++)
            _sortIndices[i] = i;

        _sortIndices.AsSpan(0, count).Sort(new IndexComparer(_items));

        var span = CollectionsMarshal.AsSpan(_items);

        int drawCalls = 0;
        ITexture? lastTexture = null;
        byte lastLayer = 0;
        BlendMode lastBlendMode = 0;

        for (int i = 0; i < count; i++)
        {
            ref readonly var item = ref span[_sortIndices[i]];

            if (!item.Texture.IsLoaded)
                continue;

            if (drawCalls == 0)
            {
                lastTexture = item.Texture;
                lastLayer = item.Layer;
                lastBlendMode = item.BlendMode;
                drawContext.SetRenderLayer(lastLayer);
                drawContext.SetBlendMode(lastBlendMode);
                drawCalls = 1;
            }
            else
            {
                bool textureChanged = item.Texture != lastTexture;
                bool layerChanged = item.Layer != lastLayer;
                bool blendChanged = item.BlendMode != lastBlendMode;

                if (textureChanged || layerChanged || blendChanged)
                {
                    drawCalls++;
                    lastTexture = item.Texture;

                    if (layerChanged)
                    {
                        lastLayer = item.Layer;
                        drawContext.SetRenderLayer(lastLayer);
                    }

                    if (blendChanged)
                    {
                        lastBlendMode = item.BlendMode;
                        drawContext.SetBlendMode(lastBlendMode);
                    }
                }
            }

            drawContext.DrawTexture(
                item.Texture,
                position: item.Position,
                sourceRect: item.SourceRect,
                origin: item.Origin,
                rotation: item.Rotation,
                scale: item.Scale,
                color: item.Tint,
                flip: item.Flip);
        }

        _lastEstimatedDrawCalls = drawCalls;
        _items.Clear();
        _nextInsertionOrder = 0;

        drawContext.SetRenderLayer(savedLayer);
        drawContext.SetBlendMode(savedBlendMode);

        TryShrinkBuffers(count);
    }

    /// <summary>
    /// Clears all queued sprites without rendering.
    /// </summary>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
        _items.Clear();
        _nextInsertionOrder = 0;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        ArrayPool<int>.Shared.Return(_sortIndices);
        _sortIndices = [];
    }

    private void TryShrinkBuffers(int itemCount)
    {
        if (_sortIndices.Length > Math.Max(itemCount, MinSortIndexCapacity) * ShrinkRatio)
        {
            if (++_lowUsageFrames >= ShrinkAfterFrames)
            {
                int targetCapacity = Math.Max(itemCount * 2, MinSortIndexCapacity);

                ArrayPool<int>.Shared.Return(_sortIndices);
                _sortIndices = ArrayPool<int>.Shared.Rent(targetCapacity);

                if (_items.Capacity > targetCapacity)
                    _items.Capacity = targetCapacity;

                _lowUsageFrames = 0;
            }
        }
        else
        {
            _lowUsageFrames = 0;
        }
    }

    /// <summary>
    /// Compares batch items by index into the items list, avoiding full-struct copies during sort swaps.
    /// Only 4-byte indices are swapped instead of the 80+ byte <see cref="SpriteBatchItem"/> structs.
    /// </summary>
    private readonly struct IndexComparer(List<SpriteBatchItem> items) : IComparer<int>
    {
        public int Compare(int a, int b)
        {
            var span = CollectionsMarshal.AsSpan(items);
            ref readonly var itemA = ref span[a];
            ref readonly var itemB = ref span[b];

            int layerCmp = itemA.Layer.CompareTo(itemB.Layer);
            if (layerCmp != 0) return layerCmp;
            int blendCmp = itemA.BlendMode.CompareTo(itemB.BlendMode);
            if (blendCmp != 0) return blendCmp;
            int texCmp = itemA.Texture.SortKey.CompareTo(itemB.Texture.SortKey);
            return texCmp != 0 ? texCmp : itemA.InsertionOrder.CompareTo(itemB.InsertionOrder);
        }
    }
}

/// <summary>
/// Represents a single sprite draw call in the batch.
/// </summary>
internal readonly struct SpriteBatchItem(
    ITexture texture,
    Vector2 position,
    Rectangle sourceRect,
    Vector2 scale,
    float rotation,
    Vector2 origin,
    Color tint,
    byte layer,
    SpriteFlip flip,
    BlendMode blendMode,
    int insertionOrder)
{
    public readonly ITexture Texture = texture;
    public readonly Vector2 Position = position;
    public readonly Rectangle SourceRect = sourceRect;
    public readonly Vector2 Scale = scale;
    public readonly float Rotation = rotation;
    public readonly Vector2 Origin = origin;
    public readonly Color Tint = tint;
    public readonly byte Layer = layer;
    public readonly SpriteFlip Flip = flip;
    public readonly BlendMode BlendMode = blendMode;
    public readonly int InsertionOrder = insertionOrder;
}