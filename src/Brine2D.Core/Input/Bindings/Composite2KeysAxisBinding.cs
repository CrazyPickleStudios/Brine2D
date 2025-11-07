namespace Brine2D.Core.Input.Bindings;

/// <summary>
///     Axis binding that composes two keyboard keys into a single 1D axis value.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>Pressing <see cref="Negative" /> contributes -1; pressing <see cref="Positive" /> contributes +1.</description></item>
///         <item><description>Pressing both keys simultaneously cancels out and yields 0.</description></item>
///         <item><description>The composed value is optionally inverted via <see cref="Invert" /> and then scaled by <see cref="Scale" />.</description></item>
///         <item><description>Intended to map pairs like A/D, Left/Right, or Up/Down to a normalized axis.</description></item>
///         <item><description>If either key is <see cref="Key.Unknown" />, typical <see cref="IKeyboard" /> implementations treat it as not pressed.</description></item>
///     </list>
/// </remarks>
/// <example>
///     <code><![CDATA[
/// // Move horizontally with Left/Right arrows:
/// // result in [-1..1], where -1 = left, +1 = right
/// var binding = new Composite2KeysAxisBinding(Key.Left, Key.Right);
/// var value = binding.Get(keyboard, mouse, gamepad, dt);
/// ]]></code>
/// </example>
public sealed class Composite2KeysAxisBinding : IAxisBinding
{
    /// <summary>
    ///     Creates a new binding with default values.
    /// </summary>
    /// <remarks>
    ///     By default, <see cref="Negative" /> and <see cref="Positive" /> are <see cref="Key.Unknown" />,
    ///     <see cref="Scale" /> is 1, and <see cref="Invert" /> is false.
    /// </remarks>
    public Composite2KeysAxisBinding()
    {
    }

    /// <summary>
    ///     Creates a new two-key axis binding.
    /// </summary>
    /// <param name="negative">Key that contributes -1 when held.</param>
    /// <param name="positive">Key that contributes +1 when held.</param>
    /// <param name="scale">Scalar multiplier applied to the composed value. Default is 1.</param>
    public Composite2KeysAxisBinding(Key negative, Key positive, float scale = 1f)
    {
        Negative = negative;
        Positive = positive;
        Scale = scale;
    }

    /// <summary>
    ///     If true, flips the sign of the composed value before scaling.
    /// </summary>
    /// <value>Defaults to <see langword="false" />.</value>
    public bool Invert { get; set; } = false;

    /// <summary>
    ///     Key that contributes -1 to the axis when held down.
    /// </summary>
    /// <value>Defaults to <see cref="Key.Unknown" />.</value>
    public Key Negative { get; set; }

    /// <summary>
    ///     Key that contributes +1 to the axis when held down.
    /// </summary>
    /// <value>Defaults to <see cref="Key.Unknown" />.</value>
    public Key Positive { get; set; }

    /// <summary>
    ///     Scalar multiplier applied to the final axis value after inversion.
    /// </summary>
    /// <remarks>
    ///     Use to adjust sensitivity or remap the range. Typical values are in [0..1], but larger values are allowed.
    /// </remarks>
    /// <value>Defaults to 1.</value>
    public float Scale { get; set; } = 1f;

    /// <summary>
    ///     Computes the current axis value for this binding.
    /// </summary>
    /// <param name="kb">Keyboard input source used to query key states.</param>
    /// <param name="mouse">Unused for this binding.</param>
    /// <param name="pad">Unused for this binding.</param>
    /// <param name="dt">Delta time for this sample (unused; axis is level-based).</param>
    /// <returns>
    ///     A value in [-Scale..+Scale] where -Scale corresponds to <see cref="Negative" />,
    ///     +Scale corresponds to <see cref="Positive" />, and 0 when neither or both are pressed.
    /// </returns>
    /// <remarks>
    ///     This is a level query (continuous while held), not edge-triggered.
    /// </remarks>
    public float Get(IKeyboard kb, IMouse mouse, IGamepad? pad, double dt)
    {
        var v = 0f;

        if (kb.IsKeyDown(Negative))
        {
            v -= 1f;
        }

        if (kb.IsKeyDown(Positive))
        {
            v += 1f;
        }

        if (Invert)
        {
            v = -v;
        }

        return v * Scale;
    }
}