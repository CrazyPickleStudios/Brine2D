using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Brine2D.Core.Components;
using Brine2D.Core.Graphics;
using Brine2D.Core.Math;
using Brine2D.SDL.Hosting;
using SDL;
using static SDL.SDL3;

namespace Brine2D.SDL.Graphics;

// Sort options for sprites (after primary sort by Layer).
public enum SpriteSortMode
{
    None,            // Keep submission order within each Layer.
    ByTexture,       // Group by texture within each Layer.
    BackToFront,     // Within Layer: smaller depth first (good for transparency).
    FrontToBack      // Within Layer: larger depth first (good for opaque).
}

public enum SpriteBlendMode
{
    StraightAlpha,
    PremultipliedAlpha
}

public enum SpriteSamplerMode
{
    Linear,
    Nearest,
    Anisotropic // uses linear min/mag/mip; placeholder until explicit anisotropy is exposed in SDL
}

internal sealed unsafe class SdlSpriteRenderer : ISpriteRenderer, IDisposable
{
    private static readonly string PS_ENTRY = "PSMain";
    private static readonly string VS_ENTRY = "VSMain";

    private static int _frameId;
    private readonly List<Batch> _batches = new(8);
    private readonly List<DrawCmd> _draws = new(256);
    private readonly SdlHost _host;

    private readonly List<Item> _items = new(1024);

    // Per-layer parallax factors (default 1,1). Applied to camera translation.
    private readonly Dictionary<int, Vector2> _parallaxByLayer = new();
    private bool _begun;

    // Pipeline config
    private SpriteBlendMode _blendMode = SpriteBlendMode.StraightAlpha;

    // Active camera (null => screen-space)
    private Camera2D? _camera;

    // Debug
    private bool _debugLog;

    private SDL_GPUBuffer* _ib;
    private uint _ibSize;
    private uint _ibWriteOffset;
    private ushort[] _indices = Array.Empty<ushort>();

    // Wrapped GPU objects
    private GpuGraphicsPipeline? _pipeline;
    private SDL_GPUTextureFormat _pipelineTargetFormat = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID;
    private GpuShader? _ps;

    private GpuSampler? _sampler;
    private SpriteSamplerMode _samplerMode = SpriteSamplerMode.Linear;
    private SpriteSortMode _sortMode = SpriteSortMode.None;
    private int _targetW, _targetH;

    private SDL_GPUTransferBuffer* _tbufIB;
    private uint _tbufIBSize;

    // Per-frame transfer buffers (staging) reused across batches
    private SDL_GPUTransferBuffer* _tbufVB;
    private uint _tbufVBSize;

    // Per-frame ring buffers for vertices/indices
    private SDL_GPUBuffer* _vb;
    private uint _vbSize;
    private uint _vbWriteOffset;

    // CPU staging (frame-wide)
    private Vertex[] _verts = Array.Empty<Vertex>();
    private GpuShader? _vs;

    // Diagnostics exposed for overlay
    public int LastItemCount { get; private set; }
    public int LastBatchCount { get; private set; }
    public int LastDrawCallCount { get; private set; }

    public SdlSpriteRenderer(SdlHost host)
    {
        _host = host;
    }

    // Old Begin: screen-space rendering
    public void Begin(int? targetWidth = null, int? targetHeight = null)
    {
        Begin(null, targetWidth, targetHeight);
    }

    // New Begin with camera (world-space)
    public void Begin(Camera2D? camera, int? targetWidth = null, int? targetHeight = null)
    {
        // First Begin of the frame: only when there are no batches yet
        if (_batches.Count == 0)
        {
            _items.Clear();
            _draws.Clear();
            _batches.Clear();

            // Reset ring offsets for the new frame
            _vbWriteOffset = 0;
            _ibWriteOffset = 0;

            _targetW = targetWidth ?? _host.Width;
            _targetH = targetHeight ?? _host.Height;

            if (_debugLog)
            {
                _frameId++;
                Debug.WriteLine(
                    $"[Sprites] Frame {_frameId} start. Target={_targetW}x{_targetH}, ShaderFormat={_host.ActiveShaderFormat}");
            }
        }

        _begun = true;
        _camera = camera;

        if (_camera != null)
        {
            if (_camera.ViewWidth == 0)
            {
                _camera.ViewWidth = _targetW;
            }

            if (_camera.ViewHeight == 0)
            {
                _camera.ViewHeight = _targetH;
            }
        }

        // Start a new batch at the current item index
        _batches.Add(new Batch { ItemStart = _items.Count, Cam = camera });
    }

    public void ClearParallax()
    {
        _parallaxByLayer.Clear();
    }

