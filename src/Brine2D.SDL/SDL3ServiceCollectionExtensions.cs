using Brine2D.Audio;
using Brine2D.Events;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.SDL;
using Brine2D.Rendering.SDL.PostProcessing;
using Brine2D.SDL.Audio;
using Brine2D.SDL.Common;
using Brine2D.SDL.Input;
using Brine2D.SDL.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Brine2D.SDL;

/// <summary>
/// Extensions for using SDL3 as the Brine2D backend.
/// </summary>
public static class SDL3ServiceCollectionExtensions
{
    /// <summary>
    /// Uses SDL3 as the backend for rendering, input, and audio.
    /// </summary>
    /// <param name="builder">The Brine2D builder.</param>
    /// <returns>The Brine2D builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method activates the SDL3 backend using the options configured
    /// in <see cref="Brine2DServiceCollectionExtensions.AddBrine2D"/>.
    /// </para>
    /// <para>
    /// This registers SDL3 implementations for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Window and event handling</description></item>
    /// <item><description>Input (keyboard, mouse, gamepad)</description></item>
    /// <item><description>Rendering (GPU or legacy)</description></item>
    /// <item><description>Audio (SDL3_mixer)</description></item>
    /// </list>
    /// <para>
    /// <strong>Note:</strong> This only registers backend implementations.
    /// For data-oriented systems, use <see cref="Brine2DServiceCollectionExtensions.UseSystems"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Object-oriented components (most users)
    /// builder.Services
    ///     .AddBrine2D(options => { /* ... */ })
    ///     .UseSDL();
    /// 
    /// // Data-oriented systems (advanced)
    /// builder.Services
    ///     .AddBrine2D(options => { /* ... */ })
    ///     .UseSystems()  // ← Core systems
    ///     .UseSDL();     // ← SDL backend
    /// </code>
    /// </example>
    public static Brine2DBuilder UseSDL(this Brine2DBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        var services = builder.Services;

        // Configure SDL-specific options from Brine2DOptions
        ConfigureWindowOptions(services);
        ConfigureRenderingOptions(services);
        ConfigureAudioOptions(services);
        ConfigureInputOptions(services);

        // Register SDL backend services in dependency order
        AddSDL3ApplicationLifetime(services);
        AddSDL3Input(services);
        AddSDL3Rendering(services);
        AddSDL3Audio(services);

        return builder;
    }
    
    #region Options Configuration
    
    private static void ConfigureWindowOptions(IServiceCollection services)
    {
        services.AddOptions<WindowOptions>()
            .BindConfiguration($"{Brine2DOptions.SectionName}:{WindowOptions.SectionName}")
            .Configure<IOptions<Brine2DOptions>>((windowOpts, brineOpts) =>
            {
                var window = brineOpts.Value.Window;
                windowOpts.Title = window.Title;
                windowOpts.Width = window.Width;
                windowOpts.Height = window.Height;
                windowOpts.Fullscreen = window.Fullscreen;
                windowOpts.Resizable = window.Resizable;
                windowOpts.Maximized = window.Maximized;
                windowOpts.Borderless = window.Borderless;
            });
    }
    
    private static void ConfigureRenderingOptions(IServiceCollection services)
    {
        services.AddOptions<RenderingOptions>()
            .BindConfiguration($"{Brine2DOptions.SectionName}:{RenderingOptions.SectionName}")
            .Configure<IOptions<Brine2DOptions>>((renderOpts, brineOpts) =>
            {
                var render = brineOpts.Value.Rendering;
                renderOpts.Backend = render.Backend;
                renderOpts.VSync = render.VSync;
                renderOpts.PreferredGPUDriver = render.PreferredGPUDriver;
                renderOpts.TargetFPS = render.TargetFPS;
                renderOpts.ClearColor = render.ClearColor;
            });
    }
    
    private static void ConfigureAudioOptions(IServiceCollection services)
    {
        services.AddOptions<AudioOptions>()
            .BindConfiguration($"{Brine2DOptions.SectionName}:{AudioOptions.SectionName}")
            .Configure<IOptions<Brine2DOptions>>((audioOpts, brineOpts) =>
            {
                var audio = brineOpts.Value.Audio;
                audioOpts.MaxTracks = audio.MaxTracks;
                audioOpts.MasterVolume = audio.MasterVolume;
                audioOpts.MusicVolume = audio.MusicVolume;
                audioOpts.SoundVolume = audio.SoundVolume;
                audioOpts.Enabled = audio.Enabled;
            });
    }
    
    private static void ConfigureInputOptions(IServiceCollection services)
    {
        services.AddOptions<InputOptions>()
            .BindConfiguration($"{Brine2DOptions.SectionName}:{InputOptions.SectionName}")
            .Configure<IOptions<Brine2DOptions>>((inputOpts, brineOpts) =>
            {
                var input = brineOpts.Value.Input;
                inputOpts.EnableGamepad = input.EnableGamepad;
                inputOpts.MaxGamepads = input.MaxGamepads;
                inputOpts.EnableRumble = input.EnableRumble;
                inputOpts.GamepadDeadZone = input.GamepadDeadZone;
            });
    }
    
