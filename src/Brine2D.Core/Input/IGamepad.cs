using Brine2D.Core.Math;

namespace Brine2D.Core.Input;

/// <summary>
///     Abstraction for a logical gamepad device that exposes normalized axes, button states,
///     trigger helpers with hysteresis, and rumble capabilities.
/// </summary>
/// <remarks>
///     <para>Axis ranges:</para>
///     <list type="bullet">
///         <item><description>LeftX/RightX: [-1, 1] (-1 = left, +1 = right).</description></item>
///         <item><description>LeftY/RightY: [-1, 1] (-1 = up, +1 = down).</description></item>
///         <item><description>LeftTrigger/RightTrigger: [0, 1] (0 = unpressed, 1 = fully pressed).</description></item>
///     </list>
///     <para>Edge semantics: Was*Pressed/Released methods return edges since the last input update (typically once per frame).</para>
///     <para>Processed properties (sticks/triggers) apply deadzone, inversion, and trigger thresholds.</para>
/// </remarks>
/// <example>
///     <code>
///     if (pad.WasButtonPressed(GamepadButton.A)) Jump();
///     var move = pad.LeftStick; // normalized, deadzone/inversion applied
///     var aimX = pad.GetAxis(GamepadAxis.RightX);
///     pad.TryRumble(12000, 16000, 80);
///     </code>
/// </example>
public interface IGamepad
{
    // Axis inversion options (applied to LeftStick/RightStick)

    /// <summary>
    ///     Gets or sets whether to invert the left stick X axis.
    /// </summary>
    bool InvertLeftX { get; set; }

    /// <summary>Gets or sets whether to invert the left stick Y axis.</summary>
    /// <remarks>Up is -1, down is +1 by convention; invert to flip.</remarks>
    /// <value>Defaults to <see langword="false" />.</value>
    bool InvertLeftY { get; set; }

    /// <summary>
    ///     Gets or sets whether to invert the right stick X axis.
    /// </summary>
    bool InvertRightX { get; set; }

    /// <summary>
    ///     Gets or sets whether to invert the right stick Y axis.
    /// </summary>
    /// <remarks>Note: Up is -1, Down is +1 by convention; invert to flip.</remarks>
    bool InvertRightY { get; set; }

    /// <summary>
    ///     Gets whether the device is currently connected and available.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Gets the current processed left stick vector after deadzone and inversion
    ///     are applied. X in [-1, 1], Y in [-1, 1].
    /// </summary>
    Vector2 LeftStick { get; }

    /// <summary>
    ///     Gets the current processed left trigger value in [0, 1].
    /// </summary>
    float LeftTrigger { get; }

    /// <summary>
    ///     Gets the human-readable device name, if available. May be null when the platform
    ///     does not provide a readable name.
    /// </summary>
    string? Name { get; }

    /// <summary>Gets or sets whether the stick deadzone is applied radially (true) or per-axis (false).</summary>
    /// <remarks>Radial preserves direction near center; per-axis can be preferable for independent axis activation.</remarks>
    /// <value>Defaults to <see langword="true" />.</value>
    bool RadialDeadzone { get; set; }

    /// <summary>
    ///     Gets the current processed right stick vector after deadzone and inversion
    ///     are applied. X in [-1, 1], Y in [-1, 1].
    /// </summary>
    Vector2 RightStick { get; }

    /// <summary>
    ///     Gets the current processed right trigger value in [0, 1].
    /// </summary>
    float RightTrigger { get; }

    // Deadzone settings

    /// <summary>Gets or sets the deadzone magnitude applied to sticks.</summary>
    /// <remarks>When <see cref="RadialDeadzone" /> is true, the deadzone applies to vector length; otherwise per-axis.</remarks>
    /// <value>Typical default ≈ 0.15.</value>
    float StickDeadzone { get; set; }

    // Capabilities

    /// <summary>
    ///     Gets whether the device supports main motor rumble.
    /// </summary>
    bool SupportsRumble { get; }

    /// <summary>
    ///     Gets whether the device supports trigger motor rumble.
    /// </summary>
    bool SupportsTriggerRumble { get; }

    // Trigger-as-button helpers (with hysteresis)

