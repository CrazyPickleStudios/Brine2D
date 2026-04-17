namespace Brine2D.Audio;

/// <summary>
/// Explicit state for the music fade state machine, replacing the implicit
/// mode inference from track handle combinations. Used by both
/// <see cref="AudioService"/> and <see cref="HeadlessAudioService"/>.
/// </summary>
internal struct MusicFade
{
    public MusicFadeMode Mode { get; private set; }
    public float Duration { get; private set; }
    public float Elapsed { get; private set; }
    public float OutgoingEntityVolume { get; private set; }

    public readonly bool IsActive => Mode != MusicFadeMode.Idle;
    public readonly bool IsCrossfading => Mode == MusicFadeMode.Crossfading;
    public readonly bool IsFadingOut => Mode == MusicFadeMode.FadingOut;
    public readonly float Progress => !IsActive ? 0f : Duration <= 0f ? 1f : Math.Clamp(Elapsed / Duration, 0f, 1f);
    public readonly bool IsComplete => IsActive && Elapsed >= Duration;
    public readonly float FadeInGain => MathF.Sin(Progress * MathF.PI * 0.5f);
    public readonly float FadeOutGain => MathF.Cos(Progress * MathF.PI * 0.5f);

    public void StartCrossfade(float duration, float outgoingEntityVolume)
    {
        Mode = MusicFadeMode.Crossfading;
        Duration = duration;
        Elapsed = 0f;
        OutgoingEntityVolume = outgoingEntityVolume;
    }

    public void StartFadeOut(float duration, float outgoingEntityVolume)
    {
        Mode = MusicFadeMode.FadingOut;
        Duration = duration;
        Elapsed = 0f;
        OutgoingEntityVolume = outgoingEntityVolume;
    }

    /// <returns><see langword="true"/> when the fade completed this frame.</returns>
    public bool Advance(float deltaTime)
    {
        if (Mode == MusicFadeMode.Idle)
            return false;

        Elapsed += deltaTime;
        return IsComplete;
    }

    public void Reset() => this = default;
}

internal enum MusicFadeMode : byte
{
    Idle,
    Crossfading,
    FadingOut
}