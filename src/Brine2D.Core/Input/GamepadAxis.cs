namespace Brine2D.Core.Input;

/// <summary>
///     Logical gamepad analog axes used by the input system.
/// </summary>
/// <remarks>
///     <para>Ranges and conventions:</para>
///     <list type="bullet">
///         <item>
///             <description><see cref="LeftX" /> / <see cref="RightX" />: normalized to [-1, 1] (-1 = left, +1 = right).</description>
///         </item>
///         <item>
///             <description><see cref="LeftY" /> / <see cref="RightY" />: normalized to [-1, 1] (-1 = up, +1 = down).</description>
///         </item>
///         <item>
///             <description>
///                 <see cref="LeftTrigger" /> / <see cref="RightTrigger" />: normalized to [0, 1] (0 = unpressed,
///                 1 = fully pressed).
///             </description>
///         </item>
///     </list>
///     <para>Values are device-agnostic and should be mapped from platform APIs in an adapter layer.</para>
/// </remarks>
public enum GamepadAxis
{
    /// <summary>
    ///     Left stick horizontal axis. -1 = left, +1 = right.
    /// </summary>
    LeftX,

    /// <summary>
    ///     Left stick vertical axis. -1 = up, +1 = down.
    /// </summary>
    LeftY,

    /// <summary>
    ///     Right stick horizontal axis. -1 = left, +1 = right.
    /// </summary>
    RightX,

    /// <summary>
    ///     Right stick vertical axis. -1 = up, +1 = down.
    /// </summary>
    RightY,

    /// <summary>
    ///     Left trigger. 0 = unpressed, 1 = fully pressed.
    /// </summary>
    LeftTrigger,

    /// <summary>
    ///     Right trigger. 0 = unpressed, 1 = fully pressed.
    /// </summary>
    RightTrigger
}