    public void Dispose()
    {
        var dev = _host.Device;
        if (dev == null)
        {
            return;
        }

        _pipeline?.Dispose();
        _pipeline = null;
        _pipelineTargetFormat = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID;

        _ps?.Dispose();
        _ps = null;

        _vs?.Dispose();
        _vs = null;

        _sampler?.Dispose();
        _sampler = null;

        if (_vb != null)
        {
            SDL_ReleaseGPUBuffer(dev, _vb);
            _vb = null;
            _vbSize = 0;
            _vbWriteOffset = 0;
        }

        if (_ib != null)
        {
            SDL_ReleaseGPUBuffer(dev, _ib);
            _ib = null;
            _ibSize = 0;
            _ibWriteOffset = 0;
        }

        if (_tbufVB != null)
        {
            SDL_ReleaseGPUTransferBuffer(dev, _tbufVB);
            _tbufVB = null;
            _tbufVBSize = 0;
        }

        if (_tbufIB != null)
        {
            SDL_ReleaseGPUTransferBuffer(dev, _tbufIB);
            _tbufIB = null;
            _tbufIBSize = 0;
        }
    }

    // Back-compat API
    public void Draw(ITexture2D texture, Rectangle? src, Rectangle dst, Color color, float rotationRadians = 0f,
        Vector2? origin = null)
    {
        Draw(texture, src, dst, color, 0, 0f, rotationRadians, origin);
    }

    // New overload with layer/depth
    public void Draw(ITexture2D texture, Rectangle? src, Rectangle dst, Color color, int layer, float depth,
        float rotationRadians = 0f, Vector2? origin = null)
    {
        if (!_begun)
        {
            return;
        }

        if (texture is not SdlTexture2D sdlTex)
        {
            return;
        }

        var s = src ?? new Rectangle(0, 0, texture.Width, texture.Height);
        var o = origin ?? new Vector2(0, 0);

        _items.Add(new Item
        {
            Tex = sdlTex,
            Src = s,
            Dst = dst,
            Color = color,
            Rotation = rotationRadians,
            Origin = o,
            Layer = layer,
            Depth = depth,
            Order = _items.Count
        });
    }

    public void End()
    {
        _begun = false;
    }

    // Toggle blend mode (rebuild pipeline)
    public void SetBlendMode(SpriteBlendMode mode)
    {
        if (_blendMode == mode)
        {
            return;
        }

        _blendMode = mode;
        if (_pipeline != null)
        {
            _pipeline.Dispose();
            _pipeline = null;
            _pipelineTargetFormat = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID;
        }
    }

    public void SetDebugLogging(bool enabled)
    {
        _debugLog = enabled;
    }

    // Configure parallax for a layer (1,1 is default world movement).
    public void SetLayerParallax(int layer, float parallaxX, float parallaxY)
    {
        _parallaxByLayer[layer] = new Vector2(parallaxX, parallaxY);
    }

    // Toggle sampler mode
    public void SetSamplerMode(SpriteSamplerMode mode)
    {
        if (_samplerMode == mode)
        {
            return;
        }

        _samplerMode = mode;
        if (_sampler != null)
        {
            _sampler.Dispose();
            _sampler = null;
        }
    }

    // Set sort mode (applies next Begin->End batch).
    public void SetSortMode(SpriteSortMode mode)
    {
        _sortMode = mode;
    }

