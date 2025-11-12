using Brine2D.Core.Graphics;
using Brine2D.Core.Input;
using Brine2D.Core.Math;
using Brine2D.SDL.Content.Loaders;
using Brine2D.SDL.Graphics;
using SDL;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using static SDL.SDL3;
using static SDL.SDL3_ttf;

namespace Brine2D.SDL.Hosting;

internal enum OverlayCorner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

internal sealed unsafe class DebugOverlay : IDisposable
{
    private readonly SdlHost _host;
    private readonly SdlSpriteRenderer _sprites;
    private readonly int _mainThreadId;

    // 1x1 white texture (tinted)
    private SdlTexture2D? _solidTex;

    // TTF state
    private TTF_Font* _ttfFont;
    private int _ttfPtSize;
    private SdlTexture2D? _ttfTex;
    private int _ttfTexW, _ttfTexH;
    private string _lastText = string.Empty;

    // Reused upload buffer for font
    private SDL_GPUTransferBuffer* _ttfTbuf;
    private uint _ttfTbufSize;

    // Layout / wrap (logical values; scaled at draw)
    private int _outerMarginPx = 12;
    private int _padLeft = 16;
    private int _padTop = 6;
    private int _padRight = 16;
    private int _padBottom = 6;
    private int _borderPx = 3;
    private bool _autoWrap;
    private int _wrapWidthPx;
    private int _lastWrapWidth = -1;

    // UI scaling
    private float _uiScale = 1f; // affects layout only

    // Colors
    private Color _textColor = Color.White;
    private Color _borderColor = new Color(160, 164, 170);
    private Color _panelColor = new Color(48, 52, 58, 235);

    // Visibility & corner
    private bool _visible;
    private OverlayCorner _corner = OverlayCorner.TopLeft;
    private bool _showPanel = true;
    private bool _showBorder = true;

    // Input toggle chord (12)
    private Key _toggleKey = Key.F12;
    private KeyboardModifiers _toggleMods = 0;
    private bool _usePlatformShortcut = true; // platform-aware Ctrl/Cmd when true

    // FPS smoothing & history (7)
    private float _fpsEma;
    private double _lastFpsUpdate;
    private int _frameCountAccum;
    private const int FpsHistCapacity = 120;
    private readonly float[] _fpsHist = new float[FpsHistCapacity];
    private int _fpsHistCount;
    private int _fpsHistIndex;

    // Embedded font fallback
    private const string EmbeddedFontResourceName = "Content.Fonts.JetBrainsMono-Regular.ttf";
    private bool _triedEmbedded;

    // Throttled text update
    private bool _textDirty = true;
    private double _nextTextUpdate;
    private double _updatePeriodSec = 0.25; // 4 Hz default

    // Target FPS for budget/severity (15,3)
    private double _targetFps = 60.0;
    private bool _autoFpsColoring = true; // (3)

    // Reused builder (8)
    private readonly StringBuilder _sb = new(256);

    // Exposed metrics
    public double LastUpdateMs { get; private set; }
    public double LastDrawMs { get; private set; }
    public double LastFrameMs { get; private set; }
    public float SmoothedFps => _fpsEma;
    public int LastSpriteItems { get; private set; }
    public int LastSpriteBatches { get; private set; }
    public int LastSpriteDrawCalls { get; private set; }
    public int TrackedResources => _host.LiveTrackedResources;

    // Font lifetime tracking
    private bool _ownsFont;                 // true if we opened it (memory / embedded)
    private TtfFont? _sharedFontRef;        // strong ref to externally supplied font (not owned)
    private bool _fontPermanentlyInvalid;   // stop trying after repeated invalidation

    public DebugOverlay(SdlHost host, SdlSpriteRenderer sprites)
    {
        _host = host;
        _sprites = sprites;
        _mainThreadId = Environment.CurrentManagedThreadId;
    }

    // Visibility & appearance
    public void Toggle() => _visible = !_visible;
    public void SetVisible(bool v) { _visible = v; }

