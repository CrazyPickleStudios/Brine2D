using Brine2D.Core;
using Brine2D.Pooling;
using System.Numerics;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Individual particle data.
/// Position, velocity, and other properties are managed by ParticleSystem.
/// </summary>
public class Particle : IPoolable
{
    public Vector2 Position { get; internal set; }
    public Vector2 Velocity { get; internal set; }
    public float Life { get; internal set; }
    public float MaxLife { get; internal set; }
    public float Size { get; internal set; }
    internal float StartSize { get; set; }
    internal float EndSize { get; set; }
    public float Rotation { get; internal set; }
    public float RotationSpeed { get; internal set; }
    internal Color StartColor { get; set; }
    internal Color EndColor { get; set; }

    /// <summary>
    /// The non-gravity component of velocity. Updated each frame for damping and
    /// speed-over-lifetime. The public <see cref="Velocity"/> equals
    /// <c>BaseVelocity + GravityVelocity</c>.
    /// </summary>
    internal Vector2 BaseVelocity { get; set; }

    /// <summary>
    /// Accumulated gravity contribution to velocity. Dampened alongside
    /// <see cref="BaseVelocity"/> when <see cref="ParticleEmitterComponent.Damping"/> is non-zero.
    /// </summary>
    internal Vector2 GravityVelocity { get; set; }

    /// <summary>
    /// Magnitude of <see cref="BaseVelocity"/> at spawn. Used as the reference speed
    /// for <see cref="ParticleEmitterComponent.StartSpeedMultiplier"/> /
    /// <see cref="ParticleEmitterComponent.EndSpeedMultiplier"/>.
    /// </summary>
    internal float BaseSpeed { get; set; }

    internal Vector2[]? TrailPositions;
    internal float[]? TrailRotations;

    /// <summary>
    /// Per-slot sampled color recorded at the moment each trail position is written.
    /// Ensures trail segments reflect the particle's color at the time they were laid down,
    /// rather than the particle's current (potentially faded) color.
    /// </summary>
    internal Color[]? TrailColors;

    /// <summary>
    /// Per-slot animation frame index recorded at the moment each trail position is written.
    /// Ensures trail segments use the frame that was active when that position was laid down,
    /// not the particle's current (potentially advanced) frame.
    /// </summary>
    internal int[]? TrailFrameIndices;

    internal int TrailIndex;

    /// <summary>
    /// How many trail slots have been written since spawn. Capped at TrailPositions.Length.
    /// Used to avoid rendering unwritten (zero) slots at particle birth.
    /// </summary>
    internal int TrailFilled;

    /// <summary>
    /// Tracks which <see cref="LifetimeFractionSubEmitter"/> indices (by position in the
    /// emitter's list) have already fired for this particle, preventing double-triggers
    /// across frames. Null when no lifetime-fraction sub-emitters are configured.
    /// </summary>
    internal HashSet<int>? FiredFractionTriggers;

    void IPoolable.Reset()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        BaseVelocity = Vector2.Zero;
        GravityVelocity = Vector2.Zero;
        BaseSpeed = 0f;
        Life = 0;
        MaxLife = 0;
        Size = 0;
        StartSize = 0;
        EndSize = 0;
        Rotation = 0;
        RotationSpeed = 0;
        StartColor = default;
        EndColor = default;
        TrailIndex = 0;
        TrailFilled = 0;

        // Null rather than Clear so the allocation is released back to the GC when the
        // particle is returned to the pool. Only particles that ever saw a
        // LifetimeFractionSubEmitter emitter will have allocated the set, and those
        // particles may later be recycled by emitters that don't use fraction triggers.
        FiredFractionTriggers = null;

        if (TrailPositions != null)
            Array.Clear(TrailPositions, 0, TrailPositions.Length);
        if (TrailRotations != null)
            Array.Clear(TrailRotations, 0, TrailRotations.Length);
        if (TrailColors != null)
            Array.Clear(TrailColors, 0, TrailColors.Length);
        if (TrailFrameIndices != null)
            Array.Fill(TrailFrameIndices, -1, 0, TrailFrameIndices.Length);
    }
}