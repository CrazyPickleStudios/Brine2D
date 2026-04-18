using System.Collections.Immutable;
using System.Numerics;

namespace Brine2D.Input;

/// <summary>
/// Base type for all input bindings. Maps a physical input to a queryable state.
/// </summary>
public abstract record InputBinding
{
    /// <summary>Returns true if the bound input is currently held.</summary>
    public abstract bool IsDown(IInputContext input);

    /// <summary>Returns true if the bound input was pressed this frame.</summary>
    public abstract bool IsPressed(IInputContext input);

    /// <summary>Returns true if the bound input was released this frame.</summary>
    public abstract bool IsReleased(IInputContext input);

    /// <summary>Returns the analog value of the binding (0 or 1 for digital inputs).</summary>
    public abstract float ReadValue(IInputContext input);
}

/// <summary>Binds a single keyboard key.</summary>
public sealed record KeyBinding(Key Key) : InputBinding
{
    public override bool IsDown(IInputContext input) => input.IsKeyDown(Key);
    public override bool IsPressed(IInputContext input) => input.IsKeyPressed(Key);
    public override bool IsReleased(IInputContext input) => input.IsKeyReleased(Key);
    public override float ReadValue(IInputContext input) => input.IsKeyDown(Key) ? 1f : 0f;
}

/// <summary>
/// Binds two keys to a single axis (−1 to 1).
/// Useful for keyboard-driven movement (e.g., A/D or Left/Right).
/// </summary>
public sealed record KeyAxisBinding(Key Positive, Key Negative) : InputBinding
{
    public override bool IsDown(IInputContext input) => input.IsKeyDown(Positive) || input.IsKeyDown(Negative);
    public override bool IsPressed(IInputContext input) => input.IsKeyPressed(Positive) || input.IsKeyPressed(Negative);
    public override bool IsReleased(IInputContext input) => input.IsKeyReleased(Positive) || input.IsKeyReleased(Negative);

    public override float ReadValue(IInputContext input)
    {
        float value = 0f;
        if (input.IsKeyDown(Positive)) value += 1f;
        if (input.IsKeyDown(Negative)) value -= 1f;
        return value;
    }
}

/// <summary>
/// Binds a combination of keys that must all be held simultaneously (e.g., Ctrl+S).
/// Press is triggered when the final key completes the combo.
/// Release is triggered when any key in the combo is released this frame,
/// provided all keys were either held or released this frame (i.e., the combo was active).
/// </summary>
public sealed record CompositeKeyBinding : InputBinding, IEquatable<CompositeKeyBinding>
{
    public ImmutableArray<Key> Keys { get; }

    public CompositeKeyBinding(params Key[] keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Length < 2)
            throw new ArgumentException("A composite binding requires at least two keys.", nameof(keys));
        Keys = [.. keys];
    }

    public bool Equals(CompositeKeyBinding? other) =>
        other is not null && Keys.AsSpan().SequenceEqual(other.Keys.AsSpan());

    public override int GetHashCode()
    {
        var hash = new HashCode();
        for (int i = 0; i < Keys.Length; i++)
            hash.Add(Keys[i]);
        return hash.ToHashCode();
    }

    public override bool IsDown(IInputContext input)
    {
        for (int i = 0; i < Keys.Length; i++)
        {
            if (!input.IsKeyDown(Keys[i]))
                return false;
        }
        return true;
    }

    public override bool IsPressed(IInputContext input)
    {
        bool anyPressedThisFrame = false;
        for (int i = 0; i < Keys.Length; i++)
        {
            if (input.IsKeyPressed(Keys[i]))
                anyPressedThisFrame = true;
            else if (!input.IsKeyDown(Keys[i]))
                return false;
        }
        return anyPressedThisFrame;
    }

    public override bool IsReleased(IInputContext input)
    {
        bool anyReleasedThisFrame = false;
        for (int i = 0; i < Keys.Length; i++)
        {
            if (input.IsKeyReleased(Keys[i]))
                anyReleasedThisFrame = true;
            else if (!input.IsKeyDown(Keys[i]))
                return false;
        }
        return anyReleasedThisFrame;
    }

    public override float ReadValue(IInputContext input) => IsDown(input) ? 1f : 0f;
}

