using Brine2D.Core.Graphics;
using Brine2D.Core.Graphics.Text;
using Brine2D.Core.Math;
using Brine2D.SDL.Content.Loaders;
using Brine2D.SDL.Graphics;
using Brine2D.SDL.Hosting;
using SDL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using static Brine2D.SDL.Graphics.Text.TextLayout;
using static SDL.SDL3;
using static SDL.SDL3_ttf;

namespace Brine2D.SDL.Graphics.Text;

internal unsafe sealed class SpriteFont : IFont, IDisposable
{
    private const int DefaultAtlasSize = 1024;
    private const int Padding = 1;
    private const int MaxAtlasSize = 4096;
    private const int MaxKerningPairs = 4096;
    private const int MeasureCacheCapacity = 512;

    private readonly int _creatorThreadId;
    private readonly SdlHost _host;
    private readonly TtfFont _ttf;
    private readonly int _ptSize;
    private readonly List<SdlTexture2D> _pages = new();
    private readonly Dictionary<uint, Glyph> _glyphs = new(256);

    private SDL_GPUTransferBuffer* _uploadBuf;
    private uint _uploadSize;
    private int _curX, _curY, _rowH;
    private short _spaceAdvance;
    private short _tabAdvance;

    private readonly Dictionary<ulong, int> _kernCache = new(256);
    private bool _kerningEnabled = true;
    private bool _baselineOrigin;

    private readonly HashSet<uint> _unsupported = new();
    private readonly HashSet<uint> _transientFailed = new();
    private uint _missingPrimary = (uint)'?';
    private uint _missingSecondary = 0xFFFD;

    private readonly Dictionary<int, MeasureEntry> _measureCache = new(MeasureCacheCapacity);
    private long _measureTick;
    private int _statMeasureHits;
    private int _statMeasureMisses;
    private int _statMeasureEvictions;

    private int _statGlyphCacheHits;
    private int _statGlyphCacheMisses;
    private int _statGlyphBitmaps;
    private int _statKerningLookups;
    private int _statKerningPairs;
    private int _statUnsupportedHits;
    private int _statTransientFails;

    private SdlTexture2D? _solidTexture;

    public int TabSpaces { get; set; } = 4;
    public int ExtraLineSpacing { get; set; } = 0;
    public int MaxPages { get; set; } = 0;

    public int Ascent { get; }
    public int Descent { get; }
    public int LineHeight { get; }
    public string DebugName { get; }
    public bool IsValid { get; private set; } = true;

    private int EffectiveLineHeight => Math.Max(1, LineHeight + ExtraLineSpacing);

    internal IReadOnlyList<SdlTexture2D> Pages => _pages;

    public SpriteFont(SdlHost host, TtfFont ttf, int ptSize, string? debugName = null)
    {
        _creatorThreadId = Environment.CurrentManagedThreadId;
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _ttf = ttf ?? throw new ArgumentNullException(nameof(ttf));
        _ptSize = ptSize > 0 ? ptSize : ttf.PointSize;
        DebugName = debugName ?? $"SpriteFont:{_ptSize}pt";

        Ascent = Math.Max(0, TTF_GetFontAscent(ttf.Ptr));
        Descent = Math.Min(0, TTF_GetFontDescent(ttf.Ptr));
        var skip = TTF_GetFontLineSkip(ttf.Ptr);
        var height = TTF_GetFontHeight(ttf.Ptr);
        LineHeight = Math.Max(skip, Math.Max(height, Ascent - Descent));

        EnsureAtlasPage(DefaultAtlasSize);
    }

    public void Dispose()
    {
        for (int i = 0; i < _pages.Count; i++)
            _pages[i].Dispose();
        _pages.Clear();

        if (_uploadBuf != null)
        {
            SDL_ReleaseGPUTransferBuffer(_host.Device, _uploadBuf);
            _uploadBuf = null;
            _uploadSize = 0;
        }

        if (_solidTexture != null)
        {
            _solidTexture.Dispose();
            _solidTexture = null;
        }
    }

    internal void SetMissingGlyphPrimary(uint cp) => _missingPrimary = cp;
    internal void SetMissingGlyphSecondary(uint cp) => _missingSecondary = cp;
    internal void SetKerningEnabled(bool enabled) => _kerningEnabled = enabled;
    internal void SetBaselineOrigin(bool baselineOrigin) => _baselineOrigin = baselineOrigin;

    [Conditional("DEBUG")]
    private void AssertThread()
    {
        Debug.Assert(Environment.CurrentManagedThreadId == _creatorThreadId,
            "SpriteFont mutating call from non-creator thread.");
    }

    internal static int HashTextFNV1a(ReadOnlySpan<char> text)
    {
        unchecked
        {
            uint hash = 2166136261;
            for (int i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 16777619;
            }
            return (int)hash;
        }
    }

    public (int width, int height) Measure(string text) => MeasureWrapped(text, 0);

    public (int width, int height) MeasureWrapped(string text, int maxWidth)
    {
        if (string.IsNullOrEmpty(text))
            return (0, EffectiveLineHeight);

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

        (int w, int h) result;
        if (maxWidth <= 0)
        {
            int width = 0, lineW = 0, height = EffectiveLineHeight;
            uint prev = 0;
            for (int i = 0; i < span.Length;)
            {
                var cp = NextCodepoint(span, ref i);
                if (cp == '\n')
                {
                    width = Math.Max(width, lineW);
                    lineW = 0;
                    prev = 0;
                    height += EffectiveLineHeight;
                    continue;
                }
                int kern = _kerningEnabled && prev != 0 ? GetKerning(prev, cp) : 0;
                lineW += kern + GetAdvance(cp);
                prev = cp;
            }
            width = Math.Max(width, lineW);
            result = (width, height);
        }
        else
        {
            var segments = WordWrapSegments(span, maxWidth);
            int maxLine = 0;
            foreach (var (start, length) in segments)
            {
                var line = span.Slice(start, length);
                int lineW = 0;
                uint prev = 0;
                for (int i = 0; i < line.Length;)
                {
                    var cp = NextCodepoint(line, ref i);
                    int kern = _kerningEnabled && prev != 0 ? GetKerning(prev, cp) : 0;
                    lineW += kern + GetAdvance(cp);
                    prev = cp;
                }
                if (lineW > maxLine) maxLine = lineW;
            }
            result = (maxLine, segments.Count * EffectiveLineHeight);
        }

        AddMeasureCacheEntry(keyHash, text, maxWidth <= 0 ? -1 : maxWidth, result.w, result.h);
        return result;
    }

