using Brine2D.Audio;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.SDL;
using Brine2D.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Brine2D.Events;

namespace Brine2D.Hosting;

/// <summary>
/// Builder for configuring and creating a game application.
/// </summary>
public class GameApplicationBuilder
{
    private readonly HostApplicationBuilder _hostBuilder;
    private Brine2DOptions _options = new();
    private bool _built = false;
    private readonly HashSet<Type> _registeredScenes = new();
    private readonly List<Type> _scenesToValidate = new();

    internal GameApplicationBuilder(string[] args, HostApplicationBuilderSettings? settings = null)
    {
        _hostBuilder = settings != null
            ? Host.CreateApplicationBuilder(settings)
            : Host.CreateApplicationBuilder(args);

        Logging.AddConsole();
        Logging.SetMinimumLevel(LogLevel.Information);
    }

    /// <summary>
    /// Gets the service collection.
    /// </summary>
    public IServiceCollection Services => _hostBuilder.Services;

    /// <summary>
    /// Gets the logging builder.
    /// </summary>
    public ILoggingBuilder Logging => _hostBuilder.Logging;

    /// <summary>
    /// Gets the host environment.
    /// </summary>
    public IHostEnvironment HostEnvironment => _hostBuilder.Environment;

    /// <summary>
    /// Configures Brine2D options.
    /// </summary>
    public GameApplicationBuilder Configure(Action<Brine2DOptions> configure)
    {
        EnsureNotBuilt();
        configure(_options);
        return this;
    }

    /// <summary>
    /// Registers a scene and defers dependency validation to <see cref="Build"/>,
    /// ensuring all engine services are visible when validation runs.
    /// </summary>
    /// <remarks>
    /// Registration is optional; scenes can be loaded without it via ActivatorUtilities.
    /// Registering provides startup-time dependency validation and slightly better
    /// DI resolution performance.
    /// </remarks>
    public GameApplicationBuilder AddScene<T>() where T : Scene
    {
        EnsureNotBuilt();
        var sceneType = typeof(T);
        _registeredScenes.Add(sceneType);
        Services.AddTransient<T>();
        _scenesToValidate.Add(sceneType);
        return this;
    }

    private Action<IEntityWorld>? _sceneConfiguration;

    /// <summary>
    /// Configures the entity world for every scene loaded during this game's lifetime.
    /// Called after default systems are added, so you can disable, replace, or extend them.
    /// Can be called multiple times; delegates are additive.
    /// </summary>
    /// <example>
    /// <code>
    /// // Disable a default system project-wide
    /// builder.ConfigureScene(world =>
    ///     world.GetSystem&lt;ParticleSystem&gt;()!.IsEnabled = false);
    ///
    /// // Add a custom system to every scene
    /// builder.ConfigureScene(world =>
    ///     world.AddSystem&lt;MyDebugOverlaySystem&gt;());
    /// </code>
    /// </example>
    public GameApplicationBuilder ConfigureScene(Action<IEntityWorld> configure)
    {
        EnsureNotBuilt();
        _sceneConfiguration += configure;
        return this;
    }

    /// <summary>
    /// Builds the game application.
    /// This method can only be called once per builder instance.
    /// For multiple game instances (e.g., dedicated server), create separate builders.
    /// </summary>
    /// <example>
    /// <code>
    /// // This will fail
    /// var builder = GameApplication.CreateBuilder();
    /// var game1 = builder.Build();
    /// var game2 = builder.Build(); // InvalidOperationException
    ///
    /// // Do this instead
    /// var game1 = GameApplication.CreateBuilder().Configure(...).Build();
    /// var game2 = GameApplication.CreateBuilder().Configure(...).Build();
    /// </code>
    /// </example>
    public GameApplication Build()
    {
        EnsureNotBuilt();
        _built = true;

        // Register scene tracking set, options, and project-level scene configuration
        Services.AddSingleton(_registeredScenes);
        Services.AddSingleton(_options);
        Services.AddSingleton(_options.Window);
        Services.AddSingleton(_options.Rendering);
        Services.AddSingleton(_options.ECS);
        Services.AddSingleton(new SceneWorldConfiguration(_sceneConfiguration));

        // Register Brine2D core services
        Services.AddBrine2D();

        // Register backend services
        if (!_options.Headless)
        {
            Services.AddSDL3EventPump();
            Services.AddSDL3Rendering();
            Services.AddSDL3Input();
            Services.AddSDL3Audio();
        }
        else
        {
            Services.AddSingleton<IRenderer, HeadlessRenderer>();
            Services.AddSingleton<IInputContext, HeadlessInputContext>();
            Services.AddSingleton<IAudioService, HeadlessAudioService>();
            Services.AddSingleton<IEventPump, HeadlessEventPump>();
        }

        // 1. Validate option values via DataAnnotations
        _options.Validate();

        // 2. Validate all required services are registered
        ValidateServiceRegistrations();

        // 3. Validate scene dependencies; runs after all services are registered
        foreach (var sceneType in _scenesToValidate)
            ValidateSceneDependencies(sceneType);

        var host = _hostBuilder.Build();
        return new GameApplication(host);
    }

