using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Brine2D.Rendering;

/// <summary>
/// SDL3 GPU API texture implementation.
/// </summary>
internal sealed class SDL3Texture : ITexture
{
    private const int StateUploading = 0;
    private const int StateReady = 1;
    private const int StateDisposed = 2;

    private readonly ILogger<SDL3Texture> _logger;
    private readonly GpuDeviceHandle _deviceHandle;
    private nint _textureHandle;
    private volatile int _state = StateReady;

    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public bool IsLoaded => _state == StateReady;
    public bool IsDisposed => _state == StateDisposed;
    public TextureScaleMode ScaleMode { get; init; }
    public int SortKey { get; }

    internal nint Handle => Volatile.Read(ref _textureHandle);

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
        SortKey = ITexture.NextSortKey();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal bool MarkUploadPending() =>
        Interlocked.CompareExchange(ref _state, StateUploading, StateReady) == StateReady;

    internal void MarkUploadComplete() =>
        Interlocked.CompareExchange(ref _state, StateReady, StateUploading);

    /// <summary>
    /// Releases the GPU texture after a deferred upload completes for an already-disposed texture.
    /// Called by <c>PollPendingUploads</c> / <c>DrainPendingUploads</c> once the fence has signaled.
    /// </summary>
    /// <param name="device">
    /// Raw GPU device handle, passed directly to avoid reliance on <see cref="GpuDeviceHandle"/> invalidation order during disposal.
    /// </param>
    internal void ReleaseDeferredGPUTexture(nint device)
    {
        var texture = Interlocked.Exchange(ref _textureHandle, nint.Zero);
        if (texture != nint.Zero && device != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUTexture(device, texture);
            _logger.LogDebug("Released deferred GPU texture: {Name}", Name);
        }
    }

    public void Dispose()
    {
        var previousState = Interlocked.Exchange(ref _state, StateDisposed);
        if (previousState == StateDisposed) return;

        if (previousState == StateUploading)
        {
            _logger.LogWarning(
                "Disposing texture '{Name}' while an upload is still pending; GPU texture release deferred until fence signals",
                Name);
            return;
        }

        var device = _deviceHandle.Handle;
        var texture = Interlocked.Exchange(ref _textureHandle, nint.Zero);
        if (texture != nint.Zero && device != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUTexture(device, texture);
            _logger.LogDebug("Released GPU texture: {Name}", Name);
        }
    }
}