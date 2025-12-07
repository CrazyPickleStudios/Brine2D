using Brine2D.Abstractions;
using Brine2D.Options;
using Microsoft.Extensions.Options;
using SDL3;

namespace Brine2D.SDL3;

public sealed class SdlWindow : IWindow
{
    private readonly WindowOptions _options;
    private readonly SdlInitializer _initializer;
    private IntPtr _window;

    public SdlWindow(IOptions<WindowOptions> options, SdlInitializer initializer)
    {
        _options = options.Value;
        _initializer = initializer;
        _initializer.EnsureInitialized();
        CreateWindow();
    }

    public string Title
    {
        get => _options.Title;
        set
        {
            _options.Title = value;
            if (_window != IntPtr.Zero)
            {
                SDL.SetWindowTitle(_window, value);
            }
        }
    }

    public int Width => _options.Width;
    public int Height => _options.Height;
    public bool IsVisible { get; private set; }

    public void Show()
    {
        if (_window != IntPtr.Zero)
        {
            SDL.ShowWindow(_window);
            IsVisible = true;
        }
    }

    public void Hide()
    {
        if (_window != IntPtr.Zero)
        {
            SDL.HideWindow(_window);
            IsVisible = false;
        }
    }

    public void Dispose()
    {
        if (_window != IntPtr.Zero)
        {
            SDL.DestroyWindow(_window);
            _window = IntPtr.Zero;
        }
        _initializer.Dispose();
    }

    private void CreateWindow()
    {
        _window = SDL.CreateWindow(
            _options.Title,
            _options.Width,
            _options.Height,
            SDL.WindowFlags.Resizable);

        if (_window == IntPtr.Zero)
        {
            throw new InvalidOperationException($"SDL_CreateWindow failed: {SDL.GetError()}");
        }
    }

    public IntPtr RawHandle => _window;
}