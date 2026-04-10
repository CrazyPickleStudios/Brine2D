using Brine2D.Core;
using Brine2D.Rendering.Text;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace Brine2D.Rendering;

internal sealed partial class SDL3Renderer
{
    public void DrawText(string text, float x, float y, Color color)
    {
        DrawText(text, x, y, new TextRenderOptions
        {
            Color = color
        });
    }

    public void DrawText(string text, float x, float y, TextRenderOptions options)
    {
        ThrowIfNotInitialized();

        if (!_frameManager.HasActiveFrame)
            return;

        if (string.IsNullOrEmpty(text))
            return;

        if (!options.ParseMarkup)
        {
            RenderPlainText(text, x, y, options);
            return;
        }

        var runs = _textRenderer.ParseText(text, options);
        RenderTextRuns(runs, x, y, options);
    }

    public Vector2 MeasureText(string text, float? fontSize = null)
    {
        return _textRenderer.MeasureText(text, fontSize);
    }

    public Vector2 MeasureText(string text, TextRenderOptions options)
    {
        return _textRenderer.MeasureTextWithOptions(text, options);
    }

    public void SetDefaultFont(IFont? font)
    {
        _textRenderer.SetDefaultFont(font);
    }

    private void RenderTextRuns(
    IReadOnlyList<TextRun> runs,
    float x,
    float y,
    TextRenderOptions options)
    {
        if (runs.Count == 0)
            return;

        _textRenderer.EnsureFontAtlasGenerated(this);

        if (_textRenderer.DefaultFontAtlas == null || _textRenderer.DefaultFontAtlas.Texture == null)
        {
            _logger.LogWarning("No font atlas available");
            return;
        }

        float defaultFontSize = _textRenderer.DefaultFont!.Size;
        float baseScale = options.FontSize / defaultFontSize;

        float maxScale = baseScale;
        for (int i = 0; i < runs.Count; i++)
        {
            if (!string.IsNullOrEmpty(runs[i].Text))
                maxScale = MathF.Max(maxScale, runs[i].FontSize / defaultFontSize);
        }

        var textSize = _textRenderer.MeasureTextRuns(runs, options.LineSpacing);

        float startX = x;
        float startY = y;

        bool fitsWithoutWrapping = !options.MaxWidth.HasValue || textSize.X <= options.MaxWidth.Value;
        bool needsWrapping = options.MaxWidth.HasValue && !fitsWithoutWrapping;

        if (options.MaxHeight.HasValue)
        {
            float totalHeight = fitsWithoutWrapping ? textSize.Y : lineHeight(maxScale, options) * EstimateWrappedLineCount(runs, x, options, defaultFontSize);
            startY = options.VerticalAlign switch
            {
                VerticalAlignment.Middle => y + (options.MaxHeight.Value - Math.Min(totalHeight, options.MaxHeight.Value)) / 2,
                VerticalAlignment.Bottom => y + options.MaxHeight.Value - Math.Min(totalHeight, options.MaxHeight.Value),
                _ => y
            };
        }

        float cursorX = startX;
        float cursorY = startY;
        float lh = lineHeight(maxScale, options);

        var atlasTexture = _textRenderer.DefaultFontAtlas.Texture;

        if (options.MaxWidth.HasValue && options.HorizontalAlign != TextAlignment.Left)
        {
            RenderRunsWithAlignedWrapping(runs, x, cursorY, lh, options, atlasTexture, defaultFontSize, y);
        }
        else
        {
            for (int i = 0; i < runs.Count; i++)
            {
                var run = runs[i];
                if (string.IsNullOrEmpty(run.Text))
                    continue;

                if (run.Font != null && !_customFontWarningLogged)
                {
                    _logger.LogWarning("Custom font on TextRun is not yet supported; falling back to default font");
                    _customFontWarningLogged = true;
                }

                if ((run.Style & (TextStyle.Bold | TextStyle.Italic)) != 0 && !_boldItalicWarningLogged)
                {
                    _logger.LogWarning("Bold/Italic text styles are not yet rendered; text will appear as normal weight");
                    _boldItalicWarningLogged = true;
                }

                float runScale = run.FontSize / defaultFontSize;

                if (needsWrapping)
                    RenderRunWithWrapping(run, ref cursorX, ref cursorY, startX, lh, options, atlasTexture, runScale, y);
                else
                    RenderRunDirect(run, ref cursorX, ref cursorY, startX, lh, options, atlasTexture, runScale, y);
            }
        }

        float lineHeight(float scale, TextRenderOptions opts) =>
            _textRenderer.DefaultFontAtlas!.LineHeight * scale * opts.LineSpacing;
        return;

        int EstimateWrappedLineCount(IReadOnlyList<TextRun> r, float originX, TextRenderOptions opts, float defFontSize)
        {
            int lines = 1;
            float cx = originX;
            for (int i = 0; i < r.Count; i++)
            {
                if (string.IsNullOrEmpty(r[i].Text)) continue;
                float rs = r[i].FontSize / defFontSize;
                var span = r[i].Text.AsSpan();
                int ws = 0;
                while (ws < span.Length)
                {
                    if (span[ws] == '\n')
                    {
                        cx = originX;
                        lines++;
                        ws++;
                        continue;
                    }

                    var rem = span[ws..];
                    int si = rem.IndexOf(' ');
                    int ni = rem.IndexOf('\n');
                    ReadOnlySpan<char> word;
                    bool atEnd;
                    if (si >= 0 && (ni < 0 || si < ni))
                    {
                        word = rem[..(si + 1)];
                        atEnd = false;
                    }
                    else if (ni >= 0)
                    {
                        word = rem[..ni];
                        atEnd = false;
                    }
                    else
                    {
                        word = rem;
                        atEnd = true;
                    }

                    var sz = _textRenderer.MeasureGlyphSpan(word, rs);
                    if (cx + sz.X > originX + opts.MaxWidth!.Value && cx > originX)
                    {
                        cx = originX;
                        lines++;
                    }
                    cx += sz.X;
                    ws += word.Length;
                    if (atEnd) break;
                }
            }
            return lines;
        }
    }

