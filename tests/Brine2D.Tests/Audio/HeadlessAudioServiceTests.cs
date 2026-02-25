using Brine2D.Audio;

namespace Brine2D.Tests.Audio;

public class HeadlessAudioServiceTests
{
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
        var service = new HeadlessAudioService(options);

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
        var service = new HeadlessAudioService(options);

        // Assert — headless volumes must mirror what the real service would use
        Assert.Equal(options.MasterVolume, service.MasterVolume);
        Assert.Equal(options.MusicVolume,  service.MusicVolume);
        Assert.Equal(options.SoundVolume,  service.SoundVolume);
    }

    [Fact]
    public void MasterVolume_CanBeChangedAfterConstruction()
    {
        // Arrange
        var service = new HeadlessAudioService(new AudioOptions());

        // Act
        service.MasterVolume = 0.25f;

        // Assert
        Assert.Equal(0.25f, service.MasterVolume);
    }
}