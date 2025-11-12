using System;
using System.IO;
using Brine2D.Core.Content;
using SDL;
using static SDL.SDL3;
using static SDL.SDL3_ttf;

namespace Brine2D.SDL.Content.Loaders;

public sealed class TtfFontLoader : IAssetLoader
{
    public Type AssetType => typeof(TtfFont);

    // Fast, side-effect free check based on extension (supports size suffix "@<pt>")
    public bool CanLoad(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Strip optional "@size" suffix (e.g. "fonts/Mono.ttf@16")
        var atIndex = path.LastIndexOf('@');
        var core = atIndex > 0 ? path[..atIndex] : path;

        // Extract extension
        var ext = Path.GetExtension(core);
        if (string.IsNullOrEmpty(ext))
            return false;

        // SDL_ttf commonly supports TTF/OTF/TTC; allow these (case-insensitive)
        return ext.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".otf", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".ttc", StringComparison.OrdinalIgnoreCase);
    }

    // Expect logical paths like "fonts/JetBrainsMono-Regular.ttf@14"
    // Suffix "@<size>" defines point size. Default 14 if omitted.
    public object Load(ContentLoadContext context, string path)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

        Parse(path, out var fontPath, out var ptSize);

        using var stream = context.OpenRead(fontPath);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var data = ms.ToArray();

        unsafe
        {
            fixed (byte* pData = data)
            {
                var io = SDL_IOFromConstMem((nint)pData, (nuint)data.Length);
                if (io == null)
                    throw new InvalidOperationException($"SDL_IOFromConstMem failed: {SDL_GetError()}");

                var font = TTF_OpenFontIO(io, true, ptSize);
                
                if (font == null)
                    throw new InvalidOperationException($"TTF_OpenFontIO '{fontPath}' failed: {SDL_GetError()}");

                return new TtfFont(font, ptSize, fontPath);
            }
        }
    }

    public async ValueTask<object> LoadAsync(ContentLoadContext context, string path, CancellationToken ct)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

        Parse(path, out var fontPath, out var ptSize);

        await using var stream = context.OpenRead(fontPath);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
        var data = ms.ToArray();

        unsafe
        {
            fixed (byte* pData = data)
            {
                var io = SDL_IOFromConstMem((nint)pData, (nuint)data.Length);
                if (io == null)
                    throw new InvalidOperationException($"SDL_IOFromConstMem failed: {SDL_GetError()}");

                var font = TTF_OpenFontIO(io, true, ptSize);
                if (font == null)
                    throw new InvalidOperationException($"TTF_OpenFontIO '{fontPath}' failed: {SDL_GetError()}");

                return new TtfFont(font, ptSize, fontPath);
            }
        }
    }

    private static void Parse(string path, out string fontPath, out int ptSize)
    {
        ptSize = 14;
        fontPath = path;
        var at = path.LastIndexOf('@');
        if (at > 0 && at < path.Length - 1)
        {
            fontPath = path.Substring(0, at);
            if (int.TryParse(path.Substring(at + 1), out var size) && size > 0)
                ptSize = size;
        }
    }
}