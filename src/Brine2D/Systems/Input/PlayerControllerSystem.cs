using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;
using Brine2D.Input;
using Brine2D.Systems.Physics;

namespace Brine2D.Systems.Input;

/// <summary>
/// System that processes player input and applies it to entities.
/// </summary>
public class PlayerControllerSystem : UpdateSystemBase
{
    private readonly IInputContext _input;
    private CachedEntityQuery<PlayerControllerComponent>? _playerQuery;

    public PlayerControllerSystem(IInputContext input)
    {
        _input = input;
    }

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        _playerQuery ??= world.CreateCachedQuery<PlayerControllerComponent>().Build();

        foreach (var (entity, controller) in _playerQuery)
        {
            var velocity = entity.GetComponent<VelocityComponent>();

            if (!controller.IsEnabled)
                continue;

            // Get input direction based on input mode
            var inputDirection = GetInputDirection(controller);
            controller.InputDirection = inputDirection;

            // Apply to velocity component if it exists
            if (velocity != null && inputDirection != Vector2.Zero)
            {
                velocity.SetDirection(inputDirection, controller.MoveSpeed);
            }
        }
    }

    private Vector2 GetInputDirection(PlayerControllerComponent controller)
    {
        var direction = Vector2.Zero;

        // Keyboard input
        if (controller.InputMode == InputMode.Keyboard || 
            controller.InputMode == InputMode.KeyboardAndGamepad)
        {
            if (_input.IsKeyDown(Key.W) || _input.IsKeyDown(Key.Up))
                direction.Y -= 1;
            if (_input.IsKeyDown(Key.S) || _input.IsKeyDown(Key.Down))
                direction.Y += 1;
            if (_input.IsKeyDown(Key.A) || _input.IsKeyDown(Key.Left))
                direction.X -= 1;
            if (_input.IsKeyDown(Key.D) || _input.IsKeyDown(Key.Right))
                direction.X += 1;
        }

        // Gamepad input (overrides keyboard if both are enabled)
        if ((controller.InputMode == InputMode.Gamepad || 
             controller.InputMode == InputMode.KeyboardAndGamepad) &&
            _input.IsGamepadConnected(controller.GamepadIndex))
        {
            var leftStick = _input.GetGamepadLeftStick(controller.GamepadIndex);
            
            // If gamepad has significant input, use it (overrides keyboard)
            if (leftStick.LengthSquared() > 0.01f)
            {
                direction = leftStick;
            }
        }

        // Normalize diagonal movement if enabled
        if (controller.NormalizeDiagonals && direction != Vector2.Zero)
        {
            direction = Vector2.Normalize(direction);
        }

        return direction;
    }
}