using System.Diagnostics;
using System.Numerics;
using Brine2D.Core;
using Brine2D.Rendering.Text;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering;

/// <summary>
///     No-op renderer for headless mode (servers, testing).
///     All rendering operations are silently ignored.
///     <para>
///         <see cref="CreateRenderTarget" /> is the sole exception: render targets require GPU
///         infrastructure that does not exist in headless mode and throw <see cref="NotSupportedException" />.
///     </para>
/// </summary>
internal sealed class HeadlessRenderer : IRenderer
{
    private readonly ILogger<HeadlessRenderer>? _logger;
    private readonly Stack<IRenderTarget?> _renderTargetStack = new();
    private readonly Stack<Rectangle?> _scissorRectStack = new();
    private readonly int _width;
    private readonly int _height;
    private BlendMode _blendMode = IRenderer.DefaultBlendMode;
    private IRenderTarget? _currentRenderTarget;
    private int _disposed;
    private byte _renderLayer = IRenderer.DefaultRenderLayer;
    private Rectangle? _scissorRect;

    public ICamera? Camera { get; set; }
    public Color ClearColor { get; set; }

    /// <inheritdoc />
    /// <remarks>
    ///     Returns <c>1</c> by default in headless mode to prevent division-by-zero.
    ///     Use the <see cref="HeadlessRenderer(int, int, ILogger{HeadlessRenderer})"/> constructor to specify a custom size
    ///     for layout-dependent tests.
    /// </remarks>
    public int Height => _height;

    public bool IsInitialized => true;

    /// <inheritdoc />
    /// <remarks>
    ///     Returns <c>1</c> by default in headless mode to prevent division-by-zero.
    ///     Use the <see cref="HeadlessRenderer(int, int, ILogger{HeadlessRenderer})"/> constructor to specify a custom size
    ///     for layout-dependent tests.
    /// </remarks>
    public int Width => _width;

    /// <summary>
    ///     Creates a headless renderer with default 1×1 viewport (safe for DI resolution).
    /// </summary>
    public HeadlessRenderer() : this(1, 1)
    {
    }

    /// <summary>
    ///     Creates a headless renderer with a custom viewport size for layout-dependent tests.
    /// </summary>
    /// <param name="width">Viewport width (clamped to at least 1).</param>
    /// <param name="height">Viewport height (clamped to at least 1).</param>
    /// <param name="logger">Optional logger for diagnostics in all build configurations.</param>
    public HeadlessRenderer(int width, int height, ILogger<HeadlessRenderer>? logger = null)
    {
        _width = Math.Max(width, 1);
        _height = Math.Max(height, 1);
        _logger = logger;
    }

    public void ApplyPostProcessing()
    {
        if (Volatile.Read(ref _disposed) == 1)
            return;
    }

    public void BeginFrame()
    {
        if (Volatile.Read(ref _disposed) == 1)
            return;

        Debug.Assert(_renderTargetStack.Count == 0,
            $"Render target stack had {_renderTargetStack.Count} unpopped entries at frame boundary");
        Debug.Assert(_scissorRectStack.Count == 0,
            $"Scissor rect stack had {_scissorRectStack.Count} unpopped entries at frame boundary");

        if (_renderTargetStack.Count > 0)
        {
            _logger?.LogWarning("Render target stack had {Count} unpopped entries at frame boundary", _renderTargetStack.Count);
            _renderTargetStack.Clear();
        }

        if (_scissorRectStack.Count > 0)
        {
            _logger?.LogWarning("Scissor rect stack had {Count} unpopped entries at frame boundary", _scissorRectStack.Count);
            _scissorRectStack.Clear();
        }

        _blendMode = IRenderer.DefaultBlendMode;
        _renderLayer = IRenderer.DefaultRenderLayer;
        _scissorRect = null;
        _currentRenderTarget = null;
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">
    ///     Always thrown — render targets require GPU infrastructure unavailable in headless mode.
    /// </exception>
    public IRenderTarget CreateRenderTarget(int width, int height)
    {
        ThrowIfDisposed();

        throw new NotSupportedException(
            "Render targets require GPU infrastructure unavailable in headless mode.");
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;
    }

    public void DrawCircleFilled(float centerX, float centerY, float radius, Color color)
    {
        ThrowIfDisposed();
    }

    public void DrawCircleFilled(Vector2 center, float radius, Color color)
    {
        ThrowIfDisposed();
    }

    public void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1f)
    {
        ThrowIfDisposed();
    }

