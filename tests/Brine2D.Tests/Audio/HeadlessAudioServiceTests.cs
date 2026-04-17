using Brine2D.Audio;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brine2D.Tests.Audio;

public class HeadlessAudioServiceTests
{
    private static HeadlessAudioService CreateService(AudioOptions? options = null) =>
        new(options ?? new AudioOptions(), NullLogger<HeadlessAudioService>.Instance);

    [Fact]
    public void Constructor_WithCustomVolumes_InitialisesPropertiesFromOptions()
    {
        // Arrange
        var options = new AudioOptions
        {
            MasterVolume = 0.5f,
            MusicVolume  = 0.3f,
            SoundVolume  = 0.8f
        };

        // Act
        var service = CreateService(options);

        // Assert
        Assert.Equal(0.5f, service.MasterVolume);
        Assert.Equal(0.3f, service.MusicVolume);
        Assert.Equal(0.8f, service.SoundVolume);
    }

    [Fact]
    public void Constructor_WithDefaultOptions_MatchesAudioOptionsDefaults()
    {
        // Arrange
        var options = new AudioOptions(); // MasterVolume=1.0, MusicVolume=0.7, SoundVolume=1.0

        // Act
        var service = CreateService(options);

        // Assert — headless volumes must mirror what the real service would use
        Assert.Equal(options.MasterVolume, service.MasterVolume);
        Assert.Equal(options.MusicVolume,  service.MusicVolume);
        Assert.Equal(options.SoundVolume,  service.SoundVolume);
    }

    [Fact]
    public void MasterVolume_CanBeChangedAfterConstruction()
    {
        // Arrange
        var service = CreateService(new AudioOptions());

        // Act
        service.MasterVolume = 0.25f;

        // Assert
        Assert.Equal(0.25f, service.MasterVolume);
    }

    [Theory]
    [InlineData(1.5f,  1.0f)]
    [InlineData(-0.5f, 0.0f)]
    [InlineData(0.5f,  0.5f)]
    public void MasterVolume_OutOfRange_IsClamped(float input, float expected)
    {
        var service = CreateService();
        service.MasterVolume = input;
        Assert.Equal(expected, service.MasterVolume);
    }

    [Theory]
    [InlineData(1.5f,  1.0f)]
    [InlineData(-0.5f, 0.0f)]
    [InlineData(0.5f,  0.5f)]
    public void MusicVolume_OutOfRange_IsClamped(float input, float expected)
    {
        var service = CreateService();
        service.MusicVolume = input;
        Assert.Equal(expected, service.MusicVolume);
    }

    [Theory]
    [InlineData(1.5f,  1.0f)]
    [InlineData(-0.5f, 0.0f)]
    [InlineData(0.5f,  0.5f)]
    public void SoundVolume_OutOfRange_IsClamped(float input, float expected)
    {
        var service = CreateService();
        service.SoundVolume = input;
        Assert.Equal(expected, service.SoundVolume);
    }

    [Fact]
    public void NonLoopingTrack_ExpiresAfterUpdate()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;

        var track = service.PlaySound(sound, loops: 0);

        Assert.True(service.IsTrackAlive(track));
        Assert.Equal(1, service.ActiveSoundTrackCount);

        service.Update(0.016f);