    private void RenderRunsWithAlignedWrapping(
        IReadOnlyList<TextRun> runs,
        float startX,
        float startY,
        float lineHeight,
        TextRenderOptions options,
        ITexture atlasTexture,
        float defaultFontSize,
        float originY)
    {
        float maxWidth = options.MaxWidth!.Value;
        bool hasMaxHeight = options.MaxHeight.HasValue && originY > float.NegativeInfinity;
        float maxY = hasMaxHeight ? originY + options.MaxHeight.Value : float.PositiveInfinity;

        var logicalLines = new List<List<(int RunIndex, int Start, int Length)>>();
        var currentLine = new List<(int RunIndex, int Start, int Length)>();
        float cx = startX;

        for (int ri = 0; ri < runs.Count; ri++)
        {
            if (string.IsNullOrEmpty(runs[ri].Text))
                continue;

            float runScale = runs[ri].FontSize / defaultFontSize;
            var text = runs[ri].Text.AsSpan();
            int segStart = 0;
            int pos = 0;

            while (pos < text.Length)
            {
                if (text[pos] == '\n')
                {
                    if (pos > segStart)
                        currentLine.Add((ri, segStart, pos - segStart));

                    logicalLines.Add(currentLine);
                    currentLine = new List<(int, int, int)>();
                    pos++;
                    segStart = pos;
                    cx = startX;
                    continue;
                }

                var remaining = text[pos..];
                int spaceIdx = remaining.IndexOf(' ');
                int nlIdx = remaining.IndexOf('\n');
                ReadOnlySpan<char> word;
                bool atEnd;
                if (spaceIdx >= 0 && (nlIdx < 0 || spaceIdx < nlIdx))
                {
                    word = remaining[..(spaceIdx + 1)];
                    atEnd = false;
                }
                else if (nlIdx >= 0)
                {
                    word = remaining[..nlIdx];
                    atEnd = false;
                }
                else
                {
                    word = remaining;
                    atEnd = true;
                }

                var wordSize = _textRenderer.MeasureGlyphSpan(word, runScale);
                if (cx + wordSize.X > startX + maxWidth && cx > startX)
                {
                    if (pos > segStart)
                        currentLine.Add((ri, segStart, pos - segStart));

                    logicalLines.Add(currentLine);
                    currentLine = new List<(int, int, int)>();
                    segStart = pos;
                    cx = startX;
                }

                cx += wordSize.X;
                pos += word.Length;
                if (atEnd) break;
            }

            if (pos > segStart)
                currentLine.Add((ri, segStart, pos - segStart));
        }

        if (currentLine.Count > 0)
            logicalLines.Add(currentLine);

        float cursorY = startY;
        foreach (var line in logicalLines)
        {
            if (hasMaxHeight && cursorY + lineHeight > maxY)
                return;

            float totalWidth = 0;
            foreach (var (runIndex, charStart, charLength) in line)
            {
                float runScale = runs[runIndex].FontSize / defaultFontSize;
                totalWidth += _textRenderer.MeasureGlyphSpan(
                    runs[runIndex].Text.AsSpan().Slice(charStart, charLength), runScale).X;
            }

            float lineOffsetX = options.HorizontalAlign switch
            {
                TextAlignment.Center => startX + (maxWidth - totalWidth) / 2,
                TextAlignment.Right => startX + maxWidth - totalWidth,
                _ => startX
            };

            float cursorX = lineOffsetX;

            foreach (var (runIndex, charStart, charLength) in line)
            {
                var run = runs[runIndex];
                if (run.Font != null && !_customFontWarningLogged)
                {
                    _logger.LogWarning("Custom font on TextRun is not yet supported; falling back to default font");
                    _customFontWarningLogged = true;
                }

                if ((run.Style & (TextStyle.Bold | TextStyle.Italic)) != 0 && !_boldItalicWarningLogged)
                {
                    _logger.LogWarning("Bold/Italic text styles are not yet rendered; text will appear as normal weight");
                    _boldItalicWarningLogged = true;
                }

                float runScale = run.FontSize / defaultFontSize;
                RenderRunDirect(
                    run.Text.AsSpan().Slice(charStart, charLength), run.Color, run.Style,
                    ref cursorX, ref cursorY, lineOffsetX, lineHeight, options, atlasTexture, runScale, originY);
            }

            cursorY += lineHeight;
        }
    }

