using System.Numerics;
using Brine2D.ECS.Components;
using Brine2D.Physics;

namespace Brine2D.Tests.ECS.Components;

public class AddSubShapeGuardTests
{
    [Fact]
    public void AddSubShape_SegmentShape_OnDynamicBody_ThrowsArgumentException()
    {
        var body = new PhysicsBodyComponent { BodyType = PhysicsBodyType.Dynamic };
        var segment = new SegmentShape(new Vector2(-50f, 0f), new Vector2(50f, 0f));

        Assert.Throws<ArgumentException>(() => body.AddSubShape(segment));
    }

    [Fact]
    public void AddSubShape_SegmentShape_OnKinematicBody_ThrowsArgumentException()
    {
        var body = new PhysicsBodyComponent { BodyType = PhysicsBodyType.Kinematic };
        var segment = new SegmentShape(new Vector2(-50f, 0f), new Vector2(50f, 0f));

        Assert.Throws<ArgumentException>(() => body.AddSubShape(segment));
    }

    [Fact]
    public void AddSubShape_SegmentShape_OnStaticBody_DoesNotThrow()
    {
        var body = new PhysicsBodyComponent { BodyType = PhysicsBodyType.Static };
        var segment = new SegmentShape(new Vector2(-50f, 0f), new Vector2(50f, 0f));

        var sub = body.AddSubShape(segment);

        Assert.NotNull(sub);
    }

    [Fact]
    public void AddSubShape_ChainShape_AlwaysThrows()
    {
        var body = new PhysicsBodyComponent { BodyType = PhysicsBodyType.Static };
        var chain = new ChainShape([new Vector2(0f, 0f), new Vector2(100f, 0f)]);

        Assert.Throws<ArgumentException>(() => body.AddSubShape(chain));
    }

    [Fact]
    public void AddSubShape_CircleShape_OnDynamicBody_Succeeds()
    {
        var body = new PhysicsBodyComponent { BodyType = PhysicsBodyType.Dynamic };

        var sub = body.AddSubShape(new CircleShape(10f));

        Assert.NotNull(sub);
    }

    [Fact]
    public void AddSubShape_BoxShape_OnDynamicBody_Succeeds()
    {
        var body = new PhysicsBodyComponent { BodyType = PhysicsBodyType.Dynamic };

        var sub = body.AddSubShape(new BoxShape(20f, 10f));

        Assert.NotNull(sub);
    }

    [Fact]
    public void AddSubShape_CapsuleShape_OnDynamicBody_Succeeds()
    {
        var body = new PhysicsBodyComponent { BodyType = PhysicsBodyType.Dynamic };

        var sub = body.AddSubShape(new CapsuleShape(new Vector2(0f, -10f), new Vector2(0f, 10f), 5f));

        Assert.NotNull(sub);
    }

    [Fact]
    public void AddSubShape_PolygonShape_OnDynamicBody_Succeeds()
    {
        var body = new PhysicsBodyComponent { BodyType = PhysicsBodyType.Dynamic };
        var polygon = new PolygonShape([
            new Vector2(-10f, -10f), new Vector2(10f, -10f),
            new Vector2(10f, 10f),   new Vector2(-10f, 10f)
        ]);

        var sub = body.AddSubShape(polygon);

        Assert.NotNull(sub);
    }

    [Fact]
    public void AddSubShape_SegmentShape_ErrorMessage_MentionsStaticBody()
    {
        var body = new PhysicsBodyComponent { BodyType = PhysicsBodyType.Dynamic };
        var segment = new SegmentShape(new Vector2(-50f, 0f), new Vector2(50f, 0f));

        var ex = Assert.Throws<ArgumentException>(() => body.AddSubShape(segment));
        Assert.Contains("static", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}