    public void SetCorner(OverlayCorner corner) { _corner = corner; _textDirty = true; }
    public void SetPanelVisible(bool visible) { _showPanel = visible; _textDirty = true; }
    public void SetBorderVisible(bool visible) { _showBorder = visible; _textDirty = true; }

    // UI scaling (affects layout calculations, not font point size)
    public void SetUiScale(float scale)
    {
        _uiScale = System.Math.Clamp(scale, 0.5f, 4f);
        if (_autoWrap) UpdateAutoWrapWidth();
        _textDirty = true;
    }

    // Update period for text metrics (8)
    public void SetUpdatePeriod(double seconds) => _updatePeriodSec = Math.Max(0.05, seconds);

    // Target FPS / severity coloring (3,15)
    public void SetTargetFps(double fps) { _targetFps = Math.Max(1.0, fps); _textDirty = true; }
    public void SetAutoFpsColoring(bool enabled) { _autoFpsColoring = enabled; }

    // Toggle chord (12)
    public void SetToggleChord(Key key, KeyboardModifiers mods, bool usePlatformShortcut = true)
    {
        _toggleKey = key;
        _toggleMods = mods;
        _usePlatformShortcut = usePlatformShortcut;
    }

    // Called by host each frame to see if we should toggle
    public bool ShouldToggle(SdlHost host)
    {
        return _usePlatformShortcut
            ? host.WasShortcutPressed(_toggleKey)
            : host.WasChordPressed(_toggleKey, _toggleMods);
    }

    // Color configuration
    public void SetColors(Color text, Color panel, Color border)
    {
        _textColor = text;
        _panelColor = panel;
        _borderColor = border;
        _textDirty = true;
    }
    public void SetTextColor(Color text) { _textColor = text; _textDirty = true; }
    public void SetPanelColor(Color panel) { _panelColor = panel; _textDirty = true; }
    public void SetBorderColor(Color border) { _borderColor = border; _textDirty = true; }

    // Alpha-only setters (14)
    public void SetPanelAlpha(byte a) => _panelColor = new Color(_panelColor.R, _panelColor.G, _panelColor.B, a);
    public void SetBorderAlpha(byte a) => _borderColor = new Color(_borderColor.R, _borderColor.G, _borderColor.B, a);

    // Layout configuration (logical values)
    public void SetBorderThickness(int pixels)
    {
        _borderPx = Math.Max(0, pixels);
        if (_autoWrap) UpdateAutoWrapWidth();
        _textDirty = true;
        _lastText = string.Empty;
    }
    public void SetPadding(int left, int top, int right, int bottom)
    {
        _padLeft = Math.Max(0, left);
        _padTop = Math.Max(0, top);
        _padRight = Math.Max(0, right);
        _padBottom = Math.Max(0, bottom);
        if (_autoWrap) UpdateAutoWrapWidth();
        _textDirty = true;
        _lastText = string.Empty;
    }
    public void SetOuterMargin(int pixels)
    {
        _outerMarginPx = Math.Max(0, pixels);
        if (_autoWrap) UpdateAutoWrapWidth();
        _textDirty = true;
        _lastText = string.Empty;
    }

    // Wrapping
    public void SetTtfWrapWidth(int pixels)
    {
        _autoWrap = false;
        _wrapWidthPx = Math.Max(0, pixels);
        _textDirty = true;
        _lastText = string.Empty;
    }
    public void AutoWrapToWindow(int outerMarginPx = 12)
    {
        _autoWrap = true;
        _outerMarginPx = Math.Max(0, outerMarginPx);
        UpdateAutoWrapWidth();
        _textDirty = true;
    }
    public void OnWindowResized()
    {
        if (_autoWrap) UpdateAutoWrapWidth();
        _textDirty = true;
    }
    private void UpdateAutoWrapWidth()
    {
        // Scaled usable width
        int margin = Scaled(_outerMarginPx);
        int border = Scaled(_borderPx);
        int padL = Scaled(_padLeft);
        int padR = Scaled(_padRight);

        var usable = _host.Width - (margin * 2) - (border * 2) - (padL + padR);
        var desired = Math.Max(usable, 1);
        if (desired != _wrapWidthPx)
        {
            _wrapWidthPx = desired;
            _textDirty = true;
            _lastText = string.Empty;
        }
    }

