using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Systems;
using Brine2D.Input;

namespace Brine2D.Input.ECS;

/// <summary>
/// System that processes player input and applies it to entities.
/// Lives in Brine2D.Input.ECS because it's the bridge between ECS and Input.
/// </summary>
public class PlayerControllerSystem : IUpdateSystem
{
    public int UpdateOrder => 10; 

    private readonly IEntityWorld _world;
    private readonly IInputService _input;

    public PlayerControllerSystem(IEntityWorld world, IInputService input)
    {
        _world = world;
        _input = input;
    }

    public void Update(GameTime gameTime)
    {
        var players = _world.GetEntitiesWithComponent<PlayerControllerComponent>();

        foreach (var entity in players)
        {
            var controller = entity.GetComponent<PlayerControllerComponent>();
            var velocity = entity.GetComponent<VelocityComponent>();

            if (controller == null || !controller.IsEnabled)
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
            if (_input.IsKeyDown(Keys.W) || _input.IsKeyDown(Keys.Up))
                direction.Y -= 1;
            if (_input.IsKeyDown(Keys.S) || _input.IsKeyDown(Keys.Down))
                direction.Y += 1;
            if (_input.IsKeyDown(Keys.A) || _input.IsKeyDown(Keys.Left))
                direction.X -= 1;
            if (_input.IsKeyDown(Keys.D) || _input.IsKeyDown(Keys.Right))
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