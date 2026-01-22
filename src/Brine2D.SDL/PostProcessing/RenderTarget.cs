using System;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL.PostProcessing;

/// <summary>
/// Represents an off-screen render target for post-processing effects.
/// Wraps an SDL3 GPU texture with ColorTarget usage.
/// </summary>
public sealed class RenderTarget : IDisposable
{
    private readonly ILogger<RenderTarget>? _logger;
    private nint _device;
    private nint _texture;
    private bool _disposed;

    public nint Texture => _texture;
    public int Width { get; }
    public int Height { get; }
    public SDL3.SDL.GPUTextureFormat Format { get; }

    internal RenderTarget(nint device, int width, int height, SDL3.SDL.GPUTextureFormat format, ILogger<RenderTarget>? logger = null)
    {
        if (device == nint.Zero)
            throw new ArgumentException("Device handle cannot be zero", nameof(device));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive");

        _device = device;
        Width = width;
        Height = height;
        Format = format;
        _logger = logger;

        CreateTexture();
    }

    private void CreateTexture()
    {
        var textureCreateInfo = new SDL3.SDL.GPUTextureCreateInfo
        {
            Type = SDL3.SDL.GPUTextureType.TextureType2D,
            Format = Format,
            Usage = SDL3.SDL.GPUTextureUsageFlags.ColorTarget | SDL3.SDL.GPUTextureUsageFlags.Sampler,
            Width = (uint)Width,
            Height = (uint)Height,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDL3.SDL.GPUSampleCount.SampleCount1
        };

        _texture = SDL3.SDL.CreateGPUTexture(_device, ref textureCreateInfo);
        if (_texture == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger?.LogError("Failed to create render target texture ({Width}x{Height}): {Error}", Width, Height, error);
            throw new InvalidOperationException($"Failed to create render target texture: {error}");
        }

        _logger?.LogDebug("Created render target: {Width}x{Height}, Format: {Format}", Width, Height, Format);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_texture != nint.Zero && _device != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUTexture(_device, _texture);
            _logger?.LogDebug("Released render target texture: {Width}x{Height}", Width, Height);
            _texture = nint.Zero;
        }

        _device = nint.Zero;
        _disposed = true;
    }
}