    private void RenderPlainText(string text, float x, float y, TextRenderOptions options)
    {
        _textRenderer.EnsureFontAtlasGenerated(this);

        if (_textRenderer.DefaultFontAtlas == null || _textRenderer.DefaultFontAtlas.Texture == null)
        {
            _logger.LogWarning("No font atlas available");
            return;
        }

        float defaultFontSize = _textRenderer.DefaultFont!.Size;
        float glyphScale = options.FontSize / defaultFontSize;

        float startX = x;
        float startY = y;
        float lineHeight = _textRenderer.DefaultFontAtlas.LineHeight * glyphScale * options.LineSpacing;
        bool needsWrapping = false;

        if (options.MaxWidth.HasValue || options.MaxHeight.HasValue)
        {
            var textSize = _textRenderer.MeasureText(text, options.FontSize, options.LineSpacing);
            bool fitsWithoutWrapping = !options.MaxWidth.HasValue || textSize.X <= options.MaxWidth.Value;
            needsWrapping = options.MaxWidth.HasValue && !fitsWithoutWrapping;

            float effectiveHeight = fitsWithoutWrapping
                ? textSize.Y
                : lineHeight * EstimateWrappedLineCount(text.AsSpan(), x, options, glyphScale);

            if (options.MaxHeight.HasValue)
            {
                startY = options.VerticalAlign switch
                {
                    VerticalAlignment.Middle => y + (options.MaxHeight.Value - Math.Min(effectiveHeight, options.MaxHeight.Value)) / 2,
                    VerticalAlignment.Bottom => y + options.MaxHeight.Value - Math.Min(effectiveHeight, options.MaxHeight.Value),
                    _ => y
                };
            }
        }

        var run = new TextRun
        {
            Text = text,
            Color = options.Color,
            Font = options.Font,
            FontSize = options.FontSize,
        };

        float cursorX = startX;
        float cursorY = startY;
        var atlasTexture = _textRenderer.DefaultFontAtlas.Texture;

        if (needsWrapping || (options.MaxWidth.HasValue && options.HorizontalAlign != TextAlignment.Left))
            RenderRunWithWrapping(run, ref cursorX, ref cursorY, startX, lineHeight, options, atlasTexture, glyphScale, y);
        else
            RenderRunDirect(run, ref cursorX, ref cursorY, startX, lineHeight, options, atlasTexture, glyphScale, y);

        int EstimateWrappedLineCount(ReadOnlySpan<char> span, float originX, TextRenderOptions opts, float scale)
        {
            int lines = 1;
            float cx = originX;
            int ws = 0;
            while (ws < span.Length)
            {
                if (span[ws] == '\n')
                {
                    cx = originX;
                    lines++;
                    ws++;
                    continue;
                }

                var rem = span[ws..];
                int si = rem.IndexOf(' ');
                int ni = rem.IndexOf('\n');
                ReadOnlySpan<char> word;
                bool atEnd;
                if (si >= 0 && (ni < 0 || si < ni))
                {
                    word = rem[..(si + 1)];
                    atEnd = false;
                }
                else if (ni >= 0)
                {
                    word = rem[..ni];
                    atEnd = false;
                }
                else
                {
                    word = rem;
                    atEnd = true;
                }

                var sz = _textRenderer.MeasureGlyphSpan(word, scale);
                if (cx + sz.X > originX + opts.MaxWidth!.Value && cx > originX)
                {
                    cx = originX;
                    lines++;
                }
                cx += sz.X;
                ws += word.Length;
                if (atEnd) break;
            }
            return lines;
        }
    }

