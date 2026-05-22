using Brine2D.Animation;
using Brine2D.Core;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Animation;

public class AsepriteClipLoaderTests
{
    private static string Frame(int x, int y, int w, int h, int ms, string? data = null)
    {
        var d = data != null ? $",\"data\":\"{data}\"" : "";
        return $"{{\"frame\":{{\"x\":{x},\"y\":{y},\"w\":{w},\"h\":{h}}},\"duration\":{ms}{d}}}";
    }

    private static string TrimmedFrame(int fx, int fy, int fw, int fh, int ms,
        int ssx, int ssy, int ssw, int ssh, int srcW, int srcH)
        => $"{{\"frame\":{{\"x\":{fx},\"y\":{fy},\"w\":{fw},\"h\":{fh}}},"
         + $"\"duration\":{ms},"
         + $"\"spriteSourceSize\":{{\"x\":{ssx},\"y\":{ssy},\"w\":{ssw},\"h\":{ssh}}},"
         + $"\"sourceSize\":{{\"w\":{srcW},\"h\":{srcH}}}}}";

    private static string FrameWithSourceSize(int fx, int fy, int fw, int fh, int ms, int srcW, int srcH)
        => $"{{\"frame\":{{\"x\":{fx},\"y\":{fy},\"w\":{fw},\"h\":{fh}}},"
         + $"\"duration\":{ms},"
         + $"\"sourceSize\":{{\"w\":{srcW},\"h\":{srcH}}}}}";

    private static string Tag(string name, int from, int to, string dir = "forward",
        int repeat = 0, string? data = null, string? color = null)
    {
        var r = repeat > 0 ? $",\"repeat\":{repeat}" : "";
        var d = data != null ? $",\"data\":\"{data}\"" : "";
        var c = color != null ? $",\"color\":\"{color}\"" : "";
        return $"{{\"name\":\"{name}\",\"from\":{from},\"to\":{to},\"direction\":\"{dir}\"{r}{d}{c}}}";
    }

    private static string Slice(string name, int frame, int bx, int by, int bw, int bh,
        int? pivotX = null, int? pivotY = null)
    {
        var pivot = pivotX.HasValue
            ? $",\"pivot\":{{\"x\":{pivotX},\"y\":{pivotY}}}"
            : "";
        return $"{{\"name\":\"{name}\",\"keys\":[{{\"frame\":{frame},"
             + $"\"bounds\":{{\"x\":{bx},\"y\":{by},\"w\":{bw},\"h\":{bh}}}{pivot}}}]}}";
    }

    private static string Build(string frames, string? tags = null, string? slices = null,
        bool arrayFormat = true)
    {
        var tagsPart = tags != null ? $"\"frameTags\":[{tags}]" : "\"frameTags\":[]";
        var slicesPart = slices != null ? $",\"slices\":[{slices}]" : "";
        var meta = $"{{{tagsPart}{slicesPart}}}";
        var framesBlock = arrayFormat ? $"[{frames}]" : $"{{{frames}}}";
        return $"{{\"frames\":{framesBlock},\"meta\":{meta}}}";
    }

    [Fact]
    public void ParseJson_ArrayFormat_ParsesFrameRectsAndDurations()
    {
        var json = Build(Frame(0, 0, 16, 16, 100) + "," + Frame(16, 0, 16, 16, 200));

        var clips = new AsepriteClipLoader().ParseJson(json, defaultClipName: "idle");

        clips.Should().HaveCount(1);
        clips[0].Frames.Should().HaveCount(2);
        clips[0].Frames[0].SourceRect.X.Should().Be(0);
        clips[0].Frames[0].Duration.Should().BeApproximately(0.1f, 0.001f);
        clips[0].Frames[1].SourceRect.X.Should().Be(16);
        clips[0].Frames[1].Duration.Should().BeApproximately(0.2f, 0.001f);
    }

