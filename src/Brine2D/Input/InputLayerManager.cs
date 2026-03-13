namespace Brine2D.Input;

/// <summary>
/// Manages input layers and routes input to them in priority order.
/// Layers can consume input to prevent lower-priority layers from receiving it.
/// </summary>
public class InputLayerManager
{
    private readonly List<IInputLayer> _layers = new();
    private readonly IInputContext _inputService;

    private bool _keyboardConsumedThisFrame;
    private bool _mouseConsumedThisFrame;
    private bool _gamepadConsumedThisFrame;

    public InputLayerManager(IInputContext inputService)
    {
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
    }

    /// <summary>Returns true if keyboard input was consumed by any layer this frame.</summary>
    public bool KeyboardConsumed => _keyboardConsumedThisFrame;

    /// <summary>Returns true if mouse input was consumed by any layer this frame.</summary>
    public bool MouseConsumed => _mouseConsumedThisFrame;

    /// <summary>Returns true if gamepad input was consumed by any layer this frame.</summary>
    public bool GamepadConsumed => _gamepadConsumedThisFrame;

    /// <summary>Registers an input layer.</summary>
    public void RegisterLayer(IInputLayer layer)
    {
        if (_layers.Contains(layer))
            return;

        var index = _layers.BinarySearch(layer, DescendingPriorityComparer.Instance);
        _layers.Insert(index < 0 ? ~index : index, layer);
    }

    /// <summary>Unregisters an input layer.</summary>
    public void UnregisterLayer(IInputLayer layer)
    {
        _layers.Remove(layer);
    }

    /// <summary>
    /// Processes input through all layers.
    /// Call this once per frame AFTER IInputService.Update().
    /// </summary>
    public void ProcessInput()
    {
        _keyboardConsumedThisFrame = false;
        _mouseConsumedThisFrame = false;
        _gamepadConsumedThisFrame = false;

        foreach (var layer in _layers)
        {
            if (!_keyboardConsumedThisFrame)
                _keyboardConsumedThisFrame = layer.ProcessKeyboardInput(_inputService);

            if (!_mouseConsumedThisFrame)
                _mouseConsumedThisFrame = layer.ProcessMouseInput(_inputService);

            if (!_gamepadConsumedThisFrame)
                _gamepadConsumedThisFrame = layer.ProcessGamepadInput(_inputService);

            if (_keyboardConsumedThisFrame && _mouseConsumedThisFrame && _gamepadConsumedThisFrame)
                break;
        }
    }

    private sealed class DescendingPriorityComparer : IComparer<IInputLayer>
    {
        public static readonly DescendingPriorityComparer Instance = new();

        public int Compare(IInputLayer? x, IInputLayer? y) =>
            (y?.Priority ?? 0).CompareTo(x?.Priority ?? 0);
    }
}