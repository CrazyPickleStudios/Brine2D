using System.Diagnostics;
using Brine2D.Core;
using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering;

/// <summary>
/// Handles vertex batching and indexed primitive rendering operations.
/// All geometry is emitted as groups of four vertices with a pre-generated static index buffer
/// using the quad pattern [0, 1, 2, 1, 3, 2], reducing vertex throughput by a third for quads.
/// </summary>
internal sealed class SDL3BatchRenderer : IDisposable
{
    private readonly ILogger<SDL3BatchRenderer> _logger;
    private readonly Vertex[] _vertexBatch;
    private int _vertexCount;
    private int _disposed;

    private nint _device;
    private nint _vertexBuffer; // Owned and released by SDL3Renderer
    private nint _whiteTexture;
    private nint _indexBuffer;

    private const int FramesInFlight = SDL3FrameManager.MaxInFlightFrames;
    private readonly nint[] _transferBuffers = new nint[FramesInFlight];
    private int _currentFrameSlot;
    private int _frameVertexOffset;
    private bool _transferBufferCycleNeeded;

    private nint _currentBoundTexture = nint.Zero;
    private TextureScaleMode _currentTextureScaleMode = TextureScaleMode.Linear;
    private ITexture? _currentBoundTextureRef;

    public readonly int MaxVertices;
    private const TextureScaleMode WhiteTextureScaleMode = TextureScaleMode.Nearest;
    private const int VerticesPerQuad = 4;
    private const int IndicesPerQuad = 6;
    private const int VerticesPerOutlineRect = VerticesPerQuad * 4;
    public static readonly int VertexSize = Marshal.SizeOf<Vertex>();

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Vertex(Vector2 position, Vector4 color, Vector2 texCoord)
    {
        public readonly Vector2 Position = position;
        public readonly Vector4 Color = color;
        public readonly Vector2 TexCoord = texCoord;
    }

    public SDL3BatchRenderer(
        ILogger<SDL3BatchRenderer> logger,
        RenderingOptions renderingOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(renderingOptions);

        int requested = renderingOptions.MaxVerticesPerFrame;
        MaxVertices = requested / VerticesPerQuad * VerticesPerQuad;

        if (MaxVertices != requested)
        {
            _logger.LogWarning(
                "MaxVerticesPerFrame ({Requested}) is not a multiple of {VerticesPerQuad}; rounded down to {Aligned}",
                requested, VerticesPerQuad, MaxVertices);
        }

        if (MaxVertices == 0)
            throw new ArgumentException("MaxVerticesPerFrame must be at least " + VerticesPerQuad, nameof(renderingOptions));

        _vertexBatch = new Vertex[MaxVertices];
    }

    public nint IndexBuffer => _indexBuffer;

    public void Initialize(nint device, nint vertexBuffer, nint whiteTexture)
    {
        _device = device;
        _vertexBuffer = vertexBuffer;
        _whiteTexture = whiteTexture;

        var transferCreateInfo = new SDL3.SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUTransferBufferUsage.Upload,
            Size = (uint)(VertexSize * MaxVertices)
        };

        for (int i = 0; i < FramesInFlight; i++)
        {
            _transferBuffers[i] = SDL3.SDL.CreateGPUTransferBuffer(_device, ref transferCreateInfo);
            if (_transferBuffers[i] == nint.Zero)
            {
                var error = SDL3.SDL.GetError();
                throw new InvalidOperationException($"Failed to create transfer buffer [{i}]: {error}");
            }
        }

