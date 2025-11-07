namespace Brine2D.Core.Input.Bindings;

/// <summary>
///     Axis binding that sources its value from a mouse axis (movement or wheel).
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description><see cref="MouseAxis.MoveX" /> / <see cref="MouseAxis.MoveY" /> use <see cref="IMouse.DeltaX" /> / <see cref="IMouse.DeltaY" /> (pixels since last frame/poll).</description></item>
///         <item><description><see cref="MouseAxis.WheelX" /> / <see cref="MouseAxis.WheelY" /> use <see cref="IMouse.WheelX" /> / <see cref="IMouse.WheelY" /> (platform-defined wheel deltas since last frame/poll).</description></item>
///         <item><description>The raw value is optionally inverted and scaled via <see cref="Invert" /> and <see cref="Scale" />.</description></item>
///     </list>
/// </remarks>
public sealed class MouseAxisBinding : IAxisBinding
{
    /// <summary>
    ///     Parameterless constructor (useful for serializers/DI).
    /// </summary>
    public MouseAxisBinding()
    {
    }

    /// <summary>
    ///     Creates a new mouse axis binding.
    /// </summary>
    /// <param name="axis">The mouse axis to read.</param>
    /// <param name="scale">A scalar applied to the sampled value. Default is 1.</param>
    /// <param name="invert">If true, negates the sampled value before scaling.</param>
    public MouseAxisBinding(MouseAxis axis, float scale = 1f, bool invert = false)
    {
        Axis = axis;
        Scale = scale;
        Invert = invert;
    }

    /// <summary>
    ///     The mouse axis this binding samples.
    /// </summary>
    public MouseAxis Axis { get; set; }

    /// <summary>
    ///     When true, the sampled value is negated before scaling.
    /// </summary>
    /// <value>Defaults to <see langword="false" />.</value>
    public bool Invert { get; set; }

    /// <summary>
    ///     Multiplier applied to the sampled axis value.
    /// </summary>
    /// <value>Defaults to 1.</value>
    public float Scale { get; set; } = 1f;

    /// <summary>
    ///     Returns the current contribution of this mouse axis binding.
    /// </summary>
    /// <param name="kb">Keyboard (unused).</param>
    /// <param name="mouse">Mouse source to sample.</param>
    /// <param name="pad">Gamepad (unused).</param>
    /// <param name="dt">Delta time (seconds) since last update; unused here.</param>
    /// <returns>
    ///     The sampled axis value after inversion and scaling.
    ///     For MoveX/MoveY this is pixels per frame/poll; for WheelX/WheelY it's platform wheel units per frame/poll.
    /// </returns>
    /// <remarks>
    ///     This binding uses per-frame/poll deltas from the input system; <paramref name="dt" /> is not used.
    /// </remarks>
    public float Get(IKeyboard kb, IMouse mouse, IGamepad? pad, double dt)
    {
        var v = Axis switch
        {
            MouseAxis.MoveX => mouse.DeltaX,
            MouseAxis.MoveY => mouse.DeltaY,
            MouseAxis.WheelX => mouse.WheelX,
            MouseAxis.WheelY => mouse.WheelY,
            _ => 0f
        };

        if (Invert)
        {
            v = -v;
        }

        return v * Scale;
    }
}