namespace Brine2D.Animation;

/// <summary>
/// An immutable snapshot of an <see cref="AnimationParameters"/> store.
/// Capture via <see cref="AnimationParameters.CaptureSnapshot"/> and restore via
/// <see cref="AnimationParameters.RestoreSnapshot"/>. Useful for cutscenes or ability-override
/// systems that need to temporarily replace the parameter set and then cleanly revert.
/// </summary>
public sealed class AnimationParametersSnapshot
{
    internal IReadOnlyDictionary<string, bool> Bools { get; }
    internal IReadOnlyDictionary<string, float> Floats { get; }
    internal IReadOnlyDictionary<string, int> Ints { get; }
    internal IReadOnlySet<string> Triggers { get; }

    internal AnimationParametersSnapshot(
        IReadOnlyDictionary<string, bool> bools,
        IReadOnlyDictionary<string, float> floats,
        IReadOnlyDictionary<string, int> ints,
        IReadOnlySet<string> triggers)
    {
        Bools = bools;
        Floats = floats;
        Ints = ints;
        Triggers = triggers;
    }
}