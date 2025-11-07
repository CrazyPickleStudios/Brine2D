using System.Text.Json;

namespace Brine2D.Core.Input;

/// <summary>
///     Maintains key binding mappings between a user-defined action enum and one or more key chords.
/// </summary>
/// <typeparam name="TAction">
///     The action enum type whose values represent input actions (must be an <see cref="Enum" />).
/// </typeparam>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>Each action can have zero or more <see cref="KeyChord" /> values bound to it.</description>
///         </item>
///         <item>
///             <description>Queries are "any-match": if any chord bound to an action is active, the action is active.</description>
///         </item>
///         <item>
///             <description><see cref="KeyChord" /> is a record struct, so list membership/removal use value equality.</description>
///         </item>
///         <item>
///             <description>
///                 Designed for per-frame polling with an <see cref="IKeyboard" /> that tracks current/previous
///                 states.
///             </description>
///         </item>
///         <item>
///             <description>Thread safety: not thread-safe; use from a single thread (typically the main/game thread).</description>
///         </item>
///         <item>
///             <description>Performance: queries are O(k) for an action with k chords (typical k is small, e.g., 1-3).</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///     enum EditorAction { Save, Run, ToggleConsole }
/// 
///     var bindings = new KeyBindings&lt;EditorAction&gt;();
///     bindings.Bind(EditorAction.Save, new KeyChord(Key.S, KeyboardModifiers.Shift)); // Shift+S
///     bindings.Bind(EditorAction.Run, Key.F5);
/// 
///     // In the frame loop:
///     if (bindings.WasPressed(EditorAction.Run, keyboard, currentMods))
///     {
///         RunProject();
///     }
///     </code>
/// </example>
public sealed class KeyBindings<TAction> where TAction : struct, Enum
{
    /// <summary>
    ///     Internal map from action → list of chords. List preserves insertion order and prevents duplicates.
    /// </summary>
    private readonly Dictionary<TAction, List<KeyChord>> _map = new();

    /// <summary>
    ///     Binds a single key with optional modifiers to the specified action.
    /// </summary>
    /// <param name="action">The action to bind.</param>
    /// <param name="key">The key to bind.</param>
    /// <param name="mods">Optional modifiers for the chord. Default is <see cref="KeyboardModifiers.None" />.</param>
    public void Bind(TAction action, Key key, KeyboardModifiers mods = KeyboardModifiers.None)
    {
        Bind(action, new KeyChord(key, mods));
    }

    /// <summary>
    ///     Binds a <see cref="KeyChord" /> to the specified action. Duplicate chords are ignored.
    /// </summary>
    /// <param name="action">The action to bind.</param>
    /// <param name="chord">The key chord to associate with the action.</param>
    public void Bind(TAction action, KeyChord chord)
    {
        if (!_map.TryGetValue(action, out var list))
        {
            _map[action] = list = new List<KeyChord>(2); // small default capacity
        }

        // Prevent duplicates based on KeyChord value equality.
        if (!list.Contains(chord))
        {
            list.Add(chord);
        }
    }

    /// <summary>
    ///     Removes all bindings for all actions.
    /// </summary>
    public void Clear()
    {
        _map.Clear();
    }

    /// <summary>
    ///     Gets a read-only view of the chords currently bound to the specified action.
    /// </summary>
    /// <param name="action">The action to query.</param>
    /// <returns>
    ///     A list of chords for the action. Returns an empty array if the action has no bindings.
    /// </returns>
    /// <remarks>The returned view reflects future updates to the action's bindings.</remarks>
    public IReadOnlyList<KeyChord> GetBindings(TAction action)
    {
        return _map.TryGetValue(action, out var list) ? list.AsReadOnly() : Array.Empty<KeyChord>();
    }