    internal void RenderInto(SDL_GPURenderPass* pass, SDL_GPUTexture* colorTarget, uint viewW, uint viewH,
        SDL_GPUTextureFormat targetFormat)
    {
        if (_items.Count == 0)
        {
            LastItemCount = 0;
            LastBatchCount = 0;
            LastDrawCallCount = 0;
            return;
        }

        if (_batches.Count == 0)
        {
            _batches.Add(new Batch { ItemStart = 0, Cam = null });
        }

        if (_debugLog)
        {
            Debug.WriteLine(
                $"[Sprites] Frame {_frameId}: Items={_items.Count}, Batches={_batches.Count}, Pass={viewW}x{viewH}, TargetFmt={targetFormat}");
        }

        EnsurePipelineAndSampler(targetFormat);

        // Ensure ring buffers and transfer buffers can hold the whole frame (no wrap mid-frame)
        var totalVBBytes = (uint)(_items.Count * 4 * sizeof(Vertex));
        var totalIBBytes = (uint)(_items.Count * 6 * sizeof(ushort));
        EnsureRingBuffers(totalVBBytes, totalIBBytes);
        EnsureTransferBuffers(totalVBBytes, totalIBBytes);

        // Per-frame base offsets in the ring buffers
        const uint Align = 16;
        var vbBase = AlignUp(_vbWriteOffset, Align);
        var ibBase = AlignUp(_ibWriteOffset, Align);

        // Prepare geometry emission records for later drawing
        List<EmitBatch> emits = new(Math.Max(1, _batches.Count));
        _draws.Clear(); // frame-wide draw commands (all batches flattened)

        // Sentinel to close last batch
        _batches.Add(new Batch { ItemStart = _items.Count, Cam = null });

        // Build geometry for all batches into the frame-wide CPU arrays once
        var totalV = 0;
        var totalI = 0;
        for (var b = 0; b < _batches.Count - 1; b++)
        {
            totalV += (_batches[b + 1].ItemStart - _batches[b].ItemStart) * 4;
            totalI += (_batches[b + 1].ItemStart - _batches[b].ItemStart) * 6;
        }

        EnsureCpuArrays(totalV, totalI);

        var vWrite = 0;
        var iWrite = 0;

        for (var b = 0; b < _batches.Count - 1; b++)
        {
            var start = _batches[b].ItemStart;
            var end = _batches[b + 1].ItemStart;
            var count = end - start;
            if (count <= 0)
            {
                if (_debugLog)
                {
                    Debug.WriteLine($"[Sprites] Batch {b}: empty");
                }

                continue;
            }

            // Per-batch camera/viewport
            var cam = _batches[b].Cam;
            var useCam = cam != null;
            int vpX = 0, vpY = 0, vpW = (int)viewW, vpH = (int)viewH;
            float halfW = vpW * 0.5f, halfH = vpH * 0.5f;
            float camCos = 1f, camSin = 0f, camZoom = 1f, camPosX = 0f, camPosY = 0f;
            var pixelSnap = false;

            if (useCam)
            {
                var vpi = cam!.ComputeViewport((int)viewW, (int)viewH);
                vpX = vpi.x;
                vpY = vpi.y;
                vpW = vpi.w;
                vpH = vpi.h;

                if (vpW <= 0 || vpH <= 0)
                {
                    if (_debugLog)
                    {
                        Debug.WriteLine(
                            $"[Sprites] Batch {b}: Camera viewport invalid {vpW}x{vpH}, fallback to full pass.");
                    }

                    vpX = 0;
                    vpY = 0;
                    vpW = (int)viewW;
                    vpH = (int)viewH;
                }

                halfW = vpW * 0.5f;
                halfH = vpH * 0.5f;

                camCos = MathF.Cos(cam.Rotation);
                camSin = MathF.Sin(cam.Rotation);
                camZoom = cam.Zoom <= 0f ? 0.0001f : cam.Zoom;
                camPosX = cam.Position.X;
                camPosY = cam.Position.Y;
                pixelSnap = cam.PixelSnap && MathF.Abs(cam.Rotation) < 1e-6f;
            }

            if (_debugLog)
            {
                Debug.WriteLine(
                    $"[Sprites] Batch {b}: start={start}, end={end}, count={count}, cam={(useCam ? "Y" : "N")}, vp=({vpX},{vpY},{vpW},{vpH}) camPos=({camPosX:F2},{camPosY:F2}) zoom={camZoom:F3} rot={(useCam ? cam!.Rotation : 0f):F3}");
            }

            // Slice/sort this batch
            var haveNonZeroLayer = false;
            for (var ii = start; ii < end; ii++)
            {
                if (_items[ii].Layer != 0)
                {
                    haveNonZeroLayer = true;
                    break;
                }
            }

            var items = _items.GetRange(start, count);
            if (_sortMode != SpriteSortMode.None || haveNonZeroLayer)
            {
                items.Sort(CompareItems);
            }

            // Local write bases into the big frame arrays
            var vBase = vWrite;
            var iBase = iWrite;

            // Build this batch's geometry into global arrays (indices/vertices relative to batch)
            SdlTexture2D? runTex = null;
            var runStartLocal = 0;
            var haveRun = false;

            var v = 0;
            var i = 0;
            ushort baseVertex = 0;

            // Precompute safe divisors for NDC mapping
            var invVpW2 = 2f / MathF.Max(1, vpW);
            var invVpH2 = 2f / MathF.Max(1, vpH);

            var nanCount = 0;

            for (var n = 0; n < items.Count; n++)
            {
                var it = items[n];

                if (!haveRun)
                {
                    runTex = it.Tex;
                    runStartLocal = i; // local to this batch
                    haveRun = true;
                }
                else if (it.Tex != runTex)
                {
                    _draws.Add(new DrawCmd
                    { Tex = runTex!, IndexStart = (uint)runStartLocal, IndexCount = (uint)(i - runStartLocal) });
                    runTex = it.Tex;
                    runStartLocal = i;
                }

                var cos = MathF.Cos(it.Rotation);
                var sin = MathF.Sin(it.Rotation);

                var ox = it.Origin.X;
                var oy = it.Origin.Y;

                float x = it.Dst.X;
                float y = it.Dst.Y;
                float w = it.Dst.Width;
                float h = it.Dst.Height;

                // Sprite quad around origin
                var x0 = x + -ox * cos - -oy * sin;
                var y0 = y + -ox * sin + -oy * cos;
                var x1 = x + (w - ox) * cos - -oy * sin;
                var y1 = y + (w - ox) * sin + -oy * cos;
                var x2 = x + (w - ox) * cos - (h - oy) * sin;
                var y2 = y + (w - ox) * sin + (h - oy) * cos;
                var x3 = x + -ox * cos - (h - oy) * sin;
                var y3 = y + -ox * sin + (h - oy) * cos;

                // World -> viewport-local pixels
                if (useCam)
                {
                    Vector2 par;
                    if (!_parallaxByLayer.TryGetValue(it.Layer, out par))
                    {
                        par = Vector2.One;
                    }

                    x0 -= camPosX * par.X;
                    y0 -= camPosY * par.Y;
                    x1 -= camPosX * par.X;
                    y1 -= camPosY * par.Y;
                    x2 -= camPosX * par.X;
                    y2 -= camPosY * par.Y;
                    x3 -= camPosX * par.X;
                    y3 -= camPosY * par.Y;

                    float tx, ty;
                    tx = x0 * camCos + y0 * camSin;
                    ty = -x0 * camSin + y0 * camCos;
                    x0 = tx;
                    y0 = ty;
                    tx = x1 * camCos + y1 * camSin;
                    ty = -x1 * camSin + y1 * camCos;
                    x1 = tx;
                    y1 = ty;
                    tx = x2 * camCos + y2 * camSin;
                    ty = -x2 * camSin + y2 * camCos;
                    x2 = tx;
                    y2 = ty;
                    tx = x3 * camCos + y3 * camSin;
                    ty = -x3 * camSin + y3 * camCos;
                    x3 = tx;
                    y3 = ty;

                    x0 = x0 * camZoom + halfW;
                    y0 = y0 * camZoom + halfH;
                    x1 = x1 * camZoom + halfW;
                    y1 = y1 * camZoom + halfH;
                    x2 = x2 * camZoom + halfW;
                    y2 = y2 * camZoom + halfH;
                    x3 = x3 * camZoom + halfW;
                    y3 = y3 * camZoom + halfH;

                    if (pixelSnap)
                    {
                        x0 = MathF.Round(x0);
                        y0 = MathF.Round(y0);
                        x1 = MathF.Round(x1);
                        y1 = MathF.Round(y1);
                        x2 = MathF.Round(x2);
                        y2 = MathF.Round(y2);
                        x3 = MathF.Round(x3);
                        y3 = MathF.Round(y3);
                    }
                }

                // UVs
                float u0 = it.Src.X;
                float v0 = it.Src.Y;
                float u1 = it.Src.X + it.Src.Width;
                float v1 = it.Src.Y + it.Src.Height;
                var invTW = 1f / it.Tex.Width;
                var invTH = 1f / it.Tex.Height;

                // Viewport-local pixels -> NDC (no offsets; viewport state handles the position)
                var nx0 = x0 * invVpW2 - 1f;
                var ny0 = 1f - y0 * invVpH2;
                var nx1 = x1 * invVpW2 - 1f;
                var ny1 = 1f - y1 * invVpH2;
                var nx2 = x2 * invVpW2 - 1f;
                var ny2 = 1f - y2 * invVpH2;
                var nx3 = x3 * invVpW2 - 1f;
                var ny3 = 1f - y3 * invVpH2;

                // NaN guard
                if (float.IsNaN(nx0) || float.IsNaN(ny0) || float.IsNaN(nx1) || float.IsNaN(ny1) ||
                    float.IsNaN(nx2) || float.IsNaN(ny2) || float.IsNaN(nx3) || float.IsNaN(ny3))
                {
                    nanCount++;
                }

                _verts[vBase + v + 0] = new Vertex(nx0, ny0, u0 * invTW, v0 * invTH, it.Color);
                _verts[vBase + v + 1] = new Vertex(nx1, ny1, u1 * invTW, v0 * invTH, it.Color);
                _verts[vBase + v + 2] = new Vertex(nx2, ny2, u1 * invTW, v1 * invTH, it.Color);
                _verts[vBase + v + 3] = new Vertex(nx3, ny3, u0 * invTW, v1 * invTH, it.Color);

                _indices[iBase + i + 0] = (ushort)(baseVertex + 0);
                _indices[iBase + i + 1] = (ushort)(baseVertex + 1);
                _indices[iBase + i + 2] = (ushort)(baseVertex + 2);
                _indices[iBase + i + 3] = (ushort)(baseVertex + 2);
                _indices[iBase + i + 4] = (ushort)(baseVertex + 3);
                _indices[iBase + i + 5] = (ushort)(baseVertex + 0);

                v += 4;
                i += 6;
                baseVertex += 4;
            }

            if (haveRun)
            {
                _draws.Add(new DrawCmd
                { Tex = runTex!, IndexStart = (uint)runStartLocal, IndexCount = (uint)(i - runStartLocal) });
            }

            if (_debugLog)
            {
                if (items.Count > 0)
                {
                    var p = _verts[vBase];
                    Debug.WriteLine(
                        $"[Sprites] Batch {b}: verts={count * 4}, indices={count * 6}, runs={_draws.Count}, firstNDC=({p.X:F3},{p.Y:F3}), NaNVerts={nanCount * 4}");
                }
                else
                {
                    Debug.WriteLine($"[Sprites] Batch {b}: (no items after sort)");
                }
            }

            // Record this batch's offsets in the ring buffers and viewport
            emits.Add(new EmitBatch
            {
                VbOffset = vbBase + (uint)(vBase * sizeof(Vertex)),
                IbOffset = ibBase + (uint)(iBase * sizeof(ushort)),
                VpX = vpX,
                VpY = vpY,
                VpW = vpW,
                VpH = vpH,
                DrawsStart = _draws.Count - (haveRun ? 1 : 0), // placeholder; adjusted below
                DrawsCount = 0 // will fix after adjusting start
            });

            // Fixup draws range for this batch (from last recorded start)
            var lastIdx = emits.Count - 1;
            var startDrawIndex = lastIdx == 0 ? 0 : emits[lastIdx - 1].DrawsStart + emits[lastIdx - 1].DrawsCount;
            emits[lastIdx] = new EmitBatch
            {
                VbOffset = emits[lastIdx].VbOffset,
                IbOffset = emits[lastIdx].IbOffset,
                VpX = emits[lastIdx].VpX,
                VpY = emits[lastIdx].VpY,
                VpW = emits[lastIdx].VpW,
                VpH = emits[lastIdx].VpH,
                DrawsStart = startDrawIndex,
                DrawsCount = _draws.Count - startDrawIndex
            };

            // Advance frame-wide write heads
            vWrite += v;
            iWrite += i;
        }

        // Upload entire frame's geometry once (single copy pass, 2 uploads)
        var vbBytes = (uint)(vWrite * sizeof(Vertex));
        var ibBytes = (uint)(iWrite * sizeof(ushort));
        UploadFrameOnce(vbBase, vbBytes, ibBase, ibBytes);

        // Commit ring offsets
        _vbWriteOffset = vbBase + vbBytes;
        _ibWriteOffset = ibBase + ibBytes;

        // Diagnostics: expose counts to overlay
        LastItemCount = _items.Count;
        LastBatchCount = emits.Count;
        LastDrawCallCount = _draws.Count;

        // Now draw per batch from the uploaded ring regions
        for (var b = 0; b < emits.Count; b++)
        {
            var e = emits[b];

            SDL_BindGPUGraphicsPipeline(pass, _pipeline!.Ptr);

            SDL_GPUViewport vpState;
            vpState.x = (uint)e.VpX;
            vpState.y = (uint)e.VpY;
            vpState.w = (uint)e.VpW;
            vpState.h = (uint)e.VpH;
            vpState.min_depth = 0;
            vpState.max_depth = 1;
            SDL_SetGPUViewport(pass, &vpState);

            SDL_GPUBufferBinding vbBind;
            vbBind.buffer = _vb;
            vbBind.offset = e.VbOffset;
            SDL_BindGPUVertexBuffers(pass, 0, &vbBind, 1);

            SDL_GPUBufferBinding ibBind;
            ibBind.buffer = _ib;
            ibBind.offset = e.IbOffset;
            SDL_BindGPUIndexBuffer(pass, &ibBind, SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT);

            var endDraw = e.DrawsStart + e.DrawsCount;
            for (var d = e.DrawsStart; d < endDraw; d++)
            {
                var dc = _draws[d];
                SDL_GPUTextureSamplerBinding ts;
                ts.texture = dc.Tex.Texture;
                ts.sampler = _sampler!.Ptr;
                SDL_BindGPUFragmentSamplers(pass, 0, &ts, 1);

                SDL_DrawGPUIndexedPrimitives(pass, dc.IndexCount, 1, dc.IndexStart, 0, 0);
            }
        }

        if (_debugLog)
        {
            Debug.WriteLine($"[Sprites] Frame {_frameId} end.");
        }

        // Reset for next frame
        _items.Clear();
        _draws.Clear();
        _batches.Clear();
    }

