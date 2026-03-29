namespace Brine2D.Audio;

/// <summary>
/// Composite audio service combining loading and playback.
/// <para>
/// Prefer depending on the narrower <see cref="ISoundLoader"/>, <see cref="IMusicLoader"/>, 
/// or <see cref="IAudioPlayer"/> interfaces when only a subset of functionality is needed.
/// </para>
/// </summary>
public interface IAudioService : ISoundLoader, IMusicLoader, IAudioPlayer;