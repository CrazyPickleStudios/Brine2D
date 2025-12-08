using Brine2D.Engine;
using Brine2D.Options;
using Microsoft.Extensions.Options;
using SDL3;

namespace Brine2D.SDL3;

public sealed class SdlWindow : IWindow
{
    private readonly SdlInitializer _initializer;
    private readonly WindowOptions _options;

    public SdlWindow(IOptions<WindowOptions> options, SdlInitializer initializer)
    {
        _options = options.Value;
        _initializer = initializer;
        _initializer.EnsureInitialized();
        CreateWindow();
    }

    public int Height => _options.Height;
    public bool IsVisible { get; private set; }
    public IntPtr RawHandle { get; private set; }

    public string Title
    {
        get => _options.Title;
        set
        {
            _options.Title = value;
            if (RawHandle != IntPtr.Zero)
            {
                SDL.SetWindowTitle(RawHandle, value);
            }
        }
    }

    public int Width => _options.Width;

    public void Dispose()
    {
        if (RawHandle != IntPtr.Zero)
        {
            SDL.DestroyWindow(RawHandle);
            RawHandle = IntPtr.Zero;
        }

        _initializer.Dispose();
    }

    public void Hide()
    {
        if (RawHandle != IntPtr.Zero)
        {
            SDL.HideWindow(RawHandle);
            IsVisible = false;
        }
    }

    public void Show()
    {
        if (RawHandle != IntPtr.Zero)
        {
            SDL.ShowWindow(RawHandle);
            IsVisible = true;
        }
    }

    private void CreateWindow()
    {
        RawHandle = SDL.CreateWindow(
            _options.Title,
            _options.Width,
            _options.Height,
            SDL.WindowFlags.Resizable);

        if (RawHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"SDL_CreateWindow failed: {SDL.GetError()}");
        }
    }
}