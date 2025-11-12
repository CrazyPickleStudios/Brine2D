using Brine2D.Core.Graphics;
using Brine2D.Core.Graphics.Text;
using Brine2D.Core.Math;
using System;
using System.Collections.Generic;
using System.Globalization;
using static Brine2D.SDL.Graphics.Text.TextLayout;
using static Brine2D.SDL.Graphics.Text.SpriteFont;

namespace Brine2D.SDL.Graphics.Text;

internal sealed class CompositeFont : IFont
{
    private const int MeasureCacheCapacity = 256;
    private readonly List<SpriteFont> _fonts;
    private readonly string _debugName;
    private int _fallbackHits;
    private int _unsupportedHits;

    private readonly Dictionary<int, MeasureEntry> _measureCache = new(MeasureCacheCapacity);
    private long _measureTick;
    private int _statMeasureHits;
    private int _statMeasureMisses;
    private int _statMeasureEvictions;

    private readonly Dictionary<SpriteFont, Dictionary<ulong, int>> _kernCacheByFont = new();
    private readonly Dictionary<uint, SpriteFont> _fontByCodepoint = new(256);

    private ITextShaper? _shaper;

    public SpriteFont PrimaryFont => _fonts[0];
    public int LineHeight { get; }
    public int ExtraLineSpacing { get; set; }
    public int TabSpaces
    {
        get => _fonts[0].TabSpaces;
        set { foreach (var f in _fonts) f.TabSpaces = value; }
    }
    public int MaxPages
    {
        get => _fonts[0].MaxPages;
        set { foreach (var f in _fonts) f.MaxPages = value; }
    }

    public CompositeFont(List<SpriteFont> fonts, string debugName)
    {
        if (fonts == null || fonts.Count == 0)
            throw new ArgumentException("CompositeFont requires at least one font.");
        _fonts = fonts;
        _debugName = debugName;
        LineHeight = fonts[0].LineHeight;
    }

    public void SetTextShaper(ITextShaper? shaper) => _shaper = shaper;

    internal SpriteFont ResolveFont(uint cp)
    {
        if (_fontByCodepoint.TryGetValue(cp, out var cached))
            return cached;

        var primary = _fonts[0];
        if (primary.IsRenderable(cp))
        {
            _fontByCodepoint[cp] = primary;
            return primary;
        }
        for (int i = 1; i < _fonts.Count; i++)
        {
            var f = _fonts[i];
            if (f.IsRenderable(cp))
            {
                _fallbackHits++;
                _fontByCodepoint[cp] = f;
                return f;
            }
        }
        _unsupportedHits++;
        _fontByCodepoint[cp] = primary;
        return primary;
    }

    internal int KerningApproxCached(SpriteFont font, uint left, uint right)
    {
        if (left == 0 || right == 0) return 0;
        if (!_kernCacheByFont.TryGetValue(font, out var map))
        {
            map = new Dictionary<ulong, int>(256);
            _kernCacheByFont[font] = map;
        }
        ulong key = ((ulong)left << 32) | right;
        if (map.TryGetValue(key, out var val)) return val;

        string pair = string.Concat(char.ConvertFromUtf32((int)left), char.ConvertFromUtf32((int)right));
        var (w, _) = font.Measure(pair);
        int without = font.GetAdvanceInternal(left) + font.GetAdvanceInternal(right);
        int k = w - without;
        if (k >= -1 && k <= 1) k = 0;
        map[key] = k;
        return k;
    }

    public (int width, int height) Measure(string text) => MeasureWrapped(text, 0);

    public (int width, int height) MeasureWrapped(string text, int maxWidth)
    {
        if (string.IsNullOrEmpty(text))
            return (0, LineHeight + ExtraLineSpacing);
        int keyHash = ComputeMeasureKeyHash(text, maxWidth <= 0 ? -1 : maxWidth);
        if (_measureCache.TryGetValue(keyHash, out var entry) &&
            entry.Matches(text, maxWidth <= 0 ? -1 : maxWidth))
        {
            _statMeasureHits++;
            entry.LastUsed = ++_measureTick;
            _measureCache[keyHash] = entry;
            return (entry.Width, entry.Height);
        }

        _statMeasureMisses++;
        var span = text.AsSpan();
        int effectiveLH = LineHeight + ExtraLineSpacing;

        (int w, int h) result;
        if (maxWidth <= 0)
        {
            int wMax = 0, lineW = 0, hAcc = effectiveLH;
            uint prev = 0; SpriteFont prevFont = _fonts[0];
            for (int i = 0; i < span.Length;)
            {
                var cp = NextCodepoint(span, ref i);
                if (cp == '\n')
                {
                    wMax = Math.Max(wMax, lineW);
                    lineW = 0; prev = 0; prevFont = _fonts[0]; hAcc += effectiveLH;
                    continue;
                }
                var fnt = ResolveFont(cp);
                int adv = fnt.GetAdvanceInternal(cp);
                if (prev != 0 && prevFont == fnt)
                    lineW += adv + KerningApproxCached(fnt, prev, cp);
                else
                    lineW += adv;
                prev = cp; prevFont = fnt;
            }
            wMax = Math.Max(wMax, lineW);
            result = (wMax, hAcc);
        }
        else
        {
            var segments = WordWrap(span, maxWidth);
            int maxLine = 0;
            foreach (var (start, len) in segments)
            {
                var line = span.Slice(start, len);
                int lineW = 0; uint prev = 0; SpriteFont prevFont = _fonts[0];
                for (int i = 0; i < line.Length;)
                {
                    var cp = NextCodepoint(line, ref i);
                    var fnt = ResolveFont(cp);
                    int adv = fnt.GetAdvanceInternal(cp);
                    if (prev != 0 && prevFont == fnt)
                        lineW += adv + KerningApproxCached(fnt, prev, cp);
                    else
                        lineW += adv;
                    prev = cp; prevFont = fnt;
                }
                if (lineW > maxLine) maxLine = lineW;
            }
            result = (maxLine, segments.Count * effectiveLH);
        }
        AddMeasureCacheEntry(keyHash, text, maxWidth <= 0 ? -1 : maxWidth, result.w, result.h);
        return result;
    }