    /// <summary>
    /// Validates a scene's constructor dependencies against the fully-populated service collection.
    /// Respects <see cref="ActivatorUtilitiesConstructorAttribute"/> for constructor selection,
    /// matching the behavior of <see cref="ActivatorUtilities"/>.
    /// </summary>
    private void ValidateSceneDependencies(Type sceneType)
    {
        var constructors = sceneType.GetConstructors();
        if (constructors.Length == 0) return;

        var constructor =
            constructors.FirstOrDefault(c => c.GetCustomAttribute<ActivatorUtilitiesConstructorAttribute>() != null)
            // Fallback: pick the first declared constructor, matching ActivatorUtilities' own behavior
            // for the common single-constructor case. For types with multiple constructors and no
            // [ActivatorUtilitiesConstructor] attribute, ActivatorUtilities actually picks the one
            // whose parameters are most satisfiable, but that requires a full resolution attempt.
            // The first-declared heuristic is correct for virtually all scenes and avoids that cost.
            // If validation false-positives on a multi-constructor scene, add [ActivatorUtilitiesConstructor].
            ?? constructors[0];

        var parameters = constructor.GetParameters();
        var errors = new List<string>();

        foreach (var param in parameters)
        {
            var paramType = param.ParameterType;
            if (!Services.Any(d => d.ServiceType == paramType))
            {
                errors.Add(
                    $"  • {param.Name} ({paramType.Name}) - not registered. " +
                    $"Add it via builder.Services.Add...() before calling Build().");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Scene {sceneType.Name} has missing dependencies:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors) + Environment.NewLine +
                Environment.NewLine +
                $"Fix: Register missing services in Program.cs before calling Build()");
        }
    }

    /// <summary>
    /// Validates that all required framework services are registered.
    /// Both SDL3 and headless backends register all four interfaces,
    /// so these checks apply unconditionally.
    /// </summary>
    private void ValidateServiceRegistrations()
    {
        var errors = new List<string>();
        bool HasService<T>() => Services.Any(d => d.ServiceType == typeof(T));

        // Core engine services
        if (!HasService<GameEngine>())
            errors.Add("GameEngine is not registered. This should not happen; please report this bug.");
        if (!HasService<GameLoop>())
            errors.Add("GameLoop is not registered. This should not happen; please report this bug.");
        if (!HasService<ISceneManager>())
            errors.Add("ISceneManager is not registered. This should not happen; please report this bug.");
        if (!HasService<IEntityWorld>())
            errors.Add("IEntityWorld is not registered. This should not happen; please report this bug.");

        // Backend services; both SDL3 and headless modes register all four
        if (!HasService<IRenderer>())
            errors.Add("IRenderer is not registered. SDL3 or headless backend should be auto-registered.");
        if (!HasService<IInputContext>())
            errors.Add("IInputContext is not registered. SDL3 or headless backend should be auto-registered.");
        if (!HasService<IEventPump>())
            errors.Add("IEventPump is not registered. SDL3 or headless backend should be auto-registered.");
        if (!HasService<IMainThreadDispatcher>())
            errors.Add("IMainThreadDispatcher is not registered. SDL3 or headless backend should be auto-registered.");

        // Services required by default scene systems
        if (!HasService<ICameraManager>())
            errors.Add("ICameraManager is not registered. This should not happen; please report this bug.");
        if (!HasService<ICamera>())
            errors.Add("ICamera is not registered. This should not happen; please report this bug.");

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Game application service registration validation failed:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(e => $"  • {e}")) + Environment.NewLine +
                Environment.NewLine +
                "This is likely a framework bug. Please report it with your configuration.");
        }
    }

    private void EnsureNotBuilt()
    {
        if (_built)
            throw new InvalidOperationException(
                "Cannot modify GameApplicationBuilder after Build() has been called." + Environment.NewLine +
                Environment.NewLine +
                "Each builder can only be built once. If you need multiple game instances, create separate builders:" + Environment.NewLine +
                "  var game1 = GameApplication.CreateBuilder().Configure(...).Build();" + Environment.NewLine +
                "  var game2 = GameApplication.CreateBuilder().Configure(...).Build();");
    }
}