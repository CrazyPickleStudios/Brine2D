using Brine2D.Audio;
using Brine2D.ECS;
using Brine2D.Systems.Audio;
using NSubstitute;

namespace Brine2D.Tests.Systems.Audio;

public class AudioSourceComponentTests : TestBase
{
    #region Default Values

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Assert
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
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;
        var mockSound = Substitute.For<ISoundEffect>();

        // Act
        audioSource.SoundEffect = mockSound;

        // Assert
        Assert.Equal(mockSound, audioSource.SoundEffect);
    }

    [Fact]
    public void Music_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;
        var mockMusic = Substitute.For<IMusic>();

        // Act
        audioSource.Music = mockMusic;

        // Assert
        Assert.Equal(mockMusic, audioSource.Music);
    }

    #endregion

    #region Volume

    [Fact]
    public void Volume_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.Volume = 0.5f;

        // Assert
        Assert.Equal(0.5f, audioSource.Volume);
    }

    [Fact]
    public void Volume_CanBeZero()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.Volume = 0f;

        // Assert
        Assert.Equal(0f, audioSource.Volume);
    }

    [Fact]
    public void Volume_DefaultIsOne()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Assert
        Assert.Equal(1.0f, audioSource.Volume);
    }

    #endregion

    #region Playback Control

    [Fact]
    public void PlayOnEnable_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.PlayOnEnable = true;

        // Assert
        Assert.True(audioSource.PlayOnEnable);
    }

    [Fact]
    public void Loop_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.Loop = true;

        // Assert
        Assert.True(audioSource.Loop);
    }

    [Fact]
    public void LoopCount_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.LoopCount = 3;

        // Assert
        Assert.Equal(3, audioSource.LoopCount);
    }

    [Fact]
    public void LoopCount_CanBeNegativeForInfinite()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.LoopCount = -1;

        // Assert
        Assert.Equal(-1, audioSource.LoopCount);
    }

    [Fact]
    public void TriggerPlay_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.TriggerPlay = true;

        // Assert
        Assert.True(audioSource.TriggerPlay);
    }

    [Fact]
    public void TriggerStop_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.TriggerStop = true;

        // Assert
        Assert.True(audioSource.TriggerStop);
    }

    [Fact]
    public void IsPlaying_CanBeSetInternally()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act - Simulate system setting this
        audioSource.IsPlaying = true;

        // Assert
        Assert.True(audioSource.IsPlaying);
    }

    #endregion

    #region Spatial Audio

    [Fact]
    public void EnableSpatialAudio_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.EnableSpatialAudio = true;

        // Assert
        Assert.True(audioSource.EnableSpatialAudio);
    }

    [Fact]
    public void MinDistance_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.MinDistance = 100f;

        // Assert
        Assert.Equal(100f, audioSource.MinDistance);
    }

    [Fact]
    public void MaxDistance_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.MaxDistance = 1000f;

        // Assert
        Assert.Equal(1000f, audioSource.MaxDistance);
    }

    [Fact]
    public void RolloffFactor_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.RolloffFactor = 2.0f;

        // Assert
        Assert.Equal(2.0f, audioSource.RolloffFactor);
    }

    [Fact]
    public void RolloffFactor_CanBeZero()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.RolloffFactor = 0f;

        // Assert
        Assert.Equal(0f, audioSource.RolloffFactor);
    }

    [Fact]
    public void SpatialBlend_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act
        audioSource.SpatialBlend = 0.5f;

        // Assert
        Assert.Equal(0.5f, audioSource.SpatialBlend);
    }

    [Fact]
    public void SpatialVolume_CanBeSetBySystem()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act - Simulate system calculating this
        audioSource.SpatialVolume = 0.75f;

        // Assert
        Assert.Equal(0.75f, audioSource.SpatialVolume);
    }

    [Fact]
    public void SpatialPan_CanBeSetBySystem()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<AudioSourceComponent>();
        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Act - Simulate system calculating this
        audioSource.SpatialPan = 0.3f;

        // Assert
        Assert.Equal(0.3f, audioSource.SpatialPan);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AudioSourceComponent_SimpleSoundEffect_WorksCorrectly()
    {
        // Arrange & Act - Simple sound effect
        var world = CreateTestWorld();
        var mockSound = Substitute.For<ISoundEffect>();

        var entity = world.CreateEntity()
            .AddComponent<AudioSourceComponent>(a =>
            {
                a.SoundEffect = mockSound;
                a.Volume = 0.8f;
                a.TriggerPlay = true;
            });

        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Assert
        Assert.Equal(mockSound, audioSource.SoundEffect);
        Assert.Equal(0.8f, audioSource.Volume);
        Assert.True(audioSource.TriggerPlay);
    }

    [Fact]
    public void AudioSourceComponent_BackgroundMusic_WorksCorrectly()
    {
        // Arrange & Act - Background music
        var world = CreateTestWorld();
        var mockMusic = Substitute.For<IMusic>();

        var entity = world.CreateEntity()
            .AddComponent<AudioSourceComponent>(a =>
            {
                a.Music = mockMusic;
                a.Volume = 0.6f;
                a.Loop = true;
                a.PlayOnEnable = true;
            });

        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Assert
        Assert.Equal(mockMusic, audioSource.Music);
        Assert.Equal(0.6f, audioSource.Volume);
        Assert.True(audioSource.Loop);
        Assert.True(audioSource.PlayOnEnable);
    }

    [Fact]
    public void AudioSourceComponent_SpatialAudio_WorksCorrectly()
    {
        // Arrange & Act - Spatial audio setup
        var world = CreateTestWorld();
        var mockSound = Substitute.For<ISoundEffect>();

        var entity = world.CreateEntity()
            .AddComponent<AudioSourceComponent>(a =>
            {
                a.SoundEffect = mockSound;
                a.EnableSpatialAudio = true;
                a.MinDistance = 100f;
                a.MaxDistance = 800f;
                a.RolloffFactor = 1.5f;
                a.SpatialBlend = 0.9f;
            });

        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Assert
        Assert.True(audioSource.EnableSpatialAudio);
        Assert.Equal(100f, audioSource.MinDistance);
        Assert.Equal(800f, audioSource.MaxDistance);
        Assert.Equal(1.5f, audioSource.RolloffFactor);
        Assert.Equal(0.9f, audioSource.SpatialBlend);
    }

    [Fact]
    public void AudioSourceComponent_CompleteSetup_WorksCorrectly()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var mockSound = Substitute.For<ISoundEffect>();

        var entity = world.CreateEntity()
            .AddComponent<AudioSourceComponent>(a =>
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

        var audioSource = entity.GetComponent<AudioSourceComponent>()!;

        // Assert
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