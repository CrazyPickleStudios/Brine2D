using System.Numerics;

namespace Brine2D.Animation;

/// <summary>
/// Drives a <see cref="SpriteAnimator"/> by selecting an animation clip based on two continuous
/// float parameters (e.g., horizontal and vertical velocity, directional aim angle + speed).
/// </summary>
/// <remarks>
/// <para>
/// Each node is a 2D position paired with a clip name. When <see cref="SetValue"/> is called,
/// or nodes are added or removed, the tree evaluates immediately so <see cref="ActiveClip"/> and
/// the underlying <see cref="SpriteAnimator"/> always reflect the current state.
/// </para>
/// <para>
/// Clip selection uses nearest-neighbor (Euclidean distance) — transitions are abrupt and occur
/// at the Voronoi boundary between nodes. Use <see cref="CrossFadeDuration"/> to soften the
/// cut visually.
/// </para>
/// <para>
/// When the two nearest nodes both carry a speed override, <see cref="SpriteAnimator.Speed"/>
/// is linearly interpolated between them based on the distance to each node. Speed values are
/// floored at <c>0.001</c> unless <see cref="AllowZeroSpeed"/> is <c>true</c>.
/// </para>
/// <para>
/// When <see cref="RespectNonLoopingClips"/> is <c>true</c> (the default), the tree will not
/// interrupt a non-looping clip that was started outside the tree. Set to <c>false</c> to give
/// the tree unconditional ownership of the animator.
/// </para>
/// </remarks>
public sealed class AnimationBlendSelector2D
{
    private readonly SpriteAnimator _animator;
    private readonly List<BlendNode2D> _nodes = new();
    private string? _activeClip;
    private float _x;
    private float _y;
    private bool _dirty;

    private const float PositionEpsilon = 1e-5f;

    /// <summary>Gets the X component of the current blend parameter.</summary>
    public float X => _x;

    /// <summary>Gets the Y component of the current blend parameter.</summary>
    public float Y => _y;

    /// <summary>
    /// Gets the name of the clip currently selected by the tree, or <c>null</c> if no nodes
    /// have been added.
    /// </summary>
    public string? ActiveClip => _activeClip;

    /// <summary>Gets the number of nodes currently registered in the tree.</summary>
    public int NodeCount => _nodes.Count;

    /// <summary>
    /// When greater than zero, node changes trigger a cross-fade of this duration (seconds)
    /// instead of a hard cut. Defaults to <c>0</c> (hard cut).
    /// </summary>
    public float CrossFadeDuration { get; set; }

    /// <summary>
    /// When <c>true</c> (default), the tree will not interrupt a non-looping clip that was
    /// started outside the tree. Set to <c>false</c> to give the tree unconditional ownership
    /// of the animator.
    /// </summary>
    /// <remarks>
    /// <b>Limitation:</b> the yield check is name-based. If a clip played via
    /// <see cref="SpriteAnimator.PlayDirect"/> or <see cref="SpriteAnimator.PlayDirectQueued"/>
    /// happens to share a name with one of this tree's nodes, and the tree previously drove that
    /// same clip, the tree will not yield to it. Avoid reusing node clip names for externally-driven
    /// one-shot clips to prevent this ambiguity.
    /// </remarks>
    public bool RespectNonLoopingClips { get; set; } = true;

    /// <summary>
    /// When <c>false</c> (default), node speed values are floored at <c>0.001</c> to prevent
    /// accidentally freezing the animator. Set to <c>true</c> when a node speed of exactly
    /// <c>0</c> is intentional.
    /// </summary>
    public bool AllowZeroSpeed { get; set; }

    /// <summary>
    /// When <c>false</c>, evaluation is a no-op and the tree stops driving the animator.
    /// The current clip continues playing untouched. Defaults to <c>true</c>.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Raised when the active clip selection changes. Provides the previous clip name (or
    /// <c>null</c> if none was active) and the new clip name.
    /// </summary>
    public event Action<string?, string>? OnClipChanged;

    public AnimationBlendSelector2D(SpriteAnimator animator)
    {
        ArgumentNullException.ThrowIfNull(animator);
        _animator = animator;
    }

