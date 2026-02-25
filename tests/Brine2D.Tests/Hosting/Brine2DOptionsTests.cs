using Brine2D.Hosting;

namespace Brine2D.Tests.Hosting;

public class Brine2DOptionsTests
{
    #region Defaults

    [Fact]
    public void Validate_DefaultOptions_DoesNotThrow()
    {
        // Arrange
        var options = new Brine2DOptions();

        // Act & Assert
        options.Validate(); // must not throw
    }

    #endregion

    #region Audio validation

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Validate_MasterVolumeOutOfRange_ThrowsWithAudioPrefixAndPropertyName(float volume)
    {
        // Arrange
        var options = new Brine2DOptions();
        options.Audio.MasterVolume = volume;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(options.Validate);

        // Assert
        Assert.Contains("Audio", ex.Message);
        Assert.Contains("MasterVolume", ex.Message);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Validate_MusicVolumeOutOfRange_ThrowsWithAudioPrefixAndPropertyName(float volume)
    {
        // Arrange
        var options = new Brine2DOptions();
        options.Audio.MusicVolume = volume;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(options.Validate);

        // Assert
        Assert.Contains("Audio", ex.Message);
        Assert.Contains("MusicVolume", ex.Message);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Validate_SoundVolumeOutOfRange_ThrowsWithAudioPrefixAndPropertyName(float volume)
    {
        // Arrange
        var options = new Brine2DOptions();
        options.Audio.SoundVolume = volume;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(options.Validate);

        // Assert
        Assert.Contains("Audio", ex.Message);
        Assert.Contains("SoundVolume", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public void Validate_MaxTracksOutOfRange_ThrowsWithAudioPrefixAndPropertyName(int maxTracks)
    {
        // Arrange
        var options = new Brine2DOptions();
        options.Audio.MaxTracks = maxTracks;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(options.Validate);

        // Assert
        Assert.Contains("Audio", ex.Message);
        Assert.Contains("MaxTracks", ex.Message);
    }

    #endregion

    #region LoadingScreenMinimumDisplayMs validation

    [Fact]
    public void Validate_NegativeLoadingScreenMs_Throws()
    {
        // Arrange
        var options = new Brine2DOptions { LoadingScreenMinimumDisplayMs = -1 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_ZeroLoadingScreenMs_DoesNotThrow()
    {
        // Arrange — zero is explicitly documented as "disable"
        var options = new Brine2DOptions { LoadingScreenMinimumDisplayMs = 0 };

        // Act & Assert
        options.Validate();
    }

    #endregion

    #region Error message quality

    [Fact]
    public void Validate_MultipleInvalidValues_ThrowsWithAllErrorsInOneMessage()
    {
        // Arrange
        var options = new Brine2DOptions();
        options.Audio.MasterVolume = 2.0f;
        options.Audio.MusicVolume  = -1.0f;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(options.Validate);

        // Assert — both errors must appear in the single thrown message
        Assert.Contains("MasterVolume", ex.Message);
        Assert.Contains("MusicVolume",  ex.Message);
    }

    [Fact]
    public void Validate_InvalidOptions_ErrorMessageContainsActionableFixHint()
    {
        // Arrange
        var options = new Brine2DOptions();
        options.Audio.MaxTracks = 0;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(options.Validate);

        // Assert — the fix hint should point developers to the right place
        Assert.Contains("Program.cs", ex.Message);
    }

    #endregion
}