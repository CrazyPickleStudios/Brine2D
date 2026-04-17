using Brine2D.Audio;
using Brine2D.ECS;
using Brine2D.Systems.Audio;
using NSubstitute;

namespace Brine2D.Tests.Systems.Audio;

public class AudioSourceComponentTests : TestBase
{
    private SoundEffectSourceComponent CreateSoundEffectSource(Action<SoundEffectSourceComponent>? configure = null)
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<SoundEffectSourceComponent>(configure);
        return entity.GetComponent<SoundEffectSourceComponent>()!;
    }

    private MusicSourceComponent CreateMusicSource(Action<MusicSourceComponent>? configure = null)
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<MusicSourceComponent>(configure);
        return entity.GetComponent<MusicSourceComponent>()!;
    }

    #region Default Values

    [Fact]
    public void SoundEffectSourceComponent_DefaultValues_AreCorrect()
    {
        var source = CreateSoundEffectSource();

        Assert.Null(source.SoundEffect);
        Assert.Equal(1.0f, source.Volume);
        Assert.False(source.PlayOnEnable);
        Assert.Equal(0, source.LoopCount);
        Assert.False(source.IsPlaying);
        Assert.False(source.TriggerPlay);
        Assert.False(source.TriggerStop);
        Assert.False(source.EnableSpatialAudio);
        Assert.Equal(50f, source.MinDistance);
        Assert.Equal(500f, source.MaxDistance);
        Assert.Equal(1.0f, source.RolloffFactor);
        Assert.Equal(1.0f, source.SpatialBlend);
        Assert.Equal(1.0f, source.SpatialVolume);
        Assert.Equal(0f, source.SpatialPan);
        Assert.Equal("sfx", source.Bus);
        Assert.Equal(0f, source.PitchVariation);
        Assert.Equal(0f, source.VolumeVariation);
    }

    [Fact]
    public void MusicSourceComponent_DefaultValues_AreCorrect()
    {
        var source = CreateMusicSource();

        Assert.Null(source.Music);
        Assert.Equal(0f, source.CrossfadeDuration);
        Assert.Equal(0f, source.FadeOutDuration);
        Assert.Equal(1.0f, source.Volume);
        Assert.False(source.PlayOnEnable);
        Assert.Equal(0, source.LoopCount);
        Assert.False(source.IsPlaying);
        Assert.False(source.IsPaused);
        Assert.False(source.TriggerPlay);
        Assert.False(source.TriggerStop);
        Assert.False(source.TriggerPause);
        Assert.False(source.TriggerResume);
        Assert.Equal("music", source.Bus);
    }

    #endregion

    #region Sound and Music

    [Fact]
    public void SoundEffect_SetAndGet_WorksCorrectly()
    {
        var source = CreateSoundEffectSource();
        var mockSound = Substitute.For<ISoundEffect>();

        source.SoundEffect = mockSound;

        Assert.Equal(mockSound, source.SoundEffect);
    }

    [Fact]
    public void Music_SetAndGet_WorksCorrectly()
    {
        var source = CreateMusicSource();
        var mockMusic = Substitute.For<IMusic>();

        source.Music = mockMusic;

        Assert.Equal(mockMusic, source.Music);
    }

    #endregion

    #region Volume

    [Fact]
    public void Volume_DefaultIsOne()
    {
        Assert.Equal(1.0f, CreateSoundEffectSource().Volume);
    }

    [Fact]
    public void Volume_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.Volume = 0.5f;
        Assert.Equal(0.5f, source.Volume);
    }

    [Fact]
    public void Volume_CanBeZero()
    {
        var source = CreateSoundEffectSource();
        source.Volume = 0f;
        Assert.Equal(0f, source.Volume);
    }

    #endregion

    #region Playback Control

    [Fact]
    public void PlayOnEnable_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.PlayOnEnable = true;
        Assert.True(source.PlayOnEnable);
    }

    [Fact]
    public void LoopCount_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.LoopCount = 3;
        Assert.Equal(3, source.LoopCount);
    }

    [Fact]
    public void LoopCount_NegativeOneIsInfinite()
    {
        var source = CreateSoundEffectSource();
        source.LoopCount = -1;
        Assert.Equal(-1, source.LoopCount);
    }

    [Fact]
    public void TriggerPlay_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.TriggerPlay = true;
        Assert.True(source.TriggerPlay);
    }

    [Fact]
    public void TriggerStop_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.TriggerStop = true;
        Assert.True(source.TriggerStop);
    }

    [Fact]
    public void IsPlaying_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.IsPlaying = true;
        Assert.True(source.IsPlaying);
    }

    [Fact]
    public void CrossfadeDuration_CanBeSet()
    {
        var source = CreateMusicSource();
        source.CrossfadeDuration = 2.5f;
        Assert.Equal(2.5f, source.CrossfadeDuration);
    }

    [Fact]
    public void FadeOutDuration_CanBeSet()
    {
        var source = CreateMusicSource();
        source.FadeOutDuration = 1.5f;
        Assert.Equal(1.5f, source.FadeOutDuration);
    }

    [Fact]
    public void FadeOutDuration_NegativeClamped()
    {
        var source = CreateMusicSource();
        source.FadeOutDuration = -1f;
        Assert.Equal(0f, source.FadeOutDuration);
    }

    [Fact]
    public void TriggerPause_CanBeSet()
    {
        var source = CreateMusicSource();
        source.TriggerPause = true;
        Assert.True(source.TriggerPause);
    }

    [Fact]
    public void TriggerResume_CanBeSet()
    {
        var source = CreateMusicSource();
        source.TriggerResume = true;
        Assert.True(source.TriggerResume);
    }

    [Fact]
    public void IsPaused_CanBeSet()
    {
        var source = CreateMusicSource();
        source.IsPaused = true;
        Assert.True(source.IsPaused);
    }

    #endregion

    #region Bus

    [Fact]
    public void SoundEffectSourceComponent_Bus_DefaultIsSfx()
    {
        var source = CreateSoundEffectSource();
        Assert.Equal("sfx", source.Bus);
    }

    [Fact]
    public void MusicSourceComponent_Bus_DefaultIsMusic()
    {
        var source = CreateMusicSource();
        Assert.Equal("music", source.Bus);
    }

    [Fact]
    public void Bus_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.Bus = "ui";
        Assert.Equal("ui", source.Bus);
    }

    #endregion

    #region Sound Variation

    [Fact]
    public void PitchVariation_DefaultIsZero()
    {
        var source = CreateSoundEffectSource();
        Assert.Equal(0f, source.PitchVariation);
    }

    [Fact]
    public void PitchVariation_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.PitchVariation = 0.3f;
        Assert.Equal(0.3f, source.PitchVariation);
    }

    [Theory]
    [InlineData(-0.5f, 0f)]
    [InlineData(1.5f, 1f)]
    [InlineData(0.5f, 0.5f)]
    public void PitchVariation_OutOfRange_IsClamped(float input, float expected)
    {
        var source = CreateSoundEffectSource();
        source.PitchVariation = input;
        Assert.Equal(expected, source.PitchVariation);
    }

    [Fact]
    public void VolumeVariation_DefaultIsZero()
    {
        var source = CreateSoundEffectSource();
        Assert.Equal(0f, source.VolumeVariation);
    }

    [Fact]
    public void VolumeVariation_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.VolumeVariation = 0.2f;
        Assert.Equal(0.2f, source.VolumeVariation);
    }

    [Theory]
    [InlineData(-0.5f, 0f)]
    [InlineData(1.5f, 1f)]
    [InlineData(0.5f, 0.5f)]
    public void VolumeVariation_OutOfRange_IsClamped(float input, float expected)
    {
        var source = CreateSoundEffectSource();
        source.VolumeVariation = input;
        Assert.Equal(expected, source.VolumeVariation);
    }

    #endregion

    #region Spatial Audio

    [Fact]
    public void EnableSpatialAudio_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.EnableSpatialAudio = true;
        Assert.True(source.EnableSpatialAudio);
    }

    [Fact]
    public void MinDistance_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.MinDistance = 100f;
        Assert.Equal(100f, source.MinDistance);
    }

    [Fact]
    public void MaxDistance_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.MaxDistance = 1000f;
        Assert.Equal(1000f, source.MaxDistance);
    }

    [Fact]
    public void RolloffFactor_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.RolloffFactor = 2.0f;
        Assert.Equal(2.0f, source.RolloffFactor);
    }

    [Fact]
    public void RolloffFactor_CanBeZero()
    {
        var source = CreateSoundEffectSource();
        source.RolloffFactor = 0f;
        Assert.Equal(0f, source.RolloffFactor);
    }

    [Fact]
    public void SpatialBlend_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.SpatialBlend = 0.5f;
        Assert.Equal(0.5f, source.SpatialBlend);
    }

    [Fact]
    public void SpatialVolume_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.SpatialVolume = 0.75f;
        Assert.Equal(0.75f, source.SpatialVolume);
    }

    [Fact]
    public void SpatialPan_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.SpatialPan = 0.3f;
        Assert.Equal(0.3f, source.SpatialPan);
    }

    [Fact]
    public void DopplerFactor_DefaultIsZero()
    {
        var source = CreateSoundEffectSource();
        Assert.Equal(0f, source.DopplerFactor);
    }

    [Fact]
    public void DopplerFactor_CanBeSet()
    {
        var source = CreateSoundEffectSource();
        source.DopplerFactor = 1.5f;
        Assert.Equal(1.5f, source.DopplerFactor);
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(6f, 5f)]
    [InlineData(2.5f, 2.5f)]
    public void DopplerFactor_OutOfRange_IsClamped(float input, float expected)
    {
        var source = CreateSoundEffectSource();
        source.DopplerFactor = input;
        Assert.Equal(expected, source.DopplerFactor);
    }

    [Fact]
    public void SpatialPitch_DefaultIsOne()
    {
        var source = CreateSoundEffectSource();
        Assert.Equal(1.0f, source.SpatialPitch);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SoundEffectSourceComponent_FullConfiguration()
    {
        var mockSound = Substitute.For<ISoundEffect>();

        var source = CreateSoundEffectSource(a =>
        {
            a.SoundEffect = mockSound;
            a.Volume = 0.8f;
            a.TriggerPlay = true;
        });

        Assert.Equal(mockSound, source.SoundEffect);
        Assert.Equal(0.8f, source.Volume);
        Assert.True(source.TriggerPlay);
    }

    [Fact]
    public void MusicSourceComponent_FullConfiguration()
    {
        var mockMusic = Substitute.For<IMusic>();

        var source = CreateMusicSource(a =>
        {
            a.Music = mockMusic;
            a.Volume = 0.6f;
            a.LoopCount = -1;
            a.PlayOnEnable = true;
            a.CrossfadeDuration = 2.0f;
            a.FadeOutDuration = 1.5f;
        });

        Assert.Equal(mockMusic, source.Music);
        Assert.Equal(0.6f, source.Volume);
        Assert.Equal(-1, source.LoopCount);
        Assert.True(source.PlayOnEnable);
        Assert.Equal(2.0f, source.CrossfadeDuration);
        Assert.Equal(1.5f, source.FadeOutDuration);
    }

    [Fact]
    public void SoundEffectSourceComponent_SpatialAudioConfiguration()
    {
        var mockSound = Substitute.For<ISoundEffect>();

        var source = CreateSoundEffectSource(a =>
        {
            a.SoundEffect = mockSound;
            a.EnableSpatialAudio = true;
            a.MinDistance = 100f;
            a.MaxDistance = 800f;
            a.RolloffFactor = 1.5f;
            a.SpatialBlend = 0.9f;
        });

        Assert.True(source.EnableSpatialAudio);
        Assert.Equal(100f, source.MinDistance);
        Assert.Equal(800f, source.MaxDistance);
        Assert.Equal(1.5f, source.RolloffFactor);
        Assert.Equal(0.9f, source.SpatialBlend);
    }

    [Fact]
    public void SoundEffectSourceComponent_CompleteConfiguration()
    {
        var mockSound = Substitute.For<ISoundEffect>();

        var source = CreateSoundEffectSource(a =>
        {
            a.SoundEffect = mockSound;
            a.Volume = 0.7f;
            a.PlayOnEnable = true;
            a.LoopCount = 3;
            a.EnableSpatialAudio = true;
            a.MinDistance = 75f;
            a.MaxDistance = 600f;
            a.RolloffFactor = 1.2f;
            a.SpatialBlend = 0.8f;
        });

        Assert.Equal(mockSound, source.SoundEffect);
        Assert.Equal(0.7f, source.Volume);
        Assert.True(source.PlayOnEnable);
        Assert.Equal(3, source.LoopCount);
        Assert.True(source.EnableSpatialAudio);
        Assert.Equal(75f, source.MinDistance);
        Assert.Equal(600f, source.MaxDistance);
        Assert.Equal(1.2f, source.RolloffFactor);
        Assert.Equal(0.8f, source.SpatialBlend);
    }

    #endregion
}