    private void RenderRunDirect(
        TextRun run,
        ref float cursorX,
        ref float cursorY,
        float startX,
        float lineHeight,
        TextRenderOptions options,
        ITexture atlasTexture,
        float glyphScale,
        float originY = float.NegativeInfinity)
    {
        RenderRunDirect(run.Text.AsSpan(), run.Color, run.Style,
            ref cursorX, ref cursorY, startX, lineHeight, options, atlasTexture, glyphScale, originY);
    }

    private void RenderRunDirect(
        ReadOnlySpan<char> text,
        Color color,
        TextStyle style,
        ref float cursorX,
        ref float cursorY,
        float startX,
        float lineHeight,
        TextRenderOptions options,
        ITexture atlasTexture,
        float glyphScale,
        float originY = float.NegativeInfinity)
    {
        if (options.MaxHeight.HasValue && originY > float.NegativeInfinity &&
            cursorY + lineHeight > originY + options.MaxHeight.Value)
            return;

        bool needDecorations = (style & (TextStyle.Underline | TextStyle.Strikethrough)) != 0;

        if (options.ShadowOffset.HasValue)
        {
            float shadowStartX = cursorX;
            float shadowStartY = cursorY;
            float shadowLineStartX = startX + options.ShadowOffset.Value.X;
            float shadowX = cursorX + options.ShadowOffset.Value.X;
            float shadowY = cursorY + options.ShadowOffset.Value.Y;
            float? shadowMaxY = options.MaxHeight.HasValue && originY > float.NegativeInfinity
                ? originY + options.MaxHeight.Value : null;

            if (needDecorations)
            {
                float lastShadowSegStartX = shadowX;
                RenderGlyphs(text, shadowX, shadowY,
                             options.ShadowColor, atlasTexture,
                             ref cursorX, ref cursorY, shadowLineStartX, lineHeight, true, glyphScale,
                             onLineBreak: (segStartX, segEndX, segY) =>
                             {
                                 DrawDecorations(style, segStartX, segEndX, segY, lineHeight, options.ShadowColor);
                                 lastShadowSegStartX = shadowLineStartX;
                             },
                             maxY: shadowMaxY);
                DrawDecorations(style, lastShadowSegStartX, cursorX, cursorY, lineHeight, options.ShadowColor);
            }
            else
            {
                RenderGlyphs(text, shadowX, shadowY,
                             options.ShadowColor, atlasTexture,
                             ref cursorX, ref cursorY, shadowLineStartX, lineHeight, false, glyphScale,
                             maxY: shadowMaxY);
            }

            cursorX = shadowStartX;
            cursorY = shadowStartY;
        }

        if (needDecorations)
        {
            float lastSegStartX = cursorX;

            RenderGlyphs(text, cursorX, cursorY, color, atlasTexture,
                         ref cursorX, ref cursorY, startX, lineHeight, true, glyphScale,
                         onLineBreak: (segStartX, segEndX, segY) =>
                         {
                             DrawDecorations(style, segStartX, segEndX, segY, lineHeight, color);
                             lastSegStartX = startX;
                         },
                         maxY: options.MaxHeight.HasValue && originY > float.NegativeInfinity
                             ? originY + options.MaxHeight.Value : null);

            DrawDecorations(style, lastSegStartX, cursorX, cursorY, lineHeight, color);
        }
        else
        {
            RenderGlyphs(text, cursorX, cursorY, color, atlasTexture,
                         ref cursorX, ref cursorY, startX, lineHeight, true, glyphScale,
                         maxY: options.MaxHeight.HasValue && originY > float.NegativeInfinity
                             ? originY + options.MaxHeight.Value : null);
        }
    }

