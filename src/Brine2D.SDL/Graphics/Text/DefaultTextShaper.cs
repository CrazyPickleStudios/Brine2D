using System;
using System.Collections.Generic;

namespace Brine2D.SDL.Graphics.Text
{
    // TODO: Eventually integrate HarfBuzz for more complex shaping.
    // TODO: Can look into Skia since it isn't a native dependency, but it may not be robust enough.
    internal sealed class DefaultTextShaper : ITextShaper
    {
        public ShapingMode Mode => ShapingMode.Simple;

        public void Shape(
            ReadOnlySpan<char> text,
            List<ShapedGlyph> output,
            CompositeFont fonts,
            in ShapingFeatures features,
            bool measureOnly)
        {
            output.Clear();
            uint prevCp = 0;
            SpriteFont prevFont = fonts.PrimaryFont;

            for (int i = 0; i < text.Length;)
            {
                int localStart = i;
                uint cp = NextCodepoint(text, ref i);
                var font = fonts.ResolveFont(cp);

                int advance = font.GetAdvanceInternal(cp);
                var script = ClassifyScript(cp);
                var dir = script == TextScript.Arabic || script == TextScript.Hebrew ? TextDirection.RTL : TextDirection.LTR;

                int kern = 0;
                if (!measureOnly && features.Kerning && prevCp != 0 && prevFont == font)
                    kern = fonts.KerningApproxCached(font, prevCp, cp);

                // For now: single-codepoint cluster, no ligature detection.
                output.Add(new ShapedGlyph(
                    cp,
                    glyphIndex: (int)cp,
                    advance: advance,
                    kerning: kern,
                    clusterStart: localStart,
                    clusterLength: i - localStart,
                    isLigature: false,
                    script: script,
                    direction: dir,
                    font: font));

                prevCp = cp;
                prevFont = font;
            }
        }

        private static uint NextCodepoint(ReadOnlySpan<char> span, ref int i)
        {
            if (i >= span.Length) return 0;
            char c = span[i++];
            if (char.IsHighSurrogate(c) && i < span.Length && char.IsLowSurrogate(span[i]))
            {
                char lo = span[i++];
                return (uint)char.ConvertToUtf32(c, lo);
            }
            return (uint)c;
        }

        private static TextScript ClassifyScript(uint cp)
        {
            if (cp <= 0x024F) return TextScript.Latin;
            if (cp >= 0x0400 && cp <= 0x04FF) return TextScript.Cyrillic;
            if (cp >= 0x0370 && cp <= 0x03FF) return TextScript.Greek;
            if (cp >= 0x0590 && cp <= 0x05FF) return TextScript.Hebrew;
            if (cp >= 0x0600 && cp <= 0x06FF) return TextScript.Arabic;
            if (cp >= 0x0900 && cp <= 0x097F) return TextScript.Devanagari;
            if (cp >= 0x4E00 && cp <= 0x9FFF) return TextScript.Han;
            if (cp >= 0xAC00 && cp <= 0xD7AF) return TextScript.Hangul;
            return TextScript.Unknown;
        }
    }
}