using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Brine2D.Rendering
{
    /// <summary>
    /// Represents a color with RGBA components.
    /// </summary>
    public readonly struct Color : IEquatable<Color>
    {
        public byte R { get; init; }
        
        public byte G { get; init; }
        
        public byte B { get; init; }
        
        public byte A { get; init; }

        [JsonConstructor]
        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static Color FromRgb(byte r, byte g, byte b) => new(r, g, b, 255);
        public static Color FromRgba(byte r, byte g, byte b, byte a) => new(r, g, b, a);
        
        public static Color White => new(255, 255, 255);
        public static Color Black => new(0, 0, 0);
        public static Color Red => new(255, 0, 0);
        public static Color Green => new(0, 255, 0);
        public static Color Blue => new(0, 0, 255);
        public static Color Yellow => new(255, 255, 0);
        public static Color CornflowerBlue => new(100, 149, 237);
        public static Color Transparent => new(0, 0, 0, 0);

        public bool Equals(Color other) =>
            R == other.R && G == other.G && B == other.B && A == other.A;

        public override bool Equals(object? obj) => obj is Color other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(R, G, B, A);

        public static bool operator ==(Color left, Color right) => left.Equals(right);
        public static bool operator !=(Color left, Color right) => !left.Equals(right);

        public override string ToString() => $"Color(R:{R}, G:{G}, B:{B}, A:{A})";
    }
}
