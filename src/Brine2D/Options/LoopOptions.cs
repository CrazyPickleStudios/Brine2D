namespace Brine2D.Options;

/// <summary>
///     Configuration options for the main game loop timing.
///     Controls fixed-step simulation, maximum frame rate, and timing mode.
/// </summary>
public class LoopOptions
{
    /// <summary>
    ///     Duration of a single fixed simulation step in seconds.
    ///     Common default is 1/60 (approximately 16.67 ms).
    ///     Used only when <see cref="UseFixedStep" /> is true.
    /// </summary>
    public double FixedStepSeconds { get; set; } = 1.0 / 60.0;

    /// <summary>
    ///     Optional cap on the rendering frames per second (FPS).
    ///     When <c>null</c>, rendering is uncapped and will run as fast as possible.
    /// </summary>
    public int? MaxFps { get; set; } = null;

    /// <summary>
    ///     When true, the loop runs the simulation using fixed steps of <see cref="FixedStepSeconds" />.
    ///     When false, the simulation uses variable delta time based on real elapsed time.
    /// </summary>
    public bool UseFixedStep { get; set; } = true;
}