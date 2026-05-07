using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;

namespace FeatureDemos.Scenes.Physics;

/// <summary>
/// Drives a kinematic platform in a horizontal sinusoidal path.
/// </summary>
public class MovingPlatformBehavior : Behavior
{
    private TransformComponent _transform = null!;
    private float _time;

    public Vector2 Origin { get; set; }
    public float Amplitude { get; set; } = 100f;
    public float Speed { get; set; } = 1f;

    protected override void OnAttached()
        => _transform = Entity.GetRequiredComponent<TransformComponent>();

    public override void FixedUpdate(GameTime fixedTime)
    {
        _time += (float)fixedTime.DeltaTime * Speed;
        _transform.Position = Origin + new Vector2(MathF.Sin(_time) * Amplitude, 0);
    }
}