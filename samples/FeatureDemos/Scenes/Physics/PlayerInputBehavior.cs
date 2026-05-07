using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Input;

namespace FeatureDemos.Scenes.Physics;

public class PlayerInputBehavior : Behavior
{
    private readonly IInputContext _input;
    private KinematicCharacterBody _character = null!;

    private float _velY;

    /// <summary>+1 for normal gravity, -1 for flipped. Updated by the scene when gravity flips.</summary>
    public float GravityDirection { get; set; } = 1f;

    public PlayerInputBehavior(IInputContext input)
    {
        _input = input;
    }

    protected override void OnAttached()
        => _character = Entity.GetRequiredComponent<KinematicCharacterBody>();

    public void ResetVelocity() => _velY = 0f;

    public override void FixedUpdate(GameTime fixedTime)
    {
        var dt = (float)fixedTime.DeltaTime;
        const float gravity = 980f;
        const float jumpSpeed = 620f;
        const float moveSpeed = 280f;

        float vx = 0f;
        if (_input.IsKeyDown(Key.A) || _input.IsKeyDown(Key.Left)) vx = -moveSpeed;
        if (_input.IsKeyDown(Key.D) || _input.IsKeyDown(Key.Right)) vx = moveSpeed;

        if (_character.IsGrounded)
        {
            _velY = 0f;
            if (_input.IsKeyPressed(Key.W) || _input.IsKeyPressed(Key.Up) || _input.IsKeyPressed(Key.Space))
                _velY = -jumpSpeed * GravityDirection;
        }
        else
        {
            _velY += gravity * GravityDirection * dt;
        }

        _character.MoveAndSlide(new Vector2(vx, _velY));
    }
}