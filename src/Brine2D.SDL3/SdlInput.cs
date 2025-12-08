using Brine2D.Input;

namespace Brine2D.SDL3;

internal sealed class SdlInput : IInput
{
    public SdlInput
    (
        SdlKeyboard keyboard,
        SdlMouse mouse,
        SdlGamepads gamepads,
        SdlTouch touch,
        SdlTextInput textInput
    )
    {
        Keyboard = keyboard;
        Mouse = mouse;
        Gamepads = gamepads;
        Touch = touch;
        TextInput = textInput;
    }

    public SdlGamepads Gamepads { get; }
    public SdlKeyboard Keyboard { get; }
    public SdlMouse Mouse { get; }
    public SdlTextInput TextInput { get; }
    public SdlTouch Touch { get; }
    IGamepads IInput.Gamepads => Gamepads;
    IKeyboard IInput.Keyboard => Keyboard;
    IMouse IInput.Mouse => Mouse;
    ITextInput IInput.TextInput => TextInput;
    ITouch IInput.Touch => Touch;

    public void BeginFrame()
    {
        Mouse.BeginFrame();
        Touch.BeginFrame();
        TextInput.BeginFrame();
    }

    public void EndFrame()
    {
        Keyboard.EndFrame();
        Mouse.EndFrame();
        Gamepads.EndFrame();
        Touch.EndFrame();
    }
}