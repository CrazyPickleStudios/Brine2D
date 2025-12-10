namespace Brine2D.Engine;

public interface IAudio
{
    float MasterVolume { get; set; } // 0..1
    void Play(ISound sound, float volume = 1.0f, bool loop = false);
    void Stop(ISound sound);
    void StopAll();

    IMusic? CurrentMusic { get; }
    void PlayMusic(IMusic music, float volume = 1.0f, bool loop = true);
    void PauseMusic();
    void ResumeMusic();
    void StopMusic();
}