    private void DrawDecorations(TextStyle style, float x1, float x2, float y, float lineHeight, Color color)
    {
        if (x2 <= x1)
            return;

        if ((style & TextStyle.Underline) != 0)
        {
            float underlineY = y + lineHeight - 2;
            DrawLine(x1, underlineY, x2, underlineY, color, 1f);
        }

        if ((style & TextStyle.Strikethrough) != 0)
        {
            float strikeY = y + lineHeight / 2;
            DrawLine(x1, strikeY, x2, strikeY, color, 1f);
        }
    }

    private void RenderRunWithWrapping(
        TextRun run,
        ref float cursorX,
        ref float cursorY,
        float startX,
        float lineHeight,
        TextRenderOptions options,
        ITexture atlasTexture,
        float glyphScale,
        float originY = float.NegativeInfinity)
    {
        var text = run.Text.AsSpan();
        float maxWidth = options.MaxWidth!.Value;
        bool needsAlignment = options.HorizontalAlign != TextAlignment.Left;
        bool hasMaxHeight = options.MaxHeight.HasValue && originY > float.NegativeInfinity;
        float maxY = hasMaxHeight ? originY + options.MaxHeight.Value : float.PositiveInfinity;

        if (!needsAlignment)
        {
            int wordStart = 0;
            while (wordStart < text.Length)
            {
                if (hasMaxHeight && cursorY + lineHeight > maxY)
                    return;

                if (text[wordStart] == '\n')
                {
                    cursorX = startX;
                    cursorY += lineHeight;
                    wordStart++;
                    continue;
                }

                var remaining = text[wordStart..];
                int spaceIdx = remaining.IndexOf(' ');
                int nlIdx = remaining.IndexOf('\n');
                ReadOnlySpan<char> word;
                bool atEnd;
                if (spaceIdx >= 0 && (nlIdx < 0 || spaceIdx < nlIdx))
                {
                    word = remaining[..(spaceIdx + 1)];
                    atEnd = false;
                }
                else if (nlIdx >= 0)
                {
                    word = remaining[..nlIdx];
                    atEnd = false;
                }
                else
                {
                    word = remaining;
                    atEnd = true;
                }

                var wordSize = _textRenderer.MeasureGlyphSpan(word, glyphScale);
                if (cursorX + wordSize.X > startX + maxWidth && cursorX > startX)
                {
                    cursorX = startX;
                    cursorY += lineHeight;

                    if (hasMaxHeight && cursorY + lineHeight > maxY)
                        return;
                }

                RenderRunDirect(word, run.Color, run.Style,
                    ref cursorX, ref cursorY, startX, lineHeight, options, atlasTexture, glyphScale, originY);

                wordStart += word.Length;
                if (atEnd) break;
            }
            return;
        }

        var lines = new List<(int Start, int Length)>();
        float cx = cursorX;
        int lineStart = 0;

        int ws = 0;
        while (ws < text.Length)
        {
            if (text[ws] == '\n')
            {
                lines.Add((lineStart, ws - lineStart));
                ws++;
                lineStart = ws;
                cx = startX;
                continue;
            }

            var remaining = text[ws..];
            int spaceIdx = remaining.IndexOf(' ');
            int nlIdx = remaining.IndexOf('\n');
            ReadOnlySpan<char> word;
            bool atEnd;
            if (spaceIdx >= 0 && (nlIdx < 0 || spaceIdx < nlIdx))
            {
                word = remaining[..(spaceIdx + 1)];
                atEnd = false;
            }
            else if (nlIdx >= 0)
            {
                word = remaining[..nlIdx];
                atEnd = false;
            }
            else
            {
                word = remaining;
                atEnd = true;
            }

            var wordSize = _textRenderer.MeasureGlyphSpan(word, glyphScale);
            if (cx + wordSize.X > startX + maxWidth && cx > startX)
            {
                lines.Add((lineStart, ws - lineStart));
                lineStart = ws;
                cx = startX;
            }

            cx += wordSize.X;
            ws += word.Length;
            if (atEnd) break;
        }

        if (ws > lineStart)
            lines.Add((lineStart, ws - lineStart));

        foreach (var (start, length) in lines)
        {
            if (hasMaxHeight && cursorY + lineHeight > maxY)
                return;

            var lineSpan = text.Slice(start, length);
            var lineSize = _textRenderer.MeasureGlyphSpan(lineSpan, glyphScale);

            float lineOffsetX = options.HorizontalAlign switch
            {
                TextAlignment.Center => startX + (maxWidth - lineSize.X) / 2,
                TextAlignment.Right => startX + maxWidth - lineSize.X,
                _ => startX
            };

            cursorX = lineOffsetX;
            RenderRunDirect(lineSpan, run.Color, run.Style,
                ref cursorX, ref cursorY, lineOffsetX, lineHeight, options, atlasTexture, glyphScale, originY);

            cursorX = startX;
            cursorY += lineHeight;
        }
    }

