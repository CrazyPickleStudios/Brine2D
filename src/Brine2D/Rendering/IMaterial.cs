namespace Brine2D.Rendering;

/// <summary>
/// Marker interface for custom sprite materials.
/// Implement this to carry per-sprite shader parameters or pipeline overrides.
/// </summary>
/// <remarks>
/// <para>
/// In this release, <see cref="IMaterial"/> is a hook for future per-sprite shader support.
/// A material assigned to <see cref="Brine2D.Systems.Rendering.SpriteComponent.Material"/>
/// is stored and can be read by custom render systems, but the built-in
/// <see cref="Brine2D.Systems.Rendering.SpriteRenderingSystem"/> does not yet switch
/// GPU pipelines based on the material value.
/// </para>
/// <para>
/// Future: when per-sprite shader support is added the renderer will query
/// <c>Material.PipelineKey</c> or similar to select the correct pipeline.
/// </para>
/// </remarks>
public interface IMaterial
{
    /// <summary>
    /// A short name used for diagnostics and logging.
    /// </summary>
    string Name { get; }
}