    public void DrawString(ISpriteRenderer sprites, string text, int x, int y, Color color, float scale = 1f) =>
        DrawInternal(sprites, text.AsSpan(), x, y, color, scale, 0, TextAlign.Left);

    public void DrawStringWrapped(ISpriteRenderer sprites, string text, int x, int y, int maxWidth, TextAlign align, Color color, float scale = 1f) =>
        DrawInternal(sprites, text.AsSpan(), x, y, color, scale, maxWidth, align);

    public void PrewarmAscii() => PrewarmRange(32, 126);
    public void Prewarm(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var span = text.AsSpan();
        for (int i = 0; i < span.Length;)
        {
            var cp = NextCodepoint(span, ref i);
            if (cp == '\n') continue;
            _ = GetGlyphCached(cp);
        }
    }
    public void PrewarmRange(int startInclusive, int endInclusive)
    {
        int a = Math.Min(startInclusive, endInclusive);
        int b = Math.Max(startInclusive, endInclusive);
        for (int cp = a; cp <= b; cp++)
        {
            if (cp == '\n') continue;
            _ = GetGlyphCached((uint)cp);
        }
    }
    public void ClearCache()
    {
        AssertThread();
        foreach (var p in _pages) p.Dispose();
        _pages.Clear();
        _glyphs.Clear();
        _kernCache.Clear();
        _unsupported.Clear();
        _transientFailed.Clear();
        _measureCache.Clear();
        _spaceAdvance = 0;
        _tabAdvance = 0;
        _curX = _curY = _rowH = 0;
        _statGlyphBitmaps = _statGlyphCacheHits = _statGlyphCacheMisses = 0;
        _statKerningLookups = _statKerningPairs = 0;
        _statUnsupportedHits = _statTransientFails = 0;
        _statMeasureHits = _statMeasureMisses = _statMeasureEvictions = 0;
        IsValid = true;
        EnsureAtlasPage(DefaultAtlasSize);
    }

    // Options-based layout builder (grapheme-aware caret) + truncation + justification
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

        var span = text.AsSpan();
        var segments = wrapWidth > 0 ? WordWrapSegments(span, wrapWidth) : new List<(int start, int length)> { (0, span.Length) };

