using Brine2D.Core;
using Brine2D.ECS;
using Microsoft.Extensions.Logging;

namespace Brine2D.Animation;

/// <summary>
/// ECS component that holds a <see cref="SpriteAnimator"/> and an
/// <see cref="AnimationStateMachine"/> for an entity.
/// Add this alongside <see cref="Brine2D.Systems.Rendering.SpriteComponent"/> and register
/// <see cref="Brine2D.Systems.Animation.AnimationSystem"/> in the scene.
/// </summary>
/// <remarks>
/// Layers are applied after the base animator in ascending <see cref="AnimationLayer.Priority"/>
/// order; the base animator is priority 0. Assign <see cref="BlendSelector1D"/> or
/// <see cref="BlendSelector2D"/> to drive clip selection from a continuous parameter.
/// <para>
/// Call <see cref="Dispose"/> when using this component outside the ECS (e.g. in tests). Inside
/// the ECS it is disposed automatically via <see cref="OnRemoved"/>.
/// </para>
/// </remarks>
public class AnimatorComponent : Component, IDisposable
{
    private readonly List<AnimationLayer> _layers = new();
    private bool _disposed;

    /// <summary>The primary animator that drives sprite frame selection for this entity.</summary>
    public SpriteAnimator Animator { get; }

    /// <summary>
    /// The state machine that evaluates transitions each frame for the primary animator.
    /// </summary>
    public AnimationStateMachine StateMachine { get; }

    /// <summary>Shared parameter store for the primary <see cref="StateMachine"/> transition conditions.</summary>
    public AnimationParameters Parameters { get; } = new();

    /// <summary>
    /// Optional 1D blend tree for the primary animator. Evaluated automatically each frame by
    /// <see cref="Brine2D.Systems.Animation.AnimationSystem"/>.
    /// </summary>
    public AnimationBlendSelector1D? BlendSelector1D { get; set; }

    /// <summary>
    /// Optional 2D blend tree for the primary animator. When both <see cref="BlendSelector1D"/> and
    /// <see cref="BlendSelector2D"/> are set, <see cref="BlendSelector1D"/> takes precedence.
    /// </summary>
    public AnimationBlendSelector2D? BlendSelector2D { get; set; }

    /// <summary>
    /// Read-only ordered view of all additional animation layers, sorted by
    /// <see cref="AnimationLayer.Priority"/> ascending.
    /// </summary>
    public IReadOnlyList<AnimationLayer> Layers => _layers;

    /// <summary>
    /// Returns the <see cref="SpriteFrame.HitBox"/> of the primary animator's current frame,
    /// or <c>null</c> if no animation is playing or the current frame has no hitbox defined.
    /// </summary>
    public Rectangle? CurrentHitBox => Animator.CurrentFrame?.HitBox;

    /// <summary>
    /// Returns the named hit box from the primary animator's current frame, or <c>null</c> if
    /// no animation is playing or the named box is not defined on the current frame.
    /// </summary>
    public Rectangle? GetCurrentHitBox(string name) => Animator.CurrentFrame?.GetHitBox(name);

    public AnimatorComponent() : this(null) { }

    public AnimatorComponent(ILogger<SpriteAnimator>? logger)
    {
        Animator = new SpriteAnimator(logger);
        StateMachine = new AnimationStateMachine(Animator);
    }

