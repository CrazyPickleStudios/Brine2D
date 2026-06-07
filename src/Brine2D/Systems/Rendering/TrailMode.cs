namespace Brine2D.Systems.Rendering;

/// <summary>
/// Controls how particle trail history is rendered.
/// </summary>
public enum TrailMode
{
    /// <summary>
    /// Each history slot is rendered as an individual sprite or circle.
    /// Works with all particle shapes but may leave visible gaps at high speed or low
    /// trail density.
    /// </summary>
    Sprites,

    /// <summary>
    /// Consecutive history positions are connected by <c>DrawLine</c> calls, producing a
    /// continuous ribbon. Trail size is used as the line thickness.
    /// Not available for textured particles — falls back to <see cref="Sprites"/> when a
    /// texture or atlas region is set.
    /// </summary>
    Lines,
}