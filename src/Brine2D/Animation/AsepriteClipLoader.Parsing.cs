using Brine2D.Core;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Brine2D.Animation;

public partial class AsepriteClipLoader
{
    private AnimationClip BuildClip(
        string name,
        List<AsepriteFrame> frames,
        int from,
        int to,
        AsepriteDirection direction,
        int repeatCount,
        ITexture? texture,
        string? texturePath,
        Dictionary<int, Dictionary<string, Rectangle>> hitBoxesPerFrame,
        Dictionary<int, (float x, float y)> pivotPerFrame,
        string? tagData,
        string? tagColor)
    {
        var mode = direction switch
        {
            AsepriteDirection.PingPong or AsepriteDirection.PingPongReverse => PlaybackMode.PingPong,
            _ => PlaybackMode.Loop
        };

        var clip = new AnimationClip(name)
        {
            PlaybackMode = mode,
            Texture = texture,
            TexturePath = texture == null ? texturePath : null,
            ClipTint = ParseAsepriteColor(tagColor),
        };

        if (direction == AsepriteDirection.PingPongReverse)
            clip.UserData = PingPongReverseTag;

        if (clip.UserData == null && !string.IsNullOrEmpty(tagData))
            clip.UserData = tagData;

        if (repeatCount > 0 && (mode == PlaybackMode.Loop || mode == PlaybackMode.PingPong))
            clip.RepeatCount = repeatCount;

        bool reverseOrder = direction == AsepriteDirection.Reverse;
        int start = reverseOrder ? to : from;
        int end = reverseOrder ? from : to;
        int step = reverseOrder ? -1 : 1;

        for (int i = start; reverseOrder ? i >= end : i <= end; i += step)
        {
            if (i < 0 || i >= frames.Count)
            {
                _logger?.LogWarning(
                    "BuildClip('{Name}'): frame index {Index} is out of range [0, {Max}] — skipped. " +
                    "The Aseprite tag may reference frames that were not exported.",
                    name, i, frames.Count - 1);
                continue;
            }

            var f = frames[i];
            var rect = new Rectangle(f.Frame.X, f.Frame.Y, f.Frame.W, f.Frame.H);
            float durationSeconds = f.Duration / 1000f;

            var spriteFrame = new SpriteFrame(rect, durationSeconds);

            if (!string.IsNullOrEmpty(f.Data))
                spriteFrame.UserData = f.Data;

            bool isTrimmed = f.SpriteSourceSize is { W: > 0, H: > 0 }
                && f.SourceSize is { W: > 0, H: > 0 }
                && (f.SpriteSourceSize.X != 0 || f.SpriteSourceSize.Y != 0
                    || f.SpriteSourceSize.W != f.SourceSize.W
                    || f.SpriteSourceSize.H != f.SourceSize.H);

            if (isTrimmed)
            {
                spriteFrame.DrawOffset = new System.Numerics.Vector2(
                    f.SpriteSourceSize!.X,
                    f.SpriteSourceSize!.Y);
            }

            if (hitBoxesPerFrame.TryGetValue(i, out var boxes))
            {
                foreach (var (boxName, boxRect) in boxes)
                {
                    spriteFrame.SetHitBox(boxName, boxRect);
                    if (string.Equals(boxName, HitBoxSliceName, StringComparison.Ordinal))
                        spriteFrame.HitBox = boxRect;
                }
            }

            if (pivotPerFrame.TryGetValue(i, out var pivot))
            {
                float normalizeW = f.SourceSize is { W: > 0 } ? f.SourceSize.W : rect.Width;
                float normalizeH = f.SourceSize is { H: > 0 } ? f.SourceSize.H : rect.Height;
                if (normalizeW > 0 && normalizeH > 0)
                {
                    spriteFrame.Origin = new System.Numerics.Vector2(
                        pivot.x / normalizeW,
                        pivot.y / normalizeH);
                }
            }

            clip.AddFrame(spriteFrame);
        }

        return clip;
    }

    private Dictionary<int, Dictionary<string, Rectangle>> BuildNamedHitBoxLookup(
        List<AsepriteSlice>? slices,
        int frameCount)
    {
        var result = new Dictionary<int, Dictionary<string, Rectangle>>();
        if (slices == null)
            return result;

        foreach (var slice in slices)
        {
            if (string.IsNullOrEmpty(slice.Name) || slice.Keys == null)
                continue;

            var sortedKeys = new List<AsepriteSliceKey>(slice.Keys);
            sortedKeys.Sort(static (a, b) => a.Frame.CompareTo(b.Frame));

            for (int k = 0; k < sortedKeys.Count; k++)
            {
                var key = sortedKeys[k];
                int nextKeyFrame = k + 1 < sortedKeys.Count ? sortedKeys[k + 1].Frame : frameCount;
                var bounds = key.Bounds;
                var rect = new Rectangle(bounds.X, bounds.Y, bounds.W, bounds.H);

                for (int f = key.Frame; f < nextKeyFrame; f++)
                {
                    if (!result.TryGetValue(f, out var frameBoxes))
                    {
                        frameBoxes = new Dictionary<string, Rectangle>(StringComparer.Ordinal);
                        result[f] = frameBoxes;
                    }
                    frameBoxes[slice.Name] = rect;
                }
            }
        }

        return result;
    }