    private static uint AlignUp(uint value, uint align)
    {
        Debug.Assert((align & (align - 1)) == 0);

        return value + (align - 1) & ~(align - 1);
    }

    private static uint RoundUpPow2(uint v)
    {
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;
        return v;
    }

    private static byte[] ToUtf8(string s)
    {
        return Encoding.UTF8.GetBytes(s + "\0");
    }

    private int CompareItems(Item a, Item b)
    {
        var c = a.Layer.CompareTo(b.Layer);
        if (c != 0)
        {
            return c;
        }

        switch (_sortMode)
        {
            case SpriteSortMode.ByTexture:
                {
                    var pa = (nuint)a.Tex.Texture;
                    var pb = (nuint)b.Tex.Texture;
                    if (pa != pb)
                    {
                        return pa < pb ? -1 : 1;
                    }

                    break;
                }
            case SpriteSortMode.BackToFront:
                {
                    c = a.Depth.CompareTo(b.Depth); // smaller first
                    if (c != 0)
                    {
                        return c;
                    }

                    break;
                }
            case SpriteSortMode.FrontToBack:
                {
                    c = b.Depth.CompareTo(a.Depth); // larger first
                    if (c != 0)
                    {
                        return c;
                    }

                    break;
                }
            case SpriteSortMode.None:
            default:
                break; // fall through to Order
        }

        return a.Order.CompareTo(b.Order); // stable
    }

