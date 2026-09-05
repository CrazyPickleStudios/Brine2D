using Brine2D.Core;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL.PostProcessing.Effects;

/// <summary>
/// Post-processing effect that scales the fixed-design-resolution main render target
/// into a centered, aspect-preserving rect within the current swapchain, filling the
/// remaining letterbox/pillarbox bars with <see cref="BarColor"/>.
/// </summary>
/// <remarks>
/// Unlike other <see cref="ISDL3PostProcessEffect"/> implementations, this effect's
/// "dimensions" (via <see cref="SetDimensions"/>) represent the live swapchain size,
/// not its own render size — the design resolution is fixed at construction and never
/// changes. This effect is intended to always run last in the pipeline.
/// </remarks>
public sealed class LetterboxEffect : ISDL3PostProcessEffect
{
    private readonly ILogger<LetterboxEffect>? _logger;
    private readonly int _designWidth;
    private readonly int _designHeight;
    private int _swapchainWidth;
    private int _swapchainHeight;

    public int Order { get; set; } = int.MaxValue;
    public string Name => "Letterbox";
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Color used to fill the letterbox/pillarbox bars. Defaults to opaque black.
    /// </summary>
    public Color BarColor { get; set; } = new(0, 0, 0, 255);

    public LetterboxEffect(int designWidth, int designHeight, ILogger<LetterboxEffect>? logger = null)
    {
        if (designWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(designWidth), "Design width must be positive");
        if (designHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(designHeight), "Design height must be positive");

        _designWidth = designWidth;
        _designHeight = designHeight;
        _swapchainWidth = designWidth;
        _swapchainHeight = designHeight;
        _logger = logger;
    }

    /// <summary>
    /// Updates the tracked swapchain (window) dimensions. Called on window resize.
    /// </summary>
    public void SetDimensions(int width, int height)
    {
        _swapchainWidth = width;
        _swapchainHeight = height;
    }

    /// <summary>
    /// Computes the centered, aspect-preserving destination rect for the design
    /// resolution within the given swapchain dimensions.
    /// </summary>
    public (int X, int Y, int Width, int Height) ComputeFitRect(int swapchainWidth, int swapchainHeight)
    {
        if (swapchainWidth <= 0 || swapchainHeight <= 0)
            return (0, 0, 0, 0);

        double designAspect = (double)_designWidth / _designHeight;
        double swapchainAspect = (double)swapchainWidth / swapchainHeight;

        int fitWidth, fitHeight;
        if (swapchainAspect > designAspect)
        {
            // Window is wider than the design aspect: fit to height, pillarbox left/right.
            fitHeight = swapchainHeight;
            fitWidth = (int)Math.Round(fitHeight * designAspect);
        }
        else
        {
            // Window is taller than (or equal to) the design aspect: fit to width, letterbox top/bottom.
            fitWidth = swapchainWidth;
            fitHeight = (int)Math.Round(fitWidth / designAspect);
        }

        fitWidth = Math.Clamp(fitWidth, 1, swapchainWidth);
        fitHeight = Math.Clamp(fitHeight, 1, swapchainHeight);

        int x = (swapchainWidth - fitWidth) / 2;
        int y = (swapchainHeight - fitHeight) / 2;

        return (x, y, fitWidth, fitHeight);
    }

    public void Apply(IRenderer renderer, nint sourceTexture, nint targetTexture, nint commandBuffer)
    {
        var (x, y, width, height) = ComputeFitRect(_swapchainWidth, _swapchainHeight);

        _logger?.LogDebug(
            "LetterboxEffect: Fitting {DesignWidth}x{DesignHeight} into {SwapchainWidth}x{SwapchainHeight} -> rect ({X}, {Y}, {Width}, {Height})",
            _designWidth, _designHeight, _swapchainWidth, _swapchainHeight, x, y, width, height);

        FullScreenQuad.Clear(commandBuffer, targetTexture, _swapchainWidth, _swapchainHeight, BarColor, _logger);

        FullScreenQuad.Blit(
            commandBuffer,
            sourceTexture,
            targetTexture,
            sourceX: 0,
            sourceY: 0,
            sourceWidth: _designWidth,
            sourceHeight: _designHeight,
            destX: x,
            destY: y,
            destWidth: width,
            destHeight: height,
            logger: _logger);
    }
}