    private Dictionary<int, (float x, float y)> BuildPivotLookup(
        List<AsepriteSlice>? slices,
        int frameCount)
    {
        var result = new Dictionary<int, (float x, float y)>();
        if (slices == null)
            return result;

        foreach (var slice in slices)
        {
            if (slice.Keys == null)
                continue;

            bool isPivotSlice = string.Equals(slice.Name, PivotSliceName, StringComparison.Ordinal);

            var sortedKeys = new List<AsepriteSliceKey>(slice.Keys);
            sortedKeys.Sort(static (a, b) => a.Frame.CompareTo(b.Frame));

            for (int k = 0; k < sortedKeys.Count; k++)
            {
                var key = sortedKeys[k];
                if (key.Pivot == null)
                    continue;

                int nextKeyFrame = k + 1 < sortedKeys.Count ? sortedKeys[k + 1].Frame : frameCount;

                for (int f = key.Frame; f < nextKeyFrame; f++)
                {
                    if (result.ContainsKey(f))
                    {
                        if (!isPivotSlice)
                        {
                            _logger?.LogWarning(
                                "Slice '{Slice}' has pivot data for frame {Frame} but '{PivotSlice}' already claimed it. " +
                                "Set PivotSliceName to change which slice drives Origin.",
                                slice.Name, f, PivotSliceName);
                        }
                        continue;
                    }

                    if (isPivotSlice || !result.ContainsKey(f))
                        result[f] = (key.Pivot.Value.x, key.Pivot.Value.y);
                }
            }
        }

        return result;
    }