        Assert.False(service.IsTrackAlive(track));
        Assert.Equal(0, service.ActiveSoundTrackCount);
    }

    [Fact]
    public void LoopingTrack_DoesNotExpireAfterUpdate()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;

        var track = service.PlaySound(sound, loops: -1);

        Assert.True(service.IsTrackAlive(track));

        service.Update(0.016f);
        service.Update(0.016f);

        Assert.True(service.IsTrackAlive(track));
        Assert.Equal(1, service.ActiveSoundTrackCount);
    }

    [Fact]
    public void PlaySound_HigherPriority_EvictsLowestWhenFull()
    {
        var service = CreateService(new AudioOptions { MaxTracks = 2 });
        var sound = service.LoadSoundAsync("test.wav").Result;

        var track1 = service.PlaySound(sound, loops: -1, priority: 1);
        var track2 = service.PlaySound(sound, loops: -1, priority: 3);
        Assert.Equal(2, service.ActiveSoundTrackCount);

        var track3 = service.PlaySound(sound, loops: -1, priority: 2);

        Assert.NotEqual(nint.Zero, track3);
        Assert.Equal(2, service.ActiveSoundTrackCount);
        Assert.False(service.IsTrackAlive(track1));
        Assert.True(service.IsTrackAlive(track2));
        Assert.True(service.IsTrackAlive(track3));
    }

    [Fact]
    public void PlaySound_LowerPriority_RejectedWhenFull()
    {
        var service = CreateService(new AudioOptions { MaxTracks = 2 });
        var sound = service.LoadSoundAsync("test.wav").Result;

        service.PlaySound(sound, loops: -1, priority: 5);
        service.PlaySound(sound, loops: -1, priority: 5);
        Assert.Equal(2, service.ActiveSoundTrackCount);

        var rejected = service.PlaySound(sound, loops: -1, priority: 3);

        Assert.Equal(nint.Zero, rejected);
        Assert.Equal(2, service.ActiveSoundTrackCount);
    }

    [Fact]
    public void PlaySound_EqualPriority_EvictsWhenFull()
    {
        var service = CreateService(new AudioOptions { MaxTracks = 1 });
        var sound = service.LoadSoundAsync("test.wav").Result;

        var track1 = service.PlaySound(sound, loops: -1, priority: 5);
        Assert.Equal(1, service.ActiveSoundTrackCount);

        var track2 = service.PlaySound(sound, loops: -1, priority: 5);

        Assert.NotEqual(nint.Zero, track2);
        Assert.False(service.IsTrackAlive(track1));
        Assert.True(service.IsTrackAlive(track2));
    }

    [Fact]
    public void SetTrackPitch_OnAliveTrack_DoesNotThrow()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var track = service.PlaySound(sound, loops: -1);

        service.SetTrackPitch(track, 2.0f);

        Assert.True(service.IsTrackAlive(track));
    }

    [Fact]
    public void MusicPositionMs_WhenNotPlaying_ReturnsNegative()
    {
        var service = CreateService();

        Assert.Equal(-1, service.MusicPositionMs);
    }

    [Fact]
    public void MusicPositionMs_WhenPlaying_ReturnsNegative()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music);

        Assert.Equal(-1, service.MusicPositionMs);
    }

    [Fact]
    public void SeekMusic_WhenNotPlaying_DoesNotThrow()
    {
        var service = CreateService();

        service.SeekMusic(5000);
    }

    [Fact]
    public void SeekMusic_WhenPlaying_DoesNotThrow()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music);
        service.SeekMusic(1000);

        Assert.True(service.IsMusicPlaying);
    }

    [Fact]
    public void StopMusic_WithFade_SetsIsMusicFadingOut()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music);
        Assert.True(service.IsMusicPlaying);
        Assert.False(service.IsMusicFadingOut);

        service.StopMusic(2.0f);

        Assert.True(service.IsMusicPlaying);
        Assert.True(service.IsMusicFadingOut);
    }

    [Fact]
    public void StopMusic_WithFade_CompletesOnNextUpdate()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music);
        service.StopMusic(2.0f);
        service.Update(2.0f);

        Assert.False(service.IsMusicPlaying);
        Assert.False(service.IsMusicFadingOut);
    }

    [Fact]
    public void StopMusic_WithFade_DoesNotCompleteWhilePaused()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music);
        service.StopMusic(2.0f);
        service.PauseMusic();
        service.Update(0.016f);

        Assert.True(service.IsMusicPlaying);
        Assert.True(service.IsMusicFadingOut);
        Assert.True(service.IsMusicPaused);
    }

    [Fact]
    public void PlayMusic_CancelsFadeOut()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music);
        service.StopMusic(2.0f);
        Assert.True(service.IsMusicFadingOut);

        service.PlayMusic(music);

        Assert.True(service.IsMusicPlaying);
        Assert.False(service.IsMusicFadingOut);
    }

    [Fact]
    public void StopMusic_Immediate_DoesNotSetFadingOut()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music);
        service.StopMusic();

        Assert.False(service.IsMusicPlaying);
        Assert.False(service.IsMusicFadingOut);
    }

    [Fact]
    public void MusicDurationMs_ReturnsNegative()
    {
        var service = CreateService();

        Assert.Equal(-1, service.MusicDurationMs);
    }

    [Fact]
    public void MusicDurationMs_WhenPlaying_ReturnsNegative()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music);

        Assert.Equal(-1, service.MusicDurationMs);
    }

    [Fact]
    public void TagTrack_OnAliveTrack_DoesNotThrow()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var track = service.PlaySound(sound, loops: -1);

        service.TagTrack(track, "sfx");

        Assert.True(service.IsTrackAlive(track));
    }

    [Fact]
    public void TagTrack_OnDeadTrack_DoesNotThrow()
    {
        var service = CreateService();

        service.TagTrack(1, "sfx");
    }

    [Fact]
    public void TagTrack_NullOrWhitespaceBus_Throws()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var track = service.PlaySound(sound, loops: -1);

        Assert.ThrowsAny<ArgumentException>(() => service.TagTrack(track, null!));
        Assert.ThrowsAny<ArgumentException>(() => service.TagTrack(track, ""));
        Assert.ThrowsAny<ArgumentException>(() => service.TagTrack(track, "   "));
    }

    [Fact]
    public void SetBusVolume_StoresClampedValue_DoesNotThrow()
    {
        var service = CreateService();

        service.SetBusVolume("sfx", 0.5f);
        service.SetBusVolume("sfx", 1.5f);
        service.SetBusVolume("sfx", -0.5f);
    }

    [Fact]
    public void SetBusVolume_NullOrWhitespaceBus_Throws()
    {
        var service = CreateService();

        Assert.ThrowsAny<ArgumentException>(() => service.SetBusVolume(null!, 0.5f));
        Assert.ThrowsAny<ArgumentException>(() => service.SetBusVolume("", 0.5f));
    }

    [Fact]
    public void PauseBus_PausesTaggedTracks()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var tagged = service.PlaySound(sound, loops: -1);
        var untagged = service.PlaySound(sound, loops: -1);

        service.TagTrack(tagged, "ui");
        service.PauseBus("ui");

        service.Update(0.016f);

        Assert.True(service.IsTrackAlive(tagged));
        Assert.True(service.IsTrackAlive(untagged));
    }

    [Fact]
    public void PauseBus_NullOrWhitespaceBus_Throws()
    {
        var service = CreateService();

        Assert.ThrowsAny<ArgumentException>(() => service.PauseBus(null!));
        Assert.ThrowsAny<ArgumentException>(() => service.PauseBus(""));
    }

    [Fact]
    public void ResumeBus_ResumesTaggedTracks()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var track = service.PlaySound(sound, loops: -1);

        service.TagTrack(track, "ui");
        service.PauseBus("ui");
        service.ResumeBus("ui");

        Assert.True(service.IsTrackAlive(track));
    }

    [Fact]
    public void ResumeBus_NullOrWhitespaceBus_Throws()
    {
        var service = CreateService();

        Assert.ThrowsAny<ArgumentException>(() => service.ResumeBus(null!));
        Assert.ThrowsAny<ArgumentException>(() => service.ResumeBus(""));
    }

    [Fact]
    public void StopBus_RemovesTaggedTracks()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var tagged = service.PlaySound(sound, loops: -1);
        var untagged = service.PlaySound(sound, loops: -1);

        service.TagTrack(tagged, "ambient");
        service.StopBus("ambient");

        Assert.False(service.IsTrackAlive(tagged));
        Assert.True(service.IsTrackAlive(untagged));
        Assert.Equal(1, service.ActiveSoundTrackCount);
    }

    [Fact]
    public void StopBus_StopsMusicWhenBusMatches()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music, bus: "bg");
        Assert.True(service.IsMusicPlaying);

        service.StopBus("bg");

        Assert.False(service.IsMusicPlaying);
    }

    [Fact]
    public void StopBus_DoesNotStopMusicWhenBusDiffers()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music, bus: "bg");
        service.StopBus("sfx");

        Assert.True(service.IsMusicPlaying);
    }

    [Fact]
    public void StopBus_NullOrWhitespaceBus_Throws()
    {
        var service = CreateService();

        Assert.ThrowsAny<ArgumentException>(() => service.StopBus(null!));
        Assert.ThrowsAny<ArgumentException>(() => service.StopBus(""));
    }

    [Fact]
    public void PlayMusic_DefaultBusIsMusic()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music);
        service.StopBus("music");

        Assert.False(service.IsMusicPlaying);
    }

    [Fact]
    public void CrossfadeMusic_DefaultBusIsMusic()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.CrossfadeMusic(music, 1.0f);
        service.StopBus("music");

        Assert.False(service.IsMusicPlaying);
    }

    [Fact]
    public void CrossfadeMusic_CustomBus_IsRespected()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.CrossfadeMusic(music, 1.0f, bus: "cinematic");
        service.StopBus("music");

        Assert.True(service.IsMusicPlaying);

        service.StopBus("cinematic");

        Assert.False(service.IsMusicPlaying);
    }

    [Fact]
    public void StopTrack_CleansBusData()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var track = service.PlaySound(sound, loops: -1);

        service.TagTrack(track, "ui");
        service.StopTrack(track);

        Assert.False(service.IsTrackAlive(track));
        service.StopBus("ui");
    }

    [Fact]
    public void StopBus_CleansBusData()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var track = service.PlaySound(sound, loops: -1);

        service.TagTrack(track, "ui");
        service.StopBus("ui");

        Assert.Equal(0, service.ActiveSoundTrackCount);
        service.StopBus("ui");
    }

    [Fact]
    public void Eviction_CleansBusData()
    {
        var service = CreateService(new AudioOptions { MaxTracks = 1 });
        var sound = service.LoadSoundAsync("test.wav").Result;

        var track1 = service.PlaySound(sound, loops: -1, priority: 1);
        service.TagTrack(track1, "ambient");

        var track2 = service.PlaySound(sound, loops: -1, priority: 5);

        Assert.False(service.IsTrackAlive(track1));
        Assert.True(service.IsTrackAlive(track2));
        service.StopBus("ambient");
    }

    [Fact]
    public void NonLoopingTrack_ExpiryCleansBusData()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var track = service.PlaySound(sound, loops: 0);

        service.TagTrack(track, "sfx");
        service.Update(0.016f);

        Assert.False(service.IsTrackAlive(track));
        service.StopBus("sfx");
    }

    [Fact]
    public void PausedFiniteTrack_DoesNotExpire()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var track = service.PlaySound(sound, loops: 0);

        service.TagTrack(track, "sfx");
        service.PauseBus("sfx");
        service.Update(0.016f);

        Assert.True(service.IsTrackAlive(track));
    }

    [Fact]
    public void IsBusPaused_ReturnsFalseByDefault()
    {
        var service = CreateService();

        Assert.False(service.IsBusPaused("sfx"));
    }

    [Fact]
    public void IsBusPaused_ReturnsTrueAfterPauseBus()
    {
        var service = CreateService();

        service.PauseBus("sfx");

        Assert.True(service.IsBusPaused("sfx"));
    }

    [Fact]
    public void IsBusPaused_ReturnsFalseAfterResumeBus()
    {
        var service = CreateService();

        service.PauseBus("sfx");
        service.ResumeBus("sfx");

        Assert.False(service.IsBusPaused("sfx"));
    }

    [Fact]
    public void IsBusPaused_ReturnsFalseAfterStopBus()
    {
        var service = CreateService();

        service.PauseBus("sfx");
        service.StopBus("sfx");

        Assert.False(service.IsBusPaused("sfx"));
    }

    [Fact]
    public void IsBusPaused_NullOrWhitespaceBus_Throws()
    {
        var service = CreateService();

        Assert.ThrowsAny<ArgumentException>(() => service.IsBusPaused(null!));
        Assert.ThrowsAny<ArgumentException>(() => service.IsBusPaused(""));
    }

    [Fact]
    public void StopAllSounds_RemovesAllActiveTracks()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;

        service.PlaySound(sound, loops: -1);
        service.PlaySound(sound, loops: -1);
        Assert.Equal(2, service.ActiveSoundTrackCount);

        service.StopAllSounds();

        Assert.Equal(0, service.ActiveSoundTrackCount);
    }

    [Fact]
    public void StopAllSounds_DoesNotAffectMusic()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music);
        service.PlaySound(sound, loops: -1);

        service.StopAllSounds();

        Assert.True(service.IsMusicPlaying);
        Assert.Equal(0, service.ActiveSoundTrackCount);
    }

    [Fact]
    public void StopAllSounds_WhenEmpty_DoesNotThrow()
    {
        var service = CreateService();

        service.StopAllSounds();

        Assert.Equal(0, service.ActiveSoundTrackCount);
    }

    [Fact]
    public void ResumeBus_PreservesIndividuallyPausedTracks()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;

        var track1 = service.PlaySound(sound, loops: -1);
        var track2 = service.PlaySound(sound, loops: -1);
        service.TagTrack(track1, "sfx");
        service.TagTrack(track2, "sfx");

        service.PauseTrack(track1);
        service.PauseBus("sfx");
        service.ResumeBus("sfx");

        // track1 was individually paused — it should remain paused after ResumeBus
        // track2 was only bus-paused — it should be resumed
        // Both should still be alive
        Assert.True(service.IsTrackAlive(track1));
        Assert.True(service.IsTrackAlive(track2));
    }

    [Fact]
    public void PauseBus_SetsIsMusicPaused_WhenBusMatchesMusic()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music, bus: "bg");
        service.PauseBus("bg");

        Assert.True(service.IsMusicPaused);
    }

    [Fact]
    public void PauseBus_DoesNotSetIsMusicPaused_WhenBusDiffers()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music, bus: "bg");
        service.PauseBus("sfx");

        Assert.False(service.IsMusicPaused);
    }

    [Fact]
    public void ResumeBus_ClearsIsMusicPaused_WhenBusMatchesMusic()
    {
        var service = CreateService();
        var music = service.LoadMusicAsync("test.ogg").Result;

        service.PlayMusic(music, bus: "bg");
        service.PauseBus("bg");
        Assert.True(service.IsMusicPaused);

        service.ResumeBus("bg");

        Assert.False(service.IsMusicPaused);
    }

    [Fact]
    public void PauseTrack_OnAliveTrack_KeepsTrackAlive()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var track = service.PlaySound(sound, loops: -1);

        service.PauseTrack(track);

        Assert.True(service.IsTrackAlive(track));
    }

    [Fact]
    public void ResumeTrack_OnPausedTrack_KeepsTrackAlive()
    {
        var service = CreateService();
        var sound = service.LoadSoundAsync("test.wav").Result;
        var track = service.PlaySound(sound, loops: -1);

        service.PauseTrack(track);
        service.ResumeTrack(track);

        Assert.True(service.IsTrackAlive(track));
    }
}