using Brine2D.Animation;
using Brine2D.Core;
using Brine2D.Rendering;
using Brine2D.Rendering.TextureAtlas;
using FluentAssertions;
using NSubstitute;
using System.Runtime.CompilerServices;
using Xunit;

namespace Brine2D.Tests.Animation;

public class AnimationClipTests
{
    private static ITexture MakeTexture(int w = 64, int h = 64)
    {
        var tex = Substitute.For<ITexture>();
        tex.Width.Returns(w);
        tex.Height.Returns(h);
        return tex;
    }

    private static AtlasRegion MakeRegion(string name, ITexture tex, int x, int y, int w, int h)
        => new(name, new Rectangle(x, y, w, h), tex);

    private static AnimationClip MakeClip(string name, int frames, PlaybackMode mode, float frameDuration = 0.1f)
    {
        var clip = new AnimationClip(name) { PlaybackMode = mode };
        for (int i = 0; i < frames; i++)
            clip.AddFrame(new SpriteFrame(new Rectangle(i * 16, 0, 16, 16), frameDuration));
        return clip;
    }

    [Fact]
    public void Constructor_NullName_Throws()
    {
        var act = () => new AnimationClip(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TotalDuration_SumsFrameDurations()
    {
        var clip = new AnimationClip("walk");
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        clip.AddFrame(new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.2f));
        clip.AddFrame(new SpriteFrame(new Rectangle(32, 0, 16, 16), 0.3f));

        clip.TotalDuration.Should().BeApproximately(0.6f, 0.0001f);
    }

    [Fact]
    public void TotalDuration_ReflectsMutatedFrameDuration()
    {
        var frame = new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f);
        var clip = new AnimationClip("walk");
        clip.AddFrame(frame);

        frame.Duration = 0.5f;

        clip.TotalDuration.Should().BeApproximately(0.5f, 0.0001f);
    }

    [Fact]
    public void SpriteFrame_DurationMutation_InvalidatesClipTotalDuration()
    {
        var clip = MakeClip("walk", 3, PlaybackMode.Loop, 0.1f);
        var _ = clip.TotalDuration;

        clip.Frames[1].Duration = 0.5f;

        Assert.Equal(0.1f + 0.5f + 0.1f, clip.TotalDuration, precision: 5);
    }

    [Fact]
    public void SpriteFrame_DurationMutation_AfterRemoveFrame_DoesNotInvalidate()
    {
        var clip = MakeClip("walk", 2, PlaybackMode.Loop, 0.1f);
        var frame = clip.Frames[0];
        clip.RemoveFrame(frame);

        var _ = clip.TotalDuration;
        frame.Duration = 0.9f;

        Assert.Equal(0.1f, clip.TotalDuration, precision: 5);
    }

    [Fact]
    public void SharedSpriteFrame_DurationMutation_InvalidatesBothClips()
    {
        var sharedFrame = new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f);

        var clipA = new AnimationClip("a");
        clipA.AddFrame(sharedFrame);

        var clipB = new AnimationClip("b");
        clipB.AddFrame(sharedFrame);

        var _ = clipA.TotalDuration;
        var __ = clipB.TotalDuration;

        sharedFrame.Duration = 0.5f;