/// <summary>Binds a single mouse button.</summary>
public sealed record MouseButtonBinding(MouseButton Button) : InputBinding
{
    public override bool IsDown(IInputContext input) => input.IsMouseButtonDown(Button);
    public override bool IsPressed(IInputContext input) => input.IsMouseButtonPressed(Button);
    public override bool IsReleased(IInputContext input) => input.IsMouseButtonReleased(Button);
    public override float ReadValue(IInputContext input) => input.IsMouseButtonDown(Button) ? 1f : 0f;
}

/// <summary>Binds a single gamepad button.</summary>
public sealed record GamepadButtonBinding(GamepadButton Button, int GamepadIndex = 0) : InputBinding
{
    public override bool IsDown(IInputContext input) => input.IsGamepadButtonDown(Button, GamepadIndex);
    public override bool IsPressed(IInputContext input) => input.IsGamepadButtonPressed(Button, GamepadIndex);
    public override bool IsReleased(IInputContext input) => input.IsGamepadButtonReleased(Button, GamepadIndex);
    public override float ReadValue(IInputContext input) => input.IsGamepadButtonDown(Button, GamepadIndex) ? 1f : 0f;
}

/// <summary>
/// Binds a gamepad stick axis (−1 to 1) with per-axis deadzone applied.
/// Note: this uses a per-axis (not radial) deadzone comparison. A stick at 45°
/// with small magnitude may report as active here while
/// <see cref="IInputContext.GetGamepadLeftStick"/> / <see cref="IInputContext.GetGamepadRightStick"/>
/// return <see cref="Vector2.Zero"/> due to their radial deadzone. Use the stick helpers
/// or <see cref="GamepadStickBinding"/> when you need consistent 2D directional input.
/// </summary>
public sealed record GamepadAxisBinding(GamepadAxis Axis, int GamepadIndex = 0) : InputBinding
{
    public override bool IsDown(IInputContext input)
    {
        float value = input.GetGamepadAxis(Axis, GamepadIndex);
        return Axis is GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger
            ? value > input.GamepadDeadzone
            : MathF.Abs(value) > input.GamepadDeadzone;
    }

    public override bool IsPressed(IInputContext input) =>
        input.IsGamepadAxisPressed(Axis, GamepadIndex);

    public override bool IsReleased(IInputContext input) =>
        input.IsGamepadAxisReleased(Axis, GamepadIndex);

    public override float ReadValue(IInputContext input)
    {
        float raw = input.GetGamepadAxis(Axis, GamepadIndex);
        float deadzone = input.GamepadDeadzone;
        float abs = MathF.Abs(raw);
        if (abs < deadzone)
            return 0f;
        float rescaled = (abs - deadzone) / (1f - deadzone);
        return MathF.CopySign(MathF.Min(rescaled, 1f), raw);
    }
}

/// <summary>
/// Binds a gamepad trigger (0 to 1) using <see cref="IInputContext.GetGamepadTrigger"/>.
/// Unlike <see cref="GamepadAxisBinding"/>, this clamps to the 0–1 range and uses
/// the trigger-specific pressed/released detection.
/// </summary>
public sealed record GamepadTriggerBinding(GamepadAxis Trigger, int GamepadIndex = 0) : InputBinding
{
    public GamepadAxis Trigger { get; } = Trigger is GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger
        ? Trigger
        : throw new ArgumentException("Only LeftTrigger and RightTrigger are valid.", nameof(Trigger));

    public override bool IsDown(IInputContext input) =>
        input.GetGamepadTrigger(Trigger, GamepadIndex) > input.GamepadDeadzone;

    public override bool IsPressed(IInputContext input) =>
        input.IsGamepadTriggerPressed(Trigger, GamepadIndex);

    public override bool IsReleased(IInputContext input) =>
        input.IsGamepadTriggerReleased(Trigger, GamepadIndex);

    public override float ReadValue(IInputContext input)
    {
        float raw = input.GetGamepadTrigger(Trigger, GamepadIndex);
        float deadzone = input.GamepadDeadzone;
        if (raw < deadzone)
            return 0f;
        return MathF.Min((raw - deadzone) / (1f - deadzone), 1f);
    }
}

