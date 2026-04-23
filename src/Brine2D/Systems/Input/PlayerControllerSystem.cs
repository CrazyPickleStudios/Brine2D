using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
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

    /// <summary>
    /// Default key bindings used when <see cref="PlayerControllerComponent.ActionMap"/> is null.
    /// </summary>
    private static readonly InputActionMap DefaultActionMap = CreateDefaultActionMap();

    public PlayerControllerSystem(IInputContext input)
    {
        _input = input;
    }

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        _playerQuery ??= world.CreateCachedQuery<PlayerControllerComponent>().Build();

        foreach (var (entity, controller) in _playerQuery)
        {
            var physicsBody = entity.GetComponent<PhysicsBodyComponent>();

            if (!controller.IsEnabled)
                continue;

            var inputDirection = GetInputDirection(controller);
            controller.InputDirection = inputDirection;

            if (physicsBody != null)
            {
                if (inputDirection != Vector2.Zero)
                {
                    // Set velocity directly with normalized direction * speed
                    physicsBody.LinearVelocity = inputDirection * controller.MoveSpeed;
                }
                else
                {
                    physicsBody.LinearVelocity = Vector2.Zero;
                }
            }
        }
    }

    private Vector2 GetInputDirection(PlayerControllerComponent controller)
    {
        var direction = Vector2.Zero;
        var map = controller.ActionMap ?? DefaultActionMap;

        if (controller.InputMode == InputMode.Keyboard ||
            controller.InputMode == InputMode.KeyboardAndGamepad)
        {
            float x = map.ReadValue("MoveRight", _input) - map.ReadValue("MoveLeft", _input);
            float y = map.ReadValue("MoveDown", _input) - map.ReadValue("MoveUp", _input);
            direction = new Vector2(x, y);
        }

        if ((controller.InputMode == InputMode.Gamepad ||
             controller.InputMode == InputMode.KeyboardAndGamepad) &&
            _input.IsGamepadConnected(controller.GamepadIndex))
        {
            var leftStick = _input.GetGamepadLeftStick(controller.GamepadIndex);
            if (leftStick != Vector2.Zero)
                direction = leftStick;
        }

        if (direction != Vector2.Zero)
            direction = Vector2.Normalize(direction);

        return direction;
    }

    private static InputActionMap CreateDefaultActionMap()
    {
        var map = new InputActionMap("PlayerController");
        map.AddAction(new InputAction("MoveUp", new KeyBinding(Key.W), new KeyBinding(Key.Up)));
        map.AddAction(new InputAction("MoveDown", new KeyBinding(Key.S), new KeyBinding(Key.Down)));
        map.AddAction(new InputAction("MoveLeft", new KeyBinding(Key.A), new KeyBinding(Key.Left)));
        map.AddAction(new InputAction("MoveRight", new KeyBinding(Key.D), new KeyBinding(Key.Right)));
        return map;
    }
}