    // Font configuration
    public void UseTtfFontFromContent(string contentPath, int ptSize)
    {
        var bytes = _host.Content.Load<byte[]>(contentPath);
        UseTtfFontFromMemory(bytes, ptSize);
    }
    public void UseTtfFontFromMemory(byte[] fontBytes, int ptSize)
    {
        // Release previous owned font if any
        if (_ownsFont && _ttfFont != null)
        {
            TTF_CloseFont(_ttfFont);
            _ttfFont = null;
        }
        _sharedFontRef = null;
        _fontPermanentlyInvalid = false;

        fixed (byte* p = fontBytes)
        {
            var io = SDL_IOFromConstMem((nint)p, (nuint)fontBytes.Length);
            if (io == null)
                throw new InvalidOperationException($"SDL_IOFromConstMem failed: {SDL_GetError()}");

            _ttfFont = TTF_OpenFontIO(io, true, ptSize);
            if (_ttfFont == null)
                throw new InvalidOperationException($"TTF_OpenFontIO failed: {SDL_GetError()}");
        }

        _ownsFont = true;
        _ttfPtSize = ptSize;
        _textDirty = true;
        _lastText = string.Empty;
        ReleaseTtfTexture();
    }
    public void UseTtfFontAsset(TtfFont font)
    {
        if (font == null) throw new ArgumentNullException(nameof(font));

        // Release previously owned font (if we owned it)
        if (_ownsFont && _ttfFont != null)
        {
            TTF_CloseFont(_ttfFont);
        }

        _ownsFont = false;
        _sharedFontRef = font;          // keep strong reference so native font isn't freed
        _ttfFont = font.Ptr;
        _ttfPtSize = font.PointSize;
        _fontPermanentlyInvalid = false;
        _textDirty = true;
        _lastText = string.Empty;
        ReleaseTtfTexture();
    }
    public bool EnsureEmbeddedFallbackFont(int ptSize = 14)
    {
        if (_ttfFont != null) return true;
        if (_triedEmbedded) return false;

        _triedEmbedded = true;
        var asm = typeof(DebugOverlay).Assembly;
        using var stream = asm.GetManifestResourceStream(EmbeddedFontResourceName);
        if (stream == null) return false;

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        UseTtfFontFromMemory(ms.ToArray(), ptSize);
        return true;
    }
    private void ReleaseTtfTexture()
    {
        if (_ttfTex != null)
        {
            _ttfTex.Dispose();
            _ttfTex = null;
            _ttfTexW = _ttfTexH = 0;
        }
    }

    // Metrics update + FPS history (7)
    public void UpdateMetrics(double updateMs, double drawMs)
    {
        LastUpdateMs = updateMs;
        LastDrawMs = drawMs;
        LastFrameMs = updateMs + drawMs;
        LastSpriteItems = _sprites.LastItemCount;
        LastSpriteBatches = _sprites.LastBatchCount;
        LastSpriteDrawCalls = _sprites.LastDrawCallCount;

        _frameCountAccum++;
        var now = SDL_GetTicks() / 1000.0;
        if (now - _lastFpsUpdate >= 0.5)
        {
            var fps = _frameCountAccum / (now - _lastFpsUpdate);
            _fpsEma = _fpsEma <= 0 ? (float)fps : (float)(_fpsEma * 0.85 + fps * 0.15);
            _frameCountAccum = 0;
            _lastFpsUpdate = now;

            // Push into history
            _fpsHist[_fpsHistIndex] = (float)fps;
            _fpsHistIndex = (_fpsHistIndex + 1) % FpsHistCapacity;
            if (_fpsHistCount < FpsHistCapacity) _fpsHistCount++;

            _textDirty = true; // metrics changed
        }

        // Throttle text refresh
        if (now >= _nextTextUpdate)
        {
            _textDirty = true;
            _nextTextUpdate = now + _updatePeriodSec;
        }
    }

