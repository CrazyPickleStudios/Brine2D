using System.ComponentModel.DataAnnotations;

namespace Brine2D.Assets;

/// <summary>
/// Configuration for the asset loader: preload parallelism and teardown behaviour.
/// </summary>
public class AssetOptions
{
    /// <summary>
    /// Maximum degree of parallelism for <see cref="IAssetLoader.PreloadAsync"/>.
    /// Asset loading is I/O-bound (disk reads, decode, GPU upload), so a value above
    /// <see cref="Environment.ProcessorCount"/> is usually beneficial.
    /// <see langword="null"/> (default) uses <c>Environment.ProcessorCount * 2</c>, capped at 32.
    /// </summary>
    [Range(1, 128, ErrorMessage = "MaxPreloadParallelism must be between 1 and 128 if specified.")]
    public int? MaxPreloadParallelism { get; set; }

    internal int EffectiveParallelism
        => MaxPreloadParallelism ?? Math.Min(Environment.ProcessorCount * 2, 32);
}