using Brine2D.Collision;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;

namespace Brine2D.Tests.ECS.Components;

public class ColliderComponentShouldCollideTests : TestBase
{
    [Fact]
    public void ShouldCollide_DefaultIsNull()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        Assert.Null(collider.ShouldCollide);
    }

    [Fact]
    public void ShouldCollide_CanBeSet()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.ShouldCollide = _ => true;

        Assert.NotNull(collider.ShouldCollide);
    }

    [Fact]
    public void ShouldCollide_ReturnsTrueWhenDelegateAllows()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var collider1 = entity1.GetComponent<PhysicsBodyComponent>()!;
        var collider2 = entity2.GetComponent<PhysicsBodyComponent>()!;

        collider1.ShouldCollide = _ => true;

        Assert.True(collider1.ShouldCollide(collider2));
    }

    [Fact]
    public void ShouldCollide_ReturnsFalseWhenDelegateRejects()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var collider1 = entity1.GetComponent<PhysicsBodyComponent>()!;
        var collider2 = entity2.GetComponent<PhysicsBodyComponent>()!;

        collider1.ShouldCollide = _ => false;

        Assert.False(collider1.ShouldCollide(collider2));
    }

    [Fact]
    public void ShouldCollide_ClearedOnRemoved()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var collider = entity.GetComponent<PhysicsBodyComponent>()!;
        collider.ShouldCollide = _ => true;

        entity.RemoveComponent<PhysicsBodyComponent>();

        Assert.Null(collider.ShouldCollide);
    }

    [Fact]
    public void Restitution_ClampedTo0_1()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.Restitution = 2f;
        Assert.Equal(1f, collider.Restitution);

        collider.Restitution = -1f;
        Assert.Equal(0f, collider.Restitution);
    }

    [Fact]
    public void SurfaceFriction_ClampedTo0_1()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.SurfaceFriction = 5f;
        Assert.Equal(1f, collider.SurfaceFriction);

        collider.SurfaceFriction = -1f;
        Assert.Equal(0f, collider.SurfaceFriction);
    }

    [Fact]
    public void Mass_ZeroOrNegative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => collider.Mass = 0f);
        Assert.Throws<ArgumentOutOfRangeException>(() => collider.Mass = -1f);
    }

    [Fact]
    public void LinearDamping_Negative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => collider.LinearDamping = -0.1f);
    }

    [Fact]
    public void AngularDamping_Negative_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => collider.AngularDamping = -0.1f);
    }

    [Fact]
    public void IsBullet_SetAndGet()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.IsBullet = true;
        Assert.True(collider.IsBullet);

        collider.IsBullet = false;
        Assert.False(collider.IsBullet);
    }

    [Fact]
    public void FixedRotation_SetAndGet()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.FixedRotation = true;
        Assert.True(collider.FixedRotation);
    }

    [Fact]
    public void GravityScale_SetAndGet()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.GravityScale = 0f;
        Assert.Equal(0f, collider.GravityScale);

        collider.GravityScale = -1f;
        Assert.Equal(-1f, collider.GravityScale);
    }

    [Fact]
    public void OnCollisionHit_EventFires()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var collider1 = entity1.GetComponent<PhysicsBodyComponent>()!;
        var collider2 = entity2.GetComponent<PhysicsBodyComponent>()!;

        var fired = false;
        collider1.OnCollisionHit += (_, _) => fired = true;
        collider1.NotifyCollisionHit(collider2, CollisionContact.Empty);

        Assert.True(fired);
    }

    [Fact]
    public void OnCollisionStay_EventFires()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var collider1 = entity1.GetComponent<PhysicsBodyComponent>()!;
        var collider2 = entity2.GetComponent<PhysicsBodyComponent>()!;

        var fired = false;
        collider1.OnCollisionStay += (_, _) => fired = true;
        collider1.NotifyCollisionStay(collider2, CollisionContact.Empty, null, null);

        Assert.True(fired);
    }

    [Fact]
    public void OnTriggerStay_EventFires()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.IsTrigger = true);
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var collider1 = entity1.GetComponent<PhysicsBodyComponent>()!;
        var collider2 = entity2.GetComponent<PhysicsBodyComponent>()!;

        var fired = false;
        collider1.OnTriggerStay += (_) => fired = true;
        collider1.NotifyTriggerStay(collider2);

        Assert.True(fired);
    }

    [Fact]
    public void SetPolygon_TooFewVertices_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PolygonShape(new System.Numerics.Vector2[] { new(0, 0), new(1, 0) }));
    }

    [Fact]
    public void SetPolygon_TooManyVertices_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        var verts = new System.Numerics.Vector2[9];
        Assert.Throws<ArgumentOutOfRangeException>(() => new PolygonShape(verts));
    }

    [Fact]
    public void Layer_OutOfRange_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => collider.Layer = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => collider.Layer = 64);
    }
}