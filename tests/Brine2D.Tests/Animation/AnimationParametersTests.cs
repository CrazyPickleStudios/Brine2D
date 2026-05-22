using Brine2D.Animation;
using Brine2D.Core;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Animation;

public class AnimationParametersTests
{
    [Fact]
    public void SetBool_GetBool_RoundTrips()
    {
        var p = new AnimationParameters();
        p.SetBool("grounded", true);
        p.GetBool("grounded").Should().BeTrue();
    }

    [Fact]
    public void GetBool_UnsetKey_ReturnsFalse()
    {
        var p = new AnimationParameters();
        p.GetBool("missing").Should().BeFalse();
    }

    [Fact]
    public void SetBool_False_Clears()
    {
        var p = new AnimationParameters();
        p.SetBool("grounded", true);
        p.SetBool("grounded", false);
        p.GetBool("grounded").Should().BeFalse();
    }

    [Fact]
    public void HasBool_TrueAfterSet()
    {
        var p = new AnimationParameters();
        Assert.False(p.HasBool("grounded"));
        p.SetBool("grounded", true);
        Assert.True(p.HasBool("grounded"));
    }

    [Fact]
    public void RemoveBool_ExistingKey_ReturnsTrueAndRemoves()
    {
        var p = new AnimationParameters();
        p.SetBool("grounded", true);

        p.RemoveBool("grounded").Should().BeTrue();

        p.HasBool("grounded").Should().BeFalse();
        p.GetBool("grounded").Should().BeFalse();
    }

    [Fact]
    public void RemoveBool_MissingKey_ReturnsFalse()
    {
        var p = new AnimationParameters();
        p.RemoveBool("nope").Should().BeFalse();
    }

    [Fact]
    public void SetFloat_GetFloat_RoundTrips()
    {
        var p = new AnimationParameters();
        p.SetFloat("speed", 3.5f);
        p.GetFloat("speed").Should().BeApproximately(3.5f, 0.001f);
    }

    [Fact]
    public void GetFloat_UnsetKey_ReturnsZero()
    {
        var p = new AnimationParameters();
        p.GetFloat("missing").Should().Be(0f);
    }

    [Fact]
    public void HasFloat_TrueAfterSet()
    {
        var p = new AnimationParameters();
        Assert.False(p.HasFloat("speed"));
        p.SetFloat("speed", 1.5f);
        Assert.True(p.HasFloat("speed"));
    }

    [Fact]
    public void RemoveFloat_ExistingKey_ReturnsTrueAndRemoves()
    {
        var p = new AnimationParameters();
        p.SetFloat("speed", 5f);

        p.RemoveFloat("speed").Should().BeTrue();

        p.HasFloat("speed").Should().BeFalse();
        p.GetFloat("speed").Should().Be(0f);
    }

    [Fact]
    public void RemoveFloat_MissingKey_ReturnsFalse()
    {
        var p = new AnimationParameters();
        p.RemoveFloat("nope").Should().BeFalse();
    }

    [Fact]
    public void SetInt_GetInt_RoundTrips()
    {
        var p = new AnimationParameters();
        p.SetInt("health", 42);
        p.GetInt("health").Should().Be(42);
    }

    [Fact]
    public void GetInt_UnsetKey_ReturnsZero()
    {
        var p = new AnimationParameters();
        p.GetInt("missing").Should().Be(0);
    }

    [Fact]
    public void SetInt_Overwrites_PreviousValue()
    {
        var p = new AnimationParameters();
        p.SetInt("tier", 1);
        p.SetInt("tier", 5);
        p.GetInt("tier").Should().Be(5);
    }

    [Fact]
    public void SetInt_NegativeValue_Stored()
    {
        var p = new AnimationParameters();
        p.SetInt("health", -10);
        p.GetInt("health").Should().Be(-10);
    }

    [Fact]
    public void HasInt_TrueAfterSet()
    {
        var p = new AnimationParameters();
        Assert.False(p.HasInt("combo"));
        p.SetInt("combo", 3);
        Assert.True(p.HasInt("combo"));
    }

    [Fact]
    public void RemoveInt_ExistingKey_ReturnsTrueAndRemoves()
    {
        var p = new AnimationParameters();
        p.SetInt("combo", 3);

        p.RemoveInt("combo").Should().BeTrue();

        p.HasInt("combo").Should().BeFalse();
        p.GetInt("combo").Should().Be(0);
    }

    [Fact]
    public void RemoveInt_MissingKey_ReturnsFalse()
    {
        var p = new AnimationParameters();
        p.RemoveInt("nope").Should().BeFalse();
    }

    [Fact]
    public void SetTrigger_IsTriggerArmed_ReturnsTrue()
    {
        var p = new AnimationParameters();
        p.SetTrigger("attack");
        p.IsTriggerArmed("attack").Should().BeTrue();
    }

