namespace Brine2D.UI;

/// <summary>
/// Standard easing functions for use with <see cref="UITween"/>.
/// All functions accept a normalised time <paramref name="t"/> in [0, 1] and return a
/// normalised progress value (may overshoot for Back/Elastic curves).
/// </summary>
public static class UIEasing
{
    // ── Linear ──────────────────────────────────────────────────────────────

    public static float Linear(float t) => t;

    // ── Quadratic ────────────────────────────────────────────────────────────

    public static float QuadIn(float t) => t * t;
    public static float QuadOut(float t) => t * (2f - t);
    public static float QuadInOut(float t) =>
        t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

    // ── Cubic ────────────────────────────────────────────────────────────────

    public static float CubicIn(float t) => t * t * t;
    public static float CubicOut(float t) { float f = t - 1f; return f * f * f + 1f; }
    public static float CubicInOut(float t) =>
        t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;

    // ── Quartic ──────────────────────────────────────────────────────────────

    public static float QuartIn(float t) => t * t * t * t;
    public static float QuartOut(float t) { float f = t - 1f; return 1f - f * f * f * f; }
    public static float QuartInOut(float t) =>
        t < 0.5f ? 8f * t * t * t * t : 1f - 8f * (t - 1f) * (t - 1f) * (t - 1f) * (t - 1f);

    // ── Sine ─────────────────────────────────────────────────────────────────

    public static float SineIn(float t) => 1f - MathF.Cos(t * MathF.PI / 2f);
    public static float SineOut(float t) => MathF.Sin(t * MathF.PI / 2f);
    public static float SineInOut(float t) => -(MathF.Cos(MathF.PI * t) - 1f) / 2f;

    // ── Exponential ──────────────────────────────────────────────────────────

    public static float ExpoIn(float t) => t == 0f ? 0f : MathF.Pow(2f, 10f * t - 10f);
    public static float ExpoOut(float t) => t == 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);
    public static float ExpoInOut(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        return t < 0.5f
            ? MathF.Pow(2f, 20f * t - 10f) / 2f
            : (2f - MathF.Pow(2f, -20f * t + 10f)) / 2f;
    }

    // ── Back (slight overshoot) ───────────────────────────────────────────────

    private const float BackC1 = 1.70158f;
    private const float BackC2 = BackC1 * 1.525f;
    private const float BackC3 = BackC1 + 1f;

    public static float BackIn(float t) => BackC3 * t * t * t - BackC1 * t * t;
    public static float BackOut(float t) { float f = t - 1f; return 1f + BackC3 * f * f * f + BackC1 * f * f; }
    public static float BackInOut(float t) =>
        t < 0.5f
            ? (MathF.Pow(2f * t, 2f) * ((BackC2 + 1f) * 2f * t - BackC2)) / 2f
            : (MathF.Pow(2f * t - 2f, 2f) * ((BackC2 + 1f) * (2f * t - 2f) + BackC2) + 2f) / 2f;

    // ── Bounce ───────────────────────────────────────────────────────────────

    public static float BounceOut(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        if (t < 1f / d1) return n1 * t * t;
        if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
        if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
        t -= 2.625f / d1;
        return n1 * t * t + 0.984375f;
    }

    public static float BounceIn(float t) => 1f - BounceOut(1f - t);

    public static float BounceInOut(float t) =>
        t < 0.5f
            ? (1f - BounceOut(1f - 2f * t)) / 2f
            : (1f + BounceOut(2f * t - 1f)) / 2f;

    // ── Elastic ──────────────────────────────────────────────────────────────

    private const float ElasticC4 = 2f * MathF.PI / 3f;
    private const float ElasticC5 = 2f * MathF.PI / 4.5f;

    public static float ElasticIn(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        return -MathF.Pow(2f, 10f * t - 10f) * MathF.Sin((t * 10f - 10.75f) * ElasticC4);
    }

    public static float ElasticOut(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        return MathF.Pow(2f, -10f * t) * MathF.Sin((t * 10f - 0.75f) * ElasticC4) + 1f;
    }

    public static float ElasticInOut(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        return t < 0.5f
            ? -(MathF.Pow(2f, 20f * t - 10f) * MathF.Sin((20f * t - 11.125f) * ElasticC5)) / 2f
            : (MathF.Pow(2f, -20f * t + 10f) * MathF.Sin((20f * t - 11.125f) * ElasticC5)) / 2f + 1f;
    }
}
