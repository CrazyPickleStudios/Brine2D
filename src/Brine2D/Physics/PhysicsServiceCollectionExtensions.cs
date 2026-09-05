using Brine2D.ECS.Components;
using Brine2D.Systems.Physics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Brine2D.Physics;

/// <summary>
/// Extension methods for registering Box2D physics services.
/// </summary>
public static class PhysicsServiceCollectionExtensions
{
    /// <summary>
    /// Registers Box2D physics services unconditionally so <see cref="PhysicsWorld"/> and the
    /// default <see cref="Box2DPhysicsSystem"/> are always resolvable, even when <see cref="AddPhysics"/>
    /// is never called. Box2D ships as part of core Brine2D (not a separate optional assembly),
    /// so gating registration behind an opt-in call only protected against nothing while causing
    /// a DI-resolution crash the first time a scene's default systems tried to construct
    /// <see cref="Box2DPhysicsSystem"/>. Called unconditionally from <c>AddBrine2D</c>.
    /// </summary>
    internal static void AddPhysicsCore(this IServiceCollection services)
    {
        services.TryAddScoped(_ =>
        {
            var defaults = new PhysicsOptions();
            return new PhysicsWorld(defaults.Gravity, defaults.PixelsPerMeter, defaults.SubStepCount);
        });

        services.TryAddScoped<Box2DPhysicsSystem>();
        services.TryAddScoped<Box2DDebugDrawSystem>();
        services.TryAddScoped<PrePhysicsKinematicCharacterSystem>();
        services.TryAddScoped<PostPhysicsKinematicCharacterSystem>();
        services.TryAddScoped<BuoyancySystem>();

        services.TryAddSingleton<PhysicsLayerRegistry>();
    }

    /// <summary>
    /// Configures Box2D physics (gravity, layers, thresholds, filters). <see cref="PhysicsWorld"/>
    /// and the default physics systems are always registered by <c>AddBrine2D</c>
    /// (see <see cref="AddPhysicsCore"/>) — this call is purely about configuration, not existence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Add simulation to a scene with <c>world.AddSystem&lt;Box2DPhysicsSystem&gt;()</c> —
    /// this is already done automatically as a default system unless excluded via
    /// <c>builder.ExcludeDefaultSystem&lt;Box2DPhysicsSystem&gt;()</c>.
    /// Add the debug draw overlay with <c>world.AddSystem&lt;Box2DDebugDrawSystem&gt;()</c>;
    /// both are zero-cost when not added as systems.
    /// </para>
    /// <para>
    /// <see cref="PhysicsOptions.PixelsPerMeter"/> maps to <c>B2.SetLengthUnitsPerMeter</c>,
    /// which is a <b>process-wide global</b>. All <c>AddPhysics</c> registrations across the
    /// entire application must use the same value.
    /// </para>
    /// <para>
    /// Use <see cref="AddPhysicsLayers"/> to register named physics layers before building
    /// bodies. The <see cref="PhysicsLayerRegistry"/> singleton is available via DI throughout
    /// the application.
    /// </para>
    /// <para>
    /// Add the kinematic character controller to a scene with
    /// <c>world.AddSystem&lt;PrePhysicsKinematicCharacterSystem&gt;()</c> and
    /// <c>world.AddSystem&lt;PostPhysicsKinematicCharacterSystem&gt;()</c>.
    /// </para>
    /// </remarks>
    [ExcludeFromCodeCoverage(Justification = "DI wiring; optional configuration branches require full hosting stack and callback paths that crash CI.")]
    public static IServiceCollection AddPhysics(this IServiceCollection services,
        Action<PhysicsOptions>? configure = null)
    {
        var options = new PhysicsOptions();
        configure?.Invoke(options);

        // Replace (not TryAdd): AddPhysicsCore already registered a default-options PhysicsWorld;
        // this call's whole purpose is to override it with the configured one.
        services.Replace(ServiceDescriptor.Scoped(_ =>
        {
            var world = new PhysicsWorld(options.Gravity, options.PixelsPerMeter, options.SubStepCount);

            if (options.ContactHitEventThreshold.HasValue)
                world.SetContactHitEventThreshold(options.ContactHitEventThreshold.Value);

            if (options.RestitutionThreshold.HasValue)
                world.SetRestitutionThreshold(options.RestitutionThreshold.Value);

            if (!options.SleepingEnabled)
                world.SetSleepingEnabled(false);

            if (!options.ContinuousEnabled)
                world.SetContinuousEnabled(false);

            if (options.MaxLinearSpeed.HasValue)
                world.SetMaxLinearSpeed(options.MaxLinearSpeed.Value);

            if (options.CustomCollisionFilter != null)
                world.SetCustomCollisionFilter(options.CustomCollisionFilter);

            if (options.PreSolveFilter != null)
                world.SetPreSolveFilter(options.PreSolveFilter);

            return world;
        }));

        // TryAdd here too: keeps AddPhysics self-sufficient for standalone DI usage
        // that doesn't call AddBrine2D at all.
        services.TryAddScoped<Box2DPhysicsSystem>();
        services.TryAddScoped<Box2DDebugDrawSystem>();
        services.TryAddScoped<PrePhysicsKinematicCharacterSystem>();
        services.TryAddScoped<PostPhysicsKinematicCharacterSystem>();
        services.TryAddScoped<BuoyancySystem>();

        services.TryAddSingleton<PhysicsLayerRegistry>();

        return services;
    }

