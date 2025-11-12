using System;
using System.Collections.Generic;
using System.Globalization;
using Brine2D.Core.Graphics;
using Brine2D.Core.Graphics.Text;
using Brine2D.Core.Math;

namespace Brine2D.SDL.Graphics.Text;

internal sealed class TextLayout
{
    public readonly IReadOnlyList<TextLine> Lines;
    public readonly IReadOnlyList<LayoutGlyph> Glyphs;
    public readonly IReadOnlyList<HighlightRect> SelectionRects;
    public readonly IReadOnlyList<CaretStop> CaretStops;

    public readonly int Width;
    public readonly int Height;
    public readonly float Scale;
    public readonly int OriginX;
    public readonly int OriginY;
    public readonly bool BaselineOrigin;
    public readonly int MaxWidth;
    public readonly TextAlign Align;
    public readonly bool HasSelection;
    public readonly Color SelectionColor;
    public readonly bool HasAnyDecorations;
    public readonly int LineHeight; // scaled line height (pixels)

    internal TextLayout(
        List<TextLine> lines,
        List<LayoutGlyph> glyphs,
        List<HighlightRect> selectionRects,
        List<CaretStop> caretStops,
        int width,
        int height,
        float scale,
        int originX,
        int originY,
        bool baselineOrigin,
        int maxWidth,
        TextAlign align,
        bool hasSelection,
        Color selectionColor,
        bool hasAnyDecorations,
        int lineHeight)
    {
        Lines = lines;
        Glyphs = glyphs;
        SelectionRects = selectionRects;
        CaretStops = caretStops;
        Width = width;
        Height = height;
        Scale = scale;
        OriginX = originX;
        OriginY = originY;
        BaselineOrigin = baselineOrigin;
        MaxWidth = maxWidth;
        Align = align;
        HasSelection = hasSelection;
        SelectionColor = selectionColor;
        HasAnyDecorations = hasAnyDecorations;
        LineHeight = Math.Max(1, lineHeight);
    }

    internal readonly struct TextLine
    {
        public readonly int StartIndex;    // index into source string (UTF-16)
        public readonly int Length;        // characters in this line segment
        public readonly int Width;         // unscaled line width
        public readonly int BaselineY;     // final pixel baseline Y (scaled)
        public readonly int GlyphStart;    // start index into Glyphs
        public readonly int GlyphCount;    // number of glyphs covering this line
        public readonly int CaretStart;    // start index into CaretStops
        public readonly int CaretCount;    // number of caret stops for this line

        public TextLine(int start, int length, int width, int baselineY, int glyphStart, int glyphCount, int caretStart, int caretCount)
        {
            StartIndex = start;
            Length = length;
            Width = width;
            BaselineY = baselineY;
            GlyphStart = glyphStart;
            GlyphCount = glyphCount;
            CaretStart = caretStart;
            CaretCount = caretCount;
        }
    }

    [Flags]
    internal enum TextDecoration : byte
    {
        None = 0,
        Underline = 1 << 0,
        Strikethrough = 1 << 1
    }

    internal readonly struct LayoutGlyph
    {
        public readonly uint Codepoint;
        public readonly int SourceIndex;   // UTF-16 start index for this codepoint
        public readonly int X;             // final pixel X (aligned & scaled; includes bearing)
        public readonly int Y;             // final pixel Y (top-left)
        public readonly short Advance;     // unscaled advance
        public readonly SpriteFont Font;   // owning font
        public readonly SpriteFont.Glyph Cached;
        public readonly Color Color;
        public readonly bool OverrideColor;
        public readonly TextDecoration Decorations;

        public LayoutGlyph(uint cp, int srcIndex, int x, int y, short advance,
                           SpriteFont font, SpriteFont.Glyph cached,
                           Color color, bool overrideColor, TextDecoration decorations)
        {
            Codepoint = cp;
            SourceIndex = srcIndex;
            X = x;
            Y = y;
            Advance = advance;
            Font = font;
            Cached = cached;
            Color = color;
            OverrideColor = overrideColor;
            Decorations = decorations;
        }
    }

    internal readonly struct HighlightRect
    {
        public readonly int X, Y, Width, Height;
        public HighlightRect(int x, int y, int width, int height)
        {
            X = x; Y = y; Width = width; Height = height;
        }
    }

    internal readonly struct StyleSpan
    {
        public readonly int Start;
        public readonly int Length;
        public readonly Color Color;
        public readonly TextDecoration Decorations;
        public StyleSpan(int start, int length, Color color, TextDecoration decorations = TextDecoration.None)
        {
            Start = start;
            Length = length;
            Color = color;
            Decorations = decorations;
        }
        public bool Contains(int index) => index >= Start && index < Start + Length;
    }

    internal readonly struct CaretStop
    {
        public readonly int Index; // UTF-16 caret index
        public readonly int X;     // pixel x position
        public CaretStop(int index, int x) { Index = index; X = x; }
    }

    public readonly struct HitTestResult
    {
        public readonly int Index;      // UTF-16 caret index into original string
        public readonly int LineIndex;  // line number
        public readonly int CaretX;     // caret X (pixels)
        public readonly int CaretY;     // caret top Y (pixels)
        public HitTestResult(int index, int lineIndex, int caretX, int caretY)
        {
            Index = index; LineIndex = lineIndex; CaretX = caretX; CaretY = caretY;
        }
        public bool IsValid => Index >= 0 && LineIndex >= 0;
    }