    #endregion
    
    #region SDL Backend Service Registration
    
    /// <summary>
    /// Adds SDL3 application lifetime and event handling.
    /// </summary>
    private static void AddSDL3ApplicationLifetime(IServiceCollection services)
    {
        // Register internal event bus (keyed service for SDL internals)
        services.AddKeyedSingleton<EventBus>("SDL_Internal", (sp, _) =>
            new EventBus(sp.GetService<ILogger<EventBus>>()));
        
        // Register SDL3EventPump
        services.TryAddSingleton<SDL3EventPump>(sp => new SDL3EventPump(
            sp.GetRequiredService<ILogger<SDL3EventPump>>(),
            sp.GetRequiredService<EventBus>(),
            sp.GetRequiredKeyedService<EventBus>("SDL_Internal"),
            sp.GetRequiredService<IHostApplicationLifetime>()
        ));
        
        // Register as IEventPump (IHostApplicationLifetime comes from host)
        services.TryAddSingleton<IEventPump>(sp => 
            sp.GetRequiredService<SDL3EventPump>());
    }
    
    /// <summary>
    /// Adds SDL3 input services (keyboard, mouse, gamepad).
    /// </summary>
    private static void AddSDL3Input(IServiceCollection services)
    {
        // Register SDL3 input context
        services.TryAddSingleton<IInputContext>(sp => new SDL3InputContext(
            sp.GetRequiredService<ILogger<SDL3InputContext>>(),
            sp.GetRequiredService<EventBus>(),
            sp.GetRequiredKeyedService<EventBus>("SDL_Internal"),
            sp.GetService<ISDL3WindowProvider>()
        ));

        // Register input layer manager (for input middleware pattern)
        services.TryAddSingleton<InputLayerManager>();
    }
    
    /// <summary>
    /// Adds SDL3 rendering services (GPU or legacy renderer).
    /// </summary>
    private static void AddSDL3Rendering(IServiceCollection services)
    {
        services.TryAddSingleton<IFontLoader, SDL3FontLoader>();

        // Register renderer based on configured backend
        services.TryAddSingleton<IRenderer>(provider =>
        {
            var renderingOptions = provider.GetRequiredService<IOptions<RenderingOptions>>();
            var windowOptions = provider.GetRequiredService<IOptions<WindowOptions>>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("SDL3Rendering");
            var fontLoader = provider.GetService<IFontLoader>();
            var eventBus = provider.GetService<EventBus>();
            var postProcessingOptions = provider.GetService<IOptions<PostProcessingOptions>>();
            var postProcessPipeline = provider.GetService<SDL3PostProcessPipeline>();

            // Warn if post-processing is enabled with legacy renderer
            if (postProcessingOptions?.Value?.Enabled == true && 
                renderingOptions.Value.Backend == GraphicsBackend.LegacyRenderer)
            {
                logger.LogWarning(
                    "Post-processing is not supported with LegacyRenderer backend. " +
                    "Switch to GraphicsBackend.GPU to enable post-processing effects. " +
                    "Post-processing will be disabled.");
            }

            return renderingOptions.Value.Backend switch
            {
                GraphicsBackend.GPU => new SDL3GPURenderer(
                    provider.GetRequiredService<ILogger<SDL3GPURenderer>>(),
                    loggerFactory,
                    renderingOptions,
                    windowOptions,
                    postProcessingOptions,
                    postProcessPipeline,
                    fontLoader,
                    eventBus), 
                GraphicsBackend.LegacyRenderer => new SDL3Renderer(
                    provider.GetRequiredService<ILogger<SDL3Renderer>>(),
                    loggerFactory,
                    renderingOptions,
                    windowOptions,
                    fontLoader,
                    eventBus),
                GraphicsBackend.Auto => new SDL3GPURenderer(
                    provider.GetRequiredService<ILogger<SDL3GPURenderer>>(),
                    loggerFactory,
                    renderingOptions,
                    windowOptions,
                    postProcessingOptions,
                    postProcessPipeline,
                    fontLoader,
                    eventBus), 
                _ => throw new NotSupportedException(
                    $"Backend {renderingOptions.Value.Backend} is not supported. " +
                    $"Supported backends: {GraphicsBackend.GPU}, {GraphicsBackend.LegacyRenderer}, {GraphicsBackend.Auto}")
            };
        });

        // Register window provider from renderer
        services.AddSingleton<ISDL3WindowProvider>(sp =>
            (ISDL3WindowProvider)sp.GetRequiredService<IRenderer>());

        // Register texture context from renderer
        services.TryAddSingleton<ITextureContext>(provider => 
            (ITextureContext)provider.GetRequiredService<IRenderer>());

        // Register texture loader
        services.TryAddSingleton<ITextureLoader, SDL3TextureLoader>();
    }
    
    /// <summary>
    /// Adds SDL3_mixer audio services (sound effects, music, spatial audio).
    /// </summary>
    private static void AddSDL3Audio(IServiceCollection services)
    {
        services.TryAddSingleton<IAudioService, SDL3AudioService>();
    }
    
    #endregion
}