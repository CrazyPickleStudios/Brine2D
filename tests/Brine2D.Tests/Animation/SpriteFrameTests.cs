using Brine2D.Animation;
using Brine2D.Core;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Animation;

public class SpriteFrameTests
{
    [Fact]
    public void Clone_CopiesAllVisualProperties()
    {
        var frame = new SpriteFrame(new Rectangle(10, 20, 32, 32), 0.2f)
        {
            FlipX = true,
            FlipY = false,
            Tint = new Color(255, 0, 0, 128),
            HitBox = new Rectangle(2, 2, 10, 10),
            UserData = "tag"
        };

        var clone = frame.Clone();

        clone.SourceRect.Should().Be(frame.SourceRect);
        clone.Duration.Should().BeApproximately(frame.Duration, 0.0001f);
        clone.FlipX.Should().Be(frame.FlipX);
        clone.FlipY.Should().Be(frame.FlipY);
        clone.Tint.Should().Be(frame.Tint);
        clone.HitBox.Should().Be(frame.HitBox);
        clone.UserData.Should().Be(frame.UserData);
    }

    [Fact]
    public void Clone_CopiesNamedHitBoxes()
    {
        var frame = new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f);
        frame.SetHitBox("head", new Rectangle(2, 0, 12, 6));
        frame.SetHitBox("body", new Rectangle(2, 6, 12, 10));

        var clone = frame.Clone();

        clone.GetHitBox("head").Should().Be(frame.GetHitBox("head"));
        clone.GetHitBox("body").Should().Be(frame.GetHitBox("body"));
    }

    [Fact]
    public void Clone_DoesNotShareNamedHitBoxDictionary()
    {
        var frame = new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f);
        frame.SetHitBox("attack", new Rectangle(0, 0, 8, 8));

        var clone = frame.Clone();
        clone.SetHitBox("attack", new Rectangle(4, 4, 4, 4));

        frame.GetHitBox("attack").Should().Be(new Rectangle(0, 0, 8, 8),
            "mutating named hit boxes on the clone must not affect the original");
    }

    [Fact]
    public void Clone_DoesNotCopyOnEnterSubscriptions()
    {
        var frame = new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f);
        int enterCount = 0;
        frame.OnEnter += () => enterCount++;

        // Play the clone through a temporary animator to trigger OnEnter.
        var clone = frame.Clone();
        var clip = new AnimationClip("test") { PlaybackMode = PlaybackMode.Loop };
        clip.AddFrame(clone);
        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Play("test");

        enterCount.Should().Be(0,
            "OnEnter subscribers from the original must not transfer to the clone");
    }

    [Fact]
    public void Clone_IsIndependentOfOriginalDurationChange()
    {
        var frame = new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f);
        var clone = frame.Clone();

        frame.Duration = 0.5f;

        clone.Duration.Should().BeApproximately(0.1f, 0.0001f,
            "clone duration is independent of the original");
    }
}