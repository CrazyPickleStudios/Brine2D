using Brine2D.Tilemap;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Tilemap;

public sealed class TilemapAnimatorTests
{
    private static Tileset MakeAnimatedTileset(int firstGid, int ownerLocalId, params (int localId, int durationMs)[] frames)
    {
        var tileset = new Tileset
        {
            FirstGid = firstGid,
            TileWidth = 16,
            TileHeight = 16,
            Columns = 4,
            Rows = 4,
        };

        var ownerGid = ownerLocalId + firstGid;
        var animFrames = frames
            .Select(f => new TileAnimationFrame(f.localId + firstGid, f.durationMs))
            .ToList();

        tileset.Animations[ownerGid] = new TileAnimation(ownerGid, animFrames);
        return tileset;
    }

    private static Brine2D.Tilemap.Tilemap MakeMapWithTileset(Tileset tileset)
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.AddTileset(tileset);
        return map;
    }

    [Fact]
    public void HasAnimations_AfterInitializeWithAnimatedTileset_ReturnsTrue()
    {
        var tileset = MakeAnimatedTileset(1, ownerLocalId: 0, (0, 200), (1, 200));
        var map = MakeMapWithTileset(tileset);
        var animator = new TilemapAnimator();

        animator.Initialize(map);

        animator.HasAnimations.Should().BeTrue();
    }

    [Fact]
    public void HasAnimations_AfterInitializeWithNoAnimations_ReturnsFalse()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.AddTileset(new Tileset { FirstGid = 1, TileWidth = 16, TileHeight = 16, Columns = 4, Rows = 4 });
        var animator = new TilemapAnimator();

        animator.Initialize(map);

        animator.HasAnimations.Should().BeFalse();
    }

    [Fact]
    public void ResolveGid_NonAnimatedTile_ReturnsOriginalGid()
    {
        var tileset = MakeAnimatedTileset(1, ownerLocalId: 0, (0, 200), (1, 200));
        var animator = new TilemapAnimator();
        animator.Initialize(MakeMapWithTileset(tileset));

        animator.ResolveGid(5).Should().Be(5);
    }

    [Fact]
    public void ResolveGid_BeforeAnyUpdate_ReturnsFirstFrame()
    {
        var tileset = MakeAnimatedTileset(1, ownerLocalId: 0, (0, 200), (1, 300));
        var animator = new TilemapAnimator();
        animator.Initialize(MakeMapWithTileset(tileset));

        animator.ResolveGid(1).Should().Be(1);
    }

    [Fact]
    public void ResolveGid_AfterUpdatePastFirstFrameDuration_ReturnsSecondFrame()
    {
        var tileset = MakeAnimatedTileset(1, ownerLocalId: 0, (0, 200), (1, 300));
        var animator = new TilemapAnimator();
        animator.Initialize(MakeMapWithTileset(tileset));

        animator.Update(0.25f);

        animator.ResolveGid(1).Should().Be(2);
    }

    [Fact]
    public void ResolveGid_AfterFullLoopElapsed_WrapsBackToFirstFrame()
    {
        var tileset = MakeAnimatedTileset(1, ownerLocalId: 0, (0, 200), (1, 200));
        var animator = new TilemapAnimator();
        animator.Initialize(MakeMapWithTileset(tileset));

        animator.Update(0.4f);

        animator.ResolveGid(1).Should().Be(1);
    }

    [Fact]
    public void ResolveGid_ExactlyAtFrameBoundary_ReturnsNextFrame()
    {
        var tileset = MakeAnimatedTileset(1, ownerLocalId: 0, (0, 200), (1, 200));
        var animator = new TilemapAnimator();
        animator.Initialize(MakeMapWithTileset(tileset));

        animator.Update(0.2f);

        animator.ResolveGid(1).Should().Be(2);
    }

    [Fact]
    public void Initialize_CalledTwice_ResetsElapsedTime()
    {
        var tileset = MakeAnimatedTileset(1, ownerLocalId: 0, (0, 200), (1, 200));
        var map = MakeMapWithTileset(tileset);
        var animator = new TilemapAnimator();

        animator.Initialize(map);
        animator.Update(0.25f);
        animator.Initialize(map);

        animator.ResolveGid(1).Should().Be(1);
    }

    [Fact]
    public void Update_WithZeroDelta_DoesNotAdvanceAnimation()
    {
        var tileset = MakeAnimatedTileset(1, ownerLocalId: 0, (0, 200), (1, 200));
        var animator = new TilemapAnimator();
        animator.Initialize(MakeMapWithTileset(tileset));

        animator.Update(0f);

        animator.ResolveGid(1).Should().Be(1);
    }

    [Fact]
    public void Update_AfterManyFrames_ElapsedDoesNotGrowWithoutBound()
    {
        // Two-frame animation with 200ms each = 400ms total.
        // After enough updates to accumulate >> TotalDurationMs, elapsed should still be < 400.
        var tileset = MakeAnimatedTileset(1, ownerLocalId: 0, (0, 200), (1, 200));
        var animator = new TilemapAnimator();
        animator.Initialize(MakeMapWithTileset(tileset));

        // Simulate ~1 hour of gameplay at 60fps
        for (int i = 0; i < 216_000; i++)
            animator.Update(1f / 60f);

        // If elapsed wraps correctly the animation still resolves to a valid frame.
        var resolved = animator.ResolveGid(1);
        resolved.Should().BeOneOf(1, 2);
    }

    [Fact]
    public void Update_ElapsedWrapsAtTotalDuration_AnimationLoops()
    {
        // 200ms + 200ms = 400ms total. After exactly 400ms the animator should be back to frame 0.
        var tileset = MakeAnimatedTileset(1, ownerLocalId: 0, (0, 200), (1, 200));
        var animator = new TilemapAnimator();
        animator.Initialize(MakeMapWithTileset(tileset));

        // Advance exactly one full loop (400ms = 0.4s).
        animator.Update(0.4f);

        // Back at the start → GID 1 (frame 0).
        animator.ResolveGid(1).Should().Be(1);
    }
}