    /// <inheritdoc/>
    protected internal override void OnRemoved()
    {
        Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        StateMachine.Dispose();
        Animator.Dispose();
        foreach (var layer in _layers)
            layer.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Adds an animation layer. Layers are kept sorted by <see cref="AnimationLayer.Priority"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if a layer named <paramref name="name"/> already exists.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="priority"/> is less than 1.</exception>
    public AnimationLayer AddLayer(string name, int priority = 1, ILogger<SpriteAnimator>? logger = null)
    {
        if (priority < 1)
            throw new ArgumentOutOfRangeException(nameof(priority), priority, "Layer priority must be >= 1. The base animator occupies priority 0.");

        if (_layers.Exists(l => l.Name == name))
            throw new ArgumentException($"A layer named '{name}' already exists on this component.", nameof(name));

        var layer = new AnimationLayer(name, priority, logger);
        layer.OnPriorityChanged = SortLayers;
        _layers.Add(layer);
        SortLayers();
        return layer;
    }

    /// <summary>
    /// Removes the first layer with the given name and disposes it.
    /// </summary>
    public bool RemoveLayer(string name)
    {
        var index = _layers.FindIndex(l => l.Name == name);
        if (index < 0)
            return false;
        var layer = _layers[index];
        layer.OnPriorityChanged = null;
        layer.Dispose();
        _layers.RemoveAt(index);
        return true;
    }

    /// <summary>Returns the first layer with the given name, or <c>null</c>.</summary>
    public AnimationLayer? GetLayer(string name) => _layers.Find(l => l.Name == name);

    /// <summary>Returns <c>true</c> if a layer with the given name has been added.</summary>
    public bool HasLayer(string name) => _layers.Exists(l => l.Name == name);

    /// <summary>
    /// Returns the primary <see cref="SpriteFrame.HitBox"/> of the named layer's current frame,
    /// or <c>null</c> if the layer doesn't exist, no animation is playing, or no hitbox is defined.
    /// </summary>
    public Rectangle? GetLayerHitBox(string layerName) =>
        GetLayer(layerName)?.Animator.CurrentFrame?.HitBox;

    /// <summary>
    /// Returns a named hit box from the named layer's current frame, or <c>null</c>.
    /// </summary>
    public Rectangle? GetLayerHitBox(string layerName, string hitBoxName) =>
        GetLayer(layerName)?.Animator.CurrentFrame?.GetHitBox(hitBoxName);

    /// <summary>
    /// Pauses the primary <see cref="Animator"/> and every layer animator simultaneously.
    /// No-op for animators that are already paused or not playing.
    /// </summary>
    public void PauseAllLayers()
    {
        Animator.Pause();
        foreach (var layer in _layers)
            layer.Animator.Pause();
    }

    /// <summary>
    /// Resumes the primary <see cref="Animator"/> and every layer animator simultaneously.
    /// No-op for animators that are not paused.
    /// </summary>
    public void ResumeAllLayers()
    {
        Animator.Resume();
        foreach (var layer in _layers)
            layer.Animator.Resume();
    }

    /// <summary>
    /// Stops the primary <see cref="Animator"/> and every layer animator simultaneously,
    /// clearing the active clip on each. No-op for animators that are already stopped.
    /// </summary>
    public void StopAllLayers()
    {
        Animator.Stop();
        foreach (var layer in _layers)
            layer.Animator.Stop();
    }

    private void SortLayers() => _layers.Sort(static (a, b) => a.Priority.CompareTo(b.Priority));

    /// <summary>
    /// <c>true</c> when the primary animator's current non-looping clip has finished.
    /// Equivalent to <see cref="SpriteAnimator.IsFinished"/> on <see cref="Animator"/>.
    /// </summary>
    public bool IsFinished => Animator.IsFinished;

    /// <summary>
    /// <c>true</c> when the primary animator is currently playing (not paused, not finished).
    /// Equivalent to <see cref="SpriteAnimator.IsPlaying"/> on <see cref="Animator"/>.
    /// </summary>
    public bool IsPlaying => Animator.IsPlaying;

    /// <summary>
    /// <c>true</c> when the primary animator has been explicitly paused.
    /// Equivalent to <see cref="SpriteAnimator.IsPaused"/> on <see cref="Animator"/>.
    /// </summary>
    public bool IsPaused => Animator.IsPaused;

    /// <summary>
    /// Gets the current frame of the primary animator, or <c>null</c> if no animation is active.
    /// Equivalent to <see cref="SpriteAnimator.CurrentFrame"/> on <see cref="Animator"/>.
    /// </summary>
    public SpriteFrame? CurrentFrame => Animator.CurrentFrame;
}