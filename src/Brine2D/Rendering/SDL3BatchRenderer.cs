using Brine2D.Core;
using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Runtime.InteropServices;
using Brine2D.Rendering;

namespace Brine2D.Rendering;

/// <summary>
/// Handles vertex batching and primitive rendering operations.
/// </summary>
internal sealed class SDL3BatchRenderer : IDisposable
{
    private readonly ILogger<SDL3BatchRenderer> _logger;
    private readonly SDL3StateManager _stateManager;
    private readonly List<Vertex> _vertexBatch;

    private nint _device;
    private nint _vertexBuffer;
    private nint _whiteTexture;
    private nint _sampler;
    private nint _samplerNearest;

    private const int FramesInFlight = 3;
    private readonly nint[] _transferBuffers = new nint[FramesInFlight];
    private int _currentFrameSlot;
    private int _frameVertexOffset;
    private bool _transferBufferCycleNeeded;

    private nint _currentBoundTexture = nint.Zero;
    private TextureScaleMode _currentTextureScaleMode = TextureScaleMode.Linear;

    public readonly int MaxVertices;
    private const TextureScaleMode WhiteTextureScaleMode = TextureScaleMode.Nearest;
    public static int VertexSize => Marshal.SizeOf<Vertex>();

    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex
    {
        public Vector2 Position;
        public Vector4 Color;
        public Vector2 TexCoord;
    }

    public SDL3BatchRenderer(
        ILogger<SDL3BatchRenderer> logger,
        SDL3StateManager stateManager,
        RenderingOptions renderingOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        ArgumentNullException.ThrowIfNull(renderingOptions);
        MaxVertices = renderingOptions.MaxVerticesPerFrame;
        _vertexBatch = new List<Vertex>(MaxVertices);
    }
    
    public void Initialize(nint device, nint vertexBuffer, nint whiteTexture, nint sampler, nint samplerNearest)
    {
        _device = device;
        _vertexBuffer = vertexBuffer;
        _whiteTexture = whiteTexture;
        _sampler = sampler;
        _samplerNearest = samplerNearest;

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
    }
    
    public void Clear()
    {
        _vertexBatch.Clear();
        _currentBoundTexture = nint.Zero;
        _currentTextureScaleMode = TextureScaleMode.Linear;
    }
    
    public int VertexCount => _vertexBatch.Count;
    public int FrameVertexOffset => _frameVertexOffset;
    public nint CurrentBoundTexture => _currentBoundTexture;
    public TextureScaleMode CurrentTextureScaleMode => _currentTextureScaleMode;
    
    public void DrawTexturedQuad(
        nint textureHandle,
        TextureScaleMode scaleMode,
        float x, float y, 
        float width, float height, 
        Color color,
        float u1 = 0, float v1 = 0, 
        float u2 = 1, float v2 = 1,
        float rotation = 0f,
        Action? onFlushNeeded = null)
    {
        EnsureTextureBound(textureHandle, scaleMode, onFlushNeeded);
        
        if (rotation != 0f)
            AddQuadRotated(x, y, width, height, rotation, color, u1, v1, u2, v2, onFlushNeeded);
        else
            AddQuad(x, y, width, height, color, u1, v1, u2, v2, onFlushNeeded);
    }
    
    public void DrawRectangleFilled(float x, float y, float width, float height, Color color, Action? onFlushNeeded = null)
    {
        EnsureTextureBound(_whiteTexture, WhiteTextureScaleMode, onFlushNeeded);
        AddQuad(x, y, width, height, color, onFlushNeeded: onFlushNeeded);
    }
    
