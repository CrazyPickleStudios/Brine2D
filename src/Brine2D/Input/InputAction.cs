using System.Numerics;

namespace Brine2D.Input;

/// <summary>
/// A named logical input action with one or more physical bindings.
/// Supports runtime rebinding. Query methods check all bindings and return
/// the first positive match (for digital) or the largest magnitude (for analog).
/// </summary>
/// <remarks>
/// Thread-safe: the binding list is guarded by <see cref="_bindingsLock"/>.
/// </remarks>
public sealed class InputAction
{
    private readonly Lock _bindingsLock = new();
    private readonly List<InputBinding> _bindings;
    private IReadOnlyList<InputBinding>? _bindingsSnapshot;

    public string Name { get; }

    public IReadOnlyList<InputBinding> Bindings
    {
        get
        {
            lock (_bindingsLock)
                return _bindingsSnapshot ??= [.. _bindings];
        }
    }

    public InputAction(string name, params InputBinding[] bindings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        _bindings = [.. bindings];
    }

    /// <summary>Returns true if any binding is currently held.</summary>
    public bool IsDown(IInputContext input)
    {
        lock (_bindingsLock)
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i].IsDown(input))
                    return true;
            }
        }
        return false;
    }

    /// <summary>Returns true if any binding was pressed this frame.</summary>
    public bool IsPressed(IInputContext input)
    {
        lock (_bindingsLock)
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i].IsPressed(input))
                    return true;
            }
        }
        return false;
    }

    /// <summary>Returns true if any binding was released this frame.</summary>
    public bool IsReleased(IInputContext input)
    {
        lock (_bindingsLock)
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i].IsReleased(input))
                    return true;
            }
        }
        return false;
    }

    /// <summary>Returns the value with the largest absolute magnitude across all bindings.</summary>
    public float ReadValue(IInputContext input)
    {
        float result = 0f;
        lock (_bindingsLock)
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                float value = _bindings[i].ReadValue(input);
                if (MathF.Abs(value) > MathF.Abs(result))
                    result = value;
            }
        }
        return result;
    }

    /// <summary>
    /// Reads this action paired with another as a <see cref="Vector2"/>
    /// (this action supplies X, <paramref name="yAction"/> supplies Y).
    /// </summary>
    public Vector2 ReadVector2(IInputContext input, InputAction yAction)
    {
        ArgumentNullException.ThrowIfNull(yAction);
        return new Vector2(ReadValue(input), yAction.ReadValue(input));
    }

    public void AddBinding(InputBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        lock (_bindingsLock)
        {
            _bindings.Add(binding);
            _bindingsSnapshot = null;
        }
    }

    public bool RemoveBinding(InputBinding binding)
    {
        lock (_bindingsLock)
        {
            var removed = _bindings.Remove(binding);
            if (removed) _bindingsSnapshot = null;
            return removed;
        }
    }

    public void ClearBindings()
    {
        lock (_bindingsLock)
        {
            _bindings.Clear();
            _bindingsSnapshot = null;
        }
    }
}