        Assert.Equal(0.5f, clipA.TotalDuration, precision: 5);
        Assert.Equal(0.5f, clipB.TotalDuration, precision: 5);
    }

    [Fact]
    public void SharedSpriteFrame_AbandonedClip_DoesNotPreventGC()
    {
        var sharedFrame = new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f);
        var weakA = CreateAndAbandonClip(sharedFrame);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        Assert.False(weakA.TryGetTarget(out _));

        var ex = Record.Exception(() => sharedFrame.Duration = 0.9f);
        Assert.Null(ex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<AnimationClip> CreateAndAbandonClip(SpriteFrame frame)
    {
        var clip = new AnimationClip("a");
        clip.AddFrame(frame);
        return new WeakReference<AnimationClip>(clip);
    }

    [Fact]
    public void SharedSpriteFrame_MutationAfterOneClipAbandoned_StillInvalidatesLiveClip()
    {
        var sharedFrame = new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f);
        var clipB = new AnimationClip("b");
        clipB.AddFrame(sharedFrame);
        var _ = clipB.TotalDuration;

        var weakA = CreateAndAbandonClip(sharedFrame);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        Assert.False(weakA.TryGetTarget(out var _));

        sharedFrame.Duration = 0.7f;

        Assert.Equal(0.7f, clipB.TotalDuration, precision: 5);
    }

    [Fact]
    public void AddEventAtFrame_FiresAtCorrectFrame()
    {
        var clip = MakeClip("walk", 4, PlaybackMode.Loop, 0.1f);
        int firedAtFrame = -1;
        clip.AddEventAtFrame("step", 2, args => { firedAtFrame = (int)(args.Time / 0.1f + 0.5f); });

        var animator = new SpriteAnimator();
        animator.AddAnimation(clip);
        animator.Play("walk");
        animator.Update(0.21f);

        Assert.Equal(2, firedAtFrame);
    }

    [Fact]
    public void AddEventAtFrame_NegativeIndex_Throws()
    {
        var clip = MakeClip("walk", 3, PlaybackMode.Loop);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            clip.AddEventAtFrame("e", -1, _ => { }));
    }

    [Fact]
    public void AddEventAtFrame_IndexEqualToFrameCount_Throws()
    {
        var clip = MakeClip("walk", 3, PlaybackMode.Loop);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            clip.AddEventAtFrame("e", 3, _ => { }));
    }

    [Fact]
    public void AddEventAtFrame_TimeAutoUpdates_WhenFrameDurationChanges()
    {
        var clip = MakeClip("walk", 4, PlaybackMode.Loop, 0.1f);
        clip.AddEventAtFrame("step", 2, _ => { });
        var originalTime = clip.Events[0].Time;

        clip.Frames[0].Duration = 0.5f;

        Assert.NotEqual(originalTime, clip.Events[0].Time);
        Assert.Equal(0.5f + 0.1f, clip.Events[0].Time, precision: 5);
    }

    [Fact]
    public void AnimationClip_Clone_PreservesClipTint()
    {
        var original = MakeClip("idle", 2, PlaybackMode.Loop);
        original.ClipTint = new Color(255, 128, 0, 200);

        var clone = original.Clone("idle_copy");

        Assert.Equal(original.ClipTint, clone.ClipTint);
    }

    [Fact]
    public void AnimationClip_Clone_NullClipTint_RemainsNull()
    {
        var original = MakeClip("idle", 2, PlaybackMode.Loop);
        var clone = original.Clone("idle_copy");

        Assert.Null(clone.ClipTint);
    }

    [Fact]
    public void AnimationClip_Clone_ClipTintIsIndependent()
    {
        var original = MakeClip("idle", 2, PlaybackMode.Loop);
        original.ClipTint = new Color(255, 0, 0, 255);

        var clone = original.Clone("idle_copy");
        clone.ClipTint = new Color(0, 0, 255, 255);

        Assert.Equal(new Color(255, 0, 0, 255), original.ClipTint);
        Assert.Equal(new Color(0, 0, 255, 255), clone.ClipTint);
    }

    [Fact]
    public void FromSpriteSheet_ProducesCorrectFrameCount()
    {
        var clip = AnimationClip.FromSpriteSheet("walk", 6, 16, 16, 3);

        clip.Frames.Should().HaveCount(6);
    }

    [Fact]
    public void FromSpriteSheet_ProducesCorrectRects()
    {
        var clip = AnimationClip.FromSpriteSheet("walk", 6, 16, 16, 3);

        clip.Frames[0].SourceRect.Should().Be(new Rectangle(0, 0, 16, 16));
        clip.Frames[1].SourceRect.Should().Be(new Rectangle(16, 0, 16, 16));
        clip.Frames[2].SourceRect.Should().Be(new Rectangle(32, 0, 16, 16));
        clip.Frames[3].SourceRect.Should().Be(new Rectangle(0, 16, 16, 16));
    }

    [Fact]
    public void FromSpriteSheet_WithStartOffsets_ProducesCorrectRects()
    {
        var clip = AnimationClip.FromSpriteSheet("run", 3, 16, 16, 4, startX: 16, startY: 32);

        clip.Frames[0].SourceRect.Should().Be(new Rectangle(16, 32, 16, 16));
        clip.Frames[1].SourceRect.Should().Be(new Rectangle(32, 32, 16, 16));
        clip.Frames[2].SourceRect.Should().Be(new Rectangle(48, 32, 16, 16));
    }

    [Fact]
    public void FromSpriteSheet_DefaultsToLoop()
    {
        var clip = AnimationClip.FromSpriteSheet("walk", 16, 16, 4, 4);
        clip.Loop.Should().BeTrue();
    }

    [Fact]
    public void FromAtlasRegions_CreatesOneFramePerRegion()
    {
        var tex = MakeTexture();
        var regions = new[]
        {
            MakeRegion("run_0", tex,  0, 0, 16, 16),
            MakeRegion("run_1", tex, 16, 0, 16, 16),
            MakeRegion("run_2", tex, 32, 0, 16, 16),
        };

        var clip = AnimationClip.FromAtlasRegions("run", regions);

        clip.Frames.Should().HaveCount(3);
    }

    [Fact]
    public void FromAtlasRegions_FrameSourceRectsMatchRegions()
    {
        var tex = MakeTexture();
        var regions = new[]
        {
            MakeRegion("a", tex,  0, 0, 16, 16),
            MakeRegion("b", tex, 16, 0, 32, 24),
        };

        var clip = AnimationClip.FromAtlasRegions("test", regions);

        clip.Frames[0].SourceRect.Should().Be(new Rectangle(0, 0, 16, 16));
        clip.Frames[1].SourceRect.Should().Be(new Rectangle(16, 0, 32, 24));
    }

    [Fact]
    public void FromAtlasRegions_FrameTextureMatchesRegionAtlasTexture()
    {
        var tex = MakeTexture();
        var regions = new[] { MakeRegion("x", tex, 0, 0, 16, 16) };

        var clip = AnimationClip.FromAtlasRegions("test", regions);

        clip.Frames[0].Texture.Should().BeSameAs(tex,
            "each frame must carry its region's AtlasTexture so the renderer uses the correct atlas");
    }

    [Fact]
    public void FromAtlasRegions_DefaultDuration_AppliedToAllFrames()
    {
        var tex = MakeTexture();
        var regions = new[]
        {
            MakeRegion("a", tex,  0, 0, 16, 16),
            MakeRegion("b", tex, 16, 0, 16, 16),
        };

        var clip = AnimationClip.FromAtlasRegions("test", regions, frameDuration: 0.2f);

        clip.Frames.Should().AllSatisfy(f => f.Duration.Should().BeApproximately(0.2f, 0.0001f));
    }

    [Fact]
    public void FromAtlasRegions_DefaultPlaybackMode_IsLoop()
    {
        var tex = MakeTexture();
        var regions = new[] { MakeRegion("a", tex, 0, 0, 16, 16) };

        var clip = AnimationClip.FromAtlasRegions("test", regions);

        clip.PlaybackMode.Should().Be(PlaybackMode.Loop);
    }

    [Fact]
    public void FromAtlasRegions_ExplicitPlaybackMode_IsRespected()
    {
        var tex = MakeTexture();
        var regions = new[] { MakeRegion("a", tex, 0, 0, 16, 16) };

        var clip = AnimationClip.FromAtlasRegions("test", regions, playbackMode: PlaybackMode.OnceHoldLast);

        clip.PlaybackMode.Should().Be(PlaybackMode.OnceHoldLast);
    }

    [Fact]
    public void FromAtlasRegions_EmptySequence_Throws()
    {
        var act = () => AnimationClip.FromAtlasRegions("test", []);

        act.Should().Throw<ArgumentException>().WithMessage("*regions*");
    }

    [Fact]
    public void FromAtlasRegions_NullRegions_Throws()
    {
        var act = () => AnimationClip.FromAtlasRegions("test", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromAtlasRegions_MixedSizeRegions_AllFramesPresent()
    {
        var tex = MakeTexture(128, 128);
        var regions = new[]
        {
            MakeRegion("frame0", tex,  0, 0, 16, 24),
            MakeRegion("frame1", tex, 16, 0, 32, 16),
            MakeRegion("frame2", tex, 48, 0, 12, 12),
        };

        var clip = AnimationClip.FromAtlasRegions("mixed", regions);

        clip.Frames[0].SourceRect.Width.Should().Be(16);
        clip.Frames[1].SourceRect.Width.Should().Be(32);
        clip.Frames[2].SourceRect.Width.Should().Be(12);
    }

    [Fact]
    public void LoopSetter_False_OnPingPong_YieldsPingPongOnce()
    {
        var clip = new AnimationClip("bounce") { PlaybackMode = PlaybackMode.PingPong };

        clip.Loop = false;

        clip.PlaybackMode.Should().Be(PlaybackMode.PingPongOnce,
            "turning Loop off on a PingPong clip must preserve the ping-pong direction via PingPongOnce");
    }

    [Fact]
    public void LoopSetter_False_OnLoop_YieldsOnce()
    {
        var clip = new AnimationClip("walk") { PlaybackMode = PlaybackMode.Loop };

        clip.Loop = false;

        clip.PlaybackMode.Should().Be(PlaybackMode.OnceHoldLast);
    }

    [Fact]
    public void LoopSetter_False_OnNonLooping_IsNoOp()
    {
        var clip = new AnimationClip("attack") { PlaybackMode = PlaybackMode.OnceHoldLast };

        clip.Loop = false;

        clip.PlaybackMode.Should().Be(PlaybackMode.OnceHoldLast,
            "non-looping modes must be left unchanged");
    }

    [Fact]
    public void LoopSetter_True_AlwaysYieldsLoop()
    {
        var clip = new AnimationClip("bounce") { PlaybackMode = PlaybackMode.PingPongOnce };

        clip.Loop = true;

        clip.PlaybackMode.Should().Be(PlaybackMode.Loop);
    }

    [Fact]
    public void LoopGetter_ReturnsTrueForPingPong()
    {
        var clip = new AnimationClip("bounce") { PlaybackMode = PlaybackMode.PingPong };
        clip.Loop.Should().BeTrue();
    }

    [Fact]
    public void LoopGetter_ReturnsFalseForPingPongOnce()
    {
        var clip = new AnimationClip("bounce") { PlaybackMode = PlaybackMode.PingPongOnce };
        clip.Loop.Should().BeFalse();
    }

    [Fact]
    public void SpriteFrame_HitBox_DefaultsToNull()
    {
        var frame = new SpriteFrame(new Rectangle(0, 0, 16, 16));

        frame.HitBox.Should().BeNull();
    }

    [Fact]
    public void SpriteFrame_HitBox_CanBeSetAndRead()
    {
        var frame = new SpriteFrame(new Rectangle(0, 0, 16, 16));
        var hitBox = new Rectangle(2, 2, 12, 12);

        frame.HitBox = hitBox;

        frame.HitBox.Should().Be(hitBox);
    }

    [Fact]
    public void SpriteFrame_UserData_DefaultsToNull()
    {
        var frame = new SpriteFrame(new Rectangle(0, 0, 16, 16));

        frame.UserData.Should().BeNull();
    }

    [Fact]
    public void SpriteFrame_UserData_CanStoreArbitraryObject()
    {
        var frame = new SpriteFrame(new Rectangle(0, 0, 16, 16));
        var payload = new { Damage = 10, Sound = "hit.wav" };

        frame.UserData = payload;

        frame.UserData.Should().BeSameAs(payload);
    }
}