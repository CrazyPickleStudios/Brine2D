using Brine2D.Rendering;
using FluentAssertions;
using NSubstitute;
using System.Numerics;
using Xunit;

namespace Brine2D.Tests.Tilemap;

public sealed class TilemapRendererCullingTests
{
    private static Brine2D.Tilemap.Tilemap MakeMap(int widthTiles = 20, int heightTiles = 20, int tileSize = 16) =>
        new(tileSize, tileSize, widthTiles, heightTiles);

    private static ICamera MakeCamera(float posX, float posY, int viewW, int viewH, float zoom = 1f)
    {
        var camera = Substitute.For<ICamera>();
        camera.Position.Returns(new Vector2(posX, posY));
        camera.ViewportWidth.Returns(viewW);
        camera.ViewportHeight.Returns(viewH);
        camera.Zoom.Returns(zoom);
        return camera;
    }

    [Fact]
    public void GetVisibleTileRange_NullCamera_ReturnsFullMapRange()
    {
        var renderer = new TilemapRenderer();
        var tilemap = MakeMap(10, 8);

        var result = renderer.GetVisibleTileRange(tilemap, null);

        result.Should().Be((0, 0, 9, 7));
    }

    [Fact]
    public void GetVisibleTileRange_NoLayerOffset_ReturnsExpectedRange()
    {
        var renderer = new TilemapRenderer();
        var tilemap = MakeMap(20, 20, 16);
        // Camera centred at (80,60) with a 160x120 viewport exactly covers world [0,160] x [0,120].
        var camera = MakeCamera(posX: 80f, posY: 60f, viewW: 160, viewH: 120);

        var result = renderer.GetVisibleTileRange(tilemap, camera, 0f, 0f);

        // effectiveLeft=0    → minX = max(0, floor(0/16)-1)    = 0
        // effectiveRight=160 → maxX = min(19, floor(160/16)+1) = 11
        // effectiveTop=0     → minY = max(0, floor(0/16)-1)    = 0
        // effectiveBottom=120 → maxY = min(19, floor(120/16)+1) = 8
        result.minX.Should().Be(0);
        result.minY.Should().Be(0);
        result.maxX.Should().Be(11);
        result.maxY.Should().Be(8);
    }

    [Fact]
    public void GetVisibleTileRange_PositiveOffsetX_ShrinksMaxX()
    {
        var renderer = new TilemapRenderer();
        var tilemap = MakeMap(20, 20, 16);
        var camera = MakeCamera(posX: 80f, posY: 60f, viewW: 160, viewH: 120);

        var result = renderer.GetVisibleTileRange(tilemap, camera, layerOffsetX: 32f, layerOffsetY: 0f);

        // effectiveRight = 160 - 32 = 128 → maxX = min(19, floor(128/16)+1) = 9
        // effectiveLeft  =   0 - 32 = -32 → minX = max(0, -2-1) = 0
        result.minX.Should().Be(0);
        result.maxX.Should().Be(9);
    }

    [Fact]
    public void GetVisibleTileRange_NegativeOffsetX_IncreasesMinXAndMaxX()
    {
        var renderer = new TilemapRenderer();
        var tilemap = MakeMap(20, 20, 16);
        var camera = MakeCamera(posX: 80f, posY: 60f, viewW: 160, viewH: 120);

        var result = renderer.GetVisibleTileRange(tilemap, camera, layerOffsetX: -32f, layerOffsetY: 0f);

        // effectiveLeft  = 0 - (-32) = 32  → minX = max(0, floor(32/16)-1) = 1
        // effectiveRight = 160-(-32) = 192 → maxX = min(19, floor(192/16)+1) = 13
        result.minX.Should().Be(1);
        result.maxX.Should().Be(13);
    }

    [Fact]
    public void GetVisibleTileRange_PositiveOffsetY_ShrinksMaxY()
    {
        var renderer = new TilemapRenderer();
        var tilemap = MakeMap(20, 20, 16);
        var camera = MakeCamera(posX: 80f, posY: 60f, viewW: 160, viewH: 120);

        var result = renderer.GetVisibleTileRange(tilemap, camera, layerOffsetX: 0f, layerOffsetY: 32f);

        // effectiveBottom = 120 - 32 = 88 → maxY = min(19, floor(88/16)+1) = 6
        // effectiveTop    =   0 - 32 = -32 → minY = max(0, -2-1) = 0
        result.minY.Should().Be(0);
        result.maxY.Should().Be(6);
    }

