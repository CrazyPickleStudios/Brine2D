using Brine2D.ECS.Components;
using Brine2D.Systems.Physics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Numerics;

namespace Brine2D.Physics;

/// <summary>
/// Extension methods for registering Box2D physics services.
/// </summary>
public static class PhysicsServiceCollectionExtensions
{
    /// <summary>
    /// Adds Box2D physics services. Each scene scope gets its own <see cref="PhysicsWorld"/>.
    /// Scenes that call <c>world.AddSystem&lt;Box2DPhysicsSystem&gt;()</c> will have physics
    /// simulation driven automatically during fixed update.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for physics defaults.</param>
    /// <remarks>
    /// <see cref="PhysicsOptions.PixelsPerMeter"/> maps to <c>B2.SetLengthUnitsPerMeter</c>,
    /// which is a <b>process-wide global</b> in Box2D. All <c>AddPhysics</c> registrations
    /// across the entire application must use the same value, or the second scene to create a
    /// <see cref="PhysicsWorld"/> will throw <see cref="InvalidOperationException"/>.
    /// </remarks>
    public static IServiceCollection AddPhysics(this IServiceCollection services,
        Action<PhysicsOptions>? configure = null)
    {
        var options = new PhysicsOptions();
        configure?.Invoke(options);

        services.TryAddScoped(_ =>
        {
            var world = new PhysicsWorld(options.Gravity, options.PixelsPerMeter, options.SubStepCount);

            if (options.ContactHitEventThreshold.HasValue)
                world.SetContactHitEventThreshold(options.ContactHitEventThreshold.Value);

            if (!options.SleepingEnabled)
                world.SetSleepingEnabled(false);

            if (!options.ContinuousEnabled)
                world.SetContinuousEnabled(false);

            if (options.MaxLinearSpeed.HasValue)
                world.SetMaxLinearSpeed(options.MaxLinearSpeed.Value);

            if (options.CustomCollisionFilter != null)
                world.SetCustomCollisionFilter(options.CustomCollisionFilter);

            return world;
        });

        return services;
    }
}

/// <summary>
/// Configuration for <see cref="PhysicsWorld"/> creation.
/// </summary>
public class PhysicsOptions
{
    /// <summary>
    /// World gravity in pixels per second squared. Default: (0, 980) ≈ 9.8 m/s² at 100 px/m.
    /// </summary>
    public Vector2 Gravity { get; set; } = new(0f, 980f);

    /// <summary>
    /// Pixel-to-meter ratio passed to <see cref="B2.SetLengthUnitsPerMeter"/>.
    /// Default: 100 (1 meter = 100 pixels).
    /// </summary>
    public float PixelsPerMeter { get; set; } = 100f;

    /// <summary>
    /// Number of Box2D sub-steps per fixed-update tick. Higher values improve simulation
    /// accuracy for fast-moving bodies at the cost of CPU time. Must be at least 1. Default is 4.
    /// </summary>
    public int SubStepCount { get; set; } = 4;

    /// <summary>
    /// Minimum closing speed (pixels/s) at which a contact fires
    /// <see cref="ECS.Components.PhysicsBodyComponent.OnCollisionHit"/>.
    /// Set to <c>0</c> to fire on every contact. Leave <c>null</c> to use the Box2D default
    /// (approximately 1 m/s scaled by <see cref="PixelsPerMeter"/>).
    /// </summary>
    public float? ContactHitEventThreshold { get; set; }

    /// <summary>
    /// Whether body sleeping is enabled. When <c>false</c>, all bodies stay awake permanently,
    /// increasing CPU usage but eliminating wakeup latency. Default is <c>true</c>.
    /// </summary>
    public bool SleepingEnabled { get; set; } = true;

    /// <summary>
    /// Whether world-level continuous collision detection is enabled. When <c>false</c>,
    /// fast-moving bodies may tunnel through thin shapes unless per-body
    /// <see cref="PhysicsBodyComponent.IsBullet"/> is used. Default is <c>true</c>.
    /// </summary>
    public bool ContinuousEnabled { get; set; } = true;

    /// <summary>
    /// Maximum linear speed (pixels/s) any body can reach. Box2D clamps velocity to this
    /// value each step. Leave <c>null</c> to use the Box2D default (scaled by
    /// <see cref="PixelsPerMeter"/>).
    /// </summary>
    public float? MaxLinearSpeed { get; set; }

    /// <summary>
    /// An optional world-level collision filter applied to every candidate contact pair.
    /// Return <c>false</c> to prevent the pair from colliding or triggering.
    /// Evaluated after per-body <see cref="PhysicsBodyComponent.ShouldCollide"/> predicates.
    /// The callback is invoked from the Box2D broad-phase — keep it allocation-free.
    /// </summary>
    public Func<PhysicsBodyComponent, PhysicsBodyComponent, bool>? CustomCollisionFilter { get; set; }
}