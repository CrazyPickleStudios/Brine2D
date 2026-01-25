using Brine2D.Audio;
using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Rendering;
using Brine2D.Rendering.SDL;
using Brine2D.SDL.Audio;
using Brine2D.SDL.Common;
using Brine2D.SDL.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Brine2D.SDL;

/// <summary>
/// Provides convenience extension methods for registering Brine2D services with sensible defaults.
/// </summary>
/// <remarks>
/// These methods follow ASP.NET Core conventions by providing "convention over configuration"
/// while still allowing explicit registration for advanced scenarios.
/// </remarks>
public static class Brine2DServiceCollectionExtensions
{
    /// <summary>
    /// Adds Brine2D with SDL3 backend and sensible defaults.
    /// </summary>
    /// <remarks>
    /// This method registers:
    /// <list type="bullet">
    /// <item><description>Core engine services (EventBus, GameTime)</description></item>
    /// <item><description>Engine services (SceneManager, GameLoop, GameContext)</description></item>
    /// <item><description>SDL3 application lifetime and event handling</description></item>
    /// <item><description>SDL3 input system (keyboard, mouse, gamepad)</description></item>
    /// <item><description>SDL3 rendering with GPU backend</description></item>
    /// <item><description>SDL3_mixer audio system (sound effects, music, spatial audio)</description></item>
    /// </list>
    /// <para>
    /// For advanced scenarios requiring explicit service registration or custom backends,
    /// use the individual Add* methods directly.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddBrine2D(
        this IServiceCollection services,
        Action<RenderingOptions>? configureRendering = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Register services in order of dependencies
        services.AddBrineCore();
        services.AddBrineEngine();
        services.AddSDL3ApplicationLifetime();
        services.AddSDL3Input();
        services.AddSDL3Rendering(configureRendering);
        
        // Audio included by default (batteries included!)
        services.AddSDL3Audio();

        return services;
    }
}