    /// <param name="onLineBreak">
    /// Optional callback invoked just before a newline resets the cursor.
    /// Receives (lineStartX, lineEndX, lineY) for the line segment that just ended.
    /// Used by callers that need per-line decoration (underline, strikethrough).
    /// </param>
    /// <param name="maxY">
    /// If set, glyphs on lines whose top exceeds this Y coordinate are not emitted.
    /// </param>
    private void RenderGlyphs(
        ReadOnlySpan<char> text,
        float x,
        float y,
        Color color,
        ITexture atlasTexture,
        ref float cursorX,
        ref float cursorY,
        float lineStartX,
        float lineHeight,
        bool advanceCursor,
        float glyphScale,
        Action<float, float, float>? onLineBreak = null,
        float? maxY = null)
    {
        float localCursorX = x;
        float localCursorY = y;
        float lineSegStartX = x;
        var atlasTextureHandle = GetTextureHandle(atlasTexture);

        foreach (char c in text)
        {
            if (c == '\n')
            {
                onLineBreak?.Invoke(lineSegStartX, localCursorX, localCursorY);

                if (advanceCursor)
                {
                    cursorX = lineStartX;
                    cursorY += lineHeight;
                }
                localCursorX = lineStartX;
                localCursorY += lineHeight;
                lineSegStartX = lineStartX;

                if (maxY.HasValue && localCursorY + lineHeight > maxY.Value)
                    break;

                continue;
            }

            if (maxY.HasValue && localCursorY + lineHeight > maxY.Value)
                break;

            if (!_textRenderer.DefaultFontAtlas!.TryGetGlyph(c, out var glyph))
                continue;

            float scaledW = glyph.Width * glyphScale;
            float scaledH = glyph.Height * glyphScale;

            // SDL_ttf 3 RenderGlyphBlended produces full font-height surfaces with
            // glyphs already positioned at the correct baseline offset within the
            // surface. Bearing offsets must NOT be applied here or they double-count.
            float u1 = glyph.AtlasX / (float)atlasTexture.Width;
            float v1 = glyph.AtlasY / (float)atlasTexture.Height;
            float u2 = (glyph.AtlasX + glyph.Width) / (float)atlasTexture.Width;
            float v2 = (glyph.AtlasY + glyph.Height) / (float)atlasTexture.Height;

            _batchRenderer.DrawTexturedQuad(
                atlasTextureHandle,
                atlasTexture.ScaleMode,
                localCursorX,
                localCursorY,
                scaledW,
                scaledH,
                color,
                u1, v1, u2, v2,
                onFlushNeeded: _flushBatchAction,
                textureRef: atlasTexture);

            localCursorX += glyph.Advance * glyphScale;
        }

        if (advanceCursor)
        {
            cursorX = localCursorX;
        }
    }
}