        CreateStaticIndexBuffer();
    }

    public void Clear()
    {
        _vertexCount = 0;
        _currentBoundTexture = nint.Zero;
        _currentTextureScaleMode = TextureScaleMode.Linear;
        _currentBoundTextureRef = null;
    }

    public int VertexCount => _vertexCount;
    public int FrameVertexOffset => _frameVertexOffset;
    public nint CurrentBoundTexture => _currentBoundTexture;
    public TextureScaleMode CurrentTextureScaleMode => _currentTextureScaleMode;
    internal ITexture? CurrentBoundTextureRef => _currentBoundTextureRef;

    /// <summary>
    /// Converts a vertex count (always a multiple of <see cref="VerticesPerQuad"/>)
    /// to the corresponding index count for indexed drawing.
    /// </summary>
    public static int IndicesToDraw(int vertexCount) => vertexCount / VerticesPerQuad * IndicesPerQuad;

    /// <summary>
    /// Converts a vertex offset (always quad-aligned) to the corresponding
    /// first-index offset in the static index buffer.
    /// </summary>
    public static int VertexOffsetToFirstIndex(int firstVertex) => firstVertex / VerticesPerQuad * IndicesPerQuad;

    public void DrawTexturedQuad(
        nint textureHandle,
        TextureScaleMode scaleMode,
        float x, float y,
        float width, float height,
        Color color,
        float u1 = 0, float v1 = 0,
        float u2 = 1, float v2 = 1,
        float rotation = 0f,
        Vector2? pivotOffset = null,
        Action? onFlushNeeded = null,
        ITexture? textureRef = null)
    {
        EnsureTextureBound(textureHandle, scaleMode, onFlushNeeded, textureRef);

        if (rotation != 0f)
            AddQuadRotated(x, y, width, height, pivotOffset, rotation, color, u1, v1, u2, v2, onFlushNeeded);
        else
            AddQuad(x, y, width, height, color, u1, v1, u2, v2, onFlushNeeded);
    }

    public void DrawRectangleFilled(float x, float y, float width, float height, Color color, Action? onFlushNeeded = null)
    {
        EnsureTextureBound(_whiteTexture, WhiteTextureScaleMode, onFlushNeeded);
        AddQuad(x, y, width, height, color, onFlushNeeded: onFlushNeeded);
    }

    public void DrawRectangleOutline(float x, float y, float width, float height, Color color, float thickness = 1, Action? onFlushNeeded = null)
    {
        if (width <= 0 || height <= 0)
            return;

        var ht = Math.Max(thickness, 0.5f) / 2f;

        if (ht * 2 >= width || ht * 2 >= height)
        {
            DrawRectangleFilled(x, y, width, height, color, onFlushNeeded);
            return;
        }

        EnsureTextureBound(_whiteTexture, WhiteTextureScaleMode, onFlushNeeded);
        if (!EnsureVertexCapacity(VerticesPerOutlineRect, onFlushNeeded)) return;

        var colorVec = ColorToVector4(color);

        float outerLeft = x - ht;
        float outerTop = y - ht;
        float outerRight = x + width + ht;
        float outerBottom = y + height + ht;

        float innerLeft = x + ht;
        float innerTop = y + ht;
        float innerRight = x + width - ht;
        float innerBottom = y + height - ht;

        // Top edge (full width, covers corners)
        AddVertex(outerLeft, outerTop, colorVec, pixelSnap: false);
        AddVertex(outerRight, outerTop, colorVec, pixelSnap: false);
        AddVertex(outerLeft, innerTop, colorVec, pixelSnap: false);
        AddVertex(outerRight, innerTop, colorVec, pixelSnap: false);

        // Bottom edge (full width, covers corners)
        AddVertex(outerLeft, innerBottom, colorVec, pixelSnap: false);
        AddVertex(outerRight, innerBottom, colorVec, pixelSnap: false);
        AddVertex(outerLeft, outerBottom, colorVec, pixelSnap: false);
        AddVertex(outerRight, outerBottom, colorVec, pixelSnap: false);

        // Left edge (inner span only)
        AddVertex(outerLeft, innerTop, colorVec, pixelSnap: false);
        AddVertex(innerLeft, innerTop, colorVec, pixelSnap: false);
        AddVertex(outerLeft, innerBottom, colorVec, pixelSnap: false);
        AddVertex(innerLeft, innerBottom, colorVec, pixelSnap: false);

        // Right edge (inner span only)
        AddVertex(innerRight, innerTop, colorVec, pixelSnap: false);
        AddVertex(outerRight, innerTop, colorVec, pixelSnap: false);
        AddVertex(innerRight, innerBottom, colorVec, pixelSnap: false);
        AddVertex(outerRight, innerBottom, colorVec, pixelSnap: false);
    }

    public void DrawCircleFilled(float centerX, float centerY, float radius, Color color, Action? onFlushNeeded = null)
    {
        if (radius <= 0) return;

        int segments = Math.Min(CalculateCircleSegments(radius), MaxVertices / VerticesPerQuad * 2);
        EnsureTextureBound(_whiteTexture, WhiteTextureScaleMode, onFlushNeeded);
        int quadsNeeded = (segments + 1) / 2;
        if (!EnsureVertexCapacity(quadsNeeded * VerticesPerQuad, onFlushNeeded)) return;

        float angleStep = MathF.PI * 2f / segments;
        var colorVec = ColorToVector4(color);

        for (int i = 0; i < segments - 1; i += 2)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;
            float angle3 = (i + 2) * angleStep;

            float p1x = centerX + MathF.Cos(angle1) * radius;
            float p1y = centerY + MathF.Sin(angle1) * radius;
            float p2x = centerX + MathF.Cos(angle2) * radius;
            float p2y = centerY + MathF.Sin(angle2) * radius;
            float p3x = centerX + MathF.Cos(angle3) * radius;
            float p3y = centerY + MathF.Sin(angle3) * radius;

            AddVertex(p1x, p1y, colorVec, pixelSnap: false);
            AddVertex(p2x, p2y, colorVec, pixelSnap: false);
            AddVertex(centerX, centerY, colorVec, pixelSnap: false);
            AddVertex(p3x, p3y, colorVec, pixelSnap: false);
        }

        if (segments % 2 != 0)
        {
            int last = segments - 1;
            float angle1 = last * angleStep;
            float angleMid = (last + 0.5f) * angleStep;
            float angle2 = (last + 1) * angleStep;

            float p1x = centerX + MathF.Cos(angle1) * radius;
            float p1y = centerY + MathF.Sin(angle1) * radius;
            float pMidX = centerX + MathF.Cos(angleMid) * radius;
            float pMidY = centerY + MathF.Sin(angleMid) * radius;
            float p2x = centerX + MathF.Cos(angle2) * radius;
            float p2y = centerY + MathF.Sin(angle2) * radius;

            AddVertex(p1x, p1y, colorVec, pixelSnap: false);
            AddVertex(pMidX, pMidY, colorVec, pixelSnap: false);
            AddVertex(centerX, centerY, colorVec, pixelSnap: false);
            AddVertex(p2x, p2y, colorVec, pixelSnap: false);
        }
    }

    public void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1, Action? onFlushNeeded = null)
    {
        if (radius <= 0) return;

        float halfThickness = Math.Max(thickness, 0.5f) / 2f;
        float innerRadius = radius - halfThickness;

        if (innerRadius <= 0)
        {
            DrawCircleFilled(centerX, centerY, radius + halfThickness, color, onFlushNeeded);
            return;
        }

        int segments = Math.Min(CalculateCircleSegments(radius), MaxVertices / VerticesPerQuad);
        EnsureTextureBound(_whiteTexture, WhiteTextureScaleMode, onFlushNeeded);
        if (!EnsureVertexCapacity(segments * VerticesPerQuad, onFlushNeeded)) return;

        float outerRadius = radius + halfThickness;
        float angleStep = MathF.PI * 2f / segments;
        var colorVec = ColorToVector4(color);

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;

            float cos1 = MathF.Cos(angle1);
            float sin1 = MathF.Sin(angle1);
            float cos2 = MathF.Cos(angle2);
            float sin2 = MathF.Sin(angle2);

            float outerX1 = centerX + cos1 * outerRadius;
            float outerY1 = centerY + sin1 * outerRadius;
            float innerX1 = centerX + cos1 * innerRadius;
            float innerY1 = centerY + sin1 * innerRadius;

            float outerX2 = centerX + cos2 * outerRadius;
            float outerY2 = centerY + sin2 * outerRadius;
            float innerX2 = centerX + cos2 * innerRadius;
            float innerY2 = centerY + sin2 * innerRadius;

            AddVertex(outerX1, outerY1, colorVec, pixelSnap: false);
            AddVertex(outerX2, outerY2, colorVec, pixelSnap: false);
            AddVertex(innerX1, innerY1, colorVec, pixelSnap: false);
            AddVertex(innerX2, innerY2, colorVec, pixelSnap: false);
        }
    }

    public void NewFrame(int frameSlot)
    {
        _currentFrameSlot = frameSlot;
        _frameVertexOffset = 0;
        _transferBufferCycleNeeded = false;
    }

    public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1, Action? onFlushNeeded = null)
    {
        EnsureTextureBound(_whiteTexture, WhiteTextureScaleMode, onFlushNeeded);
        if (!EnsureVertexCapacity(VerticesPerQuad, onFlushNeeded)) return;
        EmitLineVertices(x1, y1, x2, y2, Math.Max(thickness, 0.5f) / 2f, ColorToVector4(color));
    }

    /// <summary>
    /// Copies the current vertex batch to the transfer buffer (CPU-only, no GPU pass).
    /// Returns (firstVertex, vertexCount) for use in a later copy pass.
    /// </summary>
    public unsafe (int FirstVertex, int VertexCount) StageForUpload()
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (_vertexCount == 0) return (_frameVertexOffset, 0);

        Debug.Assert(_vertexCount % VerticesPerQuad == 0,
            $"Vertex count {_vertexCount} is not a multiple of {VerticesPerQuad}; index buffer will reference stale data");

        int firstVertex = _frameVertexOffset;
        int vertexCount = _vertexCount;
        var vertexDataSize = (uint)(VertexSize * vertexCount);
        var byteOffset = (uint)(firstVertex * VertexSize);

        if (firstVertex + vertexCount > MaxVertices)
        {
            _logger.LogWarning("Frame vertex budget exceeded ({Used}/{Max})", firstVertex + vertexCount, MaxVertices);
            return (firstVertex, 0);
        }

        bool cycle = _transferBufferCycleNeeded;
        _transferBufferCycleNeeded = false;

        var mappedData = SDL3.SDL.MapGPUTransferBuffer(_device, _transferBuffers[_currentFrameSlot], cycle);
        if (mappedData == nint.Zero)
        {
            _logger.LogError("Failed to map transfer buffer for vertex upload (slot {Slot})", _currentFrameSlot);
            return (firstVertex, 0);
        }

        fixed (Vertex* vertexPtr = _vertexBatch)
        {
            Buffer.MemoryCopy(
                vertexPtr,
                (void*)(mappedData + (nint)byteOffset),
                vertexDataSize,
                vertexDataSize
            );
        }
        SDL3.SDL.UnmapGPUTransferBuffer(_device, _transferBuffers[_currentFrameSlot]);

        _frameVertexOffset += vertexCount;
        return (firstVertex, vertexCount);
    }

    /// <summary>
    /// Issues the GPU copy command for a previously staged vertex range inside an already-open copy pass.
    /// </summary>
    public void UploadWithinCopyPass(nint copyPass, int firstVertex, int vertexCount)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        var byteOffset = (uint)(firstVertex * VertexSize);
        var source = new SDL3.SDL.GPUTransferBufferLocation
        {
            TransferBuffer = _transferBuffers[_currentFrameSlot],
            Offset = byteOffset
        };
        var destination = new SDL3.SDL.GPUBufferRegion
        {
            Buffer = _vertexBuffer,
            Offset = byteOffset,
            Size = (uint)(VertexSize * vertexCount)
        };
        SDL3.SDL.UploadToGPUBuffer(copyPass, ref source, ref destination, false);
    }

    public void ResetFrameVertexOffset()
    {
        _frameVertexOffset = 0;
        _transferBufferCycleNeeded = true;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        if (_device == nint.Zero) return;

        for (int i = 0; i < FramesInFlight; i++)
        {
            if (_transferBuffers[i] != nint.Zero)
            {
                SDL3.SDL.ReleaseGPUTransferBuffer(_device, _transferBuffers[i]);
                _transferBuffers[i] = nint.Zero;
            }
        }

        if (_indexBuffer != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUBuffer(_device, _indexBuffer);
            _indexBuffer = nint.Zero;
        }

        _device = nint.Zero;
    }

    private void AddQuad(
        float x, float y,
        float width, float height,
        Color color,
        float u1 = 0, float v1 = 0,
        float u2 = 1, float v2 = 1,
        Action? onFlushNeeded = null)
    {
        if (!EnsureVertexCapacity(VerticesPerQuad, onFlushNeeded)) return;

        var c = ColorToVector4(color);

        AddVertex(x, y, c, u1, v1);
        AddVertex(x + width, y, c, u2, v1);
        AddVertex(x, y + height, c, u1, v2);
        AddVertex(x + width, y + height, c, u2, v2);
    }

    private void AddQuadRotated(
        float x, float y,
        float width, float height,
        Vector2? pivotOffset,
        float rotation,
        Color color,
        float u1 = 0, float v1 = 0,
        float u2 = 1, float v2 = 1,
        Action? onFlushNeeded = null)
    {
        if (!EnsureVertexCapacity(VerticesPerQuad, onFlushNeeded)) return;

        float px = pivotOffset?.X ?? width / 2f;
        float py = pivotOffset?.Y ?? height / 2f;

        float cx = x + px;
        float cy = y + py;

        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);

        var topLeft = RotatePoint(-px, -py, cos, sin, cx, cy);
        var topRight = RotatePoint(width - px, -py, cos, sin, cx, cy);
        var bottomLeft = RotatePoint(-px, height - py, cos, sin, cx, cy);
        var bottomRight = RotatePoint(width - px, height - py, cos, sin, cx, cy);

        var c = ColorToVector4(color);

        AddVertex(topLeft.X, topLeft.Y, c, u1, v1, pixelSnap: false);
        AddVertex(topRight.X, topRight.Y, c, u2, v1, pixelSnap: false);
        AddVertex(bottomLeft.X, bottomLeft.Y, c, u1, v2, pixelSnap: false);
        AddVertex(bottomRight.X, bottomRight.Y, c, u2, v2, pixelSnap: false);
    }

    private static (float X, float Y) RotatePoint(float x, float y, float cos, float sin, float centerX, float centerY)
    {
        return (
            x * cos - y * sin + centerX,
            x * sin + y * cos + centerY
        );
    }

    private void AddVertex(float x, float y, Vector4 color, float u = 0, float v = 0, bool pixelSnap = true)
    {
        Debug.Assert(_vertexCount < MaxVertices, "Vertex buffer overflow: EnsureVertexCapacity was bypassed");

        var position = pixelSnap ? new Vector2(MathF.Round(x), MathF.Round(y)) : new Vector2(x, y);
        _vertexBatch[_vertexCount++] = new Vertex(position, color, new Vector2(u, v));
    }

    private void EnsureTextureBound(nint textureHandle, TextureScaleMode scaleMode, Action? onFlushNeeded, ITexture? textureRef = null)
    {
        Debug.Assert(textureHandle != nint.Zero, "Zero texture handle passed to EnsureTextureBound");

        if (_currentBoundTexture != nint.Zero &&
            (_currentBoundTexture != textureHandle || _currentTextureScaleMode != scaleMode))
        {
            onFlushNeeded?.Invoke();
        }

        _currentBoundTexture = textureHandle;
        _currentTextureScaleMode = scaleMode;
        _currentBoundTextureRef = textureRef;
    }

    private bool EnsureVertexCapacity(int verticesNeeded, Action? onFlushNeeded)
    {
        if (_vertexCount + verticesNeeded > MaxVertices)
        {
            var savedTexture = _currentBoundTexture;
            var savedScaleMode = _currentTextureScaleMode;
            var savedTextureRef = _currentBoundTextureRef;

            onFlushNeeded?.Invoke();

            if (_vertexCount + verticesNeeded > MaxVertices)
            {
                Debug.Fail($"Vertex budget exhausted: {verticesNeeded} needed, {MaxVertices - _vertexCount} available");
                _logger.LogWarning(
                    "Dropping primitive: vertex budget exhausted ({Needed} needed, {Available} available)",
                    verticesNeeded, MaxVertices - _vertexCount);
                return false;
            }

            _currentBoundTexture = savedTexture;
            _currentTextureScaleMode = savedScaleMode;
            _currentBoundTextureRef = savedTextureRef;
        }

        return true;
    }

    private void EmitLineVertices(float x1, float y1, float x2, float y2, float halfThickness, Vector4 color)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var length = MathF.Sqrt(dx * dx + dy * dy);

        if (length == 0) return;

        var perpX = -dy / length * halfThickness;
        var perpY = dx / length * halfThickness;

        AddVertex(x1 + perpX, y1 + perpY, color, pixelSnap: false);
        AddVertex(x2 + perpX, y2 + perpY, color, pixelSnap: false);
        AddVertex(x1 - perpX, y1 - perpY, color, pixelSnap: false);
        AddVertex(x2 - perpX, y2 - perpY, color, pixelSnap: false);
    }

    private unsafe void CreateStaticIndexBuffer()
    {
        int maxQuads = MaxVertices / VerticesPerQuad;
        int totalIndices = maxQuads * IndicesPerQuad;
        uint indexBufferSize = (uint)(totalIndices * sizeof(uint));

        var bufferCreateInfo = new SDL3.SDL.GPUBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUBufferUsageFlags.Index,
            Size = indexBufferSize
        };

        _indexBuffer = SDL3.SDL.CreateGPUBuffer(_device, ref bufferCreateInfo);
        if (_indexBuffer == nint.Zero)
            throw new InvalidOperationException($"Failed to create index buffer: {SDL3.SDL.GetError()}");

        var transferCreateInfo = new SDL3.SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL3.SDL.GPUTransferBufferUsage.Upload,
            Size = indexBufferSize
        };

        var transferBuffer = SDL3.SDL.CreateGPUTransferBuffer(_device, ref transferCreateInfo);
        if (transferBuffer == nint.Zero)
            throw new InvalidOperationException($"Failed to create index transfer buffer: {SDL3.SDL.GetError()}");

        try
        {
            var mapped = SDL3.SDL.MapGPUTransferBuffer(_device, transferBuffer, false);
            if (mapped == nint.Zero)
                throw new InvalidOperationException("Failed to map index transfer buffer");

            var indices = (uint*)mapped;
            for (int i = 0; i < maxQuads; i++)
            {
                uint bv = (uint)(i * VerticesPerQuad);
                int idx = i * IndicesPerQuad;
                indices[idx] = bv;
                indices[idx + 1] = bv + 1;
                indices[idx + 2] = bv + 2;
                indices[idx + 3] = bv + 1;
                indices[idx + 4] = bv + 3;
                indices[idx + 5] = bv + 2;
            }

            SDL3.SDL.UnmapGPUTransferBuffer(_device, transferBuffer);

            var cmdBuffer = SDL3.SDL.AcquireGPUCommandBuffer(_device);
            if (cmdBuffer == nint.Zero)
                throw new InvalidOperationException("Failed to acquire command buffer for index buffer upload");

            var copyPass = SDL3.SDL.BeginGPUCopyPass(cmdBuffer);
            if (copyPass == nint.Zero)
            {
                SDL3.SDL.SubmitGPUCommandBuffer(cmdBuffer);
                throw new InvalidOperationException(
                    $"Failed to begin copy pass for index buffer upload: {SDL3.SDL.GetError()}");
            }

            var source = new SDL3.SDL.GPUTransferBufferLocation
            {
                TransferBuffer = transferBuffer,
                Offset = 0
            };
            var destination = new SDL3.SDL.GPUBufferRegion
            {
                Buffer = _indexBuffer,
                Offset = 0,
                Size = indexBufferSize
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
                _logger.LogError("Failed to acquire fence for static index buffer upload: {Error}", SDL3.SDL.GetError());
                SDL3.SDL.WaitForGPUIdle(_device);
            }
        }
        finally
        {
            SDL3.SDL.ReleaseGPUTransferBuffer(_device, transferBuffer);
        }
    }

    private static Vector4 ColorToVector4(Color color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

    private static int CalculateCircleSegments(float radius) =>
        Math.Clamp((int)(MathF.Sqrt(radius) * 10), 8, 256);
}