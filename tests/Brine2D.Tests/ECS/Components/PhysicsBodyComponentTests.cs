using System.Numerics;
using Brine2D.Collision;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;

namespace Brine2D.Tests.ECS.Components;

public class PhysicsBodyComponentTests : TestBase
{
    private PhysicsBodyComponent MakeCollider(IEntityWorld world, Action<PhysicsBodyComponent>? configure = null)
    {
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;
        configure?.Invoke(collider);
        return collider;
    }

    // ── Default state ──────────────────────────────────────────────────────────

    [Fact]
    public void IsDirty_DefaultIsTrue()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        Assert.True(collider.IsDirty);
    }

    [Fact]
    public void IsSimulationEnabled_DefaultIsTrue()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        Assert.True(collider.IsSimulationEnabled);
    }

    [Fact]
    public void EnableHitEvents_DefaultIsTrue()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        Assert.True(collider.EnableHitEvents);
    }

    [Fact]
    public void Mass_DefaultIsOne()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        Assert.Equal(1f, collider.Mass);
    }

    [Fact]
    public void GravityScale_DefaultIsOne()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        Assert.Equal(1f, collider.GravityScale);
    }

    // ── Property validation ────────────────────────────────────────────────────

    [Theory]
    [InlineData(-1)]
    [InlineData(64)]
    [InlineData(100)]
    public void Layer_OutOfRange_Throws(int layer)
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        Assert.Throws<ArgumentOutOfRangeException>(() => collider.Layer = layer);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(63)]
    [InlineData(32)]
    public void Layer_ValidRange_Stores(int layer)
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        collider.Layer = layer;

        Assert.Equal(layer, collider.Layer);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-0.001f)]
    [InlineData(float.NegativeInfinity)]
    public void Mass_ZeroOrNegative_Throws(float mass)
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        Assert.Throws<ArgumentOutOfRangeException>(() => collider.Mass = mass);
    }

    [Fact]
    public void Mass_Positive_Stores()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        collider.Mass = 5f;

        Assert.Equal(5f, collider.Mass);
    }

    [Theory]
    [InlineData(-0.001f)]
    [InlineData(-10f)]
    public void LinearDamping_Negative_Throws(float value)
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        Assert.Throws<ArgumentOutOfRangeException>(() => collider.LinearDamping = value);
    }

    [Fact]
    public void LinearDamping_Zero_Stores()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        collider.LinearDamping = 0f;

        Assert.Equal(0f, collider.LinearDamping);
    }

    [Theory]
    [InlineData(-0.001f)]
    [InlineData(-10f)]
    public void AngularDamping_Negative_Throws(float value)
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        Assert.Throws<ArgumentOutOfRangeException>(() => collider.AngularDamping = value);
    }

    // ── Clamping ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1.5f, 1f)]
    [InlineData(-0.5f, 0f)]
    [InlineData(0.5f, 0.5f)]
    public void Restitution_ClampsTo0_1(float input, float expected)
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        collider.Restitution = input;

        Assert.Equal(expected, collider.Restitution);
    }

    [Theory]
    [InlineData(2f, 1f)]
    [InlineData(-1f, 0f)]
    [InlineData(0.3f, 0.3f)]
    public void SurfaceFriction_ClampsTo0_1(float input, float expected)
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        collider.SurfaceFriction = input;

        Assert.Equal(expected, collider.SurfaceFriction);
    }

    // ── BodyType no-op ─────────────────────────────────────────────────────────

    [Fact]
    public void BodyType_SameValue_DoesNotSetDirtyFlags()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        collider.IsDirty = false;

        collider.BodyType = PhysicsBodyType.Dynamic; // same as default

        Assert.False(collider.IsBodyTypeDirty);
        Assert.False(collider.IsDirty);
    }

    // ── IsSimulationEnabled no-op ──────────────────────────────────────────────

    [Fact]
    public void IsSimulationEnabled_SameValue_DoesNotSetDirty()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        collider.IsDirty = false;

        collider.IsSimulationEnabled = true; // same as default

        Assert.False(collider.IsDirty);
        Assert.False(collider.IsSimulationEnabledDirty);
    }

    [Fact]
    public void IsSimulationEnabled_Changed_SetsDirty()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        collider.IsDirty = false;

        collider.IsSimulationEnabled = false;

        Assert.True(collider.IsDirty);
    }

    // ── ChainShape restrictions ────────────────────────────────────────────────

    [Fact]
    public void Shape_ChainWithIsTriggerTrue_Throws()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world, c =>
        {
            c.IsTrigger = true;
            c.BodyType = PhysicsBodyType.Static;
        });

        Assert.Throws<InvalidOperationException>(() =>
            collider.Shape = new ChainShape([Vector2.Zero, Vector2.UnitX * 100f]));
    }

    [Fact]
    public void Shape_ChainWithIsBulletTrue_Throws()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world, c =>
        {
            c.IsBullet = true;
            c.BodyType = PhysicsBodyType.Static;
        });

        Assert.Throws<InvalidOperationException>(() =>
            collider.Shape = new ChainShape([Vector2.Zero, Vector2.UnitX * 100f]));
    }

    [Theory]
    [InlineData(PhysicsBodyType.Dynamic)]
    [InlineData(PhysicsBodyType.Kinematic)]
    public void Shape_ChainWithNonStaticBodyType_Throws(PhysicsBodyType bodyType)
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world, c => c.BodyType = bodyType);

        Assert.Throws<InvalidOperationException>(() =>
            collider.Shape = new ChainShape([Vector2.Zero, Vector2.UnitX * 100f]));
    }

    [Fact]
    public void Shape_ChainWithStaticBodyType_Succeeds()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world, c => c.BodyType = PhysicsBodyType.Static);

        collider.Shape = new ChainShape([Vector2.Zero, Vector2.UnitX * 100f]);

        Assert.IsType<ChainShape>(collider.Shape);
    }

    [Fact]
    public void IsTrigger_SetTrueWhileChainShape_Throws()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world, c =>
        {
            c.BodyType = PhysicsBodyType.Static;
            c.Shape = new ChainShape([Vector2.Zero, Vector2.UnitX * 100f]);
        });

        Assert.Throws<InvalidOperationException>(() => collider.IsTrigger = true);
    }

    [Fact]
    public void IsBullet_SetTrueWhileChainShape_Throws()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world, c =>
        {
            c.BodyType = PhysicsBodyType.Static;
            c.Shape = new ChainShape([Vector2.Zero, Vector2.UnitX * 100f]);
        });

        Assert.Throws<InvalidOperationException>(() => collider.IsBullet = true);
    }

    [Theory]
    [InlineData(PhysicsBodyType.Dynamic)]
    [InlineData(PhysicsBodyType.Kinematic)]
    public void BodyType_NonStaticWhileChainShape_Throws(PhysicsBodyType bodyType)
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world, c =>
        {
            c.BodyType = PhysicsBodyType.Static;
            c.Shape = new ChainShape([Vector2.Zero, Vector2.UnitX * 100f]);
        });

        Assert.Throws<InvalidOperationException>(() => collider.BodyType = bodyType);
    }

    // ── SubShape API ───────────────────────────────────────────────────────────

    [Fact]
    public void AddSubShape_ChainShape_Throws()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        Assert.Throws<ArgumentException>(() =>
            collider.AddSubShape(new ChainShape([Vector2.Zero, Vector2.UnitX * 100f])));
    }

    [Fact]
    public void AddSubShape_ValidShape_ReturnsSubShapeAndMarksDirty()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        collider.IsDirty = false;

        var sub = collider.AddSubShape(new CircleShape(10f));

        Assert.NotNull(sub);
        Assert.Single(collider.SubShapes);
        Assert.True(collider.IsDirty);
    }

    [Fact]
    public void AddSubShape_WithTriggerAndMaterials_StoresValues()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        var sub = collider.AddSubShape(new BoxShape(20f, 20f), isTrigger: true, friction: 0.4f, restitution: 0.6f);

        Assert.True(sub.IsTrigger);
        Assert.Equal(0.4f, sub.Friction);
        Assert.Equal(0.6f, sub.Restitution);
    }

    [Fact]
    public void AddSubShape_FrictionRestitutionClamped()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        var sub = collider.AddSubShape(new CircleShape(5f), friction: 2f, restitution: -1f);

        Assert.Equal(1f, sub.Friction);
        Assert.Equal(0f, sub.Restitution);
    }

    [Fact]
    public void RemoveSubShape_Unknown_ReturnsFalse()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        var other = MakeCollider(world);
        var sub = other.AddSubShape(new CircleShape(5f));

        var result = collider.RemoveSubShape(sub);

        Assert.False(result);
    }

    [Fact]
    public void RemoveSubShape_Known_ReturnsTrueAndMarksDirty()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        var sub = collider.AddSubShape(new CircleShape(10f));
        collider.IsDirty = false;

        var result = collider.RemoveSubShape(sub);

        Assert.True(result);
        Assert.Empty(collider.SubShapes);
        Assert.True(collider.IsDirty);
    }

    [Fact]
    public void ClearSubShapes_RemovesAllAndMarksDirty()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        collider.AddSubShape(new CircleShape(5f));
        collider.AddSubShape(new BoxShape(10f, 10f));
        collider.IsDirty = false;

        collider.ClearSubShapes();

        Assert.Empty(collider.SubShapes);
        Assert.True(collider.IsDirty);
    }

    [Fact]
    public void SubShape_Layer_OutOfRange_Throws()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        var sub = collider.AddSubShape(new CircleShape(10f));

        Assert.Throws<ArgumentOutOfRangeException>(() => sub.Layer = 64);
        Assert.Throws<ArgumentOutOfRangeException>(() => sub.Layer = -1);
    }

    [Fact]
    public void SubShape_Layer_NullClearsOverride()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        var sub = collider.AddSubShape(new CircleShape(10f));
        sub.Layer = 5;

        sub.Layer = null;

        Assert.Null(sub.Layer);
    }

    // ── GravityOverride ────────────────────────────────────────────────────────

    [Fact]
    public void GravityOverride_SetAndGet_Stores()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        collider.GravityOverride = new Vector2(0f, -500f);

        Assert.Equal(new Vector2(0f, -500f), collider.GravityOverride);
    }

    [Fact]
    public void GravityOverride_SetToNull_ClearsValue()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        collider.GravityOverride = new Vector2(0f, -500f);

        collider.GravityOverride = null;

        Assert.Null(collider.GravityOverride);
    }

    // ── Events via Notify* ─────────────────────────────────────────────────────

    [Fact]
    public void OnCollisionStay_Fires()
    {
        var world = CreateTestWorld();
        var collider1 = MakeCollider(world);
        var collider2 = MakeCollider(world);

        PhysicsBodyComponent? received = null;
        collider1.OnCollisionStay += (other, _) => received = other;

        collider1.NotifyCollisionStay(collider2, CollisionContact.Empty, null, null);

        Assert.Same(collider2, received);
    }

    [Fact]
    public void OnCollisionStayWithShape_Fires()
    {
        var world = CreateTestWorld();
        var collider1 = MakeCollider(world);
        var collider2 = MakeCollider(world);

        SubShape? receivedSub = null;
        collider1.OnCollisionStayWithShape += (_, _, self, _) => receivedSub = self;

        var sub = collider1.AddSubShape(new CircleShape(5f));
        collider1.NotifyCollisionStay(collider2, CollisionContact.Empty, sub, null);

        Assert.Same(sub, receivedSub);
    }

    [Fact]
    public void OnCollisionEnterWithShape_Fires()
    {
        var world = CreateTestWorld();
        var collider1 = MakeCollider(world);
        var collider2 = MakeCollider(world);

        SubShape? selfSub = null;
        SubShape? otherSub = null;
        collider1.OnCollisionEnterWithShape += (_, _, s, o) => { selfSub = s; otherSub = o; };

        var sub1 = collider1.AddSubShape(new CircleShape(5f));
        var sub2 = collider2.AddSubShape(new BoxShape(10f, 10f));
        collider1.NotifyCollisionEnter(collider2, CollisionContact.Empty, sub1, sub2);

        Assert.Same(sub1, selfSub);
        Assert.Same(sub2, otherSub);
    }

    [Fact]
    public void OnCollisionExitWithShape_Fires()
    {
        var world = CreateTestWorld();
        var collider1 = MakeCollider(world);
        var collider2 = MakeCollider(world);

        var fired = false;
        collider1.OnCollisionExitWithShape += (_, _, _) => fired = true;

        collider1.NotifyCollisionEnter(collider2, CollisionContact.Empty, null, null);
        collider1.NotifyCollisionExit(collider2);

        Assert.True(fired);
    }

    [Fact]
    public void OnTriggerStay_Fires()
    {
        var world = CreateTestWorld();
        var collider1 = MakeCollider(world, c => c.IsTrigger = true);
        var collider2 = MakeCollider(world);

        PhysicsBodyComponent? received = null;
        collider1.OnTriggerStay += other => received = other;

        collider1.NotifyTriggerStay(collider2);

        Assert.Same(collider2, received);
    }

    [Fact]
    public void OnTriggerEnterWithShape_Fires()
    {
        var world = CreateTestWorld();
        var collider1 = MakeCollider(world, c => c.IsTrigger = true);
        var collider2 = MakeCollider(world);

        SubShape? receivedSub = null;
        collider1.OnTriggerEnterWithShape += (_, self, _) => receivedSub = self;

        var sub = collider1.AddSubShape(new CircleShape(5f), isTrigger: true);
        collider1.NotifyTriggerEnter(collider2, sub, null);

        Assert.Same(sub, receivedSub);
    }

    [Fact]
    public void OnTriggerExitWithShape_Fires()
    {
        var world = CreateTestWorld();
        var collider1 = MakeCollider(world, c => c.IsTrigger = true);
        var collider2 = MakeCollider(world);

        var fired = false;
        collider1.OnTriggerExitWithShape += (_, _, _) => fired = true;

        collider1.NotifyTriggerEnter(collider2, null, null);
        collider1.NotifyTriggerExit(collider2);

        Assert.True(fired);
    }

    [Fact]
    public void OnCollisionHit_Fires()
    {
        var world = CreateTestWorld();
        var collider1 = MakeCollider(world);
        var collider2 = MakeCollider(world);

        CollisionContact? receivedContact = null;
        collider1.OnCollisionHit += (_, c) => receivedContact = c;

        var contact = new CollisionContact { Normal = Vector2.UnitY, ImpactSpeed = 10f };
        collider1.NotifyCollisionHit(collider2, contact);

        Assert.NotNull(receivedContact);
        Assert.Equal(10f, receivedContact!.Value.ImpactSpeed);
    }

    [Fact]
    public void OnBodySleep_Fires()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        var fired = false;
        collider.OnBodySleep += _ => fired = true;

        collider.NotifyBodySleep();

        Assert.True(fired);
    }

    [Fact]
    public void OnBodyWake_Fires()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        var fired = false;
        collider.OnBodyWake += _ => fired = true;

        collider.NotifyBodyWake();

        Assert.True(fired);
    }

    // ── OnRemoved state reset ──────────────────────────────────────────────────

    [Fact]
    public void OnRemoved_ClearsStateAndEvents()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.IsTrigger = false;
            });

        var collider = entity.GetComponent<PhysicsBodyComponent>()!;
        var other = MakeCollider(world);
        collider.NotifyCollisionEnter(other, CollisionContact.Empty, null, null);

        bool sleepFired = false;
        collider.OnBodySleep += _ => sleepFired = true;

        entity.RemoveComponent<PhysicsBodyComponent>();

        // Events and contacts should be cleared; sleeping event no longer fires
        collider.NotifyBodySleep();
        Assert.False(sleepFired);
        Assert.Empty(collider.CollidingEntities);
        Assert.Empty(collider.ActiveContactPairs);
        Assert.True(collider.IsDirty);
    }

    // ── ShouldCollide filter ───────────────────────────────────────────────────

    [Fact]
    public void ShouldCollide_SetNonNull_FiresShouldCollideChangedWithTrue()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);

        bool? changedArg = null;
        collider.ShouldCollideChanged += v => changedArg = v;

        collider.ShouldCollide = _ => true;

        Assert.True(changedArg);
    }

    [Fact]
    public void ShouldCollide_SetNull_FiresShouldCollideChangedWithFalse()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        collider.ShouldCollide = _ => true;

        bool? changedArg = null;
        collider.ShouldCollideChanged += v => changedArg = v;

        collider.ShouldCollide = null;

        Assert.False(changedArg);
    }

    [Fact]
    public void ShouldCollide_SetSameNonNull_DoesNotFireChanged()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        Func<PhysicsBodyComponent, bool> filter = _ => true;
        collider.ShouldCollide = filter;

        int changeCount = 0;
        collider.ShouldCollideChanged += _ => changeCount++;

        collider.ShouldCollide = _ => false; // different delegate, but both non-null

        Assert.Equal(0, changeCount);
    }

    [Fact]
    public void SubShape_ShouldCollide_SetNonNull_FiresChanged()
    {
        var world = CreateTestWorld();
        var collider = MakeCollider(world);
        var sub = collider.AddSubShape(new CircleShape(5f));

        bool? changedArg = null;
        collider.ShouldCollideChanged += v => changedArg = v;

        sub.ShouldCollide = (_, _) => true;

        Assert.True(changedArg);
    }
}