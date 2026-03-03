using Brine2D.Hosting;

namespace Brine2D.Tests.Hosting;

public class Brine2DOptionsTests
{
    [Fact]
    public void Validate_DefaultOptions_DoesNotThrow()
    {
        var options = new Brine2DOptions();
        options.Validate();
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Validate_MasterVolumeOutOfRange_ThrowsWithAudioPrefixAndPropertyName(float volume)
    {
        var options = new Brine2DOptions();
        options.Audio.MasterVolume = volume;

        var ex = Assert.Throws<GameConfigurationException>(options.Validate);

        Assert.Contains("Audio", ex.Message);
        Assert.Contains("MasterVolume", ex.Message);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Validate_MusicVolumeOutOfRange_ThrowsWithAudioPrefixAndPropertyName(float volume)
    {
        var options = new Brine2DOptions();
        options.Audio.MusicVolume = volume;

        var ex = Assert.Throws<GameConfigurationException>(options.Validate);

        Assert.Contains("Audio", ex.Message);
        Assert.Contains("MusicVolume", ex.Message);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Validate_SoundVolumeOutOfRange_ThrowsWithAudioPrefixAndPropertyName(float volume)
    {
        var options = new Brine2DOptions();
        options.Audio.SoundVolume = volume;

        var ex = Assert.Throws<GameConfigurationException>(options.Validate);

        Assert.Contains("Audio", ex.Message);
        Assert.Contains("SoundVolume", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public void Validate_MaxTracksOutOfRange_ThrowsWithAudioPrefixAndPropertyName(int maxTracks)
    {
        var options = new Brine2DOptions();
        options.Audio.MaxTracks = maxTracks;

        var ex = Assert.Throws<GameConfigurationException>(options.Validate);

        Assert.Contains("Audio", ex.Message);
        Assert.Contains("MaxTracks", ex.Message);
    }

    [Fact]
    public void Validate_NegativeLoadingScreenMs_Throws()
    {
        var options = new Brine2DOptions { LoadingScreenMinimumDisplayMs = -1 };
        Assert.Throws<GameConfigurationException>(options.Validate);
    }

    [Fact]
    public void Validate_ZeroLoadingScreenMs_DoesNotThrow()
    {
        var options = new Brine2DOptions { LoadingScreenMinimumDisplayMs = 0 };
        options.Validate();
    }

    [Fact]
    public void Validate_GracePeriodExceedsShutdownTimeout_Throws()
    {
        var options = new Brine2DOptions
        {
            ShutdownTimeoutSeconds = 5,
            ForceShutdownGracePeriodSeconds = 6
        };

        var ex = Assert.Throws<GameConfigurationException>(options.Validate);

        Assert.Contains("ForceShutdownGracePeriodSeconds", ex.Message);
    }

    [Fact]
    public void Validate_NonHeadlessZeroWindowDimensions_Throws()
    {
        var options = new Brine2DOptions { Headless = false };
        options.Window.Width = 0;

        var ex = Assert.Throws<GameConfigurationException>(options.Validate);

        Assert.Contains("Window.Width", ex.Message);
    }

    [Fact]
    public void Validate_MultipleInvalidValues_ThrowsWithAllErrorsInOneMessage()
    {
        var options = new Brine2DOptions();
        options.Audio.MasterVolume = 2.0f;
        options.Audio.MusicVolume  = -1.0f;

        var ex = Assert.Throws<GameConfigurationException>(options.Validate);

        Assert.Contains("MasterVolume", ex.Message);
        Assert.Contains("MusicVolume",  ex.Message);
    }

    [Fact]
    public void Validate_InvalidOptions_ErrorMessageContainsActionableFixHint()
    {
        var options = new Brine2DOptions();
        options.Audio.MaxTracks = 0;

        var ex = Assert.Throws<GameConfigurationException>(options.Validate);

        Assert.Contains("Program.cs", ex.Message);
    }
}