using Microsoft.Extensions.Logging;

namespace Brine2D.Animation;

/// <summary>
/// An independent animation track that can run alongside the primary <see cref="AnimatorComponent"/>
/// animator. Each layer owns its own <see cref="SpriteAnimator"/> and
/// <see cref="AnimationStateMachine"/>.
/// </summary>
/// <remarks>
/// Layers are applied in ascending <see cref="Priority"/> order; higher priority wins. By
/// default only <see cref="AnimationLayerMask.SourceRect"/> and
/// <see cref="AnimationLayerMask.Origin"/> are written. Add
/// <see cref="AnimationLayerMask.Texture"/> to <see cref="Mask"/> if the layer drives a
/// different texture atlas; it is excluded from the default to avoid clobbering the base
/// sprite texture.
/// </remarks>
public sealed class AnimationLayer : IDisposable
{
    private float _weight = 1f;
    private int _priority;
    private bool _disposed;

    internal Action? OnPriorityChanged;

    /// <summary>
    /// The animator for this layer. Add clips and call <see cref="SpriteAnimator.Play"/> directly,
    /// or configure <see cref="StateMachine"/> for automatic transitions.
    /// </summary>
    public SpriteAnimator Animator { get; }

    /// <summary>
    /// The state machine for this layer. Evaluated each frame by
    /// <see cref="Brine2D.Systems.Animation.AnimationSystem"/> before the animator is advanced.
    /// </summary>
    public AnimationStateMachine StateMachine { get; }

    /// <summary>Shared parameter store for this layer's transition conditions.</summary>
    public AnimationParameters Parameters { get; } = new();

    /// <summary>
    /// Optional 1D blend tree for this layer.
    /// </summary>
    public AnimationBlendSelector1D? BlendSelector1D { get; set; }

    /// <summary>
    /// Optional 2D blend tree for this layer. Evaluated automatically each frame by
    /// <see cref="Brine2D.Systems.Animation.AnimationSystem"/>.
    /// When both <see cref="BlendSelector1D"/> and <see cref="BlendSelector2D"/> are set,
    /// <see cref="BlendSelector1D"/> is evaluated first.
    /// </summary>
    public AnimationBlendSelector2D? BlendSelector2D { get; set; }

    /// <summary>Human-readable name for debugging.</summary>
    public string Name { get; }

    /// <summary>
    /// Controls which <see cref="Brine2D.Systems.Rendering.SpriteComponent"/> properties this layer
    /// writes. Defaults to <see cref="AnimationLayerMask.Default"/> (<see cref="AnimationLayerMask.SourceRect"/>
    /// | <see cref="AnimationLayerMask.Origin"/>).
    /// Add <see cref="AnimationLayerMask.Texture"/> when this layer drives a different texture atlas,
    /// <see cref="AnimationLayerMask.Tint"/> for tint effects, etc.
    /// </summary>
    public AnimationLayerMask Mask { get; set; } = AnimationLayerMask.Default;

    /// <summary>
    /// Controls how this layer's values are combined with existing sprite values.
    /// Defaults to <see cref="AnimationLayerBlendMode.Override"/>.
    /// </summary>
    public AnimationLayerBlendMode BlendMode { get; set; } = AnimationLayerBlendMode.Override;

    /// <summary>
    /// Evaluation order relative to other layers (base animator is priority 0).
    /// Higher values are applied last and win. Must be ≥ 1.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when set below 1.</exception>
    public int Priority
    {
        get => _priority;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Layer priority must be >= 1. The base animator occupies priority 0.");
            if (_priority == value)
                return;
            _priority = value;
            OnPriorityChanged?.Invoke();
        }
    }

    /// <summary>
    /// Blend weight [0, 1]. 1 = full override; 0 = no effect. Clamped on set.
    /// </summary>
    public float Weight
    {
        get => _weight;
        set => _weight = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// When <c>false</c>, the system skips this layer entirely each frame.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    public AnimationLayer(string name, int priority = 1, ILogger<SpriteAnimator>? logger = null)
    {
        Name = name;
        Priority = priority;
        Animator = new SpriteAnimator(logger);
        StateMachine = new AnimationStateMachine(Animator);
    }

    /// <summary>
    /// Captures a snapshot of this layer's current runtime configuration: <see cref="Weight"/>,
    /// <see cref="Mask"/>, <see cref="BlendMode"/>, and <see cref="IsEnabled"/>.
    /// Use <see cref="RestoreSnapshot"/> to restore the captured state atomically.
    /// </summary>
    public AnimationLayerSnapshot CaptureSnapshot() =>
        new(Weight, Mask, BlendMode, IsEnabled);

    /// <summary>
    /// Restores a previously captured snapshot, atomically setting <see cref="Weight"/>,
    /// <see cref="Mask"/>, <see cref="BlendMode"/>, and <see cref="IsEnabled"/>.
    /// </summary>
    public void RestoreSnapshot(AnimationLayerSnapshot snapshot)
    {
        Weight = snapshot.Weight;
        Mask = snapshot.Mask;
        BlendMode = snapshot.BlendMode;
        IsEnabled = snapshot.IsEnabled;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        OnPriorityChanged = null;
        StateMachine.Dispose();
        Animator.Dispose();
    }
}