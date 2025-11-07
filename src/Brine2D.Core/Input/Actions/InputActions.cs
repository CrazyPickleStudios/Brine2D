using System.Text;
using System.Text.Json;
using Brine2D.Core.Input.Bindings;
using Brine2D.Core.Math;

namespace Brine2D.Core.Input.Actions;

/// <summary>
///     Unified input map that exposes high-level actions (digital) and axes (analog) over keyboard, mouse, and gamepad.
/// </summary>
/// <typeparam name="TAction">Enum type for actions (digital).</typeparam>
/// <typeparam name="TAxis">Enum type for axes (analog).</typeparam>
/// <remarks>
///     <para>
///         Call <see cref="Update(IKeyboard, KeyboardModifiers, IMouse, IGamepads, double)" /> exactly once per frame
///         before querying.
///     </para>
///     <para>Not thread-safe; use from the main thread.</para>
///     <para>Features:</para>
///     <list type="bullet">
///         <item>
///             <description>Digital actions and analog axes with per-binding parameters.</description>
///         </item>
///         <item>
///             <description>Action sets (contextual enable/disable) and explicit per-action/axis enable flags.</description>
///         </item>
///         <item>
///             <description>Per-action repeat (initial delay + interval) and hold duration tracking.</description>
///         </item>
///         <item>
///             <description>
///                 Axis combination policies (MaxAbs, SumClamped, FirstNonZero) and smoothing (alpha or
///                 time-constant tau).
///             </description>
///         </item>
///         <item>
///             <description>JSON persistence: load/merge/save, Replace/Append merge modes, and diff export.</description>
///         </item>
///         <item>
///             <description>
///                 Last-input-source tracking (Keyboard/Mouse/Gamepad) and convenience queries
///                 (WasPressed/Released, TryConsumePressed, Get2D, GetRaw).
///             </description>
///         </item>
///         <item>
///             <description>Batch change notifications and duplicate-binding guards.</description>
///         </item>
///     </list>
/// </remarks>
public sealed partial class InputActions<TAction, TAxis>
    where TAction : struct, Enum
    where TAxis : struct, Enum
{
    // Bindings store
    private readonly Dictionary<TAction, List<IActionBinding>> _actionBindings = new();

    // Action sets (contextual enable/disable)
    private readonly Dictionary<TAction, string?> _actionSet = new();
    private readonly Dictionary<TAxis, List<IAxisBinding>> _axisBindings = new();

    private readonly Dictionary<TAxis, AxisCombinePolicy> _axisCombine = new();
    private readonly Dictionary<TAxis, float> _axisRawValues = new(); // pre-smoothing
    private readonly Dictionary<TAxis, float> _axisSmoothed = new();

    private readonly Dictionary<TAxis, float> _axisSmoothingAlpha = new(); // 0..1 (1 = no smoothing)

    // Time-constant smoothing (frame-rate independent). tau in seconds; alpha derived each frame.
    private readonly Dictionary<TAxis, float> _axisSmoothingTau = new();

    // Axis cache per frame: raw before smoothing and final value after smoothing
    private readonly Dictionary<TAxis, float> _axisValues = new(); // post-smoothing
    private readonly Dictionary<TAction, bool> _curr = new();

    // Explicit enable/disable controls (independent from sets)
    private readonly HashSet<TAction> _disabledActions = new();
    private readonly HashSet<TAxis> _disabledAxes = new();
    private readonly HashSet<string> _enabledSets = new(StringComparer.OrdinalIgnoreCase) { "default" };
    private readonly Dictionary<TAction, double> _holdSeconds = new();

    private readonly Dictionary<TAction, InputDevice> _lastActionSource = new();
    private readonly Dictionary<TAxis, InputDevice> _lastAxisSource = new();
    private readonly HashSet<TAction> _pressedConsumed = new();

    // Action state (per frame - edges/hold/repeat bookkeeping)
    private readonly Dictionary<TAction, bool> _prev = new();

    // Action repeat (UI-friendly key repeat)
    private readonly Dictionary<TAction, RepeatConfig> _repeatCfg = new();
    private readonly HashSet<TAction> _repeatFiredThisFrame = new();
    private readonly Dictionary<TAction, double> _repeatTimer = new();

    // Batch change suppression
    private int _bindingsBatchDepth;
    private bool _bindingsChangedDuringBatch;

    /// <summary>
    ///     Notifies listeners whenever the bindings are changed (coalesced if within a batch).
    /// </summary>
    public event Action? OnBindingsChanged;

    // Axis combine policy and smoothing
    /// <summary>Specifies how multiple axis bindings contribute to the final axis value.</summary>
    public enum AxisCombinePolicy
    {
        /// <summary>Use the binding with the largest absolute magnitude.</summary>
        MaxAbs,

        /// <summary>Sum all contributions and clamp to [-1, 1].</summary>
        SumClamped,

        /// <summary>Use the first binding that produces a non-zero value (by small epsilon).</summary>
        FirstNonZero
    }

    /// <summary>Identifies the last device that drove an action or axis.</summary>
    public enum InputDevice
    {
        None,
        Keyboard,
        Mouse,
        Gamepad
    }

    /// <summary>Merge behavior when importing bindings.</summary>
    public enum MergeMode
    {
        Replace,
        Append
    }

    /// <summary>
    ///     Preferred player gamepad index to resolve from <see cref="IGamepads" />. Null means use primary.
    /// </summary>
    public int? PlayerGamepadIndex { get; set; } = null; // null => use primary

    /// <summary>Scoped batch helper. Use with using(...) to coalesce notifications.</summary>
    public IDisposable BeginBindingsBatch()
    {
        return new BatchScope(this);
    }

    /// <summary>Begins a batch update; notifications are coalesced until <see cref="EndBindingsUpdate" />.</summary>
    public void BeginBindingsUpdate()
    {
        _bindingsBatchDepth++;
    }

    /// <summary>
    ///     Adds one or more bindings to an action. Duplicate bindings are ignored.
    ///     Triggers <see cref="OnBindingsChanged" />.
    /// </summary>
    public void BindAction(TAction action, params IActionBinding[] bindings)
    {
        if (!_actionBindings.TryGetValue(action, out var list))
        {
            _actionBindings[action] = list = new List<IActionBinding>(bindings.Length);
        }

        for (var i = 0; i < bindings.Length; i++)
        {
            AddUniqueActionBinding(list, bindings[i]);
        }

        NotifyBindingsChanged();
    }

    /// <summary>
    ///     Adds one or more bindings to an axis. Duplicate bindings are ignored.
    ///     Triggers <see cref="OnBindingsChanged" />.
    /// </summary>
    public void BindAxis(TAxis axis, params IAxisBinding[] bindings)
    {
        if (!_axisBindings.TryGetValue(axis, out var list))
        {
            _axisBindings[axis] = list = new List<IAxisBinding>(bindings.Length);
        }

        for (var i = 0; i < bindings.Length; i++)
        {
            AddUniqueAxisBinding(list, bindings[i]);
        }

        NotifyBindingsChanged();
    }

    /// <summary>Clears all bindings for a given action. Triggers <see cref="OnBindingsChanged" />.</summary>
    public void ClearActionBindings(TAction action)
    {
        if (_actionBindings.Remove(action))
        {
            NotifyBindingsChanged();
        }
    }

    // ------------ API ------------

    /// <summary>
    ///     Clears all bindings and all runtime state (sets, disabled lists, smoothing settings, last source, etc.).
    ///     Triggers <see cref="OnBindingsChanged" />.
    /// </summary>
    public void ClearAll()
    {
        _actionBindings.Clear();
        _axisBindings.Clear();
        _prev.Clear();
        _curr.Clear();
        _pressedConsumed.Clear();
        _holdSeconds.Clear();
        _axisValues.Clear();
        _axisRawValues.Clear();

        _actionSet.Clear();
        _enabledSets.Clear();
        _enabledSets.Add("default");
        _disabledActions.Clear();
        _disabledAxes.Clear();

        _repeatCfg.Clear();
        _repeatTimer.Clear();
        _repeatFiredThisFrame.Clear();

        _axisCombine.Clear();
        _axisSmoothingAlpha.Clear();
        _axisSmoothed.Clear();
        _axisSmoothingTau.Clear();

        _lastActionSource.Clear();
        _lastAxisSource.Clear();

        NotifyBindingsChanged();
    }

    /// <summary>Clears all bindings for a given axis. Triggers <see cref="OnBindingsChanged" />.</summary>
    public void ClearAxisBindings(TAxis axis)
    {
        if (_axisBindings.Remove(axis))
        {
            NotifyBindingsChanged();
        }
    }

    /// <summary>
    ///     Produces a human-readable snapshot of current state for diagnostics.
    /// </summary>
    /// <param name="includeBindings">If true, includes per-action/axis binding details.</param>
    public string DumpState(bool includeBindings = false)
    {
        var sb = new StringBuilder(2048);
        sb.AppendLine("InputActions state:");
        sb.AppendLine("Actions:");
        foreach (var kv in _actionBindings)
        {
            var action = kv.Key;
            _curr.TryGetValue(action, out var c);
            _prev.TryGetValue(action, out var p);
            var hold = GetHoldSeconds(action);
            var dev = GetLastSource(action);
            sb.Append("- ").Append(action).Append(": ");
            sb.Append("down=").Append(c ? "1" : "0").Append(", pressed=").Append(c && !p ? "1" : "0");
            sb.Append(", hold=").Append(hold.ToString("0.000")).Append("s");
            sb.Append(", last=").Append(dev).AppendLine();
            if (includeBindings)
            {
                var list = kv.Value;
                for (var i = 0; i < list.Count; i++)
                {
                    sb.Append("    ").Append(BindingToString(list[i])).AppendLine();
                }
            }
        }

        sb.AppendLine("Axes:");
        foreach (var kv in _axisBindings)
        {
            var axis = kv.Key;
            var raw = GetRaw(axis);
            var sm = Get(axis);
            var dev = GetLastSource(axis);
            sb.Append("- ").Append(axis).Append(": ");
            sb.Append("raw=").Append(raw.ToString("0.000")).Append(", sm=").Append(sm.ToString("0.000"));
            sb.Append(", last=").Append(dev).AppendLine();
            if (includeBindings)
            {
                var list = kv.Value;
                for (var i = 0; i < list.Count; i++)
                {
                    sb.Append("    ").Append(BindingToString(list[i])).AppendLine();
                }
            }
        }

        return sb.ToString();

        static string BindingToString(object b)
        {
            switch (b)
            {
                case KeyChordBinding kb:
                    return $"KeyChord({kb.Chord.Key}+{kb.Chord.Modifiers})";
                case MouseButtonBinding mb:
                    return $"MouseButton({mb.Button})";
                case GamepadButtonBinding gb:
                    return
                        $"GamepadButton({gb.Button}, pad={gb.PadIndex}, with=[{string.Join(",", gb.With ?? new List<GamepadButton>())}])";
                case GamepadTriggerBinding gt:
                    return
                        $"GamepadTrigger({gt.Axis}, press={gt.PressThreshold:0.00}, release={gt.ReleaseThreshold:0.00}, pad={gt.PadIndex})";
                case Composite2KeysAxisBinding ck:
                    return $"Composite2Keys({ck.Negative}->{ck.Positive}, scale={ck.Scale:0.###}, inv={ck.Invert})";
                case MouseAxisBinding ma:
                    return $"MouseAxis({ma.Axis}, scale={ma.Scale:0.###}, inv={ma.Invert})";
                case GamepadAxisBinding ga:
                    return
                        $"GamepadAxis({ga.Axis}, dz={ga.DeadZone:0.###}, sens={ga.Sensitivity:0.###}, curve={ga.Curve:0.###}, inv={ga.Invert}, pad={ga.PadIndex})";
                default: return b.GetType().Name;
            }
        }
    }

    /// <summary>
    ///     Enables or disables a specific action regardless of its set membership.
    ///     Disabling clears its state (hold, edges, etc.).
    /// </summary>
    public void EnableAction(TAction action, bool enabled)
    {
        if (enabled)
        {
            _disabledActions.Remove(action);
        }
        else
        {
            _disabledActions.Add(action);
            _holdSeconds[action] = 0.0;
            _prev[action] = false;
            _curr[action] = false;
        }
    }

    /// <summary>
    ///     Enables or disables a specific axis regardless of its set membership.
    ///     Disabling clears cached values and smoothing state.
    /// </summary>
    public void EnableAxis(TAxis axis, bool enabled)
    {
        if (enabled)
        {
            _disabledAxes.Remove(axis);
        }
        else
        {
            _disabledAxes.Add(axis);
            _axisValues[axis] = 0f;
            _axisRawValues[axis] = 0f;
            _axisSmoothed.Remove(axis);
        }
    }

    /// <summary>
    ///     Enables or disables an entire action set. Actions in disabled sets will not be evaluated.
    /// </summary>
    public void EnableSet(string setName, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(setName))
        {
            return;
        }

        if (enabled)
        {
            _enabledSets.Add(setName);
        }
        else
        {
            _enabledSets.Remove(setName);
        }
    }

    /// <summary>Ends a batch update and emits a single notification if changes occurred.</summary>
    public void EndBindingsUpdate()
    {
        if (_bindingsBatchDepth == 0)
        {
            return;
        }

        _bindingsBatchDepth--;
        if (_bindingsBatchDepth == 0 && _bindingsChangedDuringBatch)
        {
            _bindingsChangedDuringBatch = false;
            OnBindingsChanged?.Invoke();
        }
    }

    /// <summary>
    ///     Exports a minimal JSON that only contains differences vs a defaults instance.
    ///     Useful to persist user overrides compactly.
    /// </summary>
    public string ExportDiffJson(InputActions<TAction, TAxis> defaults)
    {
        var current = BuildPersistRoot();
        var baseDoc = defaults.BuildPersistRoot();

        var outDoc = new PersistRoot
        {
            Version = 1,
            Actions = new Dictionary<string, List<PersistActionBinding>>(),
            Axes = new Dictionary<string, List<PersistAxisBinding>>()
        };

        // Actions diff
        if (current.Actions != null)
        {
            foreach (var kv in current.Actions)
            {
                var name = kv.Key;
                var curList = kv.Value;

                List<PersistActionBinding>? baseList = null;
                var hasBase = baseDoc.Actions != null && baseDoc.Actions.TryGetValue(name, out baseList);

                if (!hasBase || baseList is null || !ActionListsEqual(curList, baseList))
                {
                    outDoc.Actions![name] = curList;
                }
            }
        }

        // Axes diff
        if (current.Axes != null)
        {
            foreach (var kv in current.Axes)
            {
                var name = kv.Key;
                var curList = kv.Value;

                List<PersistAxisBinding>? baseList = null;
                var hasBase = baseDoc.Axes != null && baseDoc.Axes.TryGetValue(name, out baseList);

                if (!hasBase || baseList is null || !AxisListsEqual(curList, baseList))
                {
                    outDoc.Axes![name] = curList;
                }
            }
        }

        return JsonSerializer.Serialize(outDoc, new JsonSerializerOptions { WriteIndented = true });

        // ——— Local helpers (type-aware equality, order-sensitive) ———

        static bool ActionListsEqual(List<PersistActionBinding> a, List<PersistActionBinding> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Count; i++)
            {
                if (!ActionPersistEquals(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        static bool AxisListsEqual(List<PersistAxisBinding> a, List<PersistAxisBinding> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Count; i++)
            {
                if (!AxisPersistEquals(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        static bool ActionPersistEquals(PersistActionBinding a, PersistActionBinding b)
        {
            if (!StrEq(a.Type, b.Type))
            {
                return false;
            }

            switch (a.Type)
            {
                case "KeyChord":
                    return StrEq(a.Key, b.Key)
                           && StrEq(a.Mods, b.Mods);

                case "MouseButton":
                    return StrEq(a.MouseButton, b.MouseButton);

                case "GamepadButton":
                    return StrEq(a.Button, b.Button)
                           && IntEq(a.Pad, b.Pad)
                           && StringListEq(a.With, b.With);

                case "GamepadTrigger":
                    return StrEq(a.Axis, b.Axis)
                           && FloatEq(a.Press, b.Press)
                           && FloatEq(a.Release, b.Release)
                           && IntEq(a.Pad, b.Pad);
            }

            // Fallback
            return StrEq(a.Axis, b.Axis)
                   && StrEq(a.Button, b.Button)
                   && StrEq(a.Key, b.Key)
                   && StrEq(a.Mods, b.Mods)
                   && StrEq(a.MouseButton, b.MouseButton)
                   && IntEq(a.Pad, b.Pad)
                   && FloatEq(a.Press, b.Press)
                   && FloatEq(a.Release, b.Release)
                   && StrEq(a.Type, b.Type)
                   && StringListEq(a.With, b.With);
        }

        static bool AxisPersistEquals(PersistAxisBinding a, PersistAxisBinding b)
        {
            if (!StrEq(a.Type, b.Type))
            {
                return false;
            }

            switch (a.Type)
            {
                case "Composite2Keys":
                    return StrEq(a.Negative, b.Negative)
                           && StrEq(a.Positive, b.Positive)
                           && FloatEq(a.Scale, b.Scale)
                           && BoolEq(a.Invert, b.Invert);

                case "MouseAxis":
                    return StrEq(a.MouseAxis, b.MouseAxis)
                           && FloatEq(a.Scale, b.Scale)
                           && BoolEq(a.Invert, b.Invert);

                case "GamepadAxis":
                    return StrEq(a.Axis, b.Axis)
                           && FloatEq(a.Deadzone, b.Deadzone)
                           && FloatEq(a.Sensitivity, b.Sensitivity)
                           && BoolEq(a.Invert, b.Invert)
                           && FloatEq(a.Curve, b.Curve)
                           && IntEq(a.Pad, b.Pad);
            }

            // Fallback
            return StrEq(a.Axis, b.Axis)
                   && FloatEq(a.Curve, b.Curve)
                   && FloatEq(a.Deadzone, b.Deadzone)
                   && BoolEq(a.Invert, b.Invert)
                   && StrEq(a.MouseAxis, b.MouseAxis)
                   && StrEq(a.Negative, b.Negative)
                   && IntEq(a.Pad, b.Pad)
                   && StrEq(a.Positive, b.Positive)
                   && FloatEq(a.Scale, b.Scale)
                   && FloatEq(a.Sensitivity, b.Sensitivity)
                   && StrEq(a.Type, b.Type);
        }

        static bool StrEq(string? a, string? b)
        {
            return string.Equals(a, b, StringComparison.Ordinal);
        }

        static bool BoolEq(bool? a, bool? b)
        {
            return a.GetValueOrDefault(false) == b.GetValueOrDefault(false) || (a is null && b is null);
        }

        static bool IntEq(int? a, int? b)
        {
            return a == b;
        }

        static bool FloatEq(float? a, float? b)
        {
            if (a is null && b is null)
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            return System.Math.Abs(a.Value - b.Value) <= 1e-6f;
        }

        static bool StringListEq(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
        {
            a ??= Array.Empty<string>();
            b ??= Array.Empty<string>();
            if (a.Count != b.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Count; i++)
            {
                if (!StrEq(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>Returns the smoothed value for the axis (or raw if smoothing is disabled).</summary>
    public float Get(TAxis axis)
    {
        return _axisValues.TryGetValue(axis, out var v) ? v : 0f;
    }

    /// <summary>Convenience to query two axes as a 2D vector.</summary>
    public Vector2 Get2D(TAxis xAxis, TAxis yAxis)
    {
        return new Vector2(Get(xAxis), Get(yAxis));
    }

    /// <summary>Returns a readonly view of the bindings for the action.</summary>
    public IReadOnlyList<IActionBinding> GetActionBindings(TAction action)
    {
        return _actionBindings.TryGetValue(action, out var list) ? list : Array.Empty<IActionBinding>();
    }

    /// <summary>Returns a readonly view of the bindings for the axis.</summary>
    public IReadOnlyList<IAxisBinding> GetAxisBindings(TAxis axis)
    {
        return _axisBindings.TryGetValue(axis, out var list) ? list : Array.Empty<IAxisBinding>();
    }

    /// <summary>Total continuous time (seconds) that the action has been held down.</summary>
    public double GetHoldSeconds(TAction action)
    {
        return _holdSeconds.TryGetValue(action, out var s) ? s : 0.0;
    }

    /// <summary>Returns the last device that caused the action to be down.</summary>
    public InputDevice GetLastSource(TAction action)
    {
        return _lastActionSource.TryGetValue(action, out var d) ? d : InputDevice.None;
    }

    /// <summary>Returns the last device that contributed a non-zero value to the axis.</summary>
    public InputDevice GetLastSource(TAxis axis)
    {
        return _lastAxisSource.TryGetValue(axis, out var d) ? d : InputDevice.None;
    }

    /// <summary>Returns the raw combined value for the axis before smoothing.</summary>
    public float GetRaw(TAxis axis)
    {
        return _axisRawValues.TryGetValue(axis, out var v) ? v : 0f;
    }

    /// <summary>Returns true if there is at least one binding for the action.</summary>
    public bool HasAction(TAction action)
    {
        return _actionBindings.ContainsKey(action);
    }

    /// <summary>Returns true if there is at least one binding for the axis.</summary>
    public bool HasAxis(TAxis axis)
    {
        return _axisBindings.ContainsKey(axis);
    }

    // Queries ----------------------------------------------------------------

    /// <summary>Returns true if the action is currently down.</summary>
    public bool IsDown(TAction action)
    {
        return _curr.TryGetValue(action, out var v) && v;
    }

    /// <summary>Loads bindings from a JSON string, replacing existing ones.</summary>
    public void LoadDefaultsFromJson(string json)
    {
        LoadFromJson(json);
    }

    /// <summary>Loads bindings from a JSON string, replacing existing ones, with optional strict enum parsing.</summary>
    public void LoadDefaultsFromJson(string json, bool strictEnums)
    {
        LoadFromJson(json, strictEnums);
    }

    /// <summary>Loads bindings from a stream, replacing existing ones.</summary>
    public void LoadDefaultsFromStream(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        LoadFromJson(reader.ReadToEnd());
    }

    /// <summary>Replaces all current bindings from JSON (keeps sets/repeat/smoothing configuration).</summary>
    public void LoadFromJson(string json)
    {
        LoadFromJson(json, false);
    }

    /// <summary>
    ///     Replaces all current bindings from JSON (keeps sets/repeat/smoothing configuration),
    ///     with optional strict enum parsing.
    /// </summary>
    public void LoadFromJson(string json, bool strictEnums)
    {
        var doc = JsonSerializer.Deserialize<PersistRoot>(json)
                  ?? throw new ArgumentException("Invalid input actions JSON.");

        using var _ = BeginBindingsBatch();

        _actionBindings.Clear();
        _axisBindings.Clear();
        _axisValues.Clear();
        _axisRawValues.Clear();
        _prev.Clear();
        _curr.Clear();
        _pressedConsumed.Clear();
        _holdSeconds.Clear();
        _lastActionSource.Clear();
        _lastAxisSource.Clear();

        // Actions
        if (doc.Actions != null)
        {
            foreach (var kv in doc.Actions)
            {
                if (!Enum.TryParse<TAction>(kv.Key, true, out var action))
                {
                    if (strictEnums)
                    {
                        throw new ArgumentException($"Unknown action enum '{kv.Key}'.");
                    }

                    continue;
                }

                var list = new List<IActionBinding>(kv.Value.Count);
                foreach (var p in kv.Value)
                {
                    if (TryCreateActionBinding(p, out var binding))
                    {
                        list.Add(binding!);
                    }
                }

                _actionBindings[action] = list;
            }
        }

        // Axes
        if (doc.Axes != null)
        {
            foreach (var kv in doc.Axes)
            {
                if (!Enum.TryParse<TAxis>(kv.Key, true, out var axis))
                {
                    if (strictEnums)
                    {
                        throw new ArgumentException($"Unknown axis enum '{kv.Key}'.");
                    }

                    continue;
                }

                var list = new List<IAxisBinding>(kv.Value.Count);
                foreach (var p in kv.Value)
                {
                    if (TryCreateAxisBinding(p, out var binding))
                    {
                        list.Add(binding!);
                    }
                }

                _axisBindings[axis] = list;
            }
        }

        // Coalesce one notification for the whole load
        NotifyBindingsChanged();
    }

    /// <summary>Merges bindings from JSON with default Replace mode.</summary>
    public void MergeFromJson(string json)
    {
        MergeFromJson(json, false, MergeMode.Replace);
    }

    /// <summary>Merges bindings from JSON with optional strict enum parsing; default Replace mode.</summary>
    public void MergeFromJson(string json, bool strictEnums)
    {
        MergeFromJson(json, strictEnums, MergeMode.Replace);
    }

    /// <summary>
    ///     Merges bindings from JSON using specified mode.
    /// </summary>
    /// <param name="json">Bindings JSON document.</param>
    /// <param name="strictEnums">If true, unknown enum names cause an exception.</param>
    /// <param name="mode">Merge behavior for incoming bindings.</param>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see cref="MergeMode.Replace" />: replace existing bindings for a given action/axis.</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="MergeMode.Append" />: append unique bindings (de-duplicated) to existing lists.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <exception cref="ArgumentException">
    ///     Thrown for invalid JSON or, when <paramref name="strictEnums" /> is true, unknown
    ///     enum names.
    /// </exception>
    public void MergeFromJson(string json, bool strictEnums, MergeMode mode)
    {
        var doc = JsonSerializer.Deserialize<PersistRoot>(json)
                  ?? throw new ArgumentException("Invalid input actions JSON.");

        using var _ = BeginBindingsBatch();

        // Actions overlay
        if (doc.Actions != null)
        {
            foreach (var kv in doc.Actions)
            {
                if (!Enum.TryParse<TAction>(kv.Key, true, out var action))
                {
                    if (strictEnums)
                    {
                        throw new ArgumentException($"Unknown action enum '{kv.Key}'.");
                    }

                    continue;
                }

                var incomingList = new List<IActionBinding>(kv.Value.Count);
                foreach (var p in kv.Value)
                {
                    if (TryCreateActionBinding(p, out var binding))
                    {
                        incomingList.Add(binding!);
                    }
                }

                if (mode == MergeMode.Replace || !_actionBindings.TryGetValue(action, out var list))
                {
                    _actionBindings[action] = incomingList;
                }
                else
                    // Append unique
                {
                    for (var i = 0; i < incomingList.Count; i++)
                    {
                        AddUniqueActionBinding(list, incomingList[i]);
                    }
                }

                // Reset state for this action
                _prev.Remove(action);
                _curr.Remove(action);
                _pressedConsumed.Remove(action);
                _holdSeconds.Remove(action);
                _repeatTimer.Remove(action);
                _repeatFiredThisFrame.Remove(action);
            }
        }

        // Axes overlay
        if (doc.Axes != null)
        {
            foreach (var kv in doc.Axes)
            {
                if (!Enum.TryParse<TAxis>(kv.Key, true, out var axis))
                {
                    if (strictEnums)
                    {
                        throw new ArgumentException($"Unknown axis enum '{kv.Key}'.");
                    }

                    continue;
                }

                var incomingList = new List<IAxisBinding>(kv.Value.Count);
                foreach (var p in kv.Value)
                {
                    if (TryCreateAxisBinding(p, out var binding))
                    {
                        incomingList.Add(binding!);
                    }
                }

                if (mode == MergeMode.Replace || !_axisBindings.TryGetValue(axis, out var list))
                {
                    _axisBindings[axis] = incomingList;
                }
                else
                {
                    for (var i = 0; i < incomingList.Count; i++)
                    {
                        AddUniqueAxisBinding(list, incomingList[i]);
                    }
                }

                _axisValues.Remove(axis);
                _axisRawValues.Remove(axis);
                _axisSmoothed.Remove(axis);
            }
        }

        // Coalesce one notification for the whole merge
        NotifyBindingsChanged();
    }

    /// <summary>Merges bindings from a stream (UTF-8) using default Replace mode.</summary>
    public void MergeFromStream(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        MergeFromJson(reader.ReadToEnd());
    }

    /// <summary>
    ///     Clears all transient state (edges, repeat timers, smoothed axis cache, holds).
    ///     Call on focus lost or when swapping profiles.
    /// </summary>
    public void ResetState()
    {
        _prev.Clear();
        _curr.Clear();
        _pressedConsumed.Clear();
        _repeatTimer.Clear();
        _repeatFiredThisFrame.Clear();
        _axisValues.Clear();
        _axisRawValues.Clear();
        _axisSmoothed.Clear();
        _holdSeconds.Clear();
    }

    /// <summary>
    ///     Assigns an optional set name to an action. If null/empty, the action is always enabled unless explicitly disabled.
    /// </summary>
    public void SetActionSet(TAction action, string? setName)
    {
        _actionSet[action] = string.IsNullOrWhiteSpace(setName) ? null : setName;
    }

    /// <summary>Sets how bindings for a specific axis are combined.</summary>
    public void SetAxisCombinePolicy(TAxis axis, AxisCombinePolicy policy)
    {
        _axisCombine[axis] = policy;
    }

    /// <summary>
    ///     Sets exponential smoothing using a fixed alpha per frame (0..1).
    ///     1 means instantaneous (no smoothing), lower values are smoother.
    /// </summary>
    public void SetAxisSmoothing(TAxis axis, float alpha)
    {
        if (alpha < 0f)
        {
            alpha = 0f;
        }

        if (alpha > 1f)
        {
            alpha = 1f;
        }

        _axisSmoothingAlpha[axis] = alpha;
        _axisSmoothingTau.Remove(axis); // alpha takes precedence
        if (!_axisSmoothed.ContainsKey(axis))
        {
            _axisSmoothed[axis] = 0f; // seed
        }
    }

    /// <summary>
    ///     Sets frame-rate independent smoothing using a time constant (tau, seconds).
    ///     Each frame: sm += (1 - e^(-dt/tau)) * (raw - sm).
    /// </summary>
    public void SetAxisSmoothingTimeConstant(TAxis axis, float tauSeconds)
    {
        if (tauSeconds <= 0f)
        {
            _axisSmoothingTau.Remove(axis);
            return;
        }

        _axisSmoothingTau[axis] = tauSeconds;
        _axisSmoothingAlpha.Remove(axis); // tau takes precedence
        if (!_axisSmoothed.ContainsKey(axis))
        {
            _axisSmoothed[axis] = 0f; // seed
        }
    }

    /// <summary>
    ///     Enables per-action repeat. While the action is held, repeats will fire after an initial delay and then at the given
    ///     interval.
    /// </summary>
    /// <param name="initialDelaySeconds">Delay before the first repeat (seconds).</param>
    /// <param name="repeatSeconds">Interval between repeats (seconds).</param>
    public void SetRepeat(TAction action, bool enabled, double initialDelaySeconds = 0.35, double repeatSeconds = 0.05)
    {
        _repeatCfg[action] = new RepeatConfig(enabled, initialDelaySeconds, repeatSeconds);
    }

    // ----------------- Persistence (bindings only; sets/repeat/smoothing not persisted) -----------------

    /// <summary>
    ///     Serializes bindings to a JSON string. Only bindings are persisted; sets/repeat/smoothing are not.
    /// </summary>
    public string ToJson()
    {
        var doc = BuildPersistRoot();
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    ///     Consumes the pressed edge for an action (once per frame) to avoid handling the same press in multiple systems.
    /// </summary>
    /// <returns>True if the pressed edge existed and was consumed by this call.</returns>
    public bool TryConsumePressed(TAction action)
    {
        if (!WasPressed(action))
        {
            return false;
        }

        if (_pressedConsumed.Contains(action))
        {
            return false;
        }

        _pressedConsumed.Add(action);
        return true;
    }

    /// <summary>
    ///     Removes a specific action binding instance (reference equality). Useful for runtime rebind UIs.
    ///     Triggers <see cref="OnBindingsChanged" /> if removed.
    /// </summary>
    public bool TryUnbindActionBinding(TAction action, IActionBinding binding)
    {
        if (_actionBindings.TryGetValue(action, out var list))
        {
            if (list.Remove(binding))
            {
                NotifyBindingsChanged();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Removes a specific axis binding instance (reference equality). Useful for runtime rebind UIs.
    ///     Triggers <see cref="OnBindingsChanged" /> if removed.
    /// </summary>
    public bool TryUnbindAxisBinding(TAxis axis, IAxisBinding binding)
    {
        if (_axisBindings.TryGetValue(axis, out var list))
        {
            if (list.Remove(binding))
            {
                NotifyBindingsChanged();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Removes all bindings for an action and clears its runtime state.
    ///     Triggers <see cref="OnBindingsChanged" />.
    /// </summary>
    public void UnbindAction(TAction action)
    {
        _actionBindings.Remove(action);
        _prev.Remove(action);
        _curr.Remove(action);
        _pressedConsumed.Remove(action);
        _holdSeconds.Remove(action);
        _repeatTimer.Remove(action);
        _repeatFiredThisFrame.Remove(action);
        _lastActionSource.Remove(action);
        NotifyBindingsChanged();
    }

    /// <summary>
    ///     Removes all bindings for an axis and clears cached values and smoothing state.
    ///     Triggers <see cref="OnBindingsChanged" />.
    /// </summary>
    public void UnbindAxis(TAxis axis)
    {
        _axisBindings.Remove(axis);
        _axisValues.Remove(axis);
        _axisRawValues.Remove(axis);
        _axisSmoothed.Remove(axis);
        _axisSmoothingAlpha.Remove(axis);
        _axisSmoothingTau.Remove(axis);
        _axisCombine.Remove(axis);
        _lastAxisSource.Remove(axis);
        NotifyBindingsChanged();
    }

    /// <summary>
    ///     Per-frame update. Computes action edges/hold/repeat and axis values (combine + smoothing).
    /// </summary>
    /// <param name="kb">Keyboard input accessor for this frame.</param>
    /// <param name="mods">Snapshot of currently active keyboard modifiers.</param>
    /// <param name="mouse">Mouse input accessor for this frame.</param>
    /// <param name="pads">Gamepad collection to resolve the active pad from.</param>
    /// <param name="dt">Delta time in seconds since the last update (internally clamped to 0.25 for stability).</param>
    public void Update(IKeyboard kb, KeyboardModifiers mods, IMouse mouse, IGamepads pads, double dt)
    {
        // Clamp huge spikes (e.g., debugger pauses) to keep smoothing stable
        if (dt > 0.25)
        {
            dt = 0.25;
        }

        var pad = ResolvePad(pads);
        _repeatFiredThisFrame.Clear();
        _pressedConsumed.Clear();

        // Actions: prev <- curr, then recompute curr
        foreach (var k in _curr.Keys)
        {
            _prev[k] = _curr[k];
        }

        foreach (var kv in _actionBindings)
        {
            var action = kv.Key;
            var binds = kv.Value;
            var down = false;

            if (!_disabledActions.Contains(action) && IsActionSetEnabled(action))
                // First binding that reports down determines device source
            {
                for (var i = 0; i < binds.Count; i++)
                {
                    if (binds[i].IsDown(kb, mods, mouse, pad))
                    {
                        down = true;
                        _lastActionSource[action] = DeviceFromBinding(binds[i]);
                        break;
                    }
                }
            }

            _curr[action] = down;
            if (!_prev.ContainsKey(action))
            {
                _prev[action] = false; // seed
            }

            // Hold duration
            if (down)
            {
                var s = 0.0;
                if (_holdSeconds.TryGetValue(action, out var hs))
                {
                    s = hs;
                }

                s += dt;
                _holdSeconds[action] = s;
            }
            else
            {
                _holdSeconds[action] = 0.0;
            }

            // Repeat logic (fires zero or more times per frame depending on remaining timer)
            if (_repeatCfg.TryGetValue(action, out var cfg) && cfg.Enabled)
            {
                var wasDown = _prev[action];

                if (down && !wasDown)
                {
                    _repeatTimer[action] = cfg.InitialDelay;
                }
                else if (down && wasDown)
                {
                    if (_repeatTimer.TryGetValue(action, out var t))
                    {
                        t -= dt;
                        while (t <= 0.0)
                        {
                            _repeatFiredThisFrame.Add(action);
                            t += cfg.Interval;
                        }

                        _repeatTimer[action] = t;
                    }
                    else
                    {
                        _repeatTimer[action] = cfg.InitialDelay;
                    }
                }
                else
                {
                    _repeatTimer.Remove(action);
                }
            }
            else
            {
                _repeatTimer.Remove(action);
            }
        }

        // Axes: compute and cache (with combine + smoothing)
        _axisValues.Clear();
        _axisRawValues.Clear();

        foreach (var kv in _axisBindings)
        {
            var axis = kv.Key;

            if (_disabledAxes.Contains(axis))
            {
                _axisValues[axis] = 0f;
                _axisRawValues[axis] = 0f;
                _axisSmoothed.Remove(axis);
                continue;
            }

            var binds = kv.Value;
            var policy = _axisCombine.TryGetValue(axis, out var p) ? p : AxisCombinePolicy.MaxAbs;

            var combined = 0f;
            var chosenDevice = InputDevice.None;

            // Combine contributions according to the selected policy
            switch (policy)
            {
                case AxisCombinePolicy.MaxAbs:
                {
                    var maxAbs = 0f;
                    for (var i = 0; i < binds.Count; i++)
                    {
                        var v = binds[i].Get(kb, mouse, pad, dt);
                        var av = MathF.Abs(v);
                        if (av > maxAbs)
                        {
                            maxAbs = av;
                            combined = v;
                            chosenDevice = DeviceFromBinding(binds[i]);
                        }
                    }

                    break;
                }
                case AxisCombinePolicy.SumClamped:
                {
                    var sum = 0f;
                    var dominantAbs = 0f;
                    for (var i = 0; i < binds.Count; i++)
                    {
                        var v = binds[i].Get(kb, mouse, pad, dt);
                        sum += v;
                        var av = MathF.Abs(v);
                        if (av >= dominantAbs)
                        {
                            dominantAbs = av;
                            chosenDevice = DeviceFromBinding(binds[i]);
                        }
                    }

                    combined = System.Math.Clamp(sum, -1f, 1f);
                    break;
                }
                case AxisCombinePolicy.FirstNonZero:
                {
                    const float eps = 1e-3f;
                    for (var i = 0; i < binds.Count; i++)
                    {
                        var v = binds[i].Get(kb, mouse, pad, dt);
                        if (MathF.Abs(v) > eps)
                        {
                            combined = v;
                            chosenDevice = DeviceFromBinding(binds[i]);
                            break;
                        }
                    }

                    break;
                }
            }

            // Cache raw value before smoothing for debugging/telemetry/custom filters
            _axisRawValues[axis] = combined;

            // Smoothing (tau has priority; else alpha)
            if (_axisSmoothingTau.TryGetValue(axis, out var tau))
            {
                if (!_axisSmoothed.TryGetValue(axis, out var sm))
                {
                    sm = 0f;
                }

                var alpha = (float)(1.0 - System.Math.Exp(-(dt / System.Math.Max(1e-6, tau))));
                sm = sm + alpha * (combined - sm);
                _axisSmoothed[axis] = sm;
                _axisValues[axis] = sm;
            }
            else if (_axisSmoothingAlpha.TryGetValue(axis, out var alpha))
            {
                if (!_axisSmoothed.TryGetValue(axis, out var sm))
                {
                    sm = 0f;
                }

                sm = sm + alpha * (combined - sm);
                _axisSmoothed[axis] = sm;
                _axisValues[axis] = sm;
            }
            else
            {
                _axisValues[axis] = combined;
            }

            if (chosenDevice != InputDevice.None)
            {
                _lastAxisSource[axis] = chosenDevice;
            }
        }
    }

    /// <summary>Returns true if the action transitioned from up to down this frame.</summary>
    public bool WasPressed(TAction action)
    {
        var c = _curr.TryGetValue(action, out var cv) && cv;
        var p = _prev.TryGetValue(action, out var pv) && pv;
        return c && !p;
    }

    /// <summary>
    ///     Convenience: true if the action was pressed (edge) or repeated this frame.
    /// </summary>
    public bool WasPressedOrRepeated(TAction action)
    {
        return WasPressed(action) || WasRepeated(action);
    }

    /// <summary>
    ///     Returns true if the action transitioned from down to up this frame.
    /// </summary>
    public bool WasReleased(TAction action)
    {
        var c = _curr.TryGetValue(action, out var cv) && cv;
        var p = _prev.TryGetValue(action, out var pv) && pv;
        return !c && p;
    }

    /// <summary>
    ///     Returns true if a repeat fired for the action on this frame.
    /// </summary>
    public bool WasRepeated(TAction action)
    {
        return _repeatFiredThisFrame.Contains(action);
    }

    /// <summary>
    ///     Writes bindings JSON to a stream in UTF-8. Only bindings are persisted.
    /// </summary>
    public void WriteJsonTo(Stream stream)
    {
        var doc = BuildPersistRoot();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        JsonSerializer.Serialize(writer, doc);
        writer.Flush();
    }

    private static bool ActionBindingEquals(IActionBinding a, IActionBinding b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a.GetType() != b.GetType())
        {
            return false;
        }

        switch (a)
        {
            case KeyChordBinding ak when b is KeyChordBinding bk:
                return ak.Chord.Key.Equals(bk.Chord.Key) && ak.Chord.Modifiers.Equals(bk.Chord.Modifiers);
            case MouseButtonBinding am when b is MouseButtonBinding bm:
                return am.Button.Equals(bm.Button);
            case GamepadButtonBinding ag when b is GamepadButtonBinding bg:
            {
                if (!ag.Button.Equals(bg.Button))
                {
                    return false;
                }

                if ((ag.PadIndex ?? -1) != (bg.PadIndex ?? -1))
                {
                    return false;
                }

                var aw = ag.With;
                var bw = bg.With;
                if (aw.Count != bw.Count)
                {
                    return false;
                }

                for (var i = 0; i < aw.Count; i++)
                {
                    if (!aw[i].Equals(bw[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
            case GamepadTriggerBinding at when b is GamepadTriggerBinding bt:
                return at.Axis.Equals(bt.Axis)
                       && System.Math.Abs(at.PressThreshold - bt.PressThreshold) < 1e-6f
                       && System.Math.Abs(at.ReleaseThreshold - bt.ReleaseThreshold) < 1e-6f
                       && (at.PadIndex ?? -1) == (bt.PadIndex ?? -1);
        }

        return false;
    }

    // Duplicate guard helpers -------------------------------------------------
    // Avoids adding semantically-identical bindings to the same action/axis.

    private static void AddUniqueActionBinding(List<IActionBinding> list, IActionBinding binding)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (ActionBindingEquals(list[i], binding))
            {
                return;
            }
        }

        list.Add(binding);
    }

    private static void AddUniqueAxisBinding(List<IAxisBinding> list, IAxisBinding binding)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (AxisBindingEquals(list[i], binding))
            {
                return;
            }
        }

        list.Add(binding);
    }

    private static bool AxisBindingEquals(IAxisBinding a, IAxisBinding b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a.GetType() != b.GetType())
        {
            return false;
        }

        switch (a)
        {
            case Composite2KeysAxisBinding ak when b is Composite2KeysAxisBinding bk:
                return ak.Negative.Equals(bk.Negative)
                       && ak.Positive.Equals(bk.Positive)
                       && System.Math.Abs(ak.Scale - bk.Scale) < 1e-6f
                       && ak.Invert == bk.Invert;
            case MouseAxisBinding am when b is MouseAxisBinding bm:
                return am.Axis.Equals(bm.Axis)
                       && System.Math.Abs(am.Scale - bm.Scale) < 1e-6f
                       && am.Invert == bm.Invert;
            case GamepadAxisBinding ag when b is GamepadAxisBinding bg:
                return ag.Axis.Equals(bg.Axis)
                       && System.Math.Abs(ag.DeadZone - bg.DeadZone) < 1e-6f
                       && System.Math.Abs(ag.Sensitivity - bg.Sensitivity) < 1e-6f
                       && System.Math.Abs(ag.Curve - bg.Curve) < 1e-6f
                       && ag.Invert == bg.Invert
                       && (ag.PadIndex ?? -1) == (bg.PadIndex ?? -1);
        }

        return false;
    }

    private static InputDevice DeviceFromBinding(object binding)
    {
        return binding switch
        {
            KeyChordBinding => InputDevice.Keyboard,
            MouseButtonBinding => InputDevice.Mouse,
            MouseAxisBinding => InputDevice.Mouse,
            GamepadButtonBinding => InputDevice.Gamepad,
            GamepadTriggerBinding => InputDevice.Gamepad,
            GamepadAxisBinding => InputDevice.Gamepad,
            _ => InputDevice.None
        };
    }

    private static bool TryCreateActionBinding(PersistActionBinding p, out IActionBinding? binding)
    {
        binding = null;

        switch (p.Type)
        {
            case "KeyChord":
                if (Enum.TryParse<Key>(p.Key, true, out var k))
                {
                    Enum.TryParse<KeyboardModifiers>(p.Mods ?? "None", true, out var mods);
                    binding = new KeyChordBinding(new KeyChord(k, mods));
                }

                break;

            case "MouseButton":
                if (Enum.TryParse<MouseButton>(p.MouseButton, true, out var mb))
                {
                    binding = new MouseButtonBinding(mb);
                }

                break;

            case "GamepadButton":
                if (Enum.TryParse<GamepadButton>(p.Button, true, out var gb))
                {
                    var with = new List<GamepadButton>();
                    if (p.With != null)
                    {
                        foreach (var s in p.With)
                        {
                            if (Enum.TryParse<GamepadButton>(s, true, out var wb))
                            {
                                with.Add(wb);
                            }
                        }
                    }

                    binding = new GamepadButtonBinding(gb, p.Pad, with);
                }

                break;

            case "GamepadTrigger":
                if (Enum.TryParse<GamepadAxis>(p.Axis, true, out var ga))
                {
                    binding = new GamepadTriggerBinding(ga, p.Press ?? 0.5f, p.Release ?? 0.45f, p.Pad);
                }

                break;
        }

        return binding != null;
    }

    private static bool TryCreateAxisBinding(PersistAxisBinding p, out IAxisBinding? binding)
    {
        binding = null;

        switch (p.Type)
        {
            case "Composite2Keys":
                if (Enum.TryParse<Key>(p.Negative, true, out var nk) &&
                    Enum.TryParse<Key>(p.Positive, true, out var pk))
                {
                    binding = new Composite2KeysAxisBinding(nk, pk, p.Scale ?? 1f) { Invert = p.Invert ?? false };
                }

                break;

            case "MouseAxis":
                if (Enum.TryParse<MouseAxis>(p.MouseAxis, true, out var ma))
                {
                    binding = new MouseAxisBinding(ma, p.Scale ?? 1f, p.Invert ?? false);
                }

                break;

            case "GamepadAxis":
                if (Enum.TryParse<GamepadAxis>(p.Axis, true, out var ga))
                {
                    binding = new GamepadAxisBinding(
                        ga,
                        p.Deadzone ?? 0.15f,
                        p.Sensitivity ?? 1f,
                        p.Invert ?? false,
                        p.Curve ?? 1f,
                        p.Pad);
                }

                break;
        }

        return binding != null;
    }

    // ---- Internal helpers for (de)serialization

    private PersistRoot BuildPersistRoot()
    {
        var doc = new PersistRoot
        {
            Version = 1,
            Actions = new Dictionary<string, List<PersistActionBinding>>(),
            Axes = new Dictionary<string, List<PersistAxisBinding>>()
        };

        foreach (var kv in _actionBindings)
        {
            var list = new List<PersistActionBinding>(kv.Value.Count);
            foreach (var b in kv.Value)
            {
                switch (b)
                {
                    case KeyChordBinding kb:
                        list.Add(new PersistActionBinding
                        {
                            Type = "KeyChord",
                            Key = kb.Chord.Key.ToString(),
                            Mods = kb.Chord.Modifiers.ToString()
                        });
                        break;
                    case MouseButtonBinding mb:
                        list.Add(new PersistActionBinding
                        {
                            Type = "MouseButton",
                            MouseButton = mb.Button.ToString()
                        });
                        break;
                    case GamepadButtonBinding gb:
                        list.Add(new PersistActionBinding
                        {
                            Type = "GamepadButton",
                            Button = gb.Button.ToString(),
                            Pad = gb.PadIndex,
                            With = gb.With?.ConvertAll(x => x.ToString()) ?? new List<string>()
                        });
                        break;
                    case GamepadTriggerBinding gt:
                        list.Add(new PersistActionBinding
                        {
                            Type = "GamepadTrigger",
                            Axis = gt.Axis.ToString(),
                            Pad = gt.PadIndex,
                            Press = gt.PressThreshold,
                            Release = gt.ReleaseThreshold
                        });
                        break;
                }
            }

            doc.Actions![kv.Key.ToString()] = list;
        }

        foreach (var kv in _axisBindings)
        {
            var list = new List<PersistAxisBinding>(kv.Value.Count);
            foreach (var b in kv.Value)
            {
                switch (b)
                {
                    case Composite2KeysAxisBinding ck:
                        list.Add(new PersistAxisBinding
                        {
                            Type = "Composite2Keys",
                            Negative = ck.Negative.ToString(),
                            Positive = ck.Positive.ToString(),
                            Scale = ck.Scale,
                            Invert = ck.Invert
                        });
                        break;
                    case MouseAxisBinding ma:
                        list.Add(new PersistAxisBinding
                        {
                            Type = "MouseAxis",
                            MouseAxis = ma.Axis.ToString(),
                            Scale = ma.Scale,
                            Invert = ma.Invert
                        });
                        break;
                    case GamepadAxisBinding ga:
                        list.Add(new PersistAxisBinding
                        {
                            Type = "GamepadAxis",
                            Axis = ga.Axis.ToString(),
                            Deadzone = ga.DeadZone,
                            Sensitivity = ga.Sensitivity,
                            Invert = ga.Invert,
                            Curve = ga.Curve,
                            Pad = ga.PadIndex
                        });
                        break;
                }
            }

            doc.Axes![kv.Key.ToString()] = list;
        }

        return doc;
    }

    private bool IsActionSetEnabled(TAction action)
    {
        if (!_actionSet.TryGetValue(action, out var set) || string.IsNullOrWhiteSpace(set))
        {
            return true; // no set means always enabled
        }

        return _enabledSets.Contains(set);
    }

    private void NotifyBindingsChanged()
    {
        if (_bindingsBatchDepth > 0)
        {
            _bindingsChangedDuringBatch = true;
            return;
        }

        OnBindingsChanged?.Invoke();
    }

    private IGamepad? ResolvePad(IGamepads pads)
    {
        if (pads == null)
        {
            return null;
        }

        if (PlayerGamepadIndex is int ix)
        {
            return pads.Get(ix);
        }

        return pads.Primary;
    }
}