    /// <summary>
    /// Adds a node associating a 2D position with a clip name. Evaluates immediately after adding.
    /// </summary>
    /// <param name="x">X coordinate in parameter space.</param>
    /// <param name="y">Y coordinate in parameter space.</param>
    /// <param name="clipName">Name of the clip to play when the parameter is nearest this node.</param>
    /// <param name="speed">
    /// Optional <see cref="SpriteAnimator.Speed"/> override at this node.
    /// When the two nearest nodes both carry a speed, the animator speed is linearly interpolated
    /// between them based on the distances to each node. A value of <c>0</c> freezes the animator
    /// when <see cref="AllowZeroSpeed"/> is <c>true</c>; otherwise it is floored at <c>0.001</c>.
    /// </param>
    public AnimationBlendSelector2D AddNode(float x, float y, string clipName, float? speed = null)
    {
        ArgumentNullException.ThrowIfNull(clipName);
        var pos = new Vector2(x, y);
        if (_nodes.Exists(n => Vector2.Distance(n.Position, pos) <= PositionEpsilon))
            throw new ArgumentException(
                $"A node at position ({x}, {y}) already exists in this blend tree. " +
                "Remove the existing node first or use a distinct position.",
                nameof(x));
        _nodes.Add(new BlendNode2D(pos, clipName, speed));
        _dirty = true;
        Evaluate();
        return this;
    }

    /// <summary>
    /// Removes the first node whose clip name equals <paramref name="clipName"/>.
    /// Returns <c>true</c> if a node was removed. Evaluates immediately after removing.
    /// </summary>
    public bool RemoveNode(string clipName)
    {
        ArgumentNullException.ThrowIfNull(clipName);
        var index = _nodes.FindIndex(n => n.ClipName == clipName);
        if (index < 0)
            return false;
        _nodes.RemoveAt(index);
        if (_nodes.Count == 0)
            _activeClip = null;
        _dirty = true;
        Evaluate();
        return true;
    }

    /// <summary>
    /// Removes the first node within <c>1e-5</c> of <paramref name="x"/>, <paramref name="y"/>.
    /// Returns <c>true</c> if a node was removed. Evaluates immediately after removing.
    /// </summary>
    public bool RemoveNode(float x, float y)
    {
        var pos = new Vector2(x, y);
        var index = _nodes.FindIndex(n => Vector2.Distance(n.Position, pos) <= PositionEpsilon);
        if (index < 0)
            return false;
        _nodes.RemoveAt(index);
        if (_nodes.Count == 0)
            _activeClip = null;
        _dirty = true;
        Evaluate();
        return true;
    }

    /// <summary>Removes all nodes from the tree and clears the active clip selection.</summary>
    public void ClearNodes()
    {
        _nodes.Clear();
        _activeClip = null;
        _dirty = false;
    }

    /// <summary>
    /// Checks that every node's clip name is registered on the underlying animator.
    /// Returns a list of human-readable issue strings. An empty list means all nodes are valid.
    /// Call this after adding nodes during setup, just as you would call
    /// <see cref="AnimationStateMachine.ValidateTransitions"/> for the state machine.
    /// </summary>
    public IReadOnlyList<string> ValidateNodes()
    {
        var issues = new List<string>();
        for (int i = 0; i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            if (!_animator.HasAnimation(node.ClipName))
                issues.Add($"Node[{i}] ({node.Position.X}, {node.Position.Y}): clip '{node.ClipName}' not found in animator.");
        }
        return issues;
    }

    /// <summary>
    /// Sets the 2D blend parameter and evaluates immediately so <see cref="ActiveClip"/> and
    /// the underlying <see cref="SpriteAnimator"/> reflect the new value right away.
    /// </summary>
    public void SetValue(float x, float y)
    {
        _x = x;
        _y = y;
        _dirty = true;
        Evaluate();
    }

