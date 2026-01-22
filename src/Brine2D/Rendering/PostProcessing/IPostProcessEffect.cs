namespace Brine2D.Rendering.PostProcessing;

/// <summary>
/// Interface for post-processing effects that transform rendered output.
/// Effects are executed in order based on the Order property.
/// Backend-agnostic interface - implementations handle GPU-specific details.
/// </summary>
public interface IPostProcessEffect
{
    /// <summary>
    /// Execution order (lower values execute first).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Name of the effect for debugging/logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this effect is currently enabled.
    /// </summary>
    bool Enabled { get; set; }
}