using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL.PostProcessing;

/// <summary>
/// Pre-configured post-processing effect chains for common visual styles.
/// </summary>
public static class PostProcessPresets
{
    /// <summary>
    /// Classic retro gaming look: Grayscale + scanlines + vignette.
    /// </summary>
    public static SDL3PostProcessPipeline AddRetroLook(
        this SDL3PostProcessPipeline pipeline, 
        nint device, 
        int width, 
        int height, 
        ILoggerFactory factory)
    {
        pipeline.AddEffectFactory(() => new Effects.GrayscaleEffect(device, width, height, factory) 
        { 
            Order = 0,
            Intensity = 0.8f 
        });
        
        // TODO: Add ScanlineEffect when implemented
        // pipeline.AddEffectFactory(() => new Effects.ScanlineEffect(device, width, height, factory) { Order = 1 });
        
        // TODO: Add VignetteEffect when implemented
        // pipeline.AddEffectFactory(() => new Effects.VignetteEffect(device, width, height, factory) { Order = 2 });
        
        return pipeline;
    }

    /// <summary>
    /// Dreamy blur effect: Soft blur + subtle bloom.
    /// </summary>
    public static SDL3PostProcessPipeline AddDreamyBlur(
        this SDL3PostProcessPipeline pipeline, 
        nint device, 
        int width, 
        int height, 
        ILoggerFactory factory)
    {
        pipeline.AddEffectFactory(() => new Effects.BlurEffect(device, width, height, factory) 
        { 
            Order = 0, 
            BlurRadius = 3.0f 
        });
        
        // TODO: Add BloomEffect when implemented
        // pipeline.AddEffectFactory(() => new Effects.BloomEffect(device, width, height, factory) { Order = 1 });
        
        return pipeline;
    }

    /// <summary>
    /// Heavy blur for depth-of-field or background blur effects.
    /// </summary>
    public static SDL3PostProcessPipeline AddHeavyBlur(
        this SDL3PostProcessPipeline pipeline, 
        nint device, 
        int width, 
        int height, 
        ILoggerFactory factory)
    {
        pipeline.AddEffectFactory(() => new Effects.BlurEffect(device, width, height, factory) 
        { 
            Order = 0, 
            BlurRadius = 8.0f,
            Enabled = true
        });
        
        return pipeline;
    }

    /// <summary>
    /// Film noir style: Heavy grayscale + high contrast.
    /// </summary>
    public static SDL3PostProcessPipeline AddFilmNoir(
        this SDL3PostProcessPipeline pipeline, 
        nint device, 
        int width, 
        int height, 
        ILoggerFactory factory)
    {
        pipeline.AddEffectFactory(() => new Effects.GrayscaleEffect(device, width, height, factory) 
        { 
            Order = 0,
            Intensity = 1.0f 
        });
        
        // TODO: Add ContrastEffect when implemented
        // pipeline.AddEffectFactory(() => new Effects.ContrastEffect(device, width, height, factory) 
        // { 
        //     Order = 1,
        //     Contrast = 1.5f 
        // });
        
        return pipeline;
    }
}