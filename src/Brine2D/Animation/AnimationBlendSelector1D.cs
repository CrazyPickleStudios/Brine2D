namespace Brine2D.Animation;

/// <summary>
///     Drives a <see cref="SpriteAnimator" /> by selecting and transitioning between animation clips
///     based on a single continuous float value (e.g., movement speed, health ratio, aim angle).
/// </summary>
/// <remarks>
///     When two adjacent nodes both have a speed override, <see cref="SpriteAnimator.Speed" /> is
///     linearly interpolated between them as <see cref="Value" /> moves between their thresholds.
///     <para>
///         When <see cref="RespectNonLoopingClips" /> is <c>true</c> (default), the tree yields to
///         non-looping clips started outside the tree and resumes once they finish.
///     </para>
///     <para>
///         Set <see cref="CrossFadeDuration" /> to cross-fade on node changes instead of hard-cutting.
///     </para>
/// </remarks>
public sealed class AnimationBlendSelector1D
{
    private const float ThresholdEpsilon = 1e-6f;
    private readonly SpriteAnimator _animator;
    private readonly List<BlendNode> _nodes = new();
    private bool _dirty;
    private float _value;

    public AnimationBlendSelector1D(SpriteAnimator animator)
    {
        ArgumentNullException.ThrowIfNull(animator);
        _animator = animator;
    }

    /// <summary>
    ///     Raised when the active clip selection changes. Provides the previous clip name (or
    ///     <c>null</c> if none was active) and the new clip name.
    /// </summary>
    public event Action<string?, string>? OnClipChanged;

    /// <summary>
    ///     Gets the name of the clip currently selected by the tree, or <c>null</c> if no nodes
    ///     have been added.
    /// </summary>
    public string? ActiveClip { get; private set; }

    /// <summary>
    ///     When <c>false</c> (default), node speed values are floored at <c>0.001</c> to prevent
    ///     accidentally freezing the animator. Set to <c>true</c> when a node speed of exactly
    ///     <c>0</c> is intentional (e.g. a freeze-at-threshold behaviour).
    /// </summary>
    public bool AllowZeroSpeed { get; set; }

    /// <summary>
    ///     When greater than zero, node changes trigger a cross-fade of this duration (seconds)
    ///     instead of a hard cut. Defaults to <c>0</c>.
    ///     <para>
    ///         The fade is only initiated on clip <em>changes</em>; speed-only updates within the same
    ///         node do not start a new fade.
    ///     </para>
    /// </summary>
    public float CrossFadeDuration { get; set; }

    /// <summary>
    ///     When <c>false</c>, evaluation is a no-op and the tree stops driving the animator.
    ///     The current clip continues playing untouched. Defaults to <c>true</c>.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets the number of nodes currently registered in the tree.</summary>
    public int NodeCount => _nodes.Count;

    /// <summary>
    ///     When <c>true</c> (default), the tree will not interrupt a non-looping clip that was
    ///     started outside the tree. Set to <c>false</c> to give the tree unconditional ownership
    ///     of the animator.
    /// </summary>
    /// <remarks>
    ///     The yield check is name-based — if an externally-driven one-shot clip shares a name with
    ///     a node the tree previously drove, the tree will not yield to it. Avoid reusing node clip
    ///     names for externally-driven one-shot clips.
    /// </remarks>
    public bool RespectNonLoopingClips { get; set; } = true;

