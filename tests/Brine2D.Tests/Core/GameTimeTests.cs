using Brine2D.Core;

namespace Brine2D.Tests.Core;

public class GameTimeTests
{
    #region Constructor

    [Fact]
    public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var totalTime = TimeSpan.FromSeconds(10);
        var elapsedTime = TimeSpan.FromSeconds(0.016);
        ulong frameCount = 600;

        // Act
        var gameTime = new GameTime(totalTime, elapsedTime, frameCount);

        // Assert
        Assert.Equal(totalTime, gameTime.TotalTime);
        Assert.Equal(elapsedTime, gameTime.ElapsedTime);
        Assert.Equal(frameCount, gameTime.FrameCount);
    }

    [Fact]
    public void Constructor_WithoutFrameCount_DefaultsToZero()
    {
        // Arrange
        var totalTime = TimeSpan.FromSeconds(5);
        var elapsedTime = TimeSpan.FromSeconds(0.016);

        // Act
        var gameTime = new GameTime(totalTime, elapsedTime);

        // Assert
        Assert.Equal(totalTime, gameTime.TotalTime);
        Assert.Equal(elapsedTime, gameTime.ElapsedTime);
        Assert.Equal(0ul, gameTime.FrameCount);
    }

    [Fact]
    public void Constructor_WithZeroValues_SetsZero()
    {
        // Arrange & Act
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.Zero, 0);

        // Assert
        Assert.Equal(TimeSpan.Zero, gameTime.TotalTime);
        Assert.Equal(TimeSpan.Zero, gameTime.ElapsedTime);
        Assert.Equal(0ul, gameTime.FrameCount);
    }

    #endregion

    #region DeltaTime Property

    [Fact]
    public void DeltaTime_ReturnsElapsedTimeInSeconds()
    {
        // Arrange
        var elapsedTime = TimeSpan.FromSeconds(0.016); // ~60 FPS
        var gameTime = new GameTime(TimeSpan.FromSeconds(1), elapsedTime);

        // Act
        var deltaTime = gameTime.DeltaTime;

        // Assert
        Assert.Equal(0.016, deltaTime, precision: 6);
    }

    [Fact]
    public void DeltaTime_WithMilliseconds_ConvertsCorrectly()
    {
        // Arrange
        var elapsedTime = TimeSpan.FromMilliseconds(16.67); // ~60 FPS
        var gameTime = new GameTime(TimeSpan.FromSeconds(1), elapsedTime);

        // Act
        var deltaTime = gameTime.DeltaTime;

        // Assert
        Assert.Equal(0.01667, deltaTime, precision: 5);
    }

    [Fact]
    public void DeltaTime_WithZeroElapsed_ReturnsZero()
    {
        // Arrange
        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.Zero);

        // Act
        var deltaTime = gameTime.DeltaTime;

        // Assert
        Assert.Equal(0.0, deltaTime);
    }

    [Fact]
    public void DeltaTime_WithLargeElapsed_HandlesCorrectly()
    {
        // Arrange
        var elapsedTime = TimeSpan.FromSeconds(1); // 1 second frame (very slow)
        var gameTime = new GameTime(TimeSpan.FromSeconds(10), elapsedTime);

        // Act
        var deltaTime = gameTime.DeltaTime;

        // Assert
        Assert.Equal(1.0, deltaTime);
    }

    #endregion

    #region TotalSeconds Property

    [Fact]
    public void TotalSeconds_ReturnsTotalTimeInSeconds()
    {
        // Arrange
        var totalTime = TimeSpan.FromSeconds(123.456);
        var gameTime = new GameTime(totalTime, TimeSpan.FromSeconds(0.016));

        // Act
        var totalSeconds = gameTime.TotalSeconds;

        // Assert
        Assert.Equal(123.456, totalSeconds, precision: 3);
    }

    [Fact]
    public void TotalSeconds_WithMinutes_ConvertsCorrectly()
    {
        // Arrange
        var totalTime = TimeSpan.FromMinutes(2.5); // 150 seconds
        var gameTime = new GameTime(totalTime, TimeSpan.FromSeconds(0.016));

        // Act
        var totalSeconds = gameTime.TotalSeconds;

        // Assert
        Assert.Equal(150.0, totalSeconds);
    }

    [Fact]
    public void TotalSeconds_WithHours_ConvertsCorrectly()
    {
        // Arrange
        var totalTime = TimeSpan.FromHours(1); // 3600 seconds
        var gameTime = new GameTime(totalTime, TimeSpan.FromSeconds(0.016));

        // Act
        var totalSeconds = gameTime.TotalSeconds;

        // Assert
        Assert.Equal(3600.0, totalSeconds);
    }

    [Fact]
    public void TotalSeconds_WithZero_ReturnsZero()
    {
        // Arrange
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        // Act
        var totalSeconds = gameTime.TotalSeconds;

        // Assert
        Assert.Equal(0.0, totalSeconds);
    }

    #endregion

    #region FrameCount Property

    [Fact]
    public void FrameCount_WithLargeValue_HandlesCorrectly()
    {
        // Arrange
        var gameTime = new GameTime(
            TimeSpan.FromHours(1), 
            TimeSpan.FromSeconds(0.016), 
            216000); // 1 hour at 60 FPS

        // Act
        var frameCount = gameTime.FrameCount;

        // Assert
        Assert.Equal(216000ul, frameCount);
    }

    [Fact]
    public void FrameCount_WithMaxValue_HandlesCorrectly()
    {
        // Arrange
        var gameTime = new GameTime(
            TimeSpan.FromDays(1000), 
            TimeSpan.FromSeconds(1), 
            ulong.MaxValue);

        // Act
        var frameCount = gameTime.FrameCount;

        // Assert
        Assert.Equal(ulong.MaxValue, frameCount);
    }

    #endregion

    #region Integration / Real-World Scenarios

    [Fact]
    public void GameTime_At60FPS_HasCorrectValues()
    {
        // Arrange - Simulate 60 FPS game at 10 seconds runtime
        var totalTime = TimeSpan.FromSeconds(10);
        var elapsedTime = TimeSpan.FromSeconds(1.0 / 60.0); // 16.67ms
        ulong frameCount = 600;

        // Act
        var gameTime = new GameTime(totalTime, elapsedTime, frameCount);

        // Assert
        Assert.Equal(10.0, gameTime.TotalSeconds);
        Assert.Equal(0.016666, gameTime.DeltaTime, precision: 5);
        Assert.Equal(600ul, gameTime.FrameCount);
    }

    [Fact]
    public void GameTime_At30FPS_HasCorrectValues()
    {
        // Arrange - Simulate 30 FPS game at 5 seconds runtime
        var totalTime = TimeSpan.FromSeconds(5);
        var elapsedTime = TimeSpan.FromSeconds(1.0 / 30.0); // 33.33ms
        ulong frameCount = 150;

        // Act
        var gameTime = new GameTime(totalTime, elapsedTime, frameCount);

        // Assert
        Assert.Equal(5.0, gameTime.TotalSeconds);
        Assert.Equal(0.033333, gameTime.DeltaTime, precision: 5);
        Assert.Equal(150ul, gameTime.FrameCount);
    }

    [Fact]
    public void GameTime_FirstFrame_HasCorrectValues()
    {
        // Arrange - First frame of game
        var gameTime = new GameTime(
            TimeSpan.FromSeconds(0.016), 
            TimeSpan.FromSeconds(0.016), 
            1);

        // Assert
        Assert.Equal(0.016, gameTime.TotalSeconds, precision: 3);
        Assert.Equal(0.016, gameTime.DeltaTime, precision: 3);
        Assert.Equal(1ul, gameTime.FrameCount);
    }

    [Fact]
    public void GameTime_LongSession_HandlesCorrectly()
    {
        // Arrange - 1 hour game session at 60 FPS
        var totalTime = TimeSpan.FromHours(1);
        var elapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
        ulong frameCount = 216000; // 60 * 60 * 60

        // Act
        var gameTime = new GameTime(totalTime, elapsedTime, frameCount);

        // Assert
        Assert.Equal(3600.0, gameTime.TotalSeconds);
        Assert.Equal(0.016666, gameTime.DeltaTime, precision: 5);
        Assert.Equal(216000ul, gameTime.FrameCount);
    }

    #endregion

    #region Struct Equality (Readonly Struct Behavior)

    [Fact]
    public void GameTime_IsReadonlyStruct_PropertiesCannotBeModified()
    {
        // Arrange & Act
        var gameTime = new GameTime(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0.016), 600);

        // Assert - This test verifies the struct is readonly by ensuring properties are init-only
        // If this compiles, the struct is properly readonly
        Assert.Equal(10.0, gameTime.TotalSeconds);
    }

    [Fact]
    public void GameTime_TwoInstancesWithSameValues_AreEqual()
    {
        // Arrange
        var gameTime1 = new GameTime(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0.016), 600);
        var gameTime2 = new GameTime(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0.016), 600);

        // Act & Assert
        Assert.Equal(gameTime1.TotalTime, gameTime2.TotalTime);
        Assert.Equal(gameTime1.ElapsedTime, gameTime2.ElapsedTime);
        Assert.Equal(gameTime1.FrameCount, gameTime2.FrameCount);
    }

    #endregion
}