using Brine2D.Pooling;
using System;
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
    public float Rotation { get; internal set; }
    public float RotationSpeed { get; internal set; }
    
    internal Vector2[]? TrailPositions;
    internal int TrailIndex;

    void IPoolable.Reset()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Life = 0;
        MaxLife = 0;
        Size = 0;
        Rotation = 0;
        RotationSpeed = 0;
        TrailIndex = 0;
        
        if (TrailPositions != null)
        {
            Array.Clear(TrailPositions, 0, TrailPositions.Length);
        }
    }
}
