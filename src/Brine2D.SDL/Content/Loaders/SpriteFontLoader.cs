using System;
using System.Collections.Generic;
using System.IO;
using Brine2D.Core.Content;
using Brine2D.Core.Graphics.Text;
using Brine2D.SDL.Graphics.Text;
using Brine2D.SDL.Hosting;

namespace Brine2D.SDL.Content.Loaders;

/// <summary>
/// Loader for .spritefont descriptor files.
/// Descriptor filename pattern:
///   Family[@Size].spritefont
///   FamilyA+FamilyB+FamilyC@Size.spritefont
/// Optional missing glyph sentinel (hex codepoint) can follow with '$':
///   FamilyA+Emoji@16$FFFD.spritefont  (primary sentinel U+FFFD)
/// If '$' omitted, defaults: primary '?' secondary U+FFFD.
/// </summary>
internal sealed class SpriteFontLoader : AssetLoader<IFont>
{
    private readonly SdlHost _host;

    public SpriteFontLoader(SdlHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public override bool CanLoad(string path) =>
        path.EndsWith(".spritefont", StringComparison.OrdinalIgnoreCase);

    public override IFont LoadTyped(ContentLoadContext context, string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        // Extract sentinel if present
        int sentinelIdx = fileName.IndexOf('$');
        string sentinelHex = string.Empty;
        if (sentinelIdx >= 0)
        {
            sentinelHex = fileName[(sentinelIdx + 1)..];
            fileName = fileName[..sentinelIdx];
        }

        var atIdx = fileName.LastIndexOf('@');
        if (atIdx < 0)
            throw new InvalidOperationException("SpriteFontLoader: missing @size spec.");

        int pt = 16;
        var sizeStr = fileName[(atIdx + 1)..];
        if (!int.TryParse(sizeStr, out pt) || pt <= 0)
            pt = 16;

        var familyList = fileName[..atIdx];
        var families = familyList.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (families.Length == 0)
            throw new InvalidOperationException("SpriteFontLoader: no font family specified.");

        uint? customMissingPrimary = ParseHexCodepoint(sentinelHex);
        var baseDir = Path.GetDirectoryName(path) ?? string.Empty;

        var spriteFonts = new List<SpriteFont>(families.Length);
        foreach (var fam in families)
        {
            // Probe extensions and naming patterns:
            // 1) Family.ext@pt
            // 2) Family@pt.ext
            var exts = new[] { ".ttf", ".otf", ".ttc" };
            TtfFont? ttf = null;
            List<string> tried = new();

            foreach (var ext in exts)
            {
                var p1 = Path.Combine(baseDir, fam + ext + "@" + pt);
                var p2 = Path.Combine(baseDir, fam + "@" + pt + ext);
                try
                {
                    ttf = _host.Content.Load<TtfFont>(p1);
                    break;
                }
                catch { tried.Add(p1); }
                try
                {
                    ttf = _host.Content.Load<TtfFont>(p2);
                    break;
                }
                catch { tried.Add(p2); }
            }

            if (ttf == null)
                throw new FileNotFoundException($"SpriteFontLoader: could not resolve font for family '{fam}' at size {pt}. Tried:\n - " + string.Join("\n - ", tried));

            var sf = new SpriteFont(_host, ttf, pt, $"{fam}@{pt}");
            if (customMissingPrimary.HasValue)
                sf.SetMissingGlyphPrimary(customMissingPrimary.Value);
            spriteFonts.Add(sf);
        }

        if (spriteFonts.Count == 1)
            return spriteFonts[0];

        return new CompositeFont(spriteFonts, Path.GetFileName(path));
    }

    public override ValueTask<IFont> LoadTypedAsync(ContentLoadContext context, string path, System.Threading.CancellationToken ct)
        => ValueTask.FromResult(LoadTyped(context, path));

    private static uint? ParseHexCodepoint(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        hex = hex.Trim();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex[2..];
        else if (hex.StartsWith("U+", StringComparison.OrdinalIgnoreCase))
            hex = hex[2..];

        if (uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var cp))
            return cp;
        return null;
    }
}