    private static List<AsepriteFrame>? TryParseArrayFormat(string json, string sourceName)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("frames", out var framesEl))
                return null;
            if (framesEl.ValueKind != JsonValueKind.Array)
                return null;

            var result = new List<AsepriteFrame>();
            foreach (var el in framesEl.EnumerateArray())
            {
                var frame = ParseFrameElement(el);
                if (frame != null)
                    result.Add(frame);
            }
            return result.Count > 0 ? result : null;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Malformed JSON in Aseprite export '{sourceName}' (array format): {ex.Message}", ex);
        }
    }

    private static List<AsepriteFrame>? TryParseHashFormat(string json, string sourceName)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("frames", out var framesEl))
                return null;
            if (framesEl.ValueKind != JsonValueKind.Object)
                return null;

            var keyed = new List<(int Order, AsepriteFrame Frame)>();
            foreach (var prop in framesEl.EnumerateObject())
            {
                var frame = ParseFrameElement(prop.Value);
                if (frame == null) continue;
                keyed.Add((ExtractTrailingIndex(prop.Name), frame));
            }

            if (keyed.Count == 0)
                return null;

            keyed.Sort(static (a, b) => a.Order.CompareTo(b.Order));
            return keyed.ConvertAll(x => x.Frame);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Malformed JSON in Aseprite export '{sourceName}' (hash format): {ex.Message}", ex);
        }
    }

    private static AsepriteFrame? ParseFrameElement(JsonElement el)
    {
        if (!el.TryGetProperty("frame", out var frameRect)) return null;
        if (!el.TryGetProperty("duration", out var durationEl)) return null;

        string? frameData = null;
        if (el.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.String)
        {
            var raw = dataEl.GetString();
            if (!string.IsNullOrEmpty(raw))
                frameData = raw;
        }

        AsepriteRect? spriteSourceSize = null;
        if (el.TryGetProperty("spriteSourceSize", out var sss))
        {
            spriteSourceSize = new AsepriteRect
            {
                X = sss.TryGetProperty("x", out var sx) ? sx.GetInt32() : 0,
                Y = sss.TryGetProperty("y", out var sy) ? sy.GetInt32() : 0,
                W = sss.TryGetProperty("w", out var sw) ? sw.GetInt32() : 0,
                H = sss.TryGetProperty("h", out var sh) ? sh.GetInt32() : 0,
            };
        }

        AsepriteSize? sourceSize = null;
        if (el.TryGetProperty("sourceSize", out var ss))
        {
            sourceSize = new AsepriteSize
            {
                W = ss.TryGetProperty("w", out var ssw) ? ssw.GetInt32() : 0,
                H = ss.TryGetProperty("h", out var ssh) ? ssh.GetInt32() : 0,
            };
        }

        return new AsepriteFrame
        {
            Frame = new AsepriteRect
            {
                X = frameRect.TryGetProperty("x", out var x) ? x.GetInt32() : 0,
                Y = frameRect.TryGetProperty("y", out var y) ? y.GetInt32() : 0,
                W = frameRect.TryGetProperty("w", out var w) ? w.GetInt32() : 0,
                H = frameRect.TryGetProperty("h", out var h) ? h.GetInt32() : 0,
            },
            Duration = durationEl.GetInt32(),
            Data = frameData,
            SpriteSourceSize = spriteSourceSize,
            SourceSize = sourceSize,
        };
    }

    private AsepriteMeta? TryParseMeta(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("meta", out var metaEl))
                return null;

            var meta = new AsepriteMeta();

            if (metaEl.TryGetProperty("frameTags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
            {
                meta.FrameTags = new List<AsepriteFrameTag>();
                foreach (var tag in tagsEl.EnumerateArray())
                {
                    string? tagData = null;
                    if (tag.TryGetProperty("data", out var tagDataEl) && tagDataEl.ValueKind == JsonValueKind.String)
                    {
                        var raw = tagDataEl.GetString();
                        if (!string.IsNullOrEmpty(raw))
                            tagData = raw;
                    }

                    string? tagColor = null;
                    if (tag.TryGetProperty("color", out var tagColorEl) && tagColorEl.ValueKind == JsonValueKind.String)
                        tagColor = tagColorEl.GetString();

                    meta.FrameTags.Add(new AsepriteFrameTag
                    {
                        Name = tag.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty,
                        From = tag.TryGetProperty("from", out var f) ? f.GetInt32() : 0,
                        To = tag.TryGetProperty("to", out var t) ? t.GetInt32() : 0,
                        Direction = tag.TryGetProperty("direction", out var d) ? d.GetString() : null,
                        Repeat = tag.TryGetProperty("repeat", out var r) ? r.GetInt32() : 0,
                        Data = tagData,
                        Color = tagColor,
                    });
                }
            }

            if (metaEl.TryGetProperty("slices", out var slicesEl) && slicesEl.ValueKind == JsonValueKind.Array)
            {
                meta.Slices = new List<AsepriteSlice>();
                foreach (var slice in slicesEl.EnumerateArray())
                {
                    var s = new AsepriteSlice
                    {
                        Name = slice.TryGetProperty("name", out var sn) ? sn.GetString() ?? string.Empty : string.Empty,
                        Keys = new List<AsepriteSliceKey>()
                    };

                    if (slice.TryGetProperty("keys", out var keysEl) && keysEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var key in keysEl.EnumerateArray())
                        {
                            if (!key.TryGetProperty("bounds", out var boundsEl)) continue;

                            (float x, float y)? pivot = null;
                            if (key.TryGetProperty("pivot", out var pivotEl))
                            {
                                var px = pivotEl.TryGetProperty("x", out var pvx) ? pvx.GetInt32() : 0;
                                var py = pivotEl.TryGetProperty("y", out var pvy) ? pvy.GetInt32() : 0;
                                pivot = (px, py);
                            }

                            s.Keys.Add(new AsepriteSliceKey
                            {
                                Frame = key.TryGetProperty("frame", out var kf) ? kf.GetInt32() : 0,
                                Bounds = new AsepriteRect
                                {
                                    X = boundsEl.TryGetProperty("x", out var bx) ? bx.GetInt32() : 0,
                                    Y = boundsEl.TryGetProperty("y", out var by) ? by.GetInt32() : 0,
                                    W = boundsEl.TryGetProperty("w", out var bw) ? bw.GetInt32() : 0,
                                    H = boundsEl.TryGetProperty("h", out var bh) ? bh.GetInt32() : 0,
                                },
                                Pivot = pivot
                            });
                        }
                    }

                    meta.Slices.Add(s);
                }
            }

            return meta;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Malformed JSON in Aseprite export 'meta' section: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger?.LogWarning(ex, "Unexpected error parsing Aseprite meta section; proceeding without tags/slices.");
            return null;
        }
    }

    private static int ExtractTrailingIndex(string key)
    {
        var span = key.AsSpan();
        var dotIdx = span.LastIndexOf('.');
        if (dotIdx >= 0)
            span = span[..dotIdx];

        int i = span.Length - 1;
        while (i >= 0 && char.IsDigit(span[i]))
            i--;

        if (i < span.Length - 1 && int.TryParse(span[(i + 1)..], out var index))
            return index;

        return 0;
    }

    private static AsepriteDirection ParseDirection(string? raw) => raw?.ToLowerInvariant() switch
    {
        "reverse" => AsepriteDirection.Reverse,
        "pingpong" => AsepriteDirection.PingPong,
        "pingpong_reverse" => AsepriteDirection.PingPongReverse,
        _ => AsepriteDirection.Forward
    };

    private static Brine2D.Core.Color? ParseAsepriteColor(string? hex)
    {
        if (string.IsNullOrEmpty(hex))
            return null;

        var s = hex.AsSpan();
        if (s.Length > 0 && s[0] == '#')
            s = s[1..];

        return s.Length switch
        {
            6 when uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var rgb)
                => new Brine2D.Core.Color((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF)),
            8 when uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var rgba)
                => new Brine2D.Core.Color((byte)((rgba >> 24) & 0xFF), (byte)((rgba >> 16) & 0xFF), (byte)((rgba >> 8) & 0xFF), (byte)(rgba & 0xFF)),
            _ => null
        };
    }
}