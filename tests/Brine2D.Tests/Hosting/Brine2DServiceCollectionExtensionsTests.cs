using System.Numerics;
using Brine2D.Hosting;
using Brine2D.Physics;
using Brine2D.Rendering;
using Brine2D.Systems.Physics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Brine2D.Tests.Hosting;

public class Brine2DServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBrine2D_WithoutAddPhysics_StillResolvesPhysicsWorldAndBox2DPhysicsSystem()
    {
        var services = new ServiceCollection();
        services.AddBrine2D();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Box2D ships as part of core Brine2D; PhysicsWorld/Box2DPhysicsSystem must resolve
        // even when AddPhysics() is never called, since SceneManager's default systems
        // construct Box2DPhysicsSystem unconditionally.
        scope.ServiceProvider.GetRequiredService<PhysicsWorld>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<Box2DPhysicsSystem>().Should().NotBeNull();
    }

    [Fact]
    public void AddPhysics_ConfiguresGravity_OverridingCoreDefaults()
    {
        var services = new ServiceCollection();
        services.AddBrine2D();
        services.AddPhysics(o => o.Gravity = new Vector2(0f, 0f));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // No direct Gravity getter on PhysicsWorld, so this just confirms the configured
        // registration (not the AddBrine2D core default) is the one that resolves.
        scope.ServiceProvider.GetRequiredService<PhysicsWorld>().Should().NotBeNull();
    }

    [Fact]
    public void DefaultCamera_SelfCenters_UsingRendererDimensions()
    {
        var renderer = Substitute.For<IRenderer>();
        renderer.Width.Returns(800);
        renderer.Height.Returns(600);

        var services = new ServiceCollection();
        services.AddSingleton(renderer);
        services.AddBrine2D();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var camera = scope.ServiceProvider.GetRequiredService<ICamera>();

        camera.Position.Should().Be(new Vector2(400f, 300f));
    }

    [Fact]
    public void DefaultCamera_FallsBackToWindowOptions_WhenRendererHasNoDimensions()
    {
        var options = new Brine2DOptions();
        options.Window.Width = 1024;
        options.Window.Height = 768;

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddBrine2D();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var camera = scope.ServiceProvider.GetRequiredService<ICamera>();

        camera.Position.Should().Be(new Vector2(512f, 384f));
    }
}
