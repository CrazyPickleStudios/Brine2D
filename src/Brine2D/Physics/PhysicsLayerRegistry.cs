using System.Collections.Frozen;

namespace Brine2D.Physics;

/// <summary>
/// Maps string layer names to Box2D layer indices (0–63). Register names at startup via
/// <see cref="Register"/> before building any physics bodies, then use
/// <see cref="GetLayer"/> and <see cref="GetMask"/> to build
/// <see cref="PhysicsQueryFilter"/> instances and assign
/// <see cref="Brine2D.ECS.Components.PhysicsBodyComponent.Layer"/> values by name.
/// </summary>
/// <remarks>
/// <para>
/// This class is intentionally mutable during the registration phase and then frozen for
/// runtime lookups. Call <see cref="Freeze"/> once all layers are registered (typically
/// at the end of your DI/startup configuration). After freezing, <see cref="Register"/>
/// throws.
/// </para>
/// <para>
/// Layer indices 0–63 map directly to Box2D category/mask bits: a body on layer N has
/// <c>categoryBits = 1UL &lt;&lt; N</c> and a query for layer N has
/// <c>CollisionMask = 1UL &lt;&lt; N</c>.
/// </para>
/// </remarks>
public sealed class PhysicsLayerRegistry
{
    private readonly Dictionary<string, int> _layers = new(StringComparer.Ordinal);
    private FrozenDictionary<string, int>? _frozen;

    /// <summary>
    /// Whether <see cref="Freeze"/> has been called. Once frozen, <see cref="Register"/> throws.
    /// </summary>
    public bool IsFrozen => _frozen != null;

    /// <summary>
    /// Registers a named layer at the given index (0–63).
    /// </summary>
    /// <param name="name">Layer name. Must be unique (case-sensitive).</param>
    /// <param name="index">Layer index 0–63, corresponding to a single bit in a 64-bit mask.</param>
    /// <exception cref="InvalidOperationException">Thrown when the registry is already frozen.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is outside 0–63.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="name"/> is already registered, or the
    ///     <paramref name="index"/> is already claimed by another name.
    /// </exception>
    public void Register(string name, int index)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 63);

        if (_frozen != null)
            throw new InvalidOperationException(
                "PhysicsLayerRegistry is frozen. Register all layers before calling Freeze().");

        if (_layers.ContainsKey(name))
            throw new ArgumentException($"Layer name '{name}' is already registered.", nameof(name));

        foreach (var (existingName, existingIndex) in _layers)
        {
            if (existingIndex == index)
                throw new ArgumentException(
                    $"Layer index {index} is already claimed by '{existingName}'.", nameof(index));
        }

        _layers[name] = index;
    }

    /// <summary>
    /// Freezes the registry. After this call <see cref="Register"/> throws and all lookups
    /// switch to a lock-free frozen dictionary for maximum read performance.
    /// </summary>
    public void Freeze()
    {
        _frozen ??= _layers.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>
    /// Returns the layer index for the given name.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the name has not been registered.</exception>
    public int GetLayer(string name)
    {
        var dict = (IReadOnlyDictionary<string, int>?)_frozen ?? _layers;
        if (!dict.TryGetValue(name, out int index))
            throw new KeyNotFoundException($"Physics layer '{name}' is not registered.");
        return index;
    }

    /// <summary>
    /// Returns the category/collision bit mask for the given layer name.
    /// Equivalent to <c>1UL &lt;&lt; GetLayer(name)</c>.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the name has not been registered.</exception>
    public ulong GetMask(string name) => 1UL << GetLayer(name);

    /// <summary>
    /// Returns the combined mask for multiple named layers.
    /// Equivalent to OR-ing <see cref="GetMask"/> for each name.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when any name has not been registered.</exception>
    public ulong GetMask(params ReadOnlySpan<string> names)
    {
        ulong mask = 0;
        foreach (var name in names)
            mask |= GetMask(name);
        return mask;
    }

    /// <summary>
    /// Tries to get the layer index for the given name. Returns <c>false</c> if not registered.
    /// </summary>
    public bool TryGetLayer(string name, out int index)
    {
        var dict = (IReadOnlyDictionary<string, int>?)_frozen ?? _layers;
        return dict.TryGetValue(name, out index);
    }

    /// <summary>
    /// Returns a <see cref="PhysicsQueryFilter"/> that hits shapes on the given named layer.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the name has not been registered.</exception>
    public PhysicsQueryFilter ForLayer(string name)
        => PhysicsQueryFilter.ForLayer(GetLayer(name));

    /// <summary>
    /// Returns a <see cref="PhysicsQueryFilter"/> that hits solid (non-sensor) shapes on the given named layer.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the name has not been registered.</exception>
    public PhysicsQueryFilter SolidLayer(string name)
        => PhysicsQueryFilter.SolidLayer(GetLayer(name));

    /// <summary>
    /// Returns a <see cref="PhysicsQueryFilter"/> that hits shapes on any of the given named layers.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when any name has not been registered.</exception>
    public PhysicsQueryFilter ForLayers(params ReadOnlySpan<string> names)
    {
        ulong mask = GetMask(names);
        return new PhysicsQueryFilter { CollisionMask = mask };
    }

    /// <summary>
    /// Returns a <see cref="PhysicsQueryFilter"/> that hits solid shapes on any of the given named layers.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when any name has not been registered.</exception>
    public PhysicsQueryFilter SolidLayers(params ReadOnlySpan<string> names)
    {
        ulong mask = GetMask(names);
        return new PhysicsQueryFilter { CollisionMask = mask, ExcludeSensors = true };
    }

    /// <summary>
    /// Returns all registered layer name → index pairs.
    /// </summary>
    public IEnumerable<KeyValuePair<string, int>> GetAllLayers()
    {
        var dict = (IReadOnlyDictionary<string, int>?)_frozen ?? _layers;
        return dict;
    }
}