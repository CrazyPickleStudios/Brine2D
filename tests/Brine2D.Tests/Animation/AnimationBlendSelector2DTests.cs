using Brine2D.Animation;
using Brine2D.Core;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Animation;

public class AnimationBlendSelector2DTests
{
    private static AnimationClip MakeClip(string name, int frames, PlaybackMode mode, float frameDuration = 0.1f)
    {
        var clip = new AnimationClip(name) { PlaybackMode = mode };
        for (int i = 0; i < frames; i++)
            clip.AddFrame(new SpriteFrame(new Rectangle(i * 16, 0, 16, 16), frameDuration));
        return clip;
    }

    private static SpriteAnimator MakeAnimator(params string[] clips)
    {
        var a = new SpriteAnimator();
        foreach (var name in clips)
        {
            var clip = new AnimationClip(name) { PlaybackMode = PlaybackMode.Loop };
            clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
            a.AddAnimation(clip);
        }
        return a;
    }

    private static SpriteAnimator MakeAnimator(params AnimationClip[] clips)
    {
        var a = new SpriteAnimator();
        foreach (var clip in clips)
            a.AddAnimation(clip);
        return a;
    }

    [Fact]
    public void Evaluate_SelectsNearestNode()
    {
        var animator = MakeAnimator("north", "south", "east", "west");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 1f, "north");
        tree.AddNode(0f, -1f, "south");
        tree.AddNode(1f, 0f, "east");
        tree.AddNode(-1f, 0f, "west");

        tree.SetValue(0.1f, 0.9f);
        tree.Evaluate();

        animator.CurrentAnimation!.Name.Should().Be("north");
    }

    [Fact]
    public void Evaluate_DoesNotCallPlay_WhenClipUnchangedAndNotFinished()
    {
        var animator = MakeAnimator("walk");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "walk");

        tree.SetValue(0f, 0f);
        tree.Evaluate();

        int starts = 0;
        animator.OnAnimationStart += _ => starts++;

        tree.Evaluate();

        starts.Should().Be(0);
    }

    [Fact]
    public void SetValue_MarksDirty_EvaluateAppliesChange()
    {
        var animator = MakeAnimator("idle", "run");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "idle");
        tree.AddNode(5f, 0f, "run");

        tree.SetValue(0f, 0f);
        tree.Evaluate();

        tree.SetValue(5f, 0f);
        tree.Evaluate();

        animator.CurrentAnimation!.Name.Should().Be("run");
    }

    [Fact]
    public void Evaluate_IsLazy_SkipsWhenNotDirty()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "idle");

        tree.SetValue(0f, 0f);
        tree.Evaluate();

        int starts = 0;
        animator.OnAnimationStart += _ => starts++;
        tree.Evaluate();

        starts.Should().Be(0);
    }

    [Fact]
    public void Evaluate_RestartsFinishedClip()
    {
        var animator = new SpriteAnimator();
        var clip = new AnimationClip("once") { PlaybackMode = PlaybackMode.OnceHoldLast };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        animator.AddAnimation(clip);

        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "once");

        tree.SetValue(0f, 0f);
        tree.Evaluate();
        animator.Update(0.2f);

        tree.Evaluate();

        animator.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void SelectsNearestNode_2DDistance()
    {
        var animator = MakeAnimator("up", "right", "down", "left");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 1f, "up");
        tree.AddNode(1f, 0f, "right");
        tree.AddNode(0f, -1f, "down");
        tree.AddNode(-1f, 0f, "left");

        tree.SetValue(0.1f, 0.9f);
        Assert.Equal("up", animator.CurrentAnimation?.Name);

        tree.SetValue(0.9f, -0.1f);
        Assert.Equal("right", animator.CurrentAnimation?.Name);

        tree.SetValue(-0.9f, 0.1f);
        Assert.Equal("left", animator.CurrentAnimation?.Name);
    }

    [Fact]
    public void NodesEmpty_EvaluateIsNoOp()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector2D(animator);
        var ex = Record.Exception(() => tree.SetValue(1f, 1f));
        Assert.Null(ex);
        Assert.Null(animator.CurrentAnimation);
    }

    [Fact]
    public void RemoveNode_ByName_Removes()
    {
        var animator = MakeAnimator("idle", "walk");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "idle");
        tree.AddNode(1f, 0f, "walk");

        Assert.True(tree.RemoveNode("walk"));
        tree.SetValue(1f, 0f);
        Assert.Equal("idle", animator.CurrentAnimation?.Name);
    }

    [Fact]
    public void RemoveNode_ByPosition_UsesEpsilonComparison()
    {
        var animator = MakeAnimator("idle", "run");
        var tree = new AnimationBlendSelector2D(animator);
        float x = 0.1f + 0.2f;
        tree.AddNode(x, 0f, "idle");
        tree.AddNode(1f, 0f, "run");

        bool removed = tree.RemoveNode(0.3f, 0f);

        Assert.True(removed);
    }

    [Fact]
    public void RemoveNode_ByPosition_ReturnsFalse_WhenNotFound()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "idle");

        Assert.False(tree.RemoveNode(9f, 9f));
    }

    [Fact]
    public void CrossFadeDuration_StartsFadeOnNodeChange()
    {
        var idle = MakeClip("idle", 2, PlaybackMode.Loop);
        var walk = MakeClip("walk", 2, PlaybackMode.Loop);
        var animator = MakeAnimator(idle, walk);
        var tree = new AnimationBlendSelector2D(animator);
        tree.CrossFadeDuration = 0.3f;
        tree.AddNode(0f, 0f, "idle");
        tree.AddNode(1f, 0f, "walk");

        tree.SetValue(0f, 0f);
        Assert.Equal("idle", animator.CurrentAnimation?.Name);

        tree.SetValue(1f, 0f);
        Assert.Equal("walk", animator.CurrentAnimation?.Name);
        Assert.True(animator.CrossFadeAlpha < 1f);
    }

    [Fact]
    public void OnClipChanged_FiredWhenClipChanges()
    {
        var animator = MakeAnimator("idle", "run");
        var tree = new AnimationBlendSelector2D(animator);

        var changes = new List<(string? From, string To)>();
        tree.OnClipChanged += (f, t) => changes.Add((f, t));

        tree.AddNode(0f, 0f, "idle");
        tree.AddNode(5f, 0f, "run");
        tree.SetValue(5f, 0f);

        changes.Should().HaveCount(2);
        changes[0].Should().Be((null, "idle"));
        changes[1].Should().Be(("idle", "run"));
    }

    [Fact]
    public void OnClipChanged_NotFiredWhenClipUnchanged()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "idle");

        tree.SetValue(0f, 0f);
        tree.Evaluate();

        int count = 0;
        tree.OnClipChanged += (_, _) => count++;

        tree.SetValue(0.1f, 0f);
        tree.Evaluate();

        count.Should().Be(0);
    }

    [Fact]
    public void RespectNonLoopingClips_DoesNotInterruptExternalClip()
    {
        var animator = MakeAnimator("idle");
        var attack = new AnimationClip("attack") { PlaybackMode = PlaybackMode.OnceHoldLast };
        attack.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.5f));
        animator.AddAnimation(attack);

        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "idle");
        tree.RespectNonLoopingClips = true;

        tree.SetValue(0f, 0f);
        tree.Evaluate();

        animator.Play("attack");
        tree.SetValue(0f, 0f);
        tree.Evaluate();

        animator.CurrentAnimation!.Name.Should().Be("attack");
    }

    [Fact]
    public void ClearNodes_ResetsActiveClip()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "idle");
        tree.SetValue(0f, 0f);
        tree.Evaluate();

        tree.ClearNodes();

        tree.ActiveClip.Should().BeNull();
    }

    [Fact]
    public void XY_Properties_ReflectLastSetValue()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector2D(animator);

        tree.SetValue(3f, -2f);

        tree.X.Should().Be(3f);
        tree.Y.Should().Be(-2f);
    }

    [Fact]
    public void AddNode_DuplicatePosition_Throws()
    {
        var animator = MakeAnimator("up", "down");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 1f, "up");

        var act = () => tree.AddNode(0f, 1f, "down");

        act.Should().Throw<ArgumentException>().WithMessage("*position*");
    }

    [Fact]
    public void AddNode_DuplicatePositionWithinEpsilon_Throws()
    {
        var animator = MakeAnimator("up", "down");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 1f, "up");

        var act = () => tree.AddNode(1e-6f, 1f, "down");

        act.Should().Throw<ArgumentException>("positions within epsilon are treated as the same");
    }

    [Fact]
    public void AddNode_DistinctPositions_DoesNotThrow()
    {
        var animator = MakeAnimator("up", "down");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 1f, "up");

        var act = () => tree.AddNode(0f, -1f, "down");

        act.Should().NotThrow();
        tree.NodeCount.Should().Be(2);
    }

    [Fact]
    public void AddNode_AfterRemoveByPosition_SamePositionAllowed()
    {
        var animator = MakeAnimator("up", "down");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 1f, "up");
        tree.RemoveNode(0f, 1f);

        var act = () => tree.AddNode(0f, 1f, "down");

        act.Should().NotThrow("node was removed so the slot is free");
    }

    [Fact]
    public void ValidateNodes_ReturnsEmpty_WhenAllClipsRegistered()
    {
        var animator = MakeAnimator("north", "south", "east", "west");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 1f, "north");
        tree.AddNode(0f, -1f, "south");
        tree.AddNode(1f, 0f, "east");
        tree.AddNode(-1f, 0f, "west");

        var issues = tree.ValidateNodes();

        issues.Should().BeEmpty("all node clip names are registered on the animator");
    }

    [Fact]
    public void ValidateNodes_ReturnsIssue_WhenClipNotRegistered()
    {
        var animator = MakeAnimator("north");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 1f, "north");
        tree.AddNode(0f, -1f, "typo_south");

        var issues = tree.ValidateNodes();

        issues.Should().HaveCount(1);
        issues[0].Should().Contain("typo_south");
    }

    [Fact]
    public void ValidateNodes_ReturnsEmpty_WhenNoNodesAdded()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector2D(animator);

        var issues = tree.ValidateNodes();

        issues.Should().BeEmpty();
    }

    [Fact]
    public void ValidateNodes_ReturnsMultipleIssues_ForMultipleMissingClips()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "idle");
        tree.AddNode(1f, 0f, "missing_a");
        tree.AddNode(0f, 1f, "missing_b");

        var issues = tree.ValidateNodes();

        issues.Should().HaveCount(2);
        issues.Should().Contain(i => i.Contains("missing_a"));
        issues.Should().Contain(i => i.Contains("missing_b"));
    }

    [Fact]
    public void IsEnabled_False_PreventsEvaluate_FromChangingClip()
    {
        var animator = MakeAnimator("idle", "run");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "idle");
        tree.AddNode(5f, 0f, "run");
        tree.SetValue(0f, 0f);
        tree.Evaluate();

        tree.IsEnabled = false;
        tree.SetValue(5f, 0f);
        tree.Evaluate();

        animator.CurrentAnimation!.Name.Should().Be("idle",
            "Evaluate is a no-op while IsEnabled is false");
    }

    [Fact]
    public void IsEnabled_True_AfterDisable_ResumesEvaluation()
    {
        var animator = MakeAnimator("idle", "run");
        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "idle");
        tree.AddNode(5f, 0f, "run");
        tree.SetValue(0f, 0f);
        tree.Evaluate();

        tree.IsEnabled = false;
        tree.SetValue(5f, 0f);
        tree.Evaluate();

        tree.IsEnabled = true;
        tree.Evaluate();

        animator.CurrentAnimation!.Name.Should().Be("run",
            "re-enabling should let the pending dirty value take effect");
    }

    [Fact]
    public void ResumesAfterExternalNonLoopingClipFinishes()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle", 2, PlaybackMode.Loop));
        animator.AddAnimation(new AnimationClip("attack") { PlaybackMode = PlaybackMode.OnceHoldLast }
            .AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f)));

        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "idle");
        tree.SetValue(0f, 0f);
        tree.Evaluate();

        animator.Play("attack", restart: true);
        tree.Evaluate();
        animator.CurrentAnimation!.Name.Should().Be("attack");

        animator.Update(0.15f);
        animator.IsFinished.Should().BeTrue();

        tree.Evaluate();
        animator.CurrentAnimation!.Name.Should().Be("idle", "2D tree must also re-engage after external clip ends");
    }

    [Fact]
    public void SpeedNotAppliedToExternalClipWhileYielding()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle", 2, PlaybackMode.Loop));
        animator.AddAnimation(new AnimationClip("attack") { PlaybackMode = PlaybackMode.OnceHoldLast }
            .AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f)));

        var tree = new AnimationBlendSelector2D(animator);
        tree.AddNode(0f, 0f, "idle", speed: 3f);
        tree.SetValue(0f, 0f);
        tree.Evaluate();

        animator.Play("attack", restart: true);
        animator.Speed = 1f;
        tree.Evaluate();

        animator.Speed.Should().Be(1f, "2D tree must not write speed while yielding");
    }
}