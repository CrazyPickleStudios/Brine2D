using Brine2D.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Brine2D.Hosting;

/// <summary>
/// Extensions for configuring rendering backends.
/// </summary>
public static class Brine2DRendererExtensions
{
    /// <summary>
    /// Explicitly configures the GPU renderer (hardware-accelerated).
    /// This is the default renderer, but can be called for clarity.
    /// </summary>
    /// <param name="builder">The Brine2D builder.</param>
    /// <param name="configure">Optional configuration for GPU-specific settings.</param>
    /// <returns>The Brine2D builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The GPU renderer uses modern graphics APIs (Vulkan, Metal, D3D12) for
    /// hardware-accelerated rendering with automatic batching.
    /// </para>
    /// <para>
    /// <strong>This is the default renderer.</strong> You only need to call this
    /// method if you want to configure GPU-specific options like VSync or target FPS.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Default (GPU renderer, no configuration needed)
    /// builder.Services.AddBrine2D().UseSDL();
    /// 
    /// // Explicit configuration
    /// builder.Services
    ///     .AddBrine2D()
    ///     .UseGPURenderer(gpu => gpu
    ///         .WithVSync(true)
    ///         .WithTargetFPS(144)
    ///         .WithDriver("vulkan"))
    ///     .UseSDL();
    /// </code>
    /// </example>
    public static Brine2DBuilder UseGPURenderer(
        this Brine2DBuilder builder,
        Action<GPURendererOptions>? configure = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.Services.Configure<RenderingOptions>(options =>
        {
            options.Backend = GraphicsBackend.GPU;
        });

        if (configure != null)
        {
            var gpuOptions = new GPURendererOptions();
            configure(gpuOptions);
            
            builder.Services.Configure<RenderingOptions>(options =>
            {
                options.VSync = gpuOptions.VSync;
                options.TargetFPS = gpuOptions.TargetFPS;
                options.PreferredGPUDriver = gpuOptions.PreferredDriver;
            });
        }

        return builder;
    }

    /// <summary>
    /// Configures the legacy SDL renderer (software/basic hardware rendering).
    /// Only use this for compatibility with older systems or debugging.
    /// </summary>
    /// <param name="builder">The Brine2D builder.</param>
    /// <returns>The Brine2D builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The legacy renderer uses SDL's older rendering API. It's slower and has
    /// fewer features than the GPU renderer.
    /// </para>
    /// <para>
    /// <strong>Use only if:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Debugging rendering issues</description></item>
    /// <item><description>Running on very old hardware</description></item>
    /// <item><description>Platform doesn't support GPU renderer</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddBrine2D()
    ///     .UseLegacyRenderer() // Explicit opt-in
    ///     .UseSDL();
    /// </code>
    /// </example>
    public static Brine2DBuilder UseLegacyRenderer(this Brine2DBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.Services.Configure<RenderingOptions>(options =>
        {
            options.Backend = GraphicsBackend.LegacyRenderer;
        });

        return builder;
    }
}

/// <summary>
/// Configuration options for GPU renderer.
/// </summary>
public class GPURendererOptions
{
    /// <summary>
    /// Gets or sets whether VSync is enabled.
    /// </summary>
    public bool VSync { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the target frames per second (0 = unlimited).
    /// </summary>
    public int TargetFPS { get; set; } = 0;
    
    /// <summary>
    /// Gets or sets the preferred GPU driver ("vulkan", "metal", "d3d12", null = auto).
    /// </summary>
    public string? PreferredDriver { get; set; }

    /// <summary>
    /// Enables VSync (limits FPS to display refresh rate).
    /// </summary>
    public GPURendererOptions WithVSync(bool enabled = true)
    {
        VSync = enabled;
        return this;
    }

    /// <summary>
    /// Sets the target frames per second (0 = unlimited).
    /// </summary>
    public GPURendererOptions WithTargetFPS(int fps)
    {
        TargetFPS = fps;
        return this;
    }

    /// <summary>
    /// Sets the preferred GPU driver.
    /// </summary>
    /// <param name="driver">"vulkan", "metal", "d3d12", or null for automatic selection.</param>
    public GPURendererOptions WithDriver(string? driver)
    {
        PreferredDriver = driver;
        return this;
    }
}