    /// <summary>
    /// Captures an immutable snapshot of the tree's runtime state: X, Y, <see cref="ActiveClip"/>,
    /// <see cref="CrossFadeDuration"/>, <see cref="RespectNonLoopingClips"/>, and
    /// <see cref="AllowZeroSpeed"/>. Node definitions are not captured.
    /// Use <see cref="RestoreSnapshot"/> to revert.
    /// </summary>
    public AnimationBlendSelector2DSnapshot CaptureSnapshot() =>
        new(_x, _y, _activeClip, CrossFadeDuration, RespectNonLoopingClips, AllowZeroSpeed);

    /// <summary>
    /// Restores a previously captured snapshot, atomically applying X, Y, <see cref="ActiveClip"/>,
    /// <see cref="CrossFadeDuration"/>, <see cref="RespectNonLoopingClips"/>, and
    /// <see cref="AllowZeroSpeed"/>. Evaluates immediately so the animator reflects the restored state.
    /// </summary>
    public void RestoreSnapshot(AnimationBlendSelector2DSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _x = snapshot.X;
        _y = snapshot.Y;
        _activeClip = snapshot.ActiveClip;
        CrossFadeDuration = snapshot.CrossFadeDuration;
        RespectNonLoopingClips = snapshot.RespectNonLoopingClips;
        AllowZeroSpeed = snapshot.AllowZeroSpeed;
        _dirty = true;
        Evaluate();
    }

    /// <summary>
    /// Evaluates the tree against the current parameter values when dirty or when the active clip
    /// has finished. Called automatically by <see cref="Brine2D.Systems.Animation.AnimationSystem"/>
    /// each tick to handle once-clip restarts.
    /// </summary>
    internal void Evaluate()
    {
        if (!IsEnabled)
            return;

        bool needsRestart = _animator.IsFinished;
        if (!_dirty && !needsRestart)
            return;

        if (_nodes.Count == 0)
        {
            _dirty = false;
            return;
        }

        var pos = new Vector2(_x, _y);

        int bestIndex = 0;
        int secondIndex = -1;
        float bestDist = Vector2.Distance(pos, _nodes[0].Position);
        float secondDist = float.MaxValue;

        for (int i = 1; i < _nodes.Count; i++)
        {
            var d = Vector2.Distance(pos, _nodes[i].Position);
            if (d < bestDist)
            {
                secondIndex = bestIndex;
                secondDist = bestDist;
                bestIndex = i;
                bestDist = d;
            }
            else if (d < secondDist)
            {
                secondIndex = i;
                secondDist = d;
            }
        }

        var best = _nodes[bestIndex];

        float? blendedSpeed = best.Speed;
        if (secondIndex >= 0 && best.Speed.HasValue && _nodes[secondIndex].Speed.HasValue)
        {
            var totalDist = bestDist + secondDist;
            var t = totalDist > 0f ? secondDist / totalDist : 0f;
            blendedSpeed = best.Speed.Value + (_nodes[secondIndex].Speed.Value - best.Speed.Value) * (1f - t);
        }

        if (ActivateClip(best.ClipName, blendedSpeed, needsRestart))
            _dirty = false;
    }

    private bool ActivateClip(string clipName, float? speed, bool forceRestart = false)
    {
        if (RespectNonLoopingClips
            && _animator.IsPlaying
            && _animator.CurrentAnimation != null
            && !_animator.CurrentAnimation.Loop
            && _animator.CurrentAnimation.Name != _activeClip)
        {
            return false;
        }

        if (speed.HasValue)
            _animator.Speed = AllowZeroSpeed ? MathF.Max(speed.Value, 0f) : MathF.Max(speed.Value, 0.001f);

        bool clipChanged = _activeClip != clipName;
        bool animatorDrifted = _animator.CurrentAnimation?.Name != clipName;

        if (!clipChanged && !animatorDrifted && !forceRestart)
            return true;

        var previousClip = _activeClip;
        _activeClip = clipName;

        if (clipChanged)
            OnClipChanged?.Invoke(previousClip, clipName);

        if (clipChanged && CrossFadeDuration > 0f && _animator.IsPlaying)
            _animator.PlayWithCrossFade(clipName, CrossFadeDuration);
        else
            _animator.Play(clipName, restart: forceRestart && !clipChanged);

        return true;
    }

    private readonly record struct BlendNode2D(Vector2 Position, string ClipName, float? Speed);
}