using System.Collections.Frozen;

namespace Brine2D.Physics;

/// <summary>
/// A static registry that maps named physics layers to their integer index (0–63).
/// Register layers once at startup; then use <see cref="MaskFor"/> to build collision masks.
/// Thread-safe for concurrent reads after all registrations are complete.
/// </summary>
public static class PhysicsLayers
{
    private static readonly Lock Lock = new();
    private static readonly Dictionary<string, int> Layers = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a named layer. <paramref name="index"/> must be in [0, 63].
    /// Re-registering the same name with the same index is a no-op.
    /// Re-registering with a different index throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public static void Register(string name, int index)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 63);

        lock (Lock)
        {
            if (Layers.TryGetValue(name, out int existing))
            {
                if (existing != index)
                    throw new InvalidOperationException(
                        $"Physics layer '{name}' is already registered as index {existing}. Cannot re-register as {index}.");
                return;
            }
            Layers[name] = index;
        }
    }

    /// <summary>
    /// Returns the layer index for <paramref name="name"/>.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the name is not registered.</exception>
    public static int Get(string name)
    {
        lock (Lock)
        {
            if (!Layers.TryGetValue(name, out int index))
                throw new KeyNotFoundException($"Physics layer '{name}' is not registered.");
            return index;
        }
    }

    /// <summary>Returns the layer index, or <c>false</c> if not found.</summary>
    public static bool TryGet(string name, out int index)
    {
        lock (Lock)
            return Layers.TryGetValue(name, out index);
    }

    /// <summary>
    /// Returns a collision mask with bits set for each named layer.
    /// </summary>
    public static ulong MaskFor(params ReadOnlySpan<string> names)
    {
        ulong mask = 0;
        foreach (var name in names)
            mask |= 1UL << Get(name);
        return mask;
    }

    /// <summary>Removes all registered layers. Intended for test teardown.</summary>
    public static void Clear()
    {
        lock (Lock)
            Layers.Clear();
    }
}