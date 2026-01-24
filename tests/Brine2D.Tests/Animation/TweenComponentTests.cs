using System.Numerics;
using Brine2D.Animation;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Animation;

public class TweenComponentTests
{
    #region Basic Interpolation Tests

    [Fact]
    public void ShouldInterpolatePositionOverTime()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;

        // Act - Update halfway through
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        // Assert
        transform.Position.X.Should().BeApproximately(50, 0.01f);
        transform.Position.Y.Should().BeApproximately(0, 0.01f);
    }

    [Fact]
    public void ShouldInterpolateScaleOverTime()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        transform.Scale = Vector2.One;

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Scale;
        tween.StartScale = new Vector2(1, 1);
        tween.EndScale = new Vector2(2, 2);
        tween.Duration = 1f;

        // Act - Update halfway through
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        // Assert
        transform.Scale.X.Should().BeApproximately(1.5f, 0.01f);
        transform.Scale.Y.Should().BeApproximately(1.5f, 0.01f);
    }

    [Fact]
    public void ShouldInterpolateRotationOverTime()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        transform.Rotation = 0;

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Rotation;
        tween.StartRotation = 0;
        tween.EndRotation = 360;
        tween.Duration = 1f;

        // Act - Update halfway through
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        // Assert
        transform.Rotation.Should().BeApproximately(180, 0.01f);
    }

    [Fact]
    public void ShouldCompleteAtEndOfDuration()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;

        // Act - Update full duration
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        // Assert
        tween.IsPlaying.Should().BeFalse();
    }

    #endregion

    #region Completion Event Tests

    [Fact]
    public void ShouldFireCompletionEvent()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;

        bool eventFired = false;
        tween.OnComplete += () => eventFired = true;

        // Act
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void ShouldNotFireCompletionEventWhenPaused()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;
        tween.Pause();

        bool eventFired = false;
        tween.OnComplete += () => eventFired = true;

        // Act
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        // Assert
        eventFired.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotFireCompletionEventWhenLooping()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;
        tween.Loop = true;

        bool eventFired = false;
        tween.OnComplete += () => eventFired = true;

        // Act
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        // Assert
        eventFired.Should().BeFalse();
        tween.IsPlaying.Should().BeTrue();
    }

    #endregion

    #region Pause and Resume Tests

    [Fact]
    public void ShouldPauseAndResumeTween()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;

        // Act - Update 25%, pause, update more (should not progress), resume, update 25%
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.25)));
        var positionAfterFirstUpdate = transform.Position.X;

        tween.Pause();
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5))); // Should not progress
        var positionWhilePaused = transform.Position.X;

        tween.Resume();
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.25)));
        var positionAfterResume = transform.Position.X;

        // Assert
        positionAfterFirstUpdate.Should().BeApproximately(25, 0.01f);
        positionWhilePaused.Should().BeApproximately(25, 0.01f); // Unchanged
        positionAfterResume.Should().BeApproximately(50, 0.01f); // Progressed after resume
    }

    [Fact]
    public void ShouldRestartTweenWithPlay()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;

        // Act - Update 50%, then restart
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        tween.Play(); // Restart

        // Assert
        tween.ElapsedTime.Should().Be(0);
        tween.IsPlaying.Should().BeTrue();
    }

    #endregion

    #region Loop and PingPong Tests

    [Fact]
    public void ShouldLoopTween()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;
        tween.Loop = true;

        // Act - Complete one cycle and continue
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));
        world.Update(new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(0.5)));

        // Assert
        tween.IsPlaying.Should().BeTrue();
        tween.ElapsedTime.Should().BeApproximately(0.5f, 0.01f);
        transform.Position.X.Should().BeApproximately(50, 0.01f);
    }

    [Fact]
    public void ShouldPingPongTween()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;
        tween.PingPong = true;

        // Act - Complete forward cycle
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));
        // Start reverse cycle halfway
        world.Update(new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(0.5)));

        // Assert
        tween.IsPlaying.Should().BeTrue();
        transform.Position.X.Should().BeApproximately(50, 0.01f); // Going back from 100
    }

    #endregion

    #region Easing Tests

    [Fact]
    public void ShouldApplyLinearEasing()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;
        tween.Easing = EasingType.Linear;

        // Act
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        // Assert - Linear should be exactly 50 at 0.5
        transform.Position.X.Should().BeApproximately(50, 0.01f);
    }

    [Fact]
    public void ShouldApplyEaseInQuadEasing()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;
        tween.Easing = EasingType.EaseInQuad;

        // Act
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        // Assert - EaseInQuad at 0.5 should be 0.5^2 = 0.25, so position = 25
        transform.Position.X.Should().BeApproximately(25, 0.01f);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ShouldNotUpdateWhenDisabled()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;
        tween.IsEnabled = false;

        // Act
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        // Assert
        transform.Position.X.Should().Be(0); // Unchanged
    }

    [Fact]
    public void ShouldHandleMissingTransformComponent()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        // No transform component added

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;

        // Act & Assert - Should not throw
        var act = () => world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        act.Should().NotThrow();
    }

    [Fact]
    public void ShouldClampElapsedTimeToDuration()
    {
        // Arrange
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        var tween = entity.AddComponent<TweenComponent>();
        tween.Type = TweenType.Position;
        tween.StartPosition = new Vector2(0, 0);
        tween.EndPosition = new Vector2(100, 0);
        tween.Duration = 1f;

        // Act - Update beyond duration
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(5.0)));

        // Assert - Should clamp to end position
        transform.Position.X.Should().BeApproximately(100, 0.01f);
    }

    #endregion
}