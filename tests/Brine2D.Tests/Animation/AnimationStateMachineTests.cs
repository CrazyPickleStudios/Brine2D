using Brine2D.Animation;
using Brine2D.Core;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Animation;

public class AnimationStateMachineTests
{
    private static SpriteAnimator MakeAnimator(params string[] clipNames)
    {
        var anim = new SpriteAnimator();
        foreach (var name in clipNames)
        {
            var clip = new AnimationClip(name) { PlaybackMode = PlaybackMode.Loop };
            clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
            anim.AddAnimation(clip);
        }
        return anim;
    }

    private static SpriteAnimator MakeAnimatorWithOnce(string onceName, params string[] loopNames)
    {
        var anim = new SpriteAnimator();
        var once = new AnimationClip(onceName) { PlaybackMode = PlaybackMode.OnceHoldLast };
        once.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        anim.AddAnimation(once);
        foreach (var name in loopNames)
        {
            var clip = new AnimationClip(name) { PlaybackMode = PlaybackMode.Loop };
            clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
            anim.AddAnimation(clip);
        }
        return anim;
    }

    [Fact]
    public void DefaultState_PlaysWhenNothingIsActive()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);
        sm.SetDefaultState("idle");

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("idle");
    }

    [Fact]
    public void Transition_FiresWhenConditionIsTrue()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("idle", "walk", () => true);

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("walk");
    }

    [Fact]
    public void Transition_DoesNotFireWhenConditionIsFalse()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("idle", "walk", () => false);

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("idle");
    }

    [Fact]
    public void Transition_DoesNotFireFromWrongState()
    {
        var animator = MakeAnimator("idle", "walk", "run");
        animator.Play("walk");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("idle", "run", () => true);

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("walk");
    }

    [Fact]
    public void AnyTransition_FiresFromAnyState()
    {
        var animator = MakeAnimator("idle", "walk", "hurt");
        animator.Play("walk");
        var sm = new AnimationStateMachine(animator);
        sm.AddAnyTransition("hurt", () => true);

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("hurt");
    }

    [Fact]
    public void NonLoopingClip_BlocksTransitionUntilFinished()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        animator.Play("attack");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("attack", "idle", () => true, canInterrupt: false);

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("attack");
    }

    [Fact]
    public void NonLoopingClip_AllowsInterruptingTransition()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        animator.Play("attack");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("attack", "idle", () => true, canInterrupt: true);

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("idle");
    }

    [Fact]
    public void DefaultState_ResumesAfterNonLoopingClipFinishes()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        var sm = new AnimationStateMachine(animator);
        sm.SetDefaultState("idle");
        animator.Play("attack");
        animator.Update(0.15f);

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("idle");
    }

    [Fact]
    public void DefaultState_DoesNotReplaceItselfRepeatedly()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);
        sm.SetDefaultState("idle");
        sm.Update(0f);

        int starts = 0;
        animator.OnAnimationStart += _ => starts++;

        sm.Update(0f);
        sm.Update(0f);

        starts.Should().Be(0);
    }

    [Fact]
    public void RemoveTransitions_ClearsCorrectPair()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("idle", "walk", () => true);
        sm.RemoveTransitions("idle", "walk");

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("idle");
    }

    [Fact]
    public void ClearTransitions_RemovesAll()
    {
        var animator = MakeAnimator("idle", "walk", "hurt");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("idle", "walk", () => true);
        sm.AddAnyTransition("hurt", () => true);
        sm.ClearTransitions();

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("idle");
    }

    [Fact]
    public void Transition_RespectsMinStateDuration()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("idle", "walk", () => true, minStateDuration: 0.2f);

        sm.Update(0.1f);

        animator.CurrentAnimation!.Name.Should().Be("idle");

        sm.Update(0.15f);

        animator.CurrentAnimation!.Name.Should().Be("walk");
    }

    [Fact]
    public void CurrentState_ReflectsActiveAnimation()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);

        sm.CurrentState.Should().Be("idle");

        sm.AddTransition("idle", "walk", () => true);
        sm.Update(0f);

        sm.CurrentState.Should().Be("walk");
    }

    [Fact]
    public void ForceStateWithCrossFade_SwitchesState_AndFiresOnStateChanged()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);

        string? capturedPrev = null, capturedNext = null;
        sm.OnStateChanged += (prev, next) => { capturedPrev = prev; capturedNext = next; };
        
        sm.ForceStateWithCrossFade("walk", fadeDuration: 0.2f);

        capturedPrev.Should().Be("idle");
        capturedNext.Should().Be("walk");
        sm.CurrentState.Should().Be("walk");
        animator.CrossFadeAlpha.Should().Be(0f);
    }

    [Fact]
    public void ForceStateWithCrossFade_ThrowsOnNonPositiveDuration()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);

        var act = () => sm.ForceStateWithCrossFade("walk", 0f);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RemoveOnCompleteTransitions_RemovesOnlyOnCompleteTransition()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle", "death");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("attack", "idle", () => true);
        sm.AddOnCompleteTransition("attack", "death");

        sm.RemoveOnCompleteTransitions("attack", "death");

        sm.TransitionCount.Should().Be(1);
    }

    [Fact]
    public void RemoveTransitions_DoesNotRemove_OnCompleteTransition()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("attack", "idle", () => true);
        sm.AddOnCompleteTransition("attack", "idle");

        sm.RemoveTransitions("attack", "idle");

        sm.TransitionCount.Should().Be(1);
    }

    [Fact]
    public void Transitions_ExposesAddedTransitions()
    {
        var anim = MakeAnimator("idle", "walk");
        var sm = new AnimationStateMachine(anim);
        sm.AddTransition("idle", "walk", () => true);

        sm.Transitions.Should().HaveCount(1);
        sm.Transitions[0].From.Should().Be("idle");
        sm.Transitions[0].To.Should().Be("walk");
    }

    [Fact]
    public void AnyTransitions_ExposesAddedAnyTransitions()
    {
        var anim = MakeAnimator("idle", "walk");
        var sm = new AnimationStateMachine(anim);
        sm.AddAnyTransition("walk", () => true);

        sm.AnyTransitions.Should().HaveCount(1);
        sm.AnyTransitions[0].To.Should().Be("walk");
    }

    [Fact]
    public void AddAnyOnCompleteTransition_FiresWhenAnyNonLoopClipFinishes()
    {
        var anim = MakeAnimatorWithOnce("attack", "idle");
        var sm = new AnimationStateMachine(anim);
        sm.AddAnyOnCompleteTransition("idle");
        anim.Play("attack");

        sm.Update(0.05f);
        anim.Update(0.05f);
        sm.Update(0.1f);
        anim.Update(0.1f);

        anim.CurrentAnimation!.Name.Should().Be("idle");
    }

    [Fact]
    public void AddAnyOnCompleteTransition_WithFalseCondition_DoesNotFire()
    {
        var anim = MakeAnimatorWithOnce("attack", "idle");
        var sm = new AnimationStateMachine(anim);
        sm.AddAnyOnCompleteTransition("idle", () => false);
        anim.Play("attack");

        sm.Update(0.05f);
        anim.Update(0.05f);
        sm.Update(0.1f);
        anim.Update(0.1f);

        anim.CurrentAnimation!.Name.Should().Be("attack");
    }

    [Fact]
    public void TransitionCount_ReflectsBothLists()
    {
        var anim = MakeAnimator("idle", "walk", "run");
        var sm = new AnimationStateMachine(anim);
        sm.AddTransition("idle", "walk", () => true);
        sm.AddAnyTransition("run", () => false);

        sm.TransitionCount.Should().Be(2);
    }

    [Fact]
    public void OnComplete_PerStateTransition_TakesPriorityOverAnyState()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle", "death");
        var sm = new AnimationStateMachine(animator);
        sm.AddOnCompleteTransition("attack", "idle");
        sm.AddAnyOnCompleteTransition("death");

        animator.Play("attack");
        animator.Update(0.15f);

        animator.CurrentAnimation!.Name.Should().Be("idle",
            "per-state on-complete is more specific and must be evaluated before AnyState");
    }

    [Fact]
    public void OnComplete_AnyStateTransition_FiresWhenNoPerStateMatches()
    {
        var animator = MakeAnimatorWithOnce("attack", "death");
        var sm = new AnimationStateMachine(animator);
        sm.AddAnyOnCompleteTransition("death");

        animator.Play("attack");
        animator.Update(0.15f);

        animator.CurrentAnimation!.Name.Should().Be("death",
            "AnyState on-complete fires when no per-state transition matches");
    }

    [Fact]
    public void OnComplete_PerStateConditionFalse_FallsThroughToAnyState()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle", "death");
        var sm = new AnimationStateMachine(animator);
        sm.AddOnCompleteTransition("attack", "idle", condition: () => false);
        sm.AddAnyOnCompleteTransition("death");

        animator.Play("attack");
        animator.Update(0.15f);

        animator.CurrentAnimation!.Name.Should().Be("death",
            "when per-state condition is false the AnyState catch-all must fire");
    }

    [Fact]
    public void OnComplete_MultiplePerStateTransitions_FirstMatchingConditionWins()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle", "combo");
        var sm = new AnimationStateMachine(animator);
        sm.AddOnCompleteTransition("attack", "combo", condition: () => false);
        sm.AddOnCompleteTransition("attack", "idle", condition: () => true);

        animator.Play("attack");
        animator.Update(0.15f);

        animator.CurrentAnimation!.Name.Should().Be("idle",
            "first per-state transition whose condition is true wins");
    }

    [Fact]
    public void OnStateChanged_FiredByDirectPlay_WhenBypassingStateMachine()
    {
        var animator = MakeAnimator("idle", "walk");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");

        string? capturedFrom = null, capturedTo = null;
        sm.OnStateChanged += (f, t) => { capturedFrom = f; capturedTo = t; };

        animator.Play("walk");

        capturedTo.Should().Be("walk",
            "OnStateChanged must fire even when Play() is called directly on the animator");
        capturedFrom.Should().Be("idle");
    }

    [Fact]
    public void OnStateChanged_NotFiredTwice_WhenInternalTransitionCallsPlay()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("idle", "walk", () => true);

        int changeCount = 0;
        sm.OnStateChanged += (_, _) => changeCount++;

        sm.Update(0f);

        changeCount.Should().Be(1,
            "OnStateChanged must fire exactly once per transition — not once from FireTransition and again from OnAnimationStart");
    }

    [Fact]
    public void OnStateChanged_FromIsNull_WhenStartingFromNoAnimation()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);

        string? capturedFrom = "sentinel";
        sm.OnStateChanged += (f, _) => capturedFrom = f;

        animator.Play("idle");

        capturedFrom.Should().BeNull("previous state is null when nothing was playing before");
    }

    [Fact]
    public void StateTimer_ResetsOnDirectPlay_NotJustViaStateMachine()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);
        sm.Update(0.5f);

        animator.Play("walk");

        sm.StateTimer.Should().BeApproximately(0f, 0.001f,
            "StateTimer must reset via OnAnimationStart even when Play() bypasses the state machine");
    }

    [Fact]
    public void ForceState_SwitchesAnimation_AndFiresOnStateChanged()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);

        string? capturedFrom = null, capturedTo = null;
        sm.OnStateChanged += (f, t) => { capturedFrom = f; capturedTo = t; };

        sm.ForceState("walk");

        animator.CurrentAnimation!.Name.Should().Be("walk");
        capturedFrom.Should().Be("idle");
        capturedTo.Should().Be("walk");
    }

    [Fact]
    public void ForceStateFromFrame_StartsAtCorrectFrame()
    {
        var clip = new AnimationClip("walk") { PlaybackMode = PlaybackMode.Loop };
        for (int i = 0; i < 4; i++)
            clip.AddFrame(new SpriteFrame(new Rectangle(i * 16, 0, 16, 16), 0.1f));
        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Play("walk");
        var sm = new AnimationStateMachine(animator);

        sm.ForceStateFromFrame("walk", startFrame: 2, restart: true);

        animator.CurrentFrameIndex.Should().Be(2);
    }

    [Fact]
    public void ForceStateFromNormalizedTime_StartsAtCorrectPosition()
    {
        var clip = new AnimationClip("walk") { PlaybackMode = PlaybackMode.Loop };
        for (int i = 0; i < 4; i++)
            clip.AddFrame(new SpriteFrame(new Rectangle(i * 16, 0, 16, 16), 0.1f));
        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Play("walk");
        var sm = new AnimationStateMachine(animator);

        sm.ForceStateFromNormalizedTime("walk", normalizedTime: 0.5f, restart: true);

        animator.NormalizedTime.Should().BeApproximately(0.5f, 0.05f);
    }

    [Fact]
    public void Transition_DoesNotFire_BeforeMinNormalizedTime()
    {
        var clip = new AnimationClip("attack") { PlaybackMode = PlaybackMode.OnceHoldLast };
        for (int i = 0; i < 4; i++)
            clip.AddFrame(new SpriteFrame(new Rectangle(i * 16, 0, 16, 16), 0.1f));
        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.AddAnimation(MakeAnimator("idle").GetAnimation("idle")!);
        animator.Play("attack");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("attack", "idle", () => true, canInterrupt: true, minNormalizedTime: 0.8f);

        animator.Update(0.1f);
        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("attack");
    }

    [Fact]
    public void Transition_Fires_AfterMinNormalizedTime()
    {
        var clip = new AnimationClip("attack") { PlaybackMode = PlaybackMode.OnceHoldLast };
        for (int i = 0; i < 4; i++)
            clip.AddFrame(new SpriteFrame(new Rectangle(i * 16, 0, 16, 16), 0.1f));
        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.AddAnimation(MakeAnimator("idle").GetAnimation("idle")!);
        animator.Play("attack");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("attack", "idle", () => true, canInterrupt: true, minNormalizedTime: 0.8f);

        animator.Update(0.35f);
        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("idle");
    }

    [Fact]
    public void RemoveTransition_ByInstance_ReturnsTrueAndPreventsItFiring()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("idle", "walk", () => true);
        var t = sm.Transitions[0];

        sm.RemoveTransition(t).Should().BeTrue();
        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("idle");
    }

    [Fact]
    public void RemoveTransition_ByInstance_ReturnsFalse_WhenNotFound()
    {
        var animator = MakeAnimator("idle", "walk");
        var sm = new AnimationStateMachine(animator);
        var foreign = new AnimationTransition("idle", "walk", () => true, false);

        sm.RemoveTransition(foreign).Should().BeFalse();
    }

    [Fact]
    public void RemoveAnyTransitions_RemovesAnyStateConditionTransition()
    {
        var animator = MakeAnimator("idle", "death");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);
        sm.AddAnyTransition("death", () => true);
        sm.RemoveAnyTransitions("death");

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("idle");
        sm.AnyTransitions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveAnyOnCompleteTransition_RemovesAnyStateOnCompleteTransition()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        var sm = new AnimationStateMachine(animator);
        sm.AddAnyOnCompleteTransition("idle");
        sm.RemoveAnyOnCompleteTransition("idle");
        animator.Play("attack");

        animator.Update(0.15f);

        animator.IsPlaying.Should().BeFalse("clip finished and no transition fired");
        animator.IsFinished.Should().BeTrue("clip should have reached its natural end");
        animator.CurrentAnimation!.Name.Should().Be("attack",
            "AnyOnComplete transition was removed so no redirect fired; CurrentAnimation stays on the finished clip");
        sm.AnyTransitions.Should().BeEmpty();
    }

    [Fact]
    public void IsInState_ReturnsTrueForCurrentState()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");

        sm.IsInState("idle").Should().BeTrue();
        sm.IsInState("run").Should().BeFalse();
    }

    [Fact]
    public void IsInState_ReturnsFalse_WhenNoAnimationPlaying()
    {
        var sm = new AnimationStateMachine(new SpriteAnimator());

        sm.IsInState("idle").Should().BeFalse();
    }

    [Fact]
    public void IsInState_UpdatesAfterTransition()
    {
        var clip1 = new AnimationClip("idle") { PlaybackMode = PlaybackMode.Loop };
        clip1.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        var clip2 = new AnimationClip("run") { PlaybackMode = PlaybackMode.Loop };
        clip2.AddFrame(new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.1f));

        var animator = new SpriteAnimator();
        animator.AddAnimation(clip1);
        animator.AddAnimation(clip2);

        bool shouldTransition = false;
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("idle", "run", () => shouldTransition);
        animator.Play("idle");

        sm.IsInState("idle").Should().BeTrue();

        shouldTransition = true;
        sm.Update(0.01f);

        sm.IsInState("run").Should().BeTrue();
        sm.IsInState("idle").Should().BeFalse();
    }

    [Fact]
    public void AddTransition_ReturnsHandle_CanBeRemovedByHandle()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);

        var handle = sm.AddTransition("idle", "walk", () => true);
        sm.RemoveTransition(handle);

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("idle",
            "removing the handle should prevent the transition from firing");
    }

    [Fact]
    public void AddAnyTransition_ReturnsHandle_CanBeRemovedByHandle()
    {
        var animator = MakeAnimator("idle", "hurt");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);

        var handle = sm.AddAnyTransition("hurt", () => true);
        sm.RemoveTransition(handle);

        sm.Update(0f);

        animator.CurrentAnimation!.Name.Should().Be("idle",
            "removing the AnyState handle should prevent the transition from firing");
    }

    [Fact]
    public void AddOnCompleteTransition_ReturnsHandle_CanBeRemovedByHandle()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        var sm = new AnimationStateMachine(animator);

        var handle = sm.AddOnCompleteTransition("attack", "idle");
        sm.RemoveTransition(handle);

        animator.Play("attack");
        animator.Update(0.15f);

        animator.IsPlaying.Should().BeFalse("clip should have finished");
        animator.IsFinished.Should().BeTrue("clip should have reached its natural end");
        animator.CurrentAnimation!.Name.Should().Be("attack",
            "on-complete transition was removed — no redirect to idle should have fired");
    }

    [Fact]
    public void RestartSelf_AnyTransition_RestartsCurrentClip()
    {
        var animator = MakeAnimator("idle");
        animator.Play("idle");
        animator.Update(0.15f);
        var sm = new AnimationStateMachine(animator);

        sm.AddAnyTransition("idle", () => true, canInterrupt: true, restartSelf: true);

        sm.Update(0f);

        animator.CurrentFrameIndex.Should().Be(0,
            "RestartSelf=true on an AnyState transition should restart the active clip from frame 0");
    }

    [Fact]
    public void RestartSelf_False_SkipsSelfTransition()
    {
        var animator = MakeAnimator("idle");
        animator.Play("idle");
        animator.Update(0.15f);
        var sm = new AnimationStateMachine(animator);

        sm.AddAnyTransition("idle", () => true, canInterrupt: true, restartSelf: false);

        var frameBefore = animator.CurrentFrameIndex;
        sm.Update(0f);

        animator.CurrentFrameIndex.Should().Be(frameBefore,
            "RestartSelf=false (default) should skip self-transitions silently");
    }

    [Fact]
    public void RestartSelf_RegularTransition_RestartsTargetClip()
    {
        var animator = MakeAnimator("idle");
        animator.Play("idle");
        animator.Update(0.15f);
        var sm = new AnimationStateMachine(animator);

        sm.AddTransition("idle", "idle", () => true, canInterrupt: true, restartSelf: true);

        sm.Update(0f);

        animator.CurrentFrameIndex.Should().Be(0,
            "RestartSelf=true on a regular transition should restart the clip");
    }

    [Fact]
    public void CaptureSnapshot_RestoreSnapshot_PreservesStateTimerAndPreviousState()
    {
        var animator = MakeAnimator("idle", "walk");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");
        sm.Update(0.3f);
        animator.Play("walk");

        var snap = sm.CaptureSnapshot();
        snap.StateTimer.Should().BeApproximately(0f, 0.001f, "timer resets on each new state");
        snap.PreviousState.Should().Be("idle");
        snap.DefaultState.Should().BeNull();
        snap.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void CaptureSnapshot_PreviousState_IsCorrectAfterTransition()
    {
        var animator = MakeAnimator("idle", "walk");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");
        sm.AddTransition("idle", "walk", () => true);
        sm.Update(0f);

        var snap = sm.CaptureSnapshot();

        snap.PreviousState.Should().Be("idle",
            "snapshot must capture the correct PreviousState after a transition");
        snap.CurrentStateName.Should().Be("walk");
    }

    [Fact]
    public void RestoreSnapshot_RestoresAllFields()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("walk");
        var sm = new AnimationStateMachine(animator);
        sm.SetDefaultState("idle");
        sm.Update(0.5f);

        var snap = sm.CaptureSnapshot();

        var animator2 = MakeAnimator("idle", "walk");
        animator2.Play("idle");
        var sm2 = new AnimationStateMachine(animator2);
        sm2.RestoreSnapshot(snap);

        sm2.StateTimer.Should().BeApproximately(snap.StateTimer, 0.001f);
        sm2.PreviousState.Should().Be(snap.PreviousState);
        sm2.IsEnabled.Should().Be(snap.IsEnabled);
    }

    [Fact]
    public void RestoreSnapshot_DoesNotFireOnStateChanged()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);
        sm.Update(0.2f);
        var snap = sm.CaptureSnapshot();

        int fired = 0;
        sm.OnStateChanged += (_, _) => fired++;
        sm.RestoreSnapshot(snap);

        fired.Should().Be(0, "RestoreSnapshot must not fire OnStateChanged");
    }

    [Fact]
    public void PreviousState_IsNull_BeforeAnyTransition()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");

        sm.PreviousState.Should().BeNull("no transition has occurred yet");
    }

    [Fact]
    public void PreviousState_ReturnsNameOfStateBeforeCurrent()
    {
        var animator = MakeAnimator("idle", "walk");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");
        sm.AddTransition("idle", "walk", () => true);

        sm.Update(0f);

        sm.CurrentState.Should().Be("walk");
        sm.PreviousState.Should().Be("idle",
            "PreviousState must return the state that was active before the transition, not the current state");
    }

    [Fact]
    public void PreviousState_DoesNotEqualCurrentState_AfterTransition()
    {
        var animator = MakeAnimator("idle", "walk");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");
        sm.AddTransition("idle", "walk", () => true);

        sm.Update(0f);

        sm.PreviousState.Should().NotBe(sm.CurrentState,
            "PreviousState and CurrentState must differ after a transition");
    }

    [Fact]
    public void PreviousState_UpdatesCorrectly_AfterChainedTransitions()
    {
        var animator = MakeAnimator("idle", "walk", "run");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");
        sm.AddTransition("idle", "walk", () => true);
        sm.Update(0f);

        sm.CurrentState.Should().Be("walk");
        sm.PreviousState.Should().Be("idle");

        sm.AddTransition("walk", "run", () => true);
        sm.Update(0f);

        sm.CurrentState.Should().Be("run");
        sm.PreviousState.Should().Be("walk");
    }

    [Fact]
    public void PreviousState_IsNull_AfterForceStop()
    {
        var animator = MakeAnimator("idle", "walk");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");
        sm.AddTransition("idle", "walk", () => true);
        sm.Update(0f);

        sm.ForceStop();

        sm.PreviousState.Should().BeNull("ForceStop clears all state tracking");
        sm.CurrentState.Should().BeNull();
    }

    [Fact]
    public void ForceStop_FiresOnStateChanged_WithNullAsNewState()
    {
        var animator = MakeAnimator("idle");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);

        string? capturedFrom = null;
        string? capturedTo = "sentinel";
        sm.OnStateChanged += (f, t) => { capturedFrom = f; capturedTo = t; };

        sm.ForceStop();

        capturedFrom.Should().Be("idle");
        capturedTo.Should().BeNull("ForceStop must pass null as the new state to OnStateChanged");
    }

    [Fact]
    public void ForceStop_DoesNotFireOnStateChanged_WhenNothingIsPlaying()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);

        int fired = 0;
        sm.OnStateChanged += (_, _) => fired++;

        sm.ForceStop();

        fired.Should().Be(0, "ForceStop on an already idle animator must not fire OnStateChanged");
    }

    [Fact]
    public void RestoreSnapshot_RoundTrips_PreviousAndCurrentState()
    {
        var animator = MakeAnimator("idle", "walk", "run");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");
        sm.AddTransition("idle", "walk", () => true);
        sm.Update(0f);

        var snap = sm.CaptureSnapshot();

        var animator2 = MakeAnimator("idle", "walk", "run");
        animator2.Play("run");
        var sm2 = new AnimationStateMachine(animator2);
        sm2.RestoreSnapshot(snap);

        sm2.PreviousState.Should().Be("idle");
    }

    [Fact]
    public void IsDisposed_IsFalse_WhenCreated()
    {
        var animator = MakeAnimator("idle");
        using var sm = new AnimationStateMachine(animator);

        sm.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void IsDisposed_IsTrue_AfterDispose()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);
        sm.Dispose();

        sm.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void IsDisposed_Update_IsNoOp_AfterDispose()
    {
        var animator = MakeAnimator("idle", "walk");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("idle", "walk", () => true);
        sm.Dispose();

        sm.Update(0.1f);

        animator.CurrentAnimation!.Name.Should().Be("idle",
            "a disposed state machine must not evaluate transitions");
    }

    [Fact]
    public void ForceStateDirect_PlaysUnregisteredClip_AndTracksState()
    {
        var animator = MakeAnimator("idle");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);

        var flyClip = new AnimationClip("fly") { PlaybackMode = PlaybackMode.Loop };
        flyClip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));

        string? capturedFrom = null, capturedTo = null;
        sm.OnStateChanged += (f, t) => { capturedFrom = f; capturedTo = t; };

        sm.ForceStateDirect(flyClip);

        animator.CurrentAnimation.Should().BeSameAs(flyClip);
        capturedFrom.Should().Be("idle");
        capturedTo.Should().Be("fly");
        sm.CurrentState.Should().Be("fly");
    }

    [Fact]
    public void ForceStateDirect_ThrowsOnNullClip()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);

        var act = () => sm.ForceStateDirect(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ForceStateDirect_NoOp_WhenSameClipAlreadyPlaying_AndRestartFalse()
    {
        var clip = new AnimationClip("fly") { PlaybackMode = PlaybackMode.Loop };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        var animator = new SpriteAnimator();
        animator.PlayDirect(clip);
        animator.Update(0.05f);
        var sm = new AnimationStateMachine(animator);

        int starts = 0;
        animator.OnAnimationStart += _ => starts++;

        sm.ForceStateDirect(clip, restart: false);

        starts.Should().Be(0, "ForceStateDirect with restart=false must not restart an already-playing clip");
    }

    [Fact]
    public void ForceStateDirect_Restarts_WhenRestartTrue()
    {
        var clip = new AnimationClip("fly") { PlaybackMode = PlaybackMode.Loop };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        var animator = new SpriteAnimator();
        animator.PlayDirect(clip);
        animator.Update(0.05f);
        var sm = new AnimationStateMachine(animator);

        sm.ForceStateDirect(clip, restart: true);

        animator.CurrentFrameIndex.Should().Be(0);
        animator.CurrentTime.Should().BeApproximately(0f, 0.001f);
    }

    [Fact]
    public void ForceStateDirectWithCrossFade_PlaysWithFade_AndTracksState()
    {
        var animator = MakeAnimator("idle");
        animator.Play("idle");
        var sm = new AnimationStateMachine(animator);

        var flyClip = new AnimationClip("fly") { PlaybackMode = PlaybackMode.Loop };
        flyClip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));

        string? capturedTo = null;
        sm.OnStateChanged += (_, t) => capturedTo = t;

        sm.ForceStateDirectWithCrossFade(flyClip, fadeDuration: 0.2f);

        animator.CurrentAnimation.Should().BeSameAs(flyClip);
        capturedTo.Should().Be("fly");
        animator.CrossFadeAlpha.Should().Be(0f, "cross-fade starts at alpha 0");
    }

    [Fact]
    public void ForceStateDirectWithCrossFade_ThrowsOnNonPositiveDuration()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);
        var clip = new AnimationClip("fly") { PlaybackMode = PlaybackMode.Loop };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));

        var act = () => sm.ForceStateDirectWithCrossFade(clip, 0f);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void OnStateChanged_DefaultStateKickoff_DoesNotFireWhenClipMissing()
    {
        var animator = MakeAnimator("idle");
        using var sm = new AnimationStateMachine(animator);
        sm.SetDefaultState("nonexistent");

        var changes = new List<(string? from, string? to)>();
        sm.OnStateChanged += (f, t) => changes.Add((f, t));

        sm.Update(0.016f);

        changes.Should().BeEmpty("Play silently failed so OnStateChanged must not fire");
    }

    [Fact]
    public void OnStateChanged_DefaultStateOnComplete_DoesNotFireWhenClipMissing()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        using var sm = new AnimationStateMachine(animator);
        sm.SetDefaultState("nonexistent");
        animator.Play("attack");

        var changes = new List<(string? from, string? to)>();
        sm.OnStateChanged += (f, t) => changes.Add((f, t));

        sm.Update(0.2f);
        animator.Update(0.2f);

        changes.Should().BeEmpty("Play silently failed so OnStateChanged must not fire");
    }

    [Fact]
    public void OnStateChanged_DirectAnimatorStop_FiresWithNullDestination()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");

        var changes = new List<(string? from, string? to)>();
        sm.OnStateChanged += (f, t) => changes.Add((f, t));

        animator.Stop();

        changes.Should().HaveCount(1);
        changes[0].Should().Be(("idle", null),
            "a direct animator.Stop() must notify FSM subscribers via OnStateChanged");
    }

    [Fact]
    public void OnStateChanged_DirectAnimatorStop_DoesNotFireWhenAlreadyIdle()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);

        var changes = new List<(string? from, string? to)>();
        sm.OnStateChanged += (f, t) => changes.Add((f, t));

        animator.Stop();

        changes.Should().BeEmpty("no active state, nothing to notify");
    }

    [Fact]
    public void OnStateChanged_DirectStopInsideForceState_DoesNotFireSpuriousNull()
    {
        var animator = MakeAnimator("idle", "run");
        using var sm = new AnimationStateMachine(animator);
        animator.Play("idle");

        var nullChanges = new List<(string? from, string? to)>();
        sm.OnStateChanged += (f, t) => { if (t == null) nullChanges.Add((f, t)); };

        sm.ForceState("run");

        nullChanges.Should().BeEmpty(
            "the Stop that occurs internally during ForceState is suppressed and must not emit a null transition");
    }

    [Fact]
    public void OnceStop_CompletesWithNewClipStartedInOnComplete_StateMachineStateIsValid()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        var onceStop = new AnimationClip("sfx") { PlaybackMode = PlaybackMode.OnceStop };
        onceStop.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.05f));
        animator.AddAnimation(onceStop);

        var sm = new AnimationStateMachine(animator);
        sm.AddOnCompleteTransition("sfx", "idle");

        animator.Play("sfx");
        animator.Update(0.1f);

        sm.CurrentState.Should().Be("idle",
            "after OnceStop completes and a transition fires to 'idle', CurrentState must be 'idle'");
        animator.IsPlaying.Should().BeTrue("the 'idle' loop clip should be playing");
    }

    [Fact]
    public void OnceStop_CompletesWithNewClipStartedInOnComplete_OnStateChanged_DoesNotFireSpuriousNull()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        var onceStop = new AnimationClip("sfx") { PlaybackMode = PlaybackMode.OnceStop };
        onceStop.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.05f));
        animator.AddAnimation(onceStop);

        var sm = new AnimationStateMachine(animator);
        sm.AddOnCompleteTransition("sfx", "idle");

        animator.Play("sfx");

        var nullChanges = new List<(string? from, string? to)>();
        sm.OnStateChanged += (f, t) => { if (t == null) nullChanges.Add((f, t)); };

        animator.Update(0.1f);

        nullChanges.Should().BeEmpty(
            "OnceStop completing with a new clip already started must not emit a spurious (idle → null) OnStateChanged");
    }

    [Fact]
    public void ForceStateQueued_WhenNothingPlaying_PlaysImmediately()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        var sm = new AnimationStateMachine(animator);

        sm.ForceStateQueued("idle");

        animator.CurrentAnimation!.Name.Should().Be("idle");
        animator.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void ForceStateQueued_WhenOnceClipPlaying_PlaysAfterCompletion()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        var sm = new AnimationStateMachine(animator);
        animator.Play("attack");

        sm.ForceStateQueued("idle");

        animator.CurrentAnimation!.Name.Should().Be("attack",
            "queue must not interrupt the currently playing non-looping clip");

        animator.Update(0.15f);

        animator.CurrentAnimation!.Name.Should().Be("idle",
            "queued animation must play after the non-looping clip finishes");
    }

    [Fact]
    public void ForceStateQueued_TracksStateChangeOnStart()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        var sm = new AnimationStateMachine(animator);
        animator.Play("attack");
        sm.ForceStateQueued("idle");

        var changes = new List<string?>();
        sm.OnStateChanged += (_, to) => changes.Add(to);

        animator.Update(0.15f);

        changes.Should().Contain("idle",
            "OnStateChanged must fire when the queued clip starts");
    }

    [Fact]
    public void ForceStateQueuedWithCrossFade_ThrowsOnNonPositiveDuration()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);

        var act = () => sm.ForceStateQueuedWithCrossFade("idle", 0f);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ForceStateDirectQueued_WhenNothingPlaying_PlaysImmediately()
    {
        var clip = new AnimationClip("fly") { PlaybackMode = PlaybackMode.Loop };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        var animator = new SpriteAnimator();
        var sm = new AnimationStateMachine(animator);

        sm.ForceStateDirectQueued(clip);

        animator.CurrentAnimation.Should().BeSameAs(clip);
        animator.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void ForceStateDirectQueued_WhenOnceClipPlaying_PlaysAfterCompletion()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        var sm = new AnimationStateMachine(animator);
        animator.Play("attack");

        var flyClip = new AnimationClip("fly") { PlaybackMode = PlaybackMode.Loop };
        flyClip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));

        sm.ForceStateDirectQueued(flyClip);

        animator.CurrentAnimation!.Name.Should().Be("attack",
            "direct queued clip must not interrupt the non-looping clip");

        animator.Update(0.15f);

        animator.CurrentAnimation.Should().BeSameAs(flyClip);
    }

    [Fact]
    public void ForceStateDirectQueued_TracksStateChangeViaOnAnimationStart()
    {
        var animator = MakeAnimatorWithOnce("attack", "idle");
        var sm = new AnimationStateMachine(animator);
        animator.Play("attack");

        var flyClip = new AnimationClip("fly") { PlaybackMode = PlaybackMode.Loop };
        flyClip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        sm.ForceStateDirectQueued(flyClip);

        var changes = new List<string?>();
        sm.OnStateChanged += (_, to) => changes.Add(to);

        animator.Update(0.15f);

        changes.Should().Contain("fly",
            "OnStateChanged must fire when the direct-queued clip starts");
        sm.CurrentState.Should().Be("fly");
    }

    [Fact]
    public void ForceStateDirectQueued_ThrowsOnNullClip()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);

        var act = () => sm.ForceStateDirectQueued(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ForceStateDirectQueuedWithCrossFade_ThrowsOnNonPositiveDuration()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);
        var clip = new AnimationClip("fly") { PlaybackMode = PlaybackMode.Loop };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));

        var act = () => sm.ForceStateDirectQueuedWithCrossFade(clip, 0f);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void OnStateExit_FiresWhenAnimatorStopped_WithNullNextState()
    {
        var animator = MakeAnimator("idle");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");

        string? capturedNext = "sentinel";
        sm.OnStateExit("idle", next => capturedNext = next);

        sm.ForceStop();

        capturedNext.Should().BeNull("stop passes null as the next state to OnStateExit callbacks");
    }

    [Fact]
    public void OnStateExit_FiresWhenTransitioningToNewState()
    {
        var animator = MakeAnimator("idle", "walk");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");
        sm.AddTransition("idle", "walk", () => true);

        string? capturedNext = null;
        sm.OnStateExit("idle", next => capturedNext = next);

        sm.Update(0f);

        capturedNext.Should().Be("walk");
    }

    [Fact]
    public void OnStateExit_DoesNotFire_ForOtherStates()
    {
        var animator = MakeAnimator("idle", "walk");
        var sm = new AnimationStateMachine(animator);
        animator.Play("idle");
        sm.AddTransition("idle", "walk", () => true);

        int fired = 0;
        sm.OnStateExit("walk", _ => fired++);

        sm.Update(0f);

        fired.Should().Be(0, "exit callback for 'walk' must not fire when 'idle' is exiting");
    }
}