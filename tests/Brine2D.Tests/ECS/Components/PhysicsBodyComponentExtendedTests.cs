using System.Numerics;
using Brine2D.Collision;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;

namespace Brine2D.Tests.ECS.Components;

public class PhysicsBodyComponentExtendedTests : TestBase
{
    private (PhysicsBodyComponent body1, PhysicsBodyComponent body2) MakePair()
    {
        var world = CreateTestWorld();
        var e1 = world.CreateEntity().AddComponent<TransformComponent>().AddComponent<PhysicsBodyComponent>();
        var e2 = world.CreateEntity().AddComponent<TransformComponent>().AddComponent<PhysicsBodyComponent>();
        return (e1.GetComponent<PhysicsBodyComponent>()!, e2.GetComponent<PhysicsBodyComponent>()!);
    }

    // ChainShape guards

    [Fact]
    public void Shape_AssignChainShape_WhenIsTrigger_Throws()
    {
        var (body, _) = MakePair();
        body.IsTrigger = true;

        Assert.Throws<InvalidOperationException>(() =>
            body.Shape = new ChainShape([Vector2.Zero, Vector2.One]));
    }

    [Fact]
    public void Shape_AssignChainShape_WhenIsBullet_Throws()
    {
        var (body, _) = MakePair();
        body.IsBullet = true;

        Assert.Throws<InvalidOperationException>(() =>
            body.Shape = new ChainShape([Vector2.Zero, Vector2.One]));
    }

    [Fact]
    public void Shape_AssignChainShape_WhenDynamic_Throws()
    {
        var (body, _) = MakePair();

        Assert.Throws<InvalidOperationException>(() =>
            body.Shape = new ChainShape([Vector2.Zero, Vector2.One]));
    }

    [Fact]
    public void Shape_AssignChainShape_WhenStaticNoFlags_Succeeds()
    {
        var (body, _) = MakePair();
        body.BodyType = PhysicsBodyType.Static;

        body.Shape = new ChainShape([Vector2.Zero, Vector2.One]);

        Assert.IsType<ChainShape>(body.Shape);
    }

    [Fact]
    public void IsTrigger_SetTrue_WhenChainShape_Throws()
    {
        var (body, _) = MakePair();
        body.BodyType = PhysicsBodyType.Static;
        body.Shape = new ChainShape([Vector2.Zero, Vector2.One]);

        Assert.Throws<InvalidOperationException>(() => body.IsTrigger = true);
    }

    [Fact]
    public void IsBullet_SetTrue_WhenChainShape_Throws()
    {
        var (body, _) = MakePair();
        body.BodyType = PhysicsBodyType.Static;
        body.Shape = new ChainShape([Vector2.Zero, Vector2.One]);

        Assert.Throws<InvalidOperationException>(() => body.IsBullet = true);
    }

    // CollidingEntities / CollidingBodies correctness

    [Fact]
    public void CollidingEntities_NotRemovedEarly_WhenContactExitsButSensorStillActive()
    {
        var (body1, body2) = MakePair();

        body1.NotifyCollisionEnter(body2, CollisionContact.Empty, null, null);
        body1.NotifyTriggerEnter(body2, null, null);

        body1.NotifyCollisionExit(body2);

        Assert.Contains(body2.Entity!, body1.CollidingEntities);
    }

    [Fact]
    public void CollidingEntities_Removed_WhenBothContactAndSensorExit()
    {
        var (body1, body2) = MakePair();

        body1.NotifyCollisionEnter(body2, CollisionContact.Empty, null, null);
        body1.NotifyTriggerEnter(body2, null, null);

        body1.NotifyCollisionExit(body2);
        body1.NotifyTriggerExit(body2);

        Assert.DoesNotContain(body2.Entity!, body1.CollidingEntities);
    }

    [Fact]
    public void CollidingEntities_NotRemovedEarly_WhenSensorExitsButContactStillActive()
    {
        var (body1, body2) = MakePair();

        body1.NotifyCollisionEnter(body2, CollisionContact.Empty, null, null);
        body1.NotifyTriggerEnter(body2, null, null);

        body1.NotifyTriggerExit(body2);

        Assert.Contains(body2.Entity!, body1.CollidingEntities);
    }

    [Fact]
    public void CollidingEntities_RemovedAfterContactOnly_WhenNoSensorPair()
    {
        var (body1, body2) = MakePair();

        body1.NotifyCollisionEnter(body2, CollisionContact.Empty, null, null);
        body1.NotifyCollisionExit(body2);

        Assert.DoesNotContain(body2.Entity!, body1.CollidingEntities);
    }

    [Fact]
    public void CollidingEntities_RemovedAfterSensorOnly_WhenNoContactPair()
    {
        var (body1, body2) = MakePair();

        body1.NotifyTriggerEnter(body2, null, null);
        body1.NotifyTriggerExit(body2);

        Assert.DoesNotContain(body2.Entity!, body1.CollidingEntities);
    }

    // CollidingBodies

    [Fact]
    public void CollidingBodies_ReturnsContactAndSensorPairs()
    {
        var world = CreateTestWorld();
        var e1 = world.CreateEntity().AddComponent<TransformComponent>().AddComponent<PhysicsBodyComponent>();
        var e2 = world.CreateEntity().AddComponent<TransformComponent>().AddComponent<PhysicsBodyComponent>();
        var e3 = world.CreateEntity().AddComponent<TransformComponent>().AddComponent<PhysicsBodyComponent>();

        var body1 = e1.GetComponent<PhysicsBodyComponent>()!;
        var body2 = e2.GetComponent<PhysicsBodyComponent>()!;
        var body3 = e3.GetComponent<PhysicsBodyComponent>()!;

        body1.NotifyCollisionEnter(body2, CollisionContact.Empty, null, null);
        body1.NotifyTriggerEnter(body3, null, null);

        var colliding = body1.CollidingBodies.ToList();
        Assert.Contains(body2, colliding);
        Assert.Contains(body3, colliding);
    }

    [Fact]
    public void CollidingBodies_Empty_WhenNoActivePairs()
    {
        var (body1, _) = MakePair();

        Assert.Empty(body1.CollidingBodies);
    }

    // Offset doc — verify rebuild is triggered

    [Fact]
    public void Offset_Change_MarksDirty()
    {
        var (body, _) = MakePair();
        body.IsDirty = false;

        body.Offset = new Vector2(10f, 5f);

        Assert.True(body.IsDirty);
    }

    // SegmentShape offset

    [Fact]
    public void SegmentShape_DefaultOffset_IsZero()
    {
        var seg = new SegmentShape(Vector2.Zero, Vector2.One);
        Assert.Equal(Vector2.Zero, seg.Offset);
    }

    [Fact]
    public void SegmentShape_WithOffset_StoredCorrectly()
    {
        var offset = new Vector2(5f, 10f);
        var seg = new SegmentShape(Vector2.Zero, Vector2.One) { Offset = offset };
        Assert.Equal(offset, seg.Offset);
    }
}