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
            Color = color,
            Font = _textRenderer.DefaultFont
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

        var textSize = _textRenderer.MeasureTextRuns(runs);

        float startX = x;
        float startY = y;

        bool fitsWithoutWrapping = !options.MaxWidth.HasValue || textSize.X <= options.MaxWidth.Value;

        if (options.MaxWidth.HasValue && fitsWithoutWrapping)
        {
            startX = options.HorizontalAlign switch
            {
                TextAlignment.Center => x + (options.MaxWidth.Value - textSize.X) / 2,
                TextAlignment.Right => x + options.MaxWidth.Value - textSize.X,
                _ => x
            };
        }

        if (options.MaxHeight.HasValue)
        {
            float totalHeight = fitsWithoutWrapping ? textSize.Y : lineHeight(baseScale, options) * EstimateWrappedLineCount(runs, x, options, defaultFontSize);
            startY = options.VerticalAlign switch
            {
                VerticalAlignment.Middle => y + (options.MaxHeight.Value - Math.Min(totalHeight, options.MaxHeight.Value)) / 2,
                VerticalAlignment.Bottom => y + options.MaxHeight.Value - Math.Min(totalHeight, options.MaxHeight.Value),
                _ => y
            };
        }

        float cursorX = startX;
        float cursorY = startY;
        float lh = lineHeight(baseScale, options);

        var atlasTexture = _textRenderer.DefaultFontAtlas.Texture;

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

            float runScale = run.FontSize / defaultFontSize;

            if (options.MaxWidth.HasValue)
                RenderRunWithWrapping(run, ref cursorX, ref cursorY, startX, lh, options, atlasTexture, runScale, y);
            else
                RenderRunDirect(run, ref cursorX, ref cursorY, startX, lh, options, atlasTexture, runScale, y);
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
                    var rem = span[ws..];
                    int si = rem.IndexOf(' ');
                    var word = si < 0 ? rem : rem[..(si + 1)];
                    var sz = _textRenderer.MeasureGlyphSpan(word, rs);
                    if (cx + sz.X > originX + opts.MaxWidth!.Value && cx > originX)
                    {
                        cx = originX;
                        lines++;
                    }
                    cx += sz.X;
                    ws += word.Length;
                    if (si < 0) break;
                }
            }
            return lines;
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

        if (options.MaxWidth.HasValue || options.MaxHeight.HasValue)
        {
            var textSize = _textRenderer.MeasureText(text, options.FontSize);
            bool fitsWithoutWrapping = !options.MaxWidth.HasValue || textSize.X <= options.MaxWidth.Value;

            if (options.MaxWidth.HasValue && fitsWithoutWrapping)
            {
                startX = options.HorizontalAlign switch
                {
                    TextAlignment.Center => x + (options.MaxWidth.Value - textSize.X) / 2,
                    TextAlignment.Right => x + options.MaxWidth.Value - textSize.X,
                    _ => x
                };
            }

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

        if (options.MaxWidth.HasValue)
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
                var rem = span[ws..];
                int si = rem.IndexOf(' ');
                var word = si < 0 ? rem : rem[..(si + 1)];
                var sz = _textRenderer.MeasureGlyphSpan(word, scale);
                if (cx + sz.X > originX + opts.MaxWidth!.Value && cx > originX)
                {
                    cx = originX;
                    lines++;
                }
                cx += sz.X;
                ws += word.Length;
                if (si < 0) break;
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
            RenderGlyphs(text, cursorX + options.ShadowOffset.Value.X,
                         cursorY + options.ShadowOffset.Value.Y,
                         options.ShadowColor, atlasTexture,
                         ref cursorX, ref cursorY, shadowLineStartX, lineHeight, false, glyphScale,
                         maxY: options.MaxHeight.HasValue && originY > float.NegativeInfinity
                             ? originY + options.MaxHeight.Value : null);
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

                var remaining = text[wordStart..];
                int spaceIdx = remaining.IndexOf(' ');
                var word = spaceIdx < 0 ? remaining : remaining[..(spaceIdx + 1)];

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
                if (spaceIdx < 0) break;
            }
            return;
        }

        var lines = new List<(int Start, int Length)>();
        float cx = cursorX;
        int lineStart = 0;

        int ws = 0;
        while (ws < text.Length)
        {
            var remaining = text[ws..];
            int spaceIdx = remaining.IndexOf(' ');
            var word = spaceIdx < 0 ? remaining : remaining[..(spaceIdx + 1)];

            var wordSize = _textRenderer.MeasureGlyphSpan(word, glyphScale);
            if (cx + wordSize.X > startX + maxWidth && cx > startX)
            {
                lines.Add((lineStart, ws - lineStart));
                lineStart = ws;
                cx = startX;
            }

            cx += wordSize.X;
            ws += word.Length;
            if (spaceIdx < 0) break;
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