    [Fact]
    public void ParseJson_HashFormat_ParsesFramesSortedByIndex()
    {
        var f0 = $"\"walk 0.aseprite\":{Frame(0, 0, 16, 16, 100)}";
        var f1 = $"\"walk 1.aseprite\":{Frame(16, 0, 16, 16, 200)}";
        var json = Build(f1 + "," + f0, arrayFormat: false);

        var clips = new AsepriteClipLoader().ParseJson(json, defaultClipName: "walk");

        clips.Should().HaveCount(1);
        clips[0].Frames.Should().HaveCount(2);
        clips[0].Frames[0].SourceRect.X.Should().Be(0);
        clips[0].Frames[1].SourceRect.X.Should().Be(16);
    }

    [Fact]
    public void ParseJson_NoFrameTags_CreatesSingleClipWithDefaultName()
    {
        var json = Build(Frame(0, 0, 16, 16, 100));

        var clips = new AsepriteClipLoader().ParseJson(json, defaultClipName: "myClip");

        clips.Should().HaveCount(1);
        clips[0].Name.Should().Be("myClip");
    }

    [Fact]
    public void ParseJson_ForwardTag_ProducesLoopClip()
    {
        var json = Build(
            Frame(0, 0, 16, 16, 100) + "," + Frame(16, 0, 16, 16, 100),
            Tag("walk", 0, 1, "forward"));

        var clips = new AsepriteClipLoader().ParseJson(json);

        clips[0].PlaybackMode.Should().Be(PlaybackMode.Loop);
    }

    [Fact]
    public void ParseJson_ReverseTag_StoresFramesInReverseOrder()
    {
        var json = Build(
            Frame(0, 0, 16, 16, 100) + "," + Frame(16, 0, 16, 16, 100),
            Tag("walk", 0, 1, "reverse"));

        var clips = new AsepriteClipLoader().ParseJson(json);

        clips[0].Frames[0].SourceRect.X.Should().Be(16);
        clips[0].Frames[1].SourceRect.X.Should().Be(0);
        clips[0].PlaybackMode.Should().Be(PlaybackMode.Loop);
    }

    [Fact]
    public void ParseJson_PingPongTag_ProducesPingPongClip()
    {
        var json = Build(
            Frame(0, 0, 16, 16, 100) + "," + Frame(16, 0, 16, 16, 100),
            Tag("walk", 0, 1, "pingpong"));

        var clips = new AsepriteClipLoader().ParseJson(json);

        clips[0].PlaybackMode.Should().Be(PlaybackMode.PingPong);
        clips[0].UserData.Should().NotBe(AsepriteClipLoader.PingPongReverseTag);
    }

    [Fact]
    public void ParseJson_PingPongReverseTag_SetsPingPongAndUserData()
    {
        var json = Build(
            Frame(0, 0, 16, 16, 100) + "," + Frame(16, 0, 16, 16, 100),
            Tag("walk", 0, 1, "pingpong_reverse"));

        var clips = new AsepriteClipLoader().ParseJson(json);

        clips[0].PlaybackMode.Should().Be(PlaybackMode.PingPong);
        clips[0].UserData.Should().Be(AsepriteClipLoader.PingPongReverseTag);
    }

    [Fact]
    public void ParseJson_RepeatField_MapsToRepeatCount()
    {
        var json = Build(Frame(0, 0, 16, 16, 100), Tag("walk", 0, 0, repeat: 3));

        var clips = new AsepriteClipLoader().ParseJson(json);

        clips[0].RepeatCount.Should().Be(3);
    }

    [Fact]
    public void ParseJson_TagData_MapsToClipUserData()
    {
        var json = Build(Frame(0, 0, 16, 16, 100), Tag("walk", 0, 0, data: "my-tag-data"));

        var clips = new AsepriteClipLoader().ParseJson(json);

        clips[0].UserData.Should().Be("my-tag-data");
    }

    [Fact]
    public void ParseJson_FrameData_MapsToFrameUserData()
    {
        var json = Build(Frame(0, 0, 16, 16, 100, data: "frame-event"));

        var clips = new AsepriteClipLoader().ParseJson(json, defaultClipName: "idle");

        clips[0].Frames[0].UserData.Should().Be("frame-event");
    }

    [Fact]
    public void ParseJson_TrimmedFrame_SetsDrawOffset()
    {
        var json = Build(TrimmedFrame(0, 0, 12, 12, 100, ssx: 4, ssy: 8, ssw: 12, ssh: 12, srcW: 16, srcH: 16));

        var clips = new AsepriteClipLoader().ParseJson(json, defaultClipName: "idle");

        clips[0].Frames[0].DrawOffset.X.Should().BeApproximately(4f, 0.001f);
        clips[0].Frames[0].DrawOffset.Y.Should().BeApproximately(8f, 0.001f);
    }

