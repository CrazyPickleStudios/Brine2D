using Brine2D.Rendering;
using Brine2D.Rendering.PostProcessing;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL.PostProcessing;

/// <summary>
/// SDL3-specific implementation of the post-processing pipeline.
/// Handles GPU texture ping-ponging and command buffer management.
/// </summary>
public class SDL3PostProcessPipeline : PostProcessPipeline, IDisposable
{
    private readonly List<Func<IPostProcessEffect>> _effectFactories = new();
    private bool _effectsInitialized = false;
    private bool _disposed;

    public SDL3PostProcessPipeline(ILogger<SDL3PostProcessPipeline>? logger = null) 
        : base(logger)
    {
    }

    /// <summary>
    /// Register an effect factory that will be called when the pipeline is first used.
    /// This allows effects to be created after the GPU device is initialized.
    /// </summary>
    public void AddEffectFactory(Func<IPostProcessEffect> factory)
    {
        _effectFactories.Add(factory);
        _effectsInitialized = false;
    }

    /// <summary>
    /// Execute all enabled SDL3 effects in order, ping-ponging between render targets.
    /// Returns true if any effects were applied, false otherwise.
    /// </summary>
    public bool Execute(IRenderer renderer, nint sourceTexture, nint targetTexture, nint commandBuffer, RenderTarget pingPongTarget)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SDL3PostProcessPipeline));

        // Initialize effects lazily on first execution
        if (!_effectsInitialized)
        {
            InitializeEffects();
        }

        EnsureSorted();

        var enabledEffects = _effects.OfType<ISDL3PostProcessEffect>().Where(e => e.Enabled).ToList();
        
        if (enabledEffects.Count == 0)
        {
            return false;
        }

        try
        {
            // For single effect: source -> target directly
            if (enabledEffects.Count == 1)
            {
                enabledEffects[0].Apply(renderer, sourceTexture, targetTexture, commandBuffer);
                return true;
            }

            // For multiple effects: ping-pong between targets
            nint currentSource = sourceTexture;
            nint currentTarget = pingPongTarget.TextureHandle;
            bool usePingPong = true;

            for (int i = 0; i < enabledEffects.Count; i++)
            {
                var isLastEffect = i == enabledEffects.Count - 1;

                // Last effect always writes to final target
                if (isLastEffect)
                {
                    currentTarget = targetTexture;
                }

                enabledEffects[i].Apply(renderer, currentSource, currentTarget, commandBuffer);

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
            
            // Fallback: Simple blit to keep rendering
            try
            {
                FullScreenQuad.Blit(commandBuffer, sourceTexture, targetTexture, 
                    pingPongTarget.Width, pingPongTarget.Height, _logger);
                return true; // Effect failed but screen rendered
            }
            catch (Exception fallbackEx)
            {
                _logger?.LogError(fallbackEx, "Fallback blit also failed");
                return false; // Complete failure
            }
        }
    }

    private void InitializeEffects()
    {
        _logger?.LogInformation("Initializing {Count} post-process effects...", _effectFactories.Count);
        
        foreach (var factory in _effectFactories)
        {
            try
            {
                var effect = factory();
                AddEffect(effect);
                _logger?.LogInformation("{EffectName} initialized", effect.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize post-process effect from factory");
            }
        }
        _effectsInitialized = true;
        _logger?.LogInformation("Post-processing pipeline ready with {Count} effect(s)", _effects.Count);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger?.LogDebug("Disposing post-process pipeline with {Count} effects", _effects.Count);

        foreach (var effect in _effects.OfType<IDisposable>())
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
        _effectFactories.Clear();
        _disposed = true;
    }
}