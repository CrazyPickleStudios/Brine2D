using Brine2D.Audio;
using SDL3;

namespace Brine2D.SDL3;

internal sealed class SdlSound : ISound
{
    public IntPtr Audio { get; }
    public float LengthSeconds { get; }

    public SdlSound(IntPtr audio, float lengthSeconds)
    {
        Audio = audio;
        LengthSeconds = lengthSeconds;
    }

    public void Dispose()
    {
        if (Audio != IntPtr.Zero)
        {
            Mixer.DestroyAudio(Audio);
        }
    }
}