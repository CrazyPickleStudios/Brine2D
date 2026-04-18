using Microsoft.Extensions.Logging;

namespace Brine2D.Input;

/// <summary>
/// Manages input layers and routes input to them in priority order.
/// All layers are always invoked each frame; the consumed flag is forwarded so
/// lower-priority layers can perform cleanup even when input was already claimed.
/// </summary>
/// <remarks>
/// Thread-safe: layer registration and processing are guarded by <see cref="_lock"/>.
/// The lock is never held while invoking layer callbacks to avoid deadlocks when
/// user handlers call <see cref="RegisterLayer"/> or <see cref="UnregisterLayer"/> re-entrantly.
/// </remarks>
public sealed class InputLayerManager : IDisposable
{
    private readonly Lock _lock = new();
    private readonly List<IInputLayer> _layers = new();
    private readonly IInputContext _inputService;
    private readonly ILogger<InputLayerManager>? _logger;

    private bool _keyboardConsumedThisFrame;
    private bool _mouseConsumedThisFrame;
    private bool _gamepadConsumedThisFrame;

    public InputLayerManager(IInputContext inputService, ILogger<InputLayerManager>? logger = null)
    {
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _logger = logger;
    }

    /// <summary>Returns true if keyboard input was consumed by any layer this frame.</summary>
    public bool KeyboardConsumed { get { lock (_lock) return _keyboardConsumedThisFrame; } }

    /// <summary>Returns true if mouse input was consumed by any layer this frame.</summary>
    public bool MouseConsumed { get { lock (_lock) return _mouseConsumedThisFrame; } }

    /// <summary>Returns true if gamepad input was consumed by any layer this frame.</summary>
    public bool GamepadConsumed { get { lock (_lock) return _gamepadConsumedThisFrame; } }

    /// <summary>Registers an input layer.</summary>
    public void RegisterLayer(IInputLayer layer)
    {
        lock (_lock)
        {
            if (_layers.Contains(layer))
                return;

            var index = _layers.BinarySearch(layer, DescendingPriorityComparer.Instance);
            _layers.Insert(index < 0 ? ~index : index, layer);
        }
    }

    /// <summary>Unregisters an input layer.</summary>
    public void UnregisterLayer(IInputLayer layer)
    {
        lock (_lock)
        {
            _layers.Remove(layer);
        }
    }

    /// <summary>
    /// Processes input through all layers in priority order.
    /// Call this once per frame after <see cref="IInputContext.Update"/>.
    /// If a layer throws, the exception is logged and processing continues
    /// with the remaining layers so that a single misbehaving layer cannot
    /// break input for the entire frame.
    /// </summary>
    public void ProcessInput()
    {
        IInputLayer[] snapshot;
        lock (_lock)
        {
            snapshot = [.. _layers];
            _keyboardConsumedThisFrame = false;
            _mouseConsumedThisFrame = false;
            _gamepadConsumedThisFrame = false;
        }

        bool kb = false, mouse = false, gp = false;

        for (int i = 0, count = snapshot.Length; i < count; i++)
        {
            var layer = snapshot[i];

            try
            {
                if (layer.ProcessKeyboardInput(_inputService, kb))
                    kb = true;

                if (layer.ProcessMouseInput(_inputService, mouse))
                    mouse = true;

                if (layer.ProcessGamepadInput(_inputService, gp))
                    gp = true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Input layer {LayerType} (priority {Priority}) threw during processing",
                    layer.GetType().Name, layer.Priority);
            }
        }

        lock (_lock)
        {
            _keyboardConsumedThisFrame = kb;
            _mouseConsumedThisFrame = mouse;
            _gamepadConsumedThisFrame = gp;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _layers.Clear();
        }
    }

    private sealed class DescendingPriorityComparer : IComparer<IInputLayer>
    {
        public static readonly DescendingPriorityComparer Instance = new();

        public int Compare(IInputLayer? x, IInputLayer? y) =>
            (y?.Priority ?? 0).CompareTo(x?.Priority ?? 0);
    }
}