    [Fact]
    public void GetVisibleTileRange_ZeroOffset_MatchesNoOffsetOverload()
    {
        var renderer = new TilemapRenderer();
        var tilemap = MakeMap(20, 20, 16);
        var camera = MakeCamera(posX: 80f, posY: 60f, viewW: 160, viewH: 120);

        var withZero = renderer.GetVisibleTileRange(tilemap, camera, 0f, 0f);
        var defaultCall = renderer.GetVisibleTileRange(tilemap, camera);

        withZero.Should().Be(defaultCall);
    }

    [Fact]
    public void GetVisibleTileRange_RangeIsAlwaysClampedToMapBounds()
    {
        var renderer = new TilemapRenderer();
        var tilemap = MakeMap(widthTiles: 5, heightTiles: 5, tileSize: 16);
        // Camera covering a much larger area than the map.
        var camera = MakeCamera(posX: 40f, posY: 40f, viewW: 10000, viewH: 10000);

        var result = renderer.GetVisibleTileRange(tilemap, camera);

        result.minX.Should().BeGreaterThanOrEqualTo(0);
        result.minY.Should().BeGreaterThanOrEqualTo(0);
        result.maxX.Should().BeLessThanOrEqualTo(4);
        result.maxY.Should().BeLessThanOrEqualTo(4);
    }

    [Fact]
    public void GetVisibleTileRange_ParallaxHalf_EffectiveCameraPositionIsHalved()
    {
        var renderer = new TilemapRenderer();
        var tilemap = MakeMap(20, 20, 16);
        // Camera at (160, 120): normally viewport covers [80,240] x [60,180].
        // With parallaxX=0.5, effectiveCamX = 80 → covers [0, 160].
        var camera = MakeCamera(posX: 160f, posY: 120f, viewW: 160, viewH: 120);

        var result = renderer.GetVisibleTileRange(tilemap, camera, 0f, 0f, parallaxX: 0.5f, parallaxY: 0.5f);

        // effectiveCamX = 160*0.5 = 80 → left=0, right=160 → minX=0, maxX=11
        // effectiveCamY = 120*0.5 = 60 → top=0, bottom=120 → minY=0, maxY=8
        result.minX.Should().Be(0);
        result.maxX.Should().Be(11);
        result.minY.Should().Be(0);
        result.maxY.Should().Be(8);
    }

    [Fact]
    public void GetVisibleTileRange_ParallaxZero_AlwaysShowsOriginRegardlessOfCamera()
    {
        var renderer = new TilemapRenderer();
        var tilemap = MakeMap(20, 20, 16);
        // Camera far from origin. A parallax-0 layer is fixed to the screen so its
        // effective camera position is always (0,0) — the viewport always covers the
        // same tile range regardless of where the camera is.
        var camera = MakeCamera(posX: 10000f, posY: 10000f, viewW: 160, viewH: 120);

        var result = renderer.GetVisibleTileRange(tilemap, camera, 0f, 0f, parallaxX: 0f, parallaxY: 0f);

        // effectiveCamX=0, effectiveCamY=0 → same range as a camera at origin
        var origin = renderer.GetVisibleTileRange(tilemap, MakeCamera(0f, 0f, 160, 120), 0f, 0f, 0f, 0f);
        result.Should().Be(origin);
    }

    [Fact]
    public void GetVisibleTileRange_ParallaxOne_BehavesIdenticallyToNoPreviousParallax()
    {
        var renderer = new TilemapRenderer();
        var tilemap = MakeMap(20, 20, 16);
        var camera = MakeCamera(posX: 80f, posY: 60f, viewW: 160, viewH: 120);

        var withParallax = renderer.GetVisibleTileRange(tilemap, camera, 0f, 0f, parallaxX: 1f, parallaxY: 1f);
        var withoutParallax = renderer.GetVisibleTileRange(tilemap, camera, 0f, 0f);

        withParallax.Should().Be(withoutParallax);
    }
}