        List<StyleSpan>? sorted = null;
        if (styleSpans != null && styleSpans.Count > 0)
        {
            sorted = new List<StyleSpan>(styleSpans);
            sorted.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        int widest = 0;
        bool anyDecor = false;
        int effectiveLH = EffectiveLineHeight;

        int ellipsisWidth = truncateWidth.HasValue && truncateWidth.Value > 0
            ? MeasureRunAdvance(ellipsisStr.AsSpan(), useKerning, prewarmOnly)
            : 0;

        for (int li = 0; li < segments.Count; li++)
        {
            var (start, length) = segments[li];
            var lineSpan = span.Slice(start, length);

            // Measure line without truncation
            int lineW = 0;
            uint prevMeasure = 0;
            for (int i = 0; i < lineSpan.Length;)
            {
                var cp = NextCodepoint(lineSpan, ref i);
                int adv = GetAdvance(cp);
                if (useKerning && _kerning_enabled(prevMeasure, cp))
                    lineW += adv + GetKerning(prevMeasure, cp);
                else
                    lineW += adv;
                prevMeasure = cp;
            }

            bool doTruncate = truncateWidth.HasValue && lineW > truncateWidth.Value;
            int truncLimit = truncateWidth ?? int.MaxValue;

            // JUSTIFICATION START: collect spaces for potential expansion
            Dictionary<int, int>? justifyExtraByUtf16Index = null;
            bool canJustify = options.Justify &&
                              wrapWidth > 0 &&
                              !doTruncate &&
                              !prewarmOnly &&
                              options.Align == TextAlign.Left &&
                              li < segments.Count - 1; // avoid justifying last line

            if (canJustify && lineW < wrapWidth && lineW > 0)
            {
                int extra = wrapWidth - lineW;
                // Count candidate spaces (U+0020) excluding a trailing one
                var spacePositions = new List<int>();
                for (int i = 0; i < lineSpan.Length;)
                {
                    int localIndex = i;
                    var cp = NextCodepoint(lineSpan, ref i);
                    if (cp == ' ' && localIndex < lineSpan.Length - 1) // ignore trailing space
                        spacePositions.Add(localIndex);
                }
                if (spacePositions.Count > 0 && extra > 0)
                {
                    int baseAdd = extra / spacePositions.Count;
                    int remainder = extra % spacePositions.Count;
                    if (baseAdd > 0 || remainder > 0)
                    {
                        justifyExtraByUtf16Index = new Dictionary<int, int>(spacePositions.Count);
                        for (int si = 0; si < spacePositions.Count; si++)
                        {
                            int add = baseAdd + (si < remainder ? 1 : 0);
                            if (add > 0)
                                justifyExtraByUtf16Index[spacePositions[si]] = add;
                        }
                        lineW = wrapWidth; // target fill
                    }
                }
            }
            // JUSTIFICATION END

            int widthForAlign = doTruncate ? truncLimit : lineW;
            int alignedX = options.Align switch
            {
                TextAlign.Center => originX - (int)(widthForAlign * 0.5f * scale),
                TextAlign.Right => originX - (int)(widthForAlign * scale),
                _ => originX
            };
            int baselineY = originY + (int)(li * effectiveLH * scale);

            uint prev = 0;
            int penXUnscaled = 0;
            int siSorted = 0;
            if (sorted != null)
                while (siSorted < sorted.Count && (sorted[siSorted].Start + sorted[siSorted].Length) <= start) siSorted++;

            int lineGlyphStart = glyphs.Count;
            int lineCaretStart = caretStops.Count;

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

                    // Compute grapheme advance
                    int graphemeAdv = 0;
                    uint ptmp = prev;
                    int graphemeIter = 0;
                    int firstCp = 0;
                    bool firstCpCaptured = false;
                    while (graphemeIter < elemSub.Length)
                    {
                        int cpLocalStart = graphemeIter;
                        var cpt = NextCodepoint(elemSub, ref graphemeIter);
                        if (!firstCpCaptured) { firstCp = cpLocalStart; firstCpCaptured = true; }
                        int advt = GetAdvance(cpt);
                        if (useKerning && _kerning_enabled(ptmp, cpt))
                            graphemeAdv += advt + GetKerning(ptmp, cpt);
                        else
                            graphemeAdv += advt;
                        ptmp = cpt;
                    }

                    bool wouldOverflow = doTruncate &&
                        (penXUnscaled + graphemeAdv + ellipsisWidth > truncLimit);
                    if (wouldOverflow)
                    {
                        AppendEllipsisGlyphs(ellipsisStr.AsSpan(), alignedX, baselineY, scale,
                                             useKerning, prewarmOnly,
                                             ref penXUnscaled, ref prev,
                                             sorted, ref siSorted,
                                             glyphs, elemAbsIndex);
                        break;
                    }

                    // caret stop at grapheme start
                    caretStops.Add(new TextLayout.CaretStop(elemAbsIndex, alignedX + (int)(penXUnscaled * scale)));

                    // Emit codepoints
                    int j = 0;
                    while (j < elemSub.Length)
                    {
                        int cpStartUtf16Local = j;
                        int cpStartUtf16Abs = elemAbsIndex + cpStartUtf16Local;
                        var cp = NextCodepoint(elemSub, ref j);

                        if (useKerning && _kerning_enabled(prev, cp))
                            penXUnscaled += GetKerning(prev, cp);

                        var baseAdvance = GetAdvance(cp);
                        int extraJustify = 0;
                        // Apply justification only to single spaces (and only when not split inside cluster)
                        if (justifyExtraByUtf16Index != null && elemLength == 1 && cp == ' ' &&
                            justifyExtraByUtf16Index.TryGetValue(elemIndex, out extraJustify))
                        {
                            baseAdvance += extraJustify;
                        }

                        var g = prewarmOnly
                            ? StubGlyph((short)baseAdvance)
                            : GetGlyphCached(cp);

                        // If we modified advance for justify, reflect it in layout glyph advance (not underlying glyph)
                        short layoutAdvance = (short)baseAdvance;

                        int drawXUnscaled = penXUnscaled + g.BearingX;
                        int drawYUnscaled = _baselineOrigin
                            ? baselineY + (int)(g.MinY * scale)
                            : baselineY + (int)((Ascent + g.MinY) * scale);

                        Color glyphColor = default;
                        TextDecoration decor = TextDecoration.None;
                        bool overrideColor = false;
                        if (sorted != null)
                        {
                            while (siSorted < sorted.Count && (sorted[siSorted].Start + sorted[siSorted].Length) <= cpStartUtf16Abs) siSorted++;
                            if (siSorted < sorted.Count && sorted[siSorted].Contains(cpStartUtf16Abs))
                            {
                                glyphColor = sorted[siSorted].Color;
                                decor = sorted[siSorted].Decorations;
                                overrideColor = true;
                            }
                        }
                        if (decor != TextDecoration.None) anyDecor = true;

                        glyphs.Add(new TextLayout.LayoutGlyph(
                            cp,
                            cpStartUtf16Abs,
                            alignedX + (int)(drawXUnscaled * scale),
                            drawYUnscaled,
                            layoutAdvance,
                            this,
                            g,
                            glyphColor,
                            overrideColor,
                            decor));

                        penXUnscaled += layoutAdvance;
                        prev = cp;
                    }
                }

                if (!(doTruncate && penXUnscaled >= truncLimit))
                    caretStops.Add(new TextLayout.CaretStop(start + length, alignedX + (int)(penXUnscaled * scale)));
            }
            else
            {
                for (int i = 0; i < lineSpan.Length;)
                {
                    int cpStartUtf16Local = i;
                    int cpStartUtf16Abs = start + cpStartUtf16Local;
                    var cp = NextCodepoint(lineSpan, ref i);

                    int advWithKern = GetAdvance(cp);
                    if (useKerning && _kerning_enabled(prev, cp))
                        advWithKern += GetKerning(prev, cp);

                    bool wouldOverflow = doTruncate &&
                        (penXUnscaled + advWithKern + ellipsisWidth > truncLimit);
                    if (wouldOverflow)
                    {
                        AppendEllipsisGlyphs(ellipsisStr.AsSpan(), alignedX, baselineY, scale,
                                             useKerning, prewarmOnly,
                                             ref penXUnscaled, ref prev,
                                             sorted, ref siSorted,
                                             glyphs, start + i);
                        break;
                    }

                    // Apply justification extra to spaces
                    int extraJustify = 0;
                    if (justifyExtraByUtf16Index != null && cp == ' ' &&
                        justifyExtraByUtf16Index.TryGetValue(cpStartUtf16Local, out extraJustify))
                    {
                        advWithKern += extraJustify;
                    }

                    if (useKerning && _kerning_enabled(prev, cp))
                        penXUnscaled += GetKerning(prev, cp);

                    int finalAdvance = advWithKern;

                    var g = prewarmOnly
                        ? StubGlyph((short)finalAdvance)
                        : GetGlyphCached(cp);

                    short layoutAdvance = (short)finalAdvance;

                    int drawXUnscaled = penXUnscaled + g.BearingX;
                    int drawYUnscaled = _baselineOrigin
                        ? baselineY + (int)(g.MinY * scale)
                        : baselineY + (int)((Ascent + g.MinY) * scale);

                    Color glyphColor = default;
                    TextDecoration decor = TextDecoration.None;
                    bool overrideColor = false;
                    if (sorted != null)
                    {
                        while (siSorted < sorted.Count && (sorted[siSorted].Start + sorted[siSorted].Length) <= cpStartUtf16Abs) siSorted++;
                        if (siSorted < sorted.Count && sorted[siSorted].Contains(cpStartUtf16Abs))
                        {
                            glyphColor = sorted[siSorted].Color;
                            decor = sorted[siSorted].Decorations;
                            overrideColor = true;
                        }
                    }
                    if (decor != TextDecoration.None) anyDecor = true;

                    glyphs.Add(new TextLayout.LayoutGlyph(
                        cp,
                        cpStartUtf16Abs,
                        alignedX + (int)(drawXUnscaled * scale),
                        drawYUnscaled,
                        layoutAdvance,
                        this,
                        g,
                        glyphColor,
                        overrideColor,
                        decor));

                    penXUnscaled += layoutAdvance;
                    prev = cp;
                }
            }

