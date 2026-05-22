using System.Runtime.CompilerServices;

namespace Brine2D.Animation;

/// <summary>
/// A simple named-parameter store designed as a companion to <see cref="AnimationStateMachine"/>
/// transition conditions. Removes the need to manually manage boolean flag resets in lambdas.
/// </summary>
/// <remarks>
/// <para>
/// Four value types are supported:
/// <list type="bullet">
///   <item><term>Bool</term><description>Latching boolean, stays set until explicitly cleared.</description></item>
///   <item><term>Float</term><description>Continuous float value, e.g. speed or blend weight.</description></item>
///   <item><term>Int</term><description>Integer value, e.g. combo counter or health tier.</description></item>
///   <item><term>Trigger</term><description>
///     Fire-once boolean: <see cref="GetTrigger"/> returns <c>true</c> exactly once after
///     <see cref="SetTrigger"/> is called, then resets automatically. Safe to poll every frame
///     in a transition condition lambda — only the first frame that reads it will see <c>true</c>.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// Typical usage:
/// <code>
/// var p = new AnimationParameters();
/// sm.AddTransition("idle", "walk", () => p.GetFloat("speed") > 0.1f);
/// sm.AddTransition("walk", "attack", () => p.GetTrigger("attackPressed"));
/// sm.AddTransition("idle", "hurt", () => p.GetInt("health") &lt;= 0);
/// // Each frame:
/// p.SetFloat("speed", velocity.Length());
/// if (inputAttack) p.SetTrigger("attackPressed");
/// </code>
/// </para>
/// <para>
/// <b>Warning:</b> <see cref="GetTrigger"/> consumes the trigger immediately upon reading it.
/// Do not combine it with other conditions using <c>&amp;&amp;</c> or <c>||</c>, as the trigger
/// will be consumed even if the other operand short-circuits the result. Use
/// <see cref="IsTriggerArmed"/> as the condition guard and call <see cref="GetTrigger"/> only
/// when you intend to consume it.
/// </para>
/// </remarks>
public sealed class AnimationParameters
{
    private readonly Dictionary<string, bool> _bools = new();
    private readonly Dictionary<string, float> _floats = new();
    private readonly Dictionary<string, int> _ints = new();
    private readonly HashSet<string> _triggers = new();

    /// <summary>Sets a boolean parameter.</summary>
    public void SetBool(string name, bool value)
    {
        ArgumentNullException.ThrowIfNull(name);
        _bools[name] = value;
    }

    /// <summary>
    /// Gets a boolean parameter. Returns <c>false</c> if the parameter has never been set.
    /// </summary>
    public bool GetBool(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _bools.GetValueOrDefault(name);
    }

    /// <summary>
    /// Returns <c>true</c> if a bool parameter with the given name has been explicitly set.
    /// </summary>
    public bool HasBool(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _bools.ContainsKey(name);
    }

    /// <summary>
    /// Removes a bool parameter entirely. Returns <c>true</c> if it existed.
    /// After removal, <see cref="HasBool"/> returns <c>false</c> and <see cref="GetBool"/>
    /// returns the default value <c>false</c>.
    /// </summary>
    public bool RemoveBool(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _bools.Remove(name);
    }

    /// <summary>Sets a float parameter.</summary>
    public void SetFloat(string name, float value)
    {
        ArgumentNullException.ThrowIfNull(name);
        _floats[name] = value;
    }

    /// <summary>
    /// Gets a float parameter. Returns <c>0f</c> if the parameter has never been set.
    /// </summary>
    public float GetFloat(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _floats.GetValueOrDefault(name);
    }

    /// <summary>
    /// Returns <c>true</c> if a float parameter with the given name has been explicitly set.
    /// </summary>
    public bool HasFloat(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _floats.ContainsKey(name);
    }

    /// <summary>
    /// Removes a float parameter entirely. Returns <c>true</c> if it existed.
    /// After removal, <see cref="HasFloat"/> returns <c>false</c> and <see cref="GetFloat"/>
    /// returns the default value <c>0f</c>.
    /// </summary>
    public bool RemoveFloat(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _floats.Remove(name);
    }

    /// <summary>Sets an integer parameter.</summary>
    public void SetInt(string name, int value)
    {
        ArgumentNullException.ThrowIfNull(name);
        _ints[name] = value;
    }

    /// <summary>
    /// Gets an integer parameter. Returns <c>0</c> if the parameter has never been set.
    /// </summary>
    public int GetInt(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _ints.GetValueOrDefault(name);
    }