    [Fact]
    public void HasTrigger_TrueWhileArmed_FalseAfterConsumed()
    {
        var p = new AnimationParameters();
        Assert.False(p.HasTrigger("attack"));
        p.SetTrigger("attack");
        Assert.True(p.HasTrigger("attack"));
        p.GetTrigger("attack");
        Assert.False(p.HasTrigger("attack"));
    }

    [Fact]
    public void GetTrigger_ConsumesTrigger()
    {
        var p = new AnimationParameters();
        p.SetTrigger("attack");

        p.GetTrigger("attack").Should().BeTrue();
        p.GetTrigger("attack").Should().BeFalse();
    }

    [Fact]
    public void ResetTrigger_DisarmsWithoutConsuming()
    {
        var p = new AnimationParameters();
        p.SetTrigger("attack");

        p.ResetTrigger("attack");

        p.IsTriggerArmed("attack").Should().BeFalse();
    }

    [Fact]
    public void GetBoolNames_ReturnsSetNames()
    {
        var p = new AnimationParameters();
        p.SetBool("grounded", true);
        p.SetBool("crouching", false);
        Assert.Contains("grounded", p.GetBoolNames());
        Assert.Contains("crouching", p.GetBoolNames());
    }

    [Fact]
    public void GetFloatNames_ReturnsSetNames()
    {
        var p = new AnimationParameters();
        p.SetFloat("speed", 1f);
        Assert.Contains("speed", p.GetFloatNames());
    }

    [Fact]
    public void GetIntNames_ReturnsSetNames()
    {
        var p = new AnimationParameters();
        p.SetInt("health", 100);
        Assert.Contains("health", p.GetIntNames());
    }

    [Fact]
    public void GetArmedTriggerNames_ReturnsArmedOnly()
    {
        var p = new AnimationParameters();
        p.SetTrigger("jump");
        p.SetTrigger("attack");
        p.GetTrigger("jump");

        var armed = p.GetArmedTriggerNames().ToList();
        Assert.DoesNotContain("jump", armed);
        Assert.Contains("attack", armed);
    }

    [Fact]
    public void Trigger_WorksAsStateMachineCondition_PollSafe()
    {
        var animator = new SpriteAnimator();
        var idle = new AnimationClip("idle") { PlaybackMode = PlaybackMode.Loop };
        idle.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        var attack = new AnimationClip("attack") { PlaybackMode = PlaybackMode.OnceHoldLast };
        attack.AddFrame(new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.1f));
        animator.AddAnimation(idle);
        animator.AddAnimation(attack);
        animator.Play("idle");

        var p = new AnimationParameters();
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("idle", "attack", () => p.GetTrigger("attack"), canInterrupt: true);

        sm.Update(0.1f);
        animator.CurrentAnimation!.Name.Should().Be("idle");

        p.SetTrigger("attack");
        sm.Update(0.1f);
        animator.CurrentAnimation!.Name.Should().Be("attack");