    public void Draw()
    {
        if (!_visible) return;
        if (_host.Device == null || _host.IsClosing) return; // skip while shutting down/device lost
        if (_host.Width <= 0 || _host.Height <= 0) return;   // minimized or zero-sized window
        Debug.Assert(Environment.CurrentManagedThreadId == _mainThreadId, "[Overlay] Draw must run on main thread.");

        if (_ttfFont == null && !EnsureEmbeddedFallbackFont(14))
            return;

        if (_autoWrap) UpdateAutoWrapWidth();

        // Build text if dirty (8)
        string text = _lastText;
        if (_textDirty || _ttfTex == null || _wrapWidthPx != _lastWrapWidth)
        {
            _sb.Clear();
            var budgetMs = 1000.0 / Math.Max(1.0, _targetFps);
            var overBudget = LastFrameMs > budgetMs + 0.001;

            // Header + performance
            _sb.Append("FPS:").Append(SmoothedFps.ToString("F1").PadLeft(5));
            _sb.Append("  Frame:").Append(LastFrameMs.ToString("F2").PadLeft(6)).Append("ms");
            _sb.Append(" (Budget:").Append(budgetMs.ToString("F2")).Append("ms @").Append(_targetFps.ToString("F0")).Append("fps");
            if (overBudget) _sb.Append(" !");
            _sb.AppendLine(")");

            if (_fpsHistCount > 0)
            {
                float min = float.MaxValue, max = float.MinValue, sum = 0f;
                for (int i = 0; i < _fpsHistCount; i++)
                {
                    var v = _fpsHist[i];
                    if (v < min) min = v;
                    if (v > max) max = v;
                    sum += v;
                }
                var avg = sum / _fpsHistCount;
                _sb.Append("FPS(min/avg/max): ").Append(min.ToString("F1")).Append('/')
                  .Append(avg.ToString("F1")).Append('/').AppendLine(max.ToString("F1"));
            }

            _sb.Append("Sprites: Items=").Append(LastSpriteItems)
              .Append("  Batches=").Append(LastSpriteBatches)
              .Append("  DrawCalls=").AppendLine(LastSpriteDrawCalls.ToString());

            var managedBytes = GC.GetTotalMemory(false);
            var gpuBytes = _host.LiveGpuTextureBytes;
            _sb.Append("Resources: Live=").Append(_host.LiveTrackedResources)
              .Append(" Total=").Append(_host.TotalTrackedResources)
              .Append("  Managed=").Append(FormatBytes(managedBytes))
              .Append("  GPU Tex≈").AppendLine(FormatBytes(gpuBytes));

            _sb.Append("Window: ").Append(_host.Width).Append('x').Append(_host.Height)
              .Append("  sRGB:").Append(_host.BackbufferIsSRGB)
              .Append("  Backend:").AppendLine(_host.ShaderFormat.ToString());

            text = _sb.ToString();
            _textDirty = false;
        }

        EnsureSolidTexture();
        EnsureTtfTexture(text);
        if (_ttfTex == null) return;

        _sprites.Begin();

        // Scaled layout
        int padL = Scaled(_padLeft);
        int padR = Scaled(_padRight);
        int padT = Scaled(_padTop);
        int padB = Scaled(_padBottom);
        int border = Scaled(_borderPx);
        int margin = Scaled(_outerMarginPx);

        var innerW = _ttfTexW + padL + padR;
        var innerH = _ttfTexH + padT + padB;
        var borderW = innerW + border * 2;
        var borderH = innerH + border * 2;

        int borderX = margin;
        int borderY = margin;
        switch (_corner)
        {
            case OverlayCorner.TopRight:
                borderX = _host.Width - borderW - margin;
                break;
            case OverlayCorner.BottomLeft:
                borderY = _host.Height - borderH - margin;
                break;
            case OverlayCorner.BottomRight:
                borderX = _host.Width - borderW - margin;
                borderY = _host.Height - borderH - margin;
                break;
        }

        var panelX = borderX + border;
        var panelY = borderY + border;

        if (_showBorder)
            _sprites.Draw(_solidTex!, null, new Rectangle(borderX, borderY, borderW, borderH), _borderColor);
        if (_showPanel)
            _sprites.Draw(_solidTex!, null, new Rectangle(panelX, panelY, innerW, innerH), _panelColor);

        // Severity-based text color (3)
        var textCol = _autoFpsColoring ? SeverityColor(LastFrameMs, _targetFps, _textColor) : _textColor;

        // Text with 1px shadow
        var tx = panelX + padL;
        var ty = panelY + padT;
        _sprites.Draw(_ttfTex!, null, new Rectangle(tx + 1, ty + 1, _ttfTexW, _ttfTexH), new Color(0, 0, 0, 128));
        _sprites.Draw(_ttfTex!, null, new Rectangle(tx, ty, _ttfTexW, _ttfTexH), textCol);

        _sprites.End();
    }

