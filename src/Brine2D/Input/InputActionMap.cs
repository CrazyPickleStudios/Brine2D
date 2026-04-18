using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Brine2D.Input;

/// <summary>
/// A named collection of <see cref="InputAction"/> instances.
/// Typically one map per game context (e.g., "Player", "UI", "Vehicle").
/// Set <see cref="Enabled"/> to <see langword="false"/> to suppress all actions
/// in this map without unregistering them.
/// </summary>
/// <remarks>
/// Thread-safe: all mutable state is guarded by <see cref="_lock"/>.
/// </remarks>
public sealed class InputActionMap
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, InputAction> _actions = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, InputAction>? _actionsSnapshot;
    private bool _enabled = true;

    public string Name { get; }

    /// <summary>
    /// Gets or sets whether this action map is enabled.
    /// When disabled, all actions return false/zero for query methods.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool Enabled
    {
        get { lock (_lock) return _enabled; }
        set { lock (_lock) _enabled = value; }
    }

    public IReadOnlyDictionary<string, InputAction> Actions
    {
        get
        {
            lock (_lock)
                return _actionsSnapshot ??= new Dictionary<string, InputAction>(_actions, StringComparer.Ordinal);
        }
    }

    public InputActionMap(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>Gets an action by name. Throws if not found.</summary>
    public InputAction this[string actionName]
    {
        get
        {
            lock (_lock)
            {
                return _actions.TryGetValue(actionName, out var action)
                    ? action
                    : throw new KeyNotFoundException($"Input action '{actionName}' not found in map '{Name}'.");
            }
        }
    }

    public void AddAction(InputAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        lock (_lock)
        {
            _actions.Add(action.Name, action);
            _actionsSnapshot = null;
        }
    }

    public bool RemoveAction(string name)
    {
        lock (_lock)
        {
            var removed = _actions.Remove(name);
            if (removed) _actionsSnapshot = null;
            return removed;
        }
    }

    public bool TryGetAction(string name, [NotNullWhen(true)] out InputAction? action)
    {
        lock (_lock)
            return _actions.TryGetValue(name, out action);
    }

    /// <summary>Returns true if any binding of the named action is currently held. Returns false if the map is disabled.</summary>
    public bool IsDown(string actionName, IInputContext input)
    {
        InputAction? action;
        lock (_lock)
        {
            if (!_enabled || !_actions.TryGetValue(actionName, out action))
                return false;
        }
        return action.IsDown(input);
    }

    /// <summary>Returns true if any binding of the named action was pressed this frame. Returns false if the map is disabled.</summary>
    public bool IsPressed(string actionName, IInputContext input)
    {
        InputAction? action;
        lock (_lock)
        {
            if (!_enabled || !_actions.TryGetValue(actionName, out action))
                return false;
        }
        return action.IsPressed(input);
    }

    /// <summary>Returns true if any binding of the named action was released this frame. Returns false if the map is disabled.</summary>
    public bool IsReleased(string actionName, IInputContext input)
    {
        InputAction? action;
        lock (_lock)
        {
            if (!_enabled || !_actions.TryGetValue(actionName, out action))
                return false;
        }
        return action.IsReleased(input);
    }

    /// <summary>Returns the analog value of the named action. Returns 0 if the map is disabled.</summary>
    public float ReadValue(string actionName, IInputContext input)
    {
        InputAction? action;
        lock (_lock)
        {
            if (!_enabled || !_actions.TryGetValue(actionName, out action))
                return 0f;
        }
        return action.ReadValue(input);
    }

    /// <summary>
    /// Reads two named actions as a <see cref="Vector2"/> (X from <paramref name="xActionName"/>,
    /// Y from <paramref name="yActionName"/>). Returns <see cref="Vector2.Zero"/> if the map is disabled.
    /// </summary>
    public Vector2 ReadVector2(string xActionName, string yActionName, IInputContext input)
    {
        InputAction? xAction, yAction;
        lock (_lock)
        {
            if (!_enabled)
                return Vector2.Zero;

            _actions.TryGetValue(xActionName, out xAction);
            _actions.TryGetValue(yActionName, out yAction);
        }

        return new Vector2(
            xAction?.ReadValue(input) ?? 0f,
            yAction?.ReadValue(input) ?? 0f);
    }
}