using Brine2D.Input;
using SDL3;

namespace Brine2D.SDL3;

internal sealed class SdlTextInput : ITextInput
{
    private readonly SdlWindow _window;

    public SdlTextInput(SdlWindow window)
    {
        _window = window;
    }

    public string Composition { get; private set; } = string.Empty;

    public int CompositionCursor { get; private set; }

    public int CompositionSelectionLength { get; private set; }

    public bool IsComposing { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public void BeginFrame()
    {
        Text = string.Empty;
    }

    public void Start()
    {
        SDL.StartTextInput(_window.RawHandle);
    }

    public void Stop()
    {
        SDL.StopTextInput(_window.RawHandle);

        IsComposing = false;
        Composition = string.Empty;
        CompositionCursor = 0;
        CompositionSelectionLength = 0;
    }

    internal void OnTextEditing(string text, int cursor, int selectionLength)
    {
        Composition = text;
        CompositionCursor = cursor;
        CompositionSelectionLength = selectionLength;
        IsComposing = Composition.Length > 0 || selectionLength > 0;

        if (!IsComposing)
        {
            Composition = string.Empty;
            CompositionCursor = 0;
            CompositionSelectionLength = 0;
        }
    }

    internal void OnTextInput(string text)
    {
        Text = text;
    }
}