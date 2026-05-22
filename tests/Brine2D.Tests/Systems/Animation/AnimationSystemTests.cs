using Brine2D.Animation;
using Brine2D.Core;
using Brine2D.Systems.Animation;
using Brine2D.Systems.Rendering;
using FluentAssertions;

namespace Brine2D.Tests.Systems.Animation;

public class AnimationSystemTests : TestBase
{
    private static AnimationClip MakeClip(string name, int frameCount, float frameDuration = 0.1f)
    {
        var clip = new AnimationClip(name);
        for (int i = 0; i < frameCount; i++)
            clip.AddFrame(new SpriteFrame(new Rectangle(i * 16, 0, 16, 16), frameDuration));
        return clip;
    }

    [Fact]
    public void Update_WritesSpriteSourceRect()
    {
        var world = CreateTestWorld();
        world.CreateEntity()
            .AddComponent<SpriteComponent>()
            .AddComponent<AnimatorComponent>(a =>
            {
                a.Animator.AddAnimation(MakeClip("walk", 4));
                a.Animator.Play("walk");
            });

        world.Flush();

        var system = new AnimationSystem();
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        var entity = world.GetEntitiesWithComponent<AnimatorComponent>().First();
        entity.GetComponent<SpriteComponent>()!.SourceRect!.Value.X.Should().Be(16);
    }

    [Fact]
    public void Update_IsTimeClamped_SkipsUpdate()
    {
        var world = CreateTestWorld();
        world.CreateEntity()
            .AddComponent<SpriteComponent>()
            .AddComponent<AnimatorComponent>(a =>
            {
                a.Animator.AddAnimation(MakeClip("walk", 4, frameDuration: 0.1f));
                a.Animator.Play("walk");
            });

        world.Flush();

        var system = new AnimationSystem();
        var clampedTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(10), IsTimeClamped: true);
        system.Update(world, clampedTime);