        sm.Update(0.1f);
        animator.CurrentAnimation!.Name.Should().Be("attack");
    }

    [Fact]
    public void Int_WorksAsStateMachineCondition()
    {
        var animator = new SpriteAnimator();
        var idle = new AnimationClip("idle") { PlaybackMode = PlaybackMode.Loop };
        idle.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        var hurt = new AnimationClip("hurt") { PlaybackMode = PlaybackMode.OnceHoldLast };
        hurt.AddFrame(new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.1f));
        animator.AddAnimation(idle);
        animator.AddAnimation(hurt);
        animator.Play("idle");

        var p = new AnimationParameters();
        var sm = new AnimationStateMachine(animator);
        sm.AddTransition("idle", "hurt", () => p.GetInt("health") <= 0, canInterrupt: true);

        p.SetInt("health", 5);
        sm.Update(0.016f);
        animator.CurrentAnimation!.Name.Should().Be("idle");

        p.SetInt("health", 0);
        sm.Update(0.016f);
        animator.CurrentAnimation!.Name.Should().Be("hurt");
    }

    [Fact]
    public void Reset_ClearsEverything()
    {
        var p = new AnimationParameters();
        p.SetBool("b", true);
        p.SetFloat("f", 1f);
        p.SetInt("i", 1);
        p.SetTrigger("t");

        p.Reset();

        p.HasBool("b").Should().BeFalse();
        p.HasFloat("f").Should().BeFalse();
        p.HasInt("i").Should().BeFalse();
        p.IsTriggerArmed("t").Should().BeFalse();
    }

    [Fact]
    public void CaptureSnapshot_RestoreSnapshot_RoundTrips()
    {
        var p = new AnimationParameters();
        p.SetBool("b", true);
        p.SetFloat("f", 2f);
        p.SetInt("i", 7);
        p.SetTrigger("t");

        var snap = p.CaptureSnapshot();
        p.Reset();
        p.RestoreSnapshot(snap);

        p.GetBool("b").Should().BeTrue();
        p.GetFloat("f").Should().Be(2f);
        p.GetInt("i").Should().Be(7);
        p.IsTriggerArmed("t").Should().BeTrue();
    }

    [Fact]
    public void CaptureRestore_RestoresAllValues_AfterMutation()
    {
        var p = new AnimationParameters();
        p.SetBool("grounded", true);
        p.SetFloat("speed", 5.5f);
        p.SetInt("health", 3);
        p.SetTrigger("jump");

        var snapshot = p.CaptureSnapshot();

        p.SetBool("grounded", false);
        p.SetFloat("speed", 0f);
        p.SetInt("health", 0);
        p.ResetTrigger("jump");

        p.RestoreSnapshot(snapshot);

        Assert.True(p.GetBool("grounded"));
        Assert.Equal(5.5f, p.GetFloat("speed"));
        Assert.Equal(3, p.GetInt("health"));
        Assert.True(p.IsTriggerArmed("jump"));
    }

    [Fact]
    public void Snapshot_IsImmutable()
    {
        var p = new AnimationParameters();
        p.SetFloat("speed", 2f);

        var snapshot = p.CaptureSnapshot();

        p.SetFloat("speed", 9f);
        p.RestoreSnapshot(snapshot);

        Assert.Equal(2f, p.GetFloat("speed"));
    }

    [Fact]
    public void RestoreSnapshot_ClearsValuesNotInSnapshot()
    {
        var p = new AnimationParameters();
        p.SetBool("a", true);
        var snapshot = p.CaptureSnapshot();

        p.SetBool("b", true);
        p.RestoreSnapshot(snapshot);

        Assert.False(p.HasBool("b"));
    }

    [Fact]
    public void ClearBools_RemovesAllBools_LeavesOtherTypesIntact()
    {
        var p = new AnimationParameters();
        p.SetBool("grounded", true);
        p.SetFloat("speed", 1f);
        p.SetInt("health", 10);
        p.SetTrigger("jump");

        p.ClearBools();

        p.HasBool("grounded").Should().BeFalse();
        p.GetBool("grounded").Should().BeFalse();
        p.HasFloat("speed").Should().BeTrue("floats untouched");
        p.HasInt("health").Should().BeTrue("ints untouched");
        p.IsTriggerArmed("jump").Should().BeTrue("triggers untouched");
    }

    [Fact]
    public void ClearFloats_RemovesAllFloats_LeavesOtherTypesIntact()
    {
        var p = new AnimationParameters();
        p.SetBool("grounded", true);
        p.SetFloat("speed", 1f);
        p.SetInt("health", 10);
        p.SetTrigger("jump");

        p.ClearFloats();

        p.HasFloat("speed").Should().BeFalse();
        p.GetFloat("speed").Should().Be(0f);
        p.HasBool("grounded").Should().BeTrue("bools untouched");
        p.HasInt("health").Should().BeTrue("ints untouched");
        p.IsTriggerArmed("jump").Should().BeTrue("triggers untouched");
    }

    [Fact]
    public void ClearInts_RemovesAllInts_LeavesOtherTypesIntact()
    {
        var p = new AnimationParameters();
        p.SetBool("grounded", true);
        p.SetFloat("speed", 1f);
        p.SetInt("health", 10);
        p.SetTrigger("jump");

        p.ClearInts();

        p.HasInt("health").Should().BeFalse();
        p.GetInt("health").Should().Be(0);
        p.HasBool("grounded").Should().BeTrue("bools untouched");
        p.HasFloat("speed").Should().BeTrue("floats untouched");
        p.IsTriggerArmed("jump").Should().BeTrue("triggers untouched");
    }

    [Fact]
    public void ClearTriggers_DisarmsAllTriggers_LeavesOtherTypesIntact()
    {
        var p = new AnimationParameters();
        p.SetBool("grounded", true);
        p.SetFloat("speed", 1f);
        p.SetInt("health", 10);
        p.SetTrigger("jump");
        p.SetTrigger("attack");

        p.ClearTriggers();

        p.IsTriggerArmed("jump").Should().BeFalse();
        p.IsTriggerArmed("attack").Should().BeFalse();
        p.GetArmedTriggerNames().Should().BeEmpty();
        p.HasBool("grounded").Should().BeTrue("bools untouched");
        p.HasFloat("speed").Should().BeTrue("floats untouched");
        p.HasInt("health").Should().BeTrue("ints untouched");
    }
}