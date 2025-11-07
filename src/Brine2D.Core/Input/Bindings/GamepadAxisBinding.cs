namespace Brine2D.Core.Input.Bindings;

/// <summary>
///     Binds a single logical gamepad axis to an abstract input axis value.
/// </summary>
/// <remarks>
///     <para>Processing steps:</para>
///     <list type="bullet">
///         <item>
///             <description>Read a normalized axis from <see cref="IGamepad.GetAxis(GamepadAxis)" />.</description>
///         </item>
///         <item>
///             <description>Apply a per-axis dead zone (sticks) or threshold (triggers).</description>
///         </item>
///         <item>
///             <description>Remap [dead zone..1] to [0..1], preserving sign for bidirectional axes.</description>
///         </item>
///         <item>
///             <description>Apply response curve (<see cref="Curve" />) and <see cref="Sensitivity" />.</description>
///         </item>
///         <item>
///             <description>Optionally invert (<see cref="Invert" />), then clamp to [-1, 1].</description>
///         </item>
///     </list>
///     <para>
///         Note: stick dead zone here is per-axis. For radial dead zone, use stick vectors exposed by
///         <see cref="IGamepad" />.
///     </para>
/// </remarks>
public sealed class GamepadAxisBinding : IAxisBinding
{
    /// <summary>
    ///     Initializes a new binding with default parameters.
    /// </summary>
    public GamepadAxisBinding()
    {
    }

    /// <summary>
    ///     Initializes a new binding for the specified <paramref name="axis" />.
    /// </summary>
    /// <param name="axis">Logical axis to read.</param>
    /// <param name="deadZone">
    ///     For sticks: per-axis dead zone in [0..1] (typical ~0.15).
    ///     For triggers: acts as a threshold below which the value is 0.
    /// </param>
    /// <param name="sensitivity">Scalar multiplier applied after curve and remap.</param>
    /// <param name="invert">If true, final value is negated.</param>
    /// <param name="curve">
    ///     Response curve exponent. 1 = linear; &gt; 1 softens near center; &lt; 1 increases center sensitivity.
    /// </param>
    /// <param name="padIndex">
    ///     Optional target pad index. Not used here; intended for higher-level routing that chooses which
    ///     <see cref="IGamepad" /> instance to pass into <see cref="Get(IKeyboard, IMouse, IGamepad, double)" />.
    /// </param>
    public GamepadAxisBinding(GamepadAxis axis, float deadZone = 0.15f, float sensitivity = 1f, bool invert = false,
        float curve = 1f, int? padIndex = null)
    {
        Axis = axis;
        DeadZone = deadZone;
        Sensitivity = sensitivity;
        Invert = invert;
        Curve = curve;
        PadIndex = padIndex;
    }

    /// <summary>
    ///     The logical gamepad axis to read.
    /// </summary>
    public GamepadAxis Axis { get; set; }

    /// <summary>
    ///     Response curve exponent. 1 = linear. Values &gt; 1 soften the center; values &lt; 1 increase center sensitivity.
    /// </summary>
    /// <value>Defaults to 1.</value>
    public float Curve { get; set; } = 1f; // 1=linear, >1 softer center (pow)

    /// <summary>
    ///     Dead zone magnitude applied per-axis for sticks, or threshold for triggers.
    ///     Expected range [0..1]. Values near 0.1–0.2 are common for sticks.
    /// </summary>
    /// <value>Defaults to 0.15.</value>
    public float DeadZone { get; set; } = 0.15f; // for sticks; triggers are 0..1 so this acts as threshold

    /// <summary>
    ///     If true, negates the final value (after sensitivity and curve).
    /// </summary>
    /// <value>Defaults to <see langword="false" />.</value>
    public bool Invert { get; set; }

    /// <summary>
    ///     Optional pad index this binding targets. Not consumed by this class; intended for external routing.
    /// </summary>
    /// <value>Defaults to <c>null</c> (use caller-resolved pad).</value>
    public int? PadIndex { get; set; }

    /// <summary>
    ///     Output multiplier applied after remap and curve. Useful for tuning responsiveness.
    /// </summary>
    /// <value>Defaults to 1.</value>
    public float Sensitivity { get; set; } = 1f;

    /// <summary>
    ///     Computes this binding's contribution using the provided input sources.
    /// </summary>
    /// <param name="kb">Keyboard (unused).</param>
    /// <param name="mouse">Mouse (unused).</param>
    /// <param name="pad">Gamepad to read. If null or disconnected, returns 0.</param>
    /// <param name="dt">Delta time (unused).</param>
    /// <returns>Value ideally in [-1..1] after processing.</returns>
    public float Get(IKeyboard kb, IMouse mouse, IGamepad? pad, double dt)
    {
        // Validate gamepad availability
        if (pad is null || !pad.IsConnected)
        {
            return 0f;
        }

        // Read raw logical axis (already normalized by the adapter)
        var v = pad.GetAxis(Axis);

        // Apply dead zone (per-axis). For triggers (0..1), this behaves as a threshold.
        var av = MathF.Abs(v);
        if (av <= DeadZone)
        {
            return 0f;
        }

        // Remap from [dead zone..1] -> [0..1] while preserving sign for bidirectional axes.
        float sign = MathF.Sign(v);
        var t = (av - DeadZone) / MathF.Max(1e-6f, 1f - DeadZone);

        // Response curve. 1 = linear.
        if (Curve != 1f)
        {
            t = MathF.Pow(t, Curve);
        }

        // Apply sign and sensitivity
        var outv = t * sign * Sensitivity;

        // Optional inversion
        if (Invert)
        {
            outv = -outv;
        }

        // Final clamp to canonical range
        return System.Math.Clamp(outv, -1f, 1f);
    }
}