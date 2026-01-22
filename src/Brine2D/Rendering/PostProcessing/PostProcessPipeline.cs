using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.PostProcessing;

/// <summary>
/// Abstract base class for managing and executing post-processing effects.
/// Backend implementations handle GPU-specific rendering details.
/// Similar to ASP.NET middleware pipeline pattern.
/// </summary>
public abstract class PostProcessPipeline
{
    protected readonly List<IPostProcessEffect> _effects = new();
    protected readonly ILogger? _logger;
    protected bool _isSorted;

    public IReadOnlyList<IPostProcessEffect> Effects => _effects.AsReadOnly();

    protected PostProcessPipeline(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Add a post-processing effect to the pipeline.
    /// </summary>
    public virtual PostProcessPipeline AddEffect(IPostProcessEffect effect)
    {
        if (effect == null)
            throw new ArgumentNullException(nameof(effect));

        _effects.Add(effect);
        _isSorted = false;
        _logger?.LogDebug("Added post-process effect: {EffectName} (order: {Order})", effect.Name, effect.Order);
        return this;
    }

    /// <summary>
    /// Remove a post-processing effect from the pipeline.
    /// </summary>
    public virtual bool RemoveEffect(IPostProcessEffect effect) => _effects.Remove(effect);

    /// <summary>
    /// Clear all effects from the pipeline.
    /// </summary>
    public virtual void Clear()
    {
        _effects.Clear();
        _isSorted = false;
    }

    /// <summary>
    /// Sort effects by order if needed.
    /// </summary>
    protected void EnsureSorted()
    {
        if (!_isSorted)
        {
            _effects.Sort((a, b) => a.Order.CompareTo(b.Order));
            _isSorted = true;

            _logger?.LogDebug("Post-process effect execution order:");
            foreach (var effect in _effects)
            {
                _logger?.LogDebug("  {Order}: {EffectName} (Enabled: {Enabled})", effect.Order, effect.Name, effect.Enabled);
            }
        }
    }
}