    public void DrawCircleFilled(float centerX, float centerY, float radius, Color color, Action? onFlushNeeded = null)
    {
        int segments = CalculateCircleSegments(radius);
        EnsureVertexCapacity(segments * 3, onFlushNeeded);
        EnsureTextureBound(_whiteTexture, WhiteTextureScaleMode, onFlushNeeded);

        float angleStep = MathF.PI * 2f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;

            AddVertex(centerX, centerY, color, pixelSnap: false);
            AddVertex(centerX + MathF.Cos(angle1) * radius, centerY + MathF.Sin(angle1) * radius, color, pixelSnap: false);
            AddVertex(centerX + MathF.Cos(angle2) * radius, centerY + MathF.Sin(angle2) * radius, color, pixelSnap: false);
        }
    }
    
    public void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1, Action? onFlushNeeded = null)
    {
        int segments = CalculateCircleSegments(radius);
        EnsureVertexCapacity(segments * 6, onFlushNeeded);
        EnsureTextureBound(_whiteTexture, WhiteTextureScaleMode, onFlushNeeded);

        float halfThickness = Math.Max(thickness, 0.5f) / 2f;
        float innerRadius = radius - halfThickness;
        float outerRadius = radius + halfThickness;
        float angleStep = MathF.PI * 2f / segments;

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

            AddVertex(outerX1, outerY1, color, pixelSnap: false);
            AddVertex(outerX2, outerY2, color, pixelSnap: false);
            AddVertex(innerX1, innerY1, color, pixelSnap: false);

            AddVertex(outerX2, outerY2, color, pixelSnap: false);
            AddVertex(innerX2, innerY2, color, pixelSnap: false);
            AddVertex(innerX1, innerY1, color, pixelSnap: false);
        }
    }
    
    private void AddQuad(
        float x, float y, 
        float width, float height, 
        Color color,
        float u1 = 0, float v1 = 0, 
        float u2 = 1, float v2 = 1,
        Action? onFlushNeeded = null)
    {
        if (_vertexBatch.Count + 6 > MaxVertices)
        {
            onFlushNeeded?.Invoke();
        }

        AddVertex(x, y, color, u1, v1);
        AddVertex(x + width, y, color, u2, v1);
        AddVertex(x, y + height, color, u1, v2);

        AddVertex(x + width, y, color, u2, v1);
        AddVertex(x + width, y + height, color, u2, v2);
        AddVertex(x, y + height, color, u1, v2);
    }
    
    private void AddQuadRotated(
        float x, float y, 
        float width, float height, 
        float rotation, 
        Color color,
        float u1 = 0, float v1 = 0, 
        float u2 = 1, float v2 = 1,
        Action? onFlushNeeded = null)
    {
        EnsureVertexCapacity(6, onFlushNeeded);

        float centerX = x + width / 2f;
        float centerY = y + height / 2f;
        float halfW = width / 2f;
        float halfH = height / 2f;

        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);

        var topLeft = RotatePoint(-halfW, -halfH, cos, sin, centerX, centerY);
        var topRight = RotatePoint(halfW, -halfH, cos, sin, centerX, centerY);
        var bottomLeft = RotatePoint(-halfW, halfH, cos, sin, centerX, centerY);
        var bottomRight = RotatePoint(halfW, halfH, cos, sin, centerX, centerY);

        AddVertex(topLeft.X, topLeft.Y, color, u1, v1);
        AddVertex(topRight.X, topRight.Y, color, u2, v1);
        AddVertex(bottomLeft.X, bottomLeft.Y, color, u1, v2);

        AddVertex(topRight.X, topRight.Y, color, u2, v1);
        AddVertex(bottomRight.X, bottomRight.Y, color, u2, v2);
        AddVertex(bottomLeft.X, bottomLeft.Y, color, u1, v2);
    }
    
    private static (float X, float Y) RotatePoint(float x, float y, float cos, float sin, float centerX, float centerY)
    {
        return (
            x * cos - y * sin + centerX,
            x * sin + y * cos + centerY
        );
    }

    private void AddVertex(float x, float y, Color color, float u = 0, float v = 0, bool pixelSnap = true)
    {
        var position = new Vector2(x, y);

        if (_stateManager.Camera != null)
        {
            position = _stateManager.Camera.WorldToScreen(position);
        }

        if (pixelSnap)
        {
            position = new Vector2(MathF.Round(position.X), MathF.Round(position.Y));
        }

        _vertexBatch.Add(new Vertex
        {
            Position = position,
            Color = ColorToVector4(color),
            TexCoord = new Vector2(u, v)
        });
    }

    private void EnsureTextureBound(nint textureHandle, TextureScaleMode scaleMode, Action? onFlushNeeded)
    {
        if (_currentBoundTexture != nint.Zero &&
            (_currentBoundTexture != textureHandle || _currentTextureScaleMode != scaleMode))
        {
            onFlushNeeded?.Invoke();
        }

        _currentBoundTexture = textureHandle;
        _currentTextureScaleMode = scaleMode;
    }
    
    private void EnsureVertexCapacity(int verticesNeeded, Action? onFlushNeeded)
    {
        if (_vertexBatch.Count + verticesNeeded > MaxVertices)
        {
            onFlushNeeded?.Invoke();
        }
    }
    
    /// <summary>
    /// Copies the current vertex batch to the transfer buffer (CPU-only, no GPU pass).
    /// Returns (firstVertex, vertexCount) for use in a later copy pass.
    /// </summary>
    public unsafe (int FirstVertex, int VertexCount) StageForUpload()
    {
        if (_vertexBatch.Count == 0) return (_frameVertexOffset, 0);

        int firstVertex = _frameVertexOffset;
        int vertexCount = _vertexBatch.Count;
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
        if (mappedData != nint.Zero)
        {
            fixed (Vertex* vertexPtr = CollectionsMarshal.AsSpan(_vertexBatch))
            {
                Buffer.MemoryCopy(
                    vertexPtr,
                    (void*)(mappedData + (nint)byteOffset),
                    vertexDataSize,
                    vertexDataSize
                );
            }
            SDL3.SDL.UnmapGPUTransferBuffer(_device, _transferBuffers[_currentFrameSlot]);
        }

        _frameVertexOffset += vertexCount;
        return (firstVertex, vertexCount);
    }

    /// <summary>
    /// Issues the GPU copy command for a previously staged vertex range inside an already-open copy pass.
    /// </summary>
    public void UploadWithinCopyPass(nint copyPass, int firstVertex, int vertexCount)
    {
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
        if (_device == nint.Zero) return;

        for (int i = 0; i < FramesInFlight; i++)
        {
            if (_transferBuffers[i] != nint.Zero)
            {
                SDL3.SDL.ReleaseGPUTransferBuffer(_device, _transferBuffers[i]);
                _transferBuffers[i] = nint.Zero;
            }
        }
    }
    
    private static Vector4 ColorToVector4(Color color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    
    private static int CalculateCircleSegments(float radius) =>
        Math.Clamp((int)(MathF.Sqrt(radius) * 10), 32, 256);
    
    public void NewFrame(int absoluteFrameIndex)
    {
        _currentFrameSlot = absoluteFrameIndex % FramesInFlight;
        _frameVertexOffset = 0;
        _transferBufferCycleNeeded = false;
    }

    public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1, Action? onFlushNeeded = null)
    {
        EnsureVertexCapacity(6, onFlushNeeded);
        EnsureTextureBound(_whiteTexture, WhiteTextureScaleMode, onFlushNeeded);

        var dx = x2 - x1;
        var dy = y2 - y1;
        var length = MathF.Sqrt(dx * dx + dy * dy);

        if (length == 0) return;

        var angle = MathF.Atan2(dy, dx);
        var halfThickness = Math.Max(thickness, 0.5f) / 2f;
        var perpX = -MathF.Sin(angle) * halfThickness;
        var perpY = MathF.Cos(angle) * halfThickness;

        AddVertex(x1 + perpX, y1 + perpY, color, pixelSnap: false);
        AddVertex(x2 + perpX, y2 + perpY, color, pixelSnap: false);
        AddVertex(x1 - perpX, y1 - perpY, color, pixelSnap: false);

        AddVertex(x2 + perpX, y2 + perpY, color, pixelSnap: false);
        AddVertex(x2 - perpX, y2 - perpY, color, pixelSnap: false);
        AddVertex(x1 - perpX, y1 - perpY, color, pixelSnap: false);
    }
}