    /// <summary>
    /// Registers named physics layers in the <see cref="PhysicsLayerRegistry"/> singleton and
    /// freezes the registry. Call this once during startup after <see cref="AddPhysics"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Delegate to register layer names and their indices (0–63).</param>
    /// <example>
    /// <code>
    /// services.AddPhysics()
    ///         .AddPhysicsLayers(layers =>
    ///         {
    ///             layers.Register("Default",    0);
    ///             layers.Register("Player",     1);
    ///             layers.Register("Enemies",    2);
    ///             layers.Register("Terrain",    3);
    ///             layers.Register("Triggers",   4);
    ///         });
    /// </code>
    /// </example>
    public static IServiceCollection AddPhysicsLayers(this IServiceCollection services,
        Action<PhysicsLayerRegistry> configure)
    {
        services.TryAddSingleton<PhysicsLayerRegistry>();

        var registry = new PhysicsLayerRegistry();
        configure(registry);
        registry.Freeze();

        services.RemoveAll<PhysicsLayerRegistry>();
        services.AddSingleton(registry);

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
    /// Pixel-to-meter ratio passed to <c>B2.SetLengthUnitsPerMeter</c>.
    /// Default: 100 (1 meter = 100 pixels).
    /// </summary>
    /// <remarks>
    /// <b>Process-wide constraint:</b> every <see cref="PhysicsWorld"/> in the same process
    /// must use the same value.
    /// </remarks>
    public float PixelsPerMeter { get; set; } = 100f;

    /// <summary>
    /// Number of Box2D sub-steps per fixed-update tick. Must be at least 1. Default is 4.
    /// </summary>
    public int SubStepCount { get; set; } = 4;

    /// <summary>
    /// Minimum closing speed (pixels/s) at which a contact fires
    /// <see cref="ECS.Components.PhysicsBodyComponent.OnCollisionHit"/>.
    /// Set to <c>0</c> to fire on every contact. Leave <c>null</c> to use the Box2D default.
    /// </summary>
    public float? ContactHitEventThreshold { get; set; }

    /// <summary>
    /// Minimum impact speed (pixels/s) required for restitution (bounciness) to be applied.
    /// Below this threshold contacts are treated as inelastic. Leave <c>null</c> to use the Box2D default.
    /// </summary>
    public float? RestitutionThreshold { get; set; }

    /// <summary>Whether body sleeping is enabled. Default is <c>true</c>.</summary>
    public bool SleepingEnabled { get; set; } = true;

    /// <summary>Whether world-level continuous collision detection is enabled. Default is <c>true</c>.</summary>
    public bool ContinuousEnabled { get; set; } = true;

    /// <summary>
    /// Maximum linear speed (pixels/s) any body can reach. Leave <c>null</c> to use the Box2D default.
    /// </summary>
    public float? MaxLinearSpeed { get; set; }

    /// <summary>
    /// An optional world-level collision filter applied to every candidate contact pair.
    /// Return <c>false</c> to prevent the pair from colliding or triggering.
    /// </summary>
    public Func<PhysicsBodyComponent, PhysicsBodyComponent, bool>? CustomCollisionFilter { get; set; }

    /// <summary>
    /// An optional pre-solve filter invoked by the Box2D solver for every active contact pair
    /// each step. Return <c>false</c> to cancel the contact for that step.
    /// Primary use-case: one-way platforms. See <see cref="PreSolveContact"/> for details.
    /// </summary>
    public Func<PreSolveContact, bool>? PreSolveFilter { get; set; }
}