namespace Brine2D.Input;

/// <summary>
/// Manages input layers and routes input to them in priority order.
/// Layers can consume input to prevent lower-priority layers from receiving it.
/// </summary>
public class InputLayerManager
{
    private readonly List<IInputLayer> _layers = new();
    private readonly IInputContext _inputService;

    // Track what was consumed this frame
    private bool _keyboardConsumedThisFrame;
    private bool _mouseConsumedThisFrame;

    public InputLayerManager(IInputContext inputService)
    {
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
    }

    /// <summary>
    /// Returns true if keyboard input was consumed by any layer this frame.
    /// </summary>
    public bool KeyboardConsumed => _keyboardConsumedThisFrame;

    /// <summary>
    /// Returns true if mouse input was consumed by any layer this frame.
    /// </summary>
    public bool MouseConsumed => _mouseConsumedThisFrame;

    /// <summary>
    /// Registers an input layer.
    /// </summary>
    public void RegisterLayer(IInputLayer layer)
    {
        if (!_layers.Contains(layer))
        {
            _layers.Add(layer);
            _layers.Sort((a, b) => b.Priority.CompareTo(a.Priority)); // Sort by priority descending
        }
    }

    /// <summary>
    /// Unregisters an input layer.
    /// </summary>
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

        foreach (var layer in _layers)
        {
            // Process keyboard if not yet consumed
            if (!_keyboardConsumedThisFrame)
            {
                _keyboardConsumedThisFrame = layer.ProcessKeyboardInput(_inputService);
            }

            // Process mouse if not yet consumed
            if (!_mouseConsumedThisFrame)
            {
                _mouseConsumedThisFrame = layer.ProcessMouseInput(_inputService);
            }

            // If both consumed, no need to check further layers
            if (_keyboardConsumedThisFrame && _mouseConsumedThisFrame)
                break;
        }
    }
}