            // Selection highlight
            if (hasSelection && glyphs.Count > lineGlyphStart)
            {
                int lineStart = start;
                int lineEnd = start + length;
                int selStartLine = Math.Max(lineStart, selectionStart);
                int selEndLine = Math.Min(lineEnd, selectionStart + selectionLength);
                if (selEndLine > selStartLine)
                {
                    int firstX = int.MaxValue;
                    int lastX = int.MinValue;
                    for (int gi = glyphs.Count - 1; gi >= lineGlyphStart; gi--)
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
                    {
                        selectionRects.Add(new TextLayout.HighlightRect(
                            firstX,
                            baselineY - (int)(Ascent * scale),
                            Math.Max(1, lastX - firstX),
                            (int)(effectiveLH * scale)));
                    }
                }
            }

            int finalLineW = doTruncate
                ? Math.Min(truncLimit, penXUnscaled)
                : (options.Justify && wrapWidth > 0 && !doTruncate && penXUnscaled < wrapWidth && canJustify
                    ? wrapWidth
                    : penXUnscaled);

            widest = Math.Max(widest, finalLineW);

            int glyphCount = glyphs.Count - lineGlyphStart;
            int caretCount = caretStops.Count - lineCaretStart;
            lines.Add(new TextLayout.TextLine(start, length, finalLineW, baselineY, lineGlyphStart, glyphCount, lineCaretStart, caretCount));
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
            _baselineOrigin,
            wrapWidth,
            options.Align,
            hasSelection,
            selectionColor.GetValueOrDefault(new Color(30, 120, 200, 80)),
            anyDecor,
            (int)(effectiveLH * scale));
    }

    // Back-compat overload
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
        if (!IsValid || _host.Device == null || layout.Glyphs.Count == 0) return;
        float scale = overrideScale ?? layout.Scale;

        var solid = GetSolidTexture();

        if (layout.HasSelection)
        {
            foreach (var r in layout.SelectionRects)
                sprites.Draw(solid, null, new Rectangle(r.X, r.Y, r.Width, r.Height), layout.SelectionColor);
        }

        foreach (var lg in layout.Glyphs)
        {
            var g = lg.Cached;
            if (g.Width <= 0 || g.Height <= 0) continue;
            var page = Pages[g.Page];
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
                int baselineY = lg.Y - (int)((Ascent + g.MinY) * scale);
                if ((lg.Decorations & TextDecoration.Underline) != 0)
                {
                    int underlineY = baselineY + (int)(scale * 1);
                    sprites.Draw(solid, null,
                        new Rectangle(lg.X, underlineY, dw, Math.Max(1, (int)(scale))),
                        lg.OverrideColor ? lg.Color : baseColor);
                }
                if ((lg.Decorations & TextDecoration.Strikethrough) != 0)
                {
                    int strikeY = baselineY - (int)(Ascent * 0.4f * scale);
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
        sprites.Draw(GetSolidTexture(), null, rect, color);
    }

    internal SdlTexture2D GetSolidTexture()
    {
        if (_solidTexture != null) return _solidTexture;

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
            throw new InvalidOperationException("Solid texture create failed.");

        SDL_GPUTransferBufferCreateInfo tci = default;
        tci.usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD;
        tci.size = 4;
        var tb = SDL_CreateGPUTransferBuffer(_host.Device, &tci);
        if (tb == null)
            throw new InvalidOperationException("Solid transfer buffer create failed.");

        var mapped = SDL_MapGPUTransferBuffer(_host.Device, tb, false);
        if (mapped == IntPtr.Zero)
            throw new InvalidOperationException("Map transfer buffer failed.");
        byte* p = (byte*)mapped;
        p[0] = 255; p[1] = 255; p[2] = 255; p[3] = 255;
        SDL_UnmapGPUTransferBuffer(_host.Device, tb);

        var cmd = SDL_AcquireGPUCommandBuffer(_host.Device);
        var copy = SDL_BeginGPUCopyPass(cmd);
        SDL_GPUTextureTransferInfo ti = default;
        ti.transfer_buffer = tb;
        ti.pixels_per_row = 1;
        ti.rows_per_layer = 1;
        SDL_GPUTextureRegion region = default;
        region.texture = tex;
        region.x = 0; region.y = 0; region.w = 1; region.h = 1; region.d = 1;
        SDL_UploadToGPUTexture(copy, &ti, &region, false);
        SDL_EndGPUCopyPass(copy);
        SDL_SubmitGPUCommandBuffer(cmd);

        SDL_ReleaseGPUTransferBuffer(_host.Device, tb);

        _solidTexture = new SdlTexture2D(_host.Device, tex, 1, 1, TextureFormat.R8G8B8A8_UNorm, $"{DebugName}:Solid");
        _host.RegisterResource(_solidTexture);
        return _solidTexture;
    }

    private bool _kerning_enabled(uint left, uint right) => _kerningEnabled && left != 0 && right != 0;

    private void DrawInternal(ISpriteRenderer sprites, ReadOnlySpan<char> text, int x, int y, Color color, float scale, int wrapWidth, TextAlign align)
    {
        if (!IsValid || _host.Device == null || text.Length == 0) return;

        if (wrapWidth <= 0)
        {
            int penX = x, penBaseY = y;
            uint prev = 0;
            for (int i = 0; i < text.Length;)
            {
                var cp = NextCodepoint(text, ref i);
                if (cp == '\n')
                {
                    penX = x;
                    penBaseY += (int)(EffectiveLineHeight * scale);
                    prev = 0;
                    continue;
                }
                if (_kerning_enabled(prev, cp))
                    penX += (int)(GetKerning(prev, cp) * scale);
                DrawGlyph(sprites, cp, ref penX, penBaseY, color, scale);
                prev = cp;
            }
            return;
        }

        var segments = WordWrapSegments(text, wrapWidth);
        for (int li = 0; li < segments.Count; li++)
        {
            var (start, length) = segments[li];
            var line = text.Slice(start, length);

            int lineW = 0;
            uint prevMeasure = 0;
            for (int i = 0; i < line.Length;)
            {
                var cp = NextCodepoint(line, ref i);
                int kern = _kerning_enabled(prevMeasure, cp) ? GetKerning(prevMeasure, cp) : 0;
                lineW += kern + GetAdvance(cp);
                prevMeasure = cp;
            }

            int startX = align switch
            {
                TextAlign.Center => x - (int)(lineW * 0.5f * scale),
                TextAlign.Right => x - (int)(lineW * scale),
                _ => x
            };

            int penX = startX;
            int penBaseY = y + (int)(li * EffectiveLineHeight * scale);
            uint prev = 0;

            for (int i = 0; i < line.Length;)
            {
                var cp = NextCodepoint(line, ref i);
                if (_kerning_enabled(prev, cp))
                    penX += (int)(GetKerning(prev, cp) * scale);
                DrawGlyph(sprites, cp, ref penX, penBaseY, color, scale);
                prev = cp;
            }
        }
    }

    private void DrawGlyph(ISpriteRenderer sprites, uint cp, ref int penX, int penBaseY, Color color, float scale)
    {
        var g = GetGlyphCached(cp);
        if (g.Width > 0 && g.Height > 0)
        {
            var page = _pages[g.Page];
            int gw = (int)MathF.Max(1, g.Width * scale);
            int gh = (int)MathF.Max(1, g.Height * scale);
            int drawX = penX + (int)(g.BearingX * scale);
            if (drawX < -5000)
            {
                penX += (int)(g.Advance * scale);
                return;
            }
            int drawY = _baselineOrigin
                ? penBaseY + (int)(g.MinY * scale)
                : penBaseY + (int)((Ascent + g.MinY) * scale);

            sprites.Draw(page,
                new Rectangle(g.OffsetX, g.OffsetY, g.Width, g.Height),
                new Rectangle(drawX, drawY, gw, gh),
                color);
        }
        penX += (int)(g.Advance * scale);
    }

    private List<(int start, int length)> WordWrapSegments(ReadOnlySpan<char> text, int maxWidth)
    {
        var segments = new List<(int, int)>();
        if (maxWidth <= 0)
        {
            segments.Add((0, text.Length));
            return segments;
        }
        int start = 0;
        int curW = 0;
        int lastBreakIndex = -1;
        uint prev = 0;
        for (int i = 0; i < text.Length;)
        {
            int cpIndex = i;
            var cp = NextCodepoint(text, ref i);
            if (cp == '\n')
            {
                segments.Add((start, cpIndex - start));
                start = i;
                curW = 0;
                lastBreakIndex = -1;
                prev = 0;
                continue;
            }
            int kern = _kerning_enabled(prev, cp) ? GetKerning(prev, cp) : 0;
            int adv = kern + GetAdvance(cp);
            int nextW = curW + adv;
            if (nextW > maxWidth && curW > 0)
            {
                if (lastBreakIndex >= 0)
                {
                    segments.Add((start, lastBreakIndex - start + 1));
                    i = lastBreakIndex + 1;
                    start = i;
                    curW = 0;
                    lastBreakIndex = -1;
                    prev = 0;
                }
                else
                {
                    segments.Add((start, Math.Max(1, cpIndex - start)));
                    start = cpIndex;
                    curW = 0;
                    lastBreakIndex = -1;
                    prev = 0;
                }
            }
            else
            {
                curW = nextW;
                if (IsBreakable(cp))
                    lastBreakIndex = i - 1;
                prev = cp;
            }
        }
        if (start <= text.Length)
            segments.Add((start, text.Length - start));
        return segments;
    }

    private static bool IsBreakable(uint cp) =>
        cp == ' ' || cp == '\t' || cp == '-' || cp == 0x00A0;

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

    private static bool KerningSkippable(uint cp)
    {
        if (cp == 0 || cp == ' ' || cp == '\t' || cp == '\n' || cp == 0x00A0) return true;
        if (cp <= 0x1F || (cp >= 0x7F && cp <= 0x9F)) return true;
        return false;
    }

    private int GetKerning(uint left, uint right)
    {
        if (KerningSkippable(left) || KerningSkippable(right)) return 0;
        _statKerningLookups++;
        ulong key = ((ulong)left << 32) | right;
        if (_kernCache.TryGetValue(key, out var cached))
            return cached;
        if (_kernCache.Count >= MaxKerningPairs)
            return 0;

        string pair = string.Concat(char.ConvertFromUtf32((int)left), char.ConvertFromUtf32((int)right));
        var utf8 = Encoding.UTF8.GetBytes(pair);
        int pairW = 0;
        SDL_Color white; white.r = 255; white.g = 255; white.b = 255; white.a = 255;
        SDL_Surface* surf;
        fixed (byte* p = utf8)
            surf = TTF_RenderText_Blended(_ttf.Ptr, p, (nuint)utf8.Length, white);
        if (surf != null)
        {
            var converted = SDL_ConvertSurface(surf, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
            if (converted != null)
            {
                pairW = converted->w;
                SDL_DestroySurface(converted);
            }
            SDL_DestroySurface(surf);
        }
        int kern = pairW - (GetAdvance(left) + GetAdvance(right));
        if (kern >= -1 && kern <= 1) kern = 0;
        _kernCache[key] = kern;
        _statKerningPairs++;
        return kern;
    }

    internal bool IsRenderable(uint cp)
    {
        if (cp == ' ' || cp == '\t') return true;
        if (_unsupported.Contains(cp)) return false;
        var g = GetGlyphCached(cp);
        return g.Width > 0 && g.Height > 0;
    }

    private Glyph GetGlyphCached(uint codepoint)
    {
        if (_glyphs.TryGetValue(codepoint, out var g))
        {
            _statGlyphCacheHits++;
            return g;
        }
        _statGlyphCacheMisses++;
        return CacheGlyph(codepoint);
    }

    private int GetAdvance(uint codepoint)
    {
        if (codepoint == '\t')
        {
            EnsureSpaceWidth();
            return _tabAdvance;
        }
        if (codepoint == ' ')
        {
            EnsureSpaceWidth();
            return _spaceAdvance;
        }
        return GetGlyphCached(codepoint).Advance;
    }

    private void EnsureSpaceWidth()
    {
        if (_spaceAdvance != 0 && _tabAdvance != 0) return;
        var space = GetGlyphCached(' ');
        int adv = space.Advance;
        if (adv <= 0)
            adv = space.Width > 0 ? space.Width : Math.Max(1, _ptSize / 2);
        _spaceAdvance = (short)adv;
        _tabAdvance = (short)(adv * Math.Max(1, TabSpaces));
    }

    private void EnsureAtlasPage(int size)
    {
        size = Math.Clamp(size, 64, MaxAtlasSize);
        if (MaxPages > 0 && _pages.Count >= MaxPages)
            return;

        SDL_GPUTextureCreateInfo ci = default;
        ci.type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D;
        ci.format = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;
        ci.width = (uint)size;
        ci.height = (uint)size;
        ci.layer_count_or_depth = 1;
        ci.num_levels = 1;
        ci.sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1;
        ci.usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER;

        var tex = SDL_CreateGPUTexture(_host.Device, &ci);
        if (tex == null)
            throw new InvalidOperationException($"SpriteFont atlas create failed: {SDL_GetError()}");

        var page = new SdlTexture2D(_host.Device, tex, size, size, TextureFormat.R8G8B8A8_UNorm, $"{DebugName}:Page{_pages.Count}");
        _pages.Add(page);
        _host.RegisterResource(page);

        _curX = Padding;
        _curY = Padding;
        _rowH = 0;
    }

    private void EnsureUploadBuffer(uint neededBytes)
    {
        if (_uploadBuf != null && _uploadSize >= neededBytes) return;
        if (_uploadBuf != null)
        {
            SDL_ReleaseGPUTransferBuffer(_host.Device, _uploadBuf);
            _uploadBuf = null;
            _uploadSize = 0;
        }
        SDL_GPUTransferBufferCreateInfo tci = default;
        tci.usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD;
        tci.size = neededBytes;
        _uploadBuf = SDL_CreateGPUTransferBuffer(_host.Device, &tci);
        if (_uploadBuf == null)
            throw new InvalidOperationException("SpriteFont upload buffer creation failed.");
        _uploadSize = tci.size;
    }

    private void MaybeShrinkUploadBuffer(uint currentBytes)
    {
        const uint ShrinkThreshold = 1024 * 1024;
        if (_uploadBuf != null && _uploadSize > currentBytes + ShrinkThreshold)
        {
            SDL_ReleaseGPUTransferBuffer(_host.Device, _uploadBuf);
            _uploadBuf = null;
            _uploadSize = 0;
            EnsureUploadBuffer(currentBytes);
        }
    }

    private Glyph CacheGlyph(uint codepoint)
    {
        AssertThread();
        if (!IsValid || _host.Device == null)
            return default;

        if (_unsupported.Contains(codepoint))
        {
            _statUnsupportedHits++;
            return MissingGlyphFallback();
        }

        if (codepoint <= 0x1F || (codepoint >= 0x7F && codepoint <= 0x9F))
        {
            var ctrl = new Glyph { Advance = (short)Math.Max(1, _ptSize / 3) };
            _glyphs[codepoint] = ctrl;
            return ctrl;
        }

        if (MaxPages > 0 && _pages.Count >= MaxPages)
        {
            var advLimited = (short)Math.Max(1, _ptSize / 2);
            var limited = new Glyph { Advance = advLimited };
            _glyphs[codepoint] = limited;
            return limited;
        }

        short advance = 0, bearingX = 0, minY = 0, maxY = 0;
        GetGlyphMetricsOrFallback(codepoint, out advance, out bearingX, out minY, out maxY);

        string s = char.ConvertFromUtf32((int)codepoint);
        var utf8 = Encoding.UTF8.GetBytes(s);
        SDL_Color white; white.r = 255; white.g = 255; white.b = 255; white.a = 255;
        SDL_Surface* surf;
        fixed (byte* p = utf8)
            surf = TTF_RenderText_Blended(_ttf.Ptr, p, (nuint)utf8.Length, white);

        if (surf == null)
        {
            if (_transientFailed.Contains(codepoint))
            {
                _unsupported.Add(codepoint);
                _statUnsupportedHits++;
                return MissingGlyphFallback();
            }
            _transientFailed.Add(codepoint);
            _statTransientFails++;
            return ResolveMissingChain(codepoint);
        }

        SDL_Surface* converted = null;
        try
        {
            converted = SDL_ConvertSurface(surf, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
            if (converted == null)
            {
                _transientFailed.Add(codepoint);
                _statTransientFails++;
                return ResolveMissingChain(codepoint);
            }

            int gw = converted->w;
            int gh = converted->h;
            if (gw == 0 || gh == 0)
            {
                _unsupported.Add(codepoint);
                _statUnsupportedHits++;
                return MissingGlyphFallback();
            }
            if (advance == 0)
                advance = (short)Math.Max(1, gw);

            var pageIdx = _pages.Count - 1;
            var page = _pages[pageIdx];

            if (_curX + gw + Padding > page.Width)
            {
                _curX = Padding;
                _curY += _rowH + Padding;
                _rowH = 0;
            }
            if (_curY + gh + Padding > page.Height)
            {
                EnsureAtlasPage(Math.Min(MaxAtlasSize, page.Width * 2));
                pageIdx = _pages.Count - 1;
                page = _pages[pageIdx];
            }

            uint bytes = (uint)(gw * gh * 4);
            EnsureUploadBuffer(bytes);

            var mapped = SDL_MapGPUTransferBuffer(_host.Device, _uploadBuf, false);
            if (mapped == nint.Zero)
                throw new InvalidOperationException("SpriteFont: map upload buffer failed.");

            var dst = (byte*)mapped;
            var src = (byte*)converted->pixels;
            var pitch = converted->pitch;
            for (int row = 0; row < gh; row++)
                Buffer.MemoryCopy(src + row * pitch, dst + row * (gw * 4), gw * 4, gw * 4);
            SDL_UnmapGPUTransferBuffer(_host.Device, _uploadBuf);

            var cmd = SDL_AcquireGPUCommandBuffer(_host.Device);
            if (cmd == null)
                throw new InvalidOperationException("SpriteFont: acquire command buffer failed.");
            var copy = SDL_BeginGPUCopyPass(cmd);

            SDL_GPUTextureTransferInfo srcInfo = default;
            srcInfo.transfer_buffer = _uploadBuf;
            srcInfo.pixels_per_row = (uint)gw;
            srcInfo.rows_per_layer = (uint)gh;

            SDL_GPUTextureRegion dstRegion = default;
            dstRegion.texture = page.Texture;
            dstRegion.x = (uint)_curX;
            dstRegion.y = (uint)_curY;
            dstRegion.w = (uint)gw;
            dstRegion.h = (uint)gh;
            dstRegion.d = 1;

            SDL_UploadToGPUTexture(copy, &srcInfo, &dstRegion, false);
            SDL_EndGPUCopyPass(copy);
            SDL_SubmitGPUCommandBuffer(cmd);

            var g = new Glyph
            {
                Page = (ushort)pageIdx,
                Width = (ushort)gw,
                Height = (ushort)gh,
                Advance = advance,
                BearingX = bearingX,
                MinY = minY,
                MaxY = maxY,
                OffsetX = (short)_curX,
                OffsetY = (short)_curY,
                U0 = (float)_curX / page.Width,
                V0 = (float)_curY / page.Height,
                U1 = (float)(_curX + gw) / page.Width,
                V1 = (float)(_curY + gh) / page.Height
            };

            _glyphs[codepoint] = g;
            _curX += gw + Padding;
            _rowH = Math.Max(_rowH, gh);
            MaybeShrinkUploadBuffer(bytes);
            _statGlyphBitmaps++;
            return g;
        }
        finally
        {
            if (converted != null) SDL_DestroySurface(converted);
            SDL_DestroySurface(surf);
        }
    }

    private Glyph ResolveMissingChain(uint originalCp)
    {
        if (originalCp != _missingPrimary && !_unsupported.Contains(_missingPrimary))
        {
            var g = CacheGlyph(_missingPrimary);
            if (g.Advance > 0 && g.Width > 0) return g;
            _unsupported.Add(_missingPrimary);
        }
        if (originalCp != _missingSecondary && !_unsupported.Contains(_missingSecondary))
        {
            var g2 = CacheGlyph(_missingSecondary);
            if (g2.Advance > 0 && g2.Width > 0) return g2;
            _unsupported.Add(_missingSecondary);
        }
        return MissingGlyphFallback();
    }

    private Glyph MissingGlyphFallback() =>
        new Glyph { Advance = (short)Math.Max(1, _ptSize / 2), BearingX = 0, MinY = 0, MaxY = (short)Ascent, Width = 0, Height = 0 };

    private void GetGlyphMetricsOrFallback(uint codepoint,
                                           out short advance,
                                           out short bearingX,
                                           out short minY,
                                           out short maxY)
    {
        advance = 0; bearingX = 0; minY = 0; maxY = 0;
        int minx = 0, maxx = 0, _miny = 0, _maxy = 0, adv = 0;
        try
        {
            if (TTF_GetGlyphMetrics(_ttf.Ptr, (uint)codepoint, &minx, &maxx, &_miny, &_maxy, &adv))
            {
                advance = (short)adv;
                bearingX = (short)minx;
                minY = (short)_miny;
                maxY = (short)_maxy;
            }
        }
        catch { }
    }

    internal int GetAdvanceInternal(uint cp) => GetAdvance(cp);
    internal Glyph GetGlyphInternal(uint cp) => GetGlyphCached(cp);

    private int ComputeMeasureKeyHash(string text, int maxWidth)
    {
        unchecked
        {
            int h = HashTextFNV1a(text.AsSpan());
            h = h * 31 + maxWidth;
            h = h * 31 + ExtraLineSpacing;
            h = h * 31 + TabSpaces;
            h = h * 31 + (_kerningEnabled ? 1 : 0);
            h = h * 31 + (_baselineOrigin ? 1 : 0);
            h = h * 31 + LineHeight;
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
        long oldestTick = long.MaxValue;
        int oldestKey = 0;
        bool found = false;
        foreach (var kv in _measureCache)
        {
            if (kv.Value.LastUsed < oldestTick)
            {
                oldestTick = kv.Value.LastUsed;
                oldestKey = kv.Key;
                found = true;
            }
        }
        if (found)
        {
            _measureCache.Remove(oldestKey);
            _statMeasureEvictions++;
        }
    }
    private struct MeasureEntry
    {
        public readonly string Text;
        public readonly int MaxWidthKey;
        public readonly int Width;
        public readonly int Height;
        public long LastUsed;
        public MeasureEntry(string text, int mw, int w, int h, long tick)
        {
            Text = text; MaxWidthKey = mw; Width = w; Height = h; LastUsed = tick;
        }
        public bool Matches(string text, int mw) => mw == MaxWidthKey && Text == text;
    }

    private int MeasureRunAdvance(ReadOnlySpan<char> run, bool useKerning, bool prewarmOnly)
    {
        int w = 0;
        uint prev = 0;
        for (int i = 0; i < run.Length;)
        {
            var cp = NextCodepoint(run, ref i);
            int adv = GetAdvance(cp);
            if (useKerning && _kerning_enabled(prev, cp))
                w += adv + GetKerning(prev, cp);
            else
                w += adv;
            prev = cp;
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
        List<StyleSpan>? sorted,
        ref int si,
        List<TextLayout.LayoutGlyph> glyphs,
        int sourceIndexBase)
    {
        if (ellipsis.Length == 0) return;

        Span<uint> cps = stackalloc uint[16];
        int cpCount = 0;
        for (int i = 0; i < ellipsis.Length && cpCount < cps.Length;)
            cps[cpCount++] = NextCodepoint(ellipsis, ref i);

        uint lp = prev;
        for (int i = 0; i < cpCount; i++)
        {
            var cp = cps[i];

            if (useKerning && _kerning_enabled(lp, cp))
                penXUnscaled += GetKerning(lp, cp);

            var g = prewarmOnly
                ? StubGlyph((short)GetAdvance(cp))
                : GetGlyphCached(cp);

            int drawXUnscaled = penXUnscaled + g.BearingX;
            int drawYUnscaled = _baselineOrigin
                ? baselineY + (int)(g.MinY * scale)
                : baselineY + (int)((Ascent + g.MinY) * scale);

            glyphs.Add(new TextLayout.LayoutGlyph(
                cp,
                sourceIndexBase,
                alignedX + (int)(drawXUnscaled * scale),
                drawYUnscaled,
                g.Advance,
                this,
                g,
                default,
                false,
                TextDecoration.None));

            penXUnscaled += g.Advance;
            lp = cp;
        }
        prev = lp;
    }

    private static Glyph StubGlyph(short advance) => new Glyph
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

    internal readonly struct SpriteFontStats
    {
        public readonly string Name;
        public readonly int PointSize;
        public readonly int Ascent;
        public readonly int Descent;
        public readonly int LineHeight;
        public readonly int Pages;
        public readonly int GlyphsCached;
        public readonly int KerningPairsCached;
        public readonly int KerningLookups;
        public readonly int GlyphCacheHits;
        public readonly int GlyphCacheMisses;
        public readonly int GlyphBitmapsCreated;
        public readonly int UnsupportedGlyphHits;
        public readonly int TransientGlyphFailures;
        public readonly int MeasureEntries;
        public readonly int MeasureHits;
        public readonly int MeasureMisses;
        public readonly int MeasureEvictions;
        public readonly int TabSpaces;
        public readonly int ExtraLineSpacing;
        public readonly int MaxPagesHint;
        public readonly long AtlasTotalPixels;
        public readonly long AtlasUsedPixels;
        public readonly float AtlasUsagePercent;
        public SpriteFontStats(
            string name,
            int pointSize,
            int ascent,
            int descent,
            int lineHeight,
            int pages,
            int glyphs,
            int kernPairs,
            int kernLookups,
            int cacheHits,
            int cacheMisses,
            int bitmaps,
            int unsupportedHits,
            int transientFails,
            int measureEntries,
            int measureHits,
            int measureMisses,
            int measureEvictions,
            int tabSpaces,
            int extraSpacing,
            int maxPagesHint,
            long atlasTotal,
            long atlasUsed,
            float atlasPct)
        {
            Name = name;
            PointSize = pointSize;
            Ascent = ascent;
            Descent = descent;
            LineHeight = lineHeight;
            Pages = pages;
            GlyphsCached = glyphs;
            KerningPairsCached = kernPairs;
            KerningLookups = kernLookups;
            GlyphCacheHits = cacheHits;
            GlyphCacheMisses = cacheMisses;
            GlyphBitmapsCreated = bitmaps;
            UnsupportedGlyphHits = unsupportedHits;
            TransientGlyphFailures = transientFails;
            MeasureEntries = measureEntries;
            MeasureHits = measureHits;
            MeasureMisses = measureMisses;
            MeasureEvictions = measureEvictions;
            TabSpaces = tabSpaces;
            ExtraLineSpacing = extraSpacing;
            MaxPagesHint = maxPagesHint;
            AtlasTotalPixels = atlasTotal;
            AtlasUsedPixels = atlasUsed;
            AtlasUsagePercent = atlasPct;
        }
    }

    private (long usedPixels, long totalPixels) GetAtlasUsage()
    {
        long total = 0;
        long used = 0;
        for (int i = 0; i < _pages.Count; i++)
        {
            var p = _pages[i];
            total += (long)p.Width * p.Height;
            if (i < _pages.Count - 1)
            {
                used += (long)p.Width * p.Height;
            }
            else
            {
                int usedHeight = Math.Clamp(_curY + _rowH + Padding, 0, p.Height);
                used += (long)p.Width * usedHeight;
            }
        }
        return (used, total);
    }

    internal SpriteFontStats GetStats()
    {
        var (used, total) = GetAtlasUsage();
        float pct = total > 0 ? (float)(used * 100.0 / total) : 0f;
        return new SpriteFontStats(
            DebugName,
            _ptSize,
            Ascent,
            Descent,
            LineHeight,
            _pages.Count,
            _glyphs.Count,
            _kernCache.Count,
            _statKerningLookups,
            _statGlyphCacheHits,
            _statGlyphCacheMisses,
            _statGlyphBitmaps,
            _statUnsupportedHits,
            _statTransientFails,
            _measureCache.Count,
            _statMeasureHits,
            _statMeasureMisses,
            _statMeasureEvictions,
            TabSpaces,
            ExtraLineSpacing,
            MaxPages,
            total,
            used,
            pct);
    }

    internal struct Glyph
    {
        public ushort Page;
        public ushort Width;
        public ushort Height;
        public short Advance;
        public short BearingX;
        public short MinY;
        public short MaxY;
        public short OffsetX;
        public short OffsetY;
        public float U0, V0, U1, V1;
    }
}