namespace Brine2D.Rendering;

/// <summary>
/// Scoped helpers for <see cref="IDrawContext"/> state that auto-restore on dispose.
/// </summary>
public static class DrawContextExtensions
{
    /// <summary>
    /// Sets the blend mode and returns a scope that restores the previous mode on dispose.
    /// </summary>
    /// <example>
    /// <code>
    /// using (drawContext.UseBlendMode(BlendMode.Additive))
    /// {
    ///     drawContext.DrawTexture(glowTexture, position);
    /// }
    /// // Previous blend mode is restored here.
    /// </code>
    /// </example>
    public static BlendModeScope UseBlendMode(this IDrawContext context, BlendMode blendMode)
    {
        return new BlendModeScope(context, blendMode);
    }

    /// <summary>
    /// Sets the render layer and returns a scope that restores the previous layer on dispose.
    /// </summary>
    /// <example>
    /// <code>
    /// using (drawContext.UseRenderLayer(200))
    /// {
    ///     drawContext.DrawTexture(foregroundTexture, position);
    /// }
    /// // Previous render layer is restored here.
    /// </code>
    /// </example>
    public static RenderLayerScope UseRenderLayer(this IDrawContext context, byte layer)
    {
        return new RenderLayerScope(context, layer);
    }
}

/// <summary>
/// Restores the previous <see cref="BlendMode"/> when disposed.
/// </summary>
public readonly struct BlendModeScope : IDisposable
{
    private readonly IDrawContext _context;
    private readonly BlendMode _previous;

    public BlendModeScope(IDrawContext context, BlendMode blendMode)
    {
        _context = context;
        _previous = context.GetBlendMode();
        context.SetBlendMode(blendMode);
    }

    public void Dispose()
    {
        _context.SetBlendMode(_previous);
    }
}

/// <summary>
/// Restores the previous render layer when disposed.
/// </summary>
public readonly struct RenderLayerScope : IDisposable
{
    private readonly IDrawContext _context;
    private readonly byte _previous;

    public RenderLayerScope(IDrawContext context, byte layer)
    {
        _context = context;
        _previous = context.GetRenderLayer();
        context.SetRenderLayer(layer);
    }

    public void Dispose()
    {
        _context.SetRenderLayer(_previous);
    }
}