/// <summary>
/// Binds a gamepad stick (left or right) and returns one axis of the radial-deadzone result.
/// Use <see cref="GamepadStick.Left"/> or <see cref="GamepadStick.Right"/> to select the stick,
/// and <see cref="GamepadStickAxis"/> to select X or Y.
/// This applies the same radial deadzone as
/// <see cref="IInputContext.GetGamepadLeftStick"/> / <see cref="IInputContext.GetGamepadRightStick"/>,
/// giving consistent 2D behavior through the binding system.
/// Edge detection (<see cref="IsPressed"/> / <see cref="IsReleased"/>) delegates to the
/// underlying per-axis methods but cross-checks the radial deadzone result so that
/// <see cref="IsPressed"/> cannot fire when the radial magnitude is below threshold.
/// </summary>
public sealed record GamepadStickBinding(GamepadStick Stick, GamepadStickAxis Axis, int GamepadIndex = 0) : InputBinding
{
    public override bool IsDown(IInputContext input) => ReadValue(input) != 0f;

    public override bool IsPressed(IInputContext input) =>
        input.IsGamepadAxisPressed(MappedAxis, GamepadIndex) && ReadValue(input) != 0f;

    public override bool IsReleased(IInputContext input) =>
        input.IsGamepadAxisReleased(MappedAxis, GamepadIndex);

    public override float ReadValue(IInputContext input)
    {
        var stick = Stick == GamepadStick.Left
            ? input.GetGamepadLeftStick(GamepadIndex)
            : input.GetGamepadRightStick(GamepadIndex);

        return Axis == GamepadStickAxis.X ? stick.X : stick.Y;
    }

    private GamepadAxis MappedAxis => (Stick, Axis) switch
    {
        (GamepadStick.Left, GamepadStickAxis.X) => GamepadAxis.LeftX,
        (GamepadStick.Left, GamepadStickAxis.Y) => GamepadAxis.LeftY,
        (GamepadStick.Right, GamepadStickAxis.X) => GamepadAxis.RightX,
        (GamepadStick.Right, GamepadStickAxis.Y) => GamepadAxis.RightY,
        _ => GamepadAxis.LeftX,
    };
}

/// <summary>Specifies which gamepad stick to bind.</summary>
public enum GamepadStick
{
    Left,
    Right,
}

/// <summary>Specifies which axis of a gamepad stick to bind.</summary>
public enum GamepadStickAxis
{
    X,
    Y,
}

/// <summary>
/// Binds the vertical mouse scroll wheel to an analog value.
/// <see cref="IsDown"/> returns true when scroll delta is non-zero this frame.
/// <see cref="ReadValue"/> returns the raw scroll delta for the frame.
/// </summary>
public sealed record MouseScrollBinding : InputBinding
{
    public override bool IsDown(IInputContext input) => input.ScrollWheelDelta != 0f;
    public override bool IsPressed(IInputContext input) => input.ScrollWheelDelta != 0f;
    public override bool IsReleased(IInputContext input) => false;
    public override float ReadValue(IInputContext input) => input.ScrollWheelDelta;
}

/// <summary>
/// Binds the horizontal mouse scroll wheel to an analog value.
/// <see cref="IsDown"/> returns true when horizontal scroll delta is non-zero this frame.
/// <see cref="ReadValue"/> returns the raw horizontal scroll delta for the frame.
/// </summary>
public sealed record MouseScrollXBinding : InputBinding
{
    public override bool IsDown(IInputContext input) => input.ScrollWheelDeltaX != 0f;
    public override bool IsPressed(IInputContext input) => input.ScrollWheelDeltaX != 0f;
    public override bool IsReleased(IInputContext input) => false;
    public override float ReadValue(IInputContext input) => input.ScrollWheelDeltaX;
}

/// <summary>
/// Binds one axis of the mouse delta (movement) to an analog value.
/// Use <see cref="MouseDeltaAxis.X"/> for horizontal and <see cref="MouseDeltaAxis.Y"/> for vertical.
/// <see cref="ReadValue"/> returns the raw delta for that axis this frame.
/// </summary>
public sealed record MouseDeltaBinding(MouseDeltaAxis Axis) : InputBinding
{
    public override bool IsDown(IInputContext input) => ReadValue(input) != 0f;
    public override bool IsPressed(IInputContext input) => ReadValue(input) != 0f;
    public override bool IsReleased(IInputContext input) => false;

    public override float ReadValue(IInputContext input) => Axis switch
    {
        MouseDeltaAxis.X => input.MouseDelta.X,
        MouseDeltaAxis.Y => input.MouseDelta.Y,
        _ => 0f,
    };
}

/// <summary>Specifies which axis of mouse movement to bind.</summary>
public enum MouseDeltaAxis
{
    X,
    Y,
}