    private static Color SeverityColor(double frameMs, double targetFps, Color baseColor)
    {
        var budget = 1000.0 / Math.Max(1.0, targetFps);
        if (frameMs > budget * 1.25) return new Color(235, 64, 52);      // red
        if (frameMs > budget * 1.05) return new Color(255, 208, 0);      // yellow
        return baseColor;                                                // normal
    }

    private static string FormatBytes(long bytes)
    {
        const long KB = 1024, MB = KB * 1024, GB = MB * 1024;
        if (bytes >= GB) return (bytes / (double)GB).ToString("F2") + " GB";
        if (bytes >= MB) return (bytes / (double)MB).ToString("F2") + " MB";
        if (bytes >= KB) return (bytes / (double)KB).ToString("F2") + " KB";
        return bytes + " B";
    }

    private void EnsureSolidTexture()
    {
        if (_solidTex != null) return;

        SDL_GPUTextureCreateInfo ci = default;
        ci.type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D;
        ci.format = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;
        ci.width = 1;
        ci.height = 1;
        ci.layer_count_or_depth = 1;
        ci.num_levels = 1;
        ci.sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1;
        ci.usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER;

        var tex = SDL_CreateGPUTexture(_host.Device, &ci);
        if (tex == null)
            throw new InvalidOperationException($"Solid texture creation failed: {SDL_GetError()}");

        byte[] pixel = { 255, 255, 255, 255 };

        SDL_GPUTransferBufferCreateInfo tci = default;
        tci.usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD;
        tci.size = 4;
        var tbuf = SDL_CreateGPUTransferBuffer(_host.Device, &tci);
        if (tbuf == null)
            throw new InvalidOperationException("Solid transfer buffer failed.");

        try
        {
            var mapped = SDL_MapGPUTransferBuffer(_host.Device, tbuf, false);
            if (mapped == nint.Zero) throw new InvalidOperationException("Map solid transfer buffer failed.");
            fixed (byte* src = pixel)
            {
                Buffer.MemoryCopy(src, (void*)mapped, 4, 4);
            }
            SDL_UnmapGPUTransferBuffer(_host.Device, tbuf);

            var cmd = SDL_AcquireGPUCommandBuffer(_host.Device);
            if (cmd == null) throw new InvalidOperationException("Acquire command buffer (solid) failed.");
            var copy = SDL_BeginGPUCopyPass(cmd);

            SDL_GPUTextureTransferInfo srcInfo = default;
            srcInfo.transfer_buffer = tbuf;
            srcInfo.pixels_per_row = 1;
            srcInfo.rows_per_layer = 1;

            SDL_GPUTextureRegion dstRegion = default;
            dstRegion.texture = tex;
            dstRegion.w = 1;
            dstRegion.h = 1;
            dstRegion.d = 1;

            SDL_UploadToGPUTexture(copy, &srcInfo, &dstRegion, false);
            SDL_EndGPUCopyPass(copy);
            SDL_SubmitGPUCommandBuffer(cmd);
        }
        finally
        {
            SDL_ReleaseGPUTransferBuffer(_host.Device, tbuf);
        }

        _solidTex = new SdlTexture2D(_host.Device, tex, 1, 1, TextureFormat.R8G8B8A8_UNorm, "DebugSolid");
    }