    /// <summary>
    /// Returns <c>true</c> if an int parameter with the given name has been explicitly set.
    /// </summary>
    public bool HasInt(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _ints.ContainsKey(name);
    }

    /// <summary>
    /// Removes an int parameter entirely. Returns <c>true</c> if it existed.
    /// After removal, <see cref="HasInt"/> returns <c>false</c> and <see cref="GetInt"/>
    /// returns the default value <c>0</c>.
    /// </summary>
    public bool RemoveInt(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _ints.Remove(name);
    }

    /// <summary>
    /// Arms a trigger. The next call to <see cref="GetTrigger"/> for this name will return
    /// <c>true</c> and immediately disarm it. Safe to call multiple times before it is read —
    /// it remains armed until consumed.
    /// </summary>
    public void SetTrigger(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        _triggers.Add(name);
    }

    /// <summary>
    /// Reads and consumes a trigger. Returns <c>true</c> exactly once after <see cref="SetTrigger"/>
    /// was called, then resets to <c>false</c> automatically.
    /// </summary>
    /// <remarks>
    /// The trigger is consumed immediately regardless of any surrounding boolean expression.
    /// Use <see cref="IsTriggerArmed"/> for non-consuming checks.
    /// </remarks>
    public bool GetTrigger(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _triggers.Remove(name);
    }

    /// <summary>
    /// Disarms a trigger without consuming it via <see cref="GetTrigger"/>.
    /// No-op if the trigger is not currently armed.
    /// </summary>
    public void ResetTrigger(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        _triggers.Remove(name);
    }

    /// <summary>
    /// Returns <c>true</c> if the named trigger is currently armed (set but not yet consumed).
    /// Does not consume the trigger.
    /// </summary>
    public bool IsTriggerArmed(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _triggers.Contains(name);
    }

    /// <summary>
    /// Returns <c>true</c> if a trigger with the given name has been explicitly set.
    /// Equivalent to <see cref="IsTriggerArmed"/>; provided for naming consistency with
    /// <see cref="HasBool"/>, <see cref="HasFloat"/>, and <see cref="HasInt"/>.
    /// </summary>
    public bool HasTrigger(string name) => IsTriggerArmed(name);

    /// <summary>Returns all bool parameter names that have been set.</summary>
    public IEnumerable<string> GetBoolNames() => _bools.Keys;

    /// <summary>Returns all float parameter names that have been set.</summary>
    public IEnumerable<string> GetFloatNames() => _floats.Keys;

    /// <summary>Returns all int parameter names that have been set.</summary>
    public IEnumerable<string> GetIntNames() => _ints.Keys;

    /// <summary>Returns all currently armed trigger names.</summary>
    public IEnumerable<string> GetArmedTriggerNames() => _triggers;

    /// <summary>Clears all bool parameters.</summary>
    public void ClearBools() => _bools.Clear();

    /// <summary>Clears all float parameters.</summary>
    public void ClearFloats() => _floats.Clear();

    /// <summary>Clears all int parameters.</summary>
    public void ClearInts() => _ints.Clear();

    /// <summary>Clears all armed triggers.</summary>
    public void ClearTriggers() => _triggers.Clear();

    /// <summary>Clears all bools, floats, ints, and triggers.</summary>
    public void Reset()
    {
        _bools.Clear();
        _floats.Clear();
        _ints.Clear();
        _triggers.Clear();
    }

    /// <summary>
    /// Captures an immutable snapshot of all current parameter values (bools, floats, ints,
    /// and armed triggers). Use <see cref="RestoreSnapshot"/> to revert to this state atomically.
    /// </summary>
    public AnimationParametersSnapshot CaptureSnapshot() =>
        new(
            new Dictionary<string, bool>(_bools),
            new Dictionary<string, float>(_floats),
            new Dictionary<string, int>(_ints),
            new HashSet<string>(_triggers));

    /// <summary>
    /// Restores all parameter values from a previously captured snapshot, replacing all current
    /// values atomically.
    /// </summary>
    public void RestoreSnapshot(AnimationParametersSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _bools.Clear();
        foreach (var kv in snapshot.Bools)
            _bools[kv.Key] = kv.Value;

        _floats.Clear();
        foreach (var kv in snapshot.Floats)
            _floats[kv.Key] = kv.Value;

        _ints.Clear();
        foreach (var kv in snapshot.Ints)
            _ints[kv.Key] = kv.Value;

        _triggers.Clear();
        foreach (var name in snapshot.Triggers)
            _triggers.Add(name);
    }
}