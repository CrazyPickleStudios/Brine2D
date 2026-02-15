namespace Brine2D.SDL.Events;

/// <summary>
/// Internal SDL3 events - not exposed to user code.
/// Used for communication between SDL3EventPump and SDL3InputService.
/// </summary>
internal record SDL3KeyDownEvent(SDL3.SDL.KeyboardEvent KeyEvent);
internal record SDL3KeyUpEvent(SDL3.SDL.KeyboardEvent KeyEvent);
internal record SDL3MouseButtonDownEvent(SDL3.SDL.MouseButtonEvent ButtonEvent);
internal record SDL3MouseButtonUpEvent(SDL3.SDL.MouseButtonEvent ButtonEvent);
internal record SDL3MouseWheelEvent(SDL3.SDL.MouseWheelEvent WheelEvent);
internal record SDL3MouseMotionEvent(SDL3.SDL.MouseMotionEvent MotionEvent);
internal record SDL3TextInputEvent(SDL3.SDL.TextInputEvent TextEvent);
internal record SDL3GamepadButtonDownEvent(SDL3.SDL.GamepadButtonEvent ButtonEvent);
internal record SDL3GamepadButtonUpEvent(SDL3.SDL.GamepadButtonEvent ButtonEvent);
internal record SDL3GamepadAddedEvent(SDL3.SDL.GamepadDeviceEvent DeviceEvent);
internal record SDL3GamepadRemovedEvent(SDL3.SDL.GamepadDeviceEvent DeviceEvent);