    private void EnsureCpuArrays(int vCount, int iCount)
    {
        if (_verts.Length < vCount)
        {
            Array.Resize(ref _verts, Math.Max(vCount, _verts.Length * 2 + 64));
        }

        if (_indices.Length < iCount)
        {
            Array.Resize(ref _indices, Math.Max(iCount, _indices.Length * 2 + 64));
        }
    }

    private void EnsurePipelineAndSampler(SDL_GPUTextureFormat targetFormat)
    {
        var dev = _host.Device;
        if (dev == null)
        {
            return;
        }

        if (_sampler == null)
        {
            SDL_GPUSamplerCreateInfo sci = default;
            var useLinear = _samplerMode != SpriteSamplerMode.Nearest;
            sci.min_filter = useLinear ? SDL_GPUFilter.SDL_GPU_FILTER_LINEAR : SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;
            sci.mag_filter = useLinear ? SDL_GPUFilter.SDL_GPU_FILTER_LINEAR : SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;
            sci.mipmap_mode = useLinear
                ? SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR
                : SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST;
            sci.address_mode_u = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
            sci.address_mode_v = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
            sci.address_mode_w = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
            sci.enable_compare = false;

            var sp = SDL_CreateGPUSampler(dev, &sci);
            if (sp == null)
            {
                throw new InvalidOperationException($"SDL_CreateGPUSampler failed: {SDL_GetError()}");
            }

            _sampler = new GpuSampler(dev, sp, "Sprites:Sampler");
            _host.RegisterResource(_sampler);
        }

        if (_pipeline != null && _pipelineTargetFormat == targetFormat)
        {
            return;
        }

        _pipeline?.Dispose();
        _pipeline = null;

        _ps?.Dispose();
        _ps = null;

        _vs?.Dispose();
        _vs = null;

        var fmt = _host.ActiveShaderFormat;
        var (vsBytes, psBytes) = SpriteShaders.GetShaders(fmt);
        if (vsBytes.Length == 0 || psBytes.Length == 0)
        {
            throw new InvalidOperationException(
                "Sprite shader bytecode missing. Provide compiled shaders for the active backend.");
        }

        fixed (byte* vsCode = vsBytes)
        fixed (byte* psCode = psBytes)
        {
            var vsEntry = ToUtf8(VS_ENTRY);
            var psEntry = ToUtf8(PS_ENTRY);
            fixed (byte* pVsEntry = vsEntry)
            fixed (byte* pPsEntry = psEntry)
            {
                SDL_GPUShaderCreateInfo vsci = default;
                vsci.code = vsCode;
                vsci.code_size = (nuint)vsBytes.Length;
                vsci.entrypoint = pVsEntry;
                vsci.stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX;
                vsci.format = fmt;
                var vptr = SDL_CreateGPUShader(dev, &vsci);
                if (vptr == null)
                {
                    throw new InvalidOperationException($"SDL_CreateGPUShader (VS) failed: {SDL_GetError()}");
                }

                _vs = new GpuShader(dev, vptr, "Sprites:VS");
                _host.RegisterResource(_vs);

                SDL_GPUShaderCreateInfo psci = default;
                psci.code = psCode;
                psci.code_size = (nuint)psBytes.Length;
                psci.entrypoint = pPsEntry;
                psci.stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT;
                psci.format = fmt;
                psci.num_samplers = 1;
                var pptr = SDL_CreateGPUShader(dev, &psci);
                if (pptr == null)
                {
                    throw new InvalidOperationException($"SDL_CreateGPUShader (PS) failed: {SDL_GetError()}");
                }

                _ps = new GpuShader(dev, pptr, "Sprites:PS");
                _host.RegisterResource(_ps);
            }
        }

        var vbufDesc = new SDL_GPUVertexBufferDescription
        {
            slot = 0,
            pitch = 32,
            input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
            instance_step_rate = 0
        };

        var attrs = stackalloc SDL_GPUVertexAttribute[3];
        attrs[0] = new SDL_GPUVertexAttribute
        {
            location = 0,
            buffer_slot = 0,
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
            offset = 0u
        };
        attrs[1] = new SDL_GPUVertexAttribute
        {
            location = 1,
            buffer_slot = 0,
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
            offset = 8u
        };
        attrs[2] = new SDL_GPUVertexAttribute
        {
            location = 2,
            buffer_slot = 0,
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
            offset = 16u
        };

        SDL_GPUVertexInputState vis = default;
        vis.vertex_buffer_descriptions = &vbufDesc;
        vis.num_vertex_buffers = 1;
        vis.vertex_attributes = attrs;
        vis.num_vertex_attributes = 3;

        SDL_GPURasterizerState rast = default;
        rast.fill_mode = SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL;
        rast.cull_mode = SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE;
        rast.front_face = SDL_GPUFrontFace.SDL_GPU_FRONTFACE_COUNTER_CLOCKWISE;

        SDL_GPUMultisampleState ms = default;
        ms.sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1;

        SDL_GPUDepthStencilState ds = default;
        ds.enable_depth_test = false;
        ds.enable_depth_write = false;

        // Blend mode: StraightAlpha uses SRC_ALPHA; Premultiplied uses ONE
        var blend = new SDL_GPUColorTargetBlendState
        {
            enable_blend = true,
            src_color_blendfactor = _blendMode == SpriteBlendMode.PremultipliedAlpha
                ? SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE
                : SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA,
            dst_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
            color_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
            src_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
            dst_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
            alpha_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
            enable_color_write_mask = true,
            color_write_mask = (SDL_GPUColorComponentFlags)(SDL_GPU_COLORCOMPONENT_R | SDL_GPU_COLORCOMPONENT_G |
                                                            SDL_GPU_COLORCOMPONENT_B | SDL_GPU_COLORCOMPONENT_A)
        };

        var ct = stackalloc SDL_GPUColorTargetDescription[1];
        ct[0] = new SDL_GPUColorTargetDescription
        {
            format = targetFormat,
            blend_state = blend
        };

        SDL_GPUGraphicsPipelineTargetInfo pti = default;
        pti.color_target_descriptions = ct;
        pti.num_color_targets = 1;
        pti.has_depth_stencil_target = false;

        SDL_GPUGraphicsPipelineCreateInfo pci = default;
        pci.vertex_shader = _vs!.Ptr;
        pci.fragment_shader = _ps!.Ptr;
        pci.vertex_input_state = vis;
        pci.primitive_type = SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST;
        pci.rasterizer_state = rast;
        pci.multisample_state = ms;
        pci.depth_stencil_state = ds;
        pci.target_info = pti;

        var pipe = SDL_CreateGPUGraphicsPipeline(dev, &pci);
        if (pipe == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUGraphicsPipeline failed: {SDL_GetError()}");
        }

        _pipeline = new GpuGraphicsPipeline(dev, pipe, $"Sprites:Pipeline[{targetFormat}]");
        _host.RegisterResource(_pipeline);

        _pipelineTargetFormat = targetFormat;
    }