    /// <summary>Gets or sets the press threshold for treating triggers as buttons.</summary>
    /// <remarks>Used with <see cref="TriggerReleaseThreshold" /> to create hysteresis.</remarks>
    /// <value>Defaults to 0.5.</value>
    float TriggerPressThreshold { get; set; }

    /// <summary>Gets or sets the release threshold for treating triggers as buttons.</summary>
    /// <remarks>Should be less than <see cref="TriggerPressThreshold" /> to avoid flicker.</remarks>
    /// <value>Defaults to 0.45.</value>
    float TriggerReleaseThreshold { get; set; }

    /// <summary>
    ///     Gets the current value of a logical gamepad axis.
    /// </summary>
    /// <param name="axis">The axis to query.</param>
    /// <returns>
    ///     A normalized value:
    ///     <list type="bullet">
    ///         <item><description>LeftX/RightX: [-1, 1] (-1 = left, +1 = right).</description></item>
    ///         <item><description>LeftY/RightY: [-1, 1] (-1 = up, +1 = down).</description></item>
    ///         <item><description>LeftTrigger/RightTrigger: [0, 1] (0 = unpressed, 1 = fully pressed).</description></item>
    ///     </list>
    /// </returns>
    float GetAxis(GamepadAxis axis);

    /// <summary>
    ///     Returns whether the specified button is currently held down.
    /// </summary>
    /// <param name="button">The logical button to query.</param>
    /// <returns>true if the button is down; otherwise, false.</returns>
    bool IsButtonDown(GamepadButton button);

    /// <summary>
    ///     Stops any active main motor rumble immediately.
    /// </summary>
    void StopRumble();

    /// <summary>
    ///     Stops any active trigger motor rumble immediately.
    /// </summary>
    void StopRumbleTriggers();

    // Rumble

    /// <summary>
    ///     Attempts to start main motor rumble.
    /// </summary>
    /// <param name="lowFrequency">Low-frequency motor strength in [0, 65535].</param>
    /// <param name="highFrequency">High-frequency motor strength in [0, 65535].</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <returns>false if unsupported or the request could not be started; otherwise, true.</returns>
    /// <remarks>
    ///     Implementations should clamp values to valid ranges and may be best-effort based on platform limits.
    /// </remarks>
    bool TryRumble(ushort lowFrequency, ushort highFrequency, uint durationMs);

    /// <summary>
    ///     Attempts to start trigger motor rumble.
    /// </summary>
    /// <param name="leftTrigger">Left trigger motor strength in [0, 65535].</param>
    /// <param name="rightTrigger">Right trigger motor strength in [0, 65535].</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <returns>false if unsupported or the request could not be started; otherwise, true.</returns>
    /// <remarks>
    ///     Only available on controllers with independent trigger haptics.
    /// </remarks>
    bool TryRumbleTriggers(ushort leftTrigger, ushort rightTrigger, uint durationMs);

    /// <summary>
    ///     Returns whether the specified button transitioned to the pressed state
    ///     since the last input update (rising edge).
    /// </summary>
    /// <param name="button">The logical button to query.</param>
    /// <returns>true if pressed this update; otherwise, false.</returns>
    bool WasButtonPressed(GamepadButton button);

    /// <summary>
    ///     Returns whether the specified button transitioned to the released state
    ///     since the last input update (falling edge).
    /// </summary>
    /// <param name="button">The logical button to query.</param>
    /// <returns>true if released this update; otherwise, false.</returns>
    bool WasButtonReleased(GamepadButton button);

    /// <summary>
    ///     Returns true if the left trigger crossed from below to above
    ///     <see cref="TriggerPressThreshold" /> since the last update.
    /// </summary>
    bool WasLeftTriggerPressed();

    /// <summary>
    ///     Returns true if the left trigger crossed from above to below
    ///     <see cref="TriggerReleaseThreshold" /> since the last update.
    /// </summary>
    bool WasLeftTriggerReleased();

    /// <summary>
    ///     Returns true if the right trigger crossed from below to above
    ///     <see cref="TriggerPressThreshold" /> since the last update.
    /// </summary>
    bool WasRightTriggerPressed();

    /// <summary>
    ///     Returns true if the right trigger crossed from above to below
    ///     <see cref="TriggerReleaseThreshold" /> since the last update.
    /// </summary>
    bool WasRightTriggerReleased();
}