using System.Globalization;
using Brine2D.Core;

namespace Brine2D.Tilemap;

/// <summary>
/// Typed accessors for Tiled property dictionaries on maps, layers, objects, and tiles.
/// All values come back as strings after loading; these helpers handle the parsing.
/// </summary>
public static class TilemapPropertyExtensions
{
    /// <summary>
    /// Returns the property value as <typeparamref name="T"/>, or <paramref name="defaultValue"/>
    /// if the key is missing or can't be parsed.
    /// </summary>
    public static T Get<T>(this Dictionary<string, string> properties, string name, T defaultValue = default!)
    {
        if (!properties.TryGetValue(name, out var raw))
            return defaultValue;

        try
        {
            if (typeof(T) == typeof(string))
                return (T)(object)raw;

            if (typeof(T) == typeof(int))
                return (T)(object)int.Parse(raw, CultureInfo.InvariantCulture);

            if (typeof(T) == typeof(float))
                return (T)(object)float.Parse(raw, CultureInfo.InvariantCulture);

            if (typeof(T) == typeof(double))
                return (T)(object)double.Parse(raw, CultureInfo.InvariantCulture);

            if (typeof(T) == typeof(bool))
            {
                if (bool.TryParse(raw, out var b)) return (T)(object)b;
                if (raw == "1") return (T)(object)true;
                if (raw == "0") return (T)(object)false;
                return defaultValue;
            }
        }
        catch (FormatException) { }
        catch (OverflowException) { }

        return defaultValue;
    }

    /// <summary>Returns true and sets <paramref name="value"/> if the key exists and can be parsed as <typeparamref name="T"/>.</summary>
    public static bool TryGet<T>(this Dictionary<string, string> properties, string name, out T value)
    {
        if (!properties.TryGetValue(name, out var raw))
        {
            value = default!;
            return false;
        }

        try
        {
            if (typeof(T) == typeof(string)) { value = (T)(object)raw; return true; }

            if (typeof(T) == typeof(int) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            { value = (T)(object)i; return true; }

            if (typeof(T) == typeof(float) && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            { value = (T)(object)f; return true; }

            if (typeof(T) == typeof(double) && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            { value = (T)(object)d; return true; }

            if (typeof(T) == typeof(bool))
            {
                if (bool.TryParse(raw, out var b)) { value = (T)(object)b; return true; }
                if (raw == "1") { value = (T)(object)true; return true; }
                if (raw == "0") { value = (T)(object)false; return true; }
            }
        }
        catch (FormatException) { }
        catch (OverflowException) { }

        value = default!;
        return false;
    }

    /// <summary>
    /// Returns the property value as a <see cref="Color"/> parsed from a Tiled color string
    /// (<c>#RRGGBB</c> or <c>#AARRGGBB</c>), or <paramref name="defaultValue"/> if the key is
    /// missing or the format isn't recognised.
    /// </summary>
    public static Color GetColor(this Dictionary<string, string> properties, string name, Color defaultValue = default)
    {
        if (!properties.TryGetValue(name, out var raw) || string.IsNullOrEmpty(raw))
            return defaultValue;

        var s = raw.TrimStart('#');
        if (s.Length == 6 && uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            return new Color((byte)(rgb >> 16), (byte)(rgb >> 8 & 0xFF), (byte)(rgb & 0xFF));

        if (s.Length == 8 && uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
            return new Color((byte)(argb >> 16 & 0xFF), (byte)(argb >> 8 & 0xFF), (byte)(argb & 0xFF), (byte)(argb >> 24));

        return defaultValue;
    }
}