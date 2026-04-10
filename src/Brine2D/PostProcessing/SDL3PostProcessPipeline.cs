using Brine2D.Rendering;
using Brine2D.Rendering.PostProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL.PostProcessing;

/// <summary>
/// SDL3-specific implementation of the post-processing pipeline.
/// Handles GPU texture ping-ponging and command buffer management.
/// </summary>
public class SDL3PostProcessPipeline : PostProcessPipeline, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<Func<IPostProcessEffect>> _effectFactories = new();
    private readonly List<ISDL3PostProcessEffect> _enabledEffectsCache = new();
    private readonly HashSet<IPostProcessEffect> _ownedEffects = new();
    private bool _diEffectsResolved;
    private bool _factoriesProcessed;
    private int _disposed;
    private int _pendingWidth;
    private int _pendingHeight;

    public SDL3PostProcessPipeline(IServiceProvider serviceProvider, ILogger<SDL3PostProcessPipeline>? logger = null) 
        : base(logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Register an effect factory that will be called when the pipeline is first used.
    /// This allows effects to be created after the GPU device is initialized.
    /// </summary>
    public void AddEffectFactory(Func<IPostProcessEffect> factory)
    {
        _effectFactories.Add(factory);
        _factoriesProcessed = false;
    }

    /// <summary>
    /// Propagate new dimensions to all SDL3 post-process effects.
    /// Call this when render targets are recreated (e.g. on window resize).
    /// If effects haven't been initialized yet, dimensions are deferred until first Execute.
    /// </summary>
    public void SetEffectDimensions(int width, int height)
    {
        _pendingWidth = width;
        _pendingHeight = height;

        foreach (var effect in _effects.OfType<ISDL3PostProcessEffect>())
        {
            effect.SetDimensions(width, height);
        }
    }

    /// <summary>
    /// Execute all enabled SDL3 effects in order, ping-ponging between render targets.
    /// Returns true if any effects were applied, false otherwise.
    /// </summary>
    public bool Execute(IRenderer renderer, nint sourceTexture, nint targetTexture, nint commandBuffer, RenderTarget pingPongTarget, int targetWidth, int targetHeight)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);

        if (!_diEffectsResolved || !_factoriesProcessed)
        {
            InitializeEffects();
        }

        EnsureSorted();

        _enabledEffectsCache.Clear();
        foreach (var effect in _effects)
        {
            if (effect is ISDL3PostProcessEffect sdlEffect && sdlEffect.Enabled)
                _enabledEffectsCache.Add(sdlEffect);
        }

        if (_enabledEffectsCache.Count == 0)
        {
            return false;
        }

        nint lastValidOutput = sourceTexture;

        try
        {
            // For single effect: source -> target directly
            if (_enabledEffectsCache.Count == 1)
            {
                _enabledEffectsCache[0].Apply(renderer, sourceTexture, targetTexture, commandBuffer);
                return true;
            }

            // For multiple effects: ping-pong between targets
            nint currentSource = sourceTexture;
            nint currentTarget = pingPongTarget.TextureHandle;
            bool usePingPong = true;

            for (int i = 0; i < _enabledEffectsCache.Count; i++)
            {
                var isLastEffect = i == _enabledEffectsCache.Count - 1;

                // Last effect always writes to final target
                if (isLastEffect)
                {
                    currentTarget = targetTexture;
                }

                _enabledEffectsCache[i].Apply(renderer, currentSource, currentTarget, commandBuffer);
                lastValidOutput = currentTarget;

                // Swap for next iteration (if not last)
                if (!isLastEffect)
                {
                    currentSource = currentTarget;
                    currentTarget = usePingPong ? sourceTexture : pingPongTarget.TextureHandle;
                    usePingPong = !usePingPong;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing SDL3 post-process pipeline - falling back to passthrough");

            if (lastValidOutput == targetTexture)
                return true;

            try
            {
                FullScreenQuad.Blit(commandBuffer, lastValidOutput, targetTexture, 
                    pingPongTarget.Width, pingPongTarget.Height, targetWidth, targetHeight, _logger);
                return true;
            }
            catch (Exception fallbackEx)
            {
                _logger?.LogError(fallbackEx, "Fallback blit also failed");
                return false;
            }
        }
    }

    /// <summary>
    /// Remove a post-processing effect from the pipeline.
    /// </summary>
    public override bool RemoveEffect(IPostProcessEffect effect)
    {
        _ownedEffects.Remove(effect);
        return base.RemoveEffect(effect);
    }

    /// <summary>
    /// Clear all effects from the pipeline.
    /// </summary>
    public override void Clear()
    {
        foreach (var effect in _ownedEffects.OfType<IDisposable>())
        {
            try
            {
                effect.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing owned effect {EffectName}", effect.GetType().Name);
            }
        }

        _ownedEffects.Clear();
        base.Clear();
        _diEffectsResolved = false;
        _factoriesProcessed = false;
    }

    private void InitializeEffects()
    {
        if (!_diEffectsResolved)
        {
            var diEffects = _serviceProvider.GetServices<IPostProcessEffect>();
            foreach (var effect in diEffects)
            {
                AddEffect(effect);
                _logger?.LogInformation("{EffectName} initialized (DI)", effect.Name);
            }
            _diEffectsResolved = true;
        }

        if (!_factoriesProcessed)
        {
            _logger?.LogInformation("Initializing {Count} factory-registered post-process effects...", _effectFactories.Count);

            foreach (var factory in _effectFactories)
            {
                try
                {
                    var effect = factory();
                    AddEffect(effect);
                    _ownedEffects.Add(effect);
                    _logger?.LogInformation("{EffectName} initialized (factory)", effect.Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to initialize post-process effect from factory");
                }
            }

            _effectFactories.Clear();
            _factoriesProcessed = true;
        }

        if (_pendingWidth > 0 && _pendingHeight > 0)
        {
            foreach (var effect in _effects.OfType<ISDL3PostProcessEffect>())
            {
                effect.SetDimensions(_pendingWidth, _pendingHeight);
            }
        }

        _logger?.LogInformation("Post-processing pipeline ready with {Count} effect(s)", _effects.Count);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _logger?.LogDebug("Disposing post-process pipeline with {Count} effects ({Owned} owned)",
            _effects.Count, _ownedEffects.Count);

        foreach (var effect in _ownedEffects.OfType<IDisposable>())
        {
            try
            {
                effect.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing effect {EffectName}", effect.GetType().Name);
            }
        }

        _effects.Clear();
        _ownedEffects.Clear();
        _effectFactories.Clear();
        _enabledEffectsCache.Clear();
    }
}