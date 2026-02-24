using Brine2D.Audio;
using Brine2D.ECS;
using Brine2D.Systems.Audio;
using NSubstitute;

namespace Brine2D.Tests.Systems.Audio;

public class AudioSourceComponentTests : TestBase
{
    private AudioSourceComponent CreateAudioSource(Action<AudioSourceComponent>? configure = null)
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<AudioSourceComponent>(configure);
        return entity.GetComponent<AudioSourceComponent>()!;
    }

    #region Default Values

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        var audioSource = CreateAudioSource();

        Assert.Null(audioSource.SoundEffect);
        Assert.Null(audioSource.Music);
        Assert.Equal(1.0f, audioSource.Volume);
        Assert.False(audioSource.PlayOnEnable);
        Assert.False(audioSource.Loop);
        Assert.Equal(0, audioSource.LoopCount);
        Assert.False(audioSource.IsPlaying);
        Assert.False(audioSource.TriggerPlay);
        Assert.False(audioSource.TriggerStop);
        Assert.False(audioSource.EnableSpatialAudio);
        Assert.Equal(50f, audioSource.MinDistance);
        Assert.Equal(500f, audioSource.MaxDistance);
        Assert.Equal(1.0f, audioSource.RolloffFactor);
        Assert.Equal(1.0f, audioSource.SpatialBlend);
        Assert.Equal(1.0f, audioSource.SpatialVolume);
        Assert.Equal(0f, audioSource.SpatialPan);
    }

    #endregion

    #region Sound and Music

    [Fact]
    public void SoundEffect_SetAndGet_WorksCorrectly()
    {
        var audioSource = CreateAudioSource();
        var mockSound = Substitute.For<ISoundEffect>();

        audioSource.SoundEffect = mockSound;

        Assert.Equal(mockSound, audioSource.SoundEffect);
    }

    [Fact]
    public void Music_SetAndGet_WorksCorrectly()
    {
        var audioSource = CreateAudioSource();
        var mockMusic = Substitute.For<IMusic>();

        audioSource.Music = mockMusic;

        Assert.Equal(mockMusic, audioSource.Music);
    }

    #endregion

    #region Volume

    [Fact]
    public void Volume_DefaultIsOne()
    {
        Assert.Equal(1.0f, CreateAudioSource().Volume);
    }

    [Fact]
    public void Volume_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.Volume = 0.5f;
        Assert.Equal(0.5f, audioSource.Volume);
    }

    [Fact]
    public void Volume_CanBeZero()
    {
        var audioSource = CreateAudioSource();
        audioSource.Volume = 0f;
        Assert.Equal(0f, audioSource.Volume);
    }

    #endregion

    #region Playback Control

    [Fact]
    public void PlayOnEnable_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.PlayOnEnable = true;
        Assert.True(audioSource.PlayOnEnable);
    }

    [Fact]
    public void Loop_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.Loop = true;
        Assert.True(audioSource.Loop);
    }

    [Fact]
    public void LoopCount_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.LoopCount = 3;
        Assert.Equal(3, audioSource.LoopCount);
    }

    [Fact]
    public void LoopCount_NegativeOneIsInfinite()
    {
        var audioSource = CreateAudioSource();
        audioSource.LoopCount = -1;
        Assert.Equal(-1, audioSource.LoopCount);
    }

    [Fact]
    public void TriggerPlay_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.TriggerPlay = true;
        Assert.True(audioSource.TriggerPlay);
    }

    [Fact]
    public void TriggerStop_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.TriggerStop = true;
        Assert.True(audioSource.TriggerStop);
    }

    [Fact]
    public void IsPlaying_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.IsPlaying = true;
        Assert.True(audioSource.IsPlaying);
    }

    #endregion

    #region Spatial Audio

    [Fact]
    public void EnableSpatialAudio_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.EnableSpatialAudio = true;
        Assert.True(audioSource.EnableSpatialAudio);
    }

    [Fact]
    public void MinDistance_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.MinDistance = 100f;
        Assert.Equal(100f, audioSource.MinDistance);
    }

    [Fact]
    public void MaxDistance_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.MaxDistance = 1000f;
        Assert.Equal(1000f, audioSource.MaxDistance);
    }

    [Fact]
    public void RolloffFactor_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.RolloffFactor = 2.0f;
        Assert.Equal(2.0f, audioSource.RolloffFactor);
    }

    [Fact]
    public void RolloffFactor_CanBeZero()
    {
        var audioSource = CreateAudioSource();
        audioSource.RolloffFactor = 0f;
        Assert.Equal(0f, audioSource.RolloffFactor);
    }

    [Fact]
    public void SpatialBlend_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.SpatialBlend = 0.5f;
        Assert.Equal(0.5f, audioSource.SpatialBlend);
    }

    [Fact]
    public void SpatialVolume_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.SpatialVolume = 0.75f;
        Assert.Equal(0.75f, audioSource.SpatialVolume);
    }

    [Fact]
    public void SpatialPan_CanBeSet()
    {
        var audioSource = CreateAudioSource();
        audioSource.SpatialPan = 0.3f;
        Assert.Equal(0.3f, audioSource.SpatialPan);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AudioSourceComponent_SoundEffectConfiguration()
    {
        var mockSound = Substitute.For<ISoundEffect>();

        var audioSource = CreateAudioSource(a =>
        {
            a.SoundEffect = mockSound;
            a.Volume = 0.8f;
            a.TriggerPlay = true;
        });

        Assert.Equal(mockSound, audioSource.SoundEffect);
        Assert.Equal(0.8f, audioSource.Volume);
        Assert.True(audioSource.TriggerPlay);
    }

    [Fact]
    public void AudioSourceComponent_BackgroundMusicConfiguration()
    {
        var mockMusic = Substitute.For<IMusic>();

        var audioSource = CreateAudioSource(a =>
        {
            a.Music = mockMusic;
            a.Volume = 0.6f;
            a.Loop = true;
            a.PlayOnEnable = true;
        });

        Assert.Equal(mockMusic, audioSource.Music);
        Assert.Equal(0.6f, audioSource.Volume);
        Assert.True(audioSource.Loop);
        Assert.True(audioSource.PlayOnEnable);
    }

    [Fact]
    public void AudioSourceComponent_SpatialAudioConfiguration()
    {
        var mockSound = Substitute.For<ISoundEffect>();

        var audioSource = CreateAudioSource(a =>
        {
            a.SoundEffect = mockSound;
            a.EnableSpatialAudio = true;
            a.MinDistance = 100f;
            a.MaxDistance = 800f;
            a.RolloffFactor = 1.5f;
            a.SpatialBlend = 0.9f;
        });

        Assert.True(audioSource.EnableSpatialAudio);
        Assert.Equal(100f, audioSource.MinDistance);
        Assert.Equal(800f, audioSource.MaxDistance);
        Assert.Equal(1.5f, audioSource.RolloffFactor);
        Assert.Equal(0.9f, audioSource.SpatialBlend);
    }

    [Fact]
    public void AudioSourceComponent_FullConfiguration()
    {
        var mockSound = Substitute.For<ISoundEffect>();

        var audioSource = CreateAudioSource(a =>
        {
            a.SoundEffect = mockSound;
            a.Volume = 0.7f;
            a.PlayOnEnable = true;
            a.Loop = true;
            a.LoopCount = 3;
            a.EnableSpatialAudio = true;
            a.MinDistance = 75f;
            a.MaxDistance = 600f;
            a.RolloffFactor = 1.2f;
            a.SpatialBlend = 0.8f;
        });

        Assert.Equal(mockSound, audioSource.SoundEffect);
        Assert.Equal(0.7f, audioSource.Volume);
        Assert.True(audioSource.PlayOnEnable);
        Assert.True(audioSource.Loop);
        Assert.Equal(3, audioSource.LoopCount);
        Assert.True(audioSource.EnableSpatialAudio);
        Assert.Equal(75f, audioSource.MinDistance);
        Assert.Equal(600f, audioSource.MaxDistance);
        Assert.Equal(1.2f, audioSource.RolloffFactor);
        Assert.Equal(0.8f, audioSource.SpatialBlend);
    }

    #endregion
}