    public void DrawString(ISpriteRenderer sprites, string text, int x, int y, Color color, float scale = 1f) =>
        DrawInternal(sprites, text.AsSpan(), x, y, color, scale, 0, TextAlign.Left);
    public void DrawStringWrapped(ISpriteRenderer sprites, string text, int x, int y, int maxWidth, TextAlign align, Color color, float scale = 1f) =>
        DrawInternal(sprites, text.AsSpan(), x, y, color, scale, maxWidth, align);

    public void PrewarmAscii() { foreach (var f in _fonts) f.PrewarmAscii(); }
    public void Prewarm(string text) { foreach (var f in _fonts) f.Prewarm(text); }
    public void PrewarmRange(int startInclusive, int endInclusive) { foreach (var f in _fonts) f.PrewarmRange(startInclusive, endInclusive); }
    public void ClearCache()
    {
        foreach (var f in _fonts) f.ClearCache();
        _fallbackHits = 0; _unsupportedHits = 0;
        _measureCache.Clear();
        _statMeasureHits = _statMeasureMisses = _statMeasureEvictions = 0;
        _kernCacheByFont.Clear();
        _fontByCodepoint.Clear();
    }

    public TextLayout BuildLayout(
        string text,
        int originX,
        int originY,
        in BuildLayoutOptions options,
        IReadOnlyList<StyleSpan>? styleSpans = null,
        int selectionStart = -1,
        int selectionLength = 0,
        Color? selectionColor = null)
    {
        var lines = new List<TextLayout.TextLine>();
        var glyphs = new List<TextLayout.LayoutGlyph>();
        var selectionRects = new List<TextLayout.HighlightRect>();
        var caretStops = new List<TextLayout.CaretStop>();

        bool hasSelection = selectionStart >= 0 && selectionLength > 0;
        int wrapWidth = (options.MaxWidth.HasValue && options.MaxWidth.Value > 0) ? options.MaxWidth.Value : 0;
        float scale = options.Scale;
        bool useKerning = !options.DisableKerning;
        bool prewarmOnly = options.PrewarmOnly;

        int? truncateWidth = options.TruncateWidth;
        string ellipsisStr = options.Ellipsis ?? "…";
        bool shapingEnabled = _shaper != null && !prewarmOnly && !options.UseGraphemeClusters;

        var span = text.AsSpan();
        var segments = wrapWidth > 0 ? WordWrap(span, wrapWidth, useKerning, prewarmOnly) : new List<(int start, int length)> { (0, span.Length) };

        int effectiveLH = LineHeight + ExtraLineSpacing;
        int widest = 0;
        bool anyDecor = false;

        List<StyleSpan>? sorted = null;
        if (styleSpans != null && styleSpans.Count > 0)
        {
            sorted = new List<StyleSpan>(styleSpans);
            sorted.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        int ellipsisWidth = truncateWidth.HasValue && truncateWidth.Value > 0
            ? MeasureRunAdvance(ellipsisStr.AsSpan(), useKerning, prewarmOnly)
            : 0;

        var shapedBuffer = shapingEnabled ? new List<ShapedGlyph>(128) : null;

        for (int li = 0; li < segments.Count; li++)
        {
            var (start, length) = segments[li];
            var lineSpan = span.Slice(start, length);

            int measuredLineW = 0;
            if (shapingEnabled)
            {
                shapedBuffer!.Clear();
                var features = ShapingFeatures.Default;
                _shaper!.Shape(lineSpan, shapedBuffer, this, in features, measureOnly: false);
                foreach (var sg in shapedBuffer)
                    measuredLineW += sg.Advance + (useKerning ? sg.Kerning : 0);
            }
            else
            {
                uint prevMeasure = 0; SpriteFont prevFontMeasure = _fonts[0];
                for (int i = 0; i < lineSpan.Length;)
                {
                    var cp = NextCodepoint(lineSpan, ref i);
                    var fnt = prewarmOnly ? _fonts[0] : ResolveFont(cp);
                    int adv = fnt.GetAdvanceInternal(cp);
                    if (useKerning && prevMeasure != 0 && prevFontMeasure == fnt)
                        measuredLineW += adv + KerningApproxCached(fnt, prevMeasure, cp);
                    else
                        measuredLineW += adv;
                    prevMeasure = cp; prevFontMeasure = fnt;
                }
            }

            bool doTruncate = truncateWidth.HasValue && measuredLineW > truncateWidth.Value;
            int truncLimit = truncateWidth ?? int.MaxValue;

            Dictionary<int, int>? justifyExtraByLocalIndex = null;
            bool canJustify =
                options.Justify &&
                wrapWidth > 0 &&
                !doTruncate &&
                !prewarmOnly &&
                options.Align == TextAlign.Left &&
                li < segments.Count - 1;

            if (canJustify && measuredLineW < wrapWidth && measuredLineW > 0)
            {
                int extra = wrapWidth - measuredLineW;
                var spacePositions = new List<int>();
                if (shapingEnabled)
                {
                    foreach (var sg in shapedBuffer!)
                        if (sg.ClusterLength == 1 && sg.Codepoint == ' ' && sg.ClusterStart < lineSpan.Length - 1)
                            spacePositions.Add(sg.ClusterStart);
                }
                else
                {
                    for (int i = 0; i < lineSpan.Length;)
                    {
                        int localIndex = i;
                        var cp = NextCodepoint(lineSpan, ref i);
                        if (cp == ' ' && localIndex < lineSpan.Length - 1)
                            spacePositions.Add(localIndex);
                    }
                }
                if (spacePositions.Count > 0 && extra > 0)
                {
                    int baseAdd = extra / spacePositions.Count;
                    int remainder = extra % spacePositions.Count;
                    if (baseAdd > 0 || remainder > 0)
                    {
                        justifyExtraByLocalIndex = new Dictionary<int, int>(spacePositions.Count);
                        for (int si = 0; si < spacePositions.Count; si++)
                        {
                            int add = baseAdd + (si < remainder ? 1 : 0);
                            if (add > 0) justifyExtraByLocalIndex[spacePositions[si]] = add;
                        }
                        measuredLineW = wrapWidth;
                    }
                }
            }

            int widthForAlign = doTruncate ? truncLimit : measuredLineW;
            int alignedX = options.Align switch
            {
                TextAlign.Center => originX - (int)(widthForAlign * 0.5f * scale),
                TextAlign.Right => originX - (int)(widthForAlign * scale),
                _ => originX
            };
            int baselineY = originY + (int)(li * effectiveLH * scale);

            uint prevCp = 0; SpriteFont prevFont = _fonts[0];
            int penXUnscaled = 0;
            int siSorted = 0;
            if (sorted != null)
                while (siSorted < sorted.Count && (sorted[siSorted].Start + sorted[siSorted].Length) <= start) siSorted++;

            int glyphLineStart = glyphs.Count;
            int caretLineStart = caretStops.Count;

            if (shapingEnabled)
            {
                for (int gi = 0; gi < shapedBuffer!.Count; gi++)
                {
                    var sg = shapedBuffer[gi];
                    int totalAdv = sg.Advance + (useKerning ? sg.Kerning : 0);

                    bool wouldOverflow = doTruncate && (penXUnscaled + totalAdv + ellipsisWidth > truncLimit);
                    if (wouldOverflow)
                    {
                        AppendEllipsisGlyphs(ellipsisStr.AsSpan(), alignedX, baselineY, scale,
                                             useKerning, prewarmOnly,
                                             ref penXUnscaled, ref prevCp, ref prevFont,
                                             sorted, ref siSorted,
                                             glyphs, start + sg.ClusterStart + sg.ClusterLength);
                        break;
                    }

                    caretStops.Add(new TextLayout.CaretStop(start + sg.ClusterStart, alignedX + (int)(penXUnscaled * scale)));

                    var g = prewarmOnly ? StubGlyph((short)totalAdv) : sg.Font.GetGlyphInternal(sg.Codepoint);

                    int drawXUnscaled = penXUnscaled + g.BearingX;
                    int drawY = baselineY + (int)((_fonts[0].Ascent + g.MinY) * scale);

                    Color glyphColor = default;
                    TextDecoration decorations = TextDecoration.None;
                    bool overrideColor = false;
                    int sourceIndex = start + sg.ClusterStart;

                    if (sorted != null)
                    {
                        while (siSorted < sorted.Count && (sorted[siSorted].Start + sorted[siSorted].Length) <= sourceIndex) siSorted++;
                        if (siSorted < sorted.Count && sorted[siSorted].Contains(sourceIndex))
                        {
                            glyphColor = sorted[siSorted].Color;
                            decorations = sorted[siSorted].Decorations;
                            overrideColor = true;
                        }
                    }
                    if (decorations != TextDecoration.None) anyDecor = true;

                    glyphs.Add(new TextLayout.LayoutGlyph(
                        sg.Codepoint,
                        sourceIndex,
                        alignedX + (int)(drawXUnscaled * scale),
                        drawY,
                        (short)totalAdv,
                        sg.Font,
                        g,
                        glyphColor,
                        overrideColor,
                        decorations));

                    penXUnscaled += totalAdv;
                    prevCp = sg.Codepoint; prevFont = sg.Font;
                }

                if (!(doTruncate && penXUnscaled >= truncLimit))
                    caretStops.Add(new TextLayout.CaretStop(start + length, alignedX + (int)(penXUnscaled * scale)));
            }
            else
            {
                if (options.UseGraphemeClusters)
                {
                    string lineStr = lineSpan.ToString();
                    var ge = StringInfo.GetTextElementEnumerator(lineStr);
                    while (ge.MoveNext())
                    {
                        int elemIndex = ge.ElementIndex;
                        int elemLength = ge.GetTextElement().Length;
                        int elemAbsIndex = start + elemIndex;
                        var elemSub = lineSpan.Slice(elemIndex, elemLength);

                        int graphemeAdv = 0;
                        uint prevTmp = prevCp; SpriteFont prevFontTmp = prevFont;
                        int jMeasure = 0;
                        while (jMeasure < elemSub.Length)
                        {
                            var cpTmp = NextCodepoint(elemSub, ref jMeasure);
                            var fntTmp = prewarmOnly ? _fonts[0] : ResolveFont(cpTmp);
                            int a = fntTmp.GetAdvanceInternal(cpTmp);
                            if (useKerning && prevTmp != 0 && prevFontTmp == fntTmp)
                                graphemeAdv += a + KerningApproxCached(fntTmp, prevTmp, cpTmp);
                            else
                                graphemeAdv += a;
                            prevTmp = cpTmp; prevFontTmp = fntTmp;
                        }

                        bool wouldOverflow = doTruncate && (penXUnscaled + graphemeAdv + ellipsisWidth > truncLimit);
                        if (wouldOverflow)
                        {
                            AppendEllipsisGlyphs(ellipsisStr.AsSpan(), alignedX, baselineY, scale,
                                                 useKerning, prewarmOnly,
                                                 ref penXUnscaled, ref prevCp, ref prevFont,
                                                 sorted, ref siSorted,
                                                 glyphs, start + length);
                            break;
                        }

                        caretStops.Add(new TextLayout.CaretStop(elemAbsIndex, alignedX + (int)(penXUnscaled * scale)));

                        int j = 0;
                        while (j < elemSub.Length)
                        {
                            int cpLocal = j;
                            int cpAbs = elemAbsIndex + cpLocal;
                            var cp = NextCodepoint(elemSub, ref j);
                            var fnt = prewarmOnly ? _fonts[0] : ResolveFont(cp);

                            if (useKerning && prevCp != 0 && prevFont == fnt)
                                penXUnscaled += KerningApproxCached(fnt, prevCp, cp);

                            int adv = fnt.GetAdvanceInternal(cp);
                            var g = prewarmOnly ? StubGlyph((short)adv) : fnt.GetGlyphInternal(cp);

                            int drawXUnscaled = penXUnscaled + g.BearingX;
                            int drawY = baselineY + (int)((_fonts[0].Ascent + g.MinY) * scale);

                            Color glyphColor = default;
                            TextDecoration decorations = TextDecoration.None;
                            bool overrideColor = false;
                            if (sorted != null)
                            {
                                while (siSorted < sorted.Count && (sorted[siSorted].Start + sorted[siSorted].Length) <= cpAbs) siSorted++;
                                if (siSorted < sorted.Count && sorted[siSorted].Contains(cpAbs))
                                {
                                    glyphColor = sorted[siSorted].Color;
                                    decorations = sorted[siSorted].Decorations;
                                    overrideColor = true;
                                }
                            }
                            if (decorations != TextDecoration.None) anyDecor = true;

                            glyphs.Add(new TextLayout.LayoutGlyph(
                                cp,
                                cpAbs,
                                alignedX + (int)(drawXUnscaled * scale),
                                drawY,
                                (short)adv,
                                fnt,
                                g,
                                glyphColor,
                                overrideColor,
                                decorations));

                            penXUnscaled += adv;
                            prevCp = cp; prevFont = fnt;
                        }
                    }

                    if (!(doTruncate && penXUnscaled >= truncLimit))
                        caretStops.Add(new TextLayout.CaretStop(start + length, alignedX + (int)(penXUnscaled * scale)));
                }
                else
                {
                    for (int i = 0; i < lineSpan.Length;)
                    {
                        int cpLocal = i;
                        int cpAbs = start + cpLocal;
                        var cp = NextCodepoint(lineSpan, ref i);
                        var fnt = prewarmOnly ? _fonts[0] : ResolveFont(cp);

                        int adv = fnt.GetAdvanceInternal(cp);
                        if (useKerning && prevCp != 0 && prevFont == fnt)
                            adv += KerningApproxCached(fnt, prevCp, cp);

                        bool wouldOverflow = doTruncate && (penXUnscaled + adv + ellipsisWidth > truncLimit);
                        if (wouldOverflow)
                        {
                            AppendEllipsisGlyphs(ellipsisStr.AsSpan(), alignedX, baselineY, scale,
                                                 useKerning, prewarmOnly,
                                                 ref penXUnscaled, ref prevCp, ref prevFont,
                                                 sorted, ref siSorted,
                                                 glyphs, start + i);
                            break;
                        }

                        if (useKerning && prevCp != 0 && prevFont == fnt)
                            penXUnscaled += KerningApproxCached(fnt, prevCp, cp);

                        var g = prewarmOnly ? StubGlyph((short)adv) : fnt.GetGlyphInternal(cp);

                        int drawXUnscaled = penXUnscaled + g.BearingX;
                        int drawY = baselineY + (int)((_fonts[0].Ascent + g.MinY) * scale);

                        Color glyphColor = default;
                        TextDecoration decorations = TextDecoration.None;
                        bool overrideColor = false;
                        if (sorted != null)
                        {
                            while (siSorted < sorted.Count && (sorted[siSorted].Start + sorted[siSorted].Length) <= cpAbs) siSorted++;
                            if (siSorted < sorted.Count && sorted[siSorted].Contains(cpAbs))
                            {
                                glyphColor = sorted[siSorted].Color;
                                decorations = sorted[siSorted].Decorations;
                                overrideColor = true;
                            }
                        }
                        if (decorations != TextDecoration.None) anyDecor = true;

                        glyphs.Add(new TextLayout.LayoutGlyph(
                            cp,
                            cpAbs,
                            alignedX + (int)(drawXUnscaled * scale),
                            drawY,
                            (short)adv,
                            fnt,
                            g,
                            glyphColor,
                            overrideColor,
                            decorations));

                        penXUnscaled += adv;
                        prevCp = cp; prevFont = fnt;
                    }
                }
            }

            if (hasSelection && glyphs.Count > glyphLineStart)
            {
                int lineStart = start;
                int lineEnd = start + length;
                int selStartLine = Math.Max(lineStart, selectionStart);
                int selEndLine = Math.Min(lineEnd, selectionStart + selectionLength);
                if (selEndLine > selStartLine)
                {
                    int firstX = int.MaxValue;
                    int lastX = int.MinValue;
                    for (int gi = glyphs.Count - 1; gi >= glyphLineStart; gi--)
                    {
                        var lg = glyphs[gi];
                        if (lg.SourceIndex < lineStart) break;
                        if (lg.SourceIndex >= lineEnd) continue;
                        if (lg.SourceIndex >= selStartLine && lg.SourceIndex < selEndLine)
                        {
                            firstX = Math.Min(firstX, lg.X);
                            lastX = Math.Max(lastX, lg.X + (int)(lg.Advance * scale));
                        }
                    }
                    if (firstX != int.MaxValue)
                        selectionRects.Add(new TextLayout.HighlightRect(
                            firstX,
                            baselineY - (int)(_fonts[0].Ascent * scale),
                            Math.Max(1, lastX - firstX),
                            (int)(effectiveLH * scale)));
                }
            }

            int finalLineW =
                doTruncate ? Math.Min(truncLimit, penXUnscaled)
                           : (justifyExtraByLocalIndex != null ? wrapWidth : penXUnscaled);
            widest = Math.Max(widest, finalLineW);

            lines.Add(new TextLayout.TextLine(start, length, finalLineW, baselineY, glyphLineStart, glyphs.Count - glyphLineStart, caretLineStart, caretStops.Count - caretLineStart));
        }

        int totalHeight = (int)(segments.Count * effectiveLH * scale);
        int layoutWidth = (int)(widest * scale);

        return new TextLayout(
            lines,
            glyphs,
            selectionRects,
            caretStops,
            layoutWidth,
            totalHeight,
            scale,
            originX,
            originY,
            false,
            wrapWidth,
            options.Align,
            hasSelection,
            selectionColor.GetValueOrDefault(new Color(30, 120, 200, 80)),
            anyDecor,
            (int)(effectiveLH * scale));
    }

    public TextLayout BuildLayout(
        string text,
        int originX,
        int originY,
        int? maxWidth = null,
        TextAlign align = TextAlign.Left,
        float scale = 1f,
        IReadOnlyList<StyleSpan>? styleSpans = null,
        int selectionStart = -1,
        int selectionLength = 0,
        Color? selectionColor = null)
        => BuildLayout(text, originX, originY,
                       new BuildLayoutOptions(maxWidth, align, scale, useGraphemeClusters: false),
                       styleSpans, selectionStart, selectionLength, selectionColor);

    public void DrawLayout(ISpriteRenderer sprites, TextLayout layout, Color baseColor, float? overrideScale = null)
    {
        float scale = overrideScale ?? layout.Scale;
        var solid = _fonts[0].GetSolidTexture();

        if (layout.HasSelection)
            foreach (var r in layout.SelectionRects)
                sprites.Draw(solid, null, new Rectangle(r.X, r.Y, r.Width, r.Height), layout.SelectionColor);

        foreach (var lg in layout.Glyphs)
        {
            var g = lg.Cached;
            if (g.Width <= 0 || g.Height <= 0) continue;
            var page = lg.Font.Pages[g.Page];
            int dw = Math.Max(1, (int)(g.Width * scale));
            int dh = Math.Max(1, (int)(g.Height * scale));
            var tint = lg.OverrideColor ? lg.Color : baseColor;
            sprites.Draw(page,
                new Rectangle(g.OffsetX, g.OffsetY, g.Width, g.Height),
                new Rectangle(lg.X, lg.Y, dw, dh),
                tint);
        }

        if (layout.HasAnyDecorations)
        {
            foreach (var lg in layout.Glyphs)
            {
                if (lg.Decorations == TextDecoration.None) continue;
                var g = lg.Cached;
                int dw = Math.Max(1, (int)(g.Advance * scale));
                int ascent = _fonts[0].Ascent;
                int baselineY = lg.Y - (int)((ascent + g.MinY) * scale);
                if ((lg.Decorations & TextDecoration.Underline) != 0)
                {
                    int underlineY = baselineY + (int)(scale);
                    sprites.Draw(solid, null,
                        new Rectangle(lg.X, underlineY, dw, Math.Max(1, (int)(scale))),
                        lg.OverrideColor ? lg.Color : baseColor);
                }
                if ((lg.Decorations & TextDecoration.Strikethrough) != 0)
                {
                    int strikeY = baselineY - (int)(ascent * 0.4f * scale);
                    sprites.Draw(solid, null,
                        new Rectangle(lg.X, strikeY, dw, Math.Max(1, (int)(scale))),
                        lg.OverrideColor ? lg.Color : baseColor);
                }
            }
        }
    }

    public void DrawCaret(ISpriteRenderer sprites, TextLayout layout, int index, Color color, int thickness = 1)
    {
        var rect = layout.GetCaretRect(index, thickness);
        var solid = _fonts[0].GetSolidTexture();
        sprites.Draw(solid, null, rect, color);
    }

    public CompositeFontStats GetStats()
    {
        long totalAtlasPixels = 0;
        long totalAtlasUsed = 0;
        int totalGlyphs = 0;
        int totalPages = 0;
        int totalKerningPairs = 0;
        int totalKerningLookups = 0;
        int totalGlyphCacheHits = 0;
        int totalGlyphCacheMisses = 0;
        int totalGlyphBitmaps = 0;
        int totalUnsupported = 0;
        int totalTransient = 0;
        int totalMeasureEntriesFonts = 0;
        int totalMeasureHitsFonts = 0;
        int totalMeasureMissesFonts = 0;
        int totalMeasureEvictionsFonts = 0;

        foreach (var f in _fonts)
        {
            var s = f.GetStats();
            totalAtlasPixels += s.AtlasTotalPixels;
            totalAtlasUsed += s.AtlasUsedPixels;
            totalGlyphs += s.GlyphsCached;
            totalPages += s.Pages;
            totalKerningPairs += s.KerningPairsCached;
            totalKerningLookups += s.KerningLookups;
            totalGlyphCacheHits += s.GlyphCacheHits;
            totalGlyphCacheMisses += s.GlyphCacheMisses;
            totalGlyphBitmaps += s.GlyphBitmapsCreated;
            totalUnsupported += s.UnsupportedGlyphHits;
            totalTransient += s.TransientGlyphFailures;
            totalMeasureEntriesFonts += s.MeasureEntries;
            totalMeasureHitsFonts += s.MeasureHits;
            totalMeasureMissesFonts += s.MeasureMisses;
            totalMeasureEvictionsFonts += s.MeasureEvictions;
        }

        float atlasPct = totalAtlasPixels > 0 ? (float)(totalAtlasUsed * 100.0 / totalAtlasPixels) : 0f;

        return new CompositeFontStats(
            _debugName,
            _fonts.Count,
            _fallbackHits,
            _unsupportedHits,
            _measureCache.Count,
            _statMeasureHits,
            _statMeasureMisses,
            _statMeasureEvictions,
            totalGlyphs,
            totalPages,
            totalKerningPairs,
            totalKerningLookups,
            totalGlyphCacheHits,
            totalGlyphCacheMisses,
            totalGlyphBitmaps,
            totalUnsupported,
            totalTransient,
            totalMeasureEntriesFonts,
            totalMeasureHitsFonts,
            totalMeasureMissesFonts,
            totalMeasureEvictionsFonts,
            totalAtlasPixels,
            totalAtlasUsed,
            atlasPct);
    }

    private void DrawInternal(ISpriteRenderer sprites, ReadOnlySpan<char> text, int x, int y, Color color, float scale, int wrapWidth, TextAlign align)
    {
        if (text.Length == 0) return;
        int effectiveLH = LineHeight + ExtraLineSpacing;

        if (wrapWidth <= 0)
        {
            int penX = x, penY = y;
            uint prev = 0; SpriteFont prevFont = _fonts[0];
            for (int i = 0; i < text.Length;)
            {
                var cp = NextCodepoint(text, ref i);
                if (cp == '\n')
                {
                    penX = x;
                    penY += (int)(effectiveLH * scale);
                    prev = 0; prevFont = _fonts[0];
                    continue;
                }
                var fnt = ResolveFont(cp);
                if (prev != 0 && prevFont == fnt)
                    penX += (int)(KerningApproxCached(fnt, prev, cp) * scale);
                DrawGlyph(fnt, sprites, cp, ref penX, penY, color, scale);
                prev = cp; prevFont = fnt;
            }
            return;
        }

        var segments = WordWrap(text, wrapWidth);
        for (int li = 0; li < segments.Count; li++)
        {
            var (start, len) = segments[li];
            var line = text.Slice(start, len);

            int lineW = 0; uint prevMeasure = 0; SpriteFont prevFontMeasure = _fonts[0];
            for (int i = 0; i < line.Length;)
            {
                var cp = NextCodepoint(line, ref i);
                var fnt = ResolveFont(cp);
                int adv = fnt.GetAdvanceInternal(cp);
                if (prevMeasure != 0 && prevFontMeasure == fnt)
                    lineW += adv + KerningApproxCached(fnt, prevMeasure, cp);
                else
                    lineW += adv;
                prevMeasure = cp; prevFontMeasure = fnt;
            }

            int startX = align switch
            {
                TextAlign.Center => x - (int)(lineW * 0.5f * scale),
                TextAlign.Right => x - (int)(lineW * scale),
                _ => x
            };

            int penX = startX;
            int penY = y + (int)(li * effectiveLH * scale);
            uint prev = 0; SpriteFont prevFont = _fonts[0];
            for (int i = 0; i < line.Length;)
            {
                var cp = NextCodepoint(line, ref i);
                var fnt = ResolveFont(cp);
                if (prev != 0 && prevFont == fnt)
                    penX += (int)(KerningApproxCached(fnt, prev, cp) * scale);
                DrawGlyph(fnt, sprites, cp, ref penX, penY, color, scale);
                prev = cp; prevFont = fnt;
            }
        }
    }

    private void DrawGlyph(SpriteFont font, ISpriteRenderer sprites, uint cp, ref int penX, int penY, Color color, float scale)
    {
        var g = font.GetGlyphInternal(cp);
        if (g.Width > 0 && g.Height > 0)
        {
            int gw = Math.Max(1, (int)(g.Width * scale));
            int gh = Math.Max(1, (int)(g.Height * scale));
            int ascent = _fonts[0].Ascent;
            int drawX = penX + (int)(g.BearingX * scale);
            int drawY = penY + (int)((ascent + g.MinY) * scale);
            var page = font.Pages[g.Page];
            sprites.Draw(page,
                new Rectangle(g.OffsetX, g.OffsetY, g.Width, g.Height),
                new Rectangle(drawX, drawY, gw, gh),
                color);
        }
        penX += (int)(g.Advance * scale);
    }

    private int ComputeMeasureKeyHash(string text, int maxWidth)
    {
        unchecked
        {
            int h = SpriteFont.HashTextFNV1a(text.AsSpan());
            h = h * 31 + maxWidth;
            h = h * 31 + ExtraLineSpacing;
            h = h * 31 + TabSpaces;
            h = h * 31 + _fonts[0].Ascent;
            h = h * 31 + _fonts[0].LineHeight;
            return h;
        }
    }
    private void AddMeasureCacheEntry(int hash, string text, int mw, int w, int h)
    {
        if (_measureCache.Count >= MeasureCacheCapacity)
            EvictLeastUsed();
        _measureCache[hash] = new MeasureEntry(text, mw, w, h, ++_measureTick);
    }
    private void EvictLeastUsed()
    {
        long oldest = long.MaxValue; int oldestKey = 0; bool found = false;
        foreach (var kv in _measureCache)
        {
            if (kv.Value.LastUsed < oldest) { oldest = kv.Value.LastUsed; oldestKey = kv.Key; found = true; }
        }
        if (found) { _measureCache.Remove(oldestKey); _statMeasureEvictions++; }
    }
    private struct MeasureEntry
    {
        public readonly string Text;
        public readonly int MaxWidthKey;
        public readonly int Width;
        public readonly int Height;
        public long LastUsed;
        public MeasureEntry(string text, int mw, int w, int h, long tick)
        { Text = text; MaxWidthKey = mw; Width = w; Height = h; LastUsed = tick; }
        public bool Matches(string text, int mw) => text == Text && mw == MaxWidthKey;
    }

    private static uint NextCodepoint(ReadOnlySpan<char> s, ref int i)
    {
        if (i >= s.Length) return 0;
        char c = s[i++];
        if (char.IsHighSurrogate(c) && i < s.Length && char.IsLowSurrogate(s[i]))
        {
            char lo = s[i++];
            return (uint)char.ConvertToUtf32(c, lo);
        }
        return (uint)c;
    }

    private List<(int start, int length)> WordWrap(ReadOnlySpan<char> text, int maxWidth)
        => WordWrap(text, maxWidth, useKerning: true, prewarmOnly: false);

    private List<(int start, int length)> WordWrap(ReadOnlySpan<char> text, int maxWidth, bool useKerning, bool prewarmOnly)
    {
        var segments = new List<(int, int)>();
        if (maxWidth <= 0)
        {
            segments.Add((0, text.Length));
            return segments;
        }
        int start = 0, curW = 0, lastBreakIndex = -1;
        uint prev = 0; SpriteFont prevFont = _fonts[0];
        for (int i = 0; i < text.Length;)
        {
            int cpIndex = i;
            var cp = NextCodepoint(text, ref i);
            if (cp == '\n')
            {
                segments.Add((start, cpIndex - start));
                start = i; curW = 0; lastBreakIndex = -1; prev = 0; prevFont = _fonts[0];
                continue;
            }
            var fnt = prewarmOnly ? _fonts[0] : ResolveFont(cp);
            int kern = (useKerning && prev != 0 && prevFont == fnt) ? KerningApproxCached(fnt, prev, cp) : 0;
            int adv = kern + fnt.GetAdvanceInternal(cp);
            int nextW = curW + adv;
            if (nextW > maxWidth && curW > 0)
            {
                if (lastBreakIndex >= 0)
                {
                    segments.Add((start, lastBreakIndex - start + 1));
                    i = lastBreakIndex + 1;
                    start = i; curW = 0; lastBreakIndex = -1; prev = 0; prevFont = _fonts[0];
                }
                else
                {
                    segments.Add((start, Math.Max(1, cpIndex - start)));
                    start = cpIndex; curW = 0; lastBreakIndex = -1; prev = 0; prevFont = _fonts[0];
                }
            }
            else
            {
                curW = nextW;
                if (cp == ' ' || cp == '\t' || cp == '-' || cp == 0x00A0)
                    lastBreakIndex = i - 1;
                prev = cp; prevFont = fnt;
            }
        }
        if (start <= text.Length)
            segments.Add((start, text.Length - start));
        return segments;
    }

    private int MeasureRunAdvance(ReadOnlySpan<char> run, bool useKerning, bool prewarmOnly)
    {
        int w = 0;
        uint prev = 0; SpriteFont prevFont = _fonts[0];
        for (int i = 0; i < run.Length;)
        {
            var cp = NextCodepoint(run, ref i);
            var fnt = prewarmOnly ? _fonts[0] : ResolveFont(cp);
            int adv = fnt.GetAdvanceInternal(cp);
            if (useKerning && prev != 0 && prevFont == fnt)
                w += adv + KerningApproxCached(fnt, prev, cp);
            else
                w += adv;
            prev = cp; prevFont = fnt;
        }
        return w;
    }

    private void AppendEllipsisGlyphs(
        ReadOnlySpan<char> ellipsis,
        int alignedX,
        int baselineY,
        float scale,
        bool useKerning,
        bool prewarmOnly,
        ref int penXUnscaled,
        ref uint prev,
        ref SpriteFont prevFont,
        List<StyleSpan>? sorted,
        ref int si,
        List<TextLayout.LayoutGlyph> glyphs,
        int sourceIndexBase)
    {
        if (ellipsis.Length == 0) return;

        var cpList = new List<uint>(ellipsis.Length);
        for (int i = 0; i < ellipsis.Length;)
            cpList.Add(NextCodepoint(ellipsis, ref i));

        for (int idx = 0; idx < cpList.Count; idx++)
        {
            uint cp = cpList[idx];
            var fnt = prewarmOnly ? _fonts[0] : ResolveFont(cp);

            if (useKerning && prev != 0 && prevFont == fnt)
                penXUnscaled += KerningApproxCached(fnt, prev, cp);

            var g = prewarmOnly
                ? StubGlyph((short)fnt.GetAdvanceInternal(cp))
                : fnt.GetGlyphInternal(cp);

            int drawXUnscaled = penXUnscaled + g.BearingX;
            int drawY = baselineY + (int)((_fonts[0].Ascent + g.MinY) * scale);

            glyphs.Add(new TextLayout.LayoutGlyph(
                cp,
                sourceIndexBase,
                alignedX + (int)(drawXUnscaled * scale),
                drawY,
                g.Advance,
                fnt,
                g,
                default,
                false,
                TextDecoration.None));

            penXUnscaled += g.Advance;
            prev = cp;
            prevFont = fnt;
        }
    }

    private static SpriteFont.Glyph StubGlyph(short advance) => new SpriteFont.Glyph
    {
        Advance = advance,
        BearingX = 0,
        MinY = 0,
        MaxY = 0,
        Width = 0,
        Height = 0,
        Page = 0,
        OffsetX = 0,
        OffsetY = 0,
        U0 = 0,
        V0 = 0,
        U1 = 0,
        V1 = 0
    };

    internal readonly struct CompositeFontStats
    {
        public readonly string Name;
        public readonly int FontCount;
        public readonly int FallbackHits;
        public readonly int UnsupportedHits;
        public readonly int CompositeMeasureEntries;
        public readonly int CompositeMeasureHits;
        public readonly int CompositeMeasureMisses;
        public readonly int CompositeMeasureEvictions;
        public readonly int TotalGlyphsCached;
        public readonly int TotalPages;
        public readonly int TotalKerningPairs;
        public readonly int TotalKerningLookups;
        public readonly int TotalGlyphCacheHits;
        public readonly int TotalGlyphCacheMisses;
        public readonly int TotalGlyphBitmaps;
        public readonly int TotalUnsupportedGlyphHits;
        public readonly int TotalTransientGlyphFailures;
        public readonly int FontsMeasureEntries;
        public readonly int FontsMeasureHits;
        public readonly int FontsMeasureMisses;
        public readonly int FontsMeasureEvictions;
        public readonly long AtlasTotalPixels;
        public readonly long AtlasUsedPixels;
        public readonly float AtlasUsagePercent;

        public CompositeFontStats(
            string name,
            int fontCount,
            int fallback,
            int unsupported,
            int compEntries,
            int compHits,
            int compMisses,
            int compEvict,
            int totalGlyphs,
            int totalPages,
            int totalKerningPairs,
            int totalKerningLookups,
            int totalCacheHits,
            int totalCacheMisses,
            int totalBitmaps,
            int totalUnsupportedGlyphs,
            int totalTransientFailures,
            int fontsEntries,
            int fontsHits,
            int fontsMisses,
            int fontsEvict,
            long atlasTotal,
            long atlasUsed,
            float atlasPct)
        {
            Name = name;
            FontCount = fontCount;
            FallbackHits = fallback;
            UnsupportedHits = unsupported;
            CompositeMeasureEntries = compEntries;
            CompositeMeasureHits = compHits;
            CompositeMeasureMisses = compMisses;
            CompositeMeasureEvictions = compEvict;
            TotalGlyphsCached = totalGlyphs;
            TotalPages = totalPages;
            TotalKerningPairs = totalKerningPairs;
            TotalKerningLookups = totalKerningLookups;
            TotalGlyphCacheHits = totalCacheHits;
            TotalGlyphCacheMisses = totalCacheMisses;
            TotalGlyphBitmaps = totalBitmaps;
            TotalUnsupportedGlyphHits = totalUnsupportedGlyphs;
            TotalTransientGlyphFailures = totalTransientFailures;
            FontsMeasureEntries = fontsEntries;
            FontsMeasureHits = fontsHits;
            FontsMeasureMisses = fontsMisses;
            FontsMeasureEvictions = fontsEvict;
            AtlasTotalPixels = atlasTotal;
            AtlasUsedPixels = atlasUsed;
            AtlasUsagePercent = atlasPct;
        }
    }
}