    [Fact]
    public void ParseJson_UntrimmedFrame_DrawOffsetIsZero()
    {
        var json = Build(TrimmedFrame(0, 0, 16, 16, 100, ssx: 0, ssy: 0, ssw: 16, ssh: 16, srcW: 16, srcH: 16));

        var clips = new AsepriteClipLoader().ParseJson(json, defaultClipName: "idle");

        clips[0].Frames[0].DrawOffset.X.Should().BeApproximately(0f, 0.001f);
        clips[0].Frames[0].DrawOffset.Y.Should().BeApproximately(0f, 0.001f);
    }

    [Fact]
    public void ParseJson_HitboxSlice_MapsToFrameHitBox()
    {
        var json = Build(Frame(0, 0, 16, 16, 100), slices: Slice("hitbox", 0, 2, 2, 8, 8));

        var clips = new AsepriteClipLoader().ParseJson(json, defaultClipName: "idle");

        clips[0].Frames[0].HitBox.Should().NotBeNull();
        clips[0].Frames[0].HitBox!.Value.X.Should().Be(2);
        clips[0].Frames[0].HitBox!.Value.Width.Should().Be(8);
    }

    [Fact]
    public void ParseJson_NamedExtraSlice_StoredAsNamedHitBox()
    {
        var json = Build(Frame(0, 0, 16, 16, 100), slices: Slice("hurtbox", 0, 1, 1, 6, 6));

        var clips = new AsepriteClipLoader().ParseJson(json, defaultClipName: "idle");

        var hurtbox = clips[0].Frames[0].TryGetHitBox("hurtbox");
        hurtbox.Should().NotBeNull();
        hurtbox!.Value.Width.Should().Be(6);
    }

