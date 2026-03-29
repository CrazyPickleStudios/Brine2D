using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Brine2D.Rendering;

/// <summary>
/// SDL3 GPU API texture implementation.
/// </summary>
public class SDL3Texture : ITexture
{
    private readonly ILogger<SDL3Texture> _logger;
    private readonly GpuDeviceHandle _deviceHandle;
    private nint _textureHandle;
    private int _disposed;

    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public bool IsLoaded => _textureHandle != nint.Zero && _disposed == 0;
    public TextureScaleMode ScaleMode { get; set; }

    internal nint Handle => _textureHandle;

    internal SDL3Texture(
        string name,
        GpuDeviceHandle deviceHandle,
        nint textureHandle,
        int width,
        int height,
        TextureScaleMode scaleMode,
        ILogger<SDL3Texture> logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _deviceHandle = deviceHandle ?? throw new ArgumentNullException(nameof(deviceHandle));
        _textureHandle = textureHandle;
        Width = width;
        Height = height;
        ScaleMode = scaleMode;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        var device = _deviceHandle.Handle;
        if (_textureHandle != nint.Zero && device != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUTexture(device, _textureHandle);
            _logger.LogDebug("Released GPU texture: {Name}", Name);
        }

        _textureHandle = nint.Zero;
    }
}