    /// <summary>
    ///     The current blend parameter. Setting this triggers an immediate evaluation so
    ///     <see cref="ActiveClip" /> and the underlying <see cref="SpriteAnimator" /> reflect the
    ///     new value right away.
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            _value = value;
            _dirty = true;
            Evaluate();
        }
    }

    /// <summary>
    ///     Adds a node associating a threshold value with a clip name.
    ///     Nodes are kept sorted by threshold ascending. Evaluates immediately after adding.
    /// </summary>
    /// <param name="threshold">
    ///     The parameter value at which this clip becomes the primary candidate.
    /// </param>
    /// <param name="clipName">
    ///     Name of the clip to play when <see cref="Value" /> is closest to this threshold.
    /// </param>
    /// <param name="speed">
    ///     Optional speed override at this threshold. Adjacent nodes with speed overrides interpolate
    ///     <see cref="SpriteAnimator.Speed" /> between them. Floored at <c>0.001</c> unless
    ///     <see cref="AllowZeroSpeed" /> is set.
    /// </param>
    public AnimationBlendSelector1D AddNode(float threshold, string clipName, float? speed = null)
    {
        ArgumentNullException.ThrowIfNull(clipName);
        if (_nodes.Exists(n => MathF.Abs(n.Threshold - threshold) <= ThresholdEpsilon))
        {
            throw new ArgumentException(
                $"A node with threshold {threshold} already exists in this blend tree. " +
                "Remove the existing node first or use a distinct threshold value.",
                nameof(threshold));
        }

        _nodes.Add(new BlendNode(threshold, clipName, speed));
        _nodes.Sort(static (a, b) => a.Threshold.CompareTo(b.Threshold));
        _dirty = true;
        Evaluate();
        return this;
    }

    /// <summary>
    ///     Captures the tree's current runtime state. Node definitions are not included.
    /// </summary>
    public AnimationBlendSelector1DSnapshot CaptureSnapshot()
    {
        return new AnimationBlendSelector1DSnapshot(_value, ActiveClip, CrossFadeDuration, RespectNonLoopingClips,
            AllowZeroSpeed);
    }

    /// <summary>Removes all nodes from the tree.</summary>
    public void ClearNodes()
    {
        _nodes.Clear();
        ActiveClip = null;
        _dirty = false;
    }

    /// <summary>
    ///     Removes the first node whose threshold is within <c>1e-6</c> of <paramref name="threshold" />.
    ///     Returns <c>true</c> if a node was removed. Evaluates immediately after removing.
    /// </summary>
    public bool RemoveNode(float threshold)
    {
        var index = _nodes.FindIndex(n => MathF.Abs(n.Threshold - threshold) <= ThresholdEpsilon);
        if (index < 0)
        {
            return false;
        }

        _nodes.RemoveAt(index);
        if (_nodes.Count == 0)
        {
            ActiveClip = null;
        }

        _dirty = true;
        Evaluate();
        return true;
    }

    /// <summary>
    ///     Removes the first node whose clip name equals <paramref name="clipName" />.
    ///     Returns <c>true</c> if a node was removed. Evaluates immediately after removing.
    /// </summary>
    public bool RemoveNode(string clipName)
    {
        ArgumentNullException.ThrowIfNull(clipName);
        var index = _nodes.FindIndex(n => n.ClipName == clipName);
        if (index < 0)
        {
            return false;
        }

        _nodes.RemoveAt(index);
        if (_nodes.Count == 0)
        {
            ActiveClip = null;
        }

        _dirty = true;
        Evaluate();
        return true;
    }

    /// <summary>
    ///     Restores a previously captured snapshot and evaluates immediately.
    /// </summary>
    public void RestoreSnapshot(AnimationBlendSelector1DSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _value = snapshot.Value;
        ActiveClip = snapshot.ActiveClip;
        CrossFadeDuration = snapshot.CrossFadeDuration;
        RespectNonLoopingClips = snapshot.RespectNonLoopingClips;
        AllowZeroSpeed = snapshot.AllowZeroSpeed;
        _dirty = true;
        Evaluate();
    }

    /// <summary>
    ///     Checks that every node's clip name is registered on the underlying animator.
    ///     Returns a list of human-readable issue strings. An empty list means all nodes are valid.
    ///     Call this after adding nodes during setup, just as you would call
    ///     <see cref="AnimationStateMachine.ValidateTransitions" /> for the state machine.
    /// </summary>
    public IReadOnlyList<string> ValidateNodes()
    {
        var issues = new List<string>();
        for (var i = 0; i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            if (!_animator.HasAnimation(node.ClipName))
            {
                issues.Add($"Node[{i}] threshold={node.Threshold}: clip '{node.ClipName}' not found in animator.");
            }
        }

        return issues;
    }

    /// <summary>
    ///     Evaluates the tree against the current <see cref="Value" /> and updates the animator when
    ///     dirty or when the active clip has finished. Called automatically by
    ///     <see cref="Brine2D.Systems.Animation.AnimationSystem" /> each tick to handle once-clip restarts.
    /// </summary>
    internal void Evaluate()
    {
        if (!IsEnabled)
        {
            return;
        }

        var needsRestart = _animator.IsFinished;
        if (!_dirty && !needsRestart)
        {
            return;
        }

        if (_nodes.Count == 0)
        {
            _dirty = false;
            return;
        }

        bool activated;

        if (_nodes.Count == 1)
        {
            activated = ActivateClip(_nodes[0].ClipName, _nodes[0].Speed, needsRestart);
        }
        else if (_value <= _nodes[0].Threshold)
        {
            activated = ActivateClip(_nodes[0].ClipName, _nodes[0].Speed, needsRestart);
        }
        else if (_value >= _nodes[^1].Threshold)
        {
            activated = ActivateClip(_nodes[^1].ClipName, _nodes[^1].Speed, needsRestart);
        }
        else
        {
            activated = false;
            for (var i = 0; i < _nodes.Count - 1; i++)
            {
                var lo = _nodes[i];
                var hi = _nodes[i + 1];

                if (_value >= lo.Threshold && _value < hi.Threshold)
                {
                    var range = hi.Threshold - lo.Threshold;
                    var t = range > 0f ? (_value - lo.Threshold) / range : 0f;

                    var selected = t < 0.5f ? lo : hi;

                    float? blendedSpeed;
                    if (lo.Speed.HasValue && hi.Speed.HasValue)
                    {
                        blendedSpeed = lo.Speed.Value + (hi.Speed.Value - lo.Speed.Value) * t;
                    }
                    else
                    {
                        blendedSpeed = selected.Speed;
                    }

                    activated = ActivateClip(selected.ClipName, blendedSpeed, needsRestart);
                    break;
                }
            }

            if (!activated)
            {
                activated = ActivateClip(_nodes[^1].ClipName, _nodes[^1].Speed, needsRestart);
            }
        }

        if (activated)
        {
            _dirty = false;
        }
    }

    private bool ActivateClip(string clipName, float? speed, bool forceRestart = false)
    {
        if (RespectNonLoopingClips
            && _animator.IsPlaying
            && _animator.CurrentAnimation != null
            && !_animator.CurrentAnimation.Loop
            && _animator.CurrentAnimation.Name != ActiveClip)
        {
            return false;
        }

        if (speed.HasValue)
        {
            _animator.Speed = AllowZeroSpeed ? MathF.Max(speed.Value, 0f) : MathF.Max(speed.Value, 0.001f);
        }

        var clipChanged = ActiveClip != clipName;
        var animatorDrifted = _animator.CurrentAnimation?.Name != clipName;

        if (!clipChanged && !animatorDrifted && !forceRestart)
        {
            return true;
        }

        var previousClip = ActiveClip;
        ActiveClip = clipName;

        if (clipChanged)
        {
            OnClipChanged?.Invoke(previousClip, clipName);
        }

        if (clipChanged && CrossFadeDuration > 0f && _animator.IsPlaying)
        {
            _animator.PlayWithCrossFade(clipName, CrossFadeDuration);
        }
        else
        {
            _animator.Play(clipName, forceRestart && !clipChanged);
        }

        return true;
    }

    private readonly record struct BlendNode(float Threshold, string ClipName, float? Speed);
}