    /// <summary>
    ///     Returns whether the specified action is currently active (any bound chord is held down).
    /// </summary>
    /// <param name="action">The action to query.</param>
    /// <param name="input">The keyboard input source.</param>
    /// <param name="currentMods">The currently active modifier mask to validate against the chord.</param>
    /// <returns><c>true</c> if any bound chord is currently down; otherwise, <c>false</c>.</returns>
    /// <remarks>Continuous test. Useful for movement or actions that should repeat while held.</remarks>
    public bool IsDown(TAction action, IKeyboard input, KeyboardModifiers currentMods)
    {
        if (!_map.TryGetValue(action, out var list))
        {
            return false;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].IsDown(input, currentMods))
            {
                return true;
            }
        }

        return false;
    }

    // JSON shape:
    // {
    //   "Save": ["Shift+S"],
    //   "Run":  ["F5", "Shift+R"]
    // }

    /// <summary>
    ///     Replaces bindings from a JSON document mapping action names to chord strings.
    /// </summary>
    /// <param name="json">A JSON string of the form: { "ActionName": ["Chord", ...], ... }.</param>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Action names are matched using <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)" />
    ///                 with <c>ignoreCase: true</c>.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Unknown action names are ignored.</description>
    ///         </item>
    ///         <item>
    ///             <description>For each recognized action, existing bindings are cleared before applying the new ones.</description>
    ///         </item>
    ///         <item>
    ///             <description>Invalid or unparsable chord strings are skipped.</description>
    ///         </item>
    ///         <item>
    ///             <description>A <c>null</c> array value for an action clears its bindings.</description>
    ///         </item>
    ///     </list>
    ///     <code language="json">
    /// {
    ///   "Save": ["Shift+S"],
    ///   "Run":  ["F5", "Shift+R"]
    /// }
    ///     </code>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if the JSON root cannot be deserialized into the expected shape.</exception>
    public void LoadFromJson(string json)
    {
        var doc = JsonSerializer.Deserialize<Dictionary<string, string[]>>(json)
                  ?? throw new ArgumentException("Invalid keybindings JSON.");

        foreach (var (actionName, chordStrings) in doc)
        {
            if (!Enum.TryParse<TAction>(actionName, true, out var action))
            {
                continue;
            }

            UnbindAll(action);
            if (chordStrings is null)
            {
                continue;
            }

            foreach (var s in chordStrings)
            {
                if (KeyChordFormat.TryParse(s, out var chord))
                {
                    Bind(action, chord);
                }
            }
        }
    }

    /// <summary>
    ///     Serializes the current bindings to a JSON string, mapping action names to chord strings.
    /// </summary>
    /// <returns>A formatted JSON string representing all bindings.</returns>
    /// <remarks>
    ///     Produces a dictionary-like JSON where keys are action names (via <c>ToString()</c>) and values are arrays of chord
    ///     strings.
    /// </remarks>
    public string ToJson()
    {
        var dict = new Dictionary<string, string[]>(_map.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in _map)
        {
            var name = kvp.Key.ToString();
            var chords = kvp.Value;
            var arr = new string[chords.Count];
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = KeyChordFormat.Format(chords[i]);
            }

            dict[name] = arr;
        }

        return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    ///     Unbinds a specific <see cref="KeyChord" /> from the specified action.
    /// </summary>
    /// <param name="action">The action whose binding should be removed.</param>
    /// <param name="chord">The chord to remove.</param>
    public void Unbind(TAction action, KeyChord chord)
    {
        if (_map.TryGetValue(action, out var list))
        {
            list.Remove(chord);
        }
    }

    /// <summary>
    ///     Removes all bindings for the specified action.
    /// </summary>
    /// <param name="action">The action to clear.</param>
    public void UnbindAll(TAction action)
    {
        _map.Remove(action);
    }

    /// <summary>
    ///     Returns <c>true</c> only on the frame an action becomes pressed (any bound chord up to down).
    /// </summary>
    /// <param name="action">The action to query.</param>
    /// <param name="input">The keyboard input source.</param>
    /// <param name="currentMods">The currently active modifier mask to validate against the chord.</param>
    /// <returns><c>true</c> on the first frame any bound chord is pressed; otherwise, <c>false</c>.</returns>
    /// <remarks>Edge-triggered test (up to down). Useful for single-fire actions (e.g., confirm, toggle).</remarks>
    public bool WasPressed(TAction action, IKeyboard input, KeyboardModifiers currentMods)
    {
        if (!_map.TryGetValue(action, out var list))
        {
            return false;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].WasPressed(input, currentMods))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Returns <c>true</c> only on the frame an action becomes released (any bound chord down to up).
    /// </summary>
    /// <param name="action">The action to query.</param>
    /// <param name="input">The keyboard input source.</param>
    /// <param name="currentMods">The currently active modifier mask to validate against the chord.</param>
    /// <returns><c>true</c> on the first frame any bound chord is released; otherwise, <c>false</c>.</returns>
    /// <remarks>Edge-triggered test (down to up). Useful for ending behaviors like sprint release or drag complete.</remarks>
    public bool WasReleased(TAction action, IKeyboard input, KeyboardModifiers currentMods)
    {
        if (!_map.TryGetValue(action, out var list))
        {
            return false;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].WasReleased(input, currentMods))
            {
                return true;
            }
        }

        return false;
    }
}