    [Fact]
    public void ParseJson_PivotOnHitboxSlice_MapsToFrameOrigin()
    {
        var json = Build(
            FrameWithSourceSize(0, 0, 16, 16, 100, 16, 16),
            slices: Slice("hitbox", 0, 0, 0, 16, 16, pivotX: 8, pivotY: 8));

        var clips = new AsepriteClipLoader().ParseJson(json, defaultClipName: "idle");

        clips[0].Frames[0].Origin.X.Should().BeApproximately(0.5f, 0.001f);
        clips[0].Frames[0].Origin.Y.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void ParseJson_TagColor_MapsToClipTint()
    {
        var json = Build(Frame(0, 0, 16, 16, 100), Tag("walk", 0, 0, color: "#FF8000"));

        var clips = new AsepriteClipLoader().ParseJson(json);

        clips[0].ClipTint.Should().NotBeNull();
        clips[0].ClipTint!.Value.R.Should().Be(255);
        clips[0].ClipTint!.Value.G.Should().Be(128);
        clips[0].ClipTint!.Value.B.Should().Be(0);
    }

    [Fact]
    public void ParseJson_TexturePath_ForwardedToClip_WhenNoTexture()
    {
        var json = Build(Frame(0, 0, 16, 16, 100));

        var clips = new AsepriteClipLoader().ParseJson(json, texturePath: "sprites/hero.png", defaultClipName: "idle");

        clips[0].TexturePath.Should().Be("sprites/hero.png");
        clips[0].Texture.Should().BeNull();
    }

    [Fact]
    public void ParseJson_InvalidJson_Throws()
    {
        var act = () => new AsepriteClipLoader().ParseJson("not-json");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ParseJson_MultipleTagsProduceMultipleClips()
    {
        var json = Build(
            Frame(0, 0, 16, 16, 100) + "," +
            Frame(16, 0, 16, 16, 100) + "," +
            Frame(32, 0, 16, 16, 100),
            Tag("idle", 0, 0) + "," + Tag("walk", 1, 2));

        var clips = new AsepriteClipLoader().ParseJson(json);

        clips.Should().HaveCount(2);
        clips[0].Name.Should().Be("idle");
        clips[1].Name.Should().Be("walk");
        clips[1].Frames.Should().HaveCount(2);
    }

    [Fact]
    public void ConfigureAnimator_AddsAllClipsToAnimator()
    {
        var json = Build(
            Frame(0, 0, 16, 16, 100) + "," + Frame(16, 0, 16, 16, 100),
            Tag("idle", 0, 0) + "," + Tag("walk", 1, 1));

        var loader = new AsepriteClipLoader();
        var clips = loader.ParseJson(json);
        var animator = new SpriteAnimator();
        loader.ConfigureAnimator(animator, clips);

        animator.AnimationNames.Should().Contain("idle");
        animator.AnimationNames.Should().Contain("walk");
    }

    [Fact]
    public void ConfigureAnimator_PingPongReverse_WiresReversedOnEnterExit()
    {
        var json = Build(
            Frame(0, 0, 16, 16, 100) + "," + Frame(16, 0, 16, 16, 100),
            Tag("walk", 0, 1, "pingpong_reverse"));

        var loader = new AsepriteClipLoader();
        var clips = loader.ParseJson(json);
        var animator = new SpriteAnimator();
        loader.ConfigureAnimator(animator, clips);

        animator.Play("walk");
        animator.Reversed.Should().BeTrue();

        animator.Stop();
        animator.Reversed.Should().BeFalse();
    }

    [Fact]
    public void ConfigureAnimator_CalledTwice_DoesNotDoubleSubscribe()
    {
        var json = Build(
            Frame(0, 0, 16, 16, 100),
            Tag("walk", 0, 0, "pingpong_reverse"));

        var loader = new AsepriteClipLoader();
        var clips = loader.ParseJson(json);
        var animator = new SpriteAnimator();
        loader.ConfigureAnimator(animator, clips);
        loader.ConfigureAnimator(animator, clips);

        animator.Play("walk");
        animator.Reversed.Should().BeTrue();

        animator.Stop();
        animator.Reversed.Should().BeFalse();
    }

    [Fact]
    public void ParseJson_SliceKeyAppliesToSubsequentFrames_UntilNextKey()
    {
        var twoKeys =
            "{\"name\":\"hitbox\",\"keys\":["
            + $"{{\"frame\":0,\"bounds\":{{\"x\":1,\"y\":1,\"w\":8,\"h\":8}}}},"
            + $"{{\"frame\":2,\"bounds\":{{\"x\":3,\"y\":3,\"w\":4,\"h\":4}}}}"
            + "]}";

        var json = Build(
            Frame(0, 0, 16, 16, 100) + "," +
            Frame(16, 0, 16, 16, 100) + "," +
            Frame(32, 0, 16, 16, 100),
            slices: twoKeys);

        var clips = new AsepriteClipLoader().ParseJson(json, defaultClipName: "walk");

        clips[0].Frames[0].HitBox!.Value.X.Should().Be(1);
        clips[0].Frames[1].HitBox!.Value.X.Should().Be(1);
        clips[0].Frames[2].HitBox!.Value.X.Should().Be(3);
    }

    [Fact]
    public void ConfigureAnimator_TwoAnimators_EachGetIndependentCallbacks()
    {
        var clip = new AnimationClip("pp_rev") { PlaybackMode = PlaybackMode.PingPong };
        clip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        clip.UserData = AsepriteClipLoader.PingPongReverseTag;

        var animator1 = new SpriteAnimator();
        var animator2 = new SpriteAnimator();
        var idleClip1 = new AnimationClip("idle") { PlaybackMode = PlaybackMode.Loop };
        idleClip1.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        var idleClip2 = new AnimationClip("idle") { PlaybackMode = PlaybackMode.Loop };
        idleClip2.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        animator1.AddAnimation(idleClip1);
        animator2.AddAnimation(idleClip2);

        var loader = new AsepriteClipLoader();
        loader.ConfigureAnimator(animator1, [clip]);
        loader.ConfigureAnimator(animator2, [clip]);

        animator1.Play("pp_rev");
        animator1.Reversed.Should().BeTrue();
        animator2.Reversed.Should().BeFalse("animator2 should not be affected by animator1 playing the clip");

        animator1.Play("idle");
        animator1.Reversed.Should().BeFalse();
        animator2.Reversed.Should().BeFalse();
    }
}