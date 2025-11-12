using System;
using System.Collections.Generic;

namespace Brine2D.SDL.Graphics.Text
{
    // Internal to avoid accessibility mismatch with internal CompositeFont / ShapedGlyph
    internal interface ITextShaper
    {
        ShapingMode Mode { get; }
        void Shape(
            ReadOnlySpan<char> text,
            List<ShapedGlyph> output,
            CompositeFont fonts,
            in ShapingFeatures features,
            bool measureOnly);
    }
}