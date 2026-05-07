using System.Numerics;
using Brine2D.Collision;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;

namespace Brine2D.Tests.ECS.Components;

public class SubShapeTests : TestBase
{
    private PhysicsBodyComponent MakeBody()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        return entity.GetComponent<PhysicsBodyComponent>()!;
    }

    [Fact]
    public void AddSubShape_ReturnsSubShape_WithCorrectDefinition()
    {
        var body = MakeBody();
        var def = new CircleShape(10f);

        var sub = body.AddSubShape(def);

        Assert.Same(def, sub.Definition);
    }

    [Fact]
    public void AddSubShape_DefaultIsTriggerFalse()
    {
        var body = MakeBody();

        var sub = body.AddSubShape(new CircleShape(10f));

        Assert.False(sub.IsTrigger);
    }

    [Fact]
    public void AddSubShape_WithIsTriggerTrue_SetsIsTrigger()
    {
        var body = MakeBody();

        var sub = body.AddSubShape(new CircleShape(10f), isTrigger: true);

        Assert.True(sub.IsTrigger);
    }

    [Fact]
    public void AddSubShape_WithFriction_ClampedAndStored()
    {
        var body = MakeBody();

        var sub = body.AddSubShape(new CircleShape(10f), friction: 2f);

        Assert.Equal(1f, sub.Friction);
    }

    [Fact]
    public void AddSubShape_WithNegativeFriction_ClampedToZero()
    {
        var body = MakeBody();

        var sub = body.AddSubShape(new CircleShape(10f), friction: -1f);

        Assert.Equal(0f, sub.Friction);
    }

    [Fact]
    public void AddSubShape_WithRestitution_ClampedAndStored()
    {
        var body = MakeBody();

        var sub = body.AddSubShape(new CircleShape(10f), restitution: 5f);

        Assert.Equal(1f, sub.Restitution);
    }

    [Fact]
    public void AddSubShape_NullFriction_IsNull()
    {
        var body = MakeBody();

        var sub = body.AddSubShape(new CircleShape(10f));

        Assert.Null(sub.Friction);
    }

    [Fact]
    public void AddSubShape_NullRestitution_IsNull()
    {
        var body = MakeBody();

        var sub = body.AddSubShape(new CircleShape(10f));

        Assert.Null(sub.Restitution);
    }

    [Fact]
    public void AddSubShape_ChainShape_Throws()
    {
        var body = MakeBody();
        var chain = new ChainShape([Vector2.Zero, Vector2.One]);

        Assert.Throws<ArgumentException>(() => body.AddSubShape(chain));
    }

    [Fact]
    public void AddSubShape_AppearsInSubShapes()
    {
        var body = MakeBody();

        var sub = body.AddSubShape(new CircleShape(10f));

        Assert.Contains(sub, body.SubShapes);
    }

    [Fact]
    public void RemoveSubShape_RemovesFromList_ReturnsTrue()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new BoxShape(10f, 10f));

        var removed = body.RemoveSubShape(sub);

        Assert.True(removed);
        Assert.DoesNotContain(sub, body.SubShapes);
    }

    [Fact]
    public void RemoveSubShape_NotPresent_ReturnsFalse()
    {
        var body = MakeBody();
        var otherBody = MakeBody();
        var sub = otherBody.AddSubShape(new CircleShape(5f));

        var removed = body.RemoveSubShape(sub);

        Assert.False(removed);
    }

    [Fact]
    public void ClearSubShapes_RemovesAll()
    {
        var body = MakeBody();
        body.AddSubShape(new CircleShape(5f));
        body.AddSubShape(new BoxShape(10f, 10f));

        body.ClearSubShapes();

        Assert.Empty(body.SubShapes);
    }

    [Fact]
    public void SubShape_Layer_SetAndGet()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));

        sub.Layer = 5;

        Assert.Equal(5, sub.Layer);
    }

    [Fact]
    public void SubShape_Layer_BelowZero_Throws()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));

        Assert.Throws<ArgumentOutOfRangeException>(() => sub.Layer = -1);
    }

    [Fact]
    public void SubShape_Layer_Above63_Throws()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));

        Assert.Throws<ArgumentOutOfRangeException>(() => sub.Layer = 64);
    }

    [Fact]
    public void SubShape_Layer_NullClears()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));
        sub.Layer = 3;

        sub.Layer = null;

        Assert.Null(sub.Layer);
    }

    [Fact]
    public void SubShape_CollisionMask_SetAndGet()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));

        sub.CollisionMask = 0xFF;

        Assert.Equal(0xFFu, sub.CollisionMask);
    }

    [Fact]
    public void SubShape_CollisionMask_NullInherits()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));

        sub.CollisionMask = null;

        Assert.Null(sub.CollisionMask);
    }

    [Fact]
    public void SubShape_EnableHitEvents_SetAndGet()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));

        sub.EnableHitEvents = false;

        Assert.False(sub.EnableHitEvents);
    }

    [Fact]
    public void SubShape_EnableHitEvents_NullInherits()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));

        sub.EnableHitEvents = null;

        Assert.Null(sub.EnableHitEvents);
    }

    [Fact]
    public void SubShape_Friction_SetAfterConstruction()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));

        sub.Friction = 0.5f;

        Assert.Equal(0.5f, sub.Friction);
    }

    [Fact]
    public void SubShape_Restitution_SetAfterConstruction()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));

        sub.Restitution = 0.3f;

        Assert.Equal(0.3f, sub.Restitution);
    }

    [Fact]
    public void SubShape_ShouldCollide_DefaultIsNull()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));

        Assert.Null(sub.ShouldCollide);
    }

    [Fact]
    public void SubShape_ShouldCollide_CanBeSet()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));

        sub.ShouldCollide = (_, _) => true;

        Assert.NotNull(sub.ShouldCollide);
    }

    [Fact]
    public void SubShape_ShouldCollide_ClearedOnRemove()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));
        sub.ShouldCollide = (_, _) => true;

        body.RemoveSubShape(sub);

        Assert.Null(sub.MarkOwnerDirty);
        Assert.Null(sub.MarkOwnerFilterDirty);
        Assert.Null(sub.MarkOwnerMaterialDirty);
        Assert.Null(sub.MarkOwnerShouldCollideChanged);
    }

    [Fact]
    public void AddSubShape_MarksDirty()
    {
        var body = MakeBody();
        body.IsDirty = false;

        body.AddSubShape(new CircleShape(10f));

        Assert.True(body.IsDirty);
    }

    [Fact]
    public void RemoveSubShape_MarksDirty()
    {
        var body = MakeBody();
        var sub = body.AddSubShape(new CircleShape(10f));
        body.IsDirty = false;

        body.RemoveSubShape(sub);

        Assert.True(body.IsDirty);
    }

    [Fact]
    public void ClearSubShapes_MarksDirty()
    {
        var body = MakeBody();
        body.AddSubShape(new CircleShape(10f));
        body.IsDirty = false;

        body.ClearSubShapes();

        Assert.True(body.IsDirty);
    }
}