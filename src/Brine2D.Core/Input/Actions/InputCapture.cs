using Brine2D.Core.Input.Bindings;

namespace Brine2D.Core.Input.Actions;

/// <summary>
///     Utilities for capturing user input bindings from the current frame.
/// </summary>
/// <remarks>
///     <para>Call after input devices have been updated for the frame. The first detected match is returned.</para>
///     <list type="bullet">
///         <item><description>Digital actions: keyboard (non‑modifier) then mouse buttons, then gamepad buttons/triggers.</description></item>
///         <item><description>Analog axes: mouse movement or wheel first, then gamepad axes.</description></item>
///     </list>
/// </remarks>
public static class InputCapture
{
    /// <summary>
    ///     Tries to capture a digital action binding based on edge events observed this frame.
    /// </summary>
    /// <param name="kb">Keyboard source for edge queries.</param>
    /// <param name="mods">Currently held modifier flags to include in a chord binding.</param>
    /// <param name="mouse">Mouse source for button edges.</param>
    /// <param name="pad">Optional gamepad source for button/trigger edges.</param>
    /// <param name="binding">Outputs the first matching binding if found.</param>
    /// <returns>True if a binding was captured this frame; otherwise, false.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Keyboard: captures non‑modifier keys only, combined with current <paramref name="mods" />.</description></item>
    ///         <item><description>Mouse: captures any button press edge.</description></item>
    ///         <item><description>Gamepad: captures any button press edge or trigger press edge using device thresholds.</description></item>
    ///         <item><description>Order is keyboard → mouse → gamepad; the first match short‑circuits.</description></item>
    ///     </list>
    /// </remarks>
    public static bool TryCaptureAction(IKeyboard kb, KeyboardModifiers mods, IMouse mouse, IGamepad? pad,
        out IActionBinding? binding)
    {
        // Keyboard press edges (skip pure modifiers/locks to avoid accidental captures like Shift alone).
        foreach (var k in Enum.GetValues<Key>())
        {
            if (!kb.WasKeyPressed(k))
            {
                continue;
            }

            if (IsModifierKey(k))
            {
                continue;
            }

            // Capture key + current modifiers as a chord (e.g., Ctrl+K).
            binding = new KeyChordBinding(new KeyChord(k, mods));
            return true;
        }

        // Mouse button press edges.
        foreach (var b in Enum.GetValues<MouseButton>())
        {
            if (mouse.WasButtonPressed(b))
            {
                binding = new MouseButtonBinding(b);
                return true;
            }
        }

        // Gamepad: buttons and triggers as digital inputs.
        if (pad != null && pad.IsConnected)
        {
            // Button press edges.
            foreach (var gb in Enum.GetValues<GamepadButton>())
            {
                if (pad.WasButtonPressed(gb))
                {
                    binding = new GamepadButtonBinding(gb);
                    return true;
                }
            }

            // Trigger press edges with hysteresis thresholds provided by the device.
            if (pad.WasLeftTriggerPressed())
            {
                binding = new GamepadTriggerBinding(GamepadAxis.LeftTrigger, pad.TriggerPressThreshold,
                    pad.TriggerReleaseThreshold);
                return true;
            }

            if (pad.WasRightTriggerPressed())
            {
                binding = new GamepadTriggerBinding(GamepadAxis.RightTrigger, pad.TriggerPressThreshold,
                    pad.TriggerReleaseThreshold);
                return true;
            }
        }

        binding = null;
        return false;
    }

    /// <summary>
    ///     Tries to capture an analog axis binding from the current frame's input deltas/values.
    /// </summary>
    /// <param name="kb">Keyboard source (not used for analog capture).</param>
    /// <param name="mouse">Mouse source for movement and wheel deltas.</param>
    /// <param name="pad">Optional gamepad source for analog axes.</param>
    /// <param name="binding">Outputs the first matching axis binding if found.</param>
    /// <returns>True if an analog axis binding was captured; otherwise, false.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Prefers mouse movement/wheel when a non‑zero delta is observed.</description></item>
    ///         <item><description>Falls back to gamepad axes when |value| ≥ 0.3 to filter center noise.</description></item>
    ///         <item><description>Keyboard is intentionally excluded; use composite bindings instead.</description></item>
    ///     </list>
    /// </remarks>
    public static bool TryCaptureAxis(IKeyboard kb, IMouse mouse, IGamepad? pad, out IAxisBinding? binding)
    {
        // Mouse motion/wheel: non-zero delta signals intent for axis mapping.
        if (MathF.Abs(mouse.DeltaX) > 0f)
        {
            binding = new MouseAxisBinding(MouseAxis.MoveX);
            return true;
        }

        if (MathF.Abs(mouse.DeltaY) > 0f)
        {
            binding = new MouseAxisBinding(MouseAxis.MoveY);
            return true;
        }

        if (MathF.Abs(mouse.WheelY) > 0f)
        {
            binding = new MouseAxisBinding(MouseAxis.WheelY);
            return true;
        }

        if (MathF.Abs(mouse.WheelX) > 0f)
        {
            binding = new MouseAxisBinding(MouseAxis.WheelX);
            return true;
        }

        // Gamepad axes: require a modest threshold to filter stick noise/center drift.
        if (pad != null && pad.IsConnected)
        {
            const float thr = 0.3f;
            foreach (var ga in Enum.GetValues<GamepadAxis>())
            {
                var v = pad.GetAxis(ga);
                if (MathF.Abs(v) >= thr)
                {
                    binding = new GamepadAxisBinding(ga);
                    return true;
                }
            }
        }

        binding = null;
        return false;
    }

    /// <summary>
    ///     Returns true if the specified key is a modifier or lock key that should not be captured as a primary action.
    /// </summary>
    private static bool IsModifierKey(Key k)
    {
        return k is Key.LeftShift or Key.RightShift
            or Key.LeftControl or Key.RightControl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftSuper or Key.RightSuper
            or Key.CapsLock or Key.NumLock or Key.ScrollLock;
    }
}