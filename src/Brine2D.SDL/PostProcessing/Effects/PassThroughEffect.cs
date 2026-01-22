using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL.PostProcessing.Effects;

/// <summary>
/// Simple pass-through effect that copies source to target with no modifications.
/// Useful for validating the post-processing pipeline works correctly.
/// </summary>
public class PassThroughEffect : ISDL3PostProcessEffect
{
    private readonly ILogger<PassThroughEffect>? _logger;
    private int _width;
    private int _height;

    public int Order { get; set; } = 0;
    public string Name => "PassThrough";
    public bool Enabled { get; set; } = true;

    public PassThroughEffect(int width, int height, ILogger<PassThroughEffect>? logger = null)
    {
        _width = width;
        _height = height;
        _logger = logger;
    }

    /// <summary>
    /// Update dimensions when viewport changes.
    /// </summary>
    public void SetDimensions(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Apply(IRenderer renderer, nint sourceTexture, nint targetTexture, nint commandBuffer)
    {
        _logger?.LogDebug("PassThroughEffect: Copying {Source} -> {Target}", sourceTexture, targetTexture);
        
        // Simple blit operation - no shader processing
        FullScreenQuad.Blit(commandBuffer, sourceTexture, targetTexture, _width, _height, _logger);
    }
}