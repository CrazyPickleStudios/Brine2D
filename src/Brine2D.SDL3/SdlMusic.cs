using Brine2D.Engine;
using SDL3;

namespace Brine2D.SDL3;

internal sealed class SdlMusic : IMusic
{
    public IntPtr Audio { get; }
    public float LengthSeconds { get; }

    public SdlMusic(IntPtr audio, float lengthSeconds)
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