    private void EnsureTtfUploadBuffer(uint neededBytes)
    {
        if (_ttfTbuf != null && _ttfTbufSize >= neededBytes) return;

        if (_ttfTbuf != null)
        {
            SDL_ReleaseGPUTransferBuffer(_host.Device, _ttfTbuf);
            _ttfTbuf = null;
            _ttfTbufSize = 0;
        }

        SDL_GPUTransferBufferCreateInfo tci = default;
        tci.usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD;
        tci.size = neededBytes;
        _ttfTbuf = SDL_CreateGPUTransferBuffer(_host.Device, &tci);
        if (_ttfTbuf == null)
            throw new InvalidOperationException("Create text transfer buffer failed.");

        _ttfTbufSize = tci.size;
    }

    private void MaybeShrinkTtfUploadBuffer(uint currentBytes)
    {
        const uint ShrinkThreshold = 1024 * 1024; // 1 MB
        if (_ttfTbuf != null && _ttfTbufSize > currentBytes + ShrinkThreshold)
        {
            SDL_ReleaseGPUTransferBuffer(_host.Device, _ttfTbuf);
            _ttfTbuf = null;
            _ttfTbufSize = 0;
            EnsureTtfUploadBuffer(currentBytes);
        }
    }

    private bool FontIsUsable()
    {
        if (_ttfFont == null) return false;
        var h = TTF_GetFontHeight(_ttfFont);
        if (h > 0) return true;

        // Height invalid; mark unusable
        return false;
    }

    private void HandleInvalidFont()
    {
        if (_fontPermanentlyInvalid) return;

        Debug.WriteLine("[Overlay] Font pointer became invalid. Attempting fallback font.");
        // Try embedded fallback if we don't already own one
        if (!_ownsFont)
        {
            // Do not close external font here; just try to get an owned fallback
            if (EnsureEmbeddedFallbackFont(14) && FontIsUsable())
            {
                Debug.WriteLine("[Overlay] Fallback embedded font restored overlay.");
                _textDirty = true;
                return;
            }
        }
        else
        {
            // Owned font became invalid unexpectedly (likely after TTF_Quit or memory issue)
            // Attempt to reopen embedded fallback
            if (EnsureEmbeddedFallbackFont(_ttfPtSize <= 0 ? 14 : _ttfPtSize) && FontIsUsable())
            {
                Debug.WriteLine("[Overlay] Re-opened embedded fallback font.");
                _textDirty = true;
                return;
            }
        }

        Debug.WriteLine("[Overlay] Unable to recover font. Disabling further text rebuilds to prevent crash.");
        _fontPermanentlyInvalid = true;
    }

    private void EnsureTtfTexture(string text)
    {
        if (_fontPermanentlyInvalid) return;
        if (_ttfFont == null) return;
        Debug.Assert(Environment.CurrentManagedThreadId == _mainThreadId, "[Overlay] EnsureTtfTexture must run on main thread.");

        // Clamp wrap width defensively
        if (_wrapWidthPx > 8192) _wrapWidthPx = 8192;

        bool needsRebuild = _ttfTex == null
                            || !string.Equals(text, _lastText, StringComparison.Ordinal)
                            || _wrapWidthPx != _lastWrapWidth;
        if (!needsRebuild) return;

        if (!FontIsUsable())
        {
            HandleInvalidFont();
            if (!FontIsUsable()) return;
        }

        _lastText = text;
        _lastWrapWidth = _wrapWidthPx;

        SDL_Color white;
        white.r = 255; white.g = 255; white.b = 255; white.a = 255;

        if (_fontPermanentlyInvalid) return;

        // SDL3_ttf “Text” APIs take UTF-8 bytes + explicit length; no need to NUL-terminate.
        var bytes = Encoding.UTF8.GetBytes(text);

        SDL_Surface* rawSurf;
        fixed (byte* pText = bytes)
        {
            rawSurf = _wrapWidthPx > 0
                ? TTF_RenderText_Blended_Wrapped(_ttfFont, pText, (nuint)bytes.Length, white, _wrapWidthPx)
                : TTF_RenderText_Blended(_ttfFont, pText, (nuint)bytes.Length, white);
        }

        if (rawSurf == null)
        {
            Debug.WriteLine($"[Overlay] TTF render failed: {SDL_GetError()}");
            return;
        }

        try
        {
            var converted = SDL_ConvertSurface(rawSurf, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
            if (converted == null)
            {
                Debug.WriteLine($"[Overlay] SDL_ConvertSurface failed: {SDL_GetError()}");
                return;
            }

            try
            {
                int w = converted->w;
                int h = converted->h;
                int pitch = converted->pitch;
                var srcPixels = (byte*)converted->pixels;

                if (_ttfTex != null && (w != _ttfTexW || h != _ttfTexH))
                {
                    _ttfTex.Dispose();
                    _ttfTex = null;
                }

                if (_ttfTex == null)
                {
                    SDL_GPUTextureCreateInfo ci = default;
                    ci.type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D;
                    ci.format = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;
                    ci.width = (uint)w;
                    ci.height = (uint)h;
                    ci.layer_count_or_depth = 1;
                    ci.num_levels = 1;
                    ci.sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1;
                    ci.usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER;

                    var tex = SDL_CreateGPUTexture(_host.Device, &ci);
                    if (tex == null)
                    {
                        Debug.WriteLine($"[Overlay] Create text texture failed: {SDL_GetError()}");
                        return;
                    }

                    _ttfTex = new SdlTexture2D(_host.Device, tex, w, h, TextureFormat.R8G8B8A8_UNorm, $"DebugTTF {w}x{h}@{_ttfPtSize}");
                    _ttfTexW = w;
                    _ttfTexH = h;
                }

                var totalBytes = (uint)(w * h * 4);
                EnsureTtfUploadBuffer(totalBytes);

                var mapped = SDL_MapGPUTransferBuffer(_host.Device, _ttfTbuf, false);
                if (mapped == nint.Zero) return;

                var dst = (byte*)mapped;
                for (int row = 0; row < h; row++)
                {
                    Buffer.MemoryCopy(srcPixels + row * pitch, dst + row * (w * 4), w * 4, w * 4);
                }
                SDL_UnmapGPUTransferBuffer(_host.Device, _ttfTbuf);

                var cmd = SDL_AcquireGPUCommandBuffer(_host.Device);
                if (cmd == null) return;
                var copy = SDL_BeginGPUCopyPass(cmd);

                SDL_GPUTextureTransferInfo srcInfo = default;
                srcInfo.transfer_buffer = _ttfTbuf;
                srcInfo.pixels_per_row = (uint)w;
                srcInfo.rows_per_layer = (uint)h;

                SDL_GPUTextureRegion dstRegion = default;
                dstRegion.texture = _ttfTex!.Texture;
                dstRegion.w = (uint)w;
                dstRegion.h = (uint)h;
                dstRegion.d = 1;

                SDL_UploadToGPUTexture(copy, &srcInfo, &dstRegion, false);
                SDL_EndGPUCopyPass(copy);
                SDL_SubmitGPUCommandBuffer(cmd);

                // After a successful upload, consider shrinking an oversized upload buffer
                MaybeShrinkTtfUploadBuffer((uint)(w * h * 4));
            }
            finally
            {
                SDL_DestroySurface(converted);
            }
        }
        finally
        {
            SDL_DestroySurface(rawSurf);
        }
    }

    private int Scaled(int logical) => (int)MathF.Round(logical * _uiScale);

    public void Dispose()
    {
        if (_ttfTex != null)
        {
            _ttfTex.Dispose();
            _ttfTex = null;
        }
        if (_ttfTbuf != null)
        {
            SDL_ReleaseGPUTransferBuffer(_host.Device, _ttfTbuf);
            _ttfTbuf = null;
            _ttfTbufSize = 0;
        }
        // Only close font if we own it
        if (_ownsFont && _ttfFont != null)
        {
            TTF_CloseFont(_ttfFont);
            _ttfFont = null;
        }
        _sharedFontRef = null; // release external reference
        if (_solidTex != null)
        {
            _solidTex.Dispose();
            _solidTex = null;
        }
    }
}