    public void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness = 1f)
    {
        ThrowIfDisposed();
    }

    public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1f)
    {
        ThrowIfDisposed();
    }

    public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f)
    {
        ThrowIfDisposed();
    }

    public void DrawRectangleFilled(float x, float y, float width, float height, Color color)
    {
        ThrowIfDisposed();
    }

    public void DrawRectangleFilled(Rectangle rect, Color color)
    {
        ThrowIfDisposed();
    }

    public void DrawRectangleOutline(float x, float y, float width, float height, Color color, float thickness = 1f)
    {
        ThrowIfDisposed();
    }

    public void DrawRectangleOutline(Rectangle rect, Color color, float thickness = 1f)
    {
        ThrowIfDisposed();
    }

    public void DrawText(string text, float x, float y, Color color)
    {
        ThrowIfDisposed();
    }

    public void DrawText(string text, float x, float y, TextRenderOptions options)
    {
        ThrowIfDisposed();
    }

    public void DrawTexture(ITexture texture, Vector2 position,
        Rectangle? sourceRect = null, Vector2? origin = null,
        float rotation = 0f, Vector2? scale = null,
        Color? color = null, SpriteFlip flip = SpriteFlip.None)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(texture);
    }

    public void DrawTexture(ITexture texture, Vector2 position)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(texture);
    }

    public void DrawTexture(ITexture texture, float x, float y)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(texture);
    }

    public void DrawTexture(ITexture texture, float x, float y, float width, float height)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(texture);
    }

    public void EndFrame()
    {
        if (Volatile.Read(ref _disposed) == 1)
            return;

        if (_renderTargetStack.Count > 0)
        {
            _logger?.LogWarning("Render target stack has {Count} unpopped entries at EndFrame", _renderTargetStack.Count);
            _renderTargetStack.Clear();
        }

        if (_scissorRectStack.Count > 0)
        {
            _logger?.LogWarning("Scissor rect stack has {Count} unpopped entries at EndFrame", _scissorRectStack.Count);
            _scissorRectStack.Clear();
        }

        Debug.Assert(_renderTargetStack.Count == 0,
            $"Render target stack has {_renderTargetStack.Count} unpopped entries at EndFrame");
        Debug.Assert(_scissorRectStack.Count == 0,
            $"Scissor rect stack has {_scissorRectStack.Count} unpopped entries at EndFrame");

        _currentRenderTarget = null;
        _scissorRect = null;
    }

    public BlendMode GetBlendMode()
    {
        ThrowIfDisposed();
        return _blendMode;
    }

    public byte GetRenderLayer()
    {
        ThrowIfDisposed();
        return _renderLayer;
    }

    public IRenderTarget? GetRenderTarget()
    {
        ThrowIfDisposed();
        return _currentRenderTarget;
    }

    public Rectangle? GetScissorRect()
    {
        ThrowIfDisposed();
        return _scissorRect;
    }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Returns a rough character-count estimate in headless mode because no font atlas
    ///     is available without GPU infrastructure. The width assumes ~0.6× the font height
    ///     per character (a common monospace-ish heuristic). Layout code that requires pixel-exact
    ///     measurements should avoid headless mode for UI integration tests.
    /// </remarks>
    public Vector2 MeasureText(string text, float? fontSize = null)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        float size = fontSize ?? 16f;
        float lineHeight = size * 1.2f;
        float charWidth = size * 0.6f;

        float maxLineWidth = 0f;
        int lineCount = 1;
        int currentLineLength = 0;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                maxLineWidth = MathF.Max(maxLineWidth, currentLineLength * charWidth);
                currentLineLength = 0;
                lineCount++;
            }
            else if (c != '\r')
            {
                currentLineLength++;
            }
        }

        maxLineWidth = MathF.Max(maxLineWidth, currentLineLength * charWidth);
        return new Vector2(maxLineWidth, lineCount * lineHeight);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Returns a rough character-count estimate in headless mode because no font atlas
    ///     is available without GPU infrastructure. The width assumes ~0.6× the font height
    ///     per character (a common monospace-ish heuristic). Layout code that requires pixel-exact
    ///     measurements should avoid headless mode for UI integration tests.
    /// </remarks>
    public Vector2 MeasureText(string text, TextRenderOptions options)
    {
        ThrowIfDisposed();
        return MeasureText(text, options.FontSize);
    }

    public void PopRenderTarget()
    {
        ThrowIfDisposed();

        if (_renderTargetStack.Count == 0)
        {
            throw new InvalidOperationException("Cannot pop render target: stack is empty");
        }

        _currentRenderTarget = _renderTargetStack.Pop();
    }

    public void PopScissorRect()
    {
        ThrowIfDisposed();

        if (_scissorRectStack.Count == 0)
        {
            throw new InvalidOperationException("Cannot pop scissor rect: stack is empty");
        }

        _scissorRect = _scissorRectStack.Pop();
    }

    public void PushRenderTarget(IRenderTarget? target)
    {
        ThrowIfDisposed();
        _renderTargetStack.Push(_currentRenderTarget);
        _currentRenderTarget = target;
    }

    public void PushScissorRect(Rectangle? rect)
    {
        ThrowIfDisposed();
        _scissorRectStack.Push(_scissorRect);
        SetScissorRect(ScissorRectHelper.Intersect(_scissorRect, rect));
    }

    public void SetBlendMode(BlendMode blendMode)
    {
        ThrowIfDisposed();

        if (!Enum.IsDefined(blendMode))
            throw new ArgumentOutOfRangeException(nameof(blendMode), blendMode, "Undefined blend mode");

        _blendMode = blendMode;
    }

    public void SetDefaultFont(IFont? font)
    {
        ThrowIfDisposed();
    }

    public void SetRenderLayer(byte layer)
    {
        ThrowIfDisposed();
        _renderLayer = layer;
    }

    public void SetRenderTarget(IRenderTarget? target)
    {
        ThrowIfDisposed();
        _currentRenderTarget = target;
    }

    public void SetScissorRect(Rectangle? rect)
    {
        ThrowIfDisposed();

        if (rect.HasValue)
        {
            var r = rect.Value;
            if (r.Width < 0 || r.Height < 0)
            {
                throw new ArgumentException(
                    "Scissor rectangle dimensions cannot be negative",
                    nameof(rect));
            }

            float maxW = _currentRenderTarget?.Width ?? _width;
            float maxH = _currentRenderTarget?.Height ?? _height;

            float clampedX = Math.Max(r.X, 0);
            float clampedY = Math.Max(r.Y, 0);
            float clampedRight = Math.Min(r.X + r.Width, maxW);
            float clampedBottom = Math.Min(r.Y + r.Height, maxH);
            float clampedW = Math.Max(clampedRight - clampedX, 0);
            float clampedH = Math.Max(clampedBottom - clampedY, 0);

            _scissorRect = new Rectangle(clampedX, clampedY, clampedW, clampedH);
            return;
        }

        _scissorRect = null;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed == 1, this);
}