    public HitTestResult HitTestPoint(int x, int y)
    {
        if (Lines.Count == 0)
            return new HitTestResult(0, 0, OriginX, OriginY);

        float lf = (y - OriginY) / (float)LineHeight;
        int li = (int)MathF.Round(lf);
        if (li < 0) li = 0; else if (li >= Lines.Count) li = Lines.Count - 1;

        var line = Lines[li];

        // Prefer grapheme-aware caret stops when available
        if (line.CaretCount > 0)
        {
            int s = line.CaretStart;
            int e = s + line.CaretCount;
            var first = CaretStops[s];
            if (line.CaretCount == 1)
            {
                int caretTop1 = line.BaselineY - LineHeight + 1;
                return new HitTestResult(first.Index, li, first.X, caretTop1);
            }
            for (int i = s + 1; i < e; i++)
            {
                var a = CaretStops[i - 1];
                var b = CaretStops[i];
                int mid = a.X + ((b.X - a.X) >> 1);
                if (x < mid)
                {
                    int caretTop = line.BaselineY - LineHeight + 1;
                    return new HitTestResult(a.Index, li, a.X, caretTop);
                }
            }
            var last = CaretStops[e - 1];
            int top = line.BaselineY - LineHeight + 1;
            return new HitTestResult(last.Index, li, last.X, top);
        }

        // Fallback: per-glyph midpoint
        int giStart = line.GlyphStart;
        int giEnd = giStart + line.GlyphCount;

        if (line.GlyphCount == 0)
        {
            int caretTop = line.BaselineY - LineHeight + 1;
            return new HitTestResult(line.StartIndex, li, OriginX, caretTop);
        }

        int closestIndex = line.StartIndex;
        int caretX = Glyphs[giStart].X;
        int scaledAdvance = (int)(Glyphs[giStart].Advance * Scale);
        int prevX = caretX;

        for (int gi = giStart; gi < giEnd; gi++)
        {
            var g = Glyphs[gi];
            int advScaled = Math.Max(1, (int)(g.Advance * Scale));
            int mid = g.X + advScaled / 2;

            if (x < mid)
            {
                closestIndex = g.SourceIndex;
                caretX = g.X;
                int caretTop = line.BaselineY - LineHeight + 1;
                return new HitTestResult(closestIndex, li, caretX, caretTop);
            }

            closestIndex = g.SourceIndex + (g.Codepoint > 0xFFFF ? 2 : 1);
            prevX = g.X;
            scaledAdvance = advScaled;
        }

        caretX = prevX + scaledAdvance;
        int top2 = line.BaselineY - LineHeight + 1;
        return new HitTestResult(closestIndex, li, caretX, top2);
    }

    public Rectangle GetCaretRect(int index, int thickness = 1)
    {
        if (Lines.Count == 0) return new Rectangle(OriginX, OriginY, Math.Max(1, thickness), LineHeight);

        int li = 0;
        for (; li < Lines.Count; li++)
        {
            var ln = Lines[li];
            if (index < ln.StartIndex + ln.Length)
            {
                if (index < ln.StartIndex) index = ln.StartIndex;
                break;
            }
        }
        if (li >= Lines.Count) li = Lines.Count - 1;
        var line = Lines[li];

        int caretX;

        if (line.CaretCount > 0)
        {
            // Snap caret to the nearest defined stop at or after 'index'
            int s = line.CaretStart;
            int e = s + line.CaretCount;
            int chosenX = CaretStops[e - 1].X;
            for (int i = s; i < e; i++)
            {
                var cs = CaretStops[i];
                if (index <= cs.Index)
                {
                    chosenX = cs.X;
                    break;
                }
            }
            caretX = chosenX;
        }
        else
        {
            // Fallback to glyph mapping
            int giStart = line.GlyphStart;
            int giEnd = giStart + line.GlyphCount;
            if (line.GlyphCount == 0)
            {
                caretX = OriginX;
            }
            else
            {
                var first = Glyphs[giStart];
                if (index <= first.SourceIndex)
                {
                    caretX = first.X;
                }
                else
                {
                    caretX = first.X;
                    for (int gi = giStart; gi < giEnd; gi++)
                    {
                        var g = Glyphs[gi];
                        if (index <= g.SourceIndex)
                        {
                            caretX = g.X;
                            break;
                        }
                        int advScaled = Math.Max(1, (int)(g.Advance * Scale));
                        caretX = g.X + advScaled;
                    }
                }
            }
        }

        int top = line.BaselineY - LineHeight + 1;
        return new Rectangle(caretX, top, Math.Max(1, thickness), LineHeight);
    }

    public readonly struct BuildLayoutOptions
    {
        public readonly int? MaxWidth;
        public readonly TextAlign Align;
        public readonly float Scale;
        public readonly bool UseGraphemeClusters;
        public readonly bool Justify;
        public readonly int? TruncateWidth;
        public readonly string? Ellipsis;
        public readonly bool PrewarmOnly;
        public readonly bool DisableKerning;
        public BuildLayoutOptions(
            int? maxWidth,
            TextAlign align = TextAlign.Left,
            float scale = 1f,
            bool useGraphemeClusters = false,
            bool justify = false,
            int? truncateWidth = null,
            string? ellipsis = null,
            bool prewarmOnly = false,
            bool disableKerning = false)
        {
            MaxWidth = maxWidth; Align = align; Scale = scale;
            UseGraphemeClusters = useGraphemeClusters;
            Justify = justify; TruncateWidth = truncateWidth; Ellipsis = ellipsis;
            PrewarmOnly = prewarmOnly; DisableKerning = disableKerning;
        }
    }
}