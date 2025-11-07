using System.Text;
using Brine2D.Core.Content;
using Brine2D.Core.Graphics;
using Brine2D.SDL.Graphics;
using Brine2D.SDL.Hosting;
using SDL;
using static SDL.SDL3;
using static SDL.SDL3_image;

namespace Brine2D.SDL.Content.Loaders;

/// <summary>
///     SDL_image-backed texture loader that produces <see cref="ITexture2D" /> from common image formats.
/// </summary>
/// <remarks>
///     <para>Supported formats (by extension): .png, .jpg/.jpeg, .bmp, .tga, .gif, .webp.</para>
///     <para>Path suffix heuristics (opt-in flags embedded in file name):</para>
///     <list type="bullet">
///         <item>
///             <description><c>.r8.</c> → load as <see cref="TextureFormat.R8_UNorm" /> (single-channel data).</description>
///         </item>
///         <item>
///             <description><c>.rg8.</c> → load as <see cref="TextureFormat.R8G8_UNorm" /> (two-channel data).</description>
///         </item>
///         <item>
///             <description><c>.lin.</c> → treat as linear RGBA8 (no sRGB decode).</description>
///         </item>
///         <item>
///             <description>
///                 <c>.pma.</c>/<c>.nopma.</c> → premultiply alpha in linear space (default on for color), or opt
///                 out.
///             </description>
///         </item>
///         <item>
///             <description><c>.mip.</c>/<c>.nomip.</c> → force enable/disable mipmap generation.</description>
///         </item>
///     </list>
///     <para>Color management and sRGB:</para>
///     <list type="bullet">
///         <item>
///             <description>Color textures default to sRGB storage when supported, with linear fallback when not.</description>
///         </item>
///         <item>
///             <description>Premultiply (when enabled) is done in linear space before encoding to storage (sRGB or UNORM).</description>
///         </item>
///     </list>
///     <para>Notes:</para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Currently resolves a physical path and uses <c>IMG_Load</c>. For non-file providers, consider
///                 extending to <c>IMG_Load_RW</c> via <see cref="ContentLoadContext.OpenRead(string)" />.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see
///                     cref="LoadTypedAsync(Brine2D.Core.Content.ContentLoadContext,string,System.Threading.CancellationToken)" />
///                 wraps the synchronous path.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///     // Registration
///     var host = new SdlHost();
///     var content = host.Content;
///     content.AddLoader(new SdlTextureLoader(host));
/// 
///     // Load color texture (sRGB, mips on by default)
///     var tex = content.Load&lt;ITexture2D&gt;("images/player.png");
/// 
///     // Load linear data texture with RG channels, no mips
///     var lut = content.Load&lt;ITexture2D&gt;("images/noise.rg8.lin.nomip.png");
///     </code>
/// </example>
public sealed unsafe class SdlTextureLoader : AssetLoader<ITexture2D>
{
    private readonly SdlHost _host;

    /// <summary>
    ///     Creates a new SDL texture loader bound to the given host/device.
    /// </summary>
    public SdlTextureLoader(SdlHost host)
    {
        _host = host;
    }

