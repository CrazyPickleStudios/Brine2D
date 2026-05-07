using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Collision;

namespace Brine2D.Tests.Collision;

public class CollisionContactTests
{
    [Fact]
    public void Default_AllPropertiesAreZero()
    {
        var contact = new CollisionContact();

        Assert.Equal(Vector2.Zero, contact.Normal);
        Assert.Equal(0f, contact.Depth);
        Assert.Equal(Vector2.Zero, contact.ContactPoint);
        Assert.Equal(0f, contact.ImpactSpeed);
    }

    [Fact]
    public void Init_SetsAllProperties()
    {
        var contact = new CollisionContact
        {
            Normal = new Vector2(0f, 1f),
            Depth = 2.5f,
            ContactPoint = new Vector2(10f, 20f),
            ImpactSpeed = 5f
        };

        Assert.Equal(new Vector2(0f, 1f), contact.Normal);
        Assert.Equal(2.5f, contact.Depth);
        Assert.Equal(new Vector2(10f, 20f), contact.ContactPoint);
        Assert.Equal(5f, contact.ImpactSpeed);
    }

    [Fact]
    public void TwoIdenticalInstances_AreEqual()
    {
        var a = new CollisionContact { Normal = new Vector2(1f, 0f), Depth = 1f, ContactPoint = new Vector2(3f, 4f), ImpactSpeed = 2f };
        var b = new CollisionContact { Normal = new Vector2(1f, 0f), Depth = 1f, ContactPoint = new Vector2(3f, 4f), ImpactSpeed = 2f };

        Assert.Equal(a, b);
    }

    [Fact]
    public void TwoDifferentInstances_AreNotEqual()
    {
        var a = new CollisionContact { Normal = new Vector2(1f, 0f), Depth = 1f };
        var b = new CollisionContact { Normal = new Vector2(0f, 1f), Depth = 1f };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Normal_CanBeNegative()
    {
        var contact = new CollisionContact { Normal = new Vector2(-1f, 0f) };

        Assert.Equal(new Vector2(-1f, 0f), contact.Normal);
    }

    [Fact]
    public void ImpactSpeed_CanBeZero()
    {
        var contact = new CollisionContact { ImpactSpeed = 0f };

        Assert.Equal(0f, contact.ImpactSpeed);
    }

    [Fact]
    public void Depth_LargeValue_IsPreserved()
    {
        var contact = new CollisionContact { Depth = 1000f };

        Assert.Equal(1000f, contact.Depth);
    }

    [Fact]
    public void FromManifold_ZeroPoints_ReturnsNormalOnly()
    {
        var manifold = new B2.Manifold
        {
            normal = new B2.Vec2 { x = 0f, y = -1f },
            pointCount = 0
        };

        var contact = CollisionContact.FromManifold(manifold);

        Assert.Equal(new Vector2(0f, -1f), contact.Normal);
        Assert.Equal(Vector2.Zero, contact.ContactPoint);
        Assert.Equal(0f, contact.Depth);
        Assert.Equal(0, contact.ContactPointCount);
    }

    [Fact]
    public void FromManifold_OnePoint_ReturnsSingleContactPoint()
    {
        var manifold = new B2.Manifold { normal = new B2.Vec2 { x = 0f, y = -1f }, pointCount = 1 };
        manifold.points[0] = new B2.ManifoldPoint
        {
            point = new B2.Vec2 { x = 10f, y = 20f },
            separation = -5f,
            normalVelocity = 3f
        };

        var contact = CollisionContact.FromManifold(manifold);

        Assert.Equal(1, contact.ContactPointCount);
        Assert.Equal(new Vector2(10f, 20f), contact.ContactPoint);
        Assert.Equal(5f, contact.Depth, precision: 4);
        Assert.Equal(3f, contact.ImpactSpeed, precision: 4);
        Assert.Equal(Vector2.Zero, contact.ContactPoint2);
    }

    [Fact]
    public void FromManifold_TwoPoints_DeepestIsPrimary()
    {
        var manifold = new B2.Manifold { normal = new B2.Vec2 { x = 0f, y = -1f }, pointCount = 2 };
        // points[0] is shallower
        manifold.points[0] = new B2.ManifoldPoint
        {
            point = new B2.Vec2 { x = 5f, y = 10f },
            separation = -2f,
            normalVelocity = 1f
        };
        // points[1] is deeper — should become the primary ContactPoint
        manifold.points[1] = new B2.ManifoldPoint
        {
            point = new B2.Vec2 { x = 15f, y = 10f },
            separation = -8f,
            normalVelocity = 2f
        };

        var contact = CollisionContact.FromManifold(manifold);

        Assert.Equal(2, contact.ContactPointCount);
        Assert.Equal(new Vector2(15f, 10f), contact.ContactPoint);
        Assert.Equal(8f, contact.Depth, precision: 4);
        Assert.Equal(new Vector2(5f, 10f), contact.ContactPoint2);
    }

    [Fact]
    public void FromManifold_TwoPoints_EqualDepth_FirstIsPrimary()
    {
        var manifold = new B2.Manifold { normal = new B2.Vec2 { x = 1f, y = 0f }, pointCount = 2 };
        manifold.points[0] = new B2.ManifoldPoint
        {
            point = new B2.Vec2 { x = 1f, y = 2f },
            separation = -4f,
            normalVelocity = 0f
        };
        manifold.points[1] = new B2.ManifoldPoint
        {
            point = new B2.Vec2 { x = 1f, y = -2f },
            separation = -4f,
            normalVelocity = 0f
        };

        var contact = CollisionContact.FromManifold(manifold);

        Assert.Equal(2, contact.ContactPointCount);
        // Equal depth — index 0 wins (no swap needed)
        Assert.Equal(new Vector2(1f, 2f), contact.ContactPoint);
        Assert.Equal(new Vector2(1f, -2f), contact.ContactPoint2);
    }

    [Fact]
    public void FromManifold_NegativeNormalVelocity_ImpactSpeedIsZero()
    {
        var manifold = new B2.Manifold { normal = new B2.Vec2 { x = 0f, y = 1f }, pointCount = 1 };
        manifold.points[0] = new B2.ManifoldPoint
        {
            point = new B2.Vec2 { x = 0f, y = 0f },
            separation = -1f,
            normalVelocity = -5f  // separating, not approaching
        };

        var contact = CollisionContact.FromManifold(manifold);

        Assert.Equal(0f, contact.ImpactSpeed);
    }

    [Fact]
    public void IsEmpty_DefaultContact_IsTrue()
    {
        var contact = default(CollisionContact);

        Assert.True(contact.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ContactWithNormal_IsFalse()
    {
        var contact = new CollisionContact { Normal = Vector2.UnitY };

        Assert.False(contact.IsEmpty);
    }
}