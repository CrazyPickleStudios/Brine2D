using System.Diagnostics;
using Brine2D.SDL.Hosting;
using SDL;
using static SDL.SDL3;

namespace Brine2D.SDL.Graphics;

internal sealed unsafe class GpuShader : ITrackedResource, IDisposable
{
    public SDL_GPUShader* Ptr { get; private set; }
    private readonly SDL_GPUDevice* _device;
    private bool _disposed;

    public string? DebugName { get; }
    public bool IsDisposed => _disposed;

    public GpuShader(SDL_GPUDevice* device, SDL_GPUShader* ptr, string? debugName = null)
    {
        _device = device;
        Ptr = ptr;
        DebugName = debugName;
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_device != null && Ptr != null)
        {
            SDL_ReleaseGPUShader(_device, Ptr);
            Ptr = null;
        }
        _disposed = true;
    }

#if DEBUG
    ~GpuShader()
    {
        if (!_disposed)
        {
            Debug.WriteLine($"[Leak] GPU Shader not disposed: {DebugName ?? "(unnamed)"}");
        }
    }
#endif
}

internal sealed unsafe class GpuGraphicsPipeline : ITrackedResource, IDisposable
{
    public SDL_GPUGraphicsPipeline* Ptr { get; private set; }
    private readonly SDL_GPUDevice* _device;
    private bool _disposed;

    public string? DebugName { get; }
    public bool IsDisposed => _disposed;

    public GpuGraphicsPipeline(SDL_GPUDevice* device, SDL_GPUGraphicsPipeline* ptr, string? debugName = null)
    {
        _device = device;
        Ptr = ptr;
        DebugName = debugName;
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_device != null && Ptr != null)
        {
            SDL_ReleaseGPUGraphicsPipeline(_device, Ptr);
            Ptr = null;
        }
        _disposed = true;
    }

#if DEBUG
    ~GpuGraphicsPipeline()
    {
        if (!_disposed)
        {
            Debug.WriteLine($"[Leak] GPU Pipeline not disposed: {DebugName ?? "(unnamed)"}");
        }
    }
#endif
}

internal sealed unsafe class GpuSampler : ITrackedResource, IDisposable
{
    public SDL_GPUSampler* Ptr { get; private set; }
    private readonly SDL_GPUDevice* _device;
    private bool _disposed;

    public string? DebugName { get; }
    public bool IsDisposed => _disposed;

    public GpuSampler(SDL_GPUDevice* device, SDL_GPUSampler* ptr, string? debugName = null)
    {
        _device = device;
        Ptr = ptr;
        DebugName = debugName;
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_device != null && Ptr != null)
        {
            SDL_ReleaseGPUSampler(_device, Ptr);
            Ptr = null;
        }
        _disposed = true;
    }

#if DEBUG
    ~GpuSampler()
    {
        if (!_disposed)
        {
            Debug.WriteLine($"[Leak] GPU Sampler not disposed: {DebugName ?? "(unnamed)"}");
        }
    }
#endif
}