    private void EnsureRingBuffers(uint neededVB, uint neededIB)
    {
        var dev = _host.Device;
        if (dev == null)
        {
            return;
        }

        if (_vb == null || _vbSize < neededVB)
        {
            if (_vb != null)
            {
                SDL_ReleaseGPUBuffer(dev, _vb);
            }

            var size = RoundUpPow2(Math.Max(neededVB, 1u << 20)); // at least 1MB
            SDL_GPUBufferCreateInfo ci = default;
            ci.usage = (SDL_GPUBufferUsageFlags)(SDL_GPU_BUFFERUSAGE_VERTEX |
                                                 SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ);
            ci.size = size;
            _vb = SDL_CreateGPUBuffer(dev, &ci);
            if (_vb == null)
            {
                throw new InvalidOperationException($"Create VB failed: {SDL_GetError()}");
            }

            _vbSize = size;
        }

        if (_ib == null || _ibSize < neededIB)
        {
            if (_ib != null)
            {
                SDL_ReleaseGPUBuffer(dev, _ib);
            }

            var size = RoundUpPow2(Math.Max(neededIB, 256u << 10)); // at least 256KB
            SDL_GPUBufferCreateInfo ci = default;
            ci.usage = (SDL_GPUBufferUsageFlags)(SDL_GPU_BUFFERUSAGE_INDEX | SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ);
            ci.size = size;
            _ib = SDL_CreateGPUBuffer(dev, &ci);
            if (_ib == null)
            {
                throw new InvalidOperationException($"Create IB failed: {SDL_GetError()}");
            }

            _ibSize = size;
        }
    }

    private void EnsureTransferBuffers(uint neededVB, uint neededIB)
    {
        var dev = _host.Device;
        if (dev == null)
        {
            return;
        }

        if (_tbufVB == null || _tbufVBSize < neededVB)
        {
            if (_tbufVB != null)
            {
                SDL_ReleaseGPUTransferBuffer(dev, _tbufVB);
            }

            SDL_GPUTransferBufferCreateInfo tci = default;
            tci.usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD;
            tci.size = RoundUpPow2(Math.Max(neededVB, 1u << 20)); // >= 1MB
            _tbufVB = SDL_CreateGPUTransferBuffer(dev, &tci);
            if (_tbufVB == null)
            {
                throw new InvalidOperationException($"Create VB transfer buffer failed: {SDL_GetError()}");
            }

            _tbufVBSize = tci.size;
        }

        if (_tbufIB == null || _tbufIBSize < neededIB)
        {
            if (_tbufIB != null)
            {
                SDL_ReleaseGPUTransferBuffer(dev, _tbufIB);
            }

            SDL_GPUTransferBufferCreateInfo tci = default;
            tci.usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD;
            tci.size = RoundUpPow2(Math.Max(neededIB, 256u << 10)); // >= 256KB
            _tbufIB = SDL_CreateGPUTransferBuffer(dev, &tci);
            if (_tbufIB == null)
            {
                throw new InvalidOperationException($"Create IB transfer buffer failed: {SDL_GetError()}");
            }

            _tbufIBSize = tci.size;
        }
    }

    // Upload both vertex and index data in a single copy pass
    private void UploadFrameOnce(uint vbBase, uint vbBytes, uint ibBase, uint ibBytes)
    {
        var dev = _host.Device;
        if (dev == null)
        {
            throw new InvalidOperationException("No GPU device.");
        }

        // Map and copy vertices
        if (vbBytes > 0)
        {
            if (vbBase + vbBytes > _tbufVBSize)
            {
                throw new InvalidOperationException("VB transfer buffer too small for frame upload.");
            }

            var mappedVB = SDL_MapGPUTransferBuffer(dev, _tbufVB, false);
            if (mappedVB == nint.Zero)
            {
                throw new InvalidOperationException($"Map VB transfer failed: {SDL_GetError()}");
            }

            fixed (Vertex* srcV = _verts)
            {
                var dst = (byte*)mappedVB + vbBase;
                Buffer.MemoryCopy(srcV, dst, vbBytes, vbBytes);
            }

            SDL_UnmapGPUTransferBuffer(dev, _tbufVB);
        }

        // Map and copy indices
        if (ibBytes > 0)
        {
            if (ibBase + ibBytes > _tbufIBSize)
            {
                throw new InvalidOperationException("IB transfer buffer too small for frame upload.");
            }

            var mappedIB = SDL_MapGPUTransferBuffer(dev, _tbufIB, false);
            if (mappedIB == nint.Zero)
            {
                throw new InvalidOperationException($"Map IB transfer failed: {SDL_GetError()}");
            }

            fixed (ushort* srcI = _indices)
            {
                var dst = (byte*)mappedIB + ibBase;
                Buffer.MemoryCopy(srcI, dst, ibBytes, ibBytes);
            }

            SDL_UnmapGPUTransferBuffer(dev, _tbufIB);
        }

        // Single copy pass with two uploads
        var cmd = SDL_AcquireGPUCommandBuffer(dev);
        if (cmd == null)
        {
            throw new InvalidOperationException("Acquire command buffer failed.");
        }

        var copy = SDL_BeginGPUCopyPass(cmd);

        if (vbBytes > 0)
        {
            SDL_GPUTransferBufferLocation srcLocV = default;
            srcLocV.transfer_buffer = _tbufVB;
            srcLocV.offset = vbBase;

            SDL_GPUBufferRegion dstRegV = default;
            dstRegV.buffer = _vb;
            dstRegV.offset = vbBase;
            dstRegV.size = vbBytes;

            SDL_UploadToGPUBuffer(copy, &srcLocV, &dstRegV, false);
        }

        if (ibBytes > 0)
        {
            SDL_GPUTransferBufferLocation srcLocI = default;
            srcLocI.transfer_buffer = _tbufIB;
            srcLocI.offset = ibBase;

            SDL_GPUBufferRegion dstRegI = default;
            dstRegI.buffer = _ib;
            dstRegI.offset = ibBase;
            dstRegI.size = ibBytes;

            SDL_UploadToGPUBuffer(copy, &srcLocI, &dstRegI, false);
        }

        SDL_EndGPUCopyPass(copy);
        SDL_SubmitGPUCommandBuffer(cmd);
    }

    private struct Batch
    {
        public int ItemStart;
        public Camera2D? Cam;
    }

    private struct DrawCmd
    {
        public SdlTexture2D Tex;
        public uint IndexStart; // relative to the batch's bound IB offset
        public uint IndexCount;
    }

    private struct EmitBatch
    {
        public uint VbOffset;
        public uint IbOffset;
        public int VpX, VpY, VpW, VpH;
        public int DrawsStart;
        public int DrawsCount;
    }

    private struct Item
    {
        public SdlTexture2D Tex;
        public Rectangle Src;
        public Rectangle Dst;
        public Color Color;
        public float Rotation;
        public Vector2 Origin;
        public int Layer;
        public float Depth;
        public int Order;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Vertex
    {
        public float X, Y;
        public float U, V;
        public float R, G, B, A;

        public Vertex(float x, float y, float u, float v, Color c)
        {
            X = x;
            Y = y;
            U = u;
            V = v;
            R = c.R / 255f;
            G = c.G / 255f;
            B = c.B / 255f;
            A = c.A / 255f;
        }
    }
}