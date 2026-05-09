using System.Numerics;
using Brine2D.Core;
using Brine2D.Physics;

namespace Brine2D.Tests;

public abstract class PhysicsTestBase : TestBase, IDisposable
{
    protected static readonly GameTime FixedTime = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    protected PhysicsWorld PhysicsWorld { get; }

    protected PhysicsTestBase(Vector2? gravity = null, float pixelsPerMeter = 100f)
    {
        PhysicsWorld = new PhysicsWorld(gravity ?? new Vector2(0f, 980f), pixelsPerMeter);
    }

    public virtual void Dispose()
    {
        PhysicsWorld.Dispose();
        PhysicsWorld.ResetForTesting();
    }
}