        var entity = world.GetEntitiesWithComponent<AnimatorComponent>().First();
        entity.GetComponent<SpriteComponent>()!.SourceRect.Should().BeNull();
    }

    [Fact]
    public void Update_NoAnimatorComponent_DoesNotThrow()
    {
        var world = CreateTestWorld();
        world.CreateEntity().AddComponent<SpriteComponent>();
        world.Flush();

        var system = new AnimationSystem();
        var act = () => system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        act.Should().NotThrow();
    }

    [Fact]
    public void Update_NoSpriteComponent_DoesNotThrow()
    {
        var world = CreateTestWorld();
        world.CreateEntity()
            .AddComponent<AnimatorComponent>(a =>
            {
                a.Animator.AddAnimation(MakeClip("walk", 4));
                a.Animator.Play("walk");
            });

        world.Flush();

        var system = new AnimationSystem();
        var act = () => system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        act.Should().NotThrow();
    }

    [Fact]
    public void CrossFade_RestoresOriginalTintAlpha_WhenFadeCompletes()
    {
        var world = CreateTestWorld();
        world.CreateEntity()
            .AddComponent<SpriteComponent>(s => s.Tint = Color.White.WithAlpha(0.5f))
            .AddComponent<AnimatorComponent>(a =>
            {
                a.Animator.AddAnimation(MakeClip("idle", 3));
                a.Animator.AddAnimation(MakeClip("walk", 3));
                a.Animator.Play("idle");
            });

        world.Flush();

        var entity = world.GetEntitiesWithComponent<AnimatorComponent>().First();
        var sprite = entity.GetComponent<SpriteComponent>()!;
        var animComp = entity.GetComponent<AnimatorComponent>()!;
        var system = new AnimationSystem();

        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        animComp.Animator.PlayWithCrossFade("walk", fadeDuration: 0.1f);
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.06)));
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.06)));

        sprite.CrossFadeGhosts.Should().BeEmpty();
        (sprite.Tint.A / 255f).Should().BeApproximately(0.5f, precision: 0.02f);
    }

    [Fact]
    public void CrossFade_SetsGhostTexturePath_WhenOutgoingClipUsesPathOnly()
    {
        var world = CreateTestWorld();

        var idleClip = new AnimationClip("idle") { TexturePath = "sprites/idle.png" };
        idleClip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));

        world.CreateEntity()
            .AddComponent<SpriteComponent>(s => s.TexturePath = "sprites/idle.png")
            .AddComponent<AnimatorComponent>(a =>
            {
                a.Animator.AddAnimation(idleClip);
                a.Animator.AddAnimation(MakeClip("walk", 3));
                a.Animator.Play("idle");
            });

        world.Flush();

        var entity = world.GetEntitiesWithComponent<AnimatorComponent>().First();
        var sprite = entity.GetComponent<SpriteComponent>()!;
        var animComp = entity.GetComponent<AnimatorComponent>()!;
        var system = new AnimationSystem();

        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        animComp.Animator.PlayWithCrossFade("walk", fadeDuration: 0.2f);
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.05)));

        sprite.CrossFadeGhosts.Should().ContainSingle();
        sprite.CrossFadeGhosts[0].Texture.Should().BeNull();
        sprite.CrossFadeGhosts[0].TexturePath.Should().Be("sprites/idle.png");
    }

    [Fact]
    public void AdditiveLayer_ApplyFrame_WritesDrawOffset()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        var trimmedOffset = new System.Numerics.Vector2(4f, 8f);
        var layerClip = new AnimationClip("fx-layer-clip") { PlaybackMode = PlaybackMode.Loop };
        layerClip.AddFrame(new SpriteFrame(new Rectangle(0, 0, 32, 32), 0.1f) { DrawOffset = trimmedOffset });

        var animComp = new AnimatorComponent();
        var baseClip = MakeClip("base", 2);
        animComp.Animator.AddAnimation(baseClip);
        animComp.Animator.Play("base");
        entity.AddComponent(animComp);

        var sprite = new SpriteComponent();
        entity.AddComponent(sprite);
        world.Flush();

        var layer = animComp.AddLayer("fx-layer", priority: 1);
        layer.BlendMode = AnimationLayerBlendMode.Additive;
        layer.Mask = AnimationLayerMask.SourceRect;
        layer.Animator.AddAnimation(layerClip);
        layer.Animator.Play("fx-layer-clip");

        var system = new AnimationSystem();
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.05)));

        sprite.Offset.Should().Be(trimmedOffset);
    }

    [Fact]
    public void BlendSelector_EvaluatedBeforeAnimatorUpdate_SameFrameEffect()
    {
        var world = CreateTestWorld();
        world.CreateEntity()
            .AddComponent<SpriteComponent>()
            .AddComponent<AnimatorComponent>(a =>
            {
                a.Animator.AddAnimation(MakeClip("idle", 2, frameDuration: 0.2f));
                a.Animator.AddAnimation(MakeClip("walk", 2, frameDuration: 0.2f));
                a.BlendSelector1D = new AnimationBlendSelector1D(a.Animator);
                a.BlendSelector1D.AddNode(0f, "idle");
                a.BlendSelector1D.AddNode(1f, "walk");
                a.BlendSelector1D.Value = 0f;
            });

        world.Flush();

        var entity = world.GetEntitiesWithComponent<AnimatorComponent>().First();
        var animComp = entity.GetComponent<AnimatorComponent>()!;
        var system = new AnimationSystem();

        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.05)));

        animComp.Animator.CurrentAnimation!.Name.Should().Be("idle");

        animComp.BlendSelector1D!.Value = 1f;

        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.05)));

        animComp.Animator.CurrentAnimation!.Name.Should().Be("walk");
    }

    [Fact]
    public void Layer_BlendSelector_EvaluatedBeforeLayerAnimatorUpdate()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        var animComp = new AnimatorComponent();
        animComp.Animator.AddAnimation(MakeClip("base", 2));
        animComp.Animator.Play("base");
        entity.AddComponent(animComp);
        entity.AddComponent(new SpriteComponent());
        world.Flush();

        var layer = animComp.AddLayer("overlay", priority: 1);
        layer.Animator.AddAnimation(MakeClip("fx-a", 2, frameDuration: 0.2f));
        layer.Animator.AddAnimation(MakeClip("fx-b", 2, frameDuration: 0.2f));

        layer.BlendSelector1D = new AnimationBlendSelector1D(layer.Animator);
        layer.BlendSelector1D.AddNode(0f, "fx-a");
        layer.BlendSelector1D.AddNode(1f, "fx-b");
        layer.BlendSelector1D.Value = 0f;

        var system = new AnimationSystem();
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.05)));

        layer.Animator.CurrentAnimation!.Name.Should().Be("fx-a");

        layer.BlendSelector1D.Value = 1f;
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.05)));

        layer.Animator.CurrentAnimation!.Name.Should().Be("fx-b");
    }
}