    /// <summary>
    ///     Returns true when the path looks like a supported image file (by extension).
    /// </summary>
    public override bool CanLoad(string path)
    {
        return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Loads an image file into GPU texture memory using SDL_image and SDL GPU APIs.
    /// </summary>
    /// <remarks>
    ///     Attempts to use sRGB storage for color textures and falls back to linear UNORM when unsupported.
    ///     Premultiplication (when enabled) is performed in linear space before encoding.
    /// </remarks>
    /// <exception cref="FileNotFoundException">If the resolved path cannot be found.</exception>
    /// <exception cref="InvalidOperationException">On SDL_image or GPU resource creation failures.</exception>
    public override ITexture2D LoadTyped(ContentLoadContext context, string path)
    {
        var resolved = context.ResolvePath(path)
                       ?? throw new FileNotFoundException($"Texture not found: {path}");

        // Heuristic: suffix-based intent for data textures and alpha handling
        // *.r8.*    -> R8_UNORM
        // *.rg8.*   -> R8G8_UNORM
        // *.lin.*   -> force RGBA8_UNORM (linear)
        // *.pma.*   -> premultiply alpha (do this in linear space)
        // *.mip.*   -> force mipmaps on
        // *.nomip.* -> force mipmaps off
        var wantR8 = resolved.Contains(".r8.", StringComparison.OrdinalIgnoreCase);
        var wantRG8 = resolved.Contains(".rg8.", StringComparison.OrdinalIgnoreCase);
        var wantLIN = resolved.Contains(".lin.", StringComparison.OrdinalIgnoreCase);
        var optOutPMA = resolved.Contains(".nopma.", StringComparison.OrdinalIgnoreCase);
        var forceMip = resolved.Contains(".mip.", StringComparison.OrdinalIgnoreCase);
        var forceNoMip = resolved.Contains(".nomip.", StringComparison.OrdinalIgnoreCase);

        // Default: color assets => premultiply (unless opted out);
        // data/linear => no premultiply
        var wantPMA = !wantR8 && !wantRG8 && !wantLIN && !optOutPMA;

        SDL_Surface* surface;
        var utf8 = Encoding.UTF8.GetBytes(resolved + "\0");
        fixed (byte* p = utf8)
        {
            surface = IMG_Load(p);
        }

        if (surface == null)
        {
            throw new InvalidOperationException($"IMG_Load failed for '{resolved}': {SDL_GetError()}");
        }

        // Normalize to RGBA8888 to have predictable component order
        // SDL_PIXELFORMAT_RGBA8888 is ABGR in memory on little-endian,
        // so use ABGR8888 to get RGBA byte order (r,g,b,a) in memory.
        var rgba = SDL_ConvertSurface(surface, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
        SDL_DestroySurface(surface);
        if (rgba == null)
        {
            throw new InvalidOperationException($"SDL_ConvertSurface failed for '{resolved}': {SDL_GetError()}");
        }

        try
        {
            var width = rgba->w;
            var height = rgba->h;

            // Pick GPU texture format
            SDL_GPUTextureFormat gpuFmt;
            TextureFormat engineFmt;

            if (wantR8)
            {
                gpuFmt = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UNORM;
                engineFmt = TextureFormat.R8_UNorm;
            }
            else if (wantRG8)
            {
                gpuFmt = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UNORM;
                engineFmt = TextureFormat.R8G8_UNorm;
            }
            else if (wantLIN)
            {
                gpuFmt = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;
                engineFmt = TextureFormat.R8G8B8A8_UNorm;
            }
            else
            {
                // Default for color: prefer sRGB, fallback to linear
                gpuFmt = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB;
                engineFmt = TextureFormat.R8G8B8A8_UNorm_sRGB;
            }

            var requestedColorSRGB = !wantR8 && !wantRG8 && !wantLIN;

            // Mip decisions
            var defaultMips = requestedColorSRGB; // color textures get mips by default
            if (wantR8 || wantRG8)
            {
                defaultMips = false; // data textures off by default
            }

            var wantMips = forceMip ? true : forceNoMip ? false : defaultMips;
            var mipCount = wantMips ? ComputeMipCount(width, height) : 1;

            SDL_GPUTextureCreateInfo ci = default;
            ci.type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D;
            ci.format = gpuFmt;
            ci.width = (uint)width;
            ci.height = (uint)height;
            ci.layer_count_or_depth = 1;
            ci.num_levels = (uint)mipCount;
            ci.sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1;
            ci.usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER;

            var tex = SDL_CreateGPUTexture(_host.Device, &ci);
            if (tex == null && gpuFmt == SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB)
            {
                // sRGB not supported: fallback to UNORM
                gpuFmt = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;
                engineFmt = TextureFormat.R8G8B8A8_UNorm;
                ci.format = gpuFmt;
                tex = SDL_CreateGPUTexture(_host.Device, &ci);
            }

            if (tex == null)
            {
                throw new InvalidOperationException($"SDL_CreateGPUTexture failed: {SDL_GetError()}");
            }

            // Upload via transfer buffer, repacking rows if needed
            var dstBpp = gpuFmt switch
            {
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UNORM => 1,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UNORM => 2,
                _ => 4
            };

            // Build a CPU buffer for level 0, then generate mip levels if needed
            var levelData = new byte[width * height * dstBpp];

            var srcBase = (byte*)rgba->pixels;
            var srcPitch = (nuint)rgba->pitch;

            var targetIsSRGB = gpuFmt == SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB;
            var sRGBFallbackUsed = requestedColorSRGB && !targetIsSRGB;

            // Fill level 0
            if (dstBpp == 4)
            {
                for (var y = 0; y < height; y++)
                {
                    var srcRow = srcBase + (nuint)y * srcPitch;
                    var dstRowOff = y * width * 4;

                    if (!sRGBFallbackUsed && !wantPMA)
                    {
                        // Straight copy per row to strip pitch
                        for (var x = 0; x < width; x++)
                        {
                            var s = x * 4;
                            levelData[dstRowOff + s + 0] = srcRow[s + 0];
                            levelData[dstRowOff + s + 1] = srcRow[s + 1];
                            levelData[dstRowOff + s + 2] = srcRow[s + 2];
                            levelData[dstRowOff + s + 3] = srcRow[s + 3];
                        }
                    }
                    else
                    {
                        for (var x = 0; x < width; x++)
                        {
                            var s = x * 4;
                            var r8 = srcRow[s + 0];
                            var g8 = srcRow[s + 1];
                            var b8 = srcRow[s + 2];
                            var a8 = srcRow[s + 3];

                            float rLin, gLin, bLin;
                            if (requestedColorSRGB) // asset intended as color in sRGB
                            {
                                rLin = SrgbToLinear(r8 / 255f);
                                gLin = SrgbToLinear(g8 / 255f);
                                bLin = SrgbToLinear(b8 / 255f);
                            }
                            else
                            {
                                rLin = r8 / 255f;
                                gLin = g8 / 255f;
                                bLin = b8 / 255f;
                            }

                            var aLin = a8 / 255f;

                            if (wantPMA)
                            {
                                rLin *= aLin;
                                gLin *= aLin;
                                bLin *= aLin;
                            }

                            if (targetIsSRGB)
                            {
                                levelData[dstRowOff + s + 0] = ToSrgb8(rLin);
                                levelData[dstRowOff + s + 1] = ToSrgb8(gLin);
                                levelData[dstRowOff + s + 2] = ToSrgb8(bLin);
                                levelData[dstRowOff + s + 3] = a8;
                            }
                            else
                            {
                                levelData[dstRowOff + s + 0] = (byte)(Math.Clamp(rLin, 0f, 1f) * 255f + 0.5f);
                                levelData[dstRowOff + s + 1] = (byte)(Math.Clamp(gLin, 0f, 1f) * 255f + 0.5f);
                                levelData[dstRowOff + s + 2] = (byte)(Math.Clamp(bLin, 0f, 1f) * 255f + 0.5f);
                                levelData[dstRowOff + s + 3] = a8;
                            }
                        }
                    }
                }
            }
            else if (dstBpp == 2)
            {
                for (var y = 0; y < height; y++)
                {
                    var srcRow = srcBase + (nuint)y * srcPitch;
                    var dstRowOff = y * width * 2;
                    for (var x = 0; x < width; x++)
                    {
                        var s = x * 4;
                        levelData[dstRowOff + x * 2 + 0] = srcRow[s + 0];
                        levelData[dstRowOff + x * 2 + 1] = srcRow[s + 1];
                    }
                }
            }
            else // dstBpp == 1
            {
                for (var y = 0; y < height; y++)
                {
                    var srcRow = srcBase + (nuint)y * srcPitch;
                    var dstRowOff = y * width;
                    for (var x = 0; x < width; x++)
                    {
                        var s = x * 4;
                        levelData[dstRowOff + x] = srcRow[s + 0];
                    }
                }
            }

            // Create a transfer buffer sized for the largest level (level 0)
            var sliceBytesL0 = (nuint)(width * height * dstBpp);
            SDL_GPUTransferBufferCreateInfo tci = default;
            tci.usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD;
            tci.size = (uint)sliceBytesL0;

            var tbuf = SDL_CreateGPUTransferBuffer(_host.Device, &tci);
            if (tbuf == null)
            {
                SDL_ReleaseGPUTexture(_host.Device, tex);
                throw new InvalidOperationException($"SDL_CreateGPUTransferBuffer failed: {SDL_GetError()}");
            }

            try
            {
                var curW = width;
                var curH = height;
                for (var mip = 0; mip < mipCount; mip++)
                {
                    var levelBytes = (nuint)(curW * curH * dstBpp);

                    var mapped = SDL_MapGPUTransferBuffer(_host.Device, tbuf, false);
                    if (mapped == nint.Zero)
                    {
                        throw new InvalidOperationException($"SDL_MapGPUTransferBuffer failed: {SDL_GetError()}");
                    }

                    fixed (byte* src = levelData)
                    {
                        Buffer.MemoryCopy(src, (void*)mapped, tci.size, levelBytes);
                    }

                    SDL_UnmapGPUTransferBuffer(_host.Device, tbuf);

                    var cmd = SDL_AcquireGPUCommandBuffer(_host.Device);
                    if (cmd == null)
                    {
                        throw new InvalidOperationException("SDL_AcquireGPUCommandBuffer returned null.");
                    }

                    var copy = SDL_BeginGPUCopyPass(cmd);

                    SDL_GPUTextureTransferInfo srcInfo = default;
                    srcInfo.transfer_buffer = tbuf;
                    srcInfo.offset = 0;
                    srcInfo.pixels_per_row = (uint)curW;
                    srcInfo.rows_per_layer = (uint)curH;

                    SDL_GPUTextureRegion dst = default;
                    dst.texture = tex;
                    dst.mip_level = (uint)mip;
                    dst.layer = 0;
                    dst.x = 0;
                    dst.y = 0;
                    dst.z = 0;
                    dst.w = (uint)curW;
                    dst.h = (uint)curH;
                    dst.d = 1;

                    SDL_UploadToGPUTexture(copy, &srcInfo, &dst, false);

                    SDL_EndGPUCopyPass(copy);
                    SDL_SubmitGPUCommandBuffer(cmd);

                    // Build next mip level if any
                    if (mip + 1 < mipCount)
                    {
                        var nextW = Math.Max(1, curW >> 1);
                        var nextH = Math.Max(1, curH >> 1);
                        levelData = Downsample2xBox(levelData, curW, curH, nextW, nextH, dstBpp, targetIsSRGB);
                        curW = nextW;
                        curH = nextH;
                    }
                }
            }
            finally
            {
                SDL_ReleaseGPUTransferBuffer(_host.Device, tbuf);
            }

            return new SdlTexture2D(_host.Device, tex, width, height, engineFmt);
        }
        finally
        {
            SDL_DestroySurface(rgba);
        }
    }

    /// <summary>
    ///     Async wrapper over the synchronous load path.
    /// </summary>
    public override ValueTask<ITexture2D> LoadTypedAsync(ContentLoadContext context, string path, CancellationToken ct)
    {
        return ValueTask.FromResult(LoadTyped(context, path));
    }

    /// <summary>
    ///     Computes the number of mip levels for a 2D texture.
    /// </summary>
    private static int ComputeMipCount(int w, int h)
    {
        var m = Math.Max(w, h);
        var count = 1;
        while (m > 1)
        {
            m >>= 1;
            count++;
        }

        return count;
    }

    /// <summary>
    ///     2× box filter downsample from source level to next mip level.
    /// </summary>
    private static byte[] Downsample2xBox(byte[] src, int srcW, int srcH, int dstW, int dstH, int bpp,
        bool storedAsSRGB)
    {
        var dst = new byte[dstW * dstH * bpp];

        if (bpp == 1)
        {
            for (var y = 0; y < dstH; y++)
            {
                var sy0 = Math.Min(srcH - 1, y * 2);
                var sy1 = Math.Min(srcH - 1, sy0 + 1);
                for (var x = 0; x < dstW; x++)
                {
                    var sx0 = Math.Min(srcW - 1, x * 2);
                    var sx1 = Math.Min(srcW - 1, sx0 + 1);

                    var i00 = sy0 * srcW + sx0;
                    var i10 = sy0 * srcW + sx1;
                    var i01 = sy1 * srcW + sx0;
                    var i11 = sy1 * srcW + sx1;

                    var v = (src[i00] + src[i10] + src[i01] + src[i11] + 2) >> 2;
                    dst[y * dstW + x] = (byte)v;
                }
            }

            return dst;
        }

        if (bpp == 2)
        {
            for (var y = 0; y < dstH; y++)
            {
                var sy0 = Math.Min(srcH - 1, y * 2);
                var sy1 = Math.Min(srcH - 1, sy0 + 1);
                for (var x = 0; x < dstW; x++)
                {
                    var sx0 = Math.Min(srcW - 1, x * 2);
                    var sx1 = Math.Min(srcW - 1, sx0 + 1);

                    var p00 = (sy0 * srcW + sx0) * 2;
                    var p10 = (sy0 * srcW + sx1) * 2;
                    var p01 = (sy1 * srcW + sx0) * 2;
                    var p11 = (sy1 * srcW + sx1) * 2;

                    var r = (src[p00 + 0] + src[p10 + 0] + src[p01 + 0] + src[p11 + 0] + 2) >> 2;
                    var g = (src[p00 + 1] + src[p10 + 1] + src[p01 + 1] + src[p11 + 1] + 2) >> 2;

                    var dp = (y * dstW + x) * 2;
                    dst[dp + 0] = (byte)r;
                    dst[dp + 1] = (byte)g;
                }
            }

            return dst;
        }

        // RGBA8
        for (var y = 0; y < dstH; y++)
        {
            var sy0 = Math.Min(srcH - 1, y * 2);
            var sy1 = Math.Min(srcH - 1, sy0 + 1);
            for (var x = 0; x < dstW; x++)
            {
                var sx0 = Math.Min(srcW - 1, x * 2);
                var sx1 = Math.Min(srcW - 1, sx0 + 1);

                var p00 = (sy0 * srcW + sx0) * 4;
                var p10 = (sy0 * srcW + sx1) * 4;
                var p01 = (sy1 * srcW + sx0) * 4;
                var p11 = (sy1 * srcW + sx1) * 4;

                // Load 4 samples
                float r0 = src[p00 + 0] / 255f,
                    g0 = src[p00 + 1] / 255f,
                    b0 = src[p00 + 2] / 255f,
                    a0 = src[p00 + 3] / 255f;
                float r1 = src[p10 + 0] / 255f,
                    g1 = src[p10 + 1] / 255f,
                    b1 = src[p10 + 2] / 255f,
                    a1 = src[p10 + 3] / 255f;
                float r2 = src[p01 + 0] / 255f,
                    g2 = src[p01 + 1] / 255f,
                    b2 = src[p01 + 2] / 255f,
                    a2 = src[p01 + 3] / 255f;
                float r3 = src[p11 + 0] / 255f,
                    g3 = src[p11 + 1] / 255f,
                    b3 = src[p11 + 2] / 255f,
                    a3 = src[p11 + 3] / 255f;

                // Decode from sRGB if stored as sRGB
                if (storedAsSRGB)
                {
                    r0 = SrgbToLinear(r0);
                    g0 = SrgbToLinear(g0);
                    b0 = SrgbToLinear(b0);
                    r1 = SrgbToLinear(r1);
                    g1 = SrgbToLinear(g1);
                    b1 = SrgbToLinear(b1);
                    r2 = SrgbToLinear(r2);
                    g2 = SrgbToLinear(g2);
                    b2 = SrgbToLinear(b2);
                    r3 = SrgbToLinear(r3);
                    g3 = SrgbToLinear(g3);
                    b3 = SrgbToLinear(b3);
                }

                // Straight box filter in linear space
                var r = (r0 + r1 + r2 + r3) * 0.25f;
                var g = (g0 + g1 + g2 + g3) * 0.25f;
                var b = (b0 + b1 + b2 + b3) * 0.25f;
                var a = (a0 + a1 + a2 + a3) * 0.25f;

                var dp = (y * dstW + x) * 4;
                if (storedAsSRGB)
                {
                    dst[dp + 0] = ToSrgb8(r);
                    dst[dp + 1] = ToSrgb8(g);
                    dst[dp + 2] = ToSrgb8(b);
                    dst[dp + 3] = (byte)(Math.Clamp(a, 0f, 1f) * 255f + 0.5f);
                }
                else
                {
                    dst[dp + 0] = (byte)(Math.Clamp(r, 0f, 1f) * 255f + 0.5f);
                    dst[dp + 1] = (byte)(Math.Clamp(g, 0f, 1f) * 255f + 0.5f);
                    dst[dp + 2] = (byte)(Math.Clamp(b, 0f, 1f) * 255f + 0.5f);
                    dst[dp + 3] = (byte)(Math.Clamp(a, 0f, 1f) * 255f + 0.5f);
                }
            }
        }

        return dst;
    }

    /// <summary>
    ///     Convert sRGB to linear (approximate IEC 61966-2-1).
    /// </summary>
    private static float SrgbToLinear(float c)
    {
        return c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4f);
    }

    /// <summary>
    ///     Encode a linear float [0,1] to 8-bit sRGB.
    /// </summary>
    private static byte ToSrgb8(float lin)
    {
        lin = Math.Clamp(lin, 0f, 1f);
        var s = lin <= 0.0031308f ? lin * 12.92f : 1.055f * (float)Math.Pow(lin, 1f / 2.4f) - 0.055f;
        var v = (int)(s * 255f + 0.5f);
        return (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
    }
}