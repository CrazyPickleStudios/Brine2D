using Brine2D.Animation;
using Brine2D.Core;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Animation;

public class AnimationBlendSelector1DTests
{
    private static AnimationClip MakeClip(string name, int frames, PlaybackMode mode, float frameDuration = 0.1f)
    {
        var clip = new AnimationClip(name) { PlaybackMode = mode };
        for (int i = 0; i < frames; i++)
            clip.AddFrame(new SpriteFrame(new Rectangle(i * 16, 0, 16, 16), frameDuration));
        return clip;
    }

    private static SpriteAnimator MakeAnimator(params string[] clipNames)
    {
        var animator = new SpriteAnimator();
        foreach (var name in clipNames)
        {
            var clip = new AnimationClip(name) { PlaybackMode = PlaybackMode.Loop };
            clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
            animator.AddAnimation(clip);
        }
        return animator;
    }

    private static SpriteAnimator MakeAnimator(params AnimationClip[] clips)
    {
        var animator = new SpriteAnimator();
        foreach (var clip in clips)
            animator.AddAnimation(clip);
        return animator;
    }

    [Fact]
    public void AddNode_DuplicateThreshold_Throws()
    {
        var animator = MakeAnimator("idle", "walk");
        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "idle");

        var act = () => tree.AddNode(0f, "walk");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*threshold*");
    }

    [Fact]
    public void AddNode_DuplicateThresholdWithinEpsilon_Throws()
    {
        var animator = MakeAnimator("idle", "walk");
        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(1f, "idle");

        var act = () => tree.AddNode(1f + 1e-7f, "walk");

        act.Should().Throw<ArgumentException>("values within epsilon are treated as the same threshold");
    }

    [Fact]
    public void AddNode_DistinctThresholds_DoesNotThrow()
    {
        var animator = MakeAnimator("idle", "walk");
        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "idle");

        var act = () => tree.AddNode(1f, "walk");

        act.Should().NotThrow();
        tree.NodeCount.Should().Be(2);
    }

    [Fact]
    public void AddNode_AfterRemove_SameThresholdAllowed()
    {
        var animator = MakeAnimator("idle", "walk");
        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "idle");
        tree.RemoveNode(0f);

        var act = () => tree.AddNode(0f, "walk");

        act.Should().NotThrow("node was removed so the slot is free");
    }

    [Fact]
    public void Evaluate_SelectsClosestNode()
    {
        var animator = MakeAnimator("idle", "walk", "run");
        var tree = new AnimationBlendSelector1D(animator)
        {
            Value = 0f
        };
        tree.AddNode(0f, "idle");
        tree.AddNode(1f, "walk");
        tree.AddNode(2f, "run");

        tree.Evaluate();

        animator.CurrentAnimation!.Name.Should().Be("idle");

        tree.Value = 1.6f;
        tree.Evaluate();
        animator.CurrentAnimation!.Name.Should().Be("run");
    }

    [Fact]
    public void SelectsFirstClip_WhenValueBelowAllThresholds()
    {
        var animator = MakeAnimator("idle", "walk", "run");
        var tree = new AnimationBlendSelector1D(animator)
            .AddNode(0f, "idle")
            .AddNode(0.5f, "walk")
            .AddNode(1f, "run");

        tree.Value = -1f;
        Assert.Equal("idle", animator.CurrentAnimation?.Name);
    }

    [Fact]
    public void SelectsLastClip_WhenValueAboveAllThresholds()
    {
        var animator = MakeAnimator("idle", "run");
        var tree = new AnimationBlendSelector1D(animator)
            .AddNode(0f, "idle")
            .AddNode(1f, "run");

        tree.Value = 5f;
        Assert.Equal("run", animator.CurrentAnimation?.Name);
    }

    [Fact]
    public void SelectsNearestClip_BetweenTwoNodes()
    {
        var animator = MakeAnimator("walk", "run");
        var tree = new AnimationBlendSelector1D(animator)
            .AddNode(0f, "walk")
            .AddNode(1f, "run");

        tree.Value = 0.3f;
        Assert.Equal("walk", animator.CurrentAnimation?.Name);

        tree.Value = 0.7f;
        Assert.Equal("run", animator.CurrentAnimation?.Name);
    }

    [Fact]
    public void DoesNotCallPlay_WhenClipUnchanged()
    {
        var animator = MakeAnimator("idle", "run");
        var tree = new AnimationBlendSelector1D(animator)
            .AddNode(0f, "idle")
            .AddNode(1f, "run");

        tree.Value = 0.1f;
        int startCount = 0;
        animator.OnAnimationStart += _ => startCount++;

        tree.Value = 0.2f;
        tree.Value = 0.15f;

        Assert.Equal(0, startCount);
    }

    [Fact]
    public void InterpolatesSpeed_BetweenNodesWithSpeedOverrides()
    {
        var animator = MakeAnimator("walk", "run");
        var tree = new AnimationBlendSelector1D(animator)
            .AddNode(0f, "walk", speed: 1f)
            .AddNode(1f, "run", speed: 2f);

        tree.Value = 0.5f;
        Assert.Equal(1.5f, animator.Speed, precision: 4);
    }

    [Fact]
    public void AppliesNodeSpeed_WhenOnlyOneNodeHasSpeed()
    {
        var animator = MakeAnimator("idle", "walk");
        var tree = new AnimationBlendSelector1D(animator)
            .AddNode(0f, "idle")
            .AddNode(1f, "walk", speed: 0.8f);

        tree.Value = 0.9f;
        Assert.Equal(0.8f, animator.Speed, precision: 4);
    }

    [Fact]
    public void SingleNode_AlwaysPlaysIt()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector1D(animator).AddNode(0f, "idle");

        tree.Value = 999f;
        Assert.Equal("idle", animator.CurrentAnimation?.Name);
    }

    [Fact]
    public void ActiveClip_ReflectsSelection()
    {
        var animator = MakeAnimator("walk", "run");
        var tree = new AnimationBlendSelector1D(animator)
            .AddNode(0f, "walk")
            .AddNode(1f, "run");

        tree.Value = 0.1f;
        Assert.Equal("walk", tree.ActiveClip);

        tree.Value = 0.9f;
        Assert.Equal("run", tree.ActiveClip);
    }

    [Fact]
    public void ClearNodes_ResetsActiveClip()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector1D(animator).AddNode(0f, "idle");
        tree.Value = 0f;

        tree.ClearNodes();
        Assert.Null(tree.ActiveClip);
    }

    [Fact]
    public void Evaluate_NodesAddedAfterConstruction()
    {
        var animator = MakeAnimator("idle", "walk");
        var tree = new AnimationBlendSelector1D(animator);
        tree.Value = 0.9f;
        Assert.Null(tree.ActiveClip);

        tree.AddNode(0f, "idle").AddNode(1f, "walk");
        tree.Evaluate();
        Assert.Equal("walk", tree.ActiveClip);
    }

    [Fact]
    public void RemoveNode_ByThreshold_RemovesNode()
    {
        var animator = MakeAnimator("walk", "run");
        var tree = new AnimationBlendSelector1D(animator)
            .AddNode(0f, "walk")
            .AddNode(1f, "run");

        var removed = tree.RemoveNode(0f);

        Assert.True(removed);
        tree.Evaluate();
        Assert.Equal("run", tree.ActiveClip);
    }

    [Fact]
    public void RemoveNode_ByThreshold_ReturnsFalse_WhenNotFound()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector1D(animator).AddNode(0f, "idle");

        Assert.False(tree.RemoveNode(9f));
    }

    [Fact]
    public void RemoveNode_ByClipName_RemovesNode()
    {
        var animator = MakeAnimator("walk", "run");
        var tree = new AnimationBlendSelector1D(animator)
            .AddNode(0f, "walk")
            .AddNode(1f, "run");

        var removed = tree.RemoveNode("walk");

        Assert.True(removed);
        tree.Value = 0f;
        Assert.Equal("run", tree.ActiveClip);
    }

    [Fact]
    public void RemoveNode_ByClipName_ReturnsFalse_WhenNotFound()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector1D(animator).AddNode(0f, "idle");

        Assert.False(tree.RemoveNode("missing"));
    }

    [Fact]
    public void RemoveLastNode_ResetsActiveClip()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector1D(animator).AddNode(0f, "idle");
        tree.Value = 0f;

        tree.RemoveNode("idle");

        Assert.Null(tree.ActiveClip);
    }

    [Fact]
    public void RemoveNode_ByThreshold_UsesEpsilonComparison()
    {
        var animator = MakeAnimator("walk", "run");
        var tree = new AnimationBlendSelector1D(animator);
        float threshold = 0.1f + 0.2f;
        tree.AddNode(threshold, "walk");
        tree.AddNode(1f, "run");

        bool removed = tree.RemoveNode(0.3f);

        Assert.True(removed);
        tree.AddNode(0.3f, "walk");
        tree.Value = 0.1f;
        Assert.Equal("walk", tree.ActiveClip);
    }

    [Fact]
    public void OnceClip_RestartsAfterFinishing()
    {
        var clip = MakeClip("attack", 3, PlaybackMode.OnceHoldLast, 0.1f);
        var animator = MakeAnimator(clip);
        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "attack");

        tree.Value = 0f;
        Assert.True(animator.IsPlaying);

        animator.Update(1f);
        Assert.True(animator.IsFinished);

        tree.Value = 0f;
        Assert.True(animator.IsPlaying, "Blend tree must restart a finished Once clip.");
    }

    [Fact]
    public void LoopClip_DoesNotRestartWhilePlaying()
    {
        var clip = MakeClip("walk", 3, PlaybackMode.Loop, 0.1f);
        var animator = MakeAnimator(clip);
        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "walk");

        tree.Value = 0f;
        animator.Update(0.05f);
        var frameIndex = animator.CurrentFrameIndex;

        tree.Value = 0f;
        Assert.Equal(frameIndex, animator.CurrentFrameIndex);
    }

    [Fact]
    public void CrossFadeDuration_DefaultIsZero()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector1D(animator);
        Assert.Equal(0f, tree.CrossFadeDuration);
    }

    [Fact]
    public void CrossFadeDuration_Zero_HardCutsOnNodeChange()
    {
        var idle = MakeClip("idle", 2, PlaybackMode.Loop);
        var walk = MakeClip("walk", 2, PlaybackMode.Loop);
        var animator = MakeAnimator(idle, walk);
        var tree = new AnimationBlendSelector1D(animator);
        tree.CrossFadeDuration = 0f;
        tree.AddNode(0f, "idle").AddNode(1f, "walk");

        tree.Value = 0f;
        Assert.Equal("idle", animator.CurrentAnimation?.Name);

        tree.Value = 1f;
        Assert.Equal("walk", animator.CurrentAnimation?.Name);
        Assert.Equal(1f, animator.CrossFadeAlpha);
    }

    [Fact]
    public void CrossFadeDuration_Positive_StartsFadeOnNodeChange()
    {
        var idle = MakeClip("idle", 2, PlaybackMode.Loop);
        var walk = MakeClip("walk", 2, PlaybackMode.Loop);
        var animator = MakeAnimator(idle, walk);
        var tree = new AnimationBlendSelector1D(animator);
        tree.CrossFadeDuration = 0.3f;
        tree.AddNode(0f, "idle").AddNode(1f, "walk");

        tree.Value = 0f;
        tree.Value = 1f;

        Assert.Equal("walk", animator.CurrentAnimation?.Name);
        Assert.True(animator.CrossFadeAlpha < 1f, "Cross-fade should be in progress after node change");
        Assert.NotNull(animator.CrossFadeOutgoingClip);
        Assert.Equal("idle", animator.CrossFadeOutgoingClip!.Name);
    }

    [Fact]
    public void CrossFadeDuration_NoFade_WhenSameNode()
    {
        var idle = MakeClip("idle", 2, PlaybackMode.Loop);
        var walk = MakeClip("walk", 2, PlaybackMode.Loop);
        var animator = MakeAnimator(idle, walk);
        var tree = new AnimationBlendSelector1D(animator);
        tree.CrossFadeDuration = 0.3f;
        tree.AddNode(0f, "idle").AddNode(1f, "walk");

        tree.Value = 0f;
        Assert.Equal(1f, animator.CrossFadeAlpha);

        tree.Value = 0.1f;
        Assert.Equal(1f, animator.CrossFadeAlpha);
    }

    [Fact]
    public void CrossFadeDuration_NoFade_WhenAnimatorNotPlaying()
    {
        var idle = MakeClip("idle", 2, PlaybackMode.Loop);
        var walk = MakeClip("walk", 2, PlaybackMode.Loop);
        var animator = MakeAnimator(idle, walk);
        var tree = new AnimationBlendSelector1D(animator) { IsEnabled = false };
        tree.CrossFadeDuration = 0.3f;
        tree.AddNode(0f, "idle").AddNode(1f, "walk");

        tree.IsEnabled = true;
        tree.Value = 1f;

        Assert.Equal("walk", animator.CurrentAnimation?.Name);
        Assert.Equal(1f, animator.CrossFadeAlpha);
    }

    [Fact]
    public void CrossFadeDuration_RestartFinishedClip_NoCrossFade()
    {
        var attack = MakeClip("attack", 2, PlaybackMode.OnceHoldLast, 0.1f);
        var animator = MakeAnimator(attack);
        var tree = new AnimationBlendSelector1D(animator);
        tree.CrossFadeDuration = 0.3f;
        tree.AddNode(0f, "attack");

        tree.Value = 0f;
        animator.Update(attack.TotalDuration + 0.1f);
        Assert.True(animator.IsFinished);

        tree.Value = 0f;

        Assert.True(animator.IsPlaying);
        Assert.Equal(1f, animator.CrossFadeAlpha);
    }

    [Fact]
    public void ValidateNodes_ReturnsEmpty_WhenAllClipsRegistered()
    {
        var animator = MakeAnimator("idle", "walk", "run");
        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "idle");
        tree.AddNode(1f, "walk");
        tree.AddNode(2f, "run");

        var issues = tree.ValidateNodes();

        issues.Should().BeEmpty("all node clip names are registered on the animator");
    }

    [Fact]
    public void ValidateNodes_ReturnsIssue_WhenClipNotRegistered()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "idle");
        tree.AddNode(1f, "typo_walk");

        var issues = tree.ValidateNodes();

        issues.Should().HaveCount(1);
        issues[0].Should().Contain("typo_walk");
    }

    [Fact]
    public void ValidateNodes_ReturnsEmpty_WhenNoNodesAdded()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector1D(animator);

        var issues = tree.ValidateNodes();

        issues.Should().BeEmpty();
    }

    [Fact]
    public void ValidateNodes_ReturnsMultipleIssues_ForMultipleMissingClips()
    {
        var animator = MakeAnimator("idle");
        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "idle");
        tree.AddNode(1f, "missing_a");
        tree.AddNode(2f, "missing_b");

        var issues = tree.ValidateNodes();

        issues.Should().HaveCount(2);
        issues.Should().Contain(i => i.Contains("missing_a"));
        issues.Should().Contain(i => i.Contains("missing_b"));
    }

    [Fact]
    public void IsEnabled_False_PreventsEvaluate_FromChangingClip()
    {
        var animator = MakeAnimator("idle", "run");
        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "idle");
        tree.AddNode(5f, "run");
        tree.Value = 0f;
        tree.Evaluate();

        tree.IsEnabled = false;
        tree.Value = 5f;
        tree.Evaluate();

        animator.CurrentAnimation!.Name.Should().Be("idle",
            "Evaluate is a no-op while IsEnabled is false");
    }

    [Fact]
    public void IsEnabled_True_AfterDisable_ResumesEvaluation()
    {
        var animator = MakeAnimator("idle", "run");
        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "idle");
        tree.AddNode(5f, "run");
        tree.Value = 0f;
        tree.Evaluate();

        tree.IsEnabled = false;
        tree.Value = 5f;
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

        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "idle");
        tree.Value = 0f;
        tree.Evaluate();

        animator.Play("attack", restart: true);
        tree.Evaluate();
        animator.CurrentAnimation!.Name.Should().Be("attack", "tree must not interrupt an external non-looping clip");

        animator.Update(0.15f);
        animator.IsFinished.Should().BeTrue();

        tree.Evaluate();
        animator.CurrentAnimation!.Name.Should().Be("idle", "tree must re-engage once the external clip is done");
    }

    [Fact]
    public void DirtyNotClearedWhenYielding_RetriedNextTick()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle", 2, PlaybackMode.Loop));
        animator.AddAnimation(new AnimationClip("attack") { PlaybackMode = PlaybackMode.OnceHoldLast }
            .AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.5f)));

        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "idle");
        tree.Value = 0f;
        tree.Evaluate();

        animator.Play("attack", restart: true);

        tree.Value = 0f;
        tree.Evaluate();
        animator.CurrentAnimation!.Name.Should().Be("attack");

        animator.Update(0.6f);
        tree.Evaluate();
        animator.CurrentAnimation!.Name.Should().Be("idle",
            "dirty state must be retained while yielding so the tree re-engages");
    }

    [Fact]
    public void SpeedNotAppliedToExternalClipWhileYielding()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle", 2, PlaybackMode.Loop));
        animator.AddAnimation(new AnimationClip("attack") { PlaybackMode = PlaybackMode.OnceHoldLast }
            .AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f)));

        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "idle", speed: 2f);
        tree.Value = 0f;
        tree.Evaluate();

        animator.Play("attack", restart: true);
        animator.Speed = 1f;

        tree.Evaluate();

        animator.Speed.Should().Be(1f, "speed must not be overwritten while the tree is yielding");
    }

    [Fact]
    public void RespectNonLoopingFalse_InterruptsExternalClip()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle", 2, PlaybackMode.Loop));
        animator.AddAnimation(new AnimationClip("attack") { PlaybackMode = PlaybackMode.OnceHoldLast }
            .AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.5f)));

        var tree = new AnimationBlendSelector1D(animator) { RespectNonLoopingClips = false };
        tree.AddNode(0f, "idle");
        tree.Value = 0f;

        animator.Play("attack", restart: true);
        tree.Value = 0f;

        animator.CurrentAnimation!.Name.Should().Be("idle",
            "with RespectNonLoopingClips=false the tree should interrupt the external clip");
    }

    [Fact]
    public void OnClipChanged_FiresOnTransition()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle", 2, PlaybackMode.Loop));
        animator.AddAnimation(MakeClip("walk", 2, PlaybackMode.Loop));

        var tree = new AnimationBlendSelector1D(animator);
        tree.AddNode(0f, "idle");
        tree.AddNode(1f, "walk");

        string? prevClip = null;
        string? newClip = null;
        tree.OnClipChanged += (p, n) => { prevClip = p; newClip = n; };

        tree.Value = 0f;
        tree.Evaluate();
        tree.Value = 1f;
        tree.Evaluate();

        prevClip.Should().Be("idle");
        newClip.Should().Be("walk");
    }
}