using Brine2D.Animation;
using Brine2D.Core;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Animation;

public class SpriteAnimatorTests
{
    private static AnimationClip MakeClip(string name, int frames = 3, float duration = 0.1f, bool loop = true)
    {
        var clip = new AnimationClip(name) { Loop = loop };
        for (int i = 0; i < frames; i++)
            clip.AddFrame(new SpriteFrame(new Rectangle(i * 16, 0, 16, 16), duration));
        return clip;
    }

    private static AnimationClip MakePingPongClip(string name, int frames = 3, float duration = 0.1f)
    {
        var clip = new AnimationClip(name) { PlaybackMode = PlaybackMode.PingPong };
        for (int i = 0; i < frames; i++)
            clip.AddFrame(new SpriteFrame(new Rectangle(i * 16, 0, 16, 16), duration));
        return clip;
    }

    [Fact]
    public void Play_SetsCurrentAnimationAndIsPlaying()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));

        animator.Play("idle");

        animator.CurrentAnimation!.Name.Should().Be("idle");
        animator.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void Play_UnknownName_DoesNothing()
    {
        var animator = new SpriteAnimator();

        animator.Play("missing");

        animator.CurrentAnimation.Should().BeNull();
        animator.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void Play_SameClipWithoutRestart_ResumesWithoutRestart()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));
        animator.Play("idle");
        animator.Update(0.15f);

        animator.Play("idle");

        animator.CurrentFrame.Should().Be(animator.CurrentAnimation!.Frames[1]);
    }

    [Fact]
    public void Play_SameClipWithRestart_RestartsFromZero()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));
        animator.Play("idle");
        animator.Update(0.15f);

        animator.Play("idle", restart: true);

        animator.CurrentFrame.Should().Be(animator.CurrentAnimation!.Frames[0]);
    }

    [Fact]
    public void Pause_StopsAdvancing()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));
        animator.Play("idle");
        animator.Pause();

        animator.Update(1f);

        animator.CurrentFrame.Should().Be(animator.CurrentAnimation!.Frames[0]);
    }

    [Fact]
    public void Resume_ContinuesAfterPause()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));
        animator.Play("idle");
        animator.Pause();
        animator.Resume();

        animator.Update(0.15f);

        animator.CurrentFrame.Should().Be(animator.CurrentAnimation!.Frames[1]);
    }

    [Fact]
    public void Stop_ClearsCurrentAnimation()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));
        animator.Play("idle");

        animator.Stop();

        animator.CurrentAnimation.Should().BeNull();
        animator.CurrentFrame.Should().BeNull();
        animator.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void Stop_WithFireCallbacks_InvokesOnExit()
    {
        bool frameFired = false;
        bool clipFired = false;
        var animator = new SpriteAnimator();
        var clip = MakeClip("idle");
        clip.OnExit += () => clipFired = true;
        clip.Frames[0].OnExit += () => frameFired = true;
        animator.AddAnimation(clip);
        animator.Play("idle");

        animator.Stop(fireCallbacks: true);

        frameFired.Should().BeTrue();
        clipFired.Should().BeTrue();
    }

    [Fact]
    public void NonLoopingClip_StopsAfterLastFrame()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("attack", frames: 2, duration: 0.1f, loop: false));
        animator.Play("attack");

        animator.Update(0.25f);

        animator.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void NonLoopingClip_FiresOnAnimationComplete()
    {
        AnimationClip? completed = null;
        var animator = new SpriteAnimator();
        animator.OnAnimationComplete += c => completed = c;
        animator.AddAnimation(MakeClip("attack", frames: 2, duration: 0.1f, loop: false));
        animator.Play("attack");

        animator.Update(0.25f);

        completed.Should().NotBeNull();
        completed!.Name.Should().Be("attack");
    }

    [Fact]
    public void LoopingClip_FiresOnLoopComplete()
    {
        AnimationClip? looped = null;
        var animator = new SpriteAnimator();
        animator.OnLoopComplete += c => looped = c;
        animator.AddAnimation(MakeClip("idle", frames: 2, duration: 0.1f));
        animator.Play("idle");

        animator.Update(0.25f);

        looped.Should().NotBeNull();
    }

    [Fact]
    public void Speed_BelowOrEqualZero_ClampsToZero()
    {
        var animator = new SpriteAnimator();

        animator.Speed = 0f;
        animator.Speed.Should().Be(0f, "zero is clamped, not rejected");

        animator.Speed = -1f;
        animator.Speed.Should().Be(0f, "negative values are clamped to zero");
    }

    [Fact]
    public void Speed_ScalesPlayback()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle", frames: 3, duration: 0.1f));
        animator.Play("idle");
        animator.Speed = 2f;

        animator.Update(0.1f);

        animator.CurrentFrame.Should().Be(animator.CurrentAnimation!.Frames[2]);
    }

    [Fact]
    public void AnimationNames_ReturnsAllRegisteredNames()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));
        animator.AddAnimation(MakeClip("walk"));

        animator.AnimationNames.Should().BeEquivalentTo(["idle", "walk"]);
    }

    [Fact]
    public void SeekToTime_JumpsToCorrectFrame()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle", frames: 3, duration: 0.1f));
        animator.Play("idle");

        animator.SeekToTime(0.25f);

        animator.CurrentFrame.Should().Be(animator.CurrentAnimation!.Frames[2]);
    }

    [Fact]
    public void SetFrame_JumpsToSpecifiedFrame()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle", frames: 3, duration: 0.1f));
        animator.Play("idle");

        animator.SetFrame(2);

        animator.CurrentFrame.Should().Be(animator.CurrentAnimation!.Frames[2]);
    }

    [Fact]
    public void HasAnimation_ReturnsTrueForAdded()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));

        animator.HasAnimation("idle").Should().BeTrue();
        animator.HasAnimation("walk").Should().BeFalse();
    }

    [Fact]
    public void RemoveAnimation_WhenActive_StopsAnimator()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));
        animator.Play("idle");

        animator.RemoveAnimation("idle");

        animator.CurrentAnimation.Should().BeNull();
        animator.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void SeekToTime_LargeValue_LoopingClip_DoesNotBreakSubsequentUpdate()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("walk", frames: 3, duration: 0.1f));
        animator.Play("walk");

        animator.SeekToTime(100f);

        var ex = Record.Exception(() => animator.Update(0.05f));
        ex.Should().BeNull();
        animator.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void SeekToTime_LargeValue_OnceClip_ClampsToLastFrame()
    {
        var animator = new SpriteAnimator();
        var clip = MakeClip("attack", frames: 3, duration: 0.1f, loop: false);
        animator.AddAnimation(clip);
        animator.Play("attack");

        animator.SeekToTime(999f);

        animator.CurrentFrameIndex.Should().Be(clip.Frames.Count - 1);
    }

    [Fact]
    public void PlayWithCrossFade_AlphaRampsFromZeroToOne()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));
        animator.AddAnimation(MakeClip("walk"));
        animator.Play("idle");

        animator.PlayWithCrossFade("walk", fadeDuration: 0.2f);
        animator.CrossFadeAlpha.Should().Be(0f);

        animator.Update(0.1f);
        animator.CrossFadeAlpha.Should().BeGreaterThan(0f).And.BeLessThan(1f);

        animator.Update(0.1f);
        animator.CrossFadeAlpha.Should().Be(1f);
        animator.CrossFadeOutgoingClip.Should().BeNull();
        animator.CrossFadeOutgoingFrame.Should().BeNull();
    }

    [Fact]
    public void PlayWithCrossFade_CapturesOutgoingFrame()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));
        animator.AddAnimation(MakeClip("walk"));
        animator.Play("idle");

        var expectedFrame = animator.CurrentFrame;
        animator.PlayWithCrossFade("walk", fadeDuration: 0.5f);

        animator.CrossFadeOutgoingFrame.Should().BeSameAs(expectedFrame);
    }

    [Fact]
    public void PlayWithCrossFade_ThrowsOnNonPositiveDuration()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));
        animator.AddAnimation(MakeClip("walk"));
        animator.Play("idle");

        var act = () => animator.PlayWithCrossFade("walk", 0f);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PlayWithCrossFade_DoesNotStartFade_WhenAnimationNotFound()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));
        animator.Play("idle");
        animator.PlayWithCrossFade("nonexistent", 0.3f, restart: true);

        animator.CrossFadeAlpha.Should().Be(1f);
        animator.CrossFadeOutgoingFrame.Should().BeNull();
    }

    [Fact]
    public void PlayWithCrossFade_DoesNotStartFade_WhenSameClipAndNoRestart()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));
        animator.Play("idle");

        animator.PlayWithCrossFade("idle", 0.3f, restart: false);

        animator.CrossFadeAlpha.Should().Be(1f);
    }

    [Fact]
    public void CurrentTime_IsCached_AndReturnsSameValueOnRepeatedAccess()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("walk"));
        animator.Play("walk");

        var t1 = animator.CurrentTime;
        var t2 = animator.CurrentTime;

        t1.Should().Be(t2);
        t1.Should().Be(0f);
    }

    [Fact]
    public void CurrentTime_UpdatesAfterAdvance()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("walk", duration: 0.2f));
        animator.Play("walk");

        animator.Update(0.05f);

        animator.CurrentTime.Should().BeApproximately(0.05f, 0.0001f);
    }

    [Fact]
    public void ClipEvent_ReceivesCorrectArgs()
    {
        var animator = new SpriteAnimator();
        var clip = MakeClip("walk", frames: 4, duration: 0.1f);
        ClipEventArgs? received = null;
        clip.AddEvent("step", 0.15f, args => received = args);
        animator.AddAnimation(clip);
        animator.Play("walk");

        animator.Update(0.2f);

        received.Should().NotBeNull();
        received!.EventName.Should().Be("step");
        received.ClipName.Should().Be("walk");
        received.Time.Should().BeApproximately(0.15f, 0.001f);
        received.NormalizedTime.Should().BeApproximately(0.15f / clip.TotalDuration, 0.001f);
    }

    [Fact]
    public void PingPong_ClipEvent_DoesNotFireOnBackwardSwing_ByDefault()
    {
        var animator = new SpriteAnimator();
        var clip = new AnimationClip("pp") { PlaybackMode = PlaybackMode.PingPong };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        clip.AddFrame(new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.1f));
        clip.AddFrame(new SpriteFrame(new Rectangle(32, 0, 16, 16), 0.1f));

        int fireCount = 0;
        clip.AddEvent("tick", 0.05f, _ => fireCount++, fireBothDirections: false);
        animator.AddAnimation(clip);
        animator.Play("pp");

        animator.Update(0.35f);

        fireCount.Should().Be(1);
    }

    [Fact]
    public void PingPong_ClipEvent_FiresOnBackwardSwing_WhenFlagSet()
    {
        var animator = new SpriteAnimator();
        var clip = new AnimationClip("pp") { PlaybackMode = PlaybackMode.PingPong };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        clip.AddFrame(new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.1f));
        clip.AddFrame(new SpriteFrame(new Rectangle(32, 0, 16, 16), 0.1f));

        int fireCount = 0;
        clip.AddEvent("tick", 0.05f, _ => fireCount++, fireBothDirections: true);
        animator.AddAnimation(clip);
        animator.Play("pp");

        animator.Update(0.35f);

        fireCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void ClipEvent_CurrentFrame_IsAccurate_InsideCallback()
    {
        var animator = new SpriteAnimator();
        var clip = MakeClip("walk", frames: 4, duration: 0.1f);
        SpriteFrame? frameAtEvent = null;
        clip.AddEvent("step", 0.15f, _ => frameAtEvent = animator.CurrentFrame);
        animator.AddAnimation(clip);
        animator.Play("walk");

        animator.Update(0.16f);

        frameAtEvent.Should().NotBeNull();
        frameAtEvent.Should().Be(clip.Frames[1]);
    }

    [Fact]
    public void SeekToTime_PingPong_SetsCorrectDirection_ForwardHalf()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakePingPongClip("pp", frames: 3, duration: 0.1f));
        animator.Play("pp");

        animator.SeekToTime(0.1f);
        animator.Update(0.15f);

        animator.CurrentFrameIndex.Should().Be(2);
    }

    [Fact]
    public void SeekToTime_PingPong_SetsCorrectDirection_BackwardHalf()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakePingPongClip("pp", frames: 3, duration: 0.1f));
        animator.Play("pp");

        animator.SeekToTime(0.35f);
        animator.Update(0.15f);

        animator.CurrentFrameIndex.Should().BeLessThan(2);
    }

    [Fact]
    public void PlayQueued_PlaysImmediately_WhenNothingIsPlaying()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("idle"));

        animator.PlayQueued("idle");

        animator.CurrentAnimation!.Name.Should().Be("idle");
        animator.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void PlayQueued_StartsAfterCurrentNonLoopingClipFinishes()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("attack", frames: 2, duration: 0.1f, loop: false));
        animator.AddAnimation(MakeClip("idle"));
        animator.Play("attack");

        animator.PlayQueued("idle");
        animator.QueuedAnimation.Should().Be("idle");

        animator.Update(0.25f);

        animator.CurrentAnimation!.Name.Should().Be("idle");
        animator.QueuedAnimation.Should().BeNull();
    }

    [Fact]
    public void PlayQueued_SecondCall_AppendsToQueue()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("attack", frames: 2, duration: 0.1f, loop: false));
        animator.AddAnimation(MakeClip("idle"));
        animator.AddAnimation(MakeClip("walk"));
        animator.Play("attack");

        animator.PlayQueued("idle");
        animator.PlayQueued("walk");

        animator.Update(0.25f);

        animator.CurrentAnimation!.Name.Should().Be("idle",
            "PlayQueued appends to a FIFO queue; the first queued clip plays after attack completes");
    }

    [Fact]
    public void CancelQueued_ClearsQueue()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("attack", frames: 2, duration: 0.1f, loop: false));
        animator.AddAnimation(MakeClip("idle"));
        animator.Play("attack");

        animator.PlayQueued("idle");
        animator.CancelQueued();

        animator.Update(0.25f);

        animator.IsFinished.Should().BeTrue("attack clip played to completion");
        animator.IsPlaying.Should().BeFalse("nothing in the queue to continue with");
        animator.CurrentAnimation!.Name.Should().Be("attack",
            "Once clip stays as CurrentAnimation after finishing; no queued clip should have started");
    }

    [Fact]
    public void Stop_ClearsQueuedAnimation()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("attack", frames: 2, duration: 0.1f, loop: false));
        animator.AddAnimation(MakeClip("idle"));
        animator.Play("attack");
        animator.PlayQueued("idle");

        animator.Stop();

        animator.QueuedAnimation.Should().BeNull();
    }

    [Fact]
    public void RemoveAnimation_ClearsQueuedAnimation_WhenQueuedNameMatches()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("attack", frames: 2, duration: 0.1f, loop: false));
        animator.AddAnimation(MakeClip("idle"));
        animator.Play("attack");
        animator.PlayQueued("idle");

        animator.RemoveAnimation("idle");

        animator.QueuedAnimation.Should().BeNull();
    }

    [Fact]
    public void SeekToTime_WithFireEvents_FiresEventsInWindow()
    {
        var animator = new SpriteAnimator();
        var clip = MakeClip("walk", frames: 4, duration: 0.1f);
        var fired = new List<string>();
        clip.AddEvent("a", 0.05f, _ => fired.Add("a"));
        clip.AddEvent("b", 0.15f, _ => fired.Add("b"));
        clip.AddEvent("c", 0.25f, _ => fired.Add("c"));
        animator.AddAnimation(clip);
        animator.Play("walk");

        animator.SeekToTime(0.2f, fireEvents: true);

        fired.Should().BeEquivalentTo(["a", "b"], because: "events at 0.05 and 0.15 fall in (0, 0.2)");
        fired.Should().NotContain("c", because: "event at 0.25 is beyond the seek target");
    }

    [Fact]
    public void SeekToTime_WithoutFireEvents_DoesNotFireEvents()
    {
        var animator = new SpriteAnimator();
        var clip = MakeClip("walk", frames: 4, duration: 0.1f);
        int fireCount = 0;
        clip.AddEvent("a", 0.05f, _ => fireCount++);
        animator.AddAnimation(clip);
        animator.Play("walk");

        animator.SeekToTime(0.2f);

        fireCount.Should().Be(0);
    }

    [Fact]
    public void SpriteFrame_UserData_AccessibleFromClipEventCallback()
    {
        var animator = new SpriteAnimator();
        var clip = MakeClip("attack", frames: 3, duration: 0.1f, loop: false);
        clip.Frames[1].UserData = "damage_frame";
        object? capturedData = null;
        clip.AddEvent("hit", 0.15f, _ => capturedData = animator.CurrentFrame?.UserData);
        animator.AddAnimation(clip);
        animator.Play("attack");

        // 0.16s lands the animator on frame 1 (elapsed 0.1–0.2s) when the event at 0.15s fires.
        // Using 0.2s would advance to frame 2 before FireClipEvents runs.
        animator.Update(0.16f);

        capturedData.Should().Be("damage_frame");
    }

    [Fact]
    public void SetFrame_DoesNotFireEventsFromBeforeJumpPosition()
    {
        var animator = new SpriteAnimator();
        var clip = MakeClip("test", frames: 3, duration: 0.1f, loop: false);
        int earlyFired = 0;
        clip.AddEvent("early", 0.05f, _ => earlyFired++);
        animator.AddAnimation(clip);
        animator.Play("test");

        animator.SetFrame(2);
        animator.Update(0.05f);

        earlyFired.Should().Be(0,
            "event at 0.05s is before frame 2's start of 0.2s and must not re-fire after a SetFrame jump");
    }

    [Fact]
    public void SetFrame_FiresEventsAfterJumpPosition()
    {
        var animator = new SpriteAnimator();
        var clip = MakeClip("test", frames: 3, duration: 0.1f, loop: false);
        int lateFired = 0;
        clip.AddEvent("late", 0.25f, _ => lateFired++);
        animator.AddAnimation(clip);
        animator.Play("test");

        animator.SetFrame(2);
        animator.Update(0.1f);

        lateFired.Should().Be(1,
            "event at 0.25s falls within the (0.2, 0.3] window after SetFrame(2)");
    }

    [Fact]
    public void SetFrame_CurrentFrameIndex_ReflectsNewFrame()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("test", frames: 4, duration: 0.1f));
        animator.Play("test");

        animator.SetFrame(3);

        animator.CurrentFrameIndex.Should().Be(3);
    }

    [Fact]
    public void PlayQueued_BehindLoopingClip_DropsQueue()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("loop", frames: 2, duration: 0.1f, loop: true));
        animator.AddAnimation(MakeClip("next", frames: 2, duration: 0.1f, loop: false));
        animator.Play("loop");

        animator.PlayQueued("next");

        animator.QueuedAnimation.Should().BeNull(
            "queuing behind a Loop animation that never completes naturally is a no-op");
    }

    [Fact]
    public void PlayQueued_BehindPingPongClip_DropsQueue()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakePingPongClip("pp", frames: 3, duration: 0.1f));
        animator.AddAnimation(MakeClip("next", frames: 2, duration: 0.1f, loop: false));
        animator.Play("pp");

        animator.PlayQueued("next");

        animator.QueuedAnimation.Should().BeNull(
            "queuing behind a PingPong animation that never completes naturally is a no-op");
    }

    [Fact]
    public void SeekToTime_PingPong_ForwardPass_ResolvesCorrectFrame()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakePingPongClip("pp", frames: 3, duration: 0.1f));
        animator.Play("pp");

        animator.SeekToTime(0.15f);

        animator.CurrentFrameIndex.Should().Be(1,
            "forward pass at 0.15s into a 0.1s-per-frame clip lands on frame 1");
    }

    [Fact]
    public void SeekToTime_PingPong_BackwardPass_ResolvesCorrectMirroredFrame()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakePingPongClip("pp", frames: 3, duration: 0.1f));
        animator.Play("pp");

        animator.SeekToTime(0.4f);

        animator.CurrentFrameIndex.Should().Be(2,
            "backward pass 0.1s into the sweep mirrors to forward position 0.2s = frame 2");
    }

    [Fact]
    public void SeekToTime_PingPong_BackwardPassAtStart_ResolvesLastFrame()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakePingPongClip("pp", frames: 3, duration: 0.1f));
        animator.Play("pp");

        // 0.3s == total: posInCycle=0.3, backward pass, frameResolveTime = total - 0 = 0.3.
        // ResolveTimeToFrame clamps to [0, total], which places us at the last frame (index 2).
        animator.SeekToTime(0.3f);

        animator.CurrentFrameIndex.Should().Be(2,
            "the backward pass starts at the last frame (frame 2)");
    }

    [Fact]
    public void PingPong_ClipEvent_NearTotalDuration_FiresOnForwardPass()
    {
        var clip = MakePingPongClip("pp", frames: 3, duration: 0.1f);
        bool eventFired = false;
        clip.AddEvent("near_end", 0.28f, _ => eventFired = true);

        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Play("pp");

        animator.Update(0.35f);

        eventFired.Should().BeTrue(
            "ClipEvent at 0.28s must fire on the forward pass; regression if _clipTime was modded at each flip");
    }

    [Fact]
    public void PingPong_OnLoopComplete_FiredAtBothDirectionFlips()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakePingPongClip("pp", frames: 3, duration: 0.1f));
        int flips = 0;
        animator.OnLoopComplete += _ => flips++;
        animator.Play("pp");

        animator.Update(0.65f);

        flips.Should().Be(2, "one flip at the end of the forward pass, one at the end of the backward pass");
    }

    [Fact]
    public void PingPong_SeekToTime_ThenUpdate_EventStillFires()
    {
        var clip = MakePingPongClip("pp", frames: 3, duration: 0.1f);
        bool eventFired = false;
        clip.AddEvent("near_end", 0.28f, _ => eventFired = true);

        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Play("pp");

        animator.SeekToTime(0.2f);
        animator.Update(0.12f);

        eventFired.Should().BeTrue(
            "event at 0.28s must fire after SeekToTime(0.2) + Update(0.12); " +
            "regression if SeekToTime stored _clipTime in [0,total] instead of cycle space");
    }

    [Fact]
    public void OnceStop_StopCalledInOnAnimationComplete_OnStoppedFiresOnce()
    {
        var clip = new AnimationClip("vfx") { PlaybackMode = PlaybackMode.OnceStop };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        clip.AddFrame(new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.1f));
        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Play("vfx");

        int stoppedCount = 0;
        animator.OnStopped += _ => stoppedCount++;
        animator.OnAnimationComplete += _ => animator.Stop();

        animator.Update(1f);

        stoppedCount.Should().Be(1);
    }

    [Fact]
    public void OnceStop_NoReentrantStop_OnStoppedFiresOnce()
    {
        var clip = new AnimationClip("vfx") { PlaybackMode = PlaybackMode.OnceStop };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        clip.AddFrame(new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.1f));
        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Play("vfx");

        int stoppedCount = 0;
        animator.OnStopped += _ => stoppedCount++;

        animator.Update(1f);

        stoppedCount.Should().Be(1);
    }

    [Fact]
    public void ReversedLoopClip_ClipEvents_FireForAllEvents()
    {
        var clip = MakeClip("run", frames: 3, duration: 0.1f);
        int fired = 0;
        clip.AddEvent("step", 0.05f, _ => fired++);

        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Reversed = true;
        animator.Play("run");

        animator.Update(clip.TotalDuration - 0.03f);

        fired.Should().BeGreaterThan(0, "events must fire on reversed loop clips");
    }

    [Fact]
    public void Loop_Reversed_EventWithFireBothDirectionsFalse_StillFires()
    {
        var clip = MakeClip("run", frames: 2, duration: 0.1f);
        int fired = 0;
        clip.AddEvent("hit", 0.05f, _ => fired++, fireBothDirections: false);

        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Reversed = true;
        animator.Play("run");
        animator.Update(clip.TotalDuration - 0.01f);

        fired.Should().BeGreaterThan(0,
            "fireBothDirections:false gates PingPong backward swings, not reversed Loop playback");
    }

    [Fact]
    public void Loop_Reversed_EventWithFireBothDirectionsTrue_DoesFire()
    {
        var clip = MakeClip("run", frames: 4, duration: 0.1f);
        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);

        int fired = 0;
        clip.AddEvent("step", 0.15f, _ => fired++, fireBothDirections: true);

        animator.Reversed = true;
        animator.Play("run");
        animator.Update(0.2f);

        fired.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ForceResetCurrentAnimation_FiresOnExitOnDepartingFrame_AndOnEnterOnFirstFrame()
    {
        var clip = new AnimationClip("walk") { PlaybackMode = PlaybackMode.Loop };
        var f0 = new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f);
        var f1 = new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.1f);
        var f2 = new SpriteFrame(new Rectangle(32, 0, 16, 16), 0.1f);
        clip.AddFrame(f0).AddFrame(f1).AddFrame(f2);

        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Play("walk");
        animator.Update(0.15f);

        animator.CurrentFrameIndex.Should().Be(1);

        int f1ExitCount = 0, f0EnterCount = 0, frameChangedCount = 0;
        SpriteFrame? lastChangedFrame = null;
        f1.OnExit += () => f1ExitCount++;
        f0.OnEnter += () => f0EnterCount++;
        animator.OnFrameChanged += f => { frameChangedCount++; lastChangedFrame = f; };

        animator.ForceResetCurrentAnimation();

        animator.CurrentFrameIndex.Should().Be(0);
        f1ExitCount.Should().Be(1);
        f0EnterCount.Should().Be(1);
        frameChangedCount.Should().Be(1);
        lastChangedFrame.Should().Be(f0);
    }

    [Fact]
    public void ForceResetCurrentAnimationToEnd_FiresOnExitOnDepartingFrame_AndOnEnterOnLastFrame()
    {
        var clip = new AnimationClip("walk") { PlaybackMode = PlaybackMode.Loop };
        var f0 = new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f);
        var f1 = new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.1f);
        var f2 = new SpriteFrame(new Rectangle(32, 0, 16, 16), 0.1f);
        clip.AddFrame(f0).AddFrame(f1).AddFrame(f2);

        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Play("walk");

        int f0ExitCount = 0, f2EnterCount = 0, frameChangedCount = 0;
        SpriteFrame? lastChangedFrame = null;
        f0.OnExit += () => f0ExitCount++;
        f2.OnEnter += () => f2EnterCount++;
        animator.OnFrameChanged += f => { frameChangedCount++; lastChangedFrame = f; };

        animator.ForceResetCurrentAnimationToEnd();

        animator.CurrentFrameIndex.Should().Be(2);
        f0ExitCount.Should().Be(1);
        f2EnterCount.Should().Be(1);
        frameChangedCount.Should().Be(1);
        lastChangedFrame.Should().Be(f2);
    }

    [Fact]
    public void ForceResetCurrentAnimation_WhenAlreadyOnFirstFrame_IsNoOp()
    {
        var clip = new AnimationClip("idle") { PlaybackMode = PlaybackMode.Loop };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        clip.AddFrame(new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.1f));

        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Play("idle");

        int frameChangedCount = 0;
        animator.OnFrameChanged += _ => frameChangedCount++;

        animator.ForceResetCurrentAnimation();

        frameChangedCount.Should().Be(0, "already on frame 0 — no frame change expected");
    }

    [Fact]
    public void Update_HugeDelta_DoesNotHang_AndAnimatorRemainsUsable()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("walk", frames: 4, duration: 0.001f));
        animator.Play("walk");

        var ex = Record.Exception(() => animator.Update(10f));

        ex.Should().BeNull();
        animator.CurrentAnimation.Should().NotBeNull();
    }

    [Fact]
    public void Update_HugeDelta_OnceClip_CompletesCleanly()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("hit", frames: 4, duration: 0.001f, loop: false));
        animator.Play("hit");

        var ex = Record.Exception(() => animator.Update(10f));

        ex.Should().BeNull();
        animator.IsFinished.Should().BeTrue();
    }

    [Fact]
    public void MaxQueueDepth_Exceeded_DropsEntry()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("attack", frames: 2, duration: 0.1f, loop: false));
        animator.AddAnimation(MakeClip("idle"));
        animator.MaxQueueDepth = 2;
        animator.Play("attack");

        animator.PlayQueued("idle");
        animator.PlayQueued("idle");
        animator.PlayQueued("idle");

        animator.AnimationQueueCount.Should().Be(2,
            "the third enqueue exceeds MaxQueueDepth=2 and must be dropped");
    }

    [Fact]
    public void ForceResetCurrentAnimation_Forward_ResetsToFrame0()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("walk", frames: 4, duration: 0.1f));
        animator.Play("walk");
        animator.Update(0.25f);

        animator.ForceResetCurrentAnimation();

        animator.CurrentFrameIndex.Should().Be(0,
            "ForceResetCurrentAnimation on a forward clip should reset to frame 0");
    }

    [Fact]
    public void ForceResetCurrentAnimation_Reversed_ResetsToLastFrame()
    {
        var animator = new SpriteAnimator { Reversed = true };
        animator.AddAnimation(MakeClip("walk", frames: 4, duration: 0.1f));
        animator.Play("walk");
        animator.Update(0.05f);
        animator.ForceResetCurrentAnimationToEnd();
        animator.Update(0.05f);

        animator.ForceResetCurrentAnimation();

        animator.CurrentFrameIndex.Should().Be(3,
            "ForceResetCurrentAnimation on a reversed clip should reset to the last frame (logical start)");
    }

    [Fact]
    public void ForceResetCurrentAnimationToEnd_Forward_ResetsToLastFrame()
    {
        var animator = new SpriteAnimator();
        animator.AddAnimation(MakeClip("walk", frames: 4, duration: 0.1f));
        animator.Play("walk");

        animator.ForceResetCurrentAnimationToEnd();

        animator.CurrentFrameIndex.Should().Be(3,
            "ForceResetCurrentAnimationToEnd on a forward clip should jump to the last frame");
    }

    [Fact]
    public void ForceResetCurrentAnimationToEnd_Reversed_ResetsToFrame0()
    {
        var animator = new SpriteAnimator { Reversed = true };
        animator.AddAnimation(MakeClip("walk", frames: 4, duration: 0.1f));
        animator.Play("walk");

        animator.ForceResetCurrentAnimationToEnd();

        animator.CurrentFrameIndex.Should().Be(0,
            "ForceResetCurrentAnimationToEnd on a reversed clip should jump to frame 0 (logical end)");
    }

    [Fact]
    public void SeekToTime_FireEvents_PingPong_FiresCorrectEventsForTravelDirection()
    {
        var clip = new AnimationClip("pp") { PlaybackMode = PlaybackMode.PingPong };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        clip.AddFrame(new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.1f));
        clip.AddFrame(new SpriteFrame(new Rectangle(32, 0, 16, 16), 0.1f));

        int fires = 0;
        clip.AddEvent("marker", 0.15f, _ => fires++, fireBothDirections: true);

        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Play("pp");

        animator.SeekToTime(0.05f, fireEvents: true);
        fires.Should().Be(0, "seek from 0 to 0.05 should not cross the marker at 0.15");

        fires = 0;
        animator.SeekToTime(0.25f, fireEvents: true);
        fires.Should().Be(1, "seek from 0.05 to 0.25 crosses the marker at 0.15 going forward");
    }
}