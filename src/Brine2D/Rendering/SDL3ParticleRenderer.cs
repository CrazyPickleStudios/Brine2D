using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering;

/// <summary>
/// Hardware-instanced renderer for particle emitters. Manages a static unit-quad vertex buffer
/// (slot 0, 4 × <c>float2</c> corners), a per-frame CPU staging array, a GPU instance buffer
/// (slot 1, N × 48-byte <see cref="ParticleInstance"/>), and uploads all staged instances once
/// per frame inside the GPU copy pass issued by <see cref="SDL3Renderer"/>.
/// </summary>
/// <remarks>
/// Initialized and owned by <see cref="SDL3Renderer"/>. Accessed internally by
/// <c>ParticleSystem</c> through <c>SDL3Renderer.ParticleRenderer</c>.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Requires a live SDL3 GPU context; covered by manual/hardware testing.")]
internal sealed class SDL3ParticleRenderer : IDisposable
{
    /// <summary>
    /// Per-instance data written to the GPU for each particle. 48 bytes, blittable.
    /// Layout must exactly match the per-instance attributes declared in particle_vertex.hlsl.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ParticleInstance
    {
        public readonly Vector2 Position;   // offset  0, 8 bytes
        public readonly float Size;         // offset  8, 4 bytes
        public readonly float Rotation;     // offset 12, 4 bytes
        public readonly Vector4 Color;      // offset 16, 16 bytes
        public readonly Vector4 UVRect;     // offset 32, 16 bytes — (u1,v1,u2,v2); SdfCircleUVRect triggers SDF path

        public ParticleInstance(Vector2 position, float size, float rotation, Vector4 color, Vector4 uvRect)
        {
            Position = position;
            Size = size;
            Rotation = rotation;
            Color = color;
            UVRect = uvRect;
        }
    }

    /// <summary>
    /// UVRect sentinel that triggers the SDF filled-circle path in <c>particle_fragment.hlsl</c>.
    /// Equivalent to the <c>[2,3]×[2,3]</c> TexCoord range used by <see cref="SDL3BatchRenderer.DrawCircleFilled"/>.
    /// </summary>
    public static readonly Vector4 SdfCircleUVRect = new(2f, 2f, 3f, 3f);

    /// <summary>UVRect covering the full texture (UV 0–1 on both axes).</summary>
    public static readonly Vector4 FullTextureUVRect = new(0f, 0f, 1f, 1f);

    /// <summary>Byte size of one <see cref="ParticleInstance"/>. Must equal 48.</summary>
    public static readonly int InstanceSize = Marshal.SizeOf<ParticleInstance>();

    private const int FramesInFlight = SDL3FrameManager.MaxInFlightFrames;

    private readonly ILogger<SDL3ParticleRenderer> _logger;
    private readonly int _maxInstances;
    private readonly ParticleInstance[] _staging;

    private nint _device;
    private nint _unitQuadVertexBuffer;
    private nint _instanceBuffer;
    private readonly nint[] _transferBuffers = new nint[FramesInFlight];

    private int _currentFrameSlot;
    private int _stagedCount;
    private int _disposed;

    /// <summary>
    /// Static unit-quad vertex buffer (slot 0). Contains 4 × <c>float2</c> corners:
    /// (-1,-1), (1,-1), (-1,1), (1,1). Never changes after initialization.
    /// </summary>
    public nint UnitQuadVertexBuffer => _unitQuadVertexBuffer;

    /// <summary>
    /// GPU instance buffer (slot 1). Sized for <see cref="RenderingOptions.MaxParticlesPerFrame"/>
    /// × 48 bytes. Overwritten once per frame via <see cref="FlushToGPU"/>.
    /// </summary>
    public nint InstanceBuffer => _instanceBuffer;

    /// <summary>Number of instances staged into the CPU buffer so far this frame.</summary>
    public int StagedCount => _stagedCount;

    public SDL3ParticleRenderer(ILogger<SDL3ParticleRenderer> logger, RenderingOptions renderingOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(renderingOptions);

        _maxInstances = renderingOptions.MaxParticlesPerFrame;
        _staging = new ParticleInstance[_maxInstances];

        Debug.Assert(InstanceSize == 48,
            $"ParticleInstance is {InstanceSize} bytes — expected 48. Check struct layout.");
    }

    /// <summary>
    /// Creates all GPU resources. Must be called after the GPU device is ready,
    /// before the first frame.
    /// </summary>
    public void Initialize(nint device)
    {
        if (device == nint.Zero)
            throw new ArgumentException("GPU device handle cannot be zero.", nameof(device));

        _device = device;

        CreateUnitQuadVertexBuffer();
        CreateInstanceBuffer();
        CreateTransferBuffers();

        _logger.LogDebug(
            "SDL3ParticleRenderer initialized (max {Max} instances, {Bytes} bytes GPU instance buffer)",
            _maxInstances, _maxInstances * InstanceSize);
    }

    /// <summary>
    /// Resets the per-frame instance cursor. Called at the start of each frame by
    /// <see cref="SDL3Renderer.BeginFrame"/> after the frame-slot fence has been waited.
    /// </summary>
    public void NewFrame(int frameSlot)
    {
        _currentFrameSlot = frameSlot;
        _stagedCount = 0;
    }

    /// <summary>
    /// Appends <paramref name="instances"/> to the CPU staging buffer and returns the
    /// <c>firstInstance</c> index to pass to
    /// <c>DrawGPUIndexedPrimitives(6, count, 0, 0, firstInstance)</c>.
    /// Returns <c>-1</c> when the per-frame instance budget is exhausted; the caller
    /// should skip recording the draw call.
    /// </summary>
    public int AppendInstances(ReadOnlySpan<ParticleInstance> instances)
    {
        if (instances.IsEmpty)
            return -1;

        if (_stagedCount >= _maxInstances)
        {
            _logger.LogWarning(
                "Particle instance budget exhausted ({Max} max); emitter draw call dropped this frame",
                _maxInstances);
            return -1;
        }

        int first = _stagedCount;
        int available = _maxInstances - _stagedCount;
        int toCopy = Math.Min(instances.Length, available);

        if (toCopy < instances.Length)
        {
            _logger.LogWarning(
                "Particle instance budget: {Requested} requested but only {Available} slot(s) remain; {Dropped} particle(s) dropped",
                instances.Length, available, instances.Length - toCopy);
        }

        instances[..toCopy].CopyTo(_staging.AsSpan(first));
        _stagedCount += toCopy;
        return first;
    }

    /// <summary>
    /// Copies all instances staged this frame from the CPU array to the GPU instance buffer.
    /// Must be called inside an active GPU copy pass.
    /// SDL3Renderer calls this once per frame at the beginning of
    /// <c>ExecuteDrawCalls</c>, before any particle draw calls are issued.
    /// </summary>
    public unsafe void FlushToGPU(nint copyPass)
    {
        if (_stagedCount == 0)
            return;

        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        var byteSize = (uint)(_stagedCount * InstanceSize);

        var mapped = SDL3.SDL.MapGPUTransferBuffer(_device, _transferBuffers[_currentFrameSlot], false);
        if (mapped == nint.Zero)
        {
            _logger.LogError(
                "Failed to map particle instance transfer buffer (slot {Slot}): {Error}",
                _currentFrameSlot, SDL3.SDL.GetError());
            return;
        }

        fixed (ParticleInstance* src = _staging)
        {
            Buffer.MemoryCopy(src, (void*)mapped, byteSize, byteSize);
        }

        SDL3.SDL.UnmapGPUTransferBuffer(_device, _transferBuffers[_currentFrameSlot]);

        var source = new SDL3.SDL.GPUTransferBufferLocation
        {
            TransferBuffer = _transferBuffers[_currentFrameSlot],
            Offset = 0
        };
        var destination = new SDL3.SDL.GPUBufferRegion
        {
            Buffer = _instanceBuffer,
            Offset = 0,
            Size = byteSize
        };

        SDL3.SDL.UploadToGPUBuffer(copyPass, ref source, ref destination, false);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        if (_device == nint.Zero)
            return;

        for (int i = 0; i < FramesInFlight; i++)
        {
            if (_transferBuffers[i] != nint.Zero)
            {
                SDL3.SDL.ReleaseGPUTransferBuffer(_device, _transferBuffers[i]);
                _transferBuffers[i] = nint.Zero;
            }
        }

        if (_instanceBuffer != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUBuffer(_device, _instanceBuffer);
            _instanceBuffer = nint.Zero;
        }

        if (_unitQuadVertexBuffer != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUBuffer(_device, _unitQuadVertexBuffer);
            _unitQuadVertexBuffer = nint.Zero;
        }

        _device = nint.Zero;
    }

    private unsafe void CreateUnitQuadVertexBuffer()
    {
        // CCW winding, Y-down (SDL convention):
        //   0(-1,-1) — top-left     1(1,-1) — top-right
        //   2(-1, 1) — bottom-left  3(1, 1) — bottom-right
        // Index pattern [0,1,2,1,3,2]: tri1 = TL,TR,BL  tri2 = TR,BR,BL  (CCW ✓)
        var corners = new Vector2[]
        {
            new(-1f, -1f),
            new( 1f, -1f),
            new(-1f,  1f),
            new( 1f,  1f),
        };

        const uint bufferSize = 4 * sizeof(float) * 2; // 4 × float2 = 32 bytes

        var bufferCreateInfo = new SDL3.SDL.GPUBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUBufferUsageFlags.Vertex,
            Size = bufferSize
        };

        _unitQuadVertexBuffer = SDL3.SDL.CreateGPUBuffer(_device, ref bufferCreateInfo);
        if (_unitQuadVertexBuffer == nint.Zero)
        {
            throw new InvalidOperationException(
                $"Failed to create unit-quad vertex buffer: {SDL3.SDL.GetError()}");
        }

        var transferCreateInfo = new SDL3.SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUTransferBufferUsage.Upload,
            Size = bufferSize
        };

        var transferBuffer = SDL3.SDL.CreateGPUTransferBuffer(_device, ref transferCreateInfo);
        if (transferBuffer == nint.Zero)
        {
            throw new InvalidOperationException(
                $"Failed to create unit-quad transfer buffer: {SDL3.SDL.GetError()}");
        }

        try
        {
            var mapped = SDL3.SDL.MapGPUTransferBuffer(_device, transferBuffer, false);
            if (mapped == nint.Zero)
            {
                throw new InvalidOperationException(
                    $"Failed to map unit-quad transfer buffer: {SDL3.SDL.GetError()}");
            }

            fixed (Vector2* src = corners)
            {
                Buffer.MemoryCopy(src, (void*)mapped, bufferSize, bufferSize);
            }

            SDL3.SDL.UnmapGPUTransferBuffer(_device, transferBuffer);

            var cmdBuffer = SDL3.SDL.AcquireGPUCommandBuffer(_device);
            if (cmdBuffer == nint.Zero)
            {
                throw new InvalidOperationException(
                    $"Failed to acquire command buffer for unit-quad upload: {SDL3.SDL.GetError()}");
            }

            var copyPass = SDL3.SDL.BeginGPUCopyPass(cmdBuffer);
            if (copyPass == nint.Zero)
            {
                SDL3.SDL.SubmitGPUCommandBuffer(cmdBuffer);
                throw new InvalidOperationException(
                    $"Failed to begin copy pass for unit-quad upload: {SDL3.SDL.GetError()}");
            }

            var source = new SDL3.SDL.GPUTransferBufferLocation
            {
                TransferBuffer = transferBuffer,
                Offset = 0
            };
            var destination = new SDL3.SDL.GPUBufferRegion
            {
                Buffer = _unitQuadVertexBuffer,
                Offset = 0,
                Size = bufferSize
            };

            SDL3.SDL.UploadToGPUBuffer(copyPass, ref source, ref destination, false);
            SDL3.SDL.EndGPUCopyPass(copyPass);

            var fence = SDL3.SDL.SubmitGPUCommandBufferAndAcquireFence(cmdBuffer);
            if (fence != nint.Zero)
            {
                nint[] fenceBuf = [fence];
                SDL3.SDL.WaitForGPUFences(_device, true, fenceBuf, 1);
                SDL3.SDL.ReleaseGPUFence(_device, fence);
            }
            else
            {
                _logger.LogWarning("Failed to acquire fence for unit-quad upload; falling back to WaitForGPUIdle");
                SDL3.SDL.WaitForGPUIdle(_device);
            }
        }
        finally
        {
            SDL3.SDL.ReleaseGPUTransferBuffer(_device, transferBuffer);
        }

        _logger.LogDebug("Unit-quad vertex buffer created and uploaded (4 corners, 32 bytes)");
    }

    private void CreateInstanceBuffer()
    {
        var bufferCreateInfo = new SDL3.SDL.GPUBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUBufferUsageFlags.Vertex,
            Size = (uint)(_maxInstances * InstanceSize)
        };

        _instanceBuffer = SDL3.SDL.CreateGPUBuffer(_device, ref bufferCreateInfo);
        if (_instanceBuffer == nint.Zero)
        {
            throw new InvalidOperationException(
                $"Failed to create particle instance buffer: {SDL3.SDL.GetError()}");
        }

        _logger.LogDebug(
            "Particle instance buffer created ({Max} × {Stride} bytes = {Total} bytes)",
            _maxInstances, InstanceSize, _maxInstances * InstanceSize);
    }

    private void CreateTransferBuffers()
    {
        var transferCreateInfo = new SDL3.SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUTransferBufferUsage.Upload,
            Size = (uint)(_maxInstances * InstanceSize)
        };

        for (int i = 0; i < FramesInFlight; i++)
        {
            _transferBuffers[i] = SDL3.SDL.CreateGPUTransferBuffer(_device, ref transferCreateInfo);
            if (_transferBuffers[i] == nint.Zero)
            {
                throw new InvalidOperationException(
                    $"Failed to create particle instance transfer buffer [{i}]: {SDL3.SDL.GetError()}");
            }
        }

        _logger.LogDebug(
            "Particle instance transfer buffers created ({Count} in-flight, {Size} bytes each)",
            FramesInFlight, _maxInstances * InstanceSize);
    }
}