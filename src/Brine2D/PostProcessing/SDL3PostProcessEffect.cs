using Brine2D.Rendering;
using Brine2D.Rendering.PostProcessing;

namespace Brine2D.Rendering.SDL.PostProcessing;

/// <summary>
/// SDL3-specific post-processing effect interface.
/// Extends the base interface with GPU-specific rendering details.
/// </summary>
public interface ISDL3PostProcessEffect : IPostProcessEffect
{
    /// <summary>
    /// Apply the effect from source texture to target texture using SDL3 GPU API.
    /// </summary>
    /// <param name="renderer">The renderer to use for drawing</param>
    /// <param name="sourceTexture">Source GPU texture handle</param>
    /// <param name="targetTexture">Target GPU texture handle</param>
    /// <param name="commandBuffer">Active GPU command buffer</param>
    void Apply(IRenderer